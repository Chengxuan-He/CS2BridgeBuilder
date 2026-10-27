using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift05RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "Lift05");
        var appearance = Lift05RoadAppearance.Regions(sample.RoadToObject);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, Lift05RoadPartitions.Carriageway, Lift05RoadPartitions.Carriageway, appearance, appearance, Lift05RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_Lift05_Lift05RoadPreparation_cs.Values.I32[0], layout);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05RoadPreparation_cs
internal static class Numeric_Types_Lift05_Lift05RoadPreparation_cs
{
    static Numeric_Types_Lift05_Lift05RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05RoadPreparation_cs");
}
