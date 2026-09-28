using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Colossal.Serialization.Entities;
using CS2Mods.Shared.Infrastructure;
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
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>Remove missing or failed Bridge Builder networks using native topology deletion.</summary>
public partial class BridgeMissingAssetSystem : GameSystemBase
{
    private PrefabSystem _prefabs = null!;
    private EntityQuery _networks;
    private BridgeInstanceRemoval? _removal;
    private bool _scan;
    private int _missingCount;
    private DateTime _started;
    private bool _timeoutReported;
    private readonly HashSet<string> _failedBridges = new(StringComparer.Ordinal);
    private readonly HashSet<Entity> _failedNetworks = new();
    private int _failureRevision;
    private bool _inspectLoaded;
    private readonly HashSet<string> _handledFailures = new(StringComparer.Ordinal);

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
        _failedBridges.Clear();
        _failedNetworks.Clear();
        _handledFailures.Clear();
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        // PrefabSystem has now mapped saved IDs to live or obsolete prefab entities.
        _removal = null;
        _missingCount = 0;
        _scan = (mode & GameMode.Game) != 0;
        _inspectLoaded = _scan;
        Enabled = _scan;
        // Do not leave obsolete geometry alive until the first UI frame.
        if (_scan) OnUpdate();
    }

    protected override void OnUpdate()
    {
        try
        {
            if (_removal == null && _failureRevision != BridgeLoadFailures.Revision) _scan = true;
            if (_scan)
            {
                _scan = false;
                _failureRevision = BridgeLoadFailures.Revision;
                Scan();
            }
            if (_removal == null) return;
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
            if (BridgeInstanceRemoval.HasPlacedReferences(EntityManager, _failedNetworks)) return;
            // Keep backing assets and prefab indices alive until native network deletion
            // has completed. In particular, never call PrefabSystem.RemovePrefab here.
            var generator = World.GetOrCreateSystemManaged<BridgeGenerationSystem>();
            foreach (var bridge in _failedBridges)
                if (!generator.RemoveInvalidBridgeAfterLoad(bridge))
                {
                    Mod.Log.Warn($"Invalid bridge '{bridge}' still has protected asset references; cleanup stopped.");
                    Enabled = false;
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
            _handledFailures.UnionWith(_failedBridges);
            _failedBridges.Clear();
            _failedNetworks.Clear();
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
        var loaded = PrefabCatalog.GetAll(_prefabs).OfType<NetGeometryPrefab>().ToArray();
        foreach (var failed in BridgeLoadFailures.Prefabs())
            if (BridgeLoadFailures.TryOwner(failed.name, out var owner)
                && !_handledFailures.Contains(owner)) names.Add(owner);
        // Only a completed city load is a readiness boundary for a whole-catalogue
        // validation. At runtime other mods may still be registering new prefabs.
        foreach (var net in _inspectLoaded ? loaded : Array.Empty<NetGeometryPrefab>())
        {
            if (!TryBridgeName(net.name, out var bridge) || net.isBuiltin || net.isReadOnly) continue;
            if (!IsInvalid(net, out var reason)) continue;
            names.Add(bridge);
            Mod.Log.Warn($"Invalid Bridge Builder prefab '{net.name}': {reason}; removing its bridge UUID group.");
        }
        _inspectLoaded = false;
        using (var entities = _networks.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!checkedPrefabs.Add(prefab) || !EntityManager.HasComponent<PrefabData>(prefab)) continue;
                var data = EntityManager.GetComponentData<PrefabData>(prefab);
                if (data.m_Index >= 0) continue;
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
        if (names.Count == 0) return;
        // A failed carried deck invalidates the whole bridge: collect the root and
        // both named decks, never the source road, shared sections or composition caches.
        foreach (var net in loaded)
            if (TryBridgeName(net.name, out var bridge) && names.Contains(bridge)
                && !net.isBuiltin && !net.isReadOnly && _prefabs.TryGetEntity(net, out var entity))
                missing.Add(entity);
        // Include obsolete sibling decks as well, even if only one failed this load.
        using (var entities = _networks.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!EntityManager.HasComponent<PrefabData>(prefab)) continue;
                var data = EntityManager.GetComponentData<PrefabData>(prefab);
                if (data.m_Index >= 0) continue;
                var id = _prefabs.GetObsoleteID(data);
                if (TryBridgeName(id.GetName(), out var bridge) && names.Contains(bridge)) missing.Add(prefab);
            }
        _failedBridges.UnionWith(names);
        _failedNetworks.UnionWith(missing);
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
        _timeoutReported = false;
        _removal = plan;
        _started = DateTime.UtcNow;
        plan.IncludeOwnedEntities(EntityManager);
        Mod.Log.Info($"Missing bridge cleanup scheduled: {names.Count} bridge(s), "
            + $"{plan.DeletedEntities.Count} network and owned entities, {plan.UpdatedEntities.Count} surviving updates.");
        plan.Apply(EntityManager);
    }

    private bool IsInvalid(NetGeometryPrefab net, out string reason)
    {
        reason = string.Empty;
        try
        {
            if (net.m_Sections == null || net.m_Sections.Length == 0
                || net.m_Sections.Any(section => section == null || section.m_Section == null))
                reason = "missing section reference";
            else if (net.TryGet<AuxiliaryNets>(out var auxiliary)
                && (auxiliary.m_AuxiliaryNets == null
                    || auxiliary.m_AuxiliaryNets.Any(item => item == null || item.m_Prefab == null)))
                reason = "missing auxiliary network reference";
            else if (!_prefabs.TryGetEntity(net, out var entity)
                || !EntityManager.HasComponent<NetData>(entity))
                reason = "missing initialized network data";
            else
            {
                var data = EntityManager.GetComponentData<NetData>(entity);
                if (!data.m_NodeArchetype.Valid || !data.m_EdgeArchetype.Valid)
                    reason = "invalid network archetype";
            }
        }
        catch (Exception exception) { reason = exception.Message; }
        return reason.Length != 0;
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
