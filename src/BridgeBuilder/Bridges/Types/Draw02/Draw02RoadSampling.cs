using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured DrawBridge02 closed network arrangement. Sampling is only the selected-road
// input stage; complete road/structure replacement is required before publishing assets.
internal static class Draw02RoadSampling
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample,
        RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, MovableSelectedRoadLayout layout)
    {
        return MovableRoadPreparation.Prepare(sample, nativeLevels,
            halfWidthDelta, closedWorld, material, Draw02RoadPartitions.Carriageway,
            Draw02RoadPartitions.Sidewalk,
            Draw02RoadAppearance.Regions(false, sample.RoadToObject),
            Draw02RoadAppearance.Regions(true, sample.RoadToObject),
            Draw02RoadAppearance.Patches(layout, sample.RoadToObject), 3, layout);
    }

    internal static MovableStraightSample Create(string owner, Entity selectedRoad,
        RenderPrefab[] roots, ContentPrefab content, float halfWidthDelta,
        RandomSeed seed, Upgraded? upgrade = null) =>
        MovableStraightSample.Create(owner, selectedRoad, roots, content, halfWidthDelta, seed,
            new[] { "DrawBridge02 Mesh", "DrawBridge02Pillar Mesh", "DrawBridge02PillarBase Mesh" },
            Draw02MechanismData.Meshes, Draw02MechanismData.ApplyComponents,
            Draw02NetworkData.MechanismPlacement, 144f, 10f,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, upgrade);
}
