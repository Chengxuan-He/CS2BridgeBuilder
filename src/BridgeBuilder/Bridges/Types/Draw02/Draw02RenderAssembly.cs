using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Draw02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, MovableSelectedRoadLayout layout)
    {
        if (roadLevels.Count != Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[0])
            throw new ArgumentException("Draw02 mechanism requires all three native road LODs.");
        var expected = new[]
        {
            "DrawBridge02 Mesh",
            "DrawBridge02_LOD1 Mesh",
            "DrawBridge02_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[1]; lod < roadLevels.Count; lod++)
        {
            // Draw02SourceRenders reconstructs private inputs from recorded built-in CIDs.
            // Reject saved/generated source Prefabs even if their names match.
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null || !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw02 LOD {lod} requires its reconstructed native source.");
        }

        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[2], Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[3], Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[4] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[5] ? Draw02FixedSidewalks.Apply(lod, Draw02FixedCurbs.Remove(lod, Draw02MovingShell.Create(lod, source, source, indices, halfWidthDelta)), layout) : Draw02RigidParts.Glass(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02RenderAssembly_cs
internal static class Numeric_Types_Draw02_Draw02RenderAssembly_cs
{
    static Numeric_Types_Draw02_Draw02RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02RenderAssembly_cs");
}
