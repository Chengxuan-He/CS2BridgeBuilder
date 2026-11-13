using System;
using System.Collections.Generic;
using BridgeBuilder.Bridges;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Runtime;

// A disposable native geometry calculation for the same straight preview span.
// Authoring assets remain unregistered; only numeric snapshots leave this world.
internal sealed class BridgePreviewObjectCourse : IDisposable
{
    private readonly World _world = new("BridgeBuilder preview object course");
    private readonly MovableNetworkPrefabs _imports;
    internal sealed class Snapshot
    {
        internal Entity Owner, StartNode, EndNode;
        internal EdgeGeometry Edge;
        internal EdgeNodeGeometry Start, End;
        internal NetCompositionData EdgeData, StartData, EndData;
        internal NetGeometryData Network;
        internal bool Visible = true;
        internal BridgePreviewComposition.Piece[] Pieces = Array.Empty<BridgePreviewComposition.Piece>();
        internal BridgePreviewComposition.Piece[] StartPieces = Array.Empty<BridgePreviewComposition.Piece>();
        internal BridgePreviewComposition.Piece[] EndPieces = Array.Empty<BridgePreviewComposition.Piece>();
    }
    private readonly Dictionary<float, Entity> _nodes = new();
    private readonly List<Snapshot> _spans = new();
    private Entity _root;

    internal BridgePreviewObjectCourse()
    { _imports = new MovableNetworkPrefabs(_world, "preview"); }

    private Entity Composition(BridgePreviewComposition.Piece[] pieces, ref NetCompositionData data)
    {
        var manager = _world.EntityManager;
        var registry = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        using var input = new NativeList<NetCompositionPiece>(Allocator.Temp);
        foreach (var entry in pieces)
        {
            var piece = entry.UnpackedComposition;
            if (registry.TryGetEntity(entry.Prefab, out var registered)) piece.m_Piece = _imports.Import(registered);
            else
            {
                piece.m_Piece = _imports.StageRender(entry.Prefab);
                var prefab = entry.Prefab;
                manager.SetComponentData(piece.m_Piece, new NetPieceData
                {
                    m_Width = prefab.m_Width, m_Length = prefab.m_Length, m_HeightRange = prefab.m_HeightRange,
                    m_SurfaceHeights = prefab.m_SurfaceHeights, m_WidthOffset = prefab.m_WidthOffset,
                    m_NodeOffset = prefab.m_NodeOffset, m_SideConnectionOffset = prefab.m_SideConnectionOffset
                });
                if (prefab.TryGet<MatchPieceVertices>(out var match)) match.Initialize(manager, piece.m_Piece);
            }
            var lanes = new List<NetPieceLane>();
            if (entry.Prefab.TryGet<NetPieceLanes>(out var source))
                foreach (var lane in source.m_Lanes ?? Array.Empty<NetLaneInfo>())
                {
                    if (lane?.m_Lane == null || !registry.TryGetEntity(lane.m_Lane, out var native))
                        throw new InvalidOperationException("Preview object course has an unavailable lane prefab.");
                    lanes.Add(new NetPieceLane { m_Lane = _imports.Import(native), m_Position = lane.m_Position,
                        m_ExtraFlags = lane.m_FindAnchor ? LaneFlags.FindAnchor : 0 });
                }
            var buffer = manager.HasBuffer<NetPieceLane>(piece.m_Piece) ? manager.GetBuffer<NetPieceLane>(piece.m_Piece) : manager.AddBuffer<NetPieceLane>(piece.m_Piece);
            buffer.Clear();
            foreach (var lane in lanes) buffer.Add(lane);
            if (buffer.Length > 1) buffer.AsNativeArray().Sort();
            if (entry.Prefab.TryGet<NetPieceCrosswalk>(out var crosswalk))
            {
                if (crosswalk.m_Lane == null || !registry.TryGetEntity(crosswalk.m_Lane, out var native))
                    throw new InvalidOperationException("Preview object course has an unavailable crosswalk lane.");
                var lane = _imports.Import(native);
                manager.AddComponentData(piece.m_Piece, new NetCrosswalkData
                    { m_Lane = lane, m_Start = crosswalk.m_Start, m_End = crosswalk.m_End });
            }
            input.Add(piece);
        }
        var entity = _world.GetOrCreateSystemManaged<MovableNetworkLanes>().PreviewComposition(_root, data);
        var packed = manager.GetBuffer<NetCompositionPiece>(entity);
        foreach (var piece in input.AsArray()) packed.Add(piece);
        return entity;
    }

    internal Snapshot Add(NetGeometryPrefab deck, BridgeRoadContext? selected,
        BridgePreviewComposition.Piece[] edgePieces, NetCompositionData edgeData,
        float start, float length, float elevation)
    {
        var snapshot = new Snapshot { EdgeData = edgeData, Pieces = edgePieces };
        var manager = _world.EntityManager;
        var registry = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        var source = selected?.SourceEntity ?? (registry.TryGetEntity(deck, out var registered) ? registered : Entity.Null);
        // Clone the private import: node selection must see the generated bridge's states,
        // while the cached imported selected road remains untouched.
        var prefab = _root == Entity.Null ? (_root = manager.Instantiate(_imports.Import(source))) : _root;
        var network = manager.GetComponentData<NetGeometryData>(prefab);
        network.m_DefaultWidth = edgeData.m_Width;
        network.m_DefaultHeightRange = edgeData.m_HeightRange;
        network.m_DefaultSurfaceHeight = edgeData.m_SurfaceHeight;
        network.m_MaxSlopeSteepness = deck.m_MaxSlopeSteepness < .001f ? 0 : deck.m_MaxSlopeSteepness;
        if (deck.TryGet<Bridge>(out var bridge))
        {
            if (bridge.m_SegmentLength > .1f)
            {
                if (!bridge.m_AllowMinimalLength) network.m_EdgeLengthRange.min = bridge.m_SegmentLength * .6f;
                network.m_EdgeLengthRange.max = bridge.m_SegmentLength * 1.4f;
            }
            network.m_ElevatedLength = network.m_EdgeLengthRange.max;
            network.m_Hanging = bridge.m_Hanging;
            network.m_Flags |= GeometryFlags.StraightEdges | GeometryFlags.StraightEnds | GeometryFlags.RequireElevated |
                GeometryFlags.SymmetricalEdges | GeometryFlags.SmoothSlopes;
            if (bridge.m_CanCurve) network.m_Flags &= ~GeometryFlags.StraightEdges;
            if (bridge.m_BuildStyle is BridgeBuildStyle.Raised or BridgeBuildStyle.Quay)
                network.m_Flags |= GeometryFlags.ElevatedIsRaised;
            if (bridge.m_BuildStyle == BridgeBuildStyle.Quay) network.m_Flags &= ~GeometryFlags.RequireElevated;
        }
        manager.SetComponentData(prefab, network);
        manager.SetComponentEnabled<PrefabData>(prefab, true);
        var edgeStates = manager.GetBuffer<NetGeometryEdgeState>(prefab);
        edgeStates.Clear();
        foreach (var info in deck.m_EdgeStates ?? Array.Empty<NetEdgeStateInfo>())
        {
            var state = new NetGeometryEdgeState();
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireAll, out state.m_CompositionAll, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireAny, out state.m_CompositionAny, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireNone, out state.m_CompositionNone, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_SetState, out state.m_State, out _);
            edgeStates.Add(state);
        }
        var states = manager.GetBuffer<NetGeometryNodeState>(prefab);
        states.Clear();
        foreach (var info in deck.m_NodeStates ?? Array.Empty<NetNodeStateInfo>())
        {
            var state = new NetGeometryNodeState { m_MatchType = info.m_MatchType };
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireAll, out state.m_CompositionAll, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireAny, out state.m_CompositionAny, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_RequireNone, out state.m_CompositionNone, out _);
            NetCompositionHelpers.GetRequirementFlags(info.m_SetState, out state.m_State, out _);
            states.Add(state);
        }
        snapshot.Network = network;
        Entity NodeAt(float z)
        {
            if (_nodes.TryGetValue(z, out var cached)) return cached;
            var entity = _world.GetOrCreateSystemManaged<MovableNetworkLanes>().CreateNode(prefab,
                new Node { m_Position = new float3(0, elevation, z), m_Rotation = quaternion.identity },
                new PseudoRandomSeed(1), new Elevation { m_Elevation = new float2(elevation) });
            _nodes.Add(z, entity);
            return entity;
        }
        var a = NodeAt(start); var b = NodeAt(start + length);
        var edge = _world.GetOrCreateSystemManaged<MovableNetworkLanes>().CreateEdge(prefab, a, b,
            new Bezier4x3(new float3(0,elevation,start), new float3(0,elevation,start+length/3),
                new float3(0,elevation,start+length*2/3), new float3(0,elevation,start+length)),
            default, new PseudoRandomSeed(1), new Elevation { m_Elevation = new float2(elevation) });
        snapshot.Owner = edge; snapshot.StartNode = a; snapshot.EndNode = b;
        _spans.Add(snapshot);
        return snapshot;
    }

    internal async System.Threading.Tasks.Task CalculateAsync(NetGeometryPrefab deck)
    {
        var manager = _world.EntityManager;
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        var helper = _world.GetOrCreateSystemManaged<MovableNetworkLanes>();
        // Native node selection must see a continuing network at the presentation cut.
        // Context roads remain hidden; their boundary object occurrences may belong to the visible course.
        // Give their distant nodes private identities, even when disjoint approaches overlap.
        foreach (var span in _spans.ToArray())
        {
            void ContinueAt(Entity boundary, bool atStart)
            {
                if (manager.GetBuffer<ConnectedEdge>(boundary).Length != 1) return;
                var curve = manager.GetComponentData<Curve>(span.Owner);
                var position = manager.GetComponentData<Node>(boundary).m_Position;
                var length = curve.m_Length;
                var remotePosition = position + new float3(0, 0, atStart ? -length : length);
                var elevation = manager.GetComponentData<Elevation>(span.Owner);
                var remote = helper.CreateNode(_root, new Node { m_Position = remotePosition, m_Rotation = quaternion.identity },
                    new PseudoRandomSeed(1), elevation);
                var start = atStart ? remote : boundary;
                var end = atStart ? boundary : remote;
                var a = atStart ? remotePosition : position;
                var b = atStart ? position : remotePosition;
                var edge = helper.CreateEdge(_root, start, end,
                    new Bezier4x3(a, math.lerp(a,b,1f/3), math.lerp(a,b,2f/3), b),
                    default, new PseudoRandomSeed(1), elevation);
                _spans.Add(new Snapshot { Owner = edge, StartNode = start, EndNode = end,
                    EdgeData = span.EdgeData, Network = span.Network, Pieces = span.Pieces, Visible = false });
            }
            ContinueAt(span.StartNode, true);
            ContinueAt(span.EndNode, false);
        }
        using var packer = new BridgePreviewComposition();
        foreach (var snapshot in _spans)
        {
            var startFlags = helper.PreviewNodeFlags(snapshot.Owner, snapshot.StartNode, _root,
                snapshot.EdgeData.m_Flags, true, environment.LeftHandTraffic);
            var endFlags = helper.PreviewNodeFlags(snapshot.Owner, snapshot.EndNode, _root,
                snapshot.EdgeData.m_Flags, false, environment.LeftHandTraffic);
            snapshot.StartPieces = packer.Compose(deck, startFlags, out snapshot.StartData, nodeComposition: true);
            snapshot.EndPieces = packer.Compose(deck, endFlags, out snapshot.EndData, nodeComposition: true);
            var edgeComposition = Composition(snapshot.Pieces, ref snapshot.EdgeData);
            var startComposition = Composition(snapshot.StartPieces, ref snapshot.StartData);
            var endComposition = Composition(snapshot.EndPieces, ref snapshot.EndData);
            manager.SetComponentData(snapshot.Owner, new Composition
                { m_Edge = edgeComposition, m_StartNode = startComposition, m_EndNode = endComposition });
        }
        await GenerationWork.Next("preview native object course composition");
        helper.InitializePreviewCompositions();
        await GenerationWork.Next("preview native object course geometry");
        _world.GetOrCreateSystemManaged<MovableNetworkGeometry>().Calculate(environment.Terrain, true);
        await helper.FinishPreviewNetworkAsync(environment.Terrain, _imports.Import(environment.Theme), environment.LeftHandTraffic,
            _imports.ImportConfiguration(environment.BuildingConfiguration));
        foreach (var snapshot in _spans)
        {
            var composition = manager.GetComponentData<Composition>(snapshot.Owner);
            snapshot.EdgeData = manager.GetComponentData<NetCompositionData>(composition.m_Edge);
            snapshot.StartData = manager.GetComponentData<NetCompositionData>(composition.m_StartNode);
            snapshot.EndData = manager.GetComponentData<NetCompositionData>(composition.m_EndNode);
            snapshot.Pieces = Packed(snapshot.Pieces, composition.m_Edge);
            snapshot.StartPieces = Packed(snapshot.StartPieces, composition.m_StartNode);
            snapshot.EndPieces = Packed(snapshot.EndPieces, composition.m_EndNode);
            snapshot.Edge = manager.GetComponentData<EdgeGeometry>(snapshot.Owner);
            snapshot.Start = manager.GetComponentData<StartNodeGeometry>(snapshot.Owner).m_Geometry;
            snapshot.End = manager.GetComponentData<EndNodeGeometry>(snapshot.Owner).m_Geometry;
        }
    }

    private BridgePreviewComposition.Piece[] Packed(BridgePreviewComposition.Piece[] source, Entity composition)
    {
        var buffer = _world.EntityManager.GetBuffer<NetCompositionPiece>(composition);
        var result = new BridgePreviewComposition.Piece[source.Length];
        for (var i = 0; i < source.Length; i++)
            result[i] = new BridgePreviewComposition.Piece(source[i].Prefab, buffer[i],
                source[i].SourcePath, source[i].RootSection, source[i].UnpackedComposition);
        return result;
    }

    public void Dispose() => _world.Dispose();

    internal BridgePreviewLanes.Lane[] Lanes(bool leftHandTraffic)
    {
        var manager = _world.EntityManager;
        var visibleOwners = new HashSet<Entity>(_nodes.Values);
        foreach (var span in _spans) if (span.Visible) visibleOwners.Add(span.Owner);
        var output = new List<BridgePreviewLanes.Lane>();
        using var query = manager.CreateEntityQuery(ComponentType.ReadOnly<Owner>(),
            ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<PrefabRef>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!visibleOwners.Contains(manager.GetComponentData<Owner>(entity).m_Owner)
                || manager.HasComponent<MasterLane>(entity) || manager.HasComponent<Game.Tools.Hidden>(entity)) continue;
            var reference = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (_imports.Original(reference) is not NetLaneGeometryPrefab prefab)
            {
                continue;
            }
            output.Add(new BridgePreviewLanes.Lane(prefab, manager.GetComponentData<Curve>(entity),
                manager.HasComponent<EdgeLane>(entity) ? manager.GetComponentData<EdgeLane>(entity) : null,
                BridgePreviewLanes.RejectedSubMeshes(manager, entity, leftHandTraffic),
                manager.HasComponent<NodeLane>(entity) ? manager.GetComponentData<NodeLane>(entity) : null,
                manager.GetComponentData<NetLaneData>(reference),
                manager.HasComponent<Elevation>(entity) ? manager.GetComponentData<Elevation>(entity) : null));
        }
        return output.ToArray();
    }

    internal ObjectSubObjectInfo[] Objects(Func<MovableNodeObjects.PreviewSelection> createSelection)
    {
        var manager = _world.EntityManager;
        var sourceWorld = World.DefaultGameObjectInjectionWorld;
        var prefabs = sourceWorld.GetExistingSystemManaged<PrefabSystem>();
        var rules = new Dictionary<Entity, IReadOnlyList<MovableRoadObjects.Rule>>();
        var output = new List<ObjectSubObjectInfo>();
        var visibleIntervals = new List<float2>();
        foreach (var span in _spans)
            if (span.Visible)
                visibleIntervals.Add(new float2(manager.GetComponentData<Node>(span.StartNode).m_Position.z,
                    manager.GetComponentData<Node>(span.EndNode).m_Position.z));
        using var environment = new MovableSamplingEnvironment(sourceWorld);
        foreach (var snapshot in _spans)
        {
            var composition = manager.GetComponentData<Composition>(snapshot.Owner);
            rules[composition.m_StartNode] = BridgePreviewPieceObjects.Compose(snapshot.StartPieces, snapshot.StartData);
            rules[composition.m_EndNode] = BridgePreviewPieceObjects.Compose(snapshot.EndPieces, snapshot.EndData);
        }
        foreach (var snapshot in _spans)
        {
            var edgeRules = BridgePreviewPieceObjects.Compose(snapshot.Pieces, snapshot.EdgeData);
            var selection = createSelection();
            var needs = _world.GetOrCreateSystemManaged<MovableRoadTrafficNeeds>();
            needs.Begin(snapshot.Owner, environment.LeftHandTraffic);
            var random = new Unity.Mathematics.Random(1);
            void Select(MovableRoadObjectPlacement.Candidate candidate, ref Unity.Mathematics.Random state)
            {
                var registered = prefabs.TryGetEntity(candidate.Prefab, out var entity);
                var context = needs.Calculate(registered && sourceWorld.EntityManager.HasComponent<TrafficSignData>(entity),
                    registered && sourceWorld.EntityManager.HasComponent<LaneDirectionData>(entity), candidate.Object.m_Position);
                var placement = selection(candidate, context, ref state);
                if (placement != null && (snapshot.Visible || BridgePreviewCourseDomain.Contains(
                    visibleIntervals, candidate.Object.m_Position.z))) output.Add(placement);
            }
            MovableRoadObjectPlacement.Visit(edgeRules, snapshot.Edge, snapshot.EdgeData, snapshot.Network,
                snapshot.StartData, snapshot.EndData, snapshot.Start, snapshot.End, ref random, Select);
        }
        foreach (var node in _nodes.Values)
        {
            var random = new Unity.Mathematics.Random(1);
            var result = _world.GetOrCreateSystemManaged<MovableNodeObjects>().Resolve(node, entity => rules[entity],
                entity => (ObjectPrefab)_imports.Original(entity), environment.Theme, environment.LeftHandTraffic,
                ref random, createSelection());
            output.AddRange(result.Placements);
        }
        return output.ToArray();
    }

}
