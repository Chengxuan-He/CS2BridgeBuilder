using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift04RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
        => GenerationWork.Synchronous(()=>CreateMechanismAsync(factory,name,roadLevels,halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateMechanismAsync(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[]
        {
            "LiftBridge04 Mesh",
            "LiftBridge04_LOD1 Mesh",
            "LiftBridge04_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values.I32[0]; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
        }

        return await MovableRenderAssembly.CreateMechanismAsync(factory, name, roadLevels, new[] { Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values.I32[1], Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values.I32[2], Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values.I32[3] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values.I32[4] ? Lift04Glass.Create(lod, source, indices, halfWidthDelta) : Lift04StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04RenderAssembly_cs
internal static class Numeric_Types_Lift04_Lift04RenderAssembly_cs
{
    static Numeric_Types_Lift04_Lift04RenderAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift04_Lift04RenderAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift04_Lift04RenderAssembly_cs"));
}
