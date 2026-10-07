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
using CS2Mods.Shared.Export;

namespace BridgeBuilder.Systems;

/// <summary>Inspect loaded references once both the mod and main menu have finished loading.</summary>
public partial class BridgeStartupAssetSystem : GameSystemBase
{
    private static BridgeStartupAssetSystem? _active;
    private bool _startupPending;
    private static bool _modLoaded;
    private bool _manualRequested;
    private bool _restartNotice;
    private bool _migrationIncomplete;
    private string? _pendingNotice;
    private readonly HashSet<string> _copiedOwners = new(StringComparer.Ordinal);
    private readonly HashSet<string> _migratedOwners = new(StringComparer.Ordinal);
    private readonly HashSet<string> _removedOwners = new(StringComparer.Ordinal);

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
        var message = string.Join("\n\n", key.Split('|').Select(k => RuntimeUiText.Get(k)));
        if (key == "BridgeSelfCheckSuccess") Mod.ShowMessage(UiStringCatalog.Current.Title, message);
        else Mod.ShowRecoveryMessage(message);
    }

    private void RunInspection(bool manual)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        IsStartupInspection = true;
        _pendingNotice = "BridgeSelfCheckIncomplete";
        _restartNotice = true;
        Mod.Log.Info($"Bridge self-check started: {(manual ? "manual" : "mod initialized + all native asset batches complete")}");
        try
        {
            var loadedIndex = new BridgeLoadedCidRecovery.LoadedIndex();
            var recoveries = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var failures = new Dictionary<string, string>(StringComparer.Ordinal);
            var system = World.GetOrCreateSystemManaged<PrefabSystem>();
            var assets = BridgeInspectionAssets.Read().Where(a => !_copiedOwners.Contains(a.Owner)).ToArray();
            if (assets.Length != 0 && BridgeAssetPack.Ensure(system) == null)
                Mod.Log.Error("Bridge pack icon could not be updated; existing bridge assets retained.");
            var owners = new HashSet<string>(assets.Select(a => a.Owner), StringComparer.Ordinal);
            var migrationCandidates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in assets)
            {
                var prefab = asset.Prefab;
                string reason;
                if (prefab == null)
                {
                    failures[asset.Owner] = "Prefab did not load: " + asset.Path;
                    continue;
                }
                // A bridge whose required content cannot run is retired under the repair-or-remove policy.
                if (!system.IsAvailable(prefab)) { failures[asset.Owner] = "Required bridge content unavailable: " + prefab.name; continue; }
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
            var validationMs = timer.ElapsedMilliseconds;
            var backup = Path.Combine(BridgeRecoveryLocation.Path,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", System.Globalization.CultureInfo.InvariantCulture));
            foreach (var recovery in recoveries.Where(p => !failures.ContainsKey(p.Key)))
            {
                var seeds = assets.Where(a => a.Owner == recovery.Key).Select(a => a.Cid).Concat(recovery.Value);
                if (!BridgeDependencyPersistence.Save(recovery.Key, seeds, out var count, out var copyError))
                { failures[recovery.Key] = "Dependency recovery copy failed: " + copyError; Mod.Log.Warn($"Bridge dependency recovery failed UUID={recovery.Key}; scheduling removal: {copyError}"); continue; }
                _copiedOwners.Add(recovery.Key);
                migrationCandidates.Add(recovery.Key);
                Mod.Log.Info($"Bridge CID recovery copied UUID={recovery.Key}, dependencies={count}, recoveredCIDs={string.Join(",", recovery.Value)}; restart required; live references unchanged.");
            }
            var migrationErrors = 0;
            foreach (var owner in owners.Where(o => !failures.ContainsKey(o) && !_copiedOwners.Contains(o)))
            {
                if (!BridgeAssetMigration.Run(owner, assets.Where(a => a.Owner == owner).Select(a => a.Cid),
                    out var changed, out var migrationError))
                {
                    migrationErrors++;
                    failures[owner] = "Dependency migration failed: " + migrationError;
                    Mod.Log.Error($"Bridge dependency migration failed UUID={owner}; scheduling removal: {migrationError}");
                }
                else if (changed) migrationCandidates.Add(owner);
            }
            var persistenceMs = timer.ElapsedMilliseconds - validationMs;
            foreach (var owner in owners.Where(o => !failures.ContainsKey(o) && !_copiedOwners.Contains(o)))
            {
                if (!BridgePortableMigration.Run(owner, system, backup, out var converted, out var conversionError))
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
            // Conversion writes the current disk graph. Normalize that graph in this SAME pass,
            // including previously converted assets, before any next-session validation.
            foreach (var owner in owners.Where(o => !failures.ContainsKey(o)).ToArray())
            {
                var selected = new[] { owner };
                if (!BridgeLegacyNames.Run(UnityEngine.Application.persistentDataPath,
                    selected, backup, out var renamed, out var renameError, PrefabAssetWriter.RelativePathFor))
                {
                    migrationErrors++;
                    failures[owner] = "Legacy component migration failed: " + renameError;
                    Mod.Log.Error($"Legacy bridge component migration failed UUID={owner}; scheduling removal: {renameError}");
                    continue;
                }
                _copiedOwners.UnionWith(renamed);
                migrationCandidates.UnionWith(renamed);
                if (!BridgeAssetLayout.Run(UnityEngine.Application.persistentDataPath, selected, backup,
                    PrefabAssetWriter.RelativePathFor, out var relocated, out var layoutError))
                {
                    migrationErrors++;
                    failures[owner] = "File layout migration failed: " + layoutError;
                    Mod.Log.Error($"Bridge file layout migration failed UUID={owner}; scheduling removal: {layoutError}");
                    continue;
                }
                _copiedOwners.UnionWith(relocated);
                migrationCandidates.UnionWith(relocated);
                if (migrationCandidates.Contains(owner)) _migratedOwners.Add(owner);
            }
            var removed = 0;
            var removalErrors = 0;
            // One failed filesystem operation must not prevent retiring other damaged bridges.
            foreach (var failure in failures)
            {
                var verdict = new Dictionary<string, string>(StringComparer.Ordinal) { [failure.Key] = failure.Value };
                var audit = BridgeDiskAudit.ForMemoryFailures(UnityEngine.Application.persistentDataPath, verdict);
                if (!audit.Complete)
                {
                    removalErrors++;
                    Mod.Log.Error($"Bridge removal failed UUID={failure.Key}: {audit.Error}");
                    continue;
                }
                Mod.Log.Warn($"Bridge self-check retiring '{failure.Key}': {failure.Value}");
                if (!audit.RetireFiles(new HashSet<string>(verdict.Keys, StringComparer.Ordinal), backup, out var error))
                {
                    removalErrors++;
                    Mod.Log.Error($"Bridge removal failed UUID={failure.Key}: {error}");
                    continue;
                }
                BridgeStartupRecovery.Retired.Add(failure.Key);
                _removedOwners.Add(failure.Key);
                removed++;
                Mod.Log.Info($"Bridge retired UUID={failure.Key}; backup={backup}; backupComplete={error.Length == 0}");
                if (error.Length != 0) Mod.Log.Warn(error);
            }
            _copiedOwners.ExceptWith(failures.Keys);
            _migratedOwners.ExceptWith(failures.Keys);
            _migratedOwners.ExceptWith(_removedOwners);
            var hadRemovals = _removedOwners.Count != 0;
            var migratedAny = _migratedOwners.Count != 0;
            _migrationIncomplete |= migrationErrors != 0 && removalErrors != 0;
            _restartNotice = hadRemovals || migratedAny || removalErrors != 0 || _migrationIncomplete;
            if (_restartNotice) BridgeSessionState.RequireRestart();
            var notices = new List<string>();
            if (migratedAny && hadRemovals) notices.Add("BridgeSelfCheckMigratedAndRemoved");
            else if (migratedAny) notices.Add("BridgeSelfCheckMigrated");
            else if (hadRemovals) notices.Add("BridgeSelfCheckRemoved");
            if (removalErrors != 0) notices.Add("BridgeSelfCheckIncomplete");
            if (_migrationIncomplete) notices.Add("BridgeSelfCheckMigrationIncomplete");
            _pendingNotice = notices.Count != 0 ? string.Join("|", notices) : manual ? "BridgeSelfCheckSuccess" : null;
            Mod.Log.Info($"Bridge self-check result: elapsedMs={timer.ElapsedMilliseconds}, validationMs={validationMs}, persistenceMs={persistenceMs}, retirementMs={timer.ElapsedMilliseconds - validationMs - persistenceMs}, owners={owners.Count}, removed={removed}, migrated={_migratedOwners.Count}, copiedAwaitingRestart={_copiedOwners.Count}, removalErrors={removalErrors}, migrationErrors={migrationErrors}; cached assets include registration failures; live-reference mutation disabled.");
        }
        finally
        {
            // No exception capture; restore lifecycle state even when the game API fails.
            IsStartupInspection = false;
            Enabled = _pendingNotice != null || _startupPending;
            Mod.Log.Info($"Bridge self-check finished: result={_pendingNotice ?? "passed"}, restartRecommended={_restartNotice}");
        }
    }
}
