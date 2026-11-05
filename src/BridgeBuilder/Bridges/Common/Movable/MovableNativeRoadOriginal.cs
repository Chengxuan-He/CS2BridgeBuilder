using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Original native road LODs bypass all road/structure mapping. Native draw batches
// use one model per Surface; split only the existing submesh index streams.
internal static class MovableNativeRoadOriginal
{
    internal static void Append(string name, RenderPrefab source,
        List<ModelImporter.Model> owned, List<ModelImporter.Model> batch, List<SurfaceAsset> surfaces)
    {
        Mesh[]? meshes = null;
        try
        {
            meshes = PrivateGeometryReader.Read(source);
            int slot = 0;
            foreach (var mesh in meshes)
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    var surface = source.GetSurfaceAsset(slot++);
                    if (surface == null) throw new InvalidOperationException("Original native road has a missing Surface.");
                    var model = Copy(name + " Original " + (slot - 1), mesh, submesh);
                    owned.Add(model); batch.Add(model); surfaces.Add(surface);
                }
            if (slot == 0 || source.surfaceAssets == null || slot != source.surfaceAssets.Count())
                throw new InvalidOperationException("Original native road has inconsistent material slots.");
        }
        finally { PrivateGeometryReader.Release(meshes); }
    }

    internal static ModelImporter.Model Copy(string name, Mesh source, int submesh)
    {
        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> indices = default;
        bool transferred = false;
        try
        {
            using var read = Mesh.AcquireReadOnlyMeshData(source);
            foreach (var attribute in source.GetVertexAttributes())
            {
                int size = Size(attribute.format) * attribute.dimension;
                if (size == 0) throw new InvalidOperationException("Unsupported original native attribute format.");
                int stride = source.GetVertexBufferStride(attribute.stream);
                int offset = source.GetVertexAttributeOffset(attribute.attribute);
                var raw = read[0].GetVertexData<byte>(attribute.stream);
                var bytes = new byte[checked(source.vertexCount * size)];
                for (int vertex = 0; vertex < source.vertexCount; vertex++)
                    NativeArray<byte>.Copy(raw, vertex * stride + offset, bytes, vertex * size, size);
                attributes.Add(new ModelImporter.Model.VertexData(attribute.attribute, attribute.format,
                    attribute.dimension, new NativeArray<byte>(bytes, Allocator.Persistent), false));
            }
            // GetIndices applies the original baseVertex; do not apply it twice.
            indices = new NativeArray<int>(source.GetIndices(submesh), Allocator.Persistent);
            var descriptor = source.GetSubMesh(submesh);
            descriptor.indexStart = 0; descriptor.baseVertex = 0;
            var model = new ModelImporter.Model(name, Matrix4x4.identity, source.vertexCount, indices,
                attributes.ToArray(), new[] { descriptor }, -1, Array.Empty<ModelImporter.Model.BoneInfo>());
            transferred = true;
            return model;
        }
        finally
        {
            if (!transferred)
            {
                if (indices.IsCreated) indices.Dispose();
                foreach (var attribute in attributes) attribute.Dispose();
            }
        }
    }

    private static int Size(VertexAttributeFormat format) => format switch
    {
        VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
        VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16 or
            VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
        VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8 or
            VertexAttributeFormat.SInt8 => 1,
        _ => 0
    };
}
