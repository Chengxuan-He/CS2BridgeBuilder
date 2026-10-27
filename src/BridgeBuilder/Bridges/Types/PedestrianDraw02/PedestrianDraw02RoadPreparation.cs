using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02RoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var physical = PedestrianDraw02RoadPartitions.Create(halfWidthDelta);
        var appearance = PedestrianDraw02RoadAppearance.Regions(physical, sample.RoadToObject);
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "PedestrianDraw02");
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, physical, physical, appearance, appearance, PedestrianDraw02RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs.Values.I32[0], layout);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs");
}
