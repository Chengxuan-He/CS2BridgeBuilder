using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

// Private straight single-network sampling. This does not provide a dual-deck arrangement,
// road replacement masks, motion seams, structural widening or a complete generator.
internal sealed class MovableStraightSample : IMovableRoadSample
{
    internal readonly MovableNetworkContext Network;
    internal MovableClosedRoad.Result Road = null!;
    public string? MaterialError=>Road.Error;
    internal Pose RoadToObject;
    private readonly Entity _sourceTheme;
    private readonly float3 _roadOrigin;
    private readonly NetPieceLayerMask _replacedLayers;
    private MovableSamplingMechanism? _mechanism;

    internal static MovableStraightSample Create(string owner, Entity selectedRoad,
        IReadOnlyList<RenderPrefab> sources, ContentPrefab content, float halfDelta,
        RandomSeed seed, IReadOnlyList<string> sourceNames,
        Func<RenderPrefab[], ObjectMeshInfo[]> meshes,
        Action<StaticObjectPrefab, ContentPrefab> applyComponents,
        Func<StaticObjectPrefab, NetSubObjectInfo> placement,
        float approachLength, float elevation, NetPieceLayerMask replacedLayers, Upgraded? upgrade = null) =>
        GenerationWork.Synchronous(() => CreateAsync(owner, selectedRoad, sources, content, halfDelta, seed, sourceNames, meshes, applyComponents, placement, approachLength, elevation, replacedLayers, upgrade));

    internal static async System.Threading.Tasks.Task<MovableStraightSample> CreateAsync(string owner, Entity selectedRoad,
        IReadOnlyList<RenderPrefab> sources, ContentPrefab content, float halfDelta,
        RandomSeed seed, IReadOnlyList<string> sourceNames,
        Func<RenderPrefab[], ObjectMeshInfo[]> meshes,
        Action<StaticObjectPrefab, ContentPrefab> applyComponents,
        Func<StaticObjectPrefab, NetSubObjectInfo> placement,
        float approachLength, float elevation, NetPieceLayerMask replacedLayers, Upgraded? upgrade = null)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        var mechanism = new MovableSamplingMechanism(owner, sources, content, halfDelta,
            sourceNames, meshes, applyComponents);
        try
        {
            var sample = new MovableStraightSample(owner, selectedRoad, mechanism.Prefab,
                placement(mechanism.Prefab), new float3(0f, elevation, 0f),
                new Elevation(new float2(elevation)), environment.Terrain, environment.Theme,
                environment.LeftHandTraffic, seed, environment.BuildingConfiguration,
                approachLength, replacedLayers, upgrade, deferRoad: true);
            try { await sample.InitializeRoadAsync(); }
            catch { sample.Network.Dispose(); throw; }
            sample._mechanism = mechanism;
            return sample;
        }
        catch { mechanism.Dispose(); throw; }
    }

    private MovableStraightSample(string owner, Entity selectedRoad, StaticObjectPrefab mechanism,
        NetSubObjectInfo placement, float3 roadOrigin, Elevation elevation,
        TerrainHeightData terrain, Entity sourceTheme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration,
        float approachLength, NetPieceLayerMask replacedLayers, Upgraded? upgrade, bool deferRoad = false)
    {
        _sourceTheme = sourceTheme;
        _roadOrigin = roadOrigin;
        _replacedLayers = replacedLayers;
        // Approach stations are supplied by the style; the native NetObject computes the node extent.
        var positions = new[]
        {
            roadOrigin + new float3(0f, 0f, -approachLength), roadOrigin,
            roadOrigin + new float3(0f, 0f, approachLength)
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
            leftHandTraffic, randomSeed, buildingConfiguration, deferCalculation: true);
        if (!deferRoad) GenerationWork.Synchronous(async () => { await InitializeRoadAsync(); return true; });
    }

    private async System.Threading.Tasks.Task InitializeRoadAsync()
    {
        try
        {
            await Network.CompleteCalculationAsync();
            await GenerationWork.Next("native closed road extraction");
            Road = await Network.CompileRoadAsync(new[]
            {
                (0, MovableNetworkContext.SurfacePart.EndNode),
                (1, MovableNetworkContext.SurfacePart.StartNode)
            }, new[] { new MovableNetworkContext.OwnerIndex(false, 1) }, _roadOrigin,
                replacedStructureLayers: _replacedLayers,
                trackSourceFaces: false);
            if(Road.Error!=null)return;
            RoadToObject = Network.RoadToMechanism(0, _roadOrigin);
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

    internal MovableClosedRoad.Result CompileLod(int lod) => GenerationWork.Synchronous(() => CompileLodAsync(lod));

    internal async System.Threading.Tasks.Task<MovableClosedRoad.Result> CompileLodAsync(int lod) => lod == 0 ? Road : await Network.CompileRoadAsync(new[]
    {
        (0, MovableNetworkContext.SurfacePart.EndNode),
        (1, MovableNetworkContext.SurfacePart.StartNode)
    }, new[] { new MovableNetworkContext.OwnerIndex(false, 1) }, _roadOrigin,
        replacedStructureLayers: _replacedLayers, trackSourceFaces: false, lod: lod);

    public void Dispose()
    {
        try { Network.Dispose(); }
        finally { _mechanism?.Dispose(); }
    }
}
