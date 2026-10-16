using System;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

// Draw01's straight closed-pose sampling arrangement. The approaches supply native
// connectivity; only the two central-node halves become the mechanism's road input.
// This is not a city-construction request and does not persist the sampling entities.
internal sealed class Draw01ClosedRoad : IDisposable
{
    internal readonly MovableNetworkContext Network;
    internal readonly MovableClosedRoad.Result Road;
    internal readonly Pose RoadToObject;
    private readonly Entity _sourceTheme;
    private readonly float3 _roadOrigin;

    internal Draw01ClosedRoad(string owner, Entity selectedRoad, StaticObjectPrefab mechanism,
        NetSubObjectInfo placement, float3 roadOrigin, Elevation elevation,
        TerrainHeightData terrain, Entity sourceTheme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration,
        Upgraded? upgrade = null)
    {
        _sourceTheme = sourceTheme;
        _roadOrigin = roadOrigin;
        // Native DrawBridge01 Bridge.m_SegmentLength is 144 m. These outer nodes
        // bound the sampling approaches, NOT the road cut or the mechanism extent.
        // The staged NetObject's 90 m offset supplies the fixed-node extent to the
        // native GeometrySystem on both incident edges.
        var positions = new[]
        {
            roadOrigin + new float3(0f, 0f, -144f), roadOrigin,
            roadOrigin + new float3(0f, 0f, 144f)
        };
        var random = randomSeed.GetRandom(0);
        var nodes = new MovableNetworkContext.NodeInput[positions.Length];
        for (var i = 0; i < nodes.Length; i++)
            nodes[i] = new MovableNetworkContext.NodeInput(selectedRoad,
                new Node { m_Position = positions[i], m_Rotation = quaternion.identity },
                new PseudoRandomSeed(ref random), elevation);
        var edges = new MovableNetworkContext.EdgeInput[2];
        for (var i = 0; i < edges.Length; i++)
            edges[i] = new MovableNetworkContext.EdgeInput(selectedRoad, i, i + 1,
                NetUtils.StraightCurve(positions[i], positions[i + 1]),
                new PseudoRandomSeed(ref random), elevation, upgrade);
        var centralNode = new MovableNetworkContext.OwnerIndex(false, 1);
        var mechanisms = new[]
        {
            new MovableNetworkContext.MechanismInput(mechanism, centralNode,
                new Transform { m_Position = roadOrigin, m_Rotation = quaternion.identity },
                placement, new TrafficLights
                {
                    m_State = TrafficLightState.Ongoing, m_CurrentSignalGroup = 1,
                    m_NextSignalGroup = 1, m_SignalGroupCount = 2
                }, new PseudoRandomSeed(ref random))
        };
        Network = new MovableNetworkContext(owner, nodes, edges, mechanisms,
            Array.Empty<MovableNetworkContext.MiddleInput>(),
            Array.Empty<MovableNetworkContext.ParentInput>(), terrain, sourceTheme,
            leftHandTraffic, randomSeed, buildingConfiguration);
        try
        {
            Road = Network.CompileRoad(new[]
            {
                (0, MovableNetworkContext.SurfacePart.EndNode),
                (1, MovableNetworkContext.SurfacePart.StartNode)
            }, new[] { centralNode }, roadOrigin);
            RoadToObject = Network.RoadToMechanism(0, roadOrigin);
        }
        catch
        {
            Network.Dispose();
            throw;
        }
    }

    // Resolve native node objects while the private lanes and signal needs still exist.
    // Their positions share Road's origin-relative frame, before mechanism bone binding.
    internal MovableNodeObjects.Result Objects()
        => Network.NodeObjects(1, _sourceTheme, _roadOrigin);

    public void Dispose() => Network.Dispose();
}
