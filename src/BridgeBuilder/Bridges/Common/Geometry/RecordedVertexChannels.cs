using System;
using Game.Prefabs;
using Colossal.Mathematics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Offline clipping supplies exact source corners and barycentric weights; no runtime face discovery.
internal static class RecordedVertexChannels
{
    internal static byte[] Interpolate(VertexAttributeDescriptor attribute, byte[] stream, int stride,
        int offset, int sourceCount, int[] corners, float[] weights)
    {
        int width = Width(attribute.format), size = width * attribute.dimension, count = corners.Length / 3;
        var dense = new byte[sourceCount * size];
        for (int i = 0; i < sourceCount; i++) Buffer.BlockCopy(stream, i * stride + offset, dense, i * size, size);
        if (attribute.attribute != VertexAttribute.Normal && attribute.attribute != VertexAttribute.Tangent)
            return InterpolateScalars(attribute.format, attribute.dimension, dense, corners, weights);
        var result = new byte[count * size];
        using var packed = new NativeArray<byte>(dense, Allocator.Temp);
        using var normals = new NativeArray<MeshNormal>(attribute.attribute == VertexAttribute.Normal ? sourceCount : 0, Allocator.Temp);
        using var tangents = new NativeArray<MeshTangent>(attribute.attribute == VertexAttribute.Tangent ? sourceCount : 0, Allocator.Temp);
        if (normals.Length != 0) MeshNormal.Unpack(new NativeSlice<byte>(packed), normals, sourceCount, attribute.format, attribute.dimension);
        if (tangents.Length != 0) MeshTangent.Unpack(new NativeSlice<byte>(packed), tangents, sourceCount, attribute.format, attribute.dimension);
        for (int i = 0; i < count; i++)
        {
            int k = i * 3;
            // Preserve untouched vertices bit-for-bit, including encoded frame and color bytes.
            int exact = weights[k] == 1f ? 0 : weights[k + 1] == 1f ? 1 : weights[k + 2] == 1f ? 2 : -1;
            if (exact >= 0) { Buffer.BlockCopy(dense, corners[k + exact] * size, result, i * size, size); continue; }
            if (normals.Length != 0)
            {
                float3 n = default;
                for (int j = 0; j < 3; j++) n += normals[corners[k + j]].m_Normal * weights[k + j];
                n = math.normalize(n);
                if (attribute.format == VertexAttributeFormat.SNorm16 && attribute.dimension == 2)
                    Buffer.BlockCopy(BitConverter.GetBytes(MathUtils.NormalToOctahedral(n)), 0, result, i * size, size);
                else for (int c = 0; c < 3; c++) Write(result, i * size + c * width, attribute.format, n[c]);
            }
            else if (tangents.Length != 0)
            {
                float4 t = default;
                for (int j = 0; j < 3; j++) t += tangents[corners[k + j]].m_Tangent * weights[k + j];
                t = NormalizeTangent(t);
                if (attribute.format == VertexAttributeFormat.Float32 && attribute.dimension == 1)
                    Buffer.BlockCopy(BitConverter.GetBytes(MathUtils.TangentToOctahedral(t)), 0, result, i * size, size);
                else for (int c = 0; c < 4; c++) Write(result, i * size + c * width, attribute.format, t[c]);
            }

        }
        return result;
    }
    internal static float4 NormalizeTangent(float4 value) =>
        new float4(math.normalize(value.xyz), value.w < 0f ? -1f : 1f);
    internal static byte[] InterpolateScalars(VertexAttributeFormat format, int dimension, byte[] source, int[] corners, float[] weights)
    {
        int width = Width(format), size = width * dimension;
        var result = new byte[corners.Length / 3 * size];
        for (int i = 0; i < corners.Length / 3; i++)
        {
            int k = i * 3;
            int exact = weights[k] == 1f ? 0 : weights[k+1] == 1f ? 1 : weights[k+2] == 1f ? 2 : -1;
            if (exact >= 0) { Buffer.BlockCopy(source, corners[k+exact]*size, result, i*size, size); continue; }
            for (int c = 0; c < dimension; c++)
            {
                double value = 0;
                for (int j = 0; j < 3; j++) value += Read(source, corners[k+j]*size+c*width, format)*weights[k+j];
                Write(result,i*size+c*width,format,value);
            }
        }
        return result;
    }
    internal static void AppendTriangles(System.Collections.Generic.List<int> output, int[] triangles, int[] sourceFaces, int firstFace, int endFace)
    {
        for (int face = 0; face < sourceFaces.Length; face++)
            if (sourceFaces[face] >= firstFace && sourceFaces[face] < endFace)
                for (int c = 0; c < 3; c++) output.Add(triangles[face*3+c]);
    }
    // Values are an explicit offline recipe; other UV components and all unspecified channels stay intact.
    internal static void WriteUv0(byte[] bytes, VertexAttributeFormat format, int dimension, UnityEngine.Vector2[] uv)
    {
        int width = Width(format);
        for (int i = 0; i < uv.Length; i++)
        {
            Write(bytes, i * dimension * width, format, uv[i].x);
            Write(bytes, (i * dimension + 1) * width, format, uv[i].y);
        }
    }
    private static int Width(VertexAttributeFormat f) => f is VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 ? 4 :
        f is VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16 or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 ? 2 : 1;
    private static double Read(byte[] b, int p, VertexAttributeFormat f) => f switch
    {
        VertexAttributeFormat.Float32 => BitConverter.ToSingle(b,p), VertexAttributeFormat.Float16 => math.f16tof32(BitConverter.ToUInt16(b,p)),
        VertexAttributeFormat.UNorm8 or VertexAttributeFormat.UInt8 => b[p], VertexAttributeFormat.SNorm8 or VertexAttributeFormat.SInt8 => (sbyte)b[p],
        VertexAttributeFormat.UNorm16 or VertexAttributeFormat.UInt16 => BitConverter.ToUInt16(b,p), VertexAttributeFormat.SNorm16 or VertexAttributeFormat.SInt16 => BitConverter.ToInt16(b,p),
        VertexAttributeFormat.UInt32 => BitConverter.ToUInt32(b,p), VertexAttributeFormat.SInt32 => BitConverter.ToInt32(b,p), _ => throw new ArgumentOutOfRangeException(nameof(f))
    };
    private static void Write(byte[] b,int p,VertexAttributeFormat f,double v)
    {
        byte[] data;
        switch(f)
        {
            case VertexAttributeFormat.Float32: data=BitConverter.GetBytes((float)v); break;
            case VertexAttributeFormat.Float16: data=BitConverter.GetBytes((ushort)math.f32tof16((float)v)); break;
            case VertexAttributeFormat.UNorm8: case VertexAttributeFormat.UInt8: b[p]=(byte)Math.Round(v); return;
            case VertexAttributeFormat.SNorm8: case VertexAttributeFormat.SInt8: b[p]=unchecked((byte)(sbyte)Math.Round(v)); return;
            case VertexAttributeFormat.UNorm16: case VertexAttributeFormat.UInt16: data=BitConverter.GetBytes((ushort)Math.Round(v)); break;
            case VertexAttributeFormat.SNorm16: case VertexAttributeFormat.SInt16: data=BitConverter.GetBytes((short)Math.Round(v)); break;
            case VertexAttributeFormat.UInt32: data=BitConverter.GetBytes((uint)Math.Round(v)); break;
            case VertexAttributeFormat.SInt32: data=BitConverter.GetBytes((int)Math.Round(v)); break;
            default: throw new ArgumentOutOfRangeException(nameof(f));
        }
        Buffer.BlockCopy(data,0,b,p,data.Length);
    }
}

