using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using BridgeBuilder.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;
using Unity.Collections;
using SubLane = Game.Net.SubLane;
using TrackLane = Game.Net.TrackLane;

namespace BridgeBuilder.Systems;

/// <summary>Bounded, read-only verification of real lanes AFTER the native update pipeline.</summary>
public partial class BridgeRailSeamAuditSystem : GameSystemBase
{
    private readonly Dictionary<Entity, int> _pending = new();
    private readonly Dictionary<Entity, string> _reported = new();
    private int _reports;
    private int _nextScanFrame;
    private EntityQuery _candidates;
    private PrefabSystem? _prefabs;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        _candidates = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Owner>(),
                ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Composition>() },
            None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() }
        });
    }

    internal void Observe(Entity node)
    {
        if (_reports >= 24 || _pending.ContainsKey(node) || _pending.Count >= 128) return;
        _pending[node] = UnityEngine.Time.frameCount;
    }

    internal void Restart(string reason)
    {
        _pending.Clear();
        _reported.Clear();
        _reports = 0;
        _nextScanFrame = 0;
        BridgeRailSeamScope.ResetDiagnostics();
        Mod.Log.Info("Rail seam audit restarted: " + reason);
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose, mode);
        _pending.Clear();
        _reported.Clear();
        _reports = 0;
        _nextScanFrame = 0;
        BridgeRailSeamScope.ResetDiagnostics();
        BridgeRailSeamPatch.EnsureCompatiblePipelines(forceDiscovery: true);
    }

    protected override void OnUpdate()
    {
        BridgeRailSeamPatch.EnsureCompatiblePipelines();
        // Independently inspect placed bridges, even when the repair's eligibility predicate never
        // accepts one. The old audit was only queued AFTER a successful repair and was blind to
        // exactly the failure reported on 2026-09-30. This path never changes ECS data.
        var scan = _reports < 24 && UnityEngine.Time.frameCount >= _nextScanFrame;
        if (_pending.Count == 0 && !scan) return;
        try
        {
            EntityManager.CompleteAllTrackedJobs();
            if (scan)
            {
                _nextScanFrame = UnityEngine.Time.frameCount + 120;
                using var candidates = _candidates.ToEntityArray(Allocator.Temp);
                foreach (var entity in candidates)
                {
                    var reference = EntityManager.GetComponentData<PrefabRef>(entity);
                    if (_prefabs == null || !_prefabs.TryGetPrefab<TrackPrefab>(reference, out var prefab)
                        || prefab == null || !prefab.name.EndsWith("_Lower", StringComparison.Ordinal)
                        || !BridgeAssetInfo.IsPrefabName(prefab.name.Substring(0, prefab.name.Length - 6))) continue;
                    var edge = EntityManager.GetComponentData<Edge>(entity);
                    Observe(edge.m_Start);
                    Observe(edge.m_End);
                }
            }
            foreach (var item in _pending.ToArray())
            {
                if (item.Value >= UnityEngine.Time.frameCount) continue;
                _pending.Remove(item.Key);
                var node = item.Key;
                if (!EntityManager.Exists(node) || EntityManager.HasComponent<Temp>(node)
                    || EntityManager.HasComponent<Deleted>(node) || !EntityManager.HasBuffer<SubLane>(node)) continue;
                var count = 0;
                var text = new StringBuilder();
                if (EntityManager.HasBuffer<ConnectedEdge>(node))
                {
                    foreach (var connection in EntityManager.GetBuffer<ConnectedEdge>(node, true))
                    {
                        var edge = connection.m_Edge;
                        if (!EntityManager.HasComponent<Edge>(edge) || !EntityManager.HasComponent<PrefabRef>(edge)) continue;
                        var ends = EntityManager.GetComponentData<Edge>(edge);
                        var prefabRef = EntityManager.GetComponentData<PrefabRef>(edge);
                        _prefabs!.TryGetPrefab<NetPrefab>(prefabRef, out var prefab);
                        var owner = EntityManager.HasComponent<Owner>(edge)
                            ? EntityManager.GetComponentData<Owner>(edge).m_Owner : Entity.Null;
                        text.Append($" [edge={edge}, prefab={prefab?.name}, readOnly={prefab?.isReadOnly}, "
                            + $"ends={ends.m_Start}/{ends.m_End}, owner={owner}, ownerHasOwner={EntityManager.HasComponent<Owner>(owner)}");
                        if (EntityManager.HasComponent<PrefabRef>(owner))
                        {
                            _prefabs.TryGetPrefab<NetPrefab>(EntityManager.GetComponentData<PrefabRef>(owner), out var parent);
                            text.Append($", parentPrefab={parent?.name}, parentReadOnly={parent?.isReadOnly}");
                            if (parent != null && parent.TryGet<AuxiliaryNets>(out var auxiliary)
                                && auxiliary.m_AuxiliaryNets != null)
                                foreach (var deck in auxiliary.m_AuxiliaryNets)
                                    if (deck != null)
                                        text.Append($", auxiliary={deck.m_Prefab?.name}@{deck.m_Position}, sameReference={deck.m_Prefab == prefab}");
                        }
                        if (EntityManager.HasComponent<Composition>(edge))
                        {
                            var composition = EntityManager.GetComponentData<Composition>(edge);
                            var atNode = ends.m_End == node ? composition.m_EndNode : composition.m_StartNode;
                            if (EntityManager.HasComponent<NetCompositionData>(atNode))
                                text.Append($", nodeFlags={EntityManager.GetComponentData<NetCompositionData>(atNode).m_Flags.m_General}");
                            if (EntityManager.HasComponent<NetCompositionData>(composition.m_Edge))
                                text.Append($", edgeState={EntityManager.GetComponentData<NetCompositionData>(composition.m_Edge).m_State}");
                            if (EntityManager.HasBuffer<NetCompositionLane>(composition.m_Edge))
                                foreach (var rail in EntityManager.GetBuffer<NetCompositionLane>(composition.m_Edge, true))
                                    if ((rail.m_Flags & LaneFlags.Track) != 0)
                                        text.Append($", rail={rail.m_Lane}/{rail.m_Position}/{rail.m_Flags}");
                        }
                        if (ends.m_Start == node && EntityManager.HasComponent<StartNodeGeometry>(edge))
                        {
                            var middle = EntityManager.GetComponentData<StartNodeGeometry>(edge).m_Geometry.m_Middle;
                            text.Append($", middle={middle.a}->{middle.d}");
                        }
                        if (ends.m_End == node && EntityManager.HasComponent<EndNodeGeometry>(edge))
                        {
                            var middle = EntityManager.GetComponentData<EndNodeGeometry>(edge).m_Geometry.m_Middle;
                            text.Append($", middle={middle.a}->{middle.d}");
                        }
                        text.Append(']');
                    }
                }
                foreach (var entry in EntityManager.GetBuffer<SubLane>(node, true))
                {
                    var lane = entry.m_SubLane;
                    if (!EntityManager.HasComponent<TrackLane>(lane) || !EntityManager.HasComponent<Lane>(lane)
                        || EntityManager.HasComponent<Deleted>(lane)) continue;
                    var data = EntityManager.GetComponentData<Lane>(lane);
                    count++;
                    if (count <= 16)
                        text.Append(" [").Append(data.m_StartNode.GetOwnerIndex()).Append(':')
                            .Append(data.m_StartNode.GetLaneIndex()).Append(" -> ")
                            .Append(data.m_EndNode.GetOwnerIndex()).Append(':')
                            .Append(data.m_EndNode.GetLaneIndex()).Append(']');
                }
                var result = $"node={node}, trackConnections={count}{text}";
                if (_reported.TryGetValue(node, out var previous) && previous == result) continue;
                if (_reports++ >= 24) { _pending.Clear(); break; }
                _reported[node] = result;
                Mod.Log.Info("Rail seam post-lane audit: " + result);
            }
        }
        catch (Exception exception)
        {
            _pending.Clear();
            _reports = 24;
            Mod.Log.Warn(exception, "Rail seam audit stopped; no network data was modified.");
        }
    }
}
