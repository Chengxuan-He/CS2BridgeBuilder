using System;
using System.Collections.Generic;
using Colossal.AssetPipeline.Importers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Projection frames are immutable vertex attributes. Only the mechanism's native
// current bone range moves them; there is no child transform or historical sample.
internal static class MovableBoneDecalModel
{
    internal static bool TryBuild(string name, IReadOnlyList<MovableRigidDecalVolume.Part> parts,
        int boneCount, out ModelImporter.Model? model, out string? error)
    {
        model = null; error = null;
        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> nativeIndices = default;
        bool transferred = false;
        try
        {
            int count = 0;
            foreach (var part in parts) count = checked(count + part.Triangles.Length * 3);
            if (count == 0) { error = "Bone decal model has no projection volume."; return false; }
            var positions = new float[count * 3]; var tangent = new float[count * 4];
            var axes = new[] { new float[count * 4], new float[count * 4], new float[count * 4] };
            var areas = new float[count * 4]; var bones = new uint[count]; var indices = new int[count];
            Bounds bounds = default; int vertex = 0;
            foreach (var part in parts)
            {
                if (part.Bone < 0 || part.Bone >= boneCount)
                { error = "Bone decal references a missing mechanism bone."; return false; }
                var x = part.Rotation * (Vector3.right * part.Size.x);
                var y = part.Rotation * (Vector3.up * part.Size.y);
                var z = part.Rotation * (Vector3.forward * part.Size.z);
                foreach (var triangle in part.Triangles)
                {
                    Write(triangle.Geometry.A.Position); Write(triangle.Geometry.B.Position); Write(triangle.Geometry.C.Position);
                }
                void Write(Vector3 local)
                {
                    var point = part.Center + part.Rotation * local;
                    positions[vertex * 3] = point.x; positions[vertex * 3 + 1] = point.y; positions[vertex * 3 + 2] = point.z;
                    tangent[vertex * 4] = 1; tangent[vertex * 4 + 3] = 1;
                    for (int component = 0; component < 3; component++)
                    {
                        axes[0][vertex * 4 + component] = x[component];
                        axes[1][vertex * 4 + component] = y[component];
                        axes[2][vertex * 4 + component] = z[component];
                        axes[component][vertex * 4 + 3] = part.Center[component];
                    }
                    for (int component = 0; component < 4; component++) areas[vertex * 4 + component] = part.Area[component];
                    bones[vertex] = (uint)part.Bone; indices[vertex] = vertex;
                    if (vertex == 0) bounds = new Bounds(point, Vector3.zero); else bounds.Encapsulate(point);
                    vertex++;
                }
            }
            Add(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, positions);
            Add(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, tangent);
            Add(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4, axes[0]);
            Add(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4, axes[1]);
            Add(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4, axes[2]);
            Add(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4, areas);
            Add(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, 1, bones);
            nativeIndices = new NativeArray<int>(indices, Allocator.Persistent);
            model = new ModelImporter.Model(name, Matrix4x4.identity, count, nativeIndices, attributes.ToArray(),
                new[] { new SubMeshDescriptor(0, count, MeshTopology.Triangles) { bounds = bounds, vertexCount = count } },
                -1, Array.Empty<ModelImporter.Model.BoneInfo>());
            transferred = true; return true;
            void Add(VertexAttribute attribute, VertexAttributeFormat format, int dimension, Array values)
            {
                var bytes = new byte[Buffer.ByteLength(values)]; Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
                attributes.Add(new ModelImporter.Model.VertexData(attribute, format, dimension,
                    new NativeArray<byte>(bytes, Allocator.Persistent), false));
            }
        }
        catch (Exception exception) { error = "Bone decal model: " + exception; return false; }
        finally
        {
            if (!transferred)
            {
                if (nativeIndices.IsCreated) nativeIndices.Dispose();
                foreach (var attribute in attributes) attribute.Dispose();
            }
        }
    }
}
