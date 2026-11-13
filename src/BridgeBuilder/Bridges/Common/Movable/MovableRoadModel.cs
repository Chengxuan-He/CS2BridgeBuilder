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
        return TryBuildCore(name,triangles,materialCount,boneCount,null,out model,out error);
    }

    internal static bool TryBuildPacked(string name, IReadOnlyList<MovableRoadGeometry.BoundTriangle> triangles,
        int materialCount, int boneCount, uint[] packed, out ModelImporter.Model? model, out string? error)
    {
        return TryBuildCore(name,triangles,materialCount,boneCount,packed,out model,out error);
    }

    private static bool TryBuildCore(string name, IReadOnlyList<MovableRoadGeometry.BoundTriangle> triangles,
        int materialCount, int boneCount, uint[]? packed, out ModelImporter.Model? model, out string? error)
    {
        model = null; error = null;

        var count = triangles.Count * 3;
        var positions = new float[count * 3]; var normals = new uint[count];
        var tangents = new uint[count]; var uv = new float[count * 4];
        var colors = new byte[count * 4]; var bones = new uint[count];
        var slots = new List<int>[materialCount];
        for (var i = 0; i < slots.Length; i++) slots[i] = new List<int>();
        var bounds = new Bounds[materialCount]; var initialized = new bool[materialCount];
        for (var i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i]; var geometry = triangle.Geometry;
            var vertices = new[] { geometry.A, geometry.B, geometry.C };
            for (var j = 0; j < 3; j++)
            {
                var index = i * 3 + j; var vertex = vertices[j];
                positions[index * 3] = vertex.Position.x;
                positions[index * 3 + 1] = vertex.Position.y;
                positions[index * 3 + 2] = vertex.Position.z;
                normals[index] = packed==null ? MathUtils.NormalToOctahedral((float3)vertex.Normal) : packed[index*11+3];
                tangents[index] = packed==null ? MathUtils.TangentToOctahedral((float4)vertex.Tangent) : packed[index*11+4];
                for (var k = 0; k < 4; k++)
                {
                    uv[index * 4 + k] = packed==null ? vertex.UV[k] : BitConverter.Int32BitsToSingle(unchecked((int)packed[index*11+5+k]));
                    // Colors describe a baked surface, not composition-matrix selectors.
                    colors[index * 4 + k] = packed==null ? (byte)Mathf.RoundToInt(Mathf.Clamp01(vertex.Color[k]) * 255f) : (byte)(packed[index*11+9]>>(k*8));
                }
                bones[index] = packed==null ? triangle.Bone : packed[index*11+10];
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
            // Keep Float32 in BOTH native layouts, including lane data in Z/W.
            var uvData=MovableRoadUv.Encode(uv,out int uvDimension);
            attributes.Add(Attribute(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, uvDimension, uvData));
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
