using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Draw02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name,
        IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, MovableSelectedRoadLayout layout)
    {
        if (roadLevels.Count != 3)
            throw new ArgumentException("Draw02 mechanism requires all three native road LODs.");
        var expected = new[] { "DrawBridge02 Mesh", "DrawBridge02_LOD1 Mesh", "DrawBridge02_LOD2 Mesh" };
        for (var lod = 0; lod < roadLevels.Count; lod++)
        {
            // Draw02SourceRenders reconstructs private inputs from recorded built-in CIDs.
            // Reject saved/generated source Prefabs even if their names match.
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null ||
                !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw02 LOD {lod} requires its reconstructed native source.");
        }
        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { 2, 2, 1 },
            (lod, mesh, source, indices) => mesh == 0
                ? Draw02FixedSidewalks.Apply(lod, Draw02FixedCurbs.Remove(lod,
                    Draw02MovingShell.Create(lod, source, source, indices, halfWidthDelta)), layout)
                : Draw02RigidParts.Glass(lod, source, indices, halfWidthDelta));
    }
}
