using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.UI;
using Colossal.Serialization.Entities;
using CS2Mods.Shared;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
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
            var candidate = BridgeRegistration.NewPrefabName();
            if (loaded.Contains(candidate) || state.Contains(candidate)
                || BridgeRegistrationStore.Find(candidate) != null)
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

        var registrationName = string.IsNullOrWhiteSpace(request.RegistrationName)
            ? BridgeNaming.BaseName(upper, lower, style)
            : request.RegistrationName.Trim();
        var options = new BridgeOptions
        {
            DoubleDeck = doubleDeck,
            LowerDeckId = lower?.Id,
            LowerDeckOpposite = request.LowerDeckOpposite,
        };

        // Reserve ownership before writing geometry. A crash leaves a pending row for recovery.
        if (!BridgeRegistrationStore.Begin(new BridgeRegistration(prefabName, registrationName,
            upper.Id, lower?.Id, style.Id, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), pending: true)))
        {
            BridgeRuntimeRequests.Complete("RegistrationFailed", registrationName);
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
                        committed = CompleteRuntimeBridge(upper, lower, style, prefabName, registrationName, state, report,
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
        Deck upper, Deck? lower, BridgeStyle style, string prefabName, string registrationName,
        ExportStateStore state, ExportReport report, bool buildAfterCreate)
    {
        var recorded = BridgeRegistrationStore.Commit(prefabName);

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
                    registrationName, prefabName);
                // Create-only confirms successful publication/registration without entering the tool.
                // Create-and-build keeps its existing activation and locked-bridge dialog behavior.
                if (!buildAfterCreate)
                    Mod.ShowMessage(UiStringCatalog.Current.Title,
                        RuntimeUiText.Get("CreatedManage", registrationName));
            }
            catch (Exception exception)
            {
                // The registry is the commit point. UI/cache failures cannot undo that commit.
                report.Warning("Bridge committed, but its UI/cache refresh failed: " + exception.Message);
                BridgeRuntimeRequests.Complete("CreatedManage", registrationName, prefabName);
            }
            return true;
        }
        else
        {
            report.Failed(prefabName, new IOException(
                "The prefab was created, but its runtime registration record could not be saved."));
            BridgeRuntimeRequests.Complete("RegistrationFailed", registrationName);
            return false;
        }
    }

    private void DeferFailedCreation(string prefabName, ExportReport report)
    {
        BridgeRegistrationStore.EndCreation(prefabName);
        // Keep live native indices and geometry allocated; retire audited disk files at boot.
        foreach (var prefab in PrefabCatalog.GetAll(_prefabSystem).OfType<NetGeometryPrefab>())
            if (BridgeLoadFailures.TryOwner(prefab.name, out var owner) && owner == prefabName)
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
        if (!BridgeRegistration.IsPrefabName(prefabName)
            || BridgeRegistrationStore.Find(prefabName) == null)
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
                if (!BridgeRegistration.IsPrefabName(prefabName)
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

    private void RenameRuntimeBridge(string prefabName, string registrationName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName))
        {
            BridgeRuntimeRequests.Complete("RenameInvalid");
            return;
        }
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            BridgeRuntimeRequests.Complete("NameRequired");
            return;
        }

        if (BridgeRegistrationStore.Find(prefabName)?.RegistrationName == registrationName.Trim())
        {
            BridgeRuntimeRequests.Complete("Renamed", registrationName.Trim());
            return;
        }
        if (!BridgeRegistrationStore.Rename(prefabName, registrationName))
        {
            BridgeRuntimeRequests.Complete("RenameFailed");
            return;
        }

        Mod.ReloadActiveLocale();
        Refresh();
        BridgeRuntimeRequests.Complete(
            "Renamed", registrationName.Trim(), prefabName);
    }

    private void DeleteRuntimeBridge(string prefabName)
    {
        var registration = BridgeRegistrationStore.Find(prefabName);
        if (!BridgeRegistration.IsPrefabName(prefabName) || registration == null)
        {
            FailDeletion(prefabName, "DeleteMissing", "Bridge UUID registration is missing or invalid.");
            return;
        }

        try
        {
            var roots = RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)).ToArray();
            if (roots.Length == 0)
            {
                FailDeletion(prefabName, "DeleteMissing", "No writable bridge prefab was found; registration retained.");
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
            _removingRegistration = registration;
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
        var registration = _removingRegistration;
        var placed = _pendingRemoval?.DeletedEntities.Count ?? 0;
        _pendingRemoval = null;
        _removingRegistration = null;
        if (registration == null) return;
        var prefabName = registration.PrefabName;
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var removed = RemoveByName(prefabName, state, report);
        if (removed.Count > 0
            && !RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)).Any()
            && BridgeRegistrationStore.Remove(prefabName))
        {
            Mod.ReloadActiveLocale();
            BridgeRuntimeRequests.Complete(
                "Deleted", registration.RegistrationName, placed);
        }
        else
        {
            FailDeletion(prefabName, "DeleteIncomplete",
                $"Deleted {removed.Count} root prefab(s), but prefab assets or UUID registration remain. "
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
        _removingRegistration = null;
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
