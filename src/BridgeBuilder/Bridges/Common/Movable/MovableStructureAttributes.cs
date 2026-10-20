using System;
using System.IO;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Only appended vertices enter this path. Original vertex-channel bytes are never re-encoded.
// Native MeshNormal/MeshTangent unpack the game's packed representations, including the
// tangent's UInt32 octahedral payload stored in a Float32x1 channel.
internal static class MovableStructureAttributes
{
    internal static bool TryInterpolate(VertexAttributeDescriptor attribute, byte[] bytes,
        int a, int b, int c, int output, float weightB, float weightC)
    {
        var normal = attribute.attribute == VertexAttribute.Normal &&
            attribute.format == VertexAttributeFormat.SNorm16 && attribute.dimension == 2;
        var tangent = attribute.attribute == VertexAttribute.Tangent &&
            attribute.format == VertexAttributeFormat.Float32 && attribute.dimension == 1;
        if (normal || tangent)
        {
            var packed = new byte[12];
            for (var i = 0; i < 4; i++)
            { packed[i] = bytes[a + i]; packed[4 + i] = bytes[b + i]; packed[8 + i] = bytes[c + i]; }
            using var input = new NativeArray<byte>(packed, Allocator.Temp);
            uint encoded;
            if (normal)
            {
                using var values = new NativeArray<MeshNormal>(3, Allocator.Temp);
                MeshNormal.Unpack(new NativeSlice<byte>(input), values, 3, attribute.format, attribute.dimension);
                var first = values[0].m_Normal;
                var value = first + (values[1].m_Normal - first) * weightB +
                    (values[2].m_Normal - first) * weightC;
                encoded = MathUtils.NormalToOctahedral(Unit(value));
            }
            else
            {
                using var values = new NativeArray<MeshTangent>(3, Allocator.Temp);
                MeshTangent.Unpack(new NativeSlice<byte>(input), values, 3, attribute.format, attribute.dimension);
                var first = values[0].m_Tangent;
                // Native source triangles can contain both handedness signs (Lift01 pillar
                // top LOD2 does). Interpolate the full tangent before native packing selects
                // the sign of w; retaining the first endpoint's sign biases the new vertex.
                var value = first + (values[1].m_Tangent - first) * weightB +
                    (values[2].m_Tangent - first) * weightC;
                encoded = MathUtils.TangentToOctahedral(new float4(Unit(value.xyz), value.w));
            }
            Buffer.BlockCopy(BitConverter.GetBytes(encoded), 0, bytes, output, 4);
            return true;
        }
        if (attribute.attribute == VertexAttribute.Color && attribute.format == VertexAttributeFormat.UNorm8)
        {
            for (var channel = 0; channel < attribute.dimension; channel++)
            {
                var value = bytes[a + channel] + (bytes[b + channel] - bytes[a + channel]) * weightB +
                    (bytes[c + channel] - bytes[a + channel]) * weightC;
                bytes[output + channel] = (byte)math.clamp(math.round(value), 0f, 255f);
            }
            return true;
        }
        return false;
    }

    private static float3 Unit(float3 value)
    {
        if (!math.all(math.isfinite(value)) || math.lengthsq(value) == 0f)
            throw new InvalidDataException("A structural subdivision has no finite interpolated direction.");
        return math.normalize(value);
    }
}
