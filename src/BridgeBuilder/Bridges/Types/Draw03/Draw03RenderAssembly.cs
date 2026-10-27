using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Draw03RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta, MovableSelectedRoadLayout layout)
    {
        if (roadLevels.Count != Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[0])
            throw new ArgumentException("Draw03 mechanism requires all three native road LODs.");
        var expected = new[]
        {
            "DrawBridge03 Mesh",
            "DrawBridge03_LOD1 Mesh",
            "DrawBridge03_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[1]; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null || !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Draw03 LOD {lod} requires its reconstructed native source.");
        }

        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[2], Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[3], Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[4] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[5] ? Draw03Glass.Create(lod, source, indices, halfWidthDelta) : lod == Numeric_Types_Draw03_Draw03RenderAssembly_cs.Values.I32[6] ? Draw03FarStructure.Create(source, indices, halfWidthDelta) : Draw03JointLayout.Apply(lod, Draw03DetailedStructure.Create(lod, source, indices, halfWidthDelta), layout, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RenderAssembly_cs
internal static class Numeric_Types_Draw03_Draw03RenderAssembly_cs
{
    static Numeric_Types_Draw03_Draw03RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RenderAssembly_cs");
}
