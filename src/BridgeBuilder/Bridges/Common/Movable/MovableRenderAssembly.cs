using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Combines prepared selected-road levels with style-specific, recorded structure plans.
// The caller owns source provenance and supplies the measured mesh count at each LOD.
internal static class MovableRenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, IReadOnlyList<int> meshCounts,
        Func<int, int, Vector3[], int[], MovableStructureModel.Plan> buildStructure)
    {
        if (roadLevels.Count != meshCounts.Count || roadLevels.Count == 0)
            throw new ArgumentException("Moving render assembly requires every measured road LOD.");
        var levels = new MovableRoadAssets.Level[roadLevels.Count];
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var vertices = new Vector3[roadLevels.Count][][];
        var indices = new int[roadLevels.Count][][];
        for (var lod = 0; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var input = roadLevels[lod];
                if (!input.HasRoad || input.Partition == null || input.Structure != null)
                    throw new ArgumentException($"Moving render LOD {lod} requires prepared road input without structure.");
                meshes = PrivateGeometryReader.Read(input.Source);
                if (meshes.Length != meshCounts[lod])
                    throw new InvalidOperationException($"Moving render LOD {lod} has a different native mesh count.");
                vertices[lod] = new Vector3[meshes.Length][];
                indices[lod] = new int[meshes.Length][];
                for (var mesh = 0; mesh < meshes.Length; mesh++)
                {
                    var source = meshes[mesh];
                    if (source == null || source.subMeshCount != 1)
                        throw new InvalidOperationException($"Moving render LOD {lod} mesh {mesh} has a different native surface layout.");
                    vertices[lod][mesh] = source.vertices;
                    indices[lod][mesh] = source.GetIndices(0);
                }
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        // Engine mesh access above stays on the main thread. Workers consume only
        // detached numerical arrays; nested vertex loops reuse the worker thread.
        var completedPlans = new MovableStructureModel.Plan[levels.Length][];
        var completedBounds = new Colossal.Mathematics.Bounds3[levels.Length];
        var snapshotMs = timer.ElapsedMilliseconds;
        AffineGeometry.Groups(levels.Length, lod =>
        {
            var input = roadLevels[lod];
            var plans = new MovableStructureModel.Plan[vertices[lod].Length];
            var bounds = input.RenderBounds;
            for (var mesh = 0; mesh < plans.Length; mesh++)
            {
                var plan = buildStructure(lod, mesh, vertices[lod][mesh], indices[lod][mesh]);
                plans[mesh] = plan;
                foreach (var position in plan.Positions)
                {
                    bounds.min = math.min(bounds.min, (float3)position);
                    bounds.max = math.max(bounds.max, (float3)position);
                }
            }
            completedPlans[lod] = plans;
            completedBounds[lod] = bounds;
        });
        var planMs = timer.ElapsedMilliseconds - snapshotMs;
        for (int lod = 0; lod < levels.Length; lod++)
        {
            var input = roadLevels[lod];
            levels[lod] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces,
                input.Partition!, completedBounds[lod], input.RoadToObject, completedPlans[lod], input.Skeleton, input.Lanes,
                input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter, input.RoadPartitions,
                input.LanePartitions, input.RoadTrianglePartitions, input.ExtraStructure);
        }
        Mod.Log.Info($"{name}: structural geometry snapshotMs={snapshotMs}; parallelPlanMs={planMs}; levels={levels.Length}; totalMs={timer.ElapsedMilliseconds}.");
        var graphStart = timer.ElapsedMilliseconds;
        var result = factory.CreateMovableRenderGraph(name, levels);
        Mod.Log.Info($"{name}: native render graph assemblyMs={timer.ElapsedMilliseconds - graphStart}; structureAndGraphMs={timer.ElapsedMilliseconds}.");
        return result;
    }
}
