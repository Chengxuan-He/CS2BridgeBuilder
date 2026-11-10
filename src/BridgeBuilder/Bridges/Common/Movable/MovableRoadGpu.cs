using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Generation only. The bound GPU buffer feeds Encode before either result is read back.
internal static partial class MovableRoadGpu
{
    private struct Step { internal Vector4 Plane; internal uint Positive; }
    private struct Path { internal uint First, Count, Bone; }
    private struct Root { internal uint First, Count; }
    private static ComputeShader? _clip, _encode;
    private const int DispatchRows = 2048;
    // Explicit working-set guard, not an input/bridge acceptance limit: use full CPU fallback.
    private const int MaximumTriangles = 2000000;

    internal static bool TryBuild(IReadOnlyList<MovableRoadGeometry.Triangle> source,
        MovableRoadGeometry.Partition partition, int boneCount, int materialCount,
        out MovableRoadGeometry.BoundTriangle[] result, out uint[]? encoded, out string? error,
        bool allowEmpty = false, IReadOnlyList<MovableRoadGeometry.Partition>? occurrencePartitions = null,
        IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? trianglePartitions = null)
    {
        result = Array.Empty<MovableRoadGeometry.BoundTriangle>(); encoded = null; error = null;
        if(source==null){error="Road construction requires source triangles.";return false;}
        if (source.Count == 0) { encoded = Array.Empty<uint>(); return true; }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Execute(source, partition, boneCount, materialCount, occurrencePartitions, trianglePartitions,
                out result, out encoded, null, false, out _);
            Mod.Log.Info($"Moving road GPU construction: input={source.Count}; output={result.Length}; doubleClip=true; connectedEncode=true; totalMs={(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency:F3}.");
            return true;
        }
        catch (Exception exception)
        {
            result = Array.Empty<MovableRoadGeometry.BoundTriangle>(); encoded = null;
            Mod.Log.Warn($"Moving road GPU construction: complete CPU fallback; reason={exception.GetType().Name}: {exception.Message}");
            return MovableRoadGeometry.TryBind(source, partition, boneCount, materialCount, out result,
                out error, allowEmpty, occurrencePartitions, trianglePartitions);
        }
    }

    private static void LoadPrograms()
    {
        if (_clip != null && _encode != null) return;
        using var stream = typeof(MovableRoadGpu).Assembly.GetManifestResourceStream("BridgeBuilder.RoadGpu")
            ?? throw new InvalidOperationException("Generation compute bundle is missing.");
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        var bundle = AssetBundle.LoadFromMemory(bytes.ToArray());
        if (bundle == null) throw new InvalidOperationException("Generation compute bundle could not be loaded.");
        ComputeShader? clip = null, encode = null;
        try
        {
            clip = bundle.LoadAsset<ComputeShader>("Assets/RoadGpu136/RoadClip.compute");
            encode = bundle.LoadAsset<ComputeShader>("Assets/RoadGpu136/RoadEncode.compute");
            if (clip == null || encode == null || !clip.HasKernel("Count") || !clip.HasKernel("Emit") || !clip.HasKernel("Probe") || !encode.HasKernel("Encode") || !encode.HasKernel("Summarize") || !encode.HasKernel("EncodeNative") || !encode.HasKernel("VerifyNative"))
                throw new InvalidOperationException("Generation compute kernels are unavailable.");
            using (var input=new ComputeBuffer(1,4,ComputeBufferType.Raw))
            using (var output=new ComputeBuffer(1,4,ComputeBufferType.Raw))
            {
                input.SetData(new uint[]{0x3f800000});output.SetData(new uint[]{0xffffffff});
                var k=clip.FindKernel("Probe");clip.SetBuffer(k,"Input",input);clip.SetBuffer(k,"Output",output);clip.Dispatch(k,1,1,1);
                var observed=new uint[1];output.GetData(observed);
                if(observed[0]!=0x2b800000)throw new NotSupportedException("Double compute execution probe did not preserve 2^-40 at unity scale.");
            }
            _clip = clip; _encode = encode;
        }
        catch { if (clip != null) UnityEngine.Object.Destroy(clip); if (encode != null) UnityEngine.Object.Destroy(encode); throw; }
        finally { bundle.Unload(false); }
    }

    private static void Execute(IReadOnlyList<MovableRoadGeometry.Triangle> source,
        MovableRoadGeometry.Partition partition, int boneCount, int materialCount,
        IReadOnlyList<MovableRoadGeometry.Partition>? occurrences,
        IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? faces,
        out MovableRoadGeometry.BoundTriangle[] result, out uint[] encoded,
        Colossal.IO.AssetDatabase.SurfaceAsset[]? surfaces, bool capture, out Prepared? prepared, Pose? pose=null,MovableMaterialCompatibility.Mapping[]? mappings=null)
    {
        var value=GenerationWork.Synchronous(async()=>{var output=new Execution();await ExecuteAsync(source,partition,boneCount,materialCount,occurrences,faces,output,surfaces,capture,pose,mappings);return output;});
        result=value.Bound;encoded=value.Encoded;prepared=value.Prepared;
    }
    private sealed class Execution
    {
        internal MovableRoadGeometry.BoundTriangle[] Bound=Array.Empty<MovableRoadGeometry.BoundTriangle>();
        internal uint[] Encoded=Array.Empty<uint>();internal Prepared? Prepared;
    }
    private static async System.Threading.Tasks.Task ExecuteAsync(IReadOnlyList<MovableRoadGeometry.Triangle> source,
        MovableRoadGeometry.Partition partition,int boneCount,int materialCount,
        IReadOnlyList<MovableRoadGeometry.Partition>? occurrences,
        IReadOnlyDictionary<(int Occurrence,int SourceTriangle),MovableRoadGeometry.Partition>? faces,
        Execution output,Colossal.IO.AssetDatabase.SurfaceAsset[]? surfaces,bool capture,Pose? pose=null,
        MovableMaterialCompatibility.Mapping[]? mappings=null)
    {
        MovableRoadGeometry.BoundTriangle[] result=Array.Empty<MovableRoadGeometry.BoundTriangle>();uint[] encoded=Array.Empty<uint>();Prepared? prepared=null;
        try
        {
        await GenerationWork.Next("GPU input planning");
        long phaseStart=Stopwatch.GetTimestamp(), preparedAt=0, uploadedAt=0, countedAt=0, prefixAt=0, emittedAt=0;
        long uploadBytes=0, readBytes=0; int readCalls=0;
        if (!SystemInfo.supportsComputeShaders || SystemInfo.graphicsShaderLevel < 50 ||
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
            throw new NotSupportedException("Validated double compute backend requires Direct3D 11 SM5.");
        if (source == null || source.Count > MaximumTriangles || boneCount <= 0 || materialCount <= 0)
            throw new InvalidOperationException("GPU input capacity or binding description is invalid.");
        if (Marshal.SizeOf<MovableRoadGeometry.Triangle>() != 228 || Marshal.SizeOf<MovableRoadGeometry.BoundTriangle>() != 232)
            throw new InvalidOperationException("Unexpected native road attribute layout.");
        var validated = new HashSet<MovableRoadGeometry.Partition>();
        void Validate(MovableRoadGeometry.Partition tree)
        {
            if (validated.Add(tree) && !MovableRoadGeometry.Validate(tree, boneCount, new HashSet<MovableRoadGeometry.Partition>()))
                throw new InvalidOperationException("Invalid measured motion partition.");
        }
        Validate(partition);
        if (occurrences != null) foreach (var tree in occurrences) Validate(tree);
        if (faces != null) foreach (var item in faces)
        { if (item.Key.Occurrence < 0 || item.Key.SourceTriangle < 0) throw new InvalidOperationException("Invalid source face identity."); Validate(item.Value); }
        var steps = new List<Step>(); var paths = new List<Path>();
        var roots = new Root[source.Count]; var ranges = new Dictionary<MovableRoadGeometry.Partition,Root>();
        bool validateVertices = surfaces == null;
        await GenerationWork.Background("GPU partition input arrays", () =>
        {
        Root Flatten(MovableRoadGeometry.Partition tree)
        {
            if (ranges.TryGetValue(tree, out var known)) return known;
            var first = paths.Count; var stack = new List<Step>();
            void Visit(MovableRoadGeometry.Partition node)
            {
                if (node.IsGap) return;
                if (node.Bone >= 0)
                {
                    if (paths.Count >= 65536 || steps.Count > 1000000) throw new InvalidOperationException("GPU partition working limit exceeded.");
                    paths.Add(new Path { First=(uint)steps.Count, Count=(uint)stack.Count, Bone=(uint)node.Bone }); steps.AddRange(stack); return;
                }
                if (stack.Count >= 24) throw new InvalidOperationException("GPU partition depth limit exceeded.");
                var f=node.ObjectToSeam;var plane=new Vector4(f[2,0],f[2,1],f[2,2],f[2,3]);
                stack.Add(new Step { Plane=plane }); Visit(node.Negative!);
                stack[stack.Count-1]=new Step { Plane=plane, Positive=1 }; Visit(node.Positive!); stack.RemoveAt(stack.Count-1);
            }
            Visit(tree); var value=new Root { First=(uint)first, Count=(uint)(paths.Count-first) };ranges.Add(tree,value);return value;
        }
        long pathWork=0;
        for (var i=0;i<source.Count;i++)
        {
            var t=source[i];
            if ((uint)t.Material >= (uint)materialCount || (validateVertices && (!MovableRoadGeometry.Valid(t.A) || !MovableRoadGeometry.Valid(t.B) || !MovableRoadGeometry.Valid(t.C))))
                throw new InvalidOperationException($"Invalid source triangle {i}.");
            var tree=partition;
            if (faces!=null)
            { if (!faces.TryGetValue((t.Occurrence,t.SourceTriangle),out tree)) throw new InvalidOperationException("Missing source-face partition."); }
            else if (occurrences!=null)
            { if ((uint)t.Occurrence >= (uint)occurrences.Count) throw new InvalidOperationException("Missing occurrence partition."); tree=occurrences[t.Occurrence]; }
            roots[i]=Flatten(tree);
            pathWork+=roots[i].Count;
            if(roots[i].Count>256 || pathWork>16000000)
                throw new InvalidOperationException("GPU partition traversal working limit exceeded.");
        }
        return true;
        });
        LoadPrograms();
        var lease=new GenerationGpuLease();var owned=lease.Buffers;ComputeShader? clip=null,encode=null;
        lease.ReleaseShaders=()=>{if(clip!=null)UnityEngine.Object.Destroy(clip);if(encode!=null)UnityEngine.Object.Destroy(encode);};
        try
        {
            clip=UnityEngine.Object.Instantiate(_clip!);encode=UnityEngine.Object.Instantiate(_encode!);
            ComputeBuffer Buffer(int n,int stride,ComputeBufferType type=ComputeBufferType.Default)
            { var b=new ComputeBuffer(Math.Max(1,n),stride,type);owned.Add(b);return b; }
            ComputeBuffer Upload(Array a,int stride)
            { var b=Buffer(a.Length,stride);if(a.Length!=0)b.SetData(a);return b; }
            var sourceArray=source as MovableRoadGeometry.Triangle[] ?? source.ToArray();
            preparedAt=Stopwatch.GetTimestamp();
            var input=Buffer(checked(source.Count*57),4,ComputeBufferType.Raw);
            if(surfaces!=null)input.SetData(sourceArray);else input.SetData(Words(sourceArray,228));
            var originalInput=input;
            long coordinateUploadBytes=0;
            if(pose.HasValue)
            {
                if(mappings==null||mappings.Length!=materialCount)throw new InvalidOperationException("Missing material coordinate mappings.");
                var q=pose.Value.rotation;var offset=pose.Value.position;var length=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
                if(!float.IsFinite(length)||length<=0||!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!float.IsFinite(offset.z))throw new InvalidOperationException("Invalid road coordinate frame.");
                q=q.normalized;float x=q.x*2f,y=q.y*2f,z=q.z*2f,xx=q.x*x,yy=q.y*y,zz=q.z*z,xy=q.x*y,xz=q.x*z,yz=q.y*z,wx=q.w*x,wy=q.w*y,wz=q.w*z;
                encode.SetVector("Rotation0",new Vector4(xx,yy,zz,xy));
                encode.SetVector("Rotation1",new Vector4(xz,yz,wx,wy));
                encode.SetVector("Rotation2",new Vector4(wz,0,0,0));encode.SetVector("FrameOffset",offset);
                var mapValues=mappings.Select(m=>new Vector2(m.Planar?1:0,m.Scale)).ToArray();var map=Upload(mapValues,8);coordinateUploadBytes=mapValues.Length*8;
                var transformed=Buffer(checked(source.Count*57),4,ComputeBufferType.Raw);int transform=encode.FindKernel("TransformInput");
                encode.SetBuffer(transform,"SourceInput",input);encode.SetBuffer(transform,"Transformed",transformed);encode.SetBuffer(transform,"Mappings",map);encode.SetInt("VertexCount",source.Count*3);
                for(int first=0;first<source.Count*3;first+=65536){encode.SetInt("VertexStart",first);encode.Dispatch(transform,(Math.Min(65536,source.Count*3-first)+63)/64,1,1);}
                input=transformed;
            }
            var rb=Upload(roots,8);var pb=Upload(paths.ToArray(),12);var sb=Upload(steps.ToArray(),20);
            var counts=Upload(Enumerable.Repeat(uint.MaxValue,source.Count).ToArray(),4);var errors=Upload(new uint[source.Count],4);var offsets=Buffer(source.Count,4);
            var dummy=Buffer(1,4,ComputeBufferType.Raw);
            uploadBytes=coordinateUploadBytes+(long)source.Count*(228+8+8)+(long)paths.Count*12+(long)steps.Count*20; uploadedAt=Stopwatch.GetTimestamp();
            void Dispatch(int kernel,ComputeBuffer destination)
            {
                clip.SetInt("TriangleCount",source.Count);clip.SetBuffer(kernel,"Input",input);clip.SetBuffer(kernel,"OriginalInput",originalInput);clip.SetBuffer(kernel,"Roots",rb);
                clip.SetBuffer(kernel,"Paths",pb);clip.SetBuffer(kernel,"Steps",sb);clip.SetBuffer(kernel,"Counts",counts);
                clip.SetBuffer(kernel,"Errors",errors);clip.SetBuffer(kernel,"Offsets",offsets);clip.SetBuffer(kernel,"Output",destination);
                for(int first=0;first<source.Count;first+=DispatchRows)
                {clip.SetInt("RowStart",first);clip.Dispatch(kernel,(Math.Min(DispatchRows,source.Count-first)+31)/32,1,1);}
            }
            async System.Threading.Tasks.Task CheckErrors()
            {var e=new uint[source.Count];await lease.Read(errors,e);readCalls++;readBytes+=(long)e.Length*4;if(e.Any(x=>x!=0))throw new InvalidOperationException("GPU clipping capacity or count/emission consistency check failed.");}
            Dispatch(clip.FindKernel("Count"),dummy);var cc=new uint[source.Count];await lease.Read(counts,cc);readCalls++;readBytes+=(long)cc.Length*4;await CheckErrors();countedAt=Stopwatch.GetTimestamp();
            var oo=new uint[source.Count];uint sum=0;
            for(int i=0;i<oo.Length;i++){oo[i]=sum;sum=checked(sum+cc[i]);}
            if(sum>MaximumTriangles)throw new InvalidOperationException("GPU output working capacity exceeded.");
            if(sum==0){result=Array.Empty<MovableRoadGeometry.BoundTriangle>();encoded=Array.Empty<uint>();return;}
            offsets.SetData(oo);var bound=Buffer(checked((int)sum*58),4,ComputeBufferType.Raw);
            uploadBytes+=(long)oo.Length*4;prefixAt=Stopwatch.GetTimestamp();
            Dispatch(clip.FindKernel("Emit"),bound);await CheckErrors();emittedAt=Stopwatch.GetTimestamp();
            if(surfaces!=null)
            {
                prepared=await PrepareNativeAsync(sourceArray,cc,oo,(int)sum,boneCount,surfaces,bound,encode,lease);
                encoded=Array.Empty<uint>();
                result=Array.Empty<MovableRoadGeometry.BoundTriangle>();
                if(capture)
                {
                    result=new MovableRoadGeometry.BoundTriangle[sum];
                    await lease.Read(bound,result);readCalls++;readBytes+=(long)sum*232;
                }
                Mod.Log.Info($"Moving road GPU pipeline detail: prepareMs={(preparedAt-phaseStart)*1000.0/Stopwatch.Frequency:F3}; uploadMs={(uploadedAt-preparedAt)*1000.0/Stopwatch.Frequency:F3}; countDispatchWaitReadMs={(countedAt-uploadedAt)*1000.0/Stopwatch.Frequency:F3}; prefixAllocateUploadMs={(prefixAt-countedAt)*1000.0/Stopwatch.Frequency:F3}; emitDispatchWaitReadMs={(emittedAt-prefixAt)*1000.0/Stopwatch.Frequency:F3}; inputUploadBytes={uploadBytes}; clippingReadBytes={readBytes}; clippingReadCalls={readCalls}; capture={capture}. Blocking read timings include preceding GPU work; native stream detail is separate.");
                return;
            }
            var packed=Buffer(checked((int)sum*33),4,ComputeBufferType.Raw);
            var kernel=encode.FindKernel("Encode");var vertices=checked((int)sum*3);
            encode.SetInt("VertexCount",vertices);encode.SetBuffer(kernel,"Bound",bound);encode.SetBuffer(kernel,"Encoded",packed);
            for(int first=0;first<vertices;first+=65536)
            {encode.SetInt("VertexStart",first);encode.Dispatch(kernel,(Math.Min(65536,vertices-first)+63)/64,1,1);}
            encoded=new uint[checked((int)sum*33)];await lease.Read(packed,encoded);
            var words=new uint[checked((int)sum*58)];await lease.Read(bound,words);
            result=new MovableRoadGeometry.BoundTriangle[sum];var handle=GCHandle.Alloc(result,GCHandleType.Pinned);
            try {var bytes=new byte[checked(words.Length*4)];System.Buffer.BlockCopy(words,0,bytes,0,bytes.Length);Marshal.Copy(bytes,0,handle.AddrOfPinnedObject(),bytes.Length);}
            finally {handle.Free();}
            foreach(var t in result)
                if(t.Bone>=boneCount || (uint)t.Geometry.Material>=(uint)materialCount || !MovableRoadGeometry.Valid(t.Geometry.A) || !MovableRoadGeometry.Valid(t.Geometry.B) || !MovableRoadGeometry.Valid(t.Geometry.C))
                    throw new InvalidOperationException("GPU output has invalid attributes or binding.");
            for(int i=0;i<result.Length;i++)
            {
                var t=result[i];
                for(int j=0;j<3;j++)
                {
                    var v=j==0?t.Geometry.A:j==1?t.Geometry.B:t.Geometry.C;var offset=(i*3+j)*11;
                    if(encoded[offset]!=Bits(v.Position.x) || encoded[offset+1]!=Bits(v.Position.y) || encoded[offset+2]!=Bits(v.Position.z) || encoded[offset+10]!=t.Bone)
                        throw new InvalidOperationException("GPU encoding did not preserve position or bone identity.");
                    for(int k=0;k<4;k++)if(encoded[offset+5+k]!=Bits(v.UV[k]))
                        throw new InvalidOperationException("GPU encoding did not preserve complete UV channels.");
                }
            }
        }
        finally
        {
            lease.Dispose();
        }
        }
        finally{output.Bound=result;output.Encoded=encoded;output.Prepared=prepared;}
    }

    private static uint Bits(float value)=>unchecked((uint)BitConverter.SingleToInt32Bits(value));

    private static uint[] Words(Array values,int stride)
    {
        var bytes=new byte[checked(values.Length*stride)];var pin=GCHandle.Alloc(values,GCHandleType.Pinned);
        try{Marshal.Copy(pin.AddrOfPinnedObject(),bytes,0,bytes.Length);}finally{pin.Free();}
        var words=new uint[bytes.Length/4];Buffer.BlockCopy(bytes,0,words,0,bytes.Length);return words;
    }
}
