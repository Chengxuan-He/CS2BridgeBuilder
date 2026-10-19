using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

// Measured DrawBridge02 closed network arrangement. Sampling is only the selected-road
// input stage; complete road/structure replacement is required before publishing assets.
internal static class Draw02RoadSampling
{
    internal static MovableStraightSample Create(string owner, Entity selectedRoad,
        IReadOnlyList<RenderPrefab> sources, ContentPrefab content, float halfWidthDelta,
        RandomSeed seed, Upgraded? upgrade = null) =>
        MovableStraightSample.Create(owner, selectedRoad, sources, content, halfWidthDelta, seed,
            new[] { "DrawBridge02 Mesh", "DrawBridge02Pillar Mesh", "DrawBridge02PillarBase Mesh" },
            Draw02MechanismData.Meshes, Draw02MechanismData.ApplyComponents,
            Draw02NetworkData.MechanismPlacement, 144f, 10f,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, upgrade);
}
