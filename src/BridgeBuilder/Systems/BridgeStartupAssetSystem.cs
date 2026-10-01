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

/// <summary>Title-screen file recovery. Never deletes placed city entities.</summary>
public partial class BridgeStartupAssetSystem : GameSystemBase
{
    private PrefabSystem _prefabs = null!;
    private readonly BridgeCleanupConfirmation _confirmation = new();
    private int _lastCount = -1;
    private bool _titleReady;

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
        _confirmation.Clear();
        _lastCount = -1;
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        _titleReady = (mode & GameMode.MainMenu) != 0;
        Enabled = _titleReady;
    }

    protected override void OnUpdate()
    {
        if (!_titleReady || GameManager.instance == null
            || (GameManager.instance.gameMode & GameMode.MainMenu) == 0)
        { Enabled = false; return; }
        // A native batch failure or unreadable migration is not corruption evidence.
        if (BridgeLoadFailures.NetworkInitializationFailed)
        {
            Mod.Log.Warn("Title bridge recovery deferred: startup recovery requires a restart; files retained.");
            Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("MissingBridgesSuspended"));
            Enabled = false;
            return;
        }
        try
        {
            EntityManager.CompleteAllTrackedJobs();
            var loaded = PrefabCatalog.GetAll(_prefabs).Concat(BridgeLoadFailures.Prefabs())
                .Where(p => p != null).Distinct().ToArray();
            if (_lastCount != loaded.Length)
            { _lastCount = loaded.Length; _confirmation.Clear(); return; }
            var owners = new HashSet<string>(BridgeRegistrationStore.Load().Select(r => r.PrefabName),
                StringComparer.Ordinal);
            foreach (var prefab in loaded)
                if (!prefab.isBuiltin && !prefab.isReadOnly && prefab.asset != null
                    && BridgeMissingAssetSystem.TryBridgeName(prefab.name, out var owner)) owners.Add(owner);
            var audit = BridgeDiskAudit.Read(UnityEngine.Application.persistentDataPath, owners, cid =>
                Colossal.IO.AssetDatabase.AssetDatabase.global.TryGetAsset<Colossal.IO.AssetDatabase.GeometryAsset>(cid, out var geometry)
                && (geometry.database != Colossal.IO.AssetDatabase.AssetDatabase.user
                    || BridgeFileAccess.Exists(geometry.path)));
            if (!audit.Complete)
            {
                Mod.Log.Warn("Title bridge audit incomplete; files retained: " + audit.Error);
                Enabled = false;
                return;
            }
            foreach (var net in loaded.OfType<NetGeometryPrefab>())
            {
                if (net.isBuiltin || net.isReadOnly
                    || !BridgeMissingAssetSystem.TryBridgeName(net.name, out var owner)
                    || BridgeStartupRecovery.Retired.Contains(owner)) continue;
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
                    if (_prefabs.AddPrefab(net)) { _confirmation.Clear(); return; }
                }
            }
            foreach (var retired in BridgeStartupRecovery.Retired) audit.Failures.Remove(retired);
            var confirmed = _confirmation.Observe(audit.Failures, UnityEngine.Time.frameCount);
            if (audit.Failures.Count != 0 && !confirmed.SetEquals(audit.Failures.Keys)) return;
            if (confirmed.Count > 0)
            {
                foreach (var failure in audit.Failures)
                    Mod.Log.Warn($"Title recovery: retiring unrepaired bridge '{failure.Key}': {failure.Value}");
                if (!World.GetOrCreateSystemManaged<BridgeGenerationSystem>()
                    .RetireInvalidBridgeFilesAtTitle(audit, confirmed))
                {
                    Mod.Log.Warn("Title bridge retirement stopped; protected references/recovery files retained.");
                    Enabled = false;
                    return;
                }
            }
            Mod.Log.Info($"Title bridge recovery complete: {BridgeStartupRecovery.Retired.Count} retired identity group(s); "
                + "placed-network cleanup is deferred until a save is loaded.");
            Enabled = false;
        }
        catch (Exception exception)
        {
            Enabled = false;
            Mod.Log.Warn(exception, "Title bridge recovery stopped; inspection failure is not deletion authority.");
        }
    }
}
