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
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material, MovableSelectedRoadLayout layout)
    {
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, Draw02RoadPartitions.Carriageway, Draw02RoadPartitions.Sidewalk, Draw02RoadAppearance.Regions(false, sample.RoadToObject), Draw02RoadAppearance.Regions(true, sample.RoadToObject), Draw02RoadAppearance.Patches(layout, sample.RoadToObject), Numeric_Types_Draw02_Draw02RoadSampling_cs.Values.I32[0], layout);
    }

    internal static MovableStraightSample Create(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfWidthDelta, RandomSeed seed, Upgraded? upgrade = null) => MovableStraightSample.Create(owner, selectedRoad, roots, content, halfWidthDelta, seed, new[] { "DrawBridge02 Mesh", "DrawBridge02Pillar Mesh", "DrawBridge02PillarBase Mesh" }, Draw02MechanismData.Meshes, Draw02MechanismData.ApplyComponents, Draw02NetworkData.MechanismPlacement, Numeric_Types_Draw02_Draw02RoadSampling_cs.Values.F32[0], Numeric_Types_Draw02_Draw02RoadSampling_cs.Values.F32[1], NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, upgrade);
}

// Embedded numerical arrays: Types_Draw02_Draw02RoadSampling_cs
internal static class Numeric_Types_Draw02_Draw02RoadSampling_cs
{
    static Numeric_Types_Draw02_Draw02RoadSampling_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02RoadSampling_cs");
}
