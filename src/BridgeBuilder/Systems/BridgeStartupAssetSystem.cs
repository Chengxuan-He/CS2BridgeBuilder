using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Colossal.Serialization.Entities;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Systems;

/// <summary>Boot-time recovery after prefab loading, before the main menu. UI updates only show results.</summary>
public partial class BridgeStartupAssetSystem : GameSystemBase
{
    // Process-session latch: neither scene transitions nor a recreated ECS system rearm it.
    // Claimed during boot, including passes that fail or are interrupted.
    private static bool _startupCheckClaimed;
    private PrefabSystem _prefabs = null!;
    private bool _titleReady;
    private int _pendingRemovedNotice;
    private string? _pendingNotice;
    private int _pendingCount;
    internal static bool IsStartupInspection { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        Enabled = false;
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose, mode);
        _titleReady = false;
        Enabled = false;
        // Preserve the boot result through the initial transition into MainMenu.
    }

    protected override void OnWorldReady()
    {
        base.OnWorldReady();
        if (_startupCheckClaimed) return;
        _startupCheckClaimed = true;
        IsStartupInspection = true;
        try { InspectStartup(); }
        finally { IsStartupInspection = false; Enabled = false; }
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        _titleReady = (mode & GameMode.MainMenu) != 0;
        Enabled = _titleReady && _pendingNotice != null;
    }

    protected override void OnUpdate()
    {
        if (!_titleReady || GameManager.instance == null
            || (GameManager.instance.gameMode & GameMode.MainMenu) == 0)
        { Enabled = false; return; }
        if (_pendingNotice == null) { Enabled = false; return; }
        if (GameManager.instance.userInterface?.appBindings == null) return;
        var key = _pendingNotice;
        _pendingNotice = null;
        Enabled = false;
        Mod.ShowRecoveryMessage(RuntimeUiText.Get(key, _pendingCount));
    }

    private void InspectStartup()
    {
        // A native batch failure or unreadable migration is not corruption evidence.
        if (BridgeLoadFailures.NetworkInitializationFailed)
        {
            Mod.Log.Warn("Title bridge recovery deferred: startup recovery requires a restart; files retained.");
            _pendingNotice = "MissingBridgesSuspended";
            Enabled = false;
            return;
        }
        try
        {
            EntityManager.CompleteAllTrackedJobs();
            var inspection = System.Diagnostics.Stopwatch.StartNew();
            var loaded = PrefabCatalog.GetAll(_prefabs).Concat(BridgeLoadFailures.Prefabs())
                .Where(p => p != null && !p.isBuiltin && !p.isReadOnly
                    && BridgeLoadFailures.TryOwner(p.name, out _)
                    && !BridgeRecoveryLocation.IsBackup(p.asset?.path)).Distinct().ToArray();
            if (!BridgeRegistrationStore.TryLoad(out var registrations))
            {
                _pendingNotice = "MissingBridgesSuspended";
                return; // An unreadable registry never authorizes retirement.
            }
            var pending = new HashSet<string>(registrations.Where(r => r.Pending).Select(r => r.PrefabName),
                StringComparer.Ordinal);
            var owners = new HashSet<string>(registrations.Select(r => r.PrefabName),
                StringComparer.Ordinal);
            foreach (var prefab in loaded)
                if (!prefab.isBuiltin && !prefab.isReadOnly && prefab.asset != null
                    && BridgeMissingAssetSystem.TryBridgeName(prefab.name, out var owner)) owners.Add(owner);
            var audit = BridgeDiskAudit.Read(UnityEngine.Application.persistentDataPath, owners, cid =>
                Colossal.IO.AssetDatabase.AssetDatabase.global.TryGetAsset<Colossal.IO.AssetDatabase.GeometryAsset>(cid, out var geometry)
                && (geometry.database != Colossal.IO.AssetDatabase.AssetDatabase.user
                    || BridgeFileAccess.Exists(geometry.path)), pending);
            if (!audit.Complete)
            {
                Mod.Log.Warn("Title bridge audit incomplete; files retained: " + audit.Error);
                Enabled = false;
                return;
            }
            var deferred = new HashSet<string>(StringComparer.Ordinal);
            // Repair independent lanes/sections too: the native importer registers these
            // separately, even when their parent bridge is quarantined.
            foreach (var prefab in loaded.Where(BridgeReferenceRecovery.Supports))
            {
                if (prefab.isBuiltin || prefab.isReadOnly || !BridgeLoadFailures.TryOwner(prefab.name, out var owner)) continue;
                var status = BridgeReferenceRecovery.Repair(prefab, _prefabs, out var detail);
                if (status == BridgeReferenceRecovery.Result.Deferred)
                {
                    // After the awaited boot prefab load, unresolved required references make
                    // the owned file unusable. Quarantine it reversibly, not indefinitely in-place.
                    if (prefab.asset != null && audit.OwnsPath(prefab.asset.path, owners)
                        && BridgeNetworkValidation.IsInvalid(prefab, out var invalid))
                    {
                        audit.Failures[owner] = "Required dependency unresolved after boot prefab load: " + invalid;
                        continue;
                    }
                    deferred.Add(owner);
                    Mod.Log.Warn($"Bridge '{owner}' retained pending dependency recovery: {detail}");
                }
                else if (status == BridgeReferenceRecovery.Result.Broken && prefab.asset != null
                    && audit.OwnsPath(prefab.asset.path, owners))
                    audit.Failures[owner] = prefab.name + ": " + detail;
            }
            // Proven disk damage is not cancelled by an unrelated unavailable dependency.
            deferred.ExceptWith(audit.Failures.Keys);
            BridgeReferenceRecovery.DeferredOwners.Clear();
            BridgeReferenceRecovery.DeferredOwners.UnionWith(deferred);
            foreach (var net in loaded.OfType<NetGeometryPrefab>())
            {
                if (net.isBuiltin || net.isReadOnly
                    || !BridgeMissingAssetSystem.TryBridgeName(net.name, out var owner)
                    || BridgeStartupRecovery.Retired.Contains(owner) || deferred.Contains(owner)
                    || audit.Failures.ContainsKey(owner)) continue;
                // Migration/registration guard normally repaired these before native initialization.
                // Do not change a registered dependency buffer at the title screen.
                if (net.GetComponent<Unlockable>() is { active: true })
                {
                    if (_prefabs.TryGetEntity(net, out _) || !BridgeUnlockSnapshot.PrepareLegacy(net))
                    {
                        BridgeLoadFailures.RequireRestart();
                        Mod.Log.Warn($"Title repair of '{net.name}' deferred until restart; files retained.");
                        Enabled = false;
                        return;
                    }
                }
                if (BridgeNetworkValidation.IsInvalid(net, out var reason))
                {
                    // Only attach runtime evidence to an independently audited owned disk file.
                    if (net.asset != null && audit.OwnsPath(net.asset.path, owners))
                        audit.Failures[owner] = net.name + ": " + reason;
                }
                else if (!_prefabs.TryGetEntity(net, out _) && !audit.Failures.ContainsKey(owner))
                {
                    // A quarantined object that recovered gets normal registration, not deletion.
                    _prefabs.AddPrefab(net);
                }
            }
            // Re-register repaired dependency assets through the ordinary native pipeline.
            foreach (var prefab in loaded.Where(BridgeReferenceRecovery.Supports))
                if (!prefab.isBuiltin && !prefab.isReadOnly
                    && BridgeLoadFailures.TryOwner(prefab.name, out var owner)
                    && !deferred.Contains(owner) && !audit.Failures.ContainsKey(owner)
                    && !BridgeStartupRecovery.Retired.Contains(owner)
                    && !BridgeNetworkValidation.IsInvalid(prefab, out _) && !_prefabs.TryGetEntity(prefab, out _))
                    _prefabs.AddPrefab(prefab);
            deferred.ExceptWith(audit.Failures.Keys);
            BridgeReferenceRecovery.DeferredOwners.ExceptWith(audit.Failures.Keys);
            foreach (var retired in BridgeStartupRecovery.Retired) audit.Failures.Remove(retired);
            // OnWorldReady is raised only after the game's awaited LoadPrefabs completes.
            // RetireFiles rechecks file hashes; no repeated per-frame disk audit is necessary.
            var confirmed = new HashSet<string>(audit.Failures.Keys, StringComparer.Ordinal);
            if (confirmed.Count > 0)
            {
                foreach (var failure in audit.Failures)
                    Mod.Log.Warn($"Title recovery: retiring unrepaired bridge '{failure.Key}': {failure.Value}");
                if (!World.GetOrCreateSystemManaged<BridgeGenerationSystem>()
                    .RetireInvalidBridgeFilesAtStartup(audit, confirmed))
                {
                    Mod.Log.Warn("Title bridge retirement stopped; protected references/recovery files retained.");
                    Enabled = false;
                    return;
                }
                _pendingRemovedNotice += confirmed.Count;
            }
            Mod.Log.Info($"Boot bridge recovery complete: {BridgeStartupRecovery.Retired.Count} retired identity group(s); "
                + "placed networks will only be checked for missing-bridge notifications when a save is loaded.");
            Mod.Log.Info($"Boot inspection: {loaded.Length} owned prefab(s), {audit.FileOwners.Count} file(s), {inspection.ElapsedMilliseconds} ms.");
            if (_pendingRemovedNotice != 0)
            {
                _pendingNotice = "DamagedBridgeAssetsRemoved";
                _pendingCount = _pendingRemovedNotice;
            }
            else if (deferred.Count != 0)
            { _pendingNotice = "BridgeReferencesDeferred"; _pendingCount = deferred.Count; }
            Enabled = false;
        }
        catch (Exception exception)
        {
            Enabled = false;
            Mod.Log.Warn(exception, "Title bridge recovery stopped; inspection failure is not deletion authority.");
        }
    }
}
