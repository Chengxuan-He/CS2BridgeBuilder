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
    internal enum Deck
    {
        Main,
        Auxiliary
    }

    internal readonly MovableNetworkContext Network;
    internal readonly MovableClosedRoad.Result MainRoad, AuxiliaryRoad;
    internal readonly Pose MainToObject, AuxiliaryToObject;
    private readonly MovableSamplingMechanism _mechanism;
    private readonly Entity _theme;
    private readonly float3[] _origins;
    private readonly NetPieceLayerMask[] _replacedLayers;
    private readonly bool _auxiliaryOpposite;
    internal static Lift03RoadSampling Create(string owner, Entity selectedMain, Entity selectedAuxiliary, RenderPrefab[] roots, ContentPrefab content, float halfWidthDelta, RandomSeed seed, NetPieceLayerMask mainReplacedLayers, NetPieceLayerMask auxiliaryReplacedLayers, Upgraded? mainUpgrade = null, Upgraded? auxiliaryUpgrade = null, bool auxiliaryOpposite = false)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        var mechanism = new MovableSamplingMechanism(owner, roots, content, halfWidthDelta, new[] { "LiftBridge03 Mesh", "LiftBridge03Pillar Mesh", "LiftBridge03PillarBase Mesh" }, Lift03MechanismData.Meshes, Lift03MechanismData.ApplyComponents);
        try
        {
            return new Lift03RoadSampling(owner, selectedMain, selectedAuxiliary, mechanism, environment, seed, mainReplacedLayers, auxiliaryReplacedLayers, mainUpgrade, auxiliaryUpgrade, auxiliaryOpposite);
        }
        catch
        {
            mechanism.Dispose();
            throw;
        }
    }

    private Lift03RoadSampling(string owner, Entity selectedMain, Entity selectedAuxiliary, MovableSamplingMechanism mechanism, MovableSamplingEnvironment environment, RandomSeed seed, NetPieceLayerMask mainReplacedLayers, NetPieceLayerMask auxiliaryReplacedLayers, Upgraded? mainUpgrade, Upgraded? auxiliaryUpgrade, bool auxiliaryOpposite)
    {
        _mechanism = mechanism;
        _auxiliaryOpposite = auxiliaryOpposite;
        _theme = environment.Theme;
        var mainOrigin = new float3(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.F32[0], Lift03NetworkData.MainElevation, Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.F32[1]);
        _origins = new[]
        {
            mainOrigin,
            mainOrigin + Lift03NetworkData.AuxiliaryOffset
        };
        _replacedLayers = new[]
        {
            mainReplacedLayers,
            auxiliaryReplacedLayers
        };
        var prefabs = new[]
        {
            selectedMain,
            selectedAuxiliary
        };
        var upgrades = new[]
        {
            mainUpgrade,
            auxiliaryUpgrade
        };
        var nodes = new MovableNetworkContext.NodeInput[Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[0]];
        var edges = new MovableNetworkContext.EdgeInput[Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[1]];
        var random = seed.GetRandom(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[2]);
        for (var deck = Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[3]; deck < Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[4]; deck++)
        {
            var elevation = new Elevation(new float2(_origins[deck].y));
            for (var station = Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[5]; station < Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[6]; station++)
                nodes[deck * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[7] + station] = new MovableNetworkContext.NodeInput(prefabs[deck], new Node { m_Position = _origins[deck] + new float3(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.F32[2], Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.F32[3], (station - Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[8]) * Lift03NetworkData.ApproachLength), m_Rotation = quaternion.identity }, new PseudoRandomSeed(ref random), elevation);
            for (var edge = Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[9]; edge < Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[10]; edge++)
            {
                var start = deck * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[11] + edge;
                var end = start + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[12];
                // Keep physical node/edge ownership unchanged while reversing the
                // auxiliary network's native composition and lane direction.
                if (deck == Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[13] && auxiliaryOpposite)
                    (start, end) = (end, start);
                edges[deck * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[14] + edge] = new MovableNetworkContext.EdgeInput(prefabs[deck], start, end, NetUtils.StraightCurve(nodes[start].Node.m_Position, nodes[end].Node.m_Position), new PseudoRandomSeed(ref random), elevation, upgrades[deck]);
            }
        }

        var parents = new MovableNetworkContext.ParentInput[Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[15]];
        for (var node = Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[16]; node < Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[17]; node++)
            parents[node] = new MovableNetworkContext.ParentInput(new MovableNetworkContext.OwnerIndex(false, node + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[18]), new MovableNetworkContext.OwnerIndex(false, node));
        for (var edge = Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[19]; edge < Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[20]; edge++)
            parents[edge + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[21]] = new MovableNetworkContext.ParentInput(new MovableNetworkContext.OwnerIndex(true, edge + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[22]), new MovableNetworkContext.OwnerIndex(true, edge));
        var mechanisms = new[]
        {
            new MovableNetworkContext.MechanismInput(mechanism.Prefab, new MovableNetworkContext.OwnerIndex(false, Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[23]), new Transform { m_Position = mainOrigin, m_Rotation = quaternion.identity }, Lift03NetworkData.MechanismPlacement(mechanism.Prefab), new TrafficLights { m_State = TrafficLightState.Ongoing, m_CurrentSignalGroup = (byte)(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[24]), m_NextSignalGroup = (byte)(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[25]), m_SignalGroupCount = (byte)(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[26]) }, new PseudoRandomSeed(ref random))
        };
        Network = new MovableNetworkContext(owner, nodes, edges, mechanisms, Array.Empty<MovableNetworkContext.MiddleInput>(), parents, environment.Terrain, environment.Theme, environment.LeftHandTraffic, seed, environment.BuildingConfiguration);
        try
        {
            MainRoad = Compile(Deck.Main, Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[27]);
            AuxiliaryRoad = Compile(Deck.Auxiliary, Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[28]);
            MainToObject = Network.RoadToMechanism(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[29], _origins[Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[30]]);
            AuxiliaryToObject = Network.RoadToMechanism(Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[31], _origins[Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[32]]);
        }
        catch
        {
            Network.Dispose();
            throw;
        }
    }

    internal MovableClosedRoad.Result CompileLod(Deck deck, int lod) => lod == Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[33] ? deck switch
    {
        Deck.Main => MainRoad,
        Deck.Auxiliary => AuxiliaryRoad,
        _ => throw new ArgumentOutOfRangeException(nameof(deck))} : Compile(deck, lod);
    private MovableClosedRoad.Result Compile(Deck deck, int lod)
    {
        var index = DeckIndex(deck);
        var reversed = index == Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[34] && _auxiliaryOpposite;
        return Network.CompileRoad(new[] { (index * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[35], reversed ? MovableNetworkContext.SurfacePart.StartNode : MovableNetworkContext.SurfacePart.EndNode), (index * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[36] + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[37], reversed ? MovableNetworkContext.SurfacePart.EndNode : MovableNetworkContext.SurfacePart.StartNode) }, new[] { new MovableNetworkContext.OwnerIndex(false, index * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[38] + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[39]) }, _origins[index], replacedStructureLayers: _replacedLayers[index], trackSourceFaces: false, lod: lod);
    }

    internal MovableNodeObjects.Result Objects(Deck deck)
    {
        var index = DeckIndex(deck);
        return Network.NodeObjects(index * Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[40] + Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[41], _theme, _origins[index]);
    }

    internal MovableRoadAssets.Level[] PrepareDeck(Deck deck, IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld, MovableRoadMaterial material, Partition sidewalk, Partition carriagewayAppearance, Partition sidewalkAppearance, MovableExistingRoadSurface.Patch[] patches)
    {
        var index = DeckIndex(deck);
        var road = index == Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[42] ? MainRoad : AuxiliaryRoad;
        var pose = index == Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[43] ? MainToObject : AuxiliaryToObject;
        var layout = new MovableSelectedRoadLayout(road, pose, deck == Deck.Main ? "Lift03 Main" : "Lift03 Auxiliary");
        return MovableRoadPreparation.Prepare(road, pose, lod => CompileLod(deck, lod), sources, halfDelta, closedWorld, material, Lift03RoadPartitions.Carriageway, sidewalk, carriagewayAppearance, sidewalkAppearance, patches, Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[44], layout,
            lod => Lift03RoadAppearance.ForLevel(deck, lod, sources[lod], pose, halfDelta));
    }

    private static int DeckIndex(Deck deck) => deck switch
    {
        Deck.Main => Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[45],
        Deck.Auxiliary => Numeric_Types_Lift03_Lift03RoadSampling_cs.Values.I32[46],
        _ => throw new ArgumentOutOfRangeException(nameof(deck))};
    public void Dispose()
    {
        try
        {
            Network.Dispose();
        }
        finally
        {
            _mechanism.Dispose();
        }
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03RoadSampling_cs
internal static class Numeric_Types_Lift03_Lift03RoadSampling_cs
{
    static Numeric_Types_Lift03_Lift03RoadSampling_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03RoadSampling_cs");
}
