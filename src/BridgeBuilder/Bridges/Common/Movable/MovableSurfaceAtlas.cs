using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using UnityEngine.Rendering;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;
using Bound = BridgeBuilder.Bridges.MovableRoadGeometry.BoundTriangle;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Extract a repeating prototype asphalt unit and preserve its measured UV gradients.
// Paint is rendered separately on the original road skeleton.
// Other appearances retain their packed charts.
internal sealed class MovableSurfaceAtlas
{
    internal const uint MovingSurface = 0x80000000;
    internal sealed class Input
    {
        internal bool PrototypeRoad;
        internal Bound[] Faces = Array.Empty<Bound>();
        internal SurfaceAsset[] Materials = Array.Empty<SurfaceAsset>();
        internal MovableExistingRoadSurface.Patch[] Patches = Array.Empty<MovableExistingRoadSurface.Patch>();
        internal SurfaceAsset Native = null!;
        internal Matrix4x4 SourceFrame = Matrix4x4.identity;
    }
    private sealed class Chart
    {
        internal Vector3 Origin, U, V, Normal;
        internal Vector2 Min, Max;
        internal Vector2 Density;
        internal int X, Y, W, H, Page, Width, Height;
        internal MovableExistingRoadSurface.Patch Patch;
        internal SurfaceAsset Source = null!;
        internal SurfaceAsset? Prepared;
        internal bool Repeat;
        internal bool RepeatCutout;
        internal bool NativeRoad;
        internal bool Periodic;
        internal SurfaceAsset? RoadSource;
        internal MovableExistingRoadSurface.Patch RoadPatch;
        internal Matrix4x4 Frame;
        internal Chart Copy() => (Chart)MemberwiseClone();
        internal object NativePeriodicKey(int padding)
        {
            // Match the actual GPU origin, including the transverse guard band.
            // A coordinate may be omitted only when neither native UV projection
            // nor either worldspace texture coordinate reads it. This lets equal
            // horizontal LOD surfaces share a unit despite different elevations,
            // without rounding or wrapping phases or changing the bake inputs.
            var origin=Origin+U*Min.x+V*Min.y-U*(padding/Density.x);
            for(int axis=0;axis<3;axis++)
                if(Patch.U[axis]==0f && Patch.V[axis]==0f && Frame[0,axis]==0f && Frame[2,axis]==0f)
                    origin[axis]=0f;
            return new { Source, Frame, U, V, W, H, Density, Origin=origin,
                Patch.Min, Patch.Max, PatchU=Patch.U, PatchV=Patch.V };
        }
    }
    private static float ProjectionDeterminant(MovableExistingRoadSurface.Patch patch, Vector3 normal)
        => Vector3.Dot((Vector3)patch.U, Vector3.Cross((Vector3)patch.V, normal));
    private static readonly string[] SampledChannels={"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask"};
    private static bool UsesZeroControl(IReadOnlyList<Chart> page, int width, int height,
        bool hasPaint, ISet<SurfaceAsset> authoredControls)
    {
        if(hasPaint || page.Count!=1)return false;
        var c=page[0];
        return (c.Repeat || c.Periodic) && c.X==0 && c.Y==0 && c.W==width && c.H==height &&
            !authoredControls.Contains(c.Source);
    }
    internal static Triangle[][] Compose(Input[] inputs, MovableRoadMaterial owner,
        IReadOnlyList<MovableNativeDecals.Region> paint, out SurfaceAsset[][] materials)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var charts = new List<Chart>();
        var panels = new List<(Triangle Source, Vertex[] Vertices, Chart? Chart)>[inputs.Length];
        for (var level = 0; level < inputs.Length; level++)
        {
            var input = inputs[level]; panels[level] = new();
            foreach (var face in input.Faces)
            {
                var t = face.Geometry;
                var panel = (Source: face, Vertices: new[] { t.A, t.B, t.C });
                var original = input.Materials[t.Material]; original.LoadHeader();
                if (MovableSurfaceAssets.IsObjectSurface(original))
                { panels[level].Add((t, panel.Vertices, null)); continue; }
                var nativeRoad = (panel.Source.Bone & MovingSurface) != 0;
                var roadPatch = nativeRoad ? input.Patches[panel.Source.Bone & ~MovingSurface] : default;
                var roadSource = nativeRoad && input.PrototypeRoad ? input.Native : null;
                var patch = nativeRoad ? OriginalProjection(t) : input.Patches[panel.Source.Bone];
                // A complete existing face inside one Draw03 wear band can use
                // that immutable unit directly. Keep the selected-road mask's
                // original projection; actual material-domain cuts happen upstream.
                if (owner.StyleId == "Draw03")
                {
                    if (nativeRoad && roadSource != null && Draw03RoadWearUnits.TryReduce(
                        roadPatch, t.A.Position, t.B.Position, t.C.Position, out var roadUnit))
                        roadPatch = roadUnit;
                    else if (!nativeRoad && Draw03RoadWearUnits.TryReduce(
                        patch, t.A.Position, t.B.Position, t.C.Position, out var unit))
                        patch = unit;
                }
                var normal = Vector3.Cross(t.B.Position-t.A.Position,t.C.Position-t.A.Position).normalized;
                // A triangle edge is not a texture coordinate system: its arbitrary
                // diagonal duplicates charts across pieces/LODs and wastes huge rectangles.
                // Use one deterministic frame per plane; appearance remains in Patch U/V.
                var axis = Mathf.Abs(Vector3.Dot(normal, Vector3.right)) < .9f
                    ? Vector3.right : Vector3.forward;
                var u = Vector3.ProjectOnPlane(axis, normal).normalized;
                var v = Vector3.Cross(normal,u);
                var origin = normal * Vector3.Dot(normal, t.A.Position);
                var min = new Vector2(float.MaxValue,float.MaxValue);
                var max = new Vector2(float.MinValue,float.MinValue);
                foreach (var vertex in panel.Vertices)
                {
                    var d = vertex.Position-origin;
                    var q = new Vector2(Vector3.Dot(d,u),Vector3.Dot(d,v));
                    min=Vector2.Min(min,q); max=Vector2.Max(max,q);
                }
                var source = nativeRoad ? original : patch.Surface ?? input.Native;
                // The material's tangent frame uses authored vertex normals, which may
                // differ from the geometric plane normal at curbs and smooth edges.
                // Decide the complete receiver representation before composing alpha.
                // Prototype patch composition already evaluates frac(Patch UV).
                // Baking that repeat over the whole receiver discards resolution.
                // All valid prototype patch projections can use a native-sized unit;
                // mixed selected-road layers still require mask-aware composition.
                bool repeat=!nativeRoad &&
                    MovableRoadGeometry.CanRepeatProjection(patch.U,patch.V,normal,t.A.Normal,t.B.Normal,t.C.Normal);
                var repeatSource=source==owner.Asset ? input.Native : source;
                var repeatCutout=patch.Repeat || source!=owner.Asset;
                var chart = charts.FirstOrDefault(c => c.NativeRoad==nativeRoad && c.RoadSource==roadSource && c.RoadPatch.Equals(roadPatch) && c.Frame==input.SourceFrame && c.Source==(repeat ? repeatSource : source) && c.Origin==origin && c.U==u && c.V==v &&
                    c.Repeat==repeat && c.RepeatCutout==repeatCutout &&
                    c.Patch.Min==patch.Min && c.Patch.Max==patch.Max &&
                    c.Patch.U==patch.U && c.Patch.V==patch.V && c.Patch.Bands==patch.Bands && c.Patch.BandRows==patch.BandRows);
                if (chart == null)
                {
                    chart = new Chart { RoadSource=roadSource, RoadPatch=roadPatch, NativeRoad=nativeRoad, Frame=input.SourceFrame, Origin=origin,U=u,V=v,Normal=normal,Min=min,Max=max,Patch=patch,Source=repeat ? repeatSource : source };
                    chart.Repeat=repeat;
                    // The old composite path was cutout/double-sided. Preserve it
                    // for local prototype patches; only asphalt uses opaque Default.
                    chart.RepeatCutout=repeatCutout;
                    charts.Add(chart);
                }
                else
                {
                    // Recompute in the retained frame (Unity vector equality tolerates
                    // roundoff); never reinterpret the panel's local min/max in another frame.
                    foreach (var vertex in panel.Vertices)
                    {
                        var d = vertex.Position-chart.Origin;
                        var q = new Vector2(Vector3.Dot(d,chart.U),Vector3.Dot(d,chart.V));
                        chart.Min=Vector2.Min(chart.Min,q); chart.Max=Vector2.Max(chart.Max,q);
                    }
                }
                panels[level].Add((t,panel.Vertices,chart));
            }
        }
        // Compatible object surfaces already carry their complete native appearance.
        // Empty LODs and all-original inputs need neither a GPU program nor an atlas.
        if (charts.Count == 0)
        {
            materials = inputs.Select(input => input.Materials).ToArray();
            return inputs.Select(input => input.Faces.Select(face => face.Geometry).ToArray()).ToArray();
        }
        const int padding=8;
        ComputeShader? program=null;
        ComputeShader Program()=>program??=MovableGpuBake.CreateProgram();
        var targets=new RenderTexture[4];
        var sources=new Dictionary<SurfaceAsset,Material>();
        var asphaltSources=new HashSet<SurfaceAsset>();
        var authoredControls=new HashSet<SurfaceAsset>();
        var sourceReaders=new List<MovableSurfaceReader>();
        try
        {
            Material Load(SurfaceAsset asset)
            {
                if(sources.TryGetValue(asset,out var cached))return cached;
                asset.LoadProperties(true);
                try
                {
                    if(asset.TryGetTexture("_WorldspaceAlbedo",out var worldAlbedo) && worldAlbedo!=null &&
                        worldAlbedo.id==owner.AsphaltTexture.id) asphaltSources.Add(asset);
                    var sourceReader = new MovableSurfaceReader(asset);
                    sourceReaders.Add(sourceReader);
                    if(sourceReader.HasAuthoredTexture("_ControlMask")) authoredControls.Add(asset);
                    cached = sourceReader.Material;
                    // Compare decoded native inputs with their composed output when
                    // diagnosing endpoint-only shading; do not touch live city assets.
                    var bindings = new System.Text.StringBuilder();
                    foreach (var property in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_ControlMask" })
                    {
                        var texture = cached.GetTexture(property);
                        bindings.Append($" {property}={texture?.name}/{texture?.width}x{texture?.height}/{texture?.graphicsFormat};");
                    }
                    Mod.Log.Info($"Composite input binding: source={asset.name}; template={asset.materialTemplateHash}; VT={asset.isVTMaterial};{bindings}");
                    sources.Add(asset,cached);return cached;
                }
                finally {asset.UnloadProperties();}
            }
            void Bind(int kernel,SurfaceAsset asset)
            {
                var m=Load(asset);
                foreach(var property in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
                    Program().SetTexture(kernel,property,m.GetTexture(property) ?? throw new InvalidOperationException($"Missing {property} on {asset.name}."));
                // Network worldspace albedo and native materials without an authored
                // control texture have no object color-variation regions to transfer.
                // Object sources retain their authored mask in the same atlas coordinates.
                // Omitting this binding invokes the native shader's WHITE default and
                // recolors the entire receiver through the mechanism's color palette.
                Program().SetTexture(kernel,"_ControlMask",!authoredControls.Contains(asset)
                    ? Texture2D.blackTexture
                    : m.GetTexture("_ControlMask") ?? throw new InvalidOperationException($"Missing _ControlMask on {asset.name}."));
            }
            SurfaceAsset PaintSource(bool yellow)
            {
                var cid=Colossal.Hash128.Parse(yellow?"97291ab7f7938fc35cc670b15f2bca36":"792a855e9d0c5c34d2c41d48b062139d");
                if(!AssetDatabase.global.TryGetAsset(cid,out SurfaceAsset result))throw new InvalidOperationException("Native paint material unavailable.");
                return result;
            }
            // Pure asphalt must never enter a whole-span atlas: packing an 84 m
            // receiver into 4096 pixels destroys its native resolution. Inspect the
            // complete source mask footprint and send homogeneous receivers to the
            // existing native-sized repeating tile, keeping mixed surfaces composed.
            foreach(var group in charts.Where(c=>c.NativeRoad && c.RoadSource!=null).GroupBy(c=>c.Source).ToArray())
            {
                var sourceMaterial=Load(group.Key);
                if(!asphaltSources.Contains(group.Key))continue;
                var domains=new HashSet<Chart>(group);
                var candidates=panels.SelectMany((level,li)=>level.Select((p,pi)=>(Panel:p,Level:li,Index:pi)))
                    .Where(item=>item.Panel.Chart!=null && domains.Contains(item.Panel.Chart) &&
                        MovableRoadGeometry.CanRepeatProjection(item.Panel.Chart.RoadPatch.U,
                            item.Panel.Chart.RoadPatch.V,item.Panel.Chart.Normal,
                            item.Panel.Vertices[0].Normal,item.Panel.Vertices[1].Normal,item.Panel.Vertices[2].Normal)).ToArray();
                if(candidates.Length==0)continue;
                var footprints=new List<Vector4>();
                var footprintIds=new int[candidates.Length];
                var unique=new Dictionary<(Vector2,Vector2,Vector2),int>();
                for(int ci=0;ci<candidates.Length;ci++)
                {
                    var panel=candidates[ci].Panel;var c=panel.Chart!;
                    Vector2 Uv(Vertex vertex)
                    {
                        var position=vertex.Position;
                        var p=new Vector4(position.x,position.y,position.z,1);
                        return new Vector2(Vector4.Dot(p,c.Patch.U),Vector4.Dot(p,c.Patch.V));
                    }
                    var key=(Uv(panel.Vertices[0]),Uv(panel.Vertices[1]),Uv(panel.Vertices[2]));
                    if(!unique.TryGetValue(key,out var id))
                    {
                        id=unique.Count;unique.Add(key,id);
                        foreach(var uv in new[]{key.Item1,key.Item2,key.Item3})footprints.Add(new Vector4(uv.x,uv.y,0,0));
                    }
                    footprintIds[ci]=id;
                }
                using var boundsBuffer=new ComputeBuffer(footprints.Count,16);
                using var resultBuffer=new ComputeBuffer(unique.Count,4);
                boundsBuffer.SetData(footprints);
                var classifier=Program().FindKernel("ClassifyAsphaltTriangles");
                var mask=sourceMaterial.GetTexture("_MaskMap");
                Program().SetTexture(classifier,"_MaskMap",mask);
                Program().SetInt("AsphaltWrapU",(int)mask.wrapModeU);
                Program().SetInt("AsphaltWrapV",(int)mask.wrapModeV);
                Program().SetBuffer(classifier,"AsphaltTriangles",boundsBuffer);
                Program().SetBuffer(classifier,"AsphaltOnly",resultBuffer);
                Program().Dispatch(classifier,unique.Count,1,1);
                var pure=new uint[unique.Count]; resultBuffer.GetData(pure);
                var repeating=new Dictionary<Chart,Chart>();
                int tiled=0;
                for(int ci=0;ci<candidates.Length;ci++)if(pure[footprintIds[ci]]!=0)
                {
                    var item=candidates[ci];var c=item.Panel.Chart!;
                    if(!repeating.TryGetValue(c,out var tile))
                    {
                        tile=new Chart {Source=c.RoadSource!,Patch=c.RoadPatch,Repeat=true,RepeatCutout=c.RoadPatch.Repeat,
                            Origin=c.Origin,U=c.U,V=c.V,Normal=c.Normal,Frame=c.Frame,Min=c.Min,Max=c.Max};
                        repeating.Add(c,tile);charts.Add(tile);
                    }
                    panels[item.Level][item.Index]=(item.Panel.Source,item.Panel.Vertices,tile);tiled++;
                }
                Mod.Log.Info($"Native-resolution asphalt: source={group.Key.name}; tiled triangles={tiled}/{candidates.Length}; unique footprints={unique.Count}; sampler={mask.wrapModeU}/{mask.wrapModeV}; homogeneous surfaces bypass atlas downscaling.");
            }
            // Remove vacated charts and recompute mixed-domain bounds after routing.
            // A pure triangle cannot make an unrelated curb allocate a full road span.
            var retained=panels.SelectMany(p=>p).Where(p=>p.Chart!=null).GroupBy(p=>p.Chart!).ToArray();
            charts=retained.Select(g=>g.Key).ToList();
            foreach(var domain in retained.Where(g=>!g.Key.Repeat))
            {
                var c=domain.Key;c.Min=new Vector2(float.MaxValue,float.MaxValue);c.Max=-c.Min;
                foreach(var panel in domain)foreach(var vertex in panel.Vertices)
                {
                    var d=vertex.Position-c.Origin;
                    var q=new Vector2(Vector3.Dot(d,c.U),Vector3.Dot(d,c.V));
                    c.Min=Vector2.Min(c.Min,q);c.Max=Vector2.Max(c.Max,q);
                }
            }
            // Resolve immutable source recipes before decoding textures or allocating
            // atlas pages. Prepared assets belong to the catalogue, not preview cleanup.
            var sourceDigests=new Dictionary<SurfaceAsset,string>();
            foreach(var c in charts.Where(c=>c.Repeat))
            {
                // Never open an unrelated generated bridge's descriptor for lookup.
                if(!c.Source.isBuiltin && !(BridgePreparedMaterials.TryGet(c.Source.id.guid.ToString(),out var sourceAsset) &&
                    ReferenceEquals(sourceAsset,c.Source)))continue;
                if(!sourceDigests.TryGetValue(c.Source,out var digest))
                {
                    using var stream=c.Source.GetReadStream();using var bytes=new MemoryStream();stream.CopyTo(bytes);
                    sourceDigests.Add(c.Source,digest=MovableStructureKey.Digest(bytes.ToArray()));
                }
                var key=MovableRepeatKey.For(digest,c.Patch.Min,c.Patch.Max,c.RepeatCutout,c.Patch.Bands,c.Patch.BandRows);
                c.Prepared=BridgePreparedMaterials.Repeat(owner.StyleId,key);
            }
            int preparedCharts=charts.Count(c=>c.Prepared!=null);
            charts.RemoveAll(c=>c.Prepared!=null);
            Mod.Log.Info($"Prepared road repeats: style={owner.StyleId}; preparedCharts={preparedCharts}; remainingCharts={charts.Count}.");
            // Native object groups partition material batches. A batch budget must
            // never reduce source resolution or force all receivers onto one page.
            var pageSize=charts.SelectMany(c=>SampledChannels.Select(p=>Load(c.Source).GetTexture(p)))
                .Where(t=>t!=null).Select(t=>Math.Max(t.width,t.height)).DefaultIfEmpty(32).Max();
            if(pageSize>SystemInfo.maxTextureSize)
                throw new InvalidOperationException("Native appearance page exceeds GPU texture dimensions.");
            var units=new Dictionary<(SurfaceAsset,string,bool),Chart>();
            var aliases=new Dictionary<Chart,Chart>();
            var desired=new Dictionary<Chart,Vector2>();
            var panelLookup=panels.SelectMany((level,li)=>level.Select((p,pi)=>(Panel:p,Level:li,Index:pi)))
                .Where(p=>p.Panel.Chart!=null).ToLookup(p=>p.Panel.Chart!);
            void AddFinite(Chart c)
            {
                var span=c.Max-c.Min;
                var pixels=Vector2.Scale(span,c.Density);
                pageSize=Math.Max(pageSize,Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Max(pixels.x,pixels.y))+2*padding));
                if(pageSize>SystemInfo.maxTextureSize)
                    throw new InvalidOperationException("Nonperiodic native road appearance exceeds GPU texture dimensions at source density.");
                // Native composition includes worldspace phase and potentially
                // clamped UVs: prototype repetition keys cannot alias these charts.
                if(c.NativeRoad) { desired.Add(c,pixels); return; }
                var key=(c.Source,MovableAppearanceUnit.Key(c.Origin+c.U*c.Min.x+c.V*c.Min.y,
                    c.U,c.V,span,c.Patch,paint),c.Repeat);
                if(units.TryGetValue(key,out var shared))aliases.Add(c,shared);
                else {units.Add(key,c);desired.Add(c,pixels);}
            }
            foreach(var c in charts.Where(c=>!c.Repeat).ToArray())
            {
                var span=c.Max-c.Min;
                var footprint=new MovableTextureFootprint();
                var cycles=new List<MovableTextureFootprint.Cycle>();
                void Include(Material material,Vector3 gu,Vector3 gv,Vector2 scale,bool explicitRepeat,bool world=false,bool control=false)
                {
                    foreach(var property in world?new[]{"_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"}:SampledChannels)
                    {
                        if(property=="_ControlMask" && !control)continue;
                        var texture=material.GetTexture(property);if(texture==null)continue;
                        var size=new Vector2(scale.x*texture.width,scale.y*texture.height);
                        footprint.Include(Vector2.Scale(new Vector2(Vector3.Dot(c.U,gu),Vector3.Dot(c.U,gv)),size),
                            Vector2.Scale(new Vector2(Vector3.Dot(c.V,gu),Vector3.Dot(c.V,gv)),size));
                        // Explicit frac addresses a complete patch/band unit. Its
                        // period is independent of the atlas sampler's clamp mode.
                        if(texture.width>1)cycles.Add(new(Vector3.Dot(c.V,gu),texture.width,explicitRepeat?TextureWrapMode.Repeat:texture.wrapModeU));
                        if(texture.height>1)cycles.Add(new(Vector3.Dot(c.V,gv),texture.height,explicitRepeat?TextureWrapMode.Repeat:texture.wrapModeV));
                    }
                }
                Include(Load(c.Source),c.Patch.U,c.Patch.V,c.Patch.Max-c.Patch.Min,!c.NativeRoad,control:authoredControls.Contains(c.Source));
                if(c.NativeRoad)
                {
                    var m=Load(c.Source);
                    bool replace=c.RoadSource!=null && asphaltSources.Contains(c.Source) &&
                        Mathf.Abs(ProjectionDeterminant(c.RoadPatch,c.Normal))>1e-10f;
                    if(replace)
                    {
                        var p=c.RoadPatch;
                        foreach(var band in p.Bands??new[]{new MovableExistingRoadSurface.RepeatBand(0,1,p.Min,p.Max)})
                        {
                            var scale=band.Max-band.Min;
                            if(p.BandRows)scale.y/=band.End-band.Start;else scale.x/=band.End-band.Start;
                            scale.y*=band.RepeatY;
                            Include(Load(c.RoadSource!),p.U,p.V,scale,true);
                        }
                    }
                    else
                    {
                        float scale=m.GetFloat("_WorldspaceUVScale");
                        Include(m,new Vector3(c.Frame.m00,c.Frame.m01,c.Frame.m02)*scale,
                            new Vector3(c.Frame.m20,c.Frame.m21,c.Frame.m22)*scale,Vector2.one,false,true);
                    }
                }
                c.Density=footprint.Density;
                float period=paint.Count==0?MovableTextureFootprint.Period(cycles,span.y,SystemInfo.maxTextureSize/c.Density.y):0;
                if(period>0 && period<span.y)
                {
                    c.Periodic=true;c.Max=new Vector2(c.Max.x,c.Min.y+period);
                    c.W=Math.Max(4,4*Mathf.CeilToInt(span.x*c.Density.x/4))+2*padding;
                    c.H=Math.Max(4,4*Mathf.CeilToInt(period*c.Density.y/4));
                    c.Width=c.W;c.Height=c.H;c.X=c.Y=0;
                    c.Density=new Vector2((c.W-2*padding)/span.x,c.H/period);
                    if(c.W>SystemInfo.maxTextureSize || c.H>SystemInfo.maxTextureSize)
                        throw new InvalidOperationException("Native periodic road unit exceeds GPU texture dimensions.");
                    Mod.Log.Info($"Native periodic composite: source={c.Source.name}; period={period:R}; pixels={c.W}x{c.H}; density={c.Density}; receiver span={span}; no downsampling.");
                    continue;
                }
                var pixels=Vector2.Scale(span,c.Density);
                int capacity=SystemInfo.maxTextureSize-2*padding;
                if(pixels.x<=capacity && pixels.y<=capacity) {AddFinite(c);continue;}
                // A merged coplanar chart may be too large although every original
                // triangle fits. Partition its existing faces before considering the
                // image limit. Preserve the frame, source phase, gradients and topology.
                var members=panelLookup[c].ToArray();
                var bounds=members.Select(item=>
                {
                    var min=new Vector2(float.MaxValue,float.MaxValue);var max=-min;
                    foreach(var vertex in item.Panel.Vertices)
                    {
                        var d=vertex.Position-c.Origin;
                        var q=new Vector2(Vector3.Dot(d,c.U),Vector3.Dot(d,c.V));
                        min=Vector2.Min(min,q);max=Vector2.Max(max,q);
                    }
                    return new MovableChartPartition.Bounds(min.x,min.y,max.x,max.y);
                }).ToArray();
                var groups=MovableChartPartition.Build(bounds,c.Density.x,c.Density.y,capacity);
                charts.Remove(c);
                foreach(var group in groups)
                {
                    var split=c.Copy();var b=group.Extent;
                    split.Min=new Vector2(b.MinX,b.MinY);split.Max=new Vector2(b.MaxX,b.MaxY);
                    charts.Add(split);AddFinite(split);
                    foreach(int id in group.Faces)
                    {
                        var item=members[id];
                        panels[item.Level][item.Index]=(item.Panel.Source,item.Panel.Vertices,split);
                    }
                }
                Mod.Log.Info($"Native road chart partition: source={c.Source.name}; faces={members.Length}; charts={groups.Count}; density={c.Density}; original triangles retained.");
            }
            var pages=new List<List<Chart>>();
            var packingClock=System.Diagnostics.Stopwatch.StartNew();
            {
                foreach(var item in desired)
                {
                    item.Key.W=Mathf.CeilToInt(Mathf.Max(1,item.Value.x))+2*padding;
                    item.Key.H=Mathf.CeilToInt(Mathf.Max(1,item.Value.y))+2*padding;
                }
                var free=new List<List<RectInt>> {new() {new RectInt(0,0,pageSize,pageSize)}};
                // Repeat-only receivers have no packed charts. Their first tile
                // must own page zero, not follow an empty synthetic atlas page.
                if(desired.Count>0)pages.Add(new List<Chart>());
                foreach(var c in desired.Keys.OrderByDescending(c=>Math.Max(c.W,c.H)).ThenByDescending(c=>c.W*c.H))
                {
                    int selected=-1,score=int.MaxValue,area=int.MaxValue;
                    RectInt slot=default;
                    for(int p=0;p<free.Count;p++)
                    foreach(var r in free[p])
                    {
                        // Coplanar paint needs forward alpha blending. Keep it out of
                        // the opaque/cutout pages without adding LOD material batches.
                        if(pages[p].Count>0 && pages[p][0].Repeat!=c.Repeat)continue;
                        if(c.W>r.width || c.H>r.height)continue;
                        int fit=Math.Min(r.width-c.W,r.height-c.H),waste=r.width*r.height-c.W*c.H;
                        if(fit>score || (fit==score && waste>=area))continue;
                        selected=p;slot=r;score=fit;area=waste;
                    }
                    if(selected<0)
                    {
                        selected=pages.Count;pages.Add(new List<Chart>());
                        slot=new RectInt(0,0,pageSize,pageSize);free.Add(new List<RectInt>{slot});
                    }
                    c.Page=selected;c.X=slot.x;c.Y=slot.y;pages[selected].Add(c);
                    var used=new RectInt(c.X,c.Y,c.W,c.H);
                    var remaining=new List<RectInt>();
                    foreach(var r in free[selected])
                    {
                        if(!r.Overlaps(used)){remaining.Add(r);continue;}
                        if(used.xMin>r.xMin)remaining.Add(new RectInt(r.x,r.y,used.xMin-r.xMin,r.height));
                        if(used.xMax<r.xMax)remaining.Add(new RectInt(used.xMax,r.y,r.xMax-used.xMax,r.height));
                        if(used.yMin>r.yMin)remaining.Add(new RectInt(r.x,r.y,r.width,used.yMin-r.yMin));
                        if(used.yMax<r.yMax)remaining.Add(new RectInt(r.x,used.yMax,r.width,r.yMax-used.yMax));
                    }
                    // Free rectangles may overlap; remove contained duplicates, then
                    // split every intersecting rectangle on the next placement.
                    for(int i=remaining.Count-1;i>=0;i--)
                        for(int j=0;j<remaining.Count;j++)
                            if(i!=j && remaining[j].xMin<=remaining[i].xMin && remaining[j].yMin<=remaining[i].yMin &&
                                remaining[j].xMax>=remaining[i].xMax && remaining[j].yMax>=remaining[i].yMax)
                            {remaining.RemoveAt(i);break;}
                    free[selected]=remaining;
                }
            }
            Mod.Log.Info($"Moving-road atlas packing completed: elapsedMs={packingClock.ElapsedMilliseconds}; pages={pages.Count}; native density retained; shrink attempts=0.");
            foreach(var alias in aliases)
            {
                var c=alias.Key;var shared=alias.Value;
                c.Page=shared.Page;c.X=shared.X;c.Y=shared.Y;c.W=shared.W;c.H=shared.H;
            }
            foreach(var c in charts.Where(c=>!c.Repeat && !c.Periodic))
            {
                var span=c.Max-c.Min;
                c.Width=c.Height=pageSize;
                // Native node receivers can collapse to a line on one chart axis. The
                // allocated one-pixel strip still uses the source sampling density there;
                // dividing by zero would turn its constant coordinate into 0 * Infinity.
                c.Density=new Vector2(span.x>0f?(c.W-2f*padding)/span.x:c.Density.x,
                    span.y>0f?(c.H-2f*padding)/span.y:c.Density.y);
            }
            var periodicUnits=new Dictionary<object,Chart>();
            foreach(var c in charts.Where(c=>c.Periodic))
            {
                // Per-generation sharing of identical source recipes only. No
                // result survives this generation and no approximate phase key is used.
                object key=c;
                if(c.NativeRoad && c.RoadSource==null)
                    key=c.NativePeriodicKey(padding);
                if(c.NativeRoad && c.RoadSource!=null && asphaltSources.Contains(c.Source) &&
                    Mathf.Abs(ProjectionDeterminant(c.RoadPatch,c.Normal))>1e-10f)
                {
                    var origin=c.Origin+c.U*c.Min.x+c.V*c.Min.y;
                    var textures=SampledChannels.Where(p=>p!="_ControlMask" || authoredControls.Contains(c.Source))
                        .Select(p=>Load(c.Source).GetTexture(p)).Where(t=>t!=null).ToArray();
                    float Phase(Vector4 gradient,bool horizontal)
                    {
                        float phase=Vector3.Dot(origin,gradient)+gradient.w;
                        var wraps=textures.Where(t=>(horizontal?t.width:t.height)>1)
                            .Select(t=>horizontal?t.wrapModeU:t.wrapModeV).Distinct().ToArray();
                        return wraps.Length==1 && wraps[0]==TextureWrapMode.Repeat?Mathf.Repeat(phase,1):
                            wraps.Length==1 && wraps[0]==TextureWrapMode.Mirror?Mathf.Repeat(phase,2):phase;
                    }
                    var rp=c.RoadPatch;
                    key=new {c.Source,c.RoadSource,c.Frame,c.U,c.V,c.W,c.H,c.Density,
                        SourceU=(Vector3)c.Patch.U,SourceV=(Vector3)c.Patch.V,
                        SourcePhase=new Vector2(Phase(c.Patch.U,true),Phase(c.Patch.V,false)),
                        RoadU=(Vector3)rp.U,RoadV=(Vector3)rp.V,
                        RoadPhase=new Vector2(Mathf.Repeat(Vector3.Dot(origin,rp.U)+rp.U.w,1),
                            Mathf.Repeat(Vector3.Dot(origin,rp.V)+rp.V.w,1)),rp.Min,rp.Max,rp.Bands,rp.BandRows};
                }
                if(periodicUnits.TryGetValue(key,out var shared))c.Page=shared.Page;
                else {c.Page=pages.Count;pages.Add(new List<Chart>{c});periodicUnits.Add(key,c);}
            }
            Mod.Log.Info($"Moving-road periodic reuse: style={owner.StyleId}; charts={charts.Count(c=>c.Periodic)}; units={periodicUnits.Count}; scope=current generation; resultCache=false.");
            // Extract one native atlas asphalt unit. Continuous receiver UVs repeat
            // this unit; no road triangle is cut and no whole-span paint overlay remains.
            var repeatPages=new Dictionary<(SurfaceAsset Source,Vector2 Min,Vector2 Max,bool Cutout,MovableExistingRoadSurface.RepeatBand[]? Bands,bool Rows),int>();
            foreach(var c in charts.Where(c=>c.Repeat))
            {
                var key=(c.Source,c.Patch.Min,c.Patch.Max,c.RepeatCutout,c.Patch.Bands,c.Patch.BandRows);
                if(!repeatPages.TryGetValue(key,out var tilePage))
                {
                    var channelTextures=SampledChannels.Select(p=>Load(c.Source).GetTexture(p)).Where(t=>t!=null).ToArray();
                    int nativeWidth=channelTextures.Max(t=>t.width),nativeHeight=channelTextures.Max(t=>t.height);
                    var width=4*Mathf.CeilToInt((c.Patch.Max.x-c.Patch.Min.x)*nativeWidth/4f);
                    var height=4*Mathf.CeilToInt((c.Patch.Max.y-c.Patch.Min.y)*nativeHeight/4f);
                    var bands=c.Patch.Bands;
                    if(bands!=null)
                    {
                        float previous=0f,requiredWidth=0f,requiredHeight=0f;
                        foreach(var band in bands)
                        {
                            if(band.Start!=previous || band.End<=band.Start || band.End>1f ||
                                band.Max.x<=band.Min.x || band.Max.y<=band.Min.y || !float.IsFinite(band.RepeatY) || band.RepeatY<=0f)
                                throw new InvalidOperationException("A repeating surface requires contiguous, non-empty texture bands.");
                            requiredWidth=Mathf.Max(requiredWidth,(band.Max.x-band.Min.x)*nativeWidth/(c.Patch.BandRows?1f:band.End-band.Start));
                            requiredHeight=Mathf.Max(requiredHeight,(band.Max.y-band.Min.y)*band.RepeatY*nativeHeight/(c.Patch.BandRows?band.End-band.Start:1f));
                            previous=band.End;
                        }
                        if(previous!=1f)throw new InvalidOperationException("Repeating surface bands must cover the complete unit.");
                        width=4*Mathf.CeilToInt(requiredWidth/4f);
                        height=4*Mathf.CeilToInt(requiredHeight/4f);
                    }
                    if(width>SystemInfo.maxTextureSize || height>SystemInfo.maxTextureSize)
                        throw new InvalidOperationException("Native repeating surface exceeds supported texture dimensions.");
                    tilePage=pages.Count;repeatPages.Add(key,tilePage);
                    var tile=new List<Chart>();
                    void AddBand(int x,int end,Vector2 min,Vector2 max,float repeatY=1f)
                    {
                        if(end<=x) { if(c.Patch.BandRows)return; throw new InvalidOperationException("A repeating surface band has no output pixels."); }
                        tile.Add(new Chart {
                            Source=c.Source,Repeat=true,Origin=Vector3.zero,
                            U=Vector3.right,V=Vector3.forward,Normal=Vector3.up,Min=Vector2.zero,Max=Vector2.one,
                            X=c.Patch.BandRows?0:x,Y=c.Patch.BandRows?x:0,
                            W=c.Patch.BandRows?width:end-x,H=c.Patch.BandRows?end-x:height,
                            Width=width,Height=height,Density=c.Patch.BandRows?new Vector2(width,end-x):new Vector2(end-x,height),
                            Patch=new MovableExistingRoadSurface.Patch(min,max,
                                new Vector4(1,0,0,0),new Vector4(0,0,repeatY,0),repeat:c.RepeatCutout)});
                    }
                    if(bands==null)AddBand(0,width,c.Patch.Min,c.Patch.Max);
                    else foreach(var band in bands)
                        AddBand(Mathf.RoundToInt(band.Start*(c.Patch.BandRows?height:width)),Mathf.RoundToInt(band.End*(c.Patch.BandRows?height:width)),band.Min,band.Max,band.RepeatY);
                    pages.Add(tile);
                    Mod.Log.Info($"Native repeating unit: {width}x{height}; cutout/double-sided={c.RepeatCutout}; source={c.Source.name}; no span atlas scaling.");
                }
                c.Page=tilePage;
            }
            foreach(var c in desired.Keys)
                Mod.Log.Info($"Moving-road chart density: source={c.Source.name}; span={c.Max-c.Min}; pixels={c.W-2*padding}x{c.H-2*padding}; texels/metre={c.Density}; page={c.Page}.");
            Mod.Log.Info($"Moving-road packed VT: domains={charts.Count}; unique={desired.Count}; pages={pages.Count}; pageSize={pageSize}; packingScale=1; max LOD-group batches=16.");
            // Crop only unused allocation; chart coordinates and pixel density stay fixed.
            // Keep at least 1024 for the native tile/mid-mip layout, or the smaller
            // authored page size. Repeat units retain their exact measured dimensions.
            var pageWidths=new int[pages.Count];var pageHeights=new int[pages.Count];
            for(int page=0;page<pages.Count;page++)
            {
                var entries=pages[page];bool repeat=entries[0].Repeat || entries[0].Periodic;
                int minimum=Math.Min(1024,pageSize);
                pageWidths[page]=repeat?entries[0].Width:Math.Min(pageSize,Math.Max(minimum,Mathf.NextPowerOfTwo(entries.Max(c=>c.X+c.W))));
                pageHeights[page]=repeat?entries[0].Height:Math.Min(pageSize,Math.Max(minimum,Mathf.NextPowerOfTwo(entries.Max(c=>c.Y+c.H))));
                if(entries.Any(c=>c.X+c.W>pageWidths[page] || c.Y+c.H>pageHeights[page]))
                    throw new InvalidOperationException("Appearance chart exceeds its allocated page.");
            }
            // Aliases across LODs are not necessarily members of the page's draw list.
            // They must use the same new UV denominator as their representative chart.
            foreach(var c in charts.Where(c=>!c.Repeat))
            {c.Width=pageWidths[c.Page];c.Height=pageHeights[c.Page];}
            long pixelCount=0;
            for(int page=0;page<pages.Count;page++)pixelCount+=(long)pageWidths[page]*pageHeights[page];
            Mod.Log.Info($"Moving-road VT composition: panels={panels.Sum(p=>p.Count)}; shared charts={charts.Count}; pages={pages.Count}; pixels={pixelCount}; receiver topology unchanged; prototype asphalt repeats; paint uses native decals.");
            var composites=new List<SurfaceAsset>();
            int savedTextures=0,sharedControls=0;
            for(int page=0;page<pages.Count;page++)
            {
                var pageClock=System.Diagnostics.Stopwatch.StartNew();
                long readbackMs=0,textureSaveMs=0;
                bool tiled=pages[page][0].Repeat || pages[page][0].Periodic;
                int width=pageWidths[page];
                int height=pageHeights[page];
                int border=tiled?0:padding;
                bool zeroControl=UsesZeroControl(pages[page],width,height,paint.Count!=0,authoredControls);
                foreach(var c in pages[page]){c.Width=width;c.Height=height;}
                for(int i=0;i<targets.Length;i++)
                {
                    // The disabled control output needs only a bound dummy UAV.
                    // Its full page is neither allocated nor generated.
                    bool unusedControl=zeroControl && i==3;
                    targets[i]=new RenderTexture(unusedControl?1:width,unusedControl?1:height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                    { enableRandomWrite=true,useMipMap=false,autoGenerateMips=false,wrapMode=TextureWrapMode.Clamp };
                    targets[i].Create();
                    var previous=RenderTexture.active;
                    try {RenderTexture.active=targets[i];GL.Clear(false,true,i==1?new Color(.5f,.5f,1,1):Color.clear);}
                    finally {RenderTexture.active=previous;}
                }
                int surfaceKernel=Program().FindKernel("SurfaceAtlas"),paintKernel=Program().FindKernel("PaintAtlas");
                Program().SetInt("SkipAtlasControl",zeroControl?1:0);
                foreach(var kernel in new[]{surfaceKernel,paintKernel})
                {
                    Program().SetTexture(kernel,"AtlasColor",targets[0]);
                    Program().SetTexture(kernel,"AtlasNormal",targets[1]);
                    Program().SetTexture(kernel,"AtlasMask",targets[2]);
                    Program().SetTexture(kernel,"AtlasControl",targets[3]);
                }
                foreach(var c in pages[page])
                {
                    var origin=c.Origin+c.U*c.Min.x+c.V*c.Min.y;
                    // Native TextureAsset serializes one wrap mode for both axes.
                    // Keep transverse endpoints inside a source-sampled guard band;
                    // the longitudinal axis alone uses the complete periodic unit.
                    if(c.Periodic)origin-=c.U*(padding/c.Density.x);
                    Program().SetVector("ChartOrigin",origin);
                    Program().SetVector("ChartU",c.U);Program().SetVector("ChartV",c.V);
                    Program().SetVector("AtlasPixels",new Vector4(c.Density.x,border,c.Density.y,0));
                    Program().SetVector("AtlasRect",new Vector4(c.X,c.Y,c.W,c.H));
                    Program().SetVector("PatchU",c.Patch.U);Program().SetVector("PatchV",c.Patch.V);
                    Program().SetVector("PatchArea",new Vector4(c.Patch.Min.x,c.Patch.Min.y,c.Patch.Max.x-c.Patch.Min.x,c.Patch.Max.y-c.Patch.Min.y));
                    // The last page extracts the prototype asphalt unit. Other pages
                    // contain only opaque/grating appearance, never painted receivers.
                    {
                        Bind(surfaceKernel,c.Source);
                        Program().SetInt("OriginalRoad",c.NativeRoad?1:0);
                        var nativeMaterial=Load(c.Source);
                        var replaceRoad=c.NativeRoad && c.RoadSource!=null && asphaltSources.Contains(c.Source) &&
                            Mathf.Abs(ProjectionDeterminant(c.RoadPatch,c.Normal))>1e-10f;
                        Program().SetInt("ReplaceRoad",replaceRoad?1:0);
                        var roadMaterial=replaceRoad?Load(c.RoadSource!):nativeMaterial;
                        foreach(var channel in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
                            Program().SetTexture(surfaceKernel,"_Road"+channel,roadMaterial.GetTexture(channel));
                        var rp=c.RoadPatch;
                        Program().SetVector("RoadU",rp.U); Program().SetVector("RoadV",rp.V);
                        var bands=rp.Bands ?? new[]{new MovableExistingRoadSurface.RepeatBand(0,1,rp.Min,rp.Max)};
                        var bandData=new Vector4[bands.Length*2];
                        for(int bi=0;bi<bands.Length;bi++) {
                            var b=bands[bi];
                            bandData[bi*2]=new Vector4(b.Start,b.End,b.RepeatY,0);
                            bandData[bi*2+1]=new Vector4(b.Min.x,b.Min.y,b.Max.x-b.Min.x,b.Max.y-b.Min.y);
                        }
                        using var bandBuffer=new ComputeBuffer(bandData.Length,16);
                        bandBuffer.SetData(bandData);
                        Program().SetBuffer(surfaceKernel,"RoadBands",bandBuffer);
                        Program().SetInt("RoadBandCount",bands.Length);
                        Program().SetInt("RoadBandRows",rp.BandRows?1:0);
                        foreach(var property in new[]{"_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})
                            Program().SetTexture(surfaceKernel,property,c.NativeRoad
                                // NetCompositionMeshLit declares optional worldspace
                                // maps as white / bump / white. Harbor roads omit them;
                                // compute bindings must supply the same native defaults.
                                ? nativeMaterial.GetTexture(property) ?? (property=="_WorldspaceNormalMap"
                                    ? Texture2D.normalTexture : Texture2D.whiteTexture)
                                : Texture2D.whiteTexture);
                        Program().SetMatrix("Frame",c.Frame);
                        Program().SetFloat("WorldScale",c.NativeRoad?nativeMaterial.GetFloat("_WorldspaceUVScale"):0);
                        Program().SetVector("BaseColor",c.NativeRoad?nativeMaterial.GetColor("_BaseColor"):Color.white);
                        Program().SetVector("NativeValues",c.NativeRoad?new Vector4(nativeMaterial.GetFloat("_Metallic"),nativeMaterial.GetFloat("_Smoothness"),0,0):Vector4.one);
                        Program().Dispatch(surfaceKernel,(c.W+7)/8,(c.H+7)/8,1);
                    }
                    // Paint projects only onto upward receivers, preserving vertical curb sides.
                    if(Vector3.Dot(c.Normal,Vector3.up)<.5f)continue;
                    foreach(var stroke in paint)
                    {
                        var d=stroke.Center-origin;
                        var center=new Vector2(Vector3.Dot(d,c.U),Vector3.Dot(d,c.V));
                        float ru=Math.Abs(Vector3.Dot(stroke.Right,c.U))*stroke.Half.x+Math.Abs(Vector3.Dot(stroke.Forward,c.U))*stroke.Half.z+Math.Abs(Vector3.Dot(stroke.Up,c.U))*stroke.Half.y;
                        float rv=Math.Abs(Vector3.Dot(stroke.Right,c.V))*stroke.Half.x+Math.Abs(Vector3.Dot(stroke.Forward,c.V))*stroke.Half.z+Math.Abs(Vector3.Dot(stroke.Up,c.V))*stroke.Half.y;
                        int x0=Math.Max(0,Mathf.FloorToInt((center.x-ru)*c.Density.x)+padding),y0=Math.Max(0,Mathf.FloorToInt((center.y-rv)*c.Density.y)+padding);
                        int x1=Math.Min(c.W,Mathf.CeilToInt((center.x+ru)*c.Density.x)+padding),y1=Math.Min(c.H,Mathf.CeilToInt((center.y+rv)*c.Density.y)+padding);
                        if(x1<=x0 || y1<=y0)continue;
                        Program().SetVector("PaintRect",new Vector4(x0,y0,x1-x0,y1-y0));
                        Program().SetVector("PaintCenter",stroke.Center);Program().SetVector("PaintRight",stroke.Right);
                        Program().SetVector("PaintUp",stroke.Up);Program().SetVector("PaintForward",stroke.Forward);Program().SetVector("PaintHalf",stroke.Half);
                        Program().SetVector("PaintColor",stroke.Yellow?new Vector4(800f/4096,3000f/4096,0,0):new Vector4(3500f/4096,820f/4096,0,0));
                        Bind(paintKernel,PaintSource(stroke.Yellow));
                        Program().Dispatch(paintKernel,(x1-x0+7)/8,(y1-y0+7)/8,1);
                    }
                }
                var textures=new Dictionary<string,TextureAsset>();
                // SurfaceAtlas samples Unity's transparent black default when the
                // source has no authored control. A complete unpainted repeat page
                // is therefore the same constant at every mip. Keep VT/padded and
                // authored-control pages on their existing channel path.
                int channelCount=zeroControl?3:targets.Length;
                if(zeroControl)
                {
                    textures.Add("_ControlMask",BridgePreparedMaterials.ZeroControl(owner.StyleId));
                    sharedControls++;
                }
                var pending=new Queue<MovableAtlasChannel>();
                long submitMs=0;
                int nextChannel=0,peakChannels=0;
                try
                {
                    while(nextChannel<channelCount || pending.Count>0)
                    {
                        while(nextChannel<channelCount && pending.Count<2)
                        {
                            var submitClock=System.Diagnostics.Stopwatch.StartNew();
                            pending.Enqueue(new MovableAtlasChannel(Program(),targets[nextChannel],nextChannel,tiled));
                            submitMs+=submitClock.ElapsedMilliseconds;
                            nextChannel++;
                            peakChannels=Math.Max(peakChannels,pending.Count);
                        }
                        using(var channel=pending.Dequeue())
                        {
                            var readbackClock=System.Diagnostics.Stopwatch.StartNew();
                            channel.Read();
                            readbackMs+=readbackClock.ElapsedMilliseconds;
                            // The next channel is already submitted while this native save runs.
                            var saveClock=System.Diagnostics.Stopwatch.StartNew();
                            textures.Add(new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask"}[channel.Channel],
                                owner.SaveTexture(channel.Texture,channel.Channel,page));
                            savedTextures++;
                            textureSaveMs+=saveClock.ElapsedMilliseconds;
                        }
                    }
                }
                finally
                {
                    while(pending.Count>0)pending.Dequeue().Dispose();
                }
                Mod.Log.Info($"Moving-road channel pipeline: page={page}; peakChannels={peakChannels}; submitMs={submitMs}; readbackMs={readbackMs}; saveMs={textureSaveMs}; preparedZeroControl={zeroControl}; resultCache=false.");
                composites.Add(tiled
                    ? owner.SaveRepeat(textures,page,pages[page][0].Periodic || pages[page][0].Patch.Repeat)
                    : owner.SaveComposite(textures,page));
                foreach(var target in targets){target.Release();UnityEngine.Object.Destroy(target);}
                Array.Clear(targets,0,targets.Length);
                Mod.Log.Info($"Moving-road appearance VT page {page}: size={width}x{height}; charts={pages[page].Count}; totalMs={pageClock.ElapsedMilliseconds}; GPU-readbackMs={readbackMs}; texture-saveMs={textureSaveMs}.");
            }
            owner.CompleteComposites(composites.Where(surface=>surface.hasVTSurfaceAsset).ToArray());
            var result=new Triangle[inputs.Length][];materials=new SurfaceAsset[inputs.Length][];
            for(int level=0;level<inputs.Length;level++)
            {
                var slots=new List<SurfaceAsset>(composites);var output=new List<Triangle>();

                foreach(var panel in panels[level])
                {
                    var c=panel.Chart;var t=panel.Source;
                    var material=c?.Page ?? 0;
                    if(c?.Prepared!=null){material=slots.IndexOf(c.Prepared);if(material<0){material=slots.Count;slots.Add(c.Prepared);}}
                    if(c==null){var original=inputs[level].Materials[t.Material];material=slots.IndexOf(original);if(material<0){material=slots.Count;slots.Add(original);}}
                    Vertex Remap(Vertex vertex)
                    {
                        if(c==null)return vertex;
                        var d=vertex.Position-c.Origin;
                        var uv=new Vector4((c.X+padding+(Vector3.Dot(d,c.U)-c.Min.x)*c.Density.x)/c.Width,
                            (c.Y+(c.Periodic?0:padding)+(Vector3.Dot(d,c.V)-c.Min.y)*c.Density.y)/c.Height,0,0);
                        // Chart padding contains the same projected surface outside the boundary.
                        // UVs address this rectangle, not the full shared page.
                        return new Vertex(vertex.Position,vertex.Normal,new Vector4(c.U.x,c.U.y,c.U.z,1),uv,Color.white);
                    }
                    var v=panel.Vertices;
                    if(c?.Repeat==true)
                    {
                        Vertex Asphalt(Vertex vertex)
                        {
                            var p=new Vector4(vertex.Position.x,vertex.Position.y,vertex.Position.z,1);
                            var uv=new Vector4(Vector4.Dot(p,c.Patch.U),Vector4.Dot(p,c.Patch.V),0,0);
                            var normal=vertex.Normal;
                            var tangent=Vector3.Cross((Vector3)c.Patch.V,normal);
                            var determinant=ProjectionDeterminant(c.Patch,normal);
                            if(Mathf.Abs(determinant)<1e-10f)
                                throw new InvalidOperationException("Native asphalt projection has a singular tangent frame.");
                            tangent=(tangent/determinant).normalized;
                            return new Vertex(vertex.Position,normal,
                                new Vector4(tangent.x,tangent.y,tangent.z,determinant>0?1:-1),uv,Color.white);
                        }
                        output.Add(new Triangle(Asphalt(v[0]),Asphalt(v[1]),Asphalt(v[2]),
                            material,t.Occurrence,t.SourceTriangle));
                        continue;
                    }
                    output.Add(new Triangle(Remap(v[0]),Remap(v[1]),Remap(v[2]),material,t.Occurrence,t.SourceTriangle));
                }
                result[level]=output.ToArray();materials[level]=slots.ToArray();
                Mod.Log.Info($"Moving-road atlas LOD{level+1}: input={inputs[level].Faces.Length}; output={output.Count}; skinned paint is separate; coalescing=0; repetition cuts=0; prototype asphalt UV gradients retained.");
            }
            Mod.Log.Info($"Moving-road GPU appearance units: charts={charts.Count}; packed pages={pages.Count}; native-sized atlas pages; textures={savedTextures} shared across LODs; preparedControls={sharedControls}; ms={clock.ElapsedMilliseconds}.");
            return result;
        }
        finally
        {
            foreach(var reader in sourceReaders)reader.Dispose();
            foreach(var target in targets)if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}
            if(program!=null)UnityEngine.Object.Destroy(program);
        }
    }

    private static MovableExistingRoadSurface.Patch OriginalProjection(Triangle triangle)
    {
        var a=triangle.A; var e=triangle.B.Position-a.Position; var f=triangle.C.Position-a.Position;
        var cross=Vector3.Cross(e,f);
        var det=cross.sqrMagnitude;
        if(det<=0f)throw new InvalidOperationException("Native road UV plane is degenerate.");
        Vector4 Gradient(float av,float bv,float cv)
        {
            var gradient=(Vector3.Cross(f,cross)*(bv-av)+Vector3.Cross(cross,e)*(cv-av))/det;
            return new Vector4(gradient.x,gradient.y,gradient.z,av-Vector3.Dot(gradient,a.Position));
        }
        return new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,
            Gradient(a.UV.x,triangle.B.UV.x,triangle.C.UV.x),Gradient(a.UV.y,triangle.B.UV.y,triangle.C.UV.y));
    }
}
