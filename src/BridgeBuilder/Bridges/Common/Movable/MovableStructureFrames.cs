using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.AssetPipeline.Importers;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Explicit source memberships only. The style owns the matching position transform;
// this applies its local direction Jacobian without changing UVs, colors or skinning.
internal static class MovableStructureFrames
{
    internal static void Apply(List<ModelImporter.Model.VertexData> attributes,
        KeyValuePair<int, Matrix4x4>[] transforms, int[]? used, int count, bool nativeBone = false)
    {
        if (transforms.Length == 0) return;
        var normal = attributes.SingleOrDefault(a => a.attribute == VertexAttribute.Normal);
        var tangent = attributes.SingleOrDefault(a => a.attribute == VertexAttribute.Tangent);
        using var normals = new NativeArray<MeshNormal>(count, Allocator.Temp);
        using var tangents = new NativeArray<MeshTangent>(count, Allocator.Temp);
        MeshNormal.Unpack(new NativeSlice<byte>(normal.data), normals, count, normal.format, normal.dimension);
        MeshTangent.Unpack(new NativeSlice<byte>(tangent.data), tangents, count, tangent.format, tangent.dimension);
        var remap = used?.Select((source, output) => (source, output)).ToDictionary(p => p.source, p => p.output);
        foreach (var pair in transforms)
        {
            int index = pair.Key;
            if (remap != null && !remap.TryGetValue(pair.Key, out index)) continue;
            var sourceT = tangents[index].m_Tangent;
            var originalNormal = (Vector3)normals[index].m_Normal;
            var n = nativeBone ? Unit(pair.Value.MultiplyVector(originalNormal), originalNormal) : pair.Value.determinant == 0f ? originalNormal :
                Unit(pair.Value.inverse.transpose.MultiplyVector(originalNormal), originalNormal);
            var forwardT = pair.Value.MultiplyVector((Vector3)sourceT.xyz);
            var t = Unit(nativeBone ? forwardT : forwardT - n * Vector3.Dot(n, forwardT), (Vector3)sourceT.xyz);
            Write(normal.data, index, MathUtils.NormalToOctahedral((float3)n));
            Write(tangent.data, index, MathUtils.TangentToOctahedral(new float4((float3)t,
                !nativeBone && pair.Value.determinant < 0f ? -sourceT.w : sourceT.w)));
        }
    }

    private static Vector3 Unit(Vector3 value, Vector3 original)
    {
        double length = Math.Sqrt((double)value.x * value.x + (double)value.y * value.y + (double)value.z * value.z);
        if (length == 0) return original;
        return new Vector3((float)(value.x / length), (float)(value.y / length), (float)(value.z / length));
    }

    private static void Write(NativeArray<byte> bytes, int index, uint value)
    {
        var encoded = BitConverter.GetBytes(value);
        for (int i = 0; i < 4; i++) bytes[index * 4 + i] = encoded[i];
    }
}
