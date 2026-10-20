using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift05RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample,
        RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject);
        var appearance = Lift05RoadAppearance.Regions(sample.RoadToObject);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material,
            Lift05RoadPartitions.Carriageway, Lift05RoadPartitions.Carriageway,
            appearance, appearance, Lift05RoadAppearance.Patches(sample.RoadToObject), 3, layout);
    }
}
