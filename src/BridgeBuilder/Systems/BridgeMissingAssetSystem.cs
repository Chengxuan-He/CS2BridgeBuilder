using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.Localization;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>Repair only obsolete Bridge Builder networks, once per loaded city.</summary>
public partial class BridgeMissingAssetSystem : GameSystemBase
{
    private PrefabSystem _prefabs = null!;
    private EntityQuery _networks;
    private BridgeInstanceRemoval? _removal;
    private bool _scan;
    private int _missingCount;
    private DateTime _started;
    private bool _timeoutReported;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        _networks = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<PrefabRef>() },
            Any = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Node>() },
            None = new[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<NetCompositionData>(), ComponentType.ReadOnly<Deleted>() },
        });
        Enabled = false;
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose, mode);
        Enabled = false;
        _removal = null;
        _scan = false;
        _missingCount = 0;
        _timeoutReported = false;
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        // PrefabSystem has now mapped saved IDs to live or obsolete prefab entities.
        _removal = null;
        _missingCount = 0;
        _scan = (mode & GameMode.Game) != 0;
        Enabled = _scan;
        // Do not leave obsolete geometry alive until the first UI frame.
        if (_scan) OnUpdate();
    }

    protected override void OnUpdate()
    {
        try
        {
            if (_scan)
            {
                _scan = false;
                Scan();
            }
            if (_removal == null) { Enabled = false; return; }
            // Catch children reconstructed by native loading/deletion systems as well.
            _removal.IncludeOwnedEntities(EntityManager);
            _removal.Apply(EntityManager);
            // Do not claim success until native deletion has processed topology and children.
            if (!_removal.IsComplete(EntityManager))
            {
                if (!_timeoutReported && (DateTime.UtcNow - _started).TotalSeconds >= 30)
                {
                    _timeoutReported = true;
                    Mod.Log.Critical("Missing bridge cleanup has not completed after 30 seconds; "
                        + $"tracking {_removal.DeletedEntities.Count} network and owned entities. Save repair is incomplete.");
                }
                return;
            }
            var bindings = GameManager.instance?.userInterface?.appBindings;
            if (bindings == null) return;
            Mod.Log.Info($"Missing bridge cleanup completed: {_missingCount} bridge(s), "
                + $"{_removal.DeletedEntities.Count} network and owned entities removed.");
            bindings.ShowMessageDialog(new MessageDialog(
                LocalizedString.Value("Bridge Builder"),
                LocalizedString.Value(RuntimeUiText.Get("MissingBridgesRemoved", _missingCount)),
                LocalizedString.Value("OK")), _ => { });
            _removal = null;
            Enabled = false;
        }
        catch (Exception exception)
        {
            // Never retry a partially applied destructive operation every frame.
            Enabled = false;
            Mod.Log.Critical(exception, "Missing bridge save repair failed; the original save file was not modified.");
            Mod.ShowMessage("Bridge Builder", RuntimeUiText.Get("MissingBridgesRepairFailed"));
        }
    }

    private void Scan()
    {
        EntityManager.CompleteAllTrackedJobs();
        var missing = new HashSet<Entity>();
        var checkedPrefabs = new HashSet<Entity>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        using (var entities = _networks.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!checkedPrefabs.Add(prefab) || !EntityManager.HasComponent<PrefabData>(prefab)) continue;
                var data = EntityManager.GetComponentData<PrefabData>(prefab);
                if (data.m_Index >= 0) continue; // Live bridges are never removed, even without a registry row.
                var id = _prefabs.GetObsoleteID(data);
                var name = id.GetName();
                if (!TryBridgeName(name, out var bridge)) continue;
                // Restrict to network prefab types, not a same-named object or composition.
                var type = id.ToUrlSegment().Split('/')[0];
                if (type != nameof(RoadPrefab) && type != nameof(TrackPrefab)
                    && type != nameof(PathwayPrefab) && type != nameof(NetGeometryPrefab)) continue;
                missing.Add(prefab);
                names.Add(bridge);
                Mod.Log.Info($"Removing missing saved Bridge Builder network: {id}");
            }
        }
        if (missing.Count == 0) return;
        var plan = BridgeInstanceRemoval.Collect(EntityManager, missing);
        // Saved towers may have lost their Owner/SubObject links. Only obsolete static
        // objects with an exact missing bridge ID are eligible, never shared source assets.
        using (var objects = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Objects.Object>() },
            None = new[] { ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<Temp>() },
        }))
        using (var entities = objects.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!EntityManager.HasComponent<PrefabData>(prefab)) continue;
                var data = EntityManager.GetComponentData<PrefabData>(prefab);
                if (data.m_Index >= 0) continue;
                var id = _prefabs.GetObsoleteID(data);
                if (id.ToUrlSegment().Split('/')[0] != nameof(StaticObjectPrefab)) continue;
                var name = id.GetName();
                foreach (var bridge in names)
                {
                    var marker = "-" + bridge;
                    var index = name.IndexOf(marker, StringComparison.Ordinal);
                    if (index < 0) continue;
                    var end = index + marker.Length;
                    if (end != name.Length && name[end] != ' ') continue;
                    plan.DeletedEntities.Add(entity);
                    Mod.Log.Info($"Removing missing saved Bridge Builder object: {id}");
                    break;
                }
            }
        // Shared junctions survive. Stop them retaining the obsolete bridge prefab when a
        // surviving, valid road can provide their native node prefab instead.
        foreach (var node in plan.UpdatedEntities)
        {
            if (!EntityManager.HasComponent<Node>(node) || !EntityManager.HasComponent<PrefabRef>(node)
                || !missing.Contains(EntityManager.GetComponentData<PrefabRef>(node).m_Prefab)
                || !EntityManager.HasBuffer<ConnectedEdge>(node)) continue;
            foreach (var connection in EntityManager.GetBuffer<ConnectedEdge>(node, true))
            {
                var edge = connection.m_Edge;
                if (!EntityManager.Exists(edge) || plan.DeletedEntities.Contains(edge)
                    || EntityManager.HasComponent<Deleted>(edge) || !EntityManager.HasComponent<PrefabRef>(edge)) continue;
                var replacement = EntityManager.GetComponentData<PrefabRef>(edge);
                if (!EntityManager.HasComponent<PrefabData>(replacement.m_Prefab)
                    || EntityManager.GetComponentData<PrefabData>(replacement.m_Prefab).m_Index < 0) continue;
                EntityManager.SetComponentData(node, replacement);
                break;
            }
        }
        _missingCount = names.Count;
        _removal = plan;
        _started = DateTime.UtcNow;
        plan.IncludeOwnedEntities(EntityManager);
        Mod.Log.Info($"Missing bridge cleanup scheduled: {names.Count} bridge(s), "
            + $"{plan.DeletedEntities.Count} network and owned entities, {plan.UpdatedEntities.Count} surviving updates.");
        plan.Apply(EntityManager);
    }

    internal static bool TryBridgeName(string? name, out string bridge)
    {
        bridge = name ?? string.Empty;
        if (bridge.EndsWith("_Lower", StringComparison.Ordinal)
            || bridge.EndsWith("_Upper", StringComparison.Ordinal))
            bridge = bridge.Substring(0, bridge.Length - 6);
        // Do not accept r{uuid} (Road Builder), tmp previews, or arbitrary suffix matches.
        return BridgeRegistration.IsPrefabName(bridge);
    }
}
