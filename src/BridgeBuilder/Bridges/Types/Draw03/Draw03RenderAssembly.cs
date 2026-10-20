using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Draw03RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, MovableSelectedRoadLayout layout)
    {
        if (roadLevels.Count != 3)
            throw new ArgumentException("Draw03 mechanism requires all three native road LODs.");
        var expected = new[] { "DrawBridge03 Mesh", "DrawBridge03_LOD1 Mesh", "DrawBridge03_LOD2 Mesh" };
        for (var lod = 0; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw03 LOD {lod} requires its reconstructed native source.");
        }
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 1
                ? Draw03Glass.Create(lod, source, indices, halfWidthDelta)
                : lod == 2
                    ? Draw03FarStructure.Create(source, indices, halfWidthDelta)
                    : Draw03JointLayout.Apply(lod,
                        Draw03DetailedStructure.Create(lod, source, indices, halfWidthDelta), layout, halfWidthDelta));
    }
}
