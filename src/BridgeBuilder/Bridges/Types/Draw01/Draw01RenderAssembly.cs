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
        => GenerationWork.Synchronous(()=>CreateMechanismAsync(factory,owner,name,roadLevels,halfWidthDelta,underside));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateMechanismAsync(TowerFactory factory, string owner, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, Draw01UndersideLayout underside)
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
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal))
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
            Draw01OuterFrameData.Apply(1, halfWidthDelta, positions, mapped, removed);
            var supplement = new MovableRoadAssets.Supplement(source, 0, new MovableStructureModel.Plan(positions, mapped, new[] { indices }, new[] { removed }, uvCorrections: Draw01UndersideUv.ForLod(1)));
            var far = roadLevels[2];
            completedRoadLevels[2] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces, far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton, far.Lanes, far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter, far.RoadPartitions, far.LanePartitions, far.RoadTrianglePartitions, supplement, far.AdditionalStructures, far.Decals);
        }
        finally
        {
            PrivateGeometryReader.Release(undersideMeshes);
        }

        for (int lod = 0; lod < meshCounts.Length; lod++)
            meshCounts[lod] = lod == Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[2] ? Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[3] : Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[4];
        return await MovableRenderAssembly.CreateMechanismAsync(factory, name, completedRoadLevels, meshCounts, (lod, mesh, source, indices) =>
        {
            if (mesh == Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[5])
                return Draw01RoadReplacement.CreateMainMeshPlan(lod, source, halfWidthDelta, new[] { indices }, underside);
            if (indices.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[6] * Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[7])
                throw new InvalidOperationException($"Draw01 mechanism LOD {lod} glass requires its 64 native triangles.");
            var positions = (Vector3[])source.Clone();
            Draw01FixedWideningData.Apply(lod, Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[8], halfWidthDelta, positions);
            return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[9]] });
        });
    }

    // Native DrawBridge01's stacked middle render, not its ordinary network pillar.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateColumnAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateColumnAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw01RenderAssembly.CreateColumn");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[10])
            throw new InvalidOperationException("Draw01 column source requires its two native LOD renders.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[11]],
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[12]]
        };
        var expected = new[]
        {
            "DrawBridge01Pillar Mesh",
            "DrawBridge01Pillar_LOD1 Mesh",
            "DrawBridge01Pillar_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Check required measured source metadata before opening geometry.
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[13]; lod < sources.Length; lod++)
        {
            var item = sources[lod];
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw01 column LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
        }

        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[14]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                if (meshes.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[15] || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[16]] == null || meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[17]].subMeshCount != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[18])
                    throw new InvalidOperationException($"Draw01 column LOD {lod} requires its one native mesh and surface slot.");
                var mesh = meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[19]];
                var supportInput0 = mesh.vertices;
                var supportInput1 = mesh.GetIndices(Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[20]);
                var plan = await GenerationWork.Background("support structure plan", () => Draw01ColumnWideningData.CreatePlan(lod, supportInput0, halfWidthDelta, new[] { supportInput1 }));
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
        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }

    // Footing conversion retains the native far topology while its merged-part defect is deferred.
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateFootingAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateFootingAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw01RenderAssembly.CreateFooting");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[21])
            throw new InvalidOperationException("Draw01 footing source requires its two native LOD renders.");
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[22]],
            lods[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[23]]
        };
        var expected = new[]
        {
            "DrawBridge01PillarBase Mesh",
            "DrawBridge01PillarBase_LOD1 Mesh",
            "DrawBridge01PillarBase_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Check required measured source metadata before opening geometry.
        for (var lod = Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[24]; lod < sources.Length; lod++)
        {
            var item = sources[lod];
            if (item == null || !string.Equals(item.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw01 footing LOD {lod} is not the permitted recorded source '{expected[lod]}'.");
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
                    throw new InvalidOperationException($"Draw01 footing LOD {lod} requires its one native mesh and surface slot.");
                var mesh = meshes[Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[30]];
                var supportInput0 = mesh.vertices;
                var supportInput1 = mesh.GetIndices(Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values.I32[31]);
                var plan = await GenerationWork.Background("support structure plan", () => Draw01FootingWideningData.CreatePlan(lod, supportInput0, supportInput1, halfWidthDelta));
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
        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01RenderAssembly_cs
internal static class Numeric_Types_Draw01_Draw01RenderAssembly_cs
{
    static Numeric_Types_Draw01_Draw01RenderAssembly_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01RenderAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01RenderAssembly_cs"));
}
