using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Colossal.AssetPipeline.Native;
using UnityEngine;
using UnityEngine.Rendering;
namespace BridgeBuilder.Bridges;

// Stateless native arithmetic on pinned owned arrays; no Mesh, ECS, allocator or rendering API.
internal static class MovablePackedFrame
{
    internal sealed class Snapshot
    {
        internal readonly byte[] Bytes;internal readonly int Offset,Stride,Width,Count,Dimension;
        internal readonly VertexAttributeFormat Format;
        internal Snapshot(byte[] bytes,int offset,int stride,int width,int count,VertexAttributeFormat format,int dimension)
        {Bytes=bytes;Offset=offset;Stride=stride;Width=width;Count=count;Format=format;Dimension=dimension;}
        internal byte[] Gather()
        {
            var result=new byte[checked(Count*Width)];
            for(int i=0;i<Count;i++)Buffer.BlockCopy(Bytes,checked(i*Stride+Offset),result,i*Width,Width);
            return result;
        }
    }
    // Call only during the native snapshot phase. Each native stream is copied once, not per vertex.
    internal static Snapshot Read(Mesh mesh,Mesh.MeshData data,VertexAttribute attribute,Dictionary<int,byte[]> streams)
    {
        int stream=mesh.GetVertexAttributeStream(attribute);
        if(!streams.TryGetValue(stream,out var bytes))streams.Add(stream,bytes=data.GetVertexData<byte>(stream).ToArray());
        var format=mesh.GetVertexAttributeFormat(attribute);int dimension=mesh.GetVertexAttributeDimension(attribute);
        int element=format switch
        {
            VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32=>4,
            VertexAttributeFormat.Float16 or VertexAttributeFormat.SNorm16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16=>2,
            VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8 or VertexAttributeFormat.SInt8=>1,
            _=>throw new InvalidDataException("Unsupported packed frame format: "+format)
        };
        return new Snapshot(bytes,mesh.GetVertexAttributeOffset(attribute),mesh.GetVertexBufferStride(stream),checked(element*dimension),mesh.vertexCount,format,dimension);
    }
    internal static (Vector3[] Normals,Vector4[] Tangents) DecodeSnapshots(Snapshot normals,Snapshot tangents)
        =>Decode(normals.Gather(),tangents.Gather(),normals.Count,normals.Format,normals.Dimension,tangents.Format,tangents.Dimension);
    internal static (Vector3[] Normals,Vector4[] Tangents) Decode(byte[] normalBytes,byte[] tangentBytes,
        int count,VertexAttributeFormat normalFormat,int normalDimension,VertexAttributeFormat tangentFormat,int tangentDimension)
    {
        var normals=new Vector3[count];var tangents=new Vector4[count];
        Convert(normalBytes,normals,count,normalFormat,normalDimension,3,false);
        Convert(tangentBytes,tangents,count,tangentFormat,tangentDimension,4,true);
        return (normals,tangents);
    }
    private static void Convert(byte[] source,Array target,int count,VertexAttributeFormat format,int dimension,int components,bool tangent)
    {
        int bytes=format==VertexAttributeFormat.Float32?4:2;
        var input=GCHandle.Alloc(source,GCHandleType.Pinned);var output=default(GCHandle);
        try
        {
            output=GCHandle.Alloc(target,GCHandleType.Pinned);
            if(format==VertexAttributeFormat.Float32&&dimension==components)
                Marshal.Copy(source,0,output.AddrOfPinnedObject(),source.Length);
            else if(format==VertexAttributeFormat.Float16)
                NativeMath.ArrayHalfToFloat(input.AddrOfPinnedObject(),count,dimension,output.AddrOfPinnedObject(),components);
            else if(!tangent&&format==VertexAttributeFormat.SNorm16&&dimension==2)
                NativeMath.ArrayOctahedralToNormals(input.AddrOfPinnedObject(),count,output.AddrOfPinnedObject());
            else if(tangent&&format==VertexAttributeFormat.Float32&&dimension==1)
                NativeMath.ArrayOctahedralToTangents(input.AddrOfPinnedObject(),count,output.AddrOfPinnedObject());
            else throw new InvalidDataException("Unsupported packed frame format/dimension: "+format+"/"+dimension);
        }
        finally{if(output.IsAllocated)output.Free();input.Free();}
    }
}
