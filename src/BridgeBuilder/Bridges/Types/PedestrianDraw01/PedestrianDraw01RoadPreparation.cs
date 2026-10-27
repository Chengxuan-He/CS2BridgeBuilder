using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw01RoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var physical = PedestrianDraw01RoadPartitions.Carriageway;
        var appearance = PedestrianDraw01RoadAppearance.Regions(physical, sample.RoadToObject);
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "PedestrianDraw01");
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, physical, physical, appearance, appearance, PedestrianDraw01RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs.Values.I32[0], layout);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs");
}
