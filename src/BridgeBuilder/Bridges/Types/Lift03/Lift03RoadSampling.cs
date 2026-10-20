using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Both selected networks share one native calculation and one mechanism. Auxiliary
// nodes/edges belong to corresponding main nodes/edges through native Owner/SubNet.
internal sealed class Lift03RoadSampling : IDisposable
{
    internal enum Deck { Main, Auxiliary }
    internal readonly MovableNetworkContext Network;
    internal readonly MovableClosedRoad.Result MainRoad, AuxiliaryRoad;
    internal readonly Pose MainToObject, AuxiliaryToObject;
    private readonly MovableSamplingMechanism _mechanism;
    private readonly Entity _theme;
    private readonly float3[] _origins;
    private readonly NetPieceLayerMask[] _replacedLayers;
    private readonly bool _auxiliaryOpposite;

    internal static Lift03RoadSampling Create(string owner, Entity selectedMain, Entity selectedAuxiliary,
        RenderPrefab[] roots, ContentPrefab content, float halfWidthDelta, RandomSeed seed,
        NetPieceLayerMask mainReplacedLayers, NetPieceLayerMask auxiliaryReplacedLayers,
        Upgraded? mainUpgrade = null, Upgraded? auxiliaryUpgrade = null, bool auxiliaryOpposite = false)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        var mechanism = new MovableSamplingMechanism(owner, roots, content, halfWidthDelta,
            new[] { "LiftBridge03 Mesh", "LiftBridge03Pillar Mesh", "LiftBridge03PillarBase Mesh" },
            Lift03MechanismData.Meshes, Lift03MechanismData.ApplyComponents);
        try
        {
            return new Lift03RoadSampling(owner, selectedMain, selectedAuxiliary, mechanism,
                environment, seed, mainReplacedLayers, auxiliaryReplacedLayers,
                mainUpgrade, auxiliaryUpgrade, auxiliaryOpposite);
        }
        catch { mechanism.Dispose(); throw; }
    }

    private Lift03RoadSampling(string owner, Entity selectedMain, Entity selectedAuxiliary,
        MovableSamplingMechanism mechanism, MovableSamplingEnvironment environment, RandomSeed seed,
        NetPieceLayerMask mainReplacedLayers, NetPieceLayerMask auxiliaryReplacedLayers,
        Upgraded? mainUpgrade, Upgraded? auxiliaryUpgrade, bool auxiliaryOpposite)
    {
        _mechanism = mechanism;
        _auxiliaryOpposite = auxiliaryOpposite;
        _theme = environment.Theme;
        var mainOrigin = new float3(0f, Lift03NetworkData.MainElevation, 0f);
        _origins = new[] { mainOrigin, mainOrigin + Lift03NetworkData.AuxiliaryOffset };
        _replacedLayers = new[] { mainReplacedLayers, auxiliaryReplacedLayers };
        var prefabs = new[] { selectedMain, selectedAuxiliary };
        var upgrades = new[] { mainUpgrade, auxiliaryUpgrade };
        var nodes = new MovableNetworkContext.NodeInput[6];
        var edges = new MovableNetworkContext.EdgeInput[4];
        var random = seed.GetRandom(0);
        for (var deck = 0; deck < 2; deck++)
        {
            var elevation = new Elevation(new float2(_origins[deck].y));
            for (var station = 0; station < 3; station++)
                nodes[deck * 3 + station] = new MovableNetworkContext.NodeInput(prefabs[deck],
                    new Node
                    {
                        m_Position = _origins[deck] + new float3(0f, 0f,
                            (station - 1) * Lift03NetworkData.ApproachLength),
                        m_Rotation = quaternion.identity
                    }, new PseudoRandomSeed(ref random), elevation);
            for (var edge = 0; edge < 2; edge++)
            {
                var start = deck * 3 + edge;
                var end = start + 1;
                // Keep physical node/edge ownership unchanged while reversing the
                // auxiliary network's native composition and lane direction.
                if (deck == 1 && auxiliaryOpposite) (start, end) = (end, start);
                edges[deck * 2 + edge] = new MovableNetworkContext.EdgeInput(prefabs[deck], start, end,
                    NetUtils.StraightCurve(nodes[start].Node.m_Position, nodes[end].Node.m_Position),
                    new PseudoRandomSeed(ref random), elevation, upgrades[deck]);
            }
        }
        var parents = new MovableNetworkContext.ParentInput[5];
        for (var node = 0; node < 3; node++)
            parents[node] = new MovableNetworkContext.ParentInput(
                new MovableNetworkContext.OwnerIndex(false, node + 3),
                new MovableNetworkContext.OwnerIndex(false, node));
        for (var edge = 0; edge < 2; edge++)
            parents[edge + 3] = new MovableNetworkContext.ParentInput(
                new MovableNetworkContext.OwnerIndex(true, edge + 2),
                new MovableNetworkContext.OwnerIndex(true, edge));
        var mechanisms = new[]
        {
            new MovableNetworkContext.MechanismInput(mechanism.Prefab,
                new MovableNetworkContext.OwnerIndex(false, 1),
                new Transform { m_Position = mainOrigin, m_Rotation = quaternion.identity },
                Lift03NetworkData.MechanismPlacement(mechanism.Prefab),
                new TrafficLights
                {
                    m_State = TrafficLightState.Ongoing, m_CurrentSignalGroup = 1,
                    m_NextSignalGroup = 1, m_SignalGroupCount = 2
                }, new PseudoRandomSeed(ref random))
        };
        Network = new MovableNetworkContext(owner, nodes, edges, mechanisms,
            Array.Empty<MovableNetworkContext.MiddleInput>(), parents, environment.Terrain,
            environment.Theme, environment.LeftHandTraffic, seed, environment.BuildingConfiguration);
        try
        {
            MainRoad = Compile(Deck.Main, 0);
            AuxiliaryRoad = Compile(Deck.Auxiliary, 0);
            MainToObject = Network.RoadToMechanism(0, _origins[0]);
            AuxiliaryToObject = Network.RoadToMechanism(0, _origins[1]);
        }
        catch { Network.Dispose(); throw; }
    }

    internal MovableClosedRoad.Result CompileLod(Deck deck, int lod) => lod == 0
        ? deck switch
        {
            Deck.Main => MainRoad, Deck.Auxiliary => AuxiliaryRoad,
            _ => throw new ArgumentOutOfRangeException(nameof(deck))
        }
        : Compile(deck, lod);

    private MovableClosedRoad.Result Compile(Deck deck, int lod)
    {
        var index = DeckIndex(deck);
        var reversed = index == 1 && _auxiliaryOpposite;
        return Network.CompileRoad(new[]
        {
            (index * 2, reversed ? MovableNetworkContext.SurfacePart.StartNode : MovableNetworkContext.SurfacePart.EndNode),
            (index * 2 + 1, reversed ? MovableNetworkContext.SurfacePart.EndNode : MovableNetworkContext.SurfacePart.StartNode)
        }, new[] { new MovableNetworkContext.OwnerIndex(false, index * 3 + 1) }, _origins[index],
            replacedStructureLayers: _replacedLayers[index], trackSourceFaces: false, lod: lod);
    }

    internal MovableNodeObjects.Result Objects(Deck deck)
    {
        var index = DeckIndex(deck);
        return Network.NodeObjects(index * 3 + 1, _theme, _origins[index]);
    }

    internal MovableRoadAssets.Level[] PrepareDeck(Deck deck,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches)
    {
        var index = DeckIndex(deck);
        var road = index == 0 ? MainRoad : AuxiliaryRoad;
        var pose = index == 0 ? MainToObject : AuxiliaryToObject;
        var layout = new MovableSelectedRoadLayout(road, pose);
        return MovableRoadPreparation.Prepare(road, pose, lod => CompileLod(deck, lod),
            sources, halfDelta, closedWorld, material, Lift03RoadPartitions.Carriageway, sidewalk,
            carriagewayAppearance, sidewalkAppearance, patches, 4, layout);
    }

    private static int DeckIndex(Deck deck) => deck switch
    {
        Deck.Main => 0, Deck.Auxiliary => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(deck))
    };

    public void Dispose()
    {
        try { Network.Dispose(); }
        finally { _mechanism.Dispose(); }
    }
}
