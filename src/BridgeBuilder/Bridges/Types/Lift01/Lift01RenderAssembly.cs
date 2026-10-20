using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        if (roadLevels.Count != 3)
            throw new ArgumentException("Lift01 mechanism requires all three native road LODs.");
        var expected = new[] { "LiftBridge01 Mesh", "LiftBridge01_LOD1 Mesh", "LiftBridge01_LOD2 Mesh" };
        for (var lod = 0; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift01 LOD {lod} requires its reconstructed native source.");
        }

        var levels = new[] { roadLevels[0], roadLevels[1], roadLevels[2] };
        var far = levels[2];
        if (far.ExtraStructure != null)
            throw new InvalidOperationException("Lift01 far structure has already been attached.");
        Mesh[]? meshes = null;
        try
        {
            meshes = PrivateGeometryReader.Read(levels[1].Source);
            if (meshes.Length != 2 || meshes[0].subMeshCount != 1)
                throw new InvalidOperationException("Lift01 tower supplement requires the recorded middle main surface.");
            var plan = Lift01FarTowerCore.Create(meshes[0].vertices, meshes[0].GetIndices(0), halfWidthDelta);
            plan = Lift01FarEndLips.Apply(plan, halfWidthDelta);
            // The shared supplemental path compacts referenced vertices and preserves
            // original main Surface/UV/channels. Towers and fixed lips use bone 0;
            // moving lips use road bone 4. Both recorded transforms and motion settings
            // are identical in the middle and far skeletons.
            var extra = new MovableRoadAssets.Supplement(levels[1].Source, 0, plan);
            levels[2] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces,
                far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton,
                far.Lanes, far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter,
                far.RoadPartitions, far.LanePartitions, far.RoadTrianglePartitions, extra);
        }
        finally { PrivateGeometryReader.Release(meshes); }

        return MovableRenderAssembly.CreateMechanism(factory, name, levels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 1
                ? Lift01Glass.Create(lod, source, indices, halfWidthDelta)
                : lod == 2
                    ? Lift01FarStructure.Create(source, indices, halfWidthDelta)
                    : Lift01DetailedStructure.Create(lod, source, indices, halfWidthDelta));
    }
}
