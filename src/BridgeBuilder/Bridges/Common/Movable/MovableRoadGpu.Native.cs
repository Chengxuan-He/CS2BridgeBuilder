using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRoadGpu
{
    internal sealed class Prepared
    {
        internal byte[] Bytes = Array.Empty<byte>();
        internal int[] Slots = Array.Empty<int>(), Counts = Array.Empty<int>(), WordOffsets = Array.Empty<int>(), UvDimensions = Array.Empty<int>();
        internal Bounds[] Bounds = Array.Empty<Bounds>();
        internal int TriangleCount;
        internal Vector3 Minimum, Maximum;

        internal ModelImporter.Model[] Models(string name,List<ModelImporter.Model> owned)
            =>GenerationWork.Synchronous(()=>ModelsAsync(name,owned));
        internal async System.Threading.Tasks.Task<ModelImporter.Model[]> ModelsAsync(string name, List<ModelImporter.Model> owned)
        {
            var output = new List<ModelImporter.Model>();
            for(int g=0;g<Slots.Length;g++)
            {
                await GenerationWork.Checkpoint("native attribute copy model "+g);
                int vertices=checked(Counts[g]*3), offset=WordOffsets[g]*4;
                var attributes=new List<ModelImporter.Model.VertexData>();
                NativeArray<int> indices=default;
                try
                {
                    void Channel(VertexAttribute attribute,VertexAttributeFormat format,int dimension,int byteCount)
                    {
                        var data=new NativeArray<byte>(byteCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                        try { NativeArray<byte>.Copy(Bytes,offset,data,0,byteCount); }
                        catch { data.Dispose(); throw; }
                        offset+=byteCount;
                        attributes.Add(new ModelImporter.Model.VertexData(attribute,format,dimension,data,false));
                    }
                    Channel(VertexAttribute.Position,VertexAttributeFormat.Float32,3,vertices*12);
                    Channel(VertexAttribute.Normal,VertexAttributeFormat.SNorm16,2,vertices*4);
                    Channel(VertexAttribute.Tangent,VertexAttributeFormat.Float32,1,vertices*4);
                    Channel(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,UvDimensions[g],vertices*UvDimensions[g]*4);
                    Channel(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4,vertices*4);
                    Channel(VertexAttribute.BlendIndices,VertexAttributeFormat.UInt32,1,vertices*4);
                    indices=new NativeArray<int>(vertices,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                    // GPU generated the ordered local identity indices immediately after attributes.
                    var values=new int[vertices];Buffer.BlockCopy(Bytes,offset,values,0,vertices*4);indices.CopyFrom(values);
                    var descriptor=new SubMeshDescriptor(0,vertices,MeshTopology.Triangles)
                    {bounds=Bounds[g],firstVertex=0,vertexCount=vertices,baseVertex=0};
                    var model=new ModelImporter.Model(name+" Material "+Slots[g],Matrix4x4.identity,vertices,indices,
                        attributes.ToArray(),new[]{descriptor},-1,Array.Empty<ModelImporter.Model.BoneInfo>());
                    owned.Add(model);output.Add(model);
                }
                catch
                {
                    if(indices.IsCreated)indices.Dispose();foreach(var a in attributes)a.Dispose();throw;
                }
            }
            return output.ToArray();
        }
    }

    internal static bool TryBuildNative(IReadOnlyList<MovableRoadGeometry.Triangle> source,
        MovableRoadGeometry.Partition partition,int bones,SurfaceAsset[] surfaces,
        out Prepared? prepared,out MovableRoadGeometry.BoundTriangle[] bound,out string? error,
        bool capture=false,bool allowEmpty=false,IReadOnlyList<MovableRoadGeometry.Partition>? occurrencePartitions=null,
        IReadOnlyDictionary<(int Occurrence,int SourceTriangle),MovableRoadGeometry.Partition>? trianglePartitions=null,
        Pose? pose=null,MovableMaterialCompatibility.Mapping[]? mappings=null)
    {
        var result=GenerationWork.Synchronous(()=>BuildNativeAsync(source,partition,bones,surfaces,capture,allowEmpty,occurrencePartitions,trianglePartitions,pose,mappings));
        prepared=result.Prepared;bound=result.Bound;error=result.Error;return result.Success;
    }
    internal sealed class NativeResult
    {
        internal bool Success;internal Prepared? Prepared;internal MovableRoadGeometry.BoundTriangle[] Bound=Array.Empty<MovableRoadGeometry.BoundTriangle>();internal string? Error;
    }
    internal static async System.Threading.Tasks.Task<NativeResult> BuildNativeAsync(IReadOnlyList<MovableRoadGeometry.Triangle> source,
        MovableRoadGeometry.Partition partition,int bones,SurfaceAsset[] surfaces,bool capture=false,bool allowEmpty=false,
        IReadOnlyList<MovableRoadGeometry.Partition>? occurrencePartitions=null,
        IReadOnlyDictionary<(int Occurrence,int SourceTriangle),MovableRoadGeometry.Partition>? trianglePartitions=null,
        Pose? pose=null,MovableMaterialCompatibility.Mapping[]? mappings=null)
    {
        var result=new NativeResult();Prepared? prepared=null;var bound=Array.Empty<MovableRoadGeometry.BoundTriangle>();string? error=null;
        try
        {
        long start=Stopwatch.GetTimestamp();
        try
        {
            if(source.Count==0){prepared=new Prepared();result.Success=true;return result;}
            var execution=new Execution();
            await ExecuteAsync(source,partition,bones,surfaces.Length,occurrencePartitions,trianglePartitions,execution,surfaces,capture,pose,mappings);
            bound=execution.Bound;prepared=execution.Prepared;
            prepared ??= new Prepared();
            Mod.Log.Info($"Moving road GPU native streams: input={source.Count}; output={prepared.TriangleCount}; capture={capture}; doubleClip=true; totalMs={(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency:F3}.");
            result.Success=true;return result;
        }
        catch(OperationCanceledException){throw;}
        catch(Exception exception)
        {
            prepared=null;bound=Array.Empty<MovableRoadGeometry.BoundTriangle>();
            Mod.Log.Warn($"Moving road GPU native streams: complete CPU fallback; reason={exception.GetType().Name}: {exception.Message}");
            try
            {
                if(pose.HasValue)source=MovableRoadGeometry.InFrame(source.Select(t=>{
                    var m=mappings![t.Material];return !m.Planar?t:new MovableRoadGeometry.Triangle(m.Apply(t.A),m.Apply(t.B),m.Apply(t.C),t.Material,t.Occurrence,t.SourceTriangle);
                }).ToArray(),pose.Value);
                result.Success=MovableRoadGeometry.TryBind(source,partition,bones,surfaces.Length,out bound,out error,allowEmpty,occurrencePartitions,trianglePartitions);return result;
            }
            catch(Exception fallbackError){error="Road construction fallback failed: "+fallbackError.Message;return result;}
        }
        }
        finally{result.Prepared=prepared;result.Bound=bound;result.Error=error;}
    }

    private static async System.Threading.Tasks.Task<Prepared> PrepareNativeAsync(MovableRoadGeometry.Triangle[] source,uint[] counts,uint[] offsets,int total,
        int bones,SurfaceAsset[] surfaces,ComputeBuffer bound,ComputeShader shader,GenerationGpuLease lease)
    {
        long start=Stopwatch.GetTimestamp();
        var owned=lease.Buffers;
        await GenerationWork.Next("GPU material stream planning");
        var canonical=MovableMaterialSlots.Canonical(surfaces,Enumerable.Range(0,source.Length).Where(i=>counts[i]!=0).Select(i=>source[i].Material));
        var slots=canonical.Where(x=>x>=0).Distinct().OrderBy(x=>x).ToArray();
        int materialSlots = surfaces.Length;
        var groupForSlot=new int[materialSlots];var groupCounts=new int[slots.Length];
        var scatter=new uint[total*2];var cursor=new uint[slots.Length];var summary=new uint[slots.Length*8];
        await GenerationWork.Background("GPU stable grouping arrays", () =>
        {
        for(int g=0;g<slots.Length;g++)for(int s=0;s<canonical.Length;s++)if(canonical[s]==slots[g])groupForSlot[s]=g;
        for(int i=0;i<source.Length;i++)groupCounts[groupForSlot[source[i].Material]]=checked(groupCounts[groupForSlot[source[i].Material]]+(int)counts[i]);
        for(int i=0;i<source.Length;i++)
        {
            int g=groupForSlot[source[i].Material];
            for(uint j=0;j<counts[i];j++){int d=checked((int)(offsets[i]+j)*2);scatter[d]=cursor[g]++;scatter[d+1]=(uint)g;}
        }
        for(int g=0;g<slots.Length;g++)for(int k=0;k<3;k++){summary[g*8+k]=0xff800000;summary[g*8+3+k]=0x007fffff;}
        return true;
        });
        ComputeBuffer New(int n,int stride,ComputeBufferType type=ComputeBufferType.Default){var b=new ComputeBuffer(Math.Max(1,n),stride,type);owned.Add(b);return b;}
        var map=New(total,8);map.SetData(scatter);var stats=New(summary.Length,4);stats.SetData(summary);
        shader.SetInt("VertexCount",total*3);shader.SetInt("BoneCount",bones);shader.SetInt("MaterialCount",surfaces.Length);
        void Run(string name,ComputeBuffer? layouts=null,ComputeBuffer? data=null)
        {
            int k=shader.FindKernel(name);shader.SetBuffer(k,"Bound",bound);shader.SetBuffer(k,"Scatter",map);shader.SetBuffer(k,"Summary",stats);
            if(layouts!=null)shader.SetBuffer(k,"Layouts",layouts);if(data!=null)shader.SetBuffer(k,"NativeData",data);
            for(int first=0;first<total*3;first+=65536){shader.SetInt("VertexStart",first);shader.Dispatch(k,(Math.Min(65536,total*3-first)+63)/64,1,1);}
        }
        long preparedAt=Stopwatch.GetTimestamp();Run("Summarize");await lease.Read(stats,summary);long summaryAt=Stopwatch.GetTimestamp();
        var result=new Prepared {Slots=slots,Counts=groupCounts,WordOffsets=new int[slots.Length],UvDimensions=new int[slots.Length],Bounds=new Bounds[slots.Length],TriangleCount=total};
        var layouts=new uint[slots.Length*4];int words=0;
        float Decode(uint x)=>BitConverter.Int32BitsToSingle(unchecked((int)((x&0x80000000)!=0?x^0x80000000:~x)));
        for(int g=0;g<slots.Length;g++)
        {
            int dim=summary[g*8+6]==0?2:4,vertices=groupCounts[g]*3;result.WordOffsets[g]=words;result.UvDimensions[g]=dim;
            layouts[g*4]=(uint)words;layouts[g*4+1]=(uint)vertices;layouts[g*4+2]=(uint)dim;layouts[g*4+3]=(uint)slots[g];
            var minimum=new Vector3(Decode(summary[g*8]),Decode(summary[g*8+1]),Decode(summary[g*8+2]));
            var maximum=new Vector3(Decode(summary[g*8+3]),Decode(summary[g*8+4]),Decode(summary[g*8+5]));
            var bounds=new Bounds();bounds.SetMinMax(minimum,maximum);result.Bounds[g]=bounds;
            if(g==0){result.Minimum=minimum;result.Maximum=maximum;}else{result.Minimum=Vector3.Min(result.Minimum,minimum);result.Maximum=Vector3.Max(result.Maximum,maximum);}
            words=checked(words+vertices*(8+dim));
        }
        var layout=New(slots.Length,16);layout.SetData(layouts);var output=New(words,4,ComputeBufferType.Raw);
        Run("EncodeNative",layout,output);
        result.Bytes=new byte[checked(words*4)];await lease.Read(output,result.Bytes);long end=Stopwatch.GetTimestamp();
        Mod.Log.Info($"Moving road GPU stream detail: groups={slots.Length}; groupingUploadMs={(preparedAt-start)*1000.0/Stopwatch.Frequency:F3}; summaryDispatchWaitReadMs={(summaryAt-preparedAt)*1000.0/Stopwatch.Frequency:F3}; layoutEncodeWaitReadMs={(end-summaryAt)*1000.0/Stopwatch.Frequency:F3}; extraUploadBytes={scatter.Length*4+summary.Length*4+layouts.Length*4}; streamReadBytes={result.Bytes.Length}; metadataReadBytes={summary.Length*4}; streamReadCalls=1; metadataReadCalls=1; boundReadBytes=0.");
        return result;
    }
}
