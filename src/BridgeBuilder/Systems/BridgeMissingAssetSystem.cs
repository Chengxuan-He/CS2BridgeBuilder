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
    private readonly BridgeCleanupConfirmation _confirmation = new();
    private int _completedFrame = -1;
    private BridgeDiskAudit? _diskAudit;
    internal bool IsCleaning => _removal != null;

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
        _inspectLoaded = false;
        _completedFrame = -1;
        _diskAudit = null;
        _confirmation.Clear();
        BridgeLoadFailures.Clear();
        _failureRevision = BridgeLoadFailures.Revision;
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
        // Do not mutate topology inside loading callbacks. Catalogue settlement and the
        // scheduled native prefab pipeline must get their normal update first.
    }

    protected override void OnUpdate()
    {
        try
        {
            if (_removal == null && (BridgeRuntimeRequests.CatalogLoading
                || World.GetOrCreateSystemManaged<BridgePublicationSystem>().IsPending
                || World.GetOrCreateSystemManaged<BridgeGenerationSystem>().IsRemoving))
            {
                _confirmation.Clear();
                return;
            }
            if (_removal == null && _failureRevision != BridgeLoadFailures.Revision)
            {
                _scan = true;
                _inspectLoaded = true;
            }
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
            if (!_removal.IsComplete(EntityManager)
                || BridgeInstanceRemoval.HasPlacedReferences(EntityManager, _failedNetworks))
            {
                _completedFrame = -1;
                if (!_timeoutReported && (DateTime.UtcNow - _started).TotalSeconds >= 30)
                {
                    _timeoutReported = true;
                    Mod.Log.Critical("Missing bridge cleanup has not completed after 30 seconds; "
                        + $"tracking {_removal.DeletedEntities.Count} network and owned entities. Save repair is incomplete.");
                }
                return;
            }
            // Observe completion on a later engine frame as well. Never delete backing
            // assets in the same update which marked the last network/child for deletion.
            if (_completedFrame < 0) { _completedFrame = UnityEngine.Time.frameCount; return; }
            if (_completedFrame == UnityEngine.Time.frameCount) return;
            // Keep backing assets and prefab indices alive until native network deletion
            // has completed. In particular, never call PrefabSystem.RemovePrefab here.
            var generator = World.GetOrCreateSystemManaged<BridgeGenerationSystem>();
            var diskFailures = new HashSet<string>(_failedBridges.Where(b =>
                _diskAudit != null && _diskAudit.Failures.ContainsKey(b)), StringComparer.Ordinal);
            if (diskFailures.Count > 0 && !generator.RemoveInvalidBridgeFilesAfterLoad(_diskAudit!, diskFailures))
            {
                Mod.Log.Warn("Invalid bridge file cleanup stopped; recovery files and live prefab indices retained.");
                Enabled = false;
                return;
            }
            foreach (var bridge in _failedBridges.Except(diskFailures))
                if (!generator.RemoveInvalidBridgeAfterLoad(bridge))
                {
                    Mod.Log.Warn($"Invalid bridge '{bridge}' still has protected asset references; cleanup stopped.");
                    Enabled = false;
                    return;
                }
            Mod.Log.Info($"Missing bridge cleanup completed: {_missingCount} bridge(s), "
                + $"{_removal.DeletedEntities.Count} network and owned entities removed.");
            var bindings = GameManager.instance?.userInterface?.appBindings;
            // Finalize independently of UI availability; never repeat destructive work
            // just because the dialog host is not ready.
            _removal = null;
            _handledFailures.UnionWith(_failedBridges);
            _failedBridges.Clear();
            _failedNetworks.Clear();
            _diskAudit = null;
            _completedFrame = -1;
            bindings?.ShowMessageDialog(new MessageDialog(
                LocalizedString.Value("Bridge Builder"),
                LocalizedString.Value(RuntimeUiText.Get("MissingBridgesRemoved", _missingCount)),
                LocalizedString.Value("OK")), _ => { });
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
        var evidence = new Dictionary<string, string>(StringComparer.Ordinal);
        var loaded = PrefabCatalog.GetAll(_prefabs).Concat(BridgeLoadFailures.Prefabs())
            .OfType<NetGeometryPrefab>().Distinct().ToArray();
        // Native registries can hide the losing asset in a duplicate-name/CID pair.
        // Validate the owned on-disk graphs as well as the objects which registered.
        var disk = BridgeDiskAudit.Read(UnityEngine.Application.persistentDataPath,
            BridgeRegistrationStore.Load().Select(r => r.PrefabName), cid =>
                Colossal.IO.AssetDatabase.AssetDatabase.global.TryGetAsset<Colossal.IO.AssetDatabase.GeometryAsset>(cid, out var geometry)
                && (geometry.database != Colossal.IO.AssetDatabase.AssetDatabase.user
                    || System.IO.File.Exists(geometry.path)));
        if (!disk.Complete)
        {
            // An incomplete disk scan is not authority to delete anything, including an
            // apparently obsolete network whose file may merely be temporarily inaccessible.
            Mod.Log.Warn("Bridge file validation incomplete; retaining bridges: " + disk.Error);
            _confirmation.Clear();
            return;
        }
        foreach (var failure in disk.Failures.Where(p => !_handledFailures.Contains(p.Key)))
        {
            names.Add(failure.Key);
            AddEvidence(failure.Key, failure.Value);
        }
        // A log entry is a request to recheck, not deletion authority. In particular,
        // a recovered tower/section failure must not delete a healthy network UUID group.
        // The caller waits for catalogue/publication settlement. At runtime a new error
        // requests this same current-state validation instead of trusting its old log.
        foreach (var net in _inspectLoaded ? loaded.OrderBy(p => p.name, StringComparer.Ordinal)
                     : Enumerable.Empty<NetGeometryPrefab>())
        {
            if (!TryBridgeName(net.name, out var bridge) || net.isBuiltin || net.isReadOnly) continue;
            if (_handledFailures.Contains(bridge)) continue;
            if (disk.Failures.ContainsKey(bridge)) continue;
            if (!BridgeNetworkValidation.IsInvalid(net, out _) && !_prefabs.TryGetEntity(net, out _))
            {
                // A previously rejected object may have recovered. Give native registration
                // another chance instead of deleting it solely because our gate rejected it.
                if (_prefabs.AddPrefab(net))
                {
                    _scan = true;
                    _inspectLoaded = true;
                    _confirmation.Clear();
                    return;
                }
                continue;
            }
            if (!IsInvalid(net, out var reason)) continue;
            names.Add(bridge);
            AddEvidence(bridge, net.name + ": " + reason);
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
                AddEvidence(bridge, "obsolete saved network: " + id);
            }
        }
        // ECS query order need not be stable across frames.
        foreach (var bridge in evidence.Keys.ToArray())
            evidence[bridge] = string.Join("; ", evidence[bridge].Split(new[] { "; " },
                StringSplitOptions.None).OrderBy(reason => reason, StringComparer.Ordinal));
        var confirmed = _confirmation.Observe(evidence, UnityEngine.Time.frameCount);
        if (names.Count != 0 && !names.SetEquals(confirmed))
        {
            // Rebuild the entire plan next frame, including recovered/missing references.
            // Do not partially remove one deck while its sibling is still being checked.
            _scan = true;
            _inspectLoaded = true;
            return;
        }
        if (names.Count == 0) return;
        _diskAudit = disk;
        var diskFailures = new HashSet<string>(names.Where(disk.Failures.ContainsKey), StringComparer.Ordinal);
        if (diskFailures.Count > 0 && !World.GetOrCreateSystemManaged<BridgeGenerationSystem>()
                .CanRetireBridgeFiles(disk, diskFailures, out _))
        {
            // Do not delete the placed networks first if a surviving asset prevents
            // the corresponding file group from being safely retired.
            Enabled = false;
            return;
        }
        foreach (var failure in evidence)
            Mod.Log.Warn($"Confirmed invalid bridge '{failure.Key}': {failure.Value}; removing UUID group.");
        World.GetOrCreateSystemManaged<BridgeGenerationSystem>().SuspendForCleanup();
        // A failed carried deck invalidates the whole bridge: collect the root and
        // both named decks, never the source road, shared sections or composition caches.
        foreach (var net in loaded)
            if ((TryBridgeName(net.name, out var bridge) && names.Contains(bridge)
                    || net.asset != null && disk.OwnsPath(net.asset.path, names))
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
        _missingCount = names.Count;
        _timeoutReported = false;
        _removal = plan;
        _started = DateTime.UtcNow;
        _completedFrame = -1;
        plan.IncludeOwnedEntities(EntityManager);
        var tools = World.GetOrCreateSystemManaged<ToolSystem>();
        if (plan.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
        Mod.Log.Info($"Missing bridge cleanup scheduled: {names.Count} bridge(s), "
            + $"{plan.DeletedEntities.Count} network and owned entities, {plan.UpdatedEntities.Count} surviving updates.");
        plan.Apply(EntityManager);

        void AddEvidence(string bridge, string reason)
        {
            evidence[bridge] = evidence.TryGetValue(bridge, out var prior) ? prior + "; " + reason : reason;
        }
    }

    private bool IsInvalid(NetGeometryPrefab net, out string reason)
    {
        reason = string.Empty;
        try
        {
            if (BridgeNetworkValidation.IsInvalid(net, out reason)) return true;
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
        catch (Exception exception)
        {
            // Failure to inspect is not proof that a player's bridge can be destroyed.
            Mod.Log.Warn($"Could not validate bridge '{net.name}'; retaining it: {exception.Message}");
            reason = string.Empty;
        }
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
