using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift02RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample,
        RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject);
        var appearance = Lift02RoadAppearance.Regions(sample.RoadToObject);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material,
            Lift02RoadPartitions.Carriageway, Lift02RoadPartitions.Carriageway,
            appearance, appearance, Lift02RoadAppearance.Patches(sample.RoadToObject), 5, layout);
    }
}

