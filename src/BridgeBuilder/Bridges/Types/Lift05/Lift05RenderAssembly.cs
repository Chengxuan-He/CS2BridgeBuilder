using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift05RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[]
        {
            "LiftBridge05 Mesh",
            "LiftBridge05_LOD1 Mesh",
            "LiftBridge05_LOD2 Mesh"
        };
        if (roadLevels.Count != expected.Length)
            throw new ArgumentException("Lift05 mechanism requires all three native road LODs.");
        for (var lod = Numeric_Types_Lift05_Lift05RenderAssembly_cs.Values.I32[0]; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null || !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift05 LOD {lod} requires its reconstructed native source.");
        }

        return MovableRenderAssembly.CreateMechanism(factory, name, roadLevels, new[] { Numeric_Types_Lift05_Lift05RenderAssembly_cs.Values.I32[1], Numeric_Types_Lift05_Lift05RenderAssembly_cs.Values.I32[2], Numeric_Types_Lift05_Lift05RenderAssembly_cs.Values.I32[3] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift05_Lift05RenderAssembly_cs.Values.I32[4] ? Lift05Glass.Create(lod, source, indices, halfWidthDelta) : Lift05StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05RenderAssembly_cs
internal static class Numeric_Types_Lift05_Lift05RenderAssembly_cs
{
    static Numeric_Types_Lift05_Lift05RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05RenderAssembly_cs");
}
