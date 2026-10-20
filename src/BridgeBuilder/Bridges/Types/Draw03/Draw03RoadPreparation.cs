using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw03RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample,
        RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, MovableSelectedRoadLayout layout)
        => MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material,
            Draw03RoadPartitions.Carriageway, Draw03RoadPartitions.Sidewalk,
            Draw03RoadAppearance.Regions(false, sample.RoadToObject),
            Draw03RoadAppearance.Regions(true, sample.RoadToObject),
            Draw03RoadAppearance.Patches(layout, sample.RoadToObject), 3, layout);
}
