using System;
using System.Collections.Generic;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
        => GenerationWork.Synchronous(()=>CreateMechanismAsync(factory,name,roadLevels,halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateMechanismAsync(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[]
        {
            "LiftBridge02 Mesh",
            "LiftBridge02_LOD1 Mesh",
            "LiftBridge02_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[0]; lod < expected.Length; lod++)
        {
            var source = roadLevels[lod].Source;
        }

        return await MovableRenderAssembly.CreateMechanismAsync(factory, name, roadLevels, new[] { Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[1], Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[2], Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[3] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values.I32[4] ? Lift02Glass.Create(lod, source, indices, halfWidthDelta) : Lift02StructurePlan.Create(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RenderAssembly_cs
internal static class Numeric_Types_Lift02_Lift02RenderAssembly_cs
{
    static Numeric_Types_Lift02_Lift02RenderAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02RenderAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02RenderAssembly_cs"));
}
