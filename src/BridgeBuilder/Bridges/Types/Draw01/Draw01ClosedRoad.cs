using System;
using System.Collections.Generic;
using Colossal.Mathematics;
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
    private Draw01SamplingMechanism? _samplingMechanism;

    internal static Draw01ClosedRoad Sample(string owner, Entity selectedRoad,
        IReadOnlyList<RenderPrefab> nativeRenderRoots, ContentPrefab content,
        float halfWidthDelta, NetSubObjectInfo placement, RandomSeed seed,
        Upgraded? upgrade = null)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        // The recorded native bridge's water elevation is 10 m. This private
        // arrangement uses the same elevation over flat zero-height terrain.
        // Calculation and visual extraction complete synchronously before the
        // height arrays are disposed; the returned context retains its own entities.
        var mechanism = new Draw01SamplingMechanism(owner, nativeRenderRoots, content, halfWidthDelta);
        try
        {
            var result = new Draw01ClosedRoad(owner, selectedRoad, mechanism.Prefab, placement,
                new float3(0f, 10f, 0f), new Elevation(new float2(10f)), environment.Terrain,
                environment.Theme, environment.LeftHandTraffic, seed,
                environment.BuildingConfiguration, upgrade);
            result._samplingMechanism = mechanism;
            return result;
        }
        catch
        {
            mechanism.Dispose();
            throw;
        }
    }

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

    // The local render bounds describe the closed geometry. Native StandingObject
    // and StackProperties extend the object's culling bounds separately; a swept
    // animation volume must not be substituted for authored stacking metadata.
    internal MovableRoadAssets.Level[] PrepareRenderLevels(IReadOnlyList<RenderPrefab> sources,
        float halfWidthDelta, Matrix4x4 closedWorld)
    {
        if (sources.Count != 3 || !math.isfinite(halfWidthDelta))
            throw new ArgumentException("Draw01 road preparation requires three render sources and a finite width delta.");
        var roadPartitions = Draw01RoadPartitions.ForRoad(Road);
        var lanePartitions = Draw01RoadPartitions.ForLanes(Road);
        var road = MovableRoadGeometry.InFrame(Road.Triangles, RoadToObject);
        var lanes = MovableRoadGeometry.InFrame(Road.LaneGeometry.Triangles, RoadToObject);
        var levels = new MovableRoadAssets.Level[3];
        for (var i = 0; i < levels.Length; i++)
        {
            var bounds = sources[i].bounds;
            bounds.min.x -= halfWidthDelta;
            bounds.max.x += halfWidthDelta;
            Include(road, ref bounds);
            Include(lanes, ref bounds);
            levels[i] = new MovableRoadAssets.Level(sources[i], Road,
                Draw01RoadPartitions.Carriageway, bounds, RoadToObject,
                closedWorld: closedWorld, lanePartitions: lanePartitions,
                roadTrianglePartitions: roadPartitions);
        }
        return levels;
    }

    private static void Include(IReadOnlyList<MovableRoadGeometry.Triangle> triangles, ref Bounds3 bounds)
    {
        foreach (var triangle in triangles)
        {
            bounds.min = math.min(bounds.min, math.min((float3)triangle.A.Position,
                math.min((float3)triangle.B.Position, (float3)triangle.C.Position)));
            bounds.max = math.max(bounds.max, math.max((float3)triangle.A.Position,
                math.max((float3)triangle.B.Position, (float3)triangle.C.Position)));
        }
    }

    public void Dispose()
    {
        try { Network.Dispose(); }
        finally { _samplingMechanism?.Dispose(); }
    }
}
