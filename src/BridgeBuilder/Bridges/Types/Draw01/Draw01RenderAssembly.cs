using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01RenderAssembly
{
    // Road levels already contain the resolved selected-road geometry, pose, partitions,
    // native skeleton and motion bounds. Never replace that input with the prototype road.
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string owner, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        if (roadLevels.Count != 3)
            throw new ArgumentException("Draw01 mechanism requires all three prepared road LODs.");
        var expected = new[] { "DrawBridge01 Mesh", "DrawBridge01_LOD1 Mesh", "DrawBridge01_LOD2 Mesh" };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        for (var lod = 0; lod < roadLevels.Count; lod++)
        {
            var level = roadLevels[lod];
            if (!level.HasRoad || level.Partition == null || level.Structure != null)
                throw new ArgumentException($"Draw01 mechanism LOD {lod} requires road input before structural assembly.");
            var item = level.Source;
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal) ||
                BridgeAssetInfo.IsOtherBridgePrefab(item.asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 mechanism LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var levels = new MovableRoadAssets.Level[3];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var input = roadLevels[lod];
                meshes = PrivateGeometryReader.Read(input.Source);
                var meshCount = lod == 2 ? 1 : 2;
                if (meshes.Length != meshCount)
                    throw new InvalidOperationException($"Draw01 mechanism LOD {lod} requires {meshCount} native meshes.");
                foreach (var mesh in meshes)
                    if (mesh == null || mesh.subMeshCount != 1)
                        throw new InvalidOperationException($"Draw01 mechanism LOD {lod} has an invalid native mesh or surface layout.");
                var plans = new MovableStructureModel.Plan[meshCount];
                plans[0] = Draw01RoadReplacement.CreateMainMeshPlan(lod, meshes[0].vertices,
                    halfWidthDelta, new[] { meshes[0].GetIndices(0) });
                if (meshCount == 2)
                {
                    var source = meshes[1].vertices;
                    var positions = (Vector3[])source.Clone();
                    var indices = meshes[1].GetIndices(0);
                    if (indices.Length != 64 * 3)
                        throw new InvalidOperationException($"Draw01 mechanism LOD {lod} glass requires its 64 native triangles.");
                    Draw01FixedWideningData.Apply(lod, 1, halfWidthDelta, positions);
                    plans[1] = new MovableStructureModel.Plan(source, positions,
                        new[] { indices }, new[] { new bool[64] });
                }
                levels[lod] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces,
                    input.Partition!, input.MotionBounds, input.RoadToObject, plans, input.Skeleton,
                    input.Lanes, input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter,
                    input.RoadPartitions, input.LanePartitions);
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }

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
