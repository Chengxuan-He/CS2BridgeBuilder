using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using SubNet = Game.Net.SubNet;

namespace BridgeBuilder.Runtime;

/// <summary>Read-only topology proof, independent of the geometry job's write batch.</summary>
internal sealed class BridgeRailSeamScope
{
    internal readonly HashSet<(Entity Edge, Entity Node)> Seams = new();
    internal readonly List<int> Indices = new();
    internal string Evidence = string.Empty;
    private readonly EntityManager _em;
    private readonly PrefabSystem? _prefabs;
    private readonly ComponentLookup<Edge> _edges;
    private readonly ComponentLookup<Owner> _owners;
    private readonly ComponentLookup<PrefabRef> _refs;
    private readonly ComponentLookup<Composition> _compositions;
    private readonly ComponentLookup<NetCompositionData> _data;
    private readonly BufferLookup<NetCompositionLane> _lanes;
    private readonly BufferLookup<ConnectedEdge> _connected;
    private readonly BufferLookup<SubNet> _subNets;
    private readonly ComponentLookup<Temp> _temp;
    private readonly ComponentLookup<Hidden> _hidden;
    private readonly Dictionary<Entity, Entity> _allowed = new();
    private readonly Dictionary<Entity, int> _indices = new();
    private static readonly HashSet<string> Reported = new();

    internal static void ResetDiagnostics() => Reported.Clear();

    private void Rejected(Entity edge, Entity node, string reason, string details = "")
    {
        if (Reported.Count >= 80 || !_refs.TryGetComponent(edge, out var reference)
            || _prefabs == null || !_prefabs.TryGetPrefab<NetPrefab>(reference, out var prefab)
            || prefab == null || !prefab.name.EndsWith("_Lower", StringComparison.Ordinal)
            || !BridgeAssetInfo.IsPrefabName(prefab.name.Substring(0, prefab.name.Length - 6))) return;
        var temporary = _temp.HasComponent(edge);
        var key = prefab.name + "/" + temporary + "/" + reason;
        if (!Reported.Add(key)) return;
        Mod.Log.Info($"Rail seam excluded: prefab={prefab.name}, temporary={temporary}, edge={edge}, "
            + $"node={node}, reason={reason}; {details}");
    }

    // Reflection reads named, exactly typed fields of the real native job. Never reproduce its
    // struct layout: adding/reordering a game field must not cause an out-of-bounds native write.
    private static readonly (string Name, Type Type)[] Fields =
    {
        ("m_EdgeData", typeof(ComponentLookup<Edge>)),
        ("m_OwnerData", typeof(ComponentLookup<Owner>)),
        ("m_PrefabRefDataFromEntity", typeof(ComponentLookup<PrefabRef>)),
        ("m_CompositionDataFromEntity", typeof(ComponentLookup<Composition>)),
        ("m_PrefabCompositionData", typeof(ComponentLookup<NetCompositionData>)),
        ("m_PrefabCompositionLanes", typeof(BufferLookup<NetCompositionLane>)),
        ("m_Edges", typeof(BufferLookup<ConnectedEdge>)),
        ("m_SubNets", typeof(BufferLookup<SubNet>)),
        ("m_TempData", typeof(ComponentLookup<Temp>)),
        ("m_HiddenData", typeof(ComponentLookup<Hidden>)),
    };

    // Node Controller replaces (and disables) Game.Net.GeometrySystem. Its inspected job has
    // the same lookup contracts, with M-prefixed names. Bind fields, never copy its struct ABI.
    internal static FieldInfo? Field(Type job, string nativeName) => job.GetField(
        job.DeclaringType?.FullName == BridgeRailSeamPatch.NodeControllerSystemName
            ? "M" + nativeName.Substring(2) : nativeName);

    internal static bool Supports(Type job) => Fields.All(f => Field(job, f.Name)?.FieldType == f.Type);
    private static T Read<T>(object job, string field) => (T)Field(job.GetType(), field)!.GetValue(job)!;

    internal BridgeRailSeamScope(GameSystemBase system, object job, NativeList<Entity> updated)
    {
        _em = system.EntityManager;
        _prefabs = system.World.GetExistingSystemManaged<PrefabSystem>();
        _edges = Read<ComponentLookup<Edge>>(job, "m_EdgeData");
        _owners = Read<ComponentLookup<Owner>>(job, "m_OwnerData");
        _refs = Read<ComponentLookup<PrefabRef>>(job, "m_PrefabRefDataFromEntity");
        _compositions = Read<ComponentLookup<Composition>>(job, "m_CompositionDataFromEntity");
        _data = Read<ComponentLookup<NetCompositionData>>(job, "m_PrefabCompositionData");
        _lanes = Read<BufferLookup<NetCompositionLane>>(job, "m_PrefabCompositionLanes");
        _connected = Read<BufferLookup<ConnectedEdge>>(job, "m_Edges");
        _subNets = Read<BufferLookup<SubNet>>(job, "m_SubNets");
        _temp = Read<ComponentLookup<Temp>>(job, "m_TempData");
        _hidden = Read<ComponentLookup<Hidden>>(job, "m_HiddenData");
        if (_prefabs == null) return;
        for (var i = 0; i < updated.Length; i++) _indices[updated[i]] = i;
        var selected = new HashSet<int>();
        var inspected = new HashSet<Entity>();
        foreach (var entity in updated)
        {
            InspectLower(entity, inspected, selected);
            // LinkAuxOffsets reads the parent's and siblings' offsets even when only
            // the upper deck is being written. Discover that read set without adding
            // any of its members to the downstream native write batch.
            var root = _owners.TryGetComponent(entity, out var owner) && _edges.HasComponent(owner.m_Owner)
                ? owner.m_Owner : entity;
            if (_subNets.TryGetBuffer(root, out var children))
                foreach (var child in children) InspectLower(child.m_SubNet, inspected, selected);
        }
        Indices.AddRange(selected.OrderBy(i => i));
    }

    private void InspectLower(Entity entity, HashSet<Entity> inspected, HashSet<int> selected)
    {
        if (!inspected.Add(entity)) return;
        if (!TryLower(entity, out var parent, out var prefab))
        {
            Rejected(entity, Entity.Null, "lower-ownership-or-prefab",
                $"owner={(_owners.TryGetComponent(entity, out var owner) ? owner.m_Owner : Entity.Null)}, "
                + $"hidden={_hidden.HasComponent(entity)}, deleted={_em.HasComponent<Deleted>(entity)}");
            return;
        }
        var edge = _edges[entity];
        Inspect(entity, edge.m_Start, parent, prefab, selected);
        Inspect(entity, edge.m_End, parent, prefab, selected);
    }

    private bool Live(Entity entity) => entity != Entity.Null && _em.Exists(entity)
        // Native geometry also updates hidden originals. Skipping them leaves native
        // crossover geometry behind when ToolClear removes Hidden without adding Updated.
        // EdgeIterator, rather than this predicate, chooses the preview/original view.
        && !_em.HasComponent<Deleted>(entity)
        && (!_temp.TryGetComponent(entity, out var temp) || (temp.m_Flags & TempFlags.Delete) == 0);

    private bool TryLower(Entity edge, out Entity parent, out Entity prefab)
    {
        parent = prefab = Entity.Null;
        if (_prefabs == null || !Live(edge) || !_edges.HasComponent(edge) || !_owners.TryGetComponent(edge, out var owner)
            || !Live(owner.m_Owner) || !_edges.HasComponent(owner.m_Owner)
            || _owners.HasComponent(owner.m_Owner) || !_refs.TryGetComponent(edge, out var reference)
            || !_refs.TryGetComponent(owner.m_Owner, out var parentRef)) return false;
        parent = owner.m_Owner;
        prefab = reference.m_Prefab;
        if (!_allowed.TryGetValue(prefab, out var root))
        {
            root = Entity.Null;
            if (_prefabs.TryGetPrefab<TrackPrefab>(prefab, out var lower) && lower != null
                && !lower.isBuiltin && !lower.isReadOnly
                && _prefabs.TryGetPrefab<NetGeometryPrefab>(parentRef.m_Prefab, out var upper) && upper != null
                && !upper.isBuiltin && !upper.isReadOnly && BridgeAssetInfo.IsPrefabName(upper.name)
                && lower.name == upper.name + "_Lower" && upper.TryGet<AuxiliaryNets>(out var auxiliary)
                && auxiliary.m_AuxiliaryNets != null
                && auxiliary.m_AuxiliaryNets.Any(a => a != null && a.m_Prefab == lower && a.m_Position.y < 0f))
                root = parentRef.m_Prefab;
            _allowed[prefab] = root;
        }
        return root != Entity.Null && root == parentRef.m_Prefab;
    }

    private void Inspect(Entity first, Entity node, Entity parentA, Entity lowerPrefab, HashSet<int> replay)
    {
        if (Seams.Contains((first, node)) || !Live(node) || !_connected.HasBuffer(node)) return;
        // Use the game's own iterator, including Temp/Hidden replacement rules. Counting raw
        // ConnectedEdge entries during construction would confuse a preview with its original.
        var iterator = new EdgeIterator(first, node, _connected, _edges, _temp, _hidden, true);
        var connections = new List<EdgeIteratorValue>();
        while (iterator.GetNext(out var next))
        {
            if (next.m_Middle || !Live(next.m_Edge) || connections.Count == 2)
            {
                Rejected(first, node, "extra-deleted-or-middle-connection", $"other={next.m_Edge}, middle={next.m_Middle}");
                return;
            }
            connections.Add(next);
        }
        if (connections.Count != 2 || connections[0].m_Edge == connections[1].m_Edge
            || !connections.Any(e => e.m_Edge == first))
        {
            Rejected(first, node, "not-two-endpoint-edges", $"count={connections.Count}");
            return;
        }
        var a = connections.First(e => e.m_Edge == first);
        var b = connections.First(e => e.m_Edge != first);
        if (!TryLower(b.m_Edge, out var parentB, out var otherPrefab) || lowerPrefab != otherPrefab || parentA == parentB)
        {
            Rejected(first, node, "different-lower-prefab-or-same-owner", $"other={b.m_Edge}, parents={parentA}/{parentB}");
            return;
        }
        if (!ParentsContinue(parentA, parentB))
        {
            Rejected(first, node, "parent-topology", $"parents={parentA}/{parentB}, "
                + $"endpoints={_edges[parentA].m_Start}/{_edges[parentA].m_End} + {_edges[parentB].m_Start}/{_edges[parentB].m_End}");
            return;
        }
        if (!RailComposition(a, out var compA) || !RailComposition(b, out var compB))
        {
            Rejected(first, node, "not-pure-rail-seam", $"other={b.m_Edge}");
            return;
        }
        if (!SameTracks(compA.m_Edge, compB.m_Edge, a.m_End == b.m_End))
        {
            Rejected(first, node, "track-layout-mismatch", $"compositions={compA.m_Edge}/{compB.m_Edge}, reverse={a.m_End == b.m_End}");
            return;
        }
        // Topology is a read dependency, not a demand that all four edges are Updated.
        // Prove both sides, but calculate ONLY members requested by the native pipeline.
        var members = new[] { first, b.m_Edge, parentA, parentB };
        Seams.Add((first, node));
        // Share the proof only within the same node view. A temporary node can
        // borrow an edge from its original, whose own view may be a real junction.
        // If that original is also read by this batch, InspectLower proves it separately.
        var otherEdge = _edges[b.m_Edge];
        var otherNode = b.m_End ? otherEdge.m_End : otherEdge.m_Start;
        if (otherNode == node) Seams.Add((b.m_Edge, otherNode));
        foreach (var member in members)
            if (_indices.TryGetValue(member, out var index)) replay.Add(index);
        if (Evidence.Length == 0)
            Evidence = $"node={node}, lower={first}/{b.m_Edge}, parents={parentA}/{parentB}, prefab={lowerPrefab}";
    }

    private bool ParentsContinue(Entity a, Entity b)
    {
        var ea = _edges[a];
        var eb = _edges[b];
        var shared = new HashSet<Entity>();
        var aStart = OriginalNode(ea.m_Start);
        var aEnd = OriginalNode(ea.m_End);
        var bStart = OriginalNode(eb.m_Start);
        var bEnd = OriginalNode(eb.m_End);
        if (aStart == Entity.Null || aEnd == Entity.Null || bStart == Entity.Null || bEnd == Entity.Null) return false;
        if (aStart == bStart || aStart == bEnd) shared.Add(aStart);
        if (aEnd == bStart || aEnd == bEnd) shared.Add(aEnd);
        if (shared.Count != 1 || _refs[a].m_Prefab != _refs[b].m_Prefab) return false;
        var common = shared.First();
        var nodeA = aStart == common ? ea.m_Start : ea.m_End;
        var nodeB = bStart == common ? eb.m_Start : eb.m_End;
        // Read the temporary view when one exists: its iterator includes the original
        // node and filters replaced edges, while a permanent node sees only originals.
        var useB = !_temp.HasComponent(nodeA) && _temp.HasComponent(nodeB);
        var node = useB ? nodeB : nodeA;
        if (!Live(node) || !_connected.HasBuffer(node)) return false;
        var iterator = new EdgeIterator(useB ? b : a, node, _connected, _edges, _temp, _hidden, true);
        var seen = new HashSet<Entity>();
        while (iterator.GetNext(out var next))
        {
            if (next.m_Middle || !Live(next.m_Edge) || (next.m_Edge != a && next.m_Edge != b)
                || !seen.Add(next.m_Edge)) return false;
        }
        return seen.Count == 2;
    }

    private Entity OriginalNode(Entity node)
    {
        var seen = new HashSet<Entity>();
        while (_temp.TryGetComponent(node, out var temp) && temp.m_Original != Entity.Null)
        {
            if (!seen.Add(node)) return Entity.Null;
            node = temp.m_Original;
        }
        return Live(node) ? node : Entity.Null;
    }

    private bool RailComposition(EdgeIteratorValue edge, out Composition composition)
    {
        composition = default;
        if (!_compositions.TryGetComponent(edge.m_Edge, out composition)
            || !_data.TryGetComponent(composition.m_Edge, out var data)
            || !_data.TryGetComponent(edge.m_End ? composition.m_EndNode : composition.m_StartNode, out var node)) return false;
        const CompositionFlags.General blocked = CompositionFlags.General.Intersection
            | CompositionFlags.General.LevelCrossing | CompositionFlags.General.Roundabout | CompositionFlags.General.DeadEnd;
        return (node.m_Flags.m_General & blocked) == 0
            && (data.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) == 0
            && (data.m_State & (CompositionState.HasForwardTrackLanes | CompositionState.HasBackwardTrackLanes)) != 0;
    }

    private bool SameTracks(Entity a, Entity b, bool reverse)
    {
        if (!_lanes.HasBuffer(a) || !_lanes.HasBuffer(b)) return false;
        var left = _lanes[a];
        var right = _lanes[b];
        var matched = new HashSet<int>();
        var count = 0;
        foreach (var lane in left)
        {
            if ((lane.m_Flags & LaneFlags.Track) == 0) continue;
            count++;
            var found = false;
            for (var j = 0; j < right.Length; j++)
            {
                var other = right[j];
                if (matched.Contains(j) || other.m_Lane != lane.m_Lane) continue;
                var position = other.m_Position;
                var flags = other.m_Flags;
                if (reverse) { position.x = -position.x; position.z = -position.z; flags ^= LaneFlags.Invert; }
                if (!position.Equals(lane.m_Position) || flags != lane.m_Flags) continue;
                matched.Add(j);
                found = true;
                break;
            }
            if (!found) return false;
        }
        var rightCount = 0;
        foreach (var lane in right) if ((lane.m_Flags & LaneFlags.Track) != 0) rightCount++;
        return count >= 2 && count == rightCount;
    }

    internal readonly struct GeometrySnapshot
    {
        private readonly Entity _entity;
        private readonly EdgeGeometry _edge;
        private readonly StartNodeGeometry _start;
        private readonly EndNodeGeometry _end;
        internal GeometrySnapshot(EntityManager em, Entity entity)
        {
            _entity = entity;
            _edge = em.GetComponentData<EdgeGeometry>(entity);
            _start = em.GetComponentData<StartNodeGeometry>(entity);
            _end = em.GetComponentData<EndNodeGeometry>(entity);
        }
        internal void Restore(EntityManager em)
        {
            em.SetComponentData(_entity, _edge);
            em.SetComponentData(_entity, _start);
            em.SetComponentData(_entity, _end);
        }
    }
}
