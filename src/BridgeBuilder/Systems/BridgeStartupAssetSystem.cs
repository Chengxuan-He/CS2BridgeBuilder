using BridgeBuilder.Runtime;
using Game.Prefabs;
using BridgeBuilder.Settings;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BridgeBuilder.Systems;

/// <summary>Inspect loaded references once both the mod and main menu have finished loading.</summary>
public partial class BridgeStartupAssetSystem : GameSystemBase
{
    private static BridgeStartupAssetSystem? _active;
    private bool _startupPending;
    private static bool _modLoaded;
    private bool _manualRequested;
    private BridgeSelfCheckReport? _pendingNotice;
    private readonly HashSet<string> _copiedOwners = new(StringComparer.Ordinal);

    internal static bool IsStartupInspection { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();
        Enabled = false;
    }

    internal static bool CanCheck => _modLoaded && GameManager.instance != null
        && GameManager.instance.gameMode == GameMode.MainMenu
        && GameManager.instance.state == GameManager.State.WorldReady
        && !GameManager.instance.isGameLoading && !IsStartupInspection && BridgeAssetLoading.Ready;

    internal void ScheduleInspectionAfterLoad()
    {
        BridgeAssetLoading.Start(() => { _startupPending = true; Enabled = true; });
        _modLoaded = true;
        _active = this;
        _startupPending = true;
        Enabled = true;
        Mod.Log.Info("Bridge self-check waiting for mod initialization + native prefab loading + PDX asset batches.");
    }

    internal static void StopInspection()
    {
        if (_active != null)
        {
            _active.Enabled = false;
            _active._startupPending = false;
        }
        BridgeAssetLoading.Stop();
        _active = null;
        _modLoaded = false;
    }

    internal void RequestManualCheck()
    {
        if (!CanCheck) return;
        _manualRequested = true;
        Enabled = true;
    }

    protected override void OnGamePreload(Purpose purpose, GameMode mode)
    {
        base.OnGamePreload(purpose, mode);
        _manualRequested = false;
        Enabled = false;
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        // Native loading may finish after Mod.OnLoad. Restore owner programs here,
        // before GameManager transitions to WorldReady and starts MeshSystem updates.
        if (_modLoaded && !BridgeCustomShaderRestoration.TryRestore(out _, out var materialError))
            Mod.Log.Error("Generated material program loading: " + materialError);
        // GameManager sets WorldReady only AFTER returning from this event.
        // Wake the next UI update; never move files inside the loading callback.
        Enabled = _modLoaded && mode == GameMode.MainMenu;
    }

    protected override void OnUpdate()
    {
        if (!CanCheck) return;
        if (_startupPending || _manualRequested)
        {
            var manual = _manualRequested;
            _manualRequested = false;
            _startupPending = false;
            RunInspection(manual);
        }
        if (_pendingNotice == null) { Enabled = _startupPending; return; }
        if (GameManager.instance.userInterface?.appBindings == null) return;
        var key = _pendingNotice;
        _pendingNotice = null;
        Enabled = _startupPending;
        Mod.ShowRecoveryMessage(key.Message());
    }

    private void RunInspection(bool manual)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        IsStartupInspection = true;
        var report = new BridgeSelfCheckReport();
        _pendingNotice = null;
        var completed = false;
        Mod.Log.Info($"Bridge self-check started: {(manual ? "manual" : "mod initialized + all native asset batches complete")}");
        try
        {
            if (!BridgeCustomShaderRestoration.TryRestore(out _, out var programError))
            {
                Mod.Log.Error("Bridge self-check retained owners: generated material registration failed; " + programError);
                return;
            }
            BridgeDeferredRemoval.Resume();
            var loadedIndex = new BridgeLoadedCidRecovery.LoadedIndex();
            var recoveries = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var failures = new Dictionary<string, string>(StringComparer.Ordinal);
            var system = World.GetOrCreateSystemManaged<PrefabSystem>();
            var assets = BridgeInspectionAssets.Read(owner => !_copiedOwners.Contains(owner) && !BridgeStartupRecovery.Retired.Contains(owner) && !BridgeDeferredRemoval.Excluded(owner)).ToArray();
            if (assets.Length != 0 && BridgeAssetPack.Ensure(system) == null)
                Mod.Log.Error("Bridge pack icon could not be updated; existing bridge assets retained.");
            var owners = new HashSet<string>(assets.Select(a => a.Owner), StringComparer.Ordinal);
            var byOwner = assets.GroupBy(a => a.Owner).ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
            var contentAvailable = BridgePreparedMaterials.RefreshContent(system);
            var unfinished = new HashSet<string>(StringComparer.Ordinal);
            foreach (var owner in owners.ToArray())
            {
                if (!byOwner[owner].Any(asset => asset.Prefab == null ? !contentAvailable : (!system.IsAvailable(asset.Prefab) || BridgePreparedMaterials.HasUnavailableRequirement(asset.Prefab)))) continue;
                unfinished.Add(owner);
                // No mutation occurred: restoring the DLC, not a restart loop, is required.
                // Keep this deferred owner out of the repair-result notification.
                owners.Remove(owner);
                Mod.Log.Warn("Bridge inspection deferred; required content unavailable or unresolved UUID=" + owner);
            }
            var scope = new BridgeInspectionScope(UnityEngine.Application.persistentDataPath, owners);
            var dependencies = new Dictionary<string, BridgeCreationDependencies>(StringComparer.Ordinal);
            foreach (var owner in owners)
            {
                report.Set(owner, BridgeSelfCheckReport.Outcome.Unfinished);
                foreach (var asset in byOwner[owner]) { scope.Paths(owner).Add(asset.Path); scope.Paths(owner).Add(asset.Path + ".cid"); }
                try { dependencies.Add(owner, scope.Dependencies(owner, byOwner[owner])); }
                catch (Exception exception) { failures[owner] = "Cache relation read failed: " + exception.Message; }
            }
            if(owners.Count!=0 && !scope.PrepareCaches(out var cachePreparationError))
            {
                Mod.Log.Error("Bridge self-check aborted before owner inspection: cache relation preparation failed; "+cachePreparationError);
                return;
            }
            var discoveryMs = timer.ElapsedMilliseconds;
            var stageStart = timer.ElapsedMilliseconds;
            var migrationCandidates = new HashSet<string>(StringComparer.Ordinal);
            var migrationErrors = 0;
            // Missing material snapshots can be hidden by a currently installed source mod.
            // Traverse every owner's disk graph, even when its live references are healthy.
            foreach (var owner in owners)
            {
                if (failures.ContainsKey(owner)) continue;
                if (!BridgeDependencyPersistence.Save(owner, byOwner[owner].Select(a => a.Cid), dependencies[owner],
                    out _, out var migrationError, out var writtenFiles))
                {
                    migrationErrors++;
                    if (dependencies[owner].Unconfirmed)
                    {
                        unfinished.Add(owner);
                        Mod.Log.Warn($"Bridge dependency inspection remains unfinished UUID={owner}: {migrationError}");
                        continue;
                    }
                    failures[owner] = "Dependency migration failed: " + migrationError;
                    Mod.Log.Error($"Bridge dependency migration failed UUID={owner}; scheduling removal: {migrationError}");
                }
                else if (writtenFiles != 0)
                {
                    migrationCandidates.Add(owner);
                    Mod.Log.Info($"Bridge dependency snapshots supplemented UUID={owner}; writtenFiles={writtenFiles}; external material dependencies included; restart required; live references unchanged.");
                }
            }
            foreach (var pair in dependencies) scope.Paths(pair.Key).UnionWith(pair.Value.Files);
            var dependencyMs = timer.ElapsedMilliseconds - stageStart;
            stageStart = timer.ElapsedMilliseconds;
            foreach (var asset in assets)
            {
                if (unfinished.Contains(asset.Owner) || failures.ContainsKey(asset.Owner)) continue;
                var prefab = asset.Prefab;
                string reason;
                if (prefab == null)
                {
                    failures[asset.Owner] = "Prefab did not load: " + asset.Path;
                    continue;
                }
                // Availability can change after discovery; preserve rather than retire the owner.
                if (!system.IsAvailable(prefab)) { unfinished.Add(asset.Owner); continue; }
                if (BridgeNetworkValidation.IsInvalid(prefab, out reason))
                {
                    var result = BridgeLoadedCidRecovery.Inspect(prefab, out var cids, out var detail, loadedIndex);
                    if (result == BridgeLoadedCidRecovery.Result.Recoverable && cids.Count != 0)
                    {
                        if (!recoveries.TryGetValue(asset.Owner, out var needed)) recoveries[asset.Owner] = needed = new(StringComparer.OrdinalIgnoreCase);
                        needed.UnionWith(cids);
                        continue;
                    }
                    reason += "; " + detail;
                }
                else if (system.TryGetEntity(prefab, out _)) continue;
                else reason = "Loaded available prefab failed registration without recoverable null CID";
                Mod.Log.Warn($"Bridge self-check invalid UUID={asset.Owner}, prefab={prefab.name}: {reason}");
                failures[asset.Owner] = reason;
            }
            var validationMs = timer.ElapsedMilliseconds - stageStart;
            stageStart = timer.ElapsedMilliseconds;
            var backup = Path.Combine(BridgeRecoveryLocation.Path,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", System.Globalization.CultureInfo.InvariantCulture));
            foreach (var recovery in recoveries.Where(p => !failures.ContainsKey(p.Key)))
            {
                var seeds = byOwner[recovery.Key].Select(a => a.Cid).Concat(recovery.Value);
                if (!BridgeDependencyPersistence.Save(recovery.Key, seeds, dependencies[recovery.Key], out var count, out var copyError, out _))
                {
                    if (dependencies[recovery.Key].Unconfirmed)
                    {
                        unfinished.Add(recovery.Key);
                        Mod.Log.Warn($"Bridge dependency recovery remains unfinished UUID={recovery.Key}: {copyError}");
                        continue;
                    }
                    failures[recovery.Key] = "Dependency recovery copy failed: " + copyError; Mod.Log.Warn($"Bridge dependency recovery failed UUID={recovery.Key}; scheduling removal: {copyError}"); continue; }
                _copiedOwners.Add(recovery.Key);
                migrationCandidates.Add(recovery.Key);
                Mod.Log.Info($"Bridge CID recovery copied UUID={recovery.Key}, dependencies={count}, recoveredCIDs={string.Join(",", recovery.Value)}; restart required; live references unchanged.");
            }
            var persistenceMs = timer.ElapsedMilliseconds - stageStart;
            stageStart = timer.ElapsedMilliseconds;
            foreach (var pair in dependencies) scope.Paths(pair.Key).UnionWith(pair.Value.Files);
            // Recovered disk references are still null in memory until restart; never serialize them back.
            foreach (var owner in owners.Where(o => !unfinished.Contains(o) && !failures.ContainsKey(o) && !_copiedOwners.Contains(o)))
            {
                if (!BridgePortableMigration.Run(owner, system, backup, out var converted, out var conversionError, byOwner[owner], dependencies[owner], scope.Paths(owner)))
                {
                    migrationErrors++;
                    failures[owner] = "Independent migration failed: " + conversionError;
                    Mod.Log.Error($"Independent bridge migration failed UUID={owner}; scheduling removal after rollback: {conversionError}");
                }
                else if (converted)
                {
                    _copiedOwners.Add(owner);
                    migrationCandidates.Add(owner);
                    BridgeSessionState.RequireRestart();
                    Mod.Log.Info($"Independent bridge migration completed UUID={owner}; native unlocks and presentation persisted; original native fees retained; restart required.");
                }
            }
            var portableMs = timer.ElapsedMilliseconds - stageStart;
            long namesMs = 0, layoutMs = 0;
            // Conversion writes the current disk graph. Normalize that graph in this SAME pass,
            // including previously converted assets, before any next-session validation.
            foreach (var owner in owners.Where(o => !unfinished.Contains(o) && !failures.ContainsKey(o)).ToArray())
            {
                var selected = new[] { owner };
                scope.Paths(owner).UnionWith(dependencies[owner].Files);
                stageStart = timer.ElapsedMilliseconds;
                if (!BridgeLegacyNames.Run(UnityEngine.Application.persistentDataPath,
                    selected, backup, out var renamed, out var renameError, BridgePrefabPersistence.RelativePathFor,
                    byOwner[owner].Where(a => a.Prefab?.name == owner && !BridgeAssetInfo.IsDependencyPath(a.Path, owner)).Select(a => a.Path),
                    cid => scope.Find(cid, owner), scope.Paths(owner)))
                {
                    migrationErrors++;
                    namesMs += timer.ElapsedMilliseconds - stageStart;
                    failures[owner] = "Legacy component migration failed: " + renameError;
                    Mod.Log.Error($"Legacy bridge component migration failed UUID={owner}; scheduling removal: {renameError}");
                    continue;
                }
                namesMs += timer.ElapsedMilliseconds - stageStart;
                stageStart = timer.ElapsedMilliseconds;
                _copiedOwners.UnionWith(renamed);
                migrationCandidates.UnionWith(renamed);
                if (!BridgeAssetLayout.Run(UnityEngine.Application.persistentDataPath, selected, backup,
                    BridgePrefabPersistence.RelativePathFor, out var relocated, out var layoutError, scope.Paths(owner), dependencies[owner].Relocate))
                {
                    migrationErrors++;
                    layoutMs += timer.ElapsedMilliseconds - stageStart;
                    // Earlier writes do not make an incomplete layout a completed migration.
                    _copiedOwners.Add(owner);
                    Mod.Log.Error($"Bridge file layout migration failed UUID={owner}; preserving assets for retry after restart: {layoutError}");
                    continue;
                }
                if(!BridgeCreationDependencies.RemoveObsoleteRelations(owner,backup,out var relationsRemoved,out var relationError))
                {
                    report.Set(owner,BridgeSelfCheckReport.Outcome.Unfinished);migrationErrors++;_copiedOwners.Add(owner);
                    Mod.Log.Error($"Bridge obsolete cache metadata cleanup failed UUID={owner}: {relationError}");
                    continue;
                }
                if(relationsRemoved)migrationCandidates.Add(owner);
                layoutMs += timer.ElapsedMilliseconds - stageStart;
                _copiedOwners.UnionWith(relocated);
                migrationCandidates.UnionWith(relocated);
                if (migrationCandidates.Contains(owner))
                {
                    report.Set(owner, BridgeSelfCheckReport.Outcome.Migrated);
                    _copiedOwners.Add(owner);
                }
                else report.Healthy(owner);
            }
            stageStart = timer.ElapsedMilliseconds;
            var removed = 0;
            var removalErrors = 0;
            // One failed filesystem operation must not prevent retiring other damaged bridges.
            foreach (var failure in failures)
            {
                try
                {
                    var verdict = new Dictionary<string, string>(StringComparer.Ordinal) { [failure.Key] = failure.Value };
                    var audit = BridgeDiskAudit.ForMemoryFailures(UnityEngine.Application.persistentDataPath, verdict, scope.Paths(failure.Key));
                    if (!audit.Complete)
                    {
                        removalErrors++;
                        Mod.Log.Error($"Bridge removal failed UUID={failure.Key}: {audit.Error}");
                        continue;
                    }
                    Mod.Log.Warn($"Bridge self-check retiring '{failure.Key}': {failure.Value}");
                    // Retirement may move/delete some files before reporting failure. Cached assets
                    // for this owner are unsafe to read again until restart, even when it is not retired.
                    _copiedOwners.Add(failure.Key);
                    if (!audit.RetireFiles(new HashSet<string>(verdict.Keys, StringComparer.Ordinal), backup, out var error))
                    {
                        removalErrors++;
                        Mod.Log.Error($"Bridge removal failed UUID={failure.Key}: {error}");
                        continue;
                    }
                    BridgeStartupRecovery.Retired.Add(failure.Key);
                    report.Set(failure.Key, error.Length == 0
                        ? BridgeSelfCheckReport.Outcome.RemovedBackedUp : BridgeSelfCheckReport.Outcome.RemovedWithoutBackup);
                    removed++;
                    Mod.Log.Info($"Bridge retired UUID={failure.Key}; backup={backup}; backupComplete={error.Length == 0}");
                    if (error.Length != 0) Mod.Log.Warn(error);
                }
                catch (Exception exception)
                {
                    removalErrors++;
                    Mod.Log.Error($"Bridge removal failed UUID={failure.Key}: {exception}");
                }
            }
            completed = true;
            var retirementMs = timer.ElapsedMilliseconds - stageStart;
            var elapsedMs = timer.ElapsedMilliseconds;
            var otherMs = elapsedMs - discoveryMs - dependencyMs - validationMs - persistenceMs - portableMs - namesMs - layoutMs - retirementMs;
            Mod.Log.Info($"Bridge self-check result: elapsedMs={elapsedMs}, validationMs={validationMs}, persistenceMs={persistenceMs}, retirementMs={retirementMs}, discoveryMs={discoveryMs}, dependencyMs={dependencyMs}, portableMs={portableMs}, namesMs={namesMs}, layoutMs={layoutMs}, otherMs={otherMs}, owners={owners.Count}, cacheTraversals={scope.CacheTraversals}, cacheHeaders={scope.CacheHeaders}, removed={removed}, migrated={report.Count(BridgeSelfCheckReport.Outcome.Migrated)}, copiedAwaitingRestart={_copiedOwners.Count}, removalErrors={removalErrors}, migrationErrors={migrationErrors}; cached assets include registration failures; live-reference mutation disabled.");
        }
        finally
        {
            // Keep completed owner outcomes; unresolved known owners remain unfinished.
            report.Interrupted = !completed;
            if (report.HasNotice)
            {
                BridgeSessionState.RequireRestart();
                _pendingNotice = report;
            }
            // Preserve exception propagation and always release the lifecycle gate.
            IsStartupInspection = false;
            Enabled = _pendingNotice != null || _startupPending;
            Mod.Log.Info($"Bridge self-check finished: results={report.Total}, interrupted={report.Interrupted}, restartRecommended={report.HasNotice}");
        }
    }
}
