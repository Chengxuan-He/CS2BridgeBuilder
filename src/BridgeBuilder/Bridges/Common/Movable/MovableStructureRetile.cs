using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Material/UV duplication at measured island seams only. Physical triangles,
// positions, procedural bones and all unrelated raw vertex channels are retained.
internal static class MovableStructureRetile
{
    internal static void Append(string name, string renderName, int meshIndex,
        MovableStructureModel.Plan plan, ModelImporter.Model original, SurfaceAsset surface,
        MovableStructureTiles tiles, List<ModelImporter.Model> owned,
        List<ModelImporter.Model> models, List<SurfaceAsset> surfaces)
    {
        // The native graph retains NetPiece tiling metadata and shader 581's
        // signed longitudinal coordinate/worldspace evaluation. Build's raw
        // channels must reach that shader without object conversion or UV retile.
        if(tiles.PreserveNativeNetwork(renderName,meshIndex,surface))
        { models.Add(original); surfaces.Add(surface); return; }
        var recipe=MovableUvDomains.For(renderName,meshIndex);
        if(recipe==null || recipe.Items.Length==0)
        { models.Add(original); surfaces.Add(tiles.NativeSurface(surface)); return; }
        if(original.subMeshCount!=1 || plan.SourceIndices.Length!=1)
            throw new InvalidOperationException("Measured UV domains require their original single-surface mesh.");
        var prepared=plan.PreparedDomains.ToDictionary(d=>d.Domain);
        if(prepared.Keys.Any(d=>d>=recipe.Items.Length))
            throw new InvalidOperationException("Prepared structural mapping requires a measured domain.");
        var rawUv=original.FirstOrDefault(a=>a.attribute==VertexAttribute.TexCoord0);
        if(rawUv==null || rawUv.dimension<2 || rawUv.format is not (VertexAttributeFormat.Float32 or VertexAttributeFormat.Float16))
            throw new InvalidOperationException("Measured UV domains require floating-point native UV0.");
        var uvBytes=rawUv.data.ToArray();
        float Uv(int index,int axis) => rawUv.format==VertexAttributeFormat.Float32
            ? BitConverter.ToSingle(uvBytes,index*rawUv.elementSize+axis*4)
            : (float)new half {value=BitConverter.ToUInt16(uvBytes,index*rawUv.elementSize+axis*2)};
        Vector2 Uv2(int index)
        {
            if(index<plan.SourcePositions.Length || plan.UvCorrections.Any(c=>c.Key==index))return new(Uv(index,0),Uv(index,1));
            // Native Float16 serialization of a constructed UV can lose the
            // sub-texel gradient of a small fragment. Evaluate its recorded source
            // interpolation before any output packing instead.
            var v=plan.Construction(index-plan.SourcePositions.Length);
            var a=Uv2(v.A);
            return a+(Uv2(v.B)-a)*v.WeightB+(Uv2(v.C)-a)*v.WeightC;
        }
        var frames=new Dictionary<int,MovableUvFrame>();
        var faceDomains=Enumerable.Repeat(-1,plan.RemoveTriangles[0].Length).ToArray();
        var fragments=plan.Fragments.ToDictionary(f=>f.Triangle);
        var fragmentDomains=new Dictionary<int,int[]>();
        var fragmentUnits=new Dictionary<int,MovableStructurePacking.Unit>();
        var fragmentUvs=new Dictionary<(int Domain,int Vertex),Vector2>();
        var mappings=recipe.Items.Select(d=>(d.A,d.B,d.C,d.Bounds,d.Triangle)).ToList();
        var retainedAxes=recipe.Items.Select((_,di)=>tiles.RetainedAxes(renderName,meshIndex,di)).ToList();
        Vector3 SourcePosition(int index)
        {
            if(index<plan.SourcePositions.Length)return plan.SourcePositions[index];
            var v=plan.Construction(index-plan.SourcePositions.Length);
            var a=plan.SourcePositions[v.A];
            return a+(plan.SourcePositions[v.B]-a)*v.WeightB+(plan.SourcePositions[v.C]-a)*v.WeightC;
        }
        int[] Face(int index) => fragments.TryGetValue(index,out var fragment) ? fragment.Indices :
            plan.RemoveTriangles[0][index] ? Array.Empty<int>() : new[]{plan.SourceIndices[0][index*3],plan.SourceIndices[0][index*3+1],plan.SourceIndices[0][index*3+2]};
        bool AddFrame(int di,int[] vertices)
        {
            if(vertices.Length==0)return false;
            var d=mappings[di];
            var frame=new MovableUvFrame(SourcePosition(d.A),SourcePosition(d.B),SourcePosition(d.C),
                Uv2(d.A),Uv2(d.B),Uv2(d.C),plan.Positions[d.A],plan.Positions[d.B],plan.Positions[d.C]);
            // Only numerical evaluation of recorded domains; rigid parts retain their
            // exact original Surface/VT references and their original attribute bytes.
            if(!frame.HasStretch && !vertices.Any(v=>(frame.Map(SourcePosition(v),plan.Positions[v],Uv2(v))-Uv2(v)).sqrMagnitude>1e-12f))return false;
            frames.Add(di,frame);
            return true;
        }
        for(int di=0;di<recipe.Items.Length;di++)
        {
            var d=recipe.Items[di];
            if(prepared.TryGetValue(di,out var finished))
            {
                foreach(int face in d.Faces)
                {
                    var vertices=Face(face);
                    foreach(int vertex in vertices)
                        if(!finished.Uvs.ContainsKey(vertex))
                            throw new InvalidOperationException("Prepared structural mapping lacks a retained vertex UV.");
                    if(faceDomains[face]!=-1)throw new InvalidOperationException("Overlapping prepared structural domains.");
                    faceDomains[face]=di;
                    if(fragments.ContainsKey(face))fragmentDomains.Add(face,Enumerable.Repeat(di,vertices.Length/3).ToArray());
                }
                continue;
            }
            // Verified finite, non-tiling prototype mappings stretch with their
            // geometry. Build already retained corrected source UVs and the
            // original interpolation for constructed vertices. Keep those bytes
            // and the native Surface, never density-map them out of their atlas.
            // Decide before constructing frames/fragment samplers: a failed
            // density sampler is not evidence for this authoring classification.
            if(tiles.PreserveSourceUv(renderName,meshIndex,di))continue;
            var members=new HashSet<int>();
            foreach(var face in d.Faces)
            {
                if(fragments.TryGetValue(face,out var fragment))
                {
                    var ids=fragment.Indices;
                    var domains=new int[ids.Length/3];
                    for(int at=0;at<ids.Length;at+=3)
                    {
                        // Some recorded fragments retain repeated-index triangles.
                        // They have no physical area at any width and no UV metric;
                        // preserve them verbatim instead of constructing a frame.
                        if(ids[at]==ids[at+1] || ids[at]==ids[at+2] || ids[at+1]==ids[at+2])
                        { domains[at/3]=-1;continue; }
                        int mapping=mappings.Count;
                        int pivot=MovableUvFrame.TrianglePivot(SourcePosition(ids[at]),SourcePosition(ids[at+1]),SourcePosition(ids[at+2]));
                        mappings.Add((ids[at+pivot],ids[at+(pivot+1)%3],ids[at+(pivot+2)%3],default,true));
                        retainedAxes.Add(retainedAxes[di]);
                        domains[at/3]=AddFrame(mapping,new[]{ids[at],ids[at+1],ids[at+2]})?mapping:-1;
                        if(domains[at/3]<0)continue;
                        var unit=new MovableStructurePacking.Unit{Id=mapping,Triangle=d.Triangle,Bounds=d.Bounds,
                            Charts=tiles.Charts(renderName,meshIndex,di),
                            A=d.Triangle?Uv2(d.A):default,B=d.Triangle?Uv2(d.B):default,C=d.Triangle?Uv2(d.C):default};
                        var corners=new[]{ids[at],ids[at+1],ids[at+2]};
                        var projection=MovableParentUv.Map(corners.Select(SourcePosition).ToArray(),corners.Select(Uv2).ToArray(),
                            corners.Select(i=>plan.Positions[i]).ToArray(),
                            new[]{SourcePosition(d.A),SourcePosition(d.B),SourcePosition(d.C)},
                            new[]{Uv2(d.A),Uv2(d.B),Uv2(d.C)},d.Triangle,d.Bounds,retainedAxes[di]);
                        var points=projection.Select(unit);
                        if(retainedAxes[di]!=0)
                        {
                            unit.RetainedAxes=retainedAxes[di];unit.ExactSourceUvs=new();
                            for(int corner=0;corner<3;corner++)
                            {
                                var sample=unit.SourceUv(projection.Exact[corner][0],projection.Exact[corner][1]);
                                var authored=Uv2(corners[corner]);
                                if((unit.RetainedAxes&1)!=0)sample.x=authored.x;
                                if((unit.RetainedAxes&2)!=0)sample.y=authored.y;
                                unit.ExactSourceUvs.Add(points[corner],sample);
                            }
                        }
                        fragmentUnits.Add(mapping,unit);
                        for(int corner=0;corner<3;corner++)fragmentUvs.Add((mapping,corners[corner]),points[corner]);
                    }
                    fragmentDomains.Add(face,domains);
                }
                else foreach(var vertex in Face(face))members.Add(vertex);
            }
            if(!AddFrame(di,members.ToArray()))continue;
            foreach(var face in d.Faces)if(!fragments.ContainsKey(face))
            {
                if(faceDomains[face]!=-1)throw new InvalidOperationException("Overlapping measured structural UV domains.");
                faceDomains[face]=di;
            }
        }
        if(frames.Count==0 && prepared.Count==0){models.Add(original);surfaces.Add(tiles.NativeSurface(surface));return;}
        Vector2 SourceSample(int domain,int vertex)
        {
            var value=frames[domain].Map(SourcePosition(vertex),plan.Positions[vertex],Uv2(vertex));
            if((retainedAxes[domain]&1)!=0)value.x=Uv2(vertex).x;
            if((retainedAxes[domain]&2)!=0)value.y=Uv2(vertex).y;
            return value;
        }
        Vector2 UnitUv(int domain,int vertex)
        {
            if(fragmentUvs.TryGetValue((domain,vertex),out var fragmentUv))return fragmentUv;
            var d=mappings[domain];
            if(d.Triangle && retainedAxes[domain]==0)return frames[domain].TriangleUnit(plan.Positions[vertex]);
            var value=SourceSample(domain,vertex);
            if(d.Triangle)
            {
                var a=Uv2(d.A);var u=Uv2(d.B)-a;var v=Uv2(d.C)-a;var delta=value-a;
                double det=(double)u.x*v.y-(double)u.y*v.x;
                if(det==0)throw new InvalidOperationException("Independent sampling axes require a measured nonsingular UV mapping.");
                return new Vector2((float)(((double)delta.x*v.y-(double)delta.y*v.x)/det),
                    (float)(((double)u.x*delta.y-(double)u.y*delta.x)/det));
            }
            return new Vector2((value.x-d.Bounds.x)/(d.Bounds.z-d.Bounds.x),(value.y-d.Bounds.y)/(d.Bounds.w-d.Bounds.y));
        }
        var units=new Dictionary<int,MovableStructurePacking.Unit>();
        var mixedFaces=new Dictionary<int,int[]>();
        for(int face=0;face<faceDomains.Length;face++)
        {
            var vertices=Face(face);
            for(int at=0;at<vertices.Length;at+=3)
            {
                int domain=fragmentDomains.TryGetValue(face,out var parts)?parts[at/3]:faceDomains[face];
                if(domain<0 || prepared.ContainsKey(domain))continue;
                if(retainedAxes[domain]!=0 && !mixedFaces.ContainsKey(domain))
                    mixedFaces.Add(domain,new[]{vertices[at],vertices[at+1],vertices[at+2]});
                if(!units.TryGetValue(domain,out var unit))
                {
                    var d=mappings[domain];var q=UnitUv(domain,vertices[at]);
                    unit=fragmentUnits.TryGetValue(domain,out var parentUnit)?parentUnit:
                        new MovableStructurePacking.Unit{Id=domain,Triangle=d.Triangle,Bounds=d.Bounds,
                        Charts=tiles.Charts(renderName,meshIndex,domain),
                        A=d.Triangle?Uv2(d.A):default,B=d.Triangle?Uv2(d.B):default,C=d.Triangle?Uv2(d.C):default};
                    unit.Min=q;unit.Max=q;units.Add(domain,unit);
                    if(retainedAxes[domain]!=0 && !fragmentUnits.ContainsKey(domain))
                    {unit.RetainedAxes=retainedAxes[domain];unit.ExactSourceUvs=new();}
                }
                for(int corner=0;corner<3;corner++)
                {
                    var q=UnitUv(domain,vertices[at+corner]);
                    if(unit.RetainedAxes!=0 && !fragmentUnits.ContainsKey(domain))
                    {
                        var sample=SourceSample(domain,vertices[at+corner]);
                        if(unit.ExactSourceUvs!.TryGetValue(q,out var previous) && previous!=sample)
                            throw new InvalidOperationException("Independent-axis source samples collide after Float32 parameterization.");
                        unit.ExactSourceUvs[q]=sample;
                    }
                    unit.Points.Add(q);unit.Min=Vector2.Min(unit.Min,q);unit.Max=Vector2.Max(unit.Max,q);
                }
            }
        }
        var slots=tiles.Build(surface,units.Values);
        Vector2 OutputUv(int domain,int vertex)
        {
            if(prepared.TryGetValue(domain,out var finished))return finished.Uvs[vertex];
            var result=slots[domain].Map(UnitUv(domain,vertex));
            // Original-atlas output keeps finite components exactly, avoiding an
            // unnecessary UV inverse/forward round trip. Prepared regions already
            // transform the combined source mapping through their finite guard.
            if(slots[domain].Original!=null)
            {
                if((retainedAxes[domain]&1)!=0)result.x=Uv2(vertex).x;
                if((retainedAxes[domain]&2)!=0)result.y=Uv2(vertex).y;
            }
            return result;
        }
        var groups=new Dictionary<SurfaceAsset,List<(int Vertex,int Domain)>>();
        int emitted=0;
        for(int face=0;face<faceDomains.Length;face++)
        {
            var vertices=Face(face);if(vertices.Length==0)continue;
            for(int at=0;at<vertices.Length;at+=3)
            {
                int domain=fragmentDomains.TryGetValue(face,out var parts)?parts[at/3]:faceDomains[face];
                SurfaceAsset material=tiles.NativeSurface(surface);
                if(domain>=0)material=prepared.TryGetValue(domain,out var finished)?finished.Surface:slots[domain].Surface;
                if(!groups.TryGetValue(material,out var entries))groups.Add(material,entries=new());
                for(int corner=0;corner<3;corner++){entries.Add((vertices[at+corner],domain));emitted++;}
            }
        }
        if(emitted!=original.indexCount)throw new InvalidOperationException("Structural UV retile changed the physical triangle count.");
        var originalNormals=original.FirstOrDefault(a=>a.attribute==VertexAttribute.Normal);
        var originalTangents=original.FirstOrDefault(a=>a.attribute==VertexAttribute.Tangent);
        using var normals=new NativeArray<MeshNormal>(originalNormals==null?0:original.vertexCount,Allocator.Temp);
        using var tangents=new NativeArray<MeshTangent>(originalTangents==null?0:original.vertexCount,Allocator.Temp);
        if(originalNormals!=null)MeshNormal.Unpack(new NativeSlice<byte>(originalNormals.data),normals,original.vertexCount,originalNormals.format,originalNormals.dimension);
        if(originalTangents!=null)MeshTangent.Unpack(new NativeSlice<byte>(originalTangents.data),tangents,original.vertexCount,originalTangents.format,originalTangents.dimension);
        foreach(var group in groups)
        {
            var entries=group.Value.Distinct().ToArray();
            var indexMap=entries.Select((e,i)=>(e,i)).ToDictionary(p=>p.e,p=>p.i);
            var indices=group.Value.Select(e=>indexMap[e]).ToArray();
            var attributes=new List<ModelImporter.Model.VertexData>();
            NativeArray<int> nativeIndices=default;
            bool success=false;
            try
            {
                bool remapped=entries.Any(e=>e.Domain>=0);
                foreach(var attribute in original)
                {
                    bool uv=remapped && attribute.attribute==VertexAttribute.TexCoord0;
                    bool normal=remapped && attribute.attribute==VertexAttribute.Normal;
                    bool tangent=remapped && attribute.attribute==VertexAttribute.Tangent;
                    var format=uv?VertexAttributeFormat.Float32:attribute.format;
                    int size=uv?attribute.dimension*4:attribute.elementSize;
                    var bytes=new byte[entries.Length*size];
                    for(int i=0;i<entries.Length;i++)
                    {
                        var entry=entries[i];
                        if(uv)
                        {
                            var value=Uv2(entry.Vertex);
                            if(entry.Domain>=0)
                                value=OutputUv(entry.Domain,entry.Vertex);
                            for(int axis=0;axis<attribute.dimension;axis++)
                                Buffer.BlockCopy(BitConverter.GetBytes(axis<2?value[axis]:Uv(entry.Vertex,axis)),0,bytes,i*size+axis*4,4);
                        }
                        else
                        {
                            NativeArray<byte>.Copy(attribute.data,entry.Vertex*size,bytes,i*size,size);
                            if(entry.Domain<0 || prepared.ContainsKey(entry.Domain) || (!normal && !tangent))continue;
                            var frame=frames[entry.Domain];
                            if(normal && format==VertexAttributeFormat.SNorm16 && attribute.dimension==2)
                            {
                                var value=frame.Rotate((Vector3)normals[entry.Vertex].m_Normal).normalized;
                                Buffer.BlockCopy(BitConverter.GetBytes(MathUtils.NormalToOctahedral((float3)value)),0,bytes,i*size,4);
                            }
                            else if(tangent && format==VertexAttributeFormat.Float32 && attribute.dimension==1)
                            {
                                var t=tangents[entry.Vertex].m_Tangent;
                                var value=frame.Rotate((Vector3)t.xyz).normalized;
                                float handedness=t.w;
                                if(mixedFaces.TryGetValue(entry.Domain,out var face))
                                {
                                    if(originalNormals==null)throw new InvalidOperationException("Independent UV axes require native normals for their tangent basis.");
                                    var normalValue=frame.Rotate((Vector3)normals[entry.Vertex].m_Normal).normalized;
                                    var adjusted=MovableUvFrame.MixedTangent(frame,value,normalValue,handedness,
                                        face.Select(SourcePosition).ToArray(),face.Select(Uv2).ToArray(),
                                        face.Select(v=>plan.Positions[v]).ToArray(),face.Select(v=>OutputUv(entry.Domain,v)).ToArray());
                                    value=new Vector3(adjusted.x,adjusted.y,adjusted.z);handedness=adjusted.w;
                                }
                                Buffer.BlockCopy(BitConverter.GetBytes(MathUtils.TangentToOctahedral(new float4((float3)value,handedness))),0,bytes,i*size,4);
                            }
                            else throw new InvalidOperationException("Unsupported native direction encoding for structural UV retile.");
                        }
                    }
                    attributes.Add(new ModelImporter.Model.VertexData(attribute.attribute,format,attribute.dimension,
                        new NativeArray<byte>(bytes,Allocator.Persistent),attribute.isPacked));
                }
                var bounds=new Bounds(plan.Positions[entries[0].Vertex],Vector3.zero);
                foreach(var entry in entries)bounds.Encapsulate(plan.Positions[entry.Vertex]);
                nativeIndices=new NativeArray<int>(indices,Allocator.Persistent);
                var model=new ModelImporter.Model(name+" UV "+models.Count,Matrix4x4.identity,entries.Length,nativeIndices,
                    attributes.ToArray(),new[]{new SubMeshDescriptor(0,indices.Length,MeshTopology.Triangles)
                    {bounds=bounds,firstVertex=0,vertexCount=entries.Length}},-1,Array.Empty<ModelImporter.Model.BoneInfo>());
                owned.Add(model);models.Add(model);surfaces.Add(group.Key);success=true;
            }
            finally
            {
                if(!success){if(nativeIndices.IsCreated)nativeIndices.Dispose();foreach(var attribute in attributes)attribute.Dispose();}
            }
        }
        Mod.Log.Info($"Structural native-density retile: {renderName}; mesh={meshIndex}; domains={frames.Count}; material units={groups.Count}; physical indices={emitted}; no period cuts.");
    }
}
