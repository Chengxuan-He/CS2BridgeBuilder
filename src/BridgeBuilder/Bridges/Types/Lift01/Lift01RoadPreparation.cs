using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01RoadPreparation
{
    internal static MovableRoadAssets.Level[]? PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material) =>
        GenerationWork.Synchronous(() => PrepareRenderLevelsAsync(sample, nativeLevels, halfWidthDelta, closedWorld, material));

    internal static async System.Threading.Tasks.Task<MovableRoadAssets.Level[]?> PrepareRenderLevelsAsync(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "Lift01");
        var appearance = Lift01RoadAppearance.Regions(sample.RoadToObject);
        // Lift01 translates the whole selected deck on TrainTracks (bone 4).
        // Pedestrian occurrences do not belong to a separately rotating leaf.
        // Fixed ends and the two physical gaps remain identical for all roles.
        return await MovableRoadPreparation.PrepareAsync(sample, nativeLevels, halfWidthDelta, closedWorld, material, Lift01RoadPartitions.Carriageway, Lift01RoadPartitions.Carriageway, appearance, appearance, Lift01RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_Lift01_Lift01RoadPreparation_cs.Values.I32[0], layout,
            lod => lod < 2
                ? Lift01HalfFloorAppearance.Create(lod, nativeLevels[lod].GetSurfaceAsset(0), sample.RoadToObject, halfWidthDelta)
                : Lift01FarAppearance.Create(nativeLevels[lod].GetSurfaceAsset(0), sample.RoadToObject, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01RoadPreparation_cs
internal static class Numeric_Types_Lift01_Lift01RoadPreparation_cs
{
    static Numeric_Types_Lift01_Lift01RoadPreparation_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01RoadPreparation_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01RoadPreparation_cs"));
}
