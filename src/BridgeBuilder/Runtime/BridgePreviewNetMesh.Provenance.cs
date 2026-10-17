using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Runtime;

internal sealed partial class BridgePreviewNetMesh
{
    internal readonly struct SourceFace
    {
        internal readonly int Piece, Mesh, Submesh, Triangle;
        internal SourceFace(int piece, int mesh, int submesh, int triangle)
        { Piece = piece; Mesh = mesh; Submesh = submesh; Triangle = triangle; }
    }

    // A separate native pass carries source identity through native half-length filtering,
    // winding and material regrouping. Never split material groups: the native generator
    // computes UV repetition from all triangles in each original material group.
    // UV.x is copied directly into the generated half4; UV.y controls native repetition.
    // The original pass remains the sole source of rendered geometry and appearance.
    private SourceFace[] TraceSources(EntityManager manager, List<NetCompositionPiece> input,
        List<(Entity Entity, int Piece, int Mesh)> origins, NetCompositionMeshData data, Mesh original)
    {
        var identities = new List<SourceFace>();
        foreach (var origin in origins)
        {
            using var vertices = manager.GetBuffer<MeshVertex>(origin.Entity).ToNativeArray(Allocator.Temp);
            using var normals = manager.GetBuffer<MeshNormal>(origin.Entity).ToNativeArray(Allocator.Temp);
            using var tangents = manager.GetBuffer<MeshTangent>(origin.Entity).ToNativeArray(Allocator.Temp);
            using var uvs = manager.GetBuffer<MeshUV0>(origin.Entity).ToNativeArray(Allocator.Temp);
            using var indices = manager.GetBuffer<MeshIndex>(origin.Entity).ToNativeArray(Allocator.Temp);
            using var materials = manager.GetBuffer<MeshMaterial>(origin.Entity).ToNativeArray(Allocator.Temp);
            var outVertices = manager.GetBuffer<MeshVertex>(origin.Entity); outVertices.Clear();
            var outNormals = manager.GetBuffer<MeshNormal>(origin.Entity); outNormals.Clear();
            var outTangents = manager.GetBuffer<MeshTangent>(origin.Entity); outTangents.Clear();
            var outUVs = manager.GetBuffer<MeshUV0>(origin.Entity); outUVs.Clear();
            var outIndices = manager.GetBuffer<MeshIndex>(origin.Entity); outIndices.Clear();
            var outMaterials = manager.GetBuffer<MeshMaterial>(origin.Entity); outMaterials.Clear();
            for (var slot = 0; slot < materials.Length; slot++)
            {
                var material = materials[slot];
                if (material.m_IndexCount % 3 != 0)
                    throw new InvalidOperationException("Native source provenance requires triangle topology.");
                var start = outVertices.Length;
                for (var index = 0; index < material.m_IndexCount; index += 3)
                {
                    var identity = identities.Count;
                    if (identity >= 1 << 30)
                        throw new InvalidOperationException("Native source provenance exceeds its exact encoding range.");
                    identities.Add(new SourceFace(origin.Piece, origin.Mesh, slot, index / 3));
                    // Three exact half-float digit bands preserve 30 bits, independently
                    // of native winding changes. No source-position threshold is reproduced.
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var source = indices[material.m_StartIndex + index + corner].m_Index;
                        outVertices.Add(vertices[source]); outNormals.Add(normals[source]);
                        outTangents.Add(tangents[source]);
                        var uv = uvs[source];
                        uv.m_Uv.x = corner == 0 ? identity & 1023 : corner == 1
                            ? 1024 + ((identity >> 10) & 1023) : 2048 + 2 * (identity >> 20);
                        outUVs.Add(uv);
                        outIndices.Add(new MeshIndex { m_Index = outIndices.Length });
                    }
                }
                outMaterials.Add(new MeshMaterial(start, material.m_IndexCount, start,
                    material.m_IndexCount, material.m_MaterialIndex));
            }
        }

        var target = manager.CreateEntity();
        Mesh? trace = null;
        var pending = Mesh.AllocateWritableMeshData(1);
        var consumed = false;
        try
        {
            var pieces = manager.AddBuffer<NetCompositionPiece>(target);
            foreach (var piece in input) pieces.Add(piece);
            manager.AddBuffer<MeshMaterial>(target);
            if (!_world.GetOrCreateSystemManaged<MeshSystem>().Generate(data, target, pending[0]))
                throw new InvalidOperationException("Native source-provenance mesh generation failed.");
            trace = new Mesh { name = "tmp_native_bridge_provenance", hideFlags = HideFlags.HideAndDontSave };
            Mesh.ApplyAndDisposeWritableMeshData(pending, trace, MeshUpdateFlags.DontRecalculateBounds);
            consumed = true;
            if (trace.subMeshCount != original.subMeshCount)
                throw new InvalidOperationException("Native provenance changed the material grouping.");
            var traceUVs = new List<Vector4>(); trace.GetUVs(0, traceUVs);
            var originalUVs = new List<Vector4>(); original.GetUVs(0, originalUVs);
            var tracePositions = trace.vertices; var originalPositions = original.vertices;
            var result = new List<SourceFace>();
            for (var slot = 0; slot < trace.subMeshCount; slot++)
            {
                var traced = trace.GetIndices(slot); var rendered = original.GetIndices(slot);
                if (traced.Length != rendered.Length)
                    throw new InvalidOperationException("Native provenance changed the emitted triangle count.");
                for (var triangle = 0; triangle < traced.Length; triangle += 3)
                {
                    var identity = 0; var mask = 0;
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var t = traced[triangle + corner]; var o = rendered[triangle + corner];
                        var a = traceUVs[t]; var b = originalUVs[o];
                        if (!tracePositions[t].Equals(originalPositions[o]) || a.y != b.y || a.z != b.z || a.w != b.w)
                            throw new InvalidOperationException("Native provenance changed emitted positions or UV repetition.");
                        var value = (int)a.x;
                        if (a.x != value || value < 0 || value > 4094)
                            throw new InvalidOperationException("Native provenance lost an exact source digit.");
                        var band = value < 1024 ? 0 : value < 2048 ? 1 : 2;
                        if ((mask & (1 << band)) != 0 || (band == 2 && (value & 1) != 0))
                            throw new InvalidOperationException("Native provenance has an ambiguous source digit.");
                        mask |= 1 << band;
                        identity |= band == 0 ? value : band == 1 ? (value - 1024) << 10 : ((value - 2048) / 2) << 20;
                    }
                    if (mask != 7 || (uint)identity >= (uint)identities.Count)
                        throw new InvalidOperationException("Native provenance refers to an unknown source face.");
                    result.Add(identities[identity]);
                }
            }
            return result.ToArray();
        }
        finally
        {
            if (!consumed) pending.Dispose();
            if (trace != null) UnityEngine.Object.DestroyImmediate(trace);
            manager.DestroyEntity(target);
        }
    }
}
