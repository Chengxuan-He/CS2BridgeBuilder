using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    internal sealed class Input
    {
        internal Bound[] Faces = Array.Empty<Bound>();
        internal SurfaceAsset[] Materials = Array.Empty<SurfaceAsset>();
        internal MovableExistingRoadSurface.Patch[] Patches = Array.Empty<MovableExistingRoadSurface.Patch>();
        internal SurfaceAsset Native = null!;
    }
    private sealed class Chart
    {
        internal Vector3 Origin, U, V, Normal;
        internal Vector2 Min, Max;
        internal Vector2 Density;
        internal int X, Y, W, H, Page, Width, Height;
        internal MovableExistingRoadSurface.Patch Patch;
        internal SurfaceAsset Source = null!;
        internal bool Repeat;
    }
    private static float ProjectionDeterminant(MovableExistingRoadSurface.Patch patch, Vector3 normal)
        => Vector3.Dot((Vector3)patch.U, Vector3.Cross((Vector3)patch.V, normal));
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
                var patch = input.Patches[panel.Source.Bone];
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
                var source = patch.Surface ?? input.Native;
                // The material's tangent frame uses authored vertex normals, which may
                // differ from the geometric plane normal at curbs and smooth edges.
                // Decide the complete receiver representation before composing alpha.
                bool repeat=(source==owner.Asset || patch.Repeat) &&
                    MovableRoadGeometry.CanRepeatProjection(patch.U,patch.V,normal,t.A.Normal,t.B.Normal,t.C.Normal);
                var chart = charts.FirstOrDefault(c => c.Source==(repeat ? input.Native : source) && c.Origin==origin && c.U==u && c.V==v &&
                    c.Repeat==repeat &&
                    c.Patch.Min==patch.Min && c.Patch.Max==patch.Max &&
                    c.Patch.U==patch.U && c.Patch.V==patch.V && c.Patch.Bands==patch.Bands);
                if (chart == null)
                {
                    chart = new Chart { Origin=origin,U=u,V=v,Normal=normal,Min=min,Max=max,Patch=patch,Source=repeat ? input.Native : source };
                    chart.Repeat=repeat;
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
        const int padding=8;
        var program=MovableGpuBake.CreateProgram();
        var targets=new RenderTexture[4];
        var sources=new Dictionary<SurfaceAsset,Material>();
        var authoredControls=new HashSet<SurfaceAsset>();
        var ownedMaterials=new HashSet<Material>();
        var ownedTextures=new HashSet<Texture>();
        var loadedVtTextures=new HashSet<VTTextureAsset>();
        try
        {
            Material Load(SurfaceAsset asset)
            {
                if(sources.TryGetValue(asset,out var cached))return cached;
                asset.LoadProperties(true);
                try
                {
                    if(asset.TryGetTexture("_ControlMask",out var control) && control!=null)
                        authoredControls.Add(asset);
                    if(asset.isVTMaterial)
                    {
                        // Decode a private read-only native source. Never unload or mutate
                        // the game's live material/VT resources.
                        var handler=asset.hasVTSurfaceAsset?new VTSurfaceDataHandler():null;
                        try
                        {
                            var template=AssetDatabase.global.resources.materialLibrary.GetMaterial(asset.materialTemplateHash);
                            cached=new Material(template);
                            ownedMaterials.Add(cached);
                            var description=AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(asset.materialTemplateHash);
                            try
                            {
                                if(handler!=null)
                                {
                                    using var reader=new BinaryReader(asset.vtSurfaceAsset.GetReadStream());
                                    handler.ReadHeader(reader);
                                    handler.FillDebugMaterial(reader,cached,description);
                                }
                            }
                            finally
                            {
                                foreach(var property in cached.GetTexturePropertyNames())
                                    if(cached.GetTexture(property) is Texture2D texture && texture!=template.GetTexture(property))
                                        ownedTextures.Add(texture);
                            }
                            // FillDebugMaterial decodes only layers packed inside VTSurface.
                            // Native LoadTexturesVT resolves the remaining layers by their
                            // preprocessed texture CIDs, not the original Texture provenance.
                            var guids=asset.preProcessedTextureGuids.ToArray();
                            for(int stack=0;stack<asset.stackCount;stack++)
                            for(int layer=0;layer<description.m_Stacks[stack].m_PropertyNames.Length;layer++)
                            {
                                if(handler!=null && handler.HandledByVTSurfaceData(stack,layer))continue;
                                var cid=guids[stack*4+layer];
                                if(!cid.isValid)continue;
                                if(!asset.database.TryGetAsset(cid,out VTTextureAsset textureAsset) &&
                                   !AssetDatabase.global.TryGetAsset(cid,out textureAsset))
                                    throw new InvalidOperationException($"Missing native VT texture {cid} on {asset.name}.");
                                // Only release decoded resources acquired by this composition.
                                // An already loaded native texture remains owned by its caller.
                                if(textureAsset.state!=LoadState.Full && textureAsset.state!=LoadState.Object)
                                    loadedVtTextures.Add(textureAsset);
                                cached.SetTexture(description.m_Stacks[stack].m_PropertyNames[layer],textureAsset.Load());
                            }
                        }
                        finally {handler?.Dispose();}
                    }
                    else cached=asset.Load(useVT:false);
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
                    program.SetTexture(kernel,property,m.GetTexture(property) ?? throw new InvalidOperationException($"Missing {property} on {asset.name}."));
                // Network worldspace albedo and native materials without an authored
                // control texture have no object color-variation regions to transfer.
                // Object sources retain their authored mask in the same atlas coordinates.
                // Omitting this binding invokes the native shader's WHITE default and
                // recolors the entire receiver through the mechanism's color palette.
                program.SetTexture(kernel,"_ControlMask",!authoredControls.Contains(asset)
                    ? Texture2D.blackTexture
                    : m.GetTexture("_ControlMask") ?? throw new InvalidOperationException($"Missing _ControlMask on {asset.name}."));
            }
            SurfaceAsset PaintSource(bool yellow)
            {
                var cid=Colossal.Hash128.Parse(yellow?"97291ab7f7938fc35cc670b15f2bca36":"792a855e9d0c5c34d2c41d48b062139d");
                if(!AssetDatabase.global.TryGetAsset(cid,out SurfaceAsset result))throw new InvalidOperationException("Native paint material unavailable.");
                return result;
            }
            // Native RequiredBatchesSystem allows 16 batches for the complete LOD group.
            // Pack appearance domains, not one native-sized image/material per plane.
            // Reserve one native repeating asphalt batch per LOD (5 structure + 9 road batches).
            const int maxPages=1;
            var pageSize=charts.Max(c=>Load(c.Source).GetTexture("_BaseColorMap").width);
            if(pageSize>SystemInfo.maxTextureSize)
                throw new InvalidOperationException("Native appearance page exceeds GPU texture dimensions.");
            var units=new Dictionary<(SurfaceAsset,string,bool),Chart>();
            var aliases=new Dictionary<Chart,Chart>();
            var desired=new Dictionary<Chart,Vector2>();
            float minimumChartScale=1;
            foreach(var c in charts.Where(c=>!c.Repeat))
            {
                var texture=Load(c.Source).GetTexture("_BaseColorMap");
                var span=c.Max-c.Min;
                float Density(Vector3 axis)
                {
                    var dx=Vector3.Dot(axis,(Vector3)c.Patch.U)*(c.Patch.Max.x-c.Patch.Min.x)*texture.width;
                    var dy=Vector3.Dot(axis,(Vector3)c.Patch.V)*(c.Patch.Max.y-c.Patch.Min.y)*texture.height;
                    return Mathf.Sqrt(dx*dx+dy*dy);
                }
                var du=Density(c.U);var dv=Density(c.V);
                // Keep comparable texel density along both physical axes. Independent
                // clamping produced 109 texels/m across a road but only 25 along it;
                // the resulting mip footprint blurred longitudinal paint even further.
                var density=du>0 && dv>0 ? Mathf.Min(du,dv) : Mathf.Max(du,dv);
                c.Density=new Vector2(density,density);
                var pixels=new Vector2(Mathf.Max(1,span.x*density),Mathf.Max(1,span.y*density));
                var capacity=pageSize-2f*padding;
                var fitted=pixels*Mathf.Min(1,capacity/Mathf.Max(pixels.x,pixels.y));
                minimumChartScale=Mathf.Min(minimumChartScale,
                    Mathf.Min(fitted.x/pixels.x,fitted.y/pixels.y));
                pixels=fitted;
                var key=(c.Source,MovableAppearanceUnit.Key(c.Origin+c.U*c.Min.x+c.V*c.Min.y,
                    c.U,c.V,span,c.Patch,paint),c.Repeat);
                if(units.TryGetValue(key,out var shared))aliases.Add(c,shared);
                else {units.Add(key,c);desired.Add(c,pixels);}
            }
            var pages=new List<List<Chart>>();
            float packingScale=1;
            var packingClock=System.Diagnostics.Stopwatch.StartNew();
            // At the final attempt every chart reaches its minimum 1x1 content size.
            // Full-page strips in both orientations cannot fit by shrinking only their
            // short axes, even when their combined pixel area is small enough.
            int lastAttempt=Mathf.CeilToInt(Mathf.Log(pageSize)/-Mathf.Log(.9f))+1;
            int packingAttempts=0;
            for(int attempt=0;attempt<=lastAttempt;attempt++)
            {
                packingAttempts++;
                packingScale=attempt==lastAttempt?0:Mathf.Pow(.9f,attempt);
                pages.Clear();
                foreach(var item in desired)
                {
                    // Scale both axes equally to preserve the appearance aspect ratio.
                    // The independently repeating native asphalt is not resampled here.
                    item.Key.W=Mathf.CeilToInt(Mathf.Max(1,item.Value.x*packingScale))+2*padding;
                    item.Key.H=Mathf.CeilToInt(Mathf.Max(1,item.Value.y*packingScale))+2*padding;
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
                        // A rejected attempt needs no further placements or allocations.
                        if(pages.Count==maxPages){pages.Add(new List<Chart>());break;}
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
                if(pages.Count<=maxPages)break;
            }
            if(pages.Count>maxPages)
                throw new InvalidOperationException($"Cannot pack {desired.Count} appearance charts into {maxPages} pages of {pageSize} pixels, even at minimum chart size.");
            Mod.Log.Info($"Moving-road atlas packing completed: attempts={packingAttempts}; elapsedMs={packingClock.ElapsedMilliseconds}; pages={pages.Count}; scale={packingScale:R}.");
            foreach(var alias in aliases)
            {
                var c=alias.Key;var shared=alias.Value;
                c.Page=shared.Page;c.X=shared.X;c.Y=shared.Y;c.W=shared.W;c.H=shared.H;
            }
            foreach(var c in charts.Where(c=>!c.Repeat))
            {
                var span=c.Max-c.Min;
                c.Width=c.Height=pageSize;
                // Native node receivers can collapse to a line on one chart axis. The
                // allocated one-pixel strip still uses the source sampling density there;
                // dividing by zero would turn its constant coordinate into 0 * Infinity.
                c.Density=new Vector2(span.x>0f?(c.W-2f*padding)/span.x:c.Density.x,
                    span.y>0f?(c.H-2f*padding)/span.y:c.Density.y);
            }
            // Extract one native atlas asphalt unit. Continuous receiver UVs repeat
            // this unit; no road triangle is cut and no whole-span paint overlay remains.
            var repeatPages=new Dictionary<(SurfaceAsset Source,Vector2 Min,Vector2 Max,bool Grating,MovableExistingRoadSurface.RepeatBand[]? Bands),int>();
            foreach(var c in charts.Where(c=>c.Repeat))
            {
                var key=(c.Source,c.Patch.Min,c.Patch.Max,c.Patch.Repeat,c.Patch.Bands);
                if(!repeatPages.TryGetValue(key,out var tilePage))
                {
                    var nativeTexture=Load(c.Source).GetTexture("_BaseColorMap");
                    var width=4*Mathf.CeilToInt((c.Patch.Max.x-c.Patch.Min.x)*nativeTexture.width/4f);
                    var height=4*Mathf.CeilToInt((c.Patch.Max.y-c.Patch.Min.y)*nativeTexture.height/4f);
                    var bands=c.Patch.Bands;
                    if(bands!=null)
                    {
                        float previous=0f,requiredWidth=0f,requiredHeight=0f;
                        foreach(var band in bands)
                        {
                            if(band.Start!=previous || band.End<=band.Start || band.End>1f ||
                                band.Max.x<=band.Min.x || band.Max.y<=band.Min.y)
                                throw new InvalidOperationException("A repeating surface requires contiguous, non-empty texture bands.");
                            requiredWidth=Mathf.Max(requiredWidth,(band.Max.x-band.Min.x)*nativeTexture.width/(band.End-band.Start));
                            requiredHeight=Mathf.Max(requiredHeight,(band.Max.y-band.Min.y)*nativeTexture.height);
                            previous=band.End;
                        }
                        if(previous!=1f)throw new InvalidOperationException("Repeating surface bands must cover the complete unit.");
                        width=4*Mathf.CeilToInt(requiredWidth/4f);
                        height=4*Mathf.CeilToInt(requiredHeight/4f);
                    }
                    tilePage=pages.Count;repeatPages.Add(key,tilePage);
                    var tile=new List<Chart>();
                    void AddBand(int x,int end,Vector2 min,Vector2 max)
                    {
                        if(end<=x)throw new InvalidOperationException("A repeating surface band has no output pixels.");
                        tile.Add(new Chart {
                            Source=c.Source,Repeat=true,Origin=Vector3.zero,
                            U=Vector3.right,V=Vector3.forward,Normal=Vector3.up,Min=Vector2.zero,Max=Vector2.one,
                            X=x,Y=0,W=end-x,H=height,Width=width,Height=height,Density=new Vector2(end-x,height),
                            Patch=new MovableExistingRoadSurface.Patch(min,max,
                                new Vector4(1,0,0,0),new Vector4(0,0,1,0),repeat:c.Patch.Repeat)});
                    }
                    if(bands==null)AddBand(0,width,c.Patch.Min,c.Patch.Max);
                    else foreach(var band in bands)
                        AddBand(Mathf.RoundToInt(band.Start*width),Mathf.RoundToInt(band.End*width),band.Min,band.Max);
                    pages.Add(tile);
                    Mod.Log.Info($"Native repeating unit: {width}x{height}; grating={c.Patch.Repeat}; source={c.Source.name}; no span atlas scaling.");
                }
                c.Page=tilePage;
            }
            foreach(var c in desired.Keys)
                Mod.Log.Info($"Moving-road chart density: source={c.Source.name}; span={c.Max-c.Min}; pixels={c.W-2*padding}x{c.H-2*padding}; texels/metre={c.Density}; page={c.Page}.");
            Mod.Log.Info($"Moving-road packed VT: domains={charts.Count}; unique={desired.Count}; pages={pages.Count}; pageSize={pageSize}; packingScale={packingScale:R}; minimum native-density scale={minimumChartScale*packingScale:R}; max LOD-group batches=16.");
            var pixelCount=pages.Sum(page=>page[0].Repeat?(long)page[0].Width*page[0].Height:(long)pageSize*pageSize);
            Mod.Log.Info($"Moving-road VT composition: panels={panels.Sum(p=>p.Count)}; shared charts={charts.Count}; pages={pages.Count}; pixels={pixelCount}; receiver topology unchanged; prototype asphalt repeats; paint uses native decals.");
            var composites=new List<SurfaceAsset>();
            for(int page=0;page<pages.Count;page++)
            {
                var pageClock=System.Diagnostics.Stopwatch.StartNew();
                long readbackMs=0,textureSaveMs=0;
                bool tiled=pages[page][0].Repeat;
                int width=tiled?pages[page][0].Width:pageSize;
                int height=tiled?pages[page][0].Height:pageSize;
                int border=tiled?0:padding;
                foreach(var c in pages[page]){c.Width=width;c.Height=height;}
                for(int i=0;i<targets.Length;i++)
                {
                    targets[i]=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                    { enableRandomWrite=true,useMipMap=false,autoGenerateMips=false,wrapMode=TextureWrapMode.Clamp };
                    targets[i].Create();
                    var previous=RenderTexture.active;
                    try {RenderTexture.active=targets[i];GL.Clear(false,true,i==1?new Color(.5f,.5f,1,1):Color.clear);}
                    finally {RenderTexture.active=previous;}
                }
                int surfaceKernel=program.FindKernel("SurfaceAtlas"),paintKernel=program.FindKernel("PaintAtlas");
                foreach(var kernel in new[]{surfaceKernel,paintKernel})
                {
                    program.SetTexture(kernel,"AtlasColor",targets[0]);
                    program.SetTexture(kernel,"AtlasNormal",targets[1]);
                    program.SetTexture(kernel,"AtlasMask",targets[2]);
                    program.SetTexture(kernel,"AtlasControl",targets[3]);
                }
                foreach(var c in pages[page])
                {
                    var origin=c.Origin+c.U*c.Min.x+c.V*c.Min.y;
                    program.SetVector("ChartOrigin",origin);
                    program.SetVector("ChartU",c.U);program.SetVector("ChartV",c.V);
                    program.SetVector("AtlasPixels",new Vector4(c.Density.x,border,c.Density.y,0));
                    program.SetVector("AtlasRect",new Vector4(c.X,c.Y,c.W,c.H));
                    program.SetVector("PatchU",c.Patch.U);program.SetVector("PatchV",c.Patch.V);
                    program.SetVector("PatchArea",new Vector4(c.Patch.Min.x,c.Patch.Min.y,c.Patch.Max.x-c.Patch.Min.x,c.Patch.Max.y-c.Patch.Min.y));
                    // The last page extracts the prototype asphalt unit. Other pages
                    // contain only opaque/grating appearance, never painted receivers.
                    {
                        Bind(surfaceKernel,c.Source);
                        program.Dispatch(surfaceKernel,(c.W+7)/8,(c.H+7)/8,1);
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
                        program.SetVector("PaintRect",new Vector4(x0,y0,x1-x0,y1-y0));
                        program.SetVector("PaintCenter",stroke.Center);program.SetVector("PaintRight",stroke.Right);
                        program.SetVector("PaintUp",stroke.Up);program.SetVector("PaintForward",stroke.Forward);program.SetVector("PaintHalf",stroke.Half);
                        program.SetVector("PaintColor",stroke.Yellow?new Vector4(800f/4096,3000f/4096,0,0):new Vector4(3500f/4096,820f/4096,0,0));
                        Bind(paintKernel,PaintSource(stroke.Yellow));
                        program.Dispatch(paintKernel,(x1-x0+7)/8,(y1-y0+7)/8,1);
                    }
                }
                var textures=new Dictionary<string,TextureAsset>();
                for(int channel=0;channel<targets.Length;channel++)
                {
                    var texture=new Texture2D(width,height,TextureFormat.BC7,true,channel!=0)
                    {
                        // Match native VT registration and cold-load address mode.
                        wrapMode=tiled?TextureWrapMode.Repeat:TextureWrapMode.Clamp,
                        anisoLevel=8
                    };
                    var mipTargets=new List<RenderTexture>{targets[channel]};
                    var compressed=new List<ComputeBuffer>();
                    try
                    {
                        var mipKernel=program.FindKernel("AtlasMip");
                        program.SetInt("MipChannel",channel);
                        var requests=new AsyncGPUReadbackRequest[texture.mipmapCount];
                        for(int mip=1;mip<texture.mipmapCount;mip++)
                        {
                            int mipWidth=Math.Max(1,width>>mip),mipHeight=Math.Max(1,height>>mip);
                            var target=new RenderTexture(mipWidth,mipHeight,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                                {enableRandomWrite=true};
                            target.Create();mipTargets.Add(target);
                            program.SetTexture(mipKernel,"MipSource",mipTargets[mip-1]);
                            program.SetTexture(mipKernel,"MipTarget",target);program.SetInts("MipExtent",mipWidth,mipHeight);
                            program.Dispatch(mipKernel,(mipWidth+7)/8,(mipHeight+7)/8,1);
                        }
                        var compressionKernel=program.FindKernel("CompressAtlas");
                        for(int mip=0;mip<texture.mipmapCount;mip++)
                        {
                            int blocksX=(Math.Max(1,width>>mip)+3)/4,blocksY=(Math.Max(1,height>>mip)+3)/4;
                            var buffer=new ComputeBuffer(blocksX*blocksY,16);compressed.Add(buffer);
                            program.SetTexture(compressionKernel,"MipSource",mipTargets[mip]);
                            program.SetBuffer(compressionKernel,"CompressedAtlas",buffer);
                            program.Dispatch(compressionKernel,(blocksX+7)/8,(blocksY+7)/8,1);
                            requests[mip]=AsyncGPUReadback.Request(buffer);
                        }
                        // GPU performs every pixel operation; CPU only transfers serialized bytes.
                        for(int mip=0;mip<texture.mipmapCount;mip++)
                        {
                            var readbackClock=System.Diagnostics.Stopwatch.StartNew();
                            var request=requests[mip];request.WaitForCompletion();
                            if(request.hasError)throw new InvalidOperationException("Moving-road GPU atlas readback failed.");
                            texture.SetPixelData(request.GetData<byte>(),mip);
                            readbackMs+=readbackClock.ElapsedMilliseconds;
                        }
                        // Serialization reads the CPU BC7 mip bytes. Uploading them
                        // back to the GPU here is unused and stalls large pages.
                        var saveClock=System.Diagnostics.Stopwatch.StartNew();
                        textures.Add(new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask"}[channel],owner.SaveTexture(texture,channel,page));
                        textureSaveMs+=saveClock.ElapsedMilliseconds;
                    }
                    finally
                    {
                        for(int mip=1;mip<mipTargets.Count;mip++){mipTargets[mip].Release();UnityEngine.Object.Destroy(mipTargets[mip]);}
                        foreach(var buffer in compressed)buffer.Dispose();
                        UnityEngine.Object.Destroy(texture);
                    }
                }
                composites.Add(pages[page][0].Repeat
                    ? (pages[page][0].Patch.Repeat ? owner.SaveRepeatingGrating(textures,page) : owner.SaveRepeatingAsphalt(textures))
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
                    if(c==null){var original=inputs[level].Materials[t.Material];material=slots.IndexOf(original);if(material<0){material=slots.Count;slots.Add(original);}}
                    Vertex Remap(Vertex vertex)
                    {
                        if(c==null)return vertex;
                        var d=vertex.Position-c.Origin;
                        var uv=new Vector4((c.X+padding+(Vector3.Dot(d,c.U)-c.Min.x)*c.Density.x)/c.Width,
                            (c.Y+padding+(Vector3.Dot(d,c.V)-c.Min.y)*c.Density.y)/c.Height,0,0);
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
                            c.Page,t.Occurrence,t.SourceTriangle));
                        continue;
                    }
                    output.Add(new Triangle(Remap(v[0]),Remap(v[1]),Remap(v[2]),material,t.Occurrence,t.SourceTriangle));
                }
                result[level]=output.ToArray();materials[level]=slots.ToArray();
                Mod.Log.Info($"Moving-road atlas LOD{level+1}: input={inputs[level].Faces.Length}; output={output.Count}; skinned paint is separate; coalescing=0; repetition cuts=0; prototype asphalt UV gradients retained.");
            }
            Mod.Log.Info($"Moving-road GPU appearance units: charts={charts.Count}; packed pages={pages.Count}; native-sized atlas pages; textures={pages.Count*4} shared across LODs; ms={clock.ElapsedMilliseconds}.");
            return result;
        }
        finally
        {
            foreach(var item in sources)
            {
                if(!ownedMaterials.Contains(item.Value))item.Key.Unload();
            }
            foreach(var material in ownedMaterials)UnityEngine.Object.Destroy(material);
            foreach(var texture in ownedTextures)UnityEngine.Object.Destroy(texture);
            foreach(var texture in loadedVtTextures)texture.Unload();
            foreach(var target in targets)if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}
            UnityEngine.Object.Destroy(program);
        }
    }
}
