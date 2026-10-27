using System.Runtime.InteropServices;
using UnityEngine;

// The exact codecs/order used by GeometryAssetSave.EmitAttributeData. This checks
// native attribute compression, not the complete Geometry/Prefab asset graph.
internal sealed class NativeUvCodec
{
    internal long Vertices,RawBytes,CompressedBytes,Blocks;
    internal NativeUvCodec(string plugin)
    {
        var library=NativeLibrary.Load(Path.GetFullPath(plugin));
        NativeLibrary.SetDllImportResolver(typeof(NativeUvCodec).Assembly,
            (name,assembly,path)=>name=="PipelinePlugin"?library:IntPtr.Zero);
    }
    [DllImport("PipelinePlugin",EntryPoint="compress_meshopt_vertex_attribute_bound")]
    private static extern long MeshBound(int count,int stride);
    [DllImport("PipelinePlugin",EntryPoint="compress_meshopt_vertex_attribute")]
    private static extern long MeshEncode(IntPtr source,int count,int stride,IntPtr target,long size);
    [DllImport("PipelinePlugin",EntryPoint="decompress_meshopt_vertex_attribute")]
    private static extern int MeshDecode(IntPtr source,long size,int count,int stride,IntPtr target);
    [DllImport("PipelinePlugin",EntryPoint="compress_calc_bound")]
    private static extern long Bound(long size,int format);
    [DllImport("PipelinePlugin",EntryPoint="compress_data")]
    private static extern long Encode(IntPtr source,long size,IntPtr target,long capacity,int format,int level);
    [DllImport("PipelinePlugin",EntryPoint="decompress_data")]
    private static extern long Decode(IntPtr source,long size,IntPtr target,long capacity,int format);

    internal void Check(IReadOnlyList<Vector2> values)
    {
        if(values.Count==0)return;
        const int stride=8;
        var raw=new byte[checked(values.Count*stride)];
        for(int i=0;i<values.Count;i++)
        {
            BitConverter.TryWriteBytes(raw.AsSpan(i*stride,4),values[i].x);
            BitConverter.TryWriteBytes(raw.AsSpan(i*stride+4,4),values[i].y);
        }
        Check(raw,values.Count,stride);
    }

    internal void Check(byte[] raw,int count,int stride)
    {
        if(raw.Length!=checked(count*stride))throw new Exception("Native UV codec input length mismatch.");
        var buffers=new List<IntPtr>();
        IntPtr Allocate(long bytes)
        {
            if(bytes<=0 || bytes>int.MaxValue)throw new Exception("Invalid native codec buffer size.");
            var pointer=Marshal.AllocHGlobal((int)bytes);buffers.Add(pointer);return pointer;
        }
        try
        {
            var source=Allocate(raw.Length);Marshal.Copy(raw,0,source,raw.Length);
            long meshCapacity=MeshBound(count,stride);
            var mesh=Allocate(meshCapacity);
            long meshSize=MeshEncode(source,count,stride,mesh,meshCapacity);
            if(meshSize<=0 || meshSize>meshCapacity)throw new Exception("Native UV meshopt encoding failed.");
            long capacity=Bound(meshSize,0);var compressed=Allocate(capacity);
            long size=Encode(mesh,meshSize,compressed,capacity,0,3);
            if(size<=0 || size>capacity)throw new Exception("Native UV ZSTD encoding failed.");
            var decodedMesh=Allocate(meshSize);
            if(Decode(compressed,size,decodedMesh,meshSize,0)!=meshSize)throw new Exception("Native UV ZSTD decoding failed.");
            var decoded=Allocate(raw.Length);
            if(MeshDecode(decodedMesh,meshSize,count,stride,decoded)!=0)throw new Exception("Native UV meshopt decoding failed.");
            var actual=new byte[raw.Length];Marshal.Copy(decoded,actual,0,actual.Length);
            if(!raw.SequenceEqual(actual))throw new Exception("Native geometry compression changed Float32 UV bits.");
            Vertices+=count;RawBytes+=raw.Length;CompressedBytes+=size;Blocks++;
        }
        finally{foreach(var pointer in buffers)Marshal.FreeHGlobal(pointer);}
    }
}
