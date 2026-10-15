using System;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01RenderAssembly
{
    // Native DrawBridge01's stacked middle render, not its ordinary network pillar.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name,
        RenderPrefab source, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != 2)
            throw new InvalidOperationException("Draw01 column source requires its two native LOD renders.");
        var sources = new[] { source, lods[0], lods[1] };
        var expected = new[] { "DrawBridge01Pillar Mesh", "DrawBridge01Pillar_LOD1 Mesh", "DrawBridge01Pillar_LOD2 Mesh" };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Preflight every source before opening any geometry. Never borrow another bridge's copy.
        for (var lod = 0; lod < sources.Length; lod++)
        {
            var item = sources[lod];
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal) ||
                BridgeAssetInfo.IsOtherBridgePrefab(item.asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 column LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = 0; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != 1 || meshes[0] == null || meshes[0].subMeshCount != 1)
                    throw new InvalidOperationException($"Draw01 column LOD {lod} requires its one native mesh and surface slot.");
                var mesh = meshes[0];
                var plan = Draw01ColumnWideningData.CreatePlan(lod, mesh.vertices, halfWidthDelta,
                    new[] { mesh.GetIndices(0) });
                // Preserve the authored vertical stacking reach and depth; only X changes.
                var bounds = original.bounds;
                var widened = new Bounds3(
                    new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z),
                    new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        // The factory collects all three renders for the ordinary owned-prefab writer.
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
