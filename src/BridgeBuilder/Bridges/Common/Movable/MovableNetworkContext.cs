using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

// One disposable calculation for an explicitly supplied measured network arrangement.
// Indices describe this arrangement, not a universal bridge layout or persisted identities.
internal sealed partial class MovableNetworkContext : IDisposable
{
    internal readonly struct NodeInput
    {
        internal readonly Entity SourcePrefab;
        internal readonly Node Node;
        internal readonly PseudoRandomSeed Seed;
        internal readonly Elevation? Elevation;
        internal NodeInput(Entity sourcePrefab, Node node, PseudoRandomSeed seed, Elevation? elevation)
        { SourcePrefab = sourcePrefab; Node = node; Seed = seed; Elevation = elevation; }
    }

    internal readonly struct EdgeInput
    {
        internal readonly Entity SourcePrefab;
        internal readonly int Start, End;
        internal readonly Bezier4x3 Curve;
        internal readonly Upgraded? Upgrade;
        internal readonly PseudoRandomSeed Seed;
        internal readonly Elevation? Elevation;
        internal EdgeInput(Entity sourcePrefab, int start, int end, Bezier4x3 curve,
            PseudoRandomSeed seed, Elevation? elevation, Upgraded? upgrade = null)
        { SourcePrefab = sourcePrefab; Start = start; End = end; Curve = curve;
            Upgrade = upgrade;
            Seed = seed; Elevation = elevation; }
    }

    internal readonly struct OwnerIndex
    {
        internal readonly bool IsEdge;
        internal readonly int Index;
        internal OwnerIndex(bool isEdge, int index) { IsEdge = isEdge; Index = index; }
    }

    internal readonly struct MechanismInput
    {
        internal readonly StaticObjectPrefab Prefab;
        internal readonly OwnerIndex Owner;
        internal readonly Transform OwnerTransform;
        internal readonly NetSubObjectInfo Placement;
        internal readonly TrafficLights SignalState;
        internal readonly PseudoRandomSeed Seed;
        internal MechanismInput(StaticObjectPrefab prefab, OwnerIndex owner, Transform ownerTransform,
            NetSubObjectInfo placement, TrafficLights signalState, PseudoRandomSeed seed)
        {
            if (placement.m_Object != prefab)
                throw new ArgumentException("Mechanism placement must reference its prepared object.", nameof(placement));
            Prefab = prefab; Owner = owner; OwnerTransform = ownerTransform;
            Placement = placement; SignalState = signalState; Seed = seed;
        }
    }

    internal readonly struct MiddleInput
    {
        internal readonly int Edge, Node;
        internal readonly float Position;
        internal MiddleInput(int edge, int node, float position)
        { Edge = edge; Node = node; Position = position; }
    }

    internal readonly struct ParentInput
    {
        internal readonly OwnerIndex Child, Parent;
        internal ParentInput(OwnerIndex child, OwnerIndex parent) { Child = child; Parent = parent; }
    }

    internal readonly World World;
    internal readonly MovableNetworkPrefabs Prefabs;
    internal readonly Entity[] Nodes, Edges, Mechanisms;
    private readonly bool _leftHandTraffic;
    private bool _disposed;

    // Terrain belongs to the caller, which must complete its producer before this synchronous call.
    // Consume entities and owner geometry only while this context is alive. Never persist them.
    internal MovableNetworkContext(string owner, IReadOnlyList<NodeInput> nodes,
        IReadOnlyList<EdgeInput> edges, IReadOnlyList<MechanismInput> mechanisms,
        IReadOnlyList<MiddleInput> middleConnections, IReadOnlyList<ParentInput> parents,
        TerrainHeightData terrain, Entity sourceTheme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration)
    {
        World = new World("BridgeBuilder movable closed network");
        Prefabs = new MovableNetworkPrefabs(World, owner);
        Nodes = new Entity[nodes.Count];
        Edges = new Entity[edges.Count];
        Mechanisms = new Entity[mechanisms.Count];
        _leftHandTraffic = leftHandTraffic;
        try
        {
            var lanes = World.GetOrCreateSystemManaged<MovableNetworkLanes>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var input = nodes[i];
                Nodes[i] = lanes.CreateNode(Prefabs.Import(input.SourcePrefab), input.Node,
                    input.Seed, input.Elevation);
            }
            for (var i = 0; i < edges.Count; i++)
            {
                var input = edges[i];
                var prefab = Prefabs.Import(input.SourcePrefab);
                Edges[i] = lanes.CreateEdge(prefab, Nodes[input.Start], Nodes[input.End], input.Curve,
                    default, input.Seed, input.Elevation);
                if (input.Upgrade.HasValue)
                    World.EntityManager.AddComponentData(Edges[i], input.Upgrade.Value);
            }
            foreach (var input in middleConnections)
                lanes.ConnectMiddle(Edges[input.Edge], Nodes[input.Node], input.Position);
            foreach (var input in parents)
                lanes.SetNetworkOwner(Owner(input.Child), Owner(input.Parent));
            for (var i = 0; i < mechanisms.Count; i++)
            {
                var input = mechanisms[i];
                Mechanisms[i] = lanes.CreateMechanism(Prefabs.StageMechanism(input.Prefab), Owner(input.Owner),
                    input.OwnerTransform, input.Placement, input.SignalState, input.Seed);
            }
            lanes.CalculateNetwork(terrain, Prefabs.Import(sourceTheme), leftHandTraffic,
                randomSeed, Prefabs.ImportConfiguration(buildingConfiguration), loaded: true);
        }
        catch
        {
            World.Dispose();
            throw;
        }
    }

    internal Entity Owner(OwnerIndex index) => index.IsEdge ? Edges[index.Index] : Nodes[index.Index];

    // CompileRoad returns world-space geometry relative to its supplied origin. The road
    // asset builder needs that frame expressed in the actually anchored mechanism frame.
    // Retain full orientation; a translation-only height fix fails on a rotated sample.
    internal UnityEngine.Pose RoadToMechanism(int mechanismIndex, float3 roadOrigin)
    {
        var transform = World.EntityManager.GetComponentData<Transform>(Mechanisms[mechanismIndex]);
        var inverse = math.inverse(transform.m_Rotation);
        return new UnityEngine.Pose(math.mul(inverse, roadOrigin - transform.m_Position), inverse);
    }

    internal MovableLaneVisuals.Batch[] LaneVisuals(IReadOnlyList<OwnerIndex> owners, float3 origin)
    {
        var entities = new Entity[owners.Count];
        for (var i = 0; i < entities.Length; i++) entities[i] = Owner(owners[i]);
        if (!MovableLaneVisuals.TryExtract(World, entities, Prefabs, _leftHandTraffic, origin,
                out var batches, out var error))
            throw new InvalidOperationException(error);
        return batches;
    }

    internal MovableRoadObjects.Rule[] ObjectRules(Entity composition)
        => MovableRoadObjects.Extract(World, composition, Prefabs);

    internal BridgePreviewComposition.Piece[] Pieces(Entity composition, out NetCompositionData data)
    {
        var manager = World.EntityManager;
        data = manager.GetComponentData<NetCompositionData>(composition);
        var source = manager.GetBuffer<NetCompositionPiece>(composition, true);
        var rootIndices = World.GetOrCreateSystemManaged<MovableNetworkLanes>().SectionRoots(composition);
        var rootSections = manager.GetBuffer<NetGeometrySection>(
            manager.GetComponentData<PrefabRef>(composition).m_Prefab, true);
        var result = new BridgePreviewComposition.Piece[source.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var piece = source[i];
            var prefab = (NetPiecePrefab)Prefabs.Original(piece.m_Piece);
            Prefabs.CheckSource(prefab);
            piece.m_Piece = Entity.Null;
            var root = (NetSectionPrefab)Prefabs.Original(rootSections[rootIndices[i]].m_Section);
            Prefabs.CheckSource(root);
            result[i] = new BridgePreviewComposition.Piece(prefab, piece,
                $"composition/section[{rootIndices[i]}]/piece[{i}]", root);
        }
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        World.Dispose();
    }
}
