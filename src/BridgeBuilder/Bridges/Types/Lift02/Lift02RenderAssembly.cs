using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[] { "LiftBridge02 Mesh", "LiftBridge02_LOD1 Mesh", "LiftBridge02_LOD2 Mesh" };
        if (roadLevels.Count != expected.Length)
            throw new ArgumentException("Lift02 mechanism requires all three native road LODs.");
        for (var lod = 0; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift02 LOD {lod} requires its reconstructed native source.");
        }
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 1
                ? Lift02Glass.Create(lod, source, indices, halfWidthDelta)
                : Lift02StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}

