using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample,
        RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject);
        var appearance = Lift01RoadAppearance.Regions(sample.RoadToObject);
        // Lift01 translates the whole selected deck on TrainTracks (bone 4).
        // Pedestrian occurrences do not belong to a separately rotating leaf.
        // Fixed ends and the two physical gaps remain identical for all roles.
        return MovableRoadPreparation.Prepare(sample, nativeLevels,
            halfWidthDelta, closedWorld, material,
            Lift01RoadPartitions.Carriageway, Lift01RoadPartitions.Carriageway,
            appearance, appearance, Lift01RoadAppearance.Patches(sample.RoadToObject), 5, layout);
    }
}
