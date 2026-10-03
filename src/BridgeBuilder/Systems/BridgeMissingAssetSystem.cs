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

/// <summary>Once-per-load read-only missing bridge detection; never deletes city networks.</summary>
public partial class BridgeMissingAssetSystem : GameSystemBase
{
    private PrefabSystem _prefabs = null!;
    private EntityQuery _networks;
    private bool _scan;
    private bool _loadPlanned;
    private bool _finished;
    private bool _loadComplete;
    private string? _pendingNotice;
    private object[] _noticeArguments = Array.Empty<object>();

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
        _scan = false;
        _loadPlanned = false;
        _finished = false;
        _loadComplete = false;
        _pendingNotice = null;
        _noticeArguments = Array.Empty<object>();
        BridgeLoadFailures.Clear();
    }

    protected override void OnGameLoaded(Context serializationContext)
    {
        base.OnGameLoaded(serializationContext);
        // LoadGameSystem invokes this after the complete Deserialize phase, including
        // InitializeObsoleteSystem. Plan now, without waiting for the runtime UI catalog.
        // This system only reads saved IDs and never submits topology changes.
        if (GameManager.instance == null || (GameManager.instance.gameMode & GameMode.Game) == 0) return;
        if (_loadPlanned) return;
        _loadPlanned = true;
        Enabled = true;
        try { Scan(); }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Missing bridge load planning failed; city topology left untouched.");
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
        try
        {
            if (_scan) { _scan = false; Scan(); }
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Missing bridge inspection failed; all networks retained.");
        }
        FinishAttempt();
    }

    private void Scan()
    {
        EntityManager.CompleteAllTrackedJobs();
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
                names.Add(bridge);
                AddEvidence(bridge, "obsolete saved network: " + id);
            }
        }
        // Count each bridge UUID once, including both decks. Unresolved dependencies
        // are reported, never used as authority to delete or modify a placed entity.
        foreach (var failure in evidence)
            Mod.Log.Warn($"Missing bridge in loaded save '{failure.Key}': {failure.Value}; notification only, all networks retained.");
        if (names.Count > 0) Notice("MissingBridgesDetected", names.Count);

        void AddEvidence(string bridge, string reason)
        {
            evidence[bridge] = evidence.TryGetValue(bridge, out var prior) ? prior + "; " + reason : reason;
        }
    }

    private void FinishAttempt()
    {
        _finished = true;
        _scan = false;
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
