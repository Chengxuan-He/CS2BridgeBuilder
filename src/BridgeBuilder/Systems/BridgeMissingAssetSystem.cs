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
    private readonly HashSet<Entity> _failedNetworks = new();
    private bool _loadPlanned;
    private bool _finished;
    private bool _loadComplete;
    private bool _applied;
    private string? _pendingNotice;
    private object[] _noticeArguments = Array.Empty<object>();
    private readonly HashSet<string> _reportedEvidence = new(StringComparer.Ordinal);
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
        _failedNetworks.Clear();
        _loadPlanned = false;
        _finished = false;
        _loadComplete = false;
        _applied = false;
        _pendingNotice = null;
        _noticeArguments = Array.Empty<object>();
        _reportedEvidence.Clear();
        BridgeLoadFailures.Clear();
    }

    protected override void OnGameLoaded(Context serializationContext)
    {
        base.OnGameLoaded(serializationContext);
        // LoadGameSystem invokes this after the complete Deserialize phase, including
        // InitializeObsoleteSystem. Plan now, without waiting for the runtime UI catalog.
        // Applying Deleted still belongs to PostTool, before native sub-element deletion.
        if (GameManager.instance == null || (GameManager.instance.gameMode & GameMode.Game) == 0) return;
        if (_loadPlanned) return;
        _loadPlanned = true;
        Enabled = true;
        if (BridgeLoadFailures.NetworkInitializationFailed) return;
        try { Scan(); }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Missing bridge load planning failed; city topology left untouched.");
            Notice("MissingBridgesRepairFailed");
            FinishAttempt();
        }
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        _loadComplete = (mode & GameMode.Game) != 0;
        // Do not discard a plan prepared during deserialization. New-game paths without
        // that callback still scan on their first safe native update.
        if (_loadComplete && !_loadPlanned)
        {
            _loadPlanned = true;
            _scan = true;
            Enabled = true;
        }
        ShowPendingNotice();
    }

    protected override void OnUpdate()
    {
        if (_finished)
        {
            ShowPendingNotice();
            Enabled = _pendingNotice != null;
            return;
        }
        if (BridgeLoadFailures.NetworkInitializationFailed)
        {
            // Default NetData after an aborted batch is not authority to erase bridges.
            Mod.Log.Warn("Automatic bridge cleanup suspended: native initialization failed or legacy unlock recovery requires a restart. "
                + "Bridge files retained; finish recovery and restart before loading/saving a city.");
            Notice("MissingBridgesSuspended");
            FinishAttempt();
            return;
        }
        try
        {
            ShowPendingNotice();
            if (!_applied && (_scan || _removal != null)
                && (World.GetOrCreateSystemManaged<BridgePublicationSystem>().IsPending
                || World.GetOrCreateSystemManaged<BridgeGenerationSystem>().IsRemoving))
            {
                StopBlocked("Another bridge publication or removal is active at the load cleanup boundary.");
                return;
            }
            if (_scan)
            {
                _scan = false;
                Scan();
            }
            if (_removal == null)
            {
                FinishAttempt();
                return;
            }
            if (!_applied)
            {
                // One submission per city load. Recheck at the safe PostTool boundary,
                // then leave subsequent native deletion/ownership maintenance to the game.
                _removal.IncludeOwnedEntities(EntityManager);
                if (!_removal.CanApply(EntityManager, out var blocked))
                {
                    StopBlocked(blocked);
                    return;
                }
                World.GetOrCreateSystemManaged<BridgeGenerationSystem>().SuspendForCleanup();
                var tools = World.GetOrCreateSystemManaged<ToolSystem>();
                if (_removal.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
                _started = DateTime.UtcNow;
                _applied = true;
                _removal.Apply(EntityManager);
            }
            // Read-only completion observation is not another scan or deletion attempt.
            // Do not claim success until native deletion has processed topology and children.
            var remaining = _removal.RemainingEntityCount(EntityManager);
            var references = BridgeInstanceRemoval.CountPlacedReferences(EntityManager, _failedNetworks);
            if (remaining != 0 || references != 0)
            {
                if (!_timeoutReported && (DateTime.UtcNow - _started).TotalSeconds >= 30)
                {
                    _timeoutReported = true;
                    Mod.Log.Warn("Missing bridge cleanup has not completed after 30 seconds; "
                        + $"remaining {remaining} of {_removal.DeletedEntities.Count} tracked entities, "
                        + $"{references} placed network references. Save repair is incomplete.");
                    Notice("MissingBridgesTimeout", remaining, references);
                    FinishAttempt();
                }
                return;
            }
            // This phase only repairs placed topology. Disk assets were handled at the
            // title screen; never delete files or remove/reindex native prefabs here.
            Mod.Log.Info($"Missing bridge cleanup completed: {_missingCount} bridge(s), "
                + $"{_removal.DeletedEntities.Count} network and owned entities removed.");
            // Finalize independently of UI availability; never repeat destructive work
            // just because the dialog host is not ready.
            Notice("MissingBridgesRemoved", _missingCount);
            FinishAttempt();
        }
        catch (Exception exception)
        {
            // Never retry a partially applied destructive operation every frame.
            Mod.Log.Warn(exception, "Missing bridge save repair failed; the original save file was not modified.");
            Notice("MissingBridgesRepairFailed");
            FinishAttempt();
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
        // No disk audit or prefab mutation in a loaded city. Retired identities remain
        // recognizable even if this session still has their live native prefab indices.
        var retiredPrefabs = new Dictionary<Entity, string>();
        foreach (var net in loaded)
            if (TryBridgeName(net.name, out var retired) && BridgeStartupRecovery.Retired.Contains(retired)
                && _prefabs.TryGetEntity(net, out var retiredEntity))
                retiredPrefabs[retiredEntity] = retired;
        using (var entities = _networks.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!checkedPrefabs.Add(prefab)) continue;
                // A retired file with no placed network is not a damaged city instance.
                // In particular, do not repeatedly count all installed corruption fixtures.
                if (retiredPrefabs.TryGetValue(prefab, out var retired))
                {
                    missing.Add(prefab);
                    names.Add(retired);
                    AddEvidence(retired, "placed network retired at title screen");
                }
                if (!EntityManager.HasComponent<PrefabData>(prefab)) continue;
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
                StringSplitOptions.None).Distinct().OrderBy(reason => reason, StringComparer.Ordinal));
        // Missing saved IDs are authoritative after Deserialize. Retired UUIDs were
        // already confirmed at the title screen; a second frame adds no new evidence.
        if (names.Count == 0) return;
        foreach (var failure in evidence)
            if (_reportedEvidence.Add(failure.Key + ":" + failure.Value))
                Mod.Log.Warn($"Missing bridge in loaded save '{failure.Key}': {failure.Value}; evaluating native topology cleanup.");
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
                    if (_reportedEvidence.Add("object:" + id))
                        Mod.Log.Info($"Missing saved Bridge Builder object selected for cleanup planning (not yet removed): {id}");
                    break;
                }
            }
        plan.IncludeOwnedEntities(EntityManager);
        if (!plan.CanApply(EntityManager, out var blocked))
        {
            StopBlocked(blocked);
            return;
        }
        _missingCount = names.Count;
        _timeoutReported = false;
        _removal = plan;
        _applied = false;
        Mod.Log.Info($"Missing bridge cleanup scheduled: {names.Count} bridge(s), "
            + $"{plan.DeletedEntities.Count} network and owned entities, {plan.UpdatedEntities.Count} surviving updates.");

        void AddEvidence(string bridge, string reason)
        {
            evidence[bridge] = evidence.TryGetValue(bridge, out var prior) ? prior + "; " + reason : reason;
        }
    }

    private void StopBlocked(string reason)
    {
        Mod.Log.Warn("Missing bridge load cleanup blocked; no retry during this city session. "
            + "No blocked plan was applied; saving is not restricted. " + reason);
        // Technical prefab/entity details stay in the log, not in an untranslated suffix.
        Notice("MissingBridgesBlocked");
        FinishAttempt();
    }

    private void FinishAttempt()
    {
        _finished = true;
        _scan = false;
        _removal = null;
        _failedNetworks.Clear();
        _applied = false;
        // Only a new OnGamePreload permits another attempt. Keep UI delivery alive if
        // necessary, but never restart scanning because the failure registry changed.
        Enabled = _pendingNotice != null;
    }

    private void Notice(string key, params object[] arguments)
    {
        _pendingNotice = key;
        _noticeArguments = arguments;
        ShowPendingNotice();
    }

    private void ShowPendingNotice()
    {
        // Loading notifications retain keys/arguments, and resolve the player's current
        // language only when the game UI can display them. Never interrupt deserialization.
        if (!_loadComplete || _pendingNotice == null
            || GameManager.instance?.userInterface?.appBindings == null) return;
        var key = _pendingNotice;
        _pendingNotice = null;
        Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get(key, _noticeArguments));
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
