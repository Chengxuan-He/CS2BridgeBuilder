using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift04RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "Lift04");
        var appearance = Lift04RoadAppearance.Regions(sample.RoadToObject);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, Lift04RoadPartitions.Carriageway, Lift04RoadPartitions.Carriageway, appearance, appearance, Lift04RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_Lift04_Lift04RoadPreparation_cs.Values.I32[0], layout);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04RoadPreparation_cs
internal static class Numeric_Types_Lift04_Lift04RoadPreparation_cs
{
    static Numeric_Types_Lift04_Lift04RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04RoadPreparation_cs");
}
