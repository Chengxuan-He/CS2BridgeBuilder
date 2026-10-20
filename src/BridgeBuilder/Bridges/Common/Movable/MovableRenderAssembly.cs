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
                var plans = new MovableStructureModel.Plan[meshes.Length];
                var bounds = input.RenderBounds;
                for (var mesh = 0; mesh < meshes.Length; mesh++)
                {
                    var source = meshes[mesh];
                    if (source == null || source.subMeshCount != 1)
                        throw new InvalidOperationException($"Moving render LOD {lod} mesh {mesh} has a different native surface layout.");
                    var plan = buildStructure(lod, mesh, source.vertices, source.GetIndices(0));
                    plans[mesh] = plan;
                    foreach (var position in plan.Positions)
                    {
                        bounds.min = math.min(bounds.min, (float3)position);
                        bounds.max = math.max(bounds.max, (float3)position);
                    }
                }
                levels[lod] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces,
                    input.Partition, bounds, input.RoadToObject, plans, input.Skeleton,
                    input.Lanes, input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter,
                    input.RoadPartitions, input.LanePartitions, input.RoadTrianglePartitions, input.ExtraStructure);
            }
            finally { PrivateGeometryReader.Release(meshes); }
        }
        return factory.CreateMovableRenderGraph(name, levels);
    }
}
