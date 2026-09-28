using BridgeBuilder.Runtime;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>
/// Read-only evidence for auxiliary rail seams. Never changes Owner, connections,
/// composition caches or geometry to hide a junction: those also control deletion
/// and routing. Log changed snapshots only, including native calculated end spans.
/// </summary>
public partial class BridgeRailSeamDiagnosticsSystem : GameSystemBase
{
    private EntityQuery _edges;
    private PrefabSystem _prefabs = null!;
    private readonly Dictionary<Entity, string> _snapshots = new();
    private int _frames;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        _edges = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Edge>(),
                ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Composition>() },
            None = new[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Hidden>() }
        });
        Enabled = false;
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose, mode);
        Enabled = false;
        _snapshots.Clear();
        _frames = 0;
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        Enabled = (mode & GameMode.Game) != 0;
    }

    protected override void OnUpdate()
    {
        if (_frames++ % 180 != 0) return;
        try
        {
            EntityManager.CompleteAllTrackedJobs();
            using var edges = _edges.ToEntityArray(Allocator.Temp);
            var live = new HashSet<Entity>();
            foreach (var entity in edges)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!_prefabs.TryGetPrefab<TrackPrefab>(prefab, out var track) || track == null) continue;
                var name = track.name;
                var auxiliary = (name.EndsWith("_Lower", StringComparison.Ordinal)
                    || name.EndsWith("_Upper", StringComparison.Ordinal))
                    && BridgeRegistration.IsPrefabName(name.Substring(0, name.Length - 6));
                if (!auxiliary && !BridgeRegistration.IsPrefabName(name)) continue;
                live.Add(entity);
                var edge = EntityManager.GetComponentData<Edge>(entity);
                var composition = EntityManager.GetComponentData<Composition>(entity);
                var text = new StringBuilder($"Rail seam audit: {name}, auxiliary={auxiliary}, edge={entity}, ");
                DescribeEdge(text, entity);
                DescribeNode(text, edge.m_Start, composition.m_StartNode);
                DescribeNode(text, edge.m_End, composition.m_EndNode);
                if (EntityManager.HasComponent<StartNodeGeometry>(entity))
                {
                    var middle = EntityManager.GetComponentData<StartNodeGeometry>(entity).m_Geometry.m_Middle;
                    text.Append(" startMiddle=").Append(middle.a).Append(" -> ").Append(middle.d);
                }
                if (EntityManager.HasComponent<EndNodeGeometry>(entity))
                {
                    var middle = EntityManager.GetComponentData<EndNodeGeometry>(entity).m_Geometry.m_Middle;
                    text.Append(" endMiddle=").Append(middle.a).Append(" -> ").Append(middle.d);
                }
                if (EntityManager.HasBuffer<NetCompositionLane>(composition.m_Edge))
                    foreach (var lane in EntityManager.GetBuffer<NetCompositionLane>(composition.m_Edge, true))
                        text.Append(" lane=").Append(lane.m_Position).Append('/').Append(lane.m_Flags)
                            .Append(" index=").Append(lane.m_Index).Append(" group=").Append(lane.m_Group)
                            .Append(" carriageway=").Append(lane.m_Carriageway);
                var snapshot = text.ToString();
                if (_snapshots.TryGetValue(entity, out var old) && old == snapshot) continue;
                _snapshots[entity] = snapshot;
                Mod.Log.Info(snapshot);
            }
            var stale = new List<Entity>();
            foreach (var entity in _snapshots.Keys)
                if (!live.Contains(entity)) stale.Add(entity);
            foreach (var entity in stale) _snapshots.Remove(entity);
        }
        catch (Exception exception)
        {
            Enabled = false;
            Mod.Log.Warn(exception, "Rail seam diagnostic stopped; no network data was modified.");
        }
    }

    private void DescribeNode(StringBuilder text, Entity node, Entity composition)
    {
        text.Append("; node=").Append(node);
        if (EntityManager.HasComponent<NetCompositionData>(composition))
        {
            var data = EntityManager.GetComponentData<NetCompositionData>(composition);
            text.Append(" flags=").Append(data.m_Flags.m_General)
                .Append(" width=").Append(data.m_Width).Append(" offset=").Append(data.m_NodeOffset);
        }
        if (!EntityManager.HasBuffer<ConnectedEdge>(node)) return;
        var connected = EntityManager.GetBuffer<ConnectedEdge>(node, true);
        text.Append(" connected=").Append(connected.Length);
        foreach (var item in connected)
        {
            text.Append(" [edge=").Append(item.m_Edge).Append(' ');
            DescribeEdge(text, item.m_Edge);
            text.Append(']');
        }
        // Composition flags alone cannot establish whether the node has a switch. Record the
        // actual path graph, for both independent and carried tracks, without changing any lane.
        if (!EntityManager.HasBuffer<Game.Net.SubLane>(node)) return;
        foreach (var item in EntityManager.GetBuffer<Game.Net.SubLane>(node, true))
        {
            var entity = item.m_SubLane;
            if (!EntityManager.HasComponent<Game.Net.TrackLane>(entity)
                || !EntityManager.HasComponent<Lane>(entity)
                || EntityManager.HasComponent<Deleted>(entity)) continue;
            var lane = EntityManager.GetComponentData<Lane>(entity);
            text.Append(" trackConnection=[").Append(entity).Append(' ')
                .Append(lane.m_StartNode.GetOwnerIndex()).Append(':')
                .Append(lane.m_StartNode.GetLaneIndex()).Append('@')
                .Append(lane.m_StartNode.GetCurvePos()).Append(" -> ")
                .Append(lane.m_EndNode.GetOwnerIndex()).Append(':')
                .Append(lane.m_EndNode.GetLaneIndex()).Append('@')
                .Append(lane.m_EndNode.GetCurvePos());
            if (EntityManager.HasComponent<NodeLane>(entity))
            {
                var state = EntityManager.GetComponentData<NodeLane>(entity);
                text.Append(" nodeFlags=").Append(state.m_Flags)
                    .Append(" shared=").Append(state.m_SharedStartCount)
                    .Append('/').Append(state.m_SharedEndCount);
            }
            text.Append(']');
        }
    }

    private void DescribeEdge(StringBuilder text, Entity entity)
    {
        if (EntityManager.HasComponent<PrefabRef>(entity))
        {
            var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (_prefabs.TryGetPrefab<NetPrefab>(prefab, out var net) && net != null)
                text.Append("prefab=").Append(net.name);
        }
        var seen = new HashSet<Entity>();
        while (EntityManager.HasComponent<Owner>(entity) && seen.Add(entity))
        {
            entity = EntityManager.GetComponentData<Owner>(entity).m_Owner;
            text.Append(" owner=").Append(entity);
            if (EntityManager.HasComponent<Edge>(entity))
            {
                var edge = EntityManager.GetComponentData<Edge>(entity);
                text.Append(" endpoints=").Append(edge.m_Start).Append('/').Append(edge.m_End);
            }
        }
        if (EntityManager.HasComponent<PrefabRef>(entity))
        {
            var root = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (_prefabs.TryGetPrefab<NetPrefab>(root, out var net) && net != null)
            {
                text.Append(" topPrefab=").Append(net.name);
                if (net.TryGet<AuxiliaryNets>(out var auxiliary))
                    text.Append(" linkEndOffsets=").Append(auxiliary.m_LinkEndOffsets);
            }
        }
    }
}
