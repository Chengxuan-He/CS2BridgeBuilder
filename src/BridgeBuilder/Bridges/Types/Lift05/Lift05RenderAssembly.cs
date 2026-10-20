using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift05RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[] { "LiftBridge05 Mesh", "LiftBridge05_LOD1 Mesh", "LiftBridge05_LOD2 Mesh" };
        if (roadLevels.Count != expected.Length)
            throw new ArgumentException("Lift05 mechanism requires all three native road LODs.");
        for (var lod = 0; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift05 LOD {lod} requires its reconstructed native source.");
        }
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 1
                ? Lift05Glass.Create(lod, source, indices, halfWidthDelta)
                : Lift05StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}
