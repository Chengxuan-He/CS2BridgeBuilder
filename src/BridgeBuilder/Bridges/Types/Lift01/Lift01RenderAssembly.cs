using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
        => GenerationWork.Synchronous(()=>CreateMechanismAsync(factory,name,roadLevels,halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateMechanismAsync(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> roadLevels, float halfWidthDelta)
    {
        var expected = new[]
        {
            "LiftBridge01 Mesh",
            "LiftBridge01_LOD1 Mesh",
            "LiftBridge01_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[1]; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
        }

        var levels = new[]
        {
            roadLevels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[2]],
            roadLevels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[3]],
            roadLevels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[4]]
        };
        var far = levels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[5]];
        Mesh[]? meshes = null;
        try
        {
            meshes = PrivateGeometryReader.Read(levels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[6]].Source);
            var plan = Lift01FarTowerCore.Create(meshes[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[10]].vertices, meshes[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[11]].GetIndices(Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[12]), halfWidthDelta);
            plan = Lift01FarEndLips.Apply(plan, halfWidthDelta);
            // The shared supplemental path compacts referenced vertices and preserves
            // original main Surface/UV/channels. Towers and fixed lips use bone 0;
            // moving lips use road bone 4. Both recorded transforms and motion settings
            // are identical in the middle and far skeletons.
            var extra = new MovableRoadAssets.Supplement(levels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[13]].Source, Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[14], plan);
            levels[Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[15]] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces, far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton, far.Lanes, far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter, far.RoadPartitions, far.LanePartitions, far.RoadTrianglePartitions, extra, far.AdditionalStructures, far.Decals);
        }
        finally
        {
            PrivateGeometryReader.Release(meshes);
        }

        return await MovableRenderAssembly.CreateMechanismAsync(factory, name, levels, new[] { Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[16], Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[17], Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[18] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[19] ? Lift01Glass.Create(lod, source, indices, halfWidthDelta) : lod == Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values.I32[20] ? Lift01FarStructure.Create(source, indices, halfWidthDelta) : Lift01DetailedStructure.Create(lod, source, indices, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01RenderAssembly_cs
internal static class Numeric_Types_Lift01_Lift01RenderAssembly_cs
{
    static Numeric_Types_Lift01_Lift01RenderAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01RenderAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01RenderAssembly_cs"));
}
