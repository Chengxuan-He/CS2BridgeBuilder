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
    // native skeleton and local render bounds. Never replace that input with the prototype road.
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string owner, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, Draw01UndersideLayout underside)
    {
        if (roadLevels.Count != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[0])
            throw new ArgumentException("Draw01 mechanism requires all three prepared road LODs.");
        var expected = new[]
        {
            "DrawBridge01 Mesh",
            "DrawBridge01_LOD1 Mesh",
            "DrawBridge01_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[1]; lod < roadLevels.Count; lod++)
        {
            var level = roadLevels[lod];
            if (!level.HasRoad || level.Partition == null || level.Structure != null)
                throw new ArgumentException($"Draw01 mechanism LOD {lod} requires road input before structural assembly.");
            var item = level.Source;
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(item.asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 mechanism LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var meshCounts = new int[roadLevels.Count];
        // The far source has no independent road slab after replacement. Restore
        // both leaves from the measured middle-LOD subset, with its original raw
        // channels/material/bones, through the existing compact supplement path.
        var completedRoadLevels = new List<MovableRoadAssets.Level>(roadLevels);
        Mesh[]? undersideMeshes = null;
        try
        {
            var source = roadLevels[1].Source;
            undersideMeshes = PrivateGeometryReader.Read(source);
            var mesh = undersideMeshes[0];
            var positions = mesh.vertices;
            var indices = mesh.GetIndices(0);
            if (indices.Length != Draw01MovingRoadData.SourceTriangleCount(1) * 3)
                throw new InvalidOperationException("Draw01 underside source topology changed.");
            var mapped = (Vector3[])positions.Clone();
            var removed = new bool[indices.Length / 3];
            Array.Fill(removed, true);
            Draw01UndersideData.Apply(1, underside, mapped, removed);
            var supplement = new MovableRoadAssets.Supplement(source, 0,
                new MovableStructureModel.Plan(positions, mapped, new[] { indices }, new[] { removed }, uvCorrections: Draw01UndersideUv.ForLod(1)));
            var far = roadLevels[2];
            completedRoadLevels[2] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces,
                far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton, far.Lanes,
                far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter, far.RoadPartitions,
                far.LanePartitions, far.RoadTrianglePartitions, supplement);
        }
        finally { PrivateGeometryReader.Release(undersideMeshes); }
        for (int lod = 0; lod < meshCounts.Length; lod++)
            meshCounts[lod] = lod == Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[4] ? Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[5] : Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[6];
        return MovableRenderAssembly.CreateMechanism(factory, name, completedRoadLevels, meshCounts,
            (lod, mesh, source, indices) =>
            {
                if (mesh == Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[8])
                    return Draw01RoadReplacement.CreateMainMeshPlan(lod, source, halfWidthDelta,
                        new[] { indices }, underside);
                if (indices.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[16] * Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[17])
                    throw new InvalidOperationException($"Draw01 mechanism LOD {lod} glass requires its 64 native triangles.");
                var positions = (Vector3[])source.Clone();
                Draw01FixedWideningData.Apply(lod, Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[18], halfWidthDelta, positions);
                return new MovableStructureModel.Plan(source, positions, new[] { indices },
                    new[] { new bool[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[20]] });
            });
    }

    // Native DrawBridge01's stacked middle render, not its ordinary network pillar.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[21])
            throw new InvalidOperationException("Draw01 column source requires its two native LOD renders.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[22]],
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[23]]
        };
        var expected = new[]
        {
            "DrawBridge01Pillar Mesh",
            "DrawBridge01Pillar_LOD1 Mesh",
            "DrawBridge01Pillar_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Preflight every source before opening any geometry. Never borrow another bridge's copy.
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[24]; lod < sources.Length; lod++)
        {
            var item = sources[lod];
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(item.asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 column LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[25]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[26] || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[27]] == null || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[28]].subMeshCount != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[29])
                    throw new InvalidOperationException($"Draw01 column LOD {lod} requires its one native mesh and surface slot.");
                var mesh = meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[30]];
                var plan = Draw01ColumnWideningData.CreatePlan(lod, mesh.vertices, halfWidthDelta, new[] { mesh.GetIndices(Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[31]) });
                // Preserve the authored vertical stacking reach and depth; only X changes.
                var bounds = original.bounds;
                var widened = new Bounds3(new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z), new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        // The factory collects all three renders for the ordinary owned-prefab writer.
        return factory.CreateMovableRenderGraph(name, levels);
    }

    // Footing conversion retains the native far topology while its merged-part defect is deferred.
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[32])
            throw new InvalidOperationException("Draw01 footing source requires its two native LOD renders.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[33]],
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[34]]
        };
        var expected = new[]
        {
            "DrawBridge01PillarBase Mesh",
            "DrawBridge01PillarBase_LOD1 Mesh",
            "DrawBridge01PillarBase_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Preflight every source before opening any geometry. Never borrow another bridge's copy.
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[35]; lod < sources.Length; lod++)
        {
            var item = sources[lod];
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(item.asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 footing LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[36]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[37] || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[38]] == null || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[39]].subMeshCount != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[40])
                    throw new InvalidOperationException($"Draw01 footing LOD {lod} requires its one native mesh and surface slot.");
                var mesh = meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[41]];
                var plan = Draw01FootingWideningData.CreatePlan(lod, mesh.vertices, mesh.GetIndices(Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[42]), halfWidthDelta);
                // Preserve the authored vertical stacking reach and depth; only X changes.
                var bounds = original.bounds;
                var widened = new Bounds3(new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z), new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        // The factory collects all three renders for the ordinary owned-prefab writer.
        return factory.CreateMovableRenderGraph(name, levels);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01RenderAssembly_cs
internal static class Numeric_Types_Draw01_Draw01RenderAssembly_cs
{
    static Numeric_Types_Draw01_Draw01RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01RenderAssembly_cs");
}
