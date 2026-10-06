using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.UI;





using CS2Mods.Shared.Infrastructure;
using Game;



using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private void CreateRuntimeBridge(BridgeRuntimeRequest request)
    {
        var state = ExportStateStore.Load();
        var report = new ExportReport(logIssues: false);
        var upper = DeckCatalog.Find(request.UpperDeckId);
        var lower = string.IsNullOrEmpty(request.LowerDeckId)
            ? null
            : DeckCatalog.Find(request.LowerDeckId);
        var style = BridgeStyleCatalog.Find(request.StyleId);

        if (upper == null)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected upper network is no longer registered."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("UpperUnavailable");
            return;
        }

        if (style == null || !style.IsInstalled)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected bridge prototype is no longer installed."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("StyleUnavailable");
            return;
        }

        var doubleDeck = !string.IsNullOrEmpty(request.LowerDeckId);
        if (doubleDeck && lower == null)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected lower network is no longer registered."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("LowerUnavailable");
            return;
        }

        if (!style.Variants.Any(variant => variant.IsDoubleDeck == doubleDeck))
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                doubleDeck
                    ? "The selected bridge style has no double-deck prototype."
                    : "The selected bridge style has no single-deck prototype."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete(doubleDeck
                ? "NoDoublePrototype"
                : "NoSinglePrototype");
            return;
        }

        var loaded = LoadedExportNames();
        var prefabName = string.Empty;
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var candidate = BridgeAssetInfo.NewPrefabName();
            if (loaded.Contains(candidate) || state.Contains(candidate)
                || BridgeAssetCatalog.Find(candidate) != null)
                continue;
            prefabName = candidate;
            break;
        }

        if (prefabName.Length == 0)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "A unique bridge UUID could not be allocated."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("UuidFailed");
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? BridgeNaming.BaseName(upper, lower, style)
            : request.DisplayName.Trim();
        var options = new BridgeOptions
        {
            DoubleDeck = doubleDeck,
            LowerDeckId = lower?.Id,
            LowerDeckOpposite = request.LowerDeckOpposite,
        };

        // Reserve the UUID in memory; persist incomplete-creation state on the root prefab itself.
        if (!BridgeAssetCatalog.Begin(new BridgeAssetInfo(prefabName, displayName,
            upper.Id, lower?.Id, style.Id, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), pending: true)))
        {
            BridgeRuntimeRequests.Complete("AssetMetadataFailed", displayName);
            return;
        }

        if (!TryBuildBridge(
            upper, lower, style, prefabName, options, overwrite: false, report,
            onPublished: ready =>
            {
                var committed = false;
                try
                {
                    if (ready)
                        committed = CompleteRuntimeBridge(upper, lower, style, prefabName, displayName, state, report,
                            request.BuildAfterCreate);
                }
                catch (Exception exception) { report.Failed(prefabName, exception); }
                if (!committed) DeferFailedCreation(prefabName, report);
                Finish(report, state, "Create runtime bridge", showMessage: false);
                // Refresh only after publication and export-state persistence, so the
                // new bridge cannot re-enter the selectable source-road catalogue.
                if (committed) Refresh();
            }))
        {
            DeferFailedCreation(prefabName, report);
            Finish(report, state, "Create runtime bridge", showMessage: false);
        }
    }

    private bool CompleteRuntimeBridge(
        Deck upper, Deck? lower, BridgeStyle style, string prefabName, string displayName,
        ExportStateStore state, ExportReport report, bool buildAfterCreate)
    {
        var recorded = BridgeAssetCatalog.Commit(prefabName);

        if (recorded)
        {
            try
            {
                state.Record(prefabName, RuntimeFingerprint(upper, lower, style));
                Mod.ReloadActiveLocale();
                var activated = buildAfterCreate && ActivatePrefab(prefabName);
                BridgeRuntimeRequests.Complete(activated
                    ? "CreatedActive"
                    : !buildAfterCreate ? "CreatedManage"
                    : _activationLocked ? "CreatedLocked" : "ActivateUnloaded",
                    displayName, prefabName);
                // Create-only confirms successful publication/assetInfo without entering the tool.
                // Create-and-build keeps its existing activation and locked-bridge dialog behavior.
                if (!buildAfterCreate)
                    Mod.ShowMessage(UiStringCatalog.Current.Title,
                        RuntimeUiText.Get("CreatedManage", displayName));
            }
            catch (Exception exception)
            {
                // The asset metadata is the commit point. UI/cache failures cannot undo it.
                report.Warning("Bridge committed, but its UI/cache refresh failed: " + exception.Message);
                BridgeRuntimeRequests.Complete("CreatedManage", displayName, prefabName);
            }
            return true;
        }
        else
        {
            report.Failed(prefabName, new IOException(
                "The prefab was created, but its completion metadata could not be saved."));
            BridgeRuntimeRequests.Complete("AssetMetadataFailed", displayName);
            return false;
        }
    }

    private void DeferFailedCreation(string prefabName, ExportReport report)
    {
        BridgeAssetCatalog.EndCreation(prefabName);
        // Keep live native indices and geometry allocated; retire audited disk files at boot.
        foreach (var prefab in PrefabCatalog.GetAll(_prefabSystem).OfType<NetGeometryPrefab>())
            if (BridgeSessionState.TryOwner(prefab.name, out var owner) && owner == prefabName)
                HideRemovedBridge(prefab);
        report.Warning($"Creation '{prefabName}' did not commit; owned files will be backed up and retired on restart.");
        BridgeRuntimeRequests.Complete("CreateFailed");
    }

    private static string RuntimeFingerprint(Deck upper, Deck? lower, BridgeStyle style)
    {
        return string.Join("|", new[]
        {
            "runtime-v1",
            upper.Road?.Fingerprint ?? upper.Id,
            style.Id,
            lower?.Id ?? string.Empty,
            lower == null ? "single" : "opp",
        });
    }

    private void ActivateRuntimeBridge(string prefabName)
    {
        if (!BridgeAssetInfo.IsPrefabName(prefabName)
            || BridgeAssetCatalog.Find(prefabName) == null)
        {
            BridgeRuntimeRequests.Complete("ActivateInvalid");
            return;
        }

        BridgeRuntimeRequests.Complete(ActivatePrefab(prefabName)
            ? "Activated"
            : _activationLocked ? "ActivateLocked" : "ActivateUnloaded");
    }

    private bool ActivatePrefab(string prefabName)
    {
        _activationLocked = false;
        try
        {
            var prefab = PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.name, prefabName, StringComparison.Ordinal));
            if (prefab == null) return false;
            if ((_gameMode & GameMode.Game) != 0)
            {
                if (!BridgeAssetInfo.IsPrefabName(prefabName)
                    || !BridgeUnlockPolicy.TryPrepareBuild(prefab, _prefabSystem, EntityManager,
                        out var locked)) return false;
                if (locked)
                {
                    _activationLocked = true;
                    // Both Create-and-build and management Build enter here. Only the original
                    // prototype's actual lock state may produce the not-unlocked dialog.
                    Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("ActivateLocked"));
                    return false;
                }
            }
            if (!World.GetOrCreateSystemManaged<ToolSystem>().ActivatePrefabTool(prefab)) return false;
            World.GetExistingSystemManaged<BridgeBuilderUISystem>()?.CloseForBuild();
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, $"Could not activate runtime bridge '{prefabName}'");
            return false;
        }
    }

    private void RenameRuntimeBridge(string prefabName, string displayName)
    {
        if (!BridgeAssetInfo.IsPrefabName(prefabName))
        {
            BridgeRuntimeRequests.Complete("RenameInvalid");
            return;
        }
        if (string.IsNullOrWhiteSpace(displayName))
        {
            BridgeRuntimeRequests.Complete("NameRequired");
            return;
        }

        if (BridgeAssetCatalog.Find(prefabName)?.DisplayName == displayName.Trim())
        {
            BridgeRuntimeRequests.Complete("Renamed", displayName.Trim());
            return;
        }
        if (!BridgeAssetCatalog.Rename(prefabName, displayName))
        {
            BridgeRuntimeRequests.Complete("RenameFailed");
            return;
        }

        // Keep the live object consistent so a later native save cannot restore the old label.
        foreach (var prefab in PrefabCatalog.GetAll(_prefabSystem).Where(p => p.name == prefabName))
        {
            var metadata = prefab.GetComponent<BridgeConstructionCost>();
            if (metadata != null) metadata.m_BridgeDisplayName = displayName.Trim();
        }

        // A label edit must not run asset-pack maintenance or rebuild the deck/preview
        // catalogues. Keep the persistent UUID and the live network graph untouched.
        Mod.ReloadActiveLocale();
        BridgeBuilderUISystem.RequestRefresh();
        World.GetExistingSystemManaged<BridgeRailSeamAuditSystem>()?.Restart("bridge renamed: " + prefabName);
        Mod.Log.Info($"Bridge renamed: prefab={prefabName}; display name persisted; network identity unchanged.");
        BridgeRuntimeRequests.Complete(
            "Renamed", displayName.Trim(), prefabName);
    }

    private void DeleteRuntimeBridge(string prefabName)
    {
        var assetInfo = BridgeAssetCatalog.Find(prefabName);
        if (!BridgeAssetInfo.IsPrefabName(prefabName) || assetInfo == null)
        {
            FailDeletion(prefabName, "DeleteMissing", "Bridge UUID assetInfo is missing or invalid.");
            return;
        }

        try
        {
            var roots = RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)).ToArray();
            if (roots.Length == 0)
            {
                FailDeletion(prefabName, "DeleteMissing", "No writable bridge prefab was found; assetInfo retained.");
                return;
            }
            var ids = new HashSet<Entity>();
            foreach (var root in roots)
                if (_prefabSystem.TryGetEntity(root, out var id)) ids.Add(id);
            var plan = BridgeInstanceRemoval.Collect(EntityManager, ids);
            if (!plan.CanApply(EntityManager, out var blocked))
            {
                FailDeletion(prefabName, "DeleteUnsafe", blocked);
                return;
            }
            // Stop placement before marking any entity. A temporary tool preview is not a
            // placed bridge, and must be released by its owning tool, not by our PrefabRef query.
            var tools = World.GetOrCreateSystemManaged<ToolSystem>();
            if (roots.Contains(tools.activePrefab)) tools.ActivatePrefabTool(null);
            if (plan.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
            ClearPreview();
            BridgePreviewState.Clear();
            plan.Apply(EntityManager);
            _pendingRemoval = plan;
            _removingAsset = assetInfo;
            _removalStartedUtc = DateTime.UtcNow;
            Mod.Log.Info($"Removing '{prefabName}': {plan.DeletedEntities.Count} network entities; "
                + "waiting for native cleanup before deleting assets. Composition caches are retained.");
            if (plan.IsComplete(EntityManager)) CompleteRuntimeDeletion();
        }
        catch (Exception exception)
        {
            FailDeletion(prefabName, "DeleteUnsafe", "Could not safely begin deletion.", exception);
        }
    }

    private void CompleteRuntimeDeletion()
    {
        var assetInfo = _removingAsset;
        var placed = _pendingRemoval?.DeletedEntities.Count ?? 0;
        _pendingRemoval = null;
        _removingAsset = null;
        if (assetInfo == null) return;
        var prefabName = assetInfo.PrefabName;
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var removed = RemoveByName(prefabName, state, report);
        if (removed.Count > 0
            && !RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)).Any())
        {
            Mod.ReloadActiveLocale();
            BridgeRuntimeRequests.Complete(
                "Deleted", assetInfo.DisplayName, placed);
        }
        else
        {
            FailDeletion(prefabName, "DeleteIncomplete",
                $"Deleted {removed.Count} root prefab(s), but prefab assets remain. "
                + "See ModsData/BridgeBuilder/last-export-report.txt for the blocking reference or asset failure.");
        }

        Finish(report, state, "Delete runtime bridge", showMessage: false);
        // Native removal has completed and state is saved. Also reflect partial
        // deletion accurately instead of keeping a stale list until reopening.
        Refresh();
    }

    private void FailDeletion(string prefabName, string stage, string reason, Exception? exception = null)
    {
        var message = $"Bridge deletion failed: stage='{stage}', prefab='{prefabName}'. {reason}";
        if (exception == null) Mod.Log.Critical(message);
        else Mod.Log.Critical(exception, message);
        _pendingRemoval = null;
        _removingAsset = null;
        BridgeRuntimeRequests.Complete(stage);
    }

    private HashSet<string> LoadedExportNames()
    {
        return new HashSet<string>(
            PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .Where(prefab => prefab.asset != null
                    && !prefab.isReadOnly)
                .Select(prefab => prefab.name),
            StringComparer.Ordinal);
    }

    private void RemoveOne()
    {
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var upper = DeckCatalog.Find(Mod.Setting?.UpperDeckId);
        if (upper == null)
        {
            report.Failed("(no deck)", new InvalidOperationException(
                "No upper deck is selected, so there is no exported bridge to remove."));
            Finish(report, state, "Remove bridge");
            return;
        }

        // The remover works from a Road Builder road, which most decks are not, so removal goes by the
        // name the export would have used. That is the same name in either case.
        var removed = RemoveByName(
            BridgeNaming.BaseName(upper, DeckCatalog.Find(Mod.Setting?.LowerDeckId), BridgeStyleCatalog.Resolve(Mod.Setting?.BridgeStyleId)),
            state,
            report);
        if (removed.Count > 0)
        {
            report.Warning("The removed prefabs stay registered in the running session. Restart the game to get rid of them.");
        }

        Finish(report, state, "Remove bridge");
    }

}
