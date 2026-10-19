using System;
using System.Collections.Generic;
using Colossal.AssetPipeline.Importers;
using Colossal.Mathematics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Encodes newly constructed road geometry using native object attributes and rigid UInt32 skin
/// indices. Carried mechanism meshes do not pass through this encoder: their channels stay raw.
/// </summary>
internal static class MovableRoadModel
{
    internal static bool TryBuild(string name, IReadOnlyList<MovableRoadGeometry.BoundTriangle> triangles,
        int materialCount, int boneCount, out ModelImporter.Model? model, out string? error)
    {
        model = null; error = null;
        if (string.IsNullOrEmpty(name) || triangles == null || triangles.Count == 0 ||
            triangles.Count > int.MaxValue / 3 || materialCount <= 0 || boneCount <= 0)
        { error = "A moving road model requires an owner, bound triangles and material slots."; return false; }

        var count = triangles.Count * 3;
        var positions = new float[count * 3]; var normals = new uint[count];
        var tangents = new uint[count]; var uv = new ushort[count * 4];
        var surfaceUV = new float[count * 2];
        var planarUV = true;
        var colors = new byte[count * 4]; var bones = new uint[count];
        var slots = new List<int>[materialCount];
        for (var i = 0; i < slots.Length; i++) slots[i] = new List<int>();
        var bounds = new Bounds[materialCount]; var initialized = new bool[materialCount];
        for (var i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i]; var geometry = triangle.Geometry;
            if (geometry.Material < 0 || geometry.Material >= materialCount || triangle.Bone >= boneCount)
            { error = $"Road triangle {i} references a missing material slot or bone."; return false; }
            var vertices = new[] { geometry.A, geometry.B, geometry.C };
            for (var j = 0; j < 3; j++)
            {
                var index = i * 3 + j; var vertex = vertices[j];
                if (!MovableRoadGeometry.Valid(vertex))
                { error = $"Road triangle {i} has invalid evaluated vertex attributes."; return false; }
                positions[index * 3] = vertex.Position.x;
                positions[index * 3 + 1] = vertex.Position.y;
                positions[index * 3 + 2] = vertex.Position.z;
                normals[index] = MathUtils.NormalToOctahedral((float3)vertex.Normal);
                tangents[index] = MathUtils.TangentToOctahedral((float4)vertex.Tangent);
                surfaceUV[index * 2] = vertex.UV.x;
                surfaceUV[index * 2 + 1] = vertex.UV.y;
                planarUV &= vertex.UV.z == 0f && vertex.UV.w == 0f;
                for (var k = 0; k < 4; k++)
                {
                    var value = new half(vertex.UV[k]);
                    if (!math.isfinite((float)value))
                    { error = $"Road triangle {i} has UV values outside the native Float16 range."; return false; }
                    uv[index * 4 + k] = value.value;
                    // Colors describe a baked surface, not composition-matrix selectors.
                    if (vertex.Color[k] < 0f || vertex.Color[k] > 1f)
                    { error = $"Road triangle {i} has colors outside the native UNorm8 range."; return false; }
                    colors[index * 4 + k] = (byte)Mathf.RoundToInt(vertex.Color[k] * 255f);
                }
                bones[index] = triangle.Bone;
                slots[geometry.Material].Add(index);
                if (!initialized[geometry.Material])
                { bounds[geometry.Material] = new Bounds(vertex.Position, Vector3.zero); initialized[geometry.Material] = true; }
                else bounds[geometry.Material].Encapsulate(vertex.Position);
            }
        }

        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> indices = default;
        try
        {
            attributes.Add(Attribute(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, positions));
            attributes.Add(Attribute(VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 2, normals));
            attributes.Add(Attribute(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 1, tangents));
            // Packed atlas coordinates can differ by less than one half-float ULP.
            // Quantizing them collapses thin, otherwise valid triangles in UV space.
            // Float32x2 is accepted by native MeshUV0.Unpack as well as object rendering.
            // Keep all four native components for lane formats that carry extra data.
            attributes.Add(planarUV
                ? Attribute(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, surfaceUV)
                : Attribute(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4, uv));
            attributes.Add(Attribute(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, colors));
            attributes.Add(Attribute(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, 1, bones));
            var combined = new List<int>(count); var submeshes = new SubMeshDescriptor[materialCount];
            for (var i = 0; i < materialCount; i++)
            {
                submeshes[i] = new SubMeshDescriptor(combined.Count, slots[i].Count, MeshTopology.Triangles)
                { bounds = bounds[i], firstVertex = 0, vertexCount = count, baseVertex = 0 };
                combined.AddRange(slots[i]);
            }
            indices = new NativeArray<int>(combined.ToArray(), Allocator.Persistent);
            // ProceduralAnimationProperties, saved on the render prefab, owns the native skeleton.
            // These per-vertex indices reference that component's bone array, not connection IDs.
            model = new ModelImporter.Model(name, Matrix4x4.identity, count, indices,
                attributes.ToArray(), submeshes, -1, Array.Empty<ModelImporter.Model.BoneInfo>());
            return true;
        }
        catch (Exception exception)
        {
            if (indices.IsCreated) indices.Dispose();
            foreach (var attribute in attributes) attribute.Dispose();
            error = "Moving road model encoding failed: " + exception.Message;
            return false;
        }
    }

    private static ModelImporter.Model.VertexData Attribute(VertexAttribute attribute,
        VertexAttributeFormat format, int dimension, Array values)
    {
        var bytes = new byte[Buffer.ByteLength(values)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return new ModelImporter.Model.VertexData(attribute, format, dimension,
            new NativeArray<byte>(bytes, Allocator.Persistent), false);
    }
}
