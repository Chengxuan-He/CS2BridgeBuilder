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
internal static class MovableRoadGpu
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
                out result, out encoded);
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
            if (clip == null || encode == null || !clip.HasKernel("Count") || !clip.HasKernel("Emit") || !clip.HasKernel("Probe") || !encode.HasKernel("Encode"))
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
        out MovableRoadGeometry.BoundTriangle[] result, out uint[] encoded)
    {
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
            if ((uint)t.Material >= (uint)materialCount || !MovableRoadGeometry.Valid(t.A) || !MovableRoadGeometry.Valid(t.B) || !MovableRoadGeometry.Valid(t.C))
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
        LoadPrograms();
        var owned=new List<ComputeBuffer>();ComputeShader? clip=null,encode=null;
        try
        {
            clip=UnityEngine.Object.Instantiate(_clip!);encode=UnityEngine.Object.Instantiate(_encode!);
            ComputeBuffer Buffer(int n,int stride,ComputeBufferType type=ComputeBufferType.Default)
            { var b=new ComputeBuffer(Math.Max(1,n),stride,type);owned.Add(b);return b; }
            ComputeBuffer Upload(Array a,int stride)
            { var b=Buffer(a.Length,stride);if(a.Length!=0)b.SetData(a);return b; }
            var sourceArray=source as MovableRoadGeometry.Triangle[] ?? source.ToArray();
            var input=Buffer(checked(source.Count*57),4,ComputeBufferType.Raw);input.SetData(Words(sourceArray,228));
            var rb=Upload(roots,8);var pb=Upload(paths.ToArray(),12);var sb=Upload(steps.ToArray(),20);
            var counts=Upload(Enumerable.Repeat(uint.MaxValue,source.Count).ToArray(),4);var errors=Upload(new uint[source.Count],4);var offsets=Buffer(source.Count,4);
            var dummy=Buffer(1,4,ComputeBufferType.Raw);
            void Dispatch(int kernel,ComputeBuffer destination)
            {
                clip.SetInt("TriangleCount",source.Count);clip.SetBuffer(kernel,"Input",input);clip.SetBuffer(kernel,"Roots",rb);
                clip.SetBuffer(kernel,"Paths",pb);clip.SetBuffer(kernel,"Steps",sb);clip.SetBuffer(kernel,"Counts",counts);
                clip.SetBuffer(kernel,"Errors",errors);clip.SetBuffer(kernel,"Offsets",offsets);clip.SetBuffer(kernel,"Output",destination);
                for(int first=0;first<source.Count;first+=DispatchRows)
                {clip.SetInt("RowStart",first);clip.Dispatch(kernel,(Math.Min(DispatchRows,source.Count-first)+31)/32,1,1);}
            }
            void CheckErrors()
            {var e=new uint[source.Count];errors.GetData(e);if(e.Any(x=>x!=0))throw new InvalidOperationException("GPU clipping capacity or count/emission consistency check failed.");}
            Dispatch(clip.FindKernel("Count"),dummy);var cc=new uint[source.Count];counts.GetData(cc);CheckErrors();
            var oo=new uint[source.Count];uint sum=0;
            for(int i=0;i<oo.Length;i++){oo[i]=sum;sum=checked(sum+cc[i]);}
            if(sum>MaximumTriangles)throw new InvalidOperationException("GPU output working capacity exceeded.");
            if(sum==0){result=Array.Empty<MovableRoadGeometry.BoundTriangle>();encoded=Array.Empty<uint>();return;}
            offsets.SetData(oo);var bound=Buffer(checked((int)sum*58),4,ComputeBufferType.Raw);
            Dispatch(clip.FindKernel("Emit"),bound);CheckErrors();
            var packed=Buffer(checked((int)sum*33),4,ComputeBufferType.Raw);
            var kernel=encode.FindKernel("Encode");var vertices=checked((int)sum*3);
            encode.SetInt("VertexCount",vertices);encode.SetBuffer(kernel,"Bound",bound);encode.SetBuffer(kernel,"Encoded",packed);
            for(int first=0;first<vertices;first+=65536)
            {encode.SetInt("VertexStart",first);encode.Dispatch(kernel,(Math.Min(65536,vertices-first)+63)/64,1,1);}
            encoded=new uint[checked((int)sum*33)];packed.GetData(encoded);
            var words=new uint[checked((int)sum*58)];bound.GetData(words);
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
            foreach(var buffer in owned)try{buffer.Dispose();}catch(Exception e){Mod.Log.Warn("GPU buffer cleanup: "+e.Message);}
            if(clip!=null)UnityEngine.Object.Destroy(clip);if(encode!=null)UnityEngine.Object.Destroy(encode);
        }
    }

    private static uint Bits(float value)=>unchecked((uint)BitConverter.SingleToInt32Bits(value));

    private static uint[] Words(Array values,int stride)
    {
        var bytes=new byte[checked(values.Length*stride)];var pin=GCHandle.Alloc(values,GCHandleType.Pinned);
        try{Marshal.Copy(pin.AddrOfPinnedObject(),bytes,0,bytes.Length);}finally{pin.Free();}
        var words=new uint[bytes.Length/4];Buffer.BlockCopy(bytes,0,words,0,bytes.Length);return words;
    }
}
