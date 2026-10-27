using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[]
        {
            "LiftBridge02 Mesh",
            "LiftBridge02_LOD1 Mesh",
            "LiftBridge02_LOD2 Mesh"
        };
        if (roadLevels.Count != expected.Length)
            throw new ArgumentException("Lift02 mechanism requires all three native road LODs.");
        for (var lod = Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[0]; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null || !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift02 LOD {lod} requires its reconstructed native source.");
        }

        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[1], Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[2], Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[3] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[4] ? Lift02Glass.Create(lod, source, indices, halfWidthDelta) : Lift02StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RenderAssembly_cs
internal static class Numeric_Types_Lift02_Lift02RenderAssembly_cs
{
    static Numeric_Types_Lift02_Lift02RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02RenderAssembly_cs");
}
