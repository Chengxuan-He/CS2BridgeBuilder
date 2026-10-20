using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift03RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> mainRoadLevels,
        IReadOnlyList<MovableRoadAssets.Level> auxiliaryRoadLevels, float structureDelta,
        float upperDelta, float lowerDelta)
    {
        IReadOnlyList<MovableRoadAssets.Level> roadLevels =
            Lift03RoadLevels.Combine(mainRoadLevels, auxiliaryRoadLevels);
        if (roadLevels.Count != 3)
            throw new ArgumentException("Lift03 mechanism requires all three prepared road LODs.");
        var expected = new[] { "LiftBridge03 Mesh", "LiftBridge03_LOD1 Mesh", "LiftBridge03_LOD2 Mesh" };
        for (var lod = 0; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift03 LOD {lod} requires its reconstructed native source.");
        }
        var levels = new[] { roadLevels[0], roadLevels[1], roadLevels[2] };
        var far = levels[2];
        if (far.ExtraStructure != null)
            throw new InvalidOperationException("Lift03 far support supplement has already been attached.");
        Mesh[]? meshes = null;
        try
        {
            meshes = PrivateGeometryReader.Read(levels[1].Source);
            if (meshes.Length != 2 || meshes[0].subMeshCount != 1)
                throw new InvalidOperationException("Lift03 support supplement requires its recorded middle main surface.");
            var supportPlan = Lift03FarSupports.CreateSupplement(meshes[0].vertices,
                meshes[0].GetIndices(0), structureDelta, upperDelta, lowerDelta);
            var extra = new MovableRoadAssets.Supplement(levels[1].Source, 0, supportPlan);
            levels[2] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces,
                far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton,
                far.Lanes, far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter,
                far.RoadPartitions, far.LanePartitions, far.RoadTrianglePartitions, extra);
        }
        finally { PrivateGeometryReader.Release(meshes); }

        return MovableRenderAssembly.CreateMechanism(factory, name, levels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 1
                ? Lift03Glass.Create(lod, source, indices, structureDelta)
                : lod == 2
                    ? Lift03FarStructure.Create(source, indices, structureDelta)
                    : Lift03DetailedStructure.Create(lod, source, indices, structureDelta, upperDelta, lowerDelta));
    }
}
