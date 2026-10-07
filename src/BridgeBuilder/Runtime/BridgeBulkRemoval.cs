using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Settings;
using BridgeBuilder.Systems;
using Game.SceneFlow;
using Game.UI.Localization;
using Game.UI;

namespace BridgeBuilder.Runtime;

internal static class BridgeBulkRemoval
{
    private static object? _pending;
    internal static bool CanRequest => _pending == null && BridgeStartupAssetSystem.CanCheck;
    internal static void Stop() => _pending = null;

    internal static void RequestConfirmation()
    {
        if (!CanRequest) return;
        var bindings = GameManager.instance?.userInterface?.appBindings;
        if (bindings == null) return;
        var confirmation = _pending = new object();
        try
        {
            var dialog = new ConfirmationDialog(
                LocalizedString.Value(RuntimeUiText.Get("RemoveAllBridgesLabel")),
                LocalizedString.Value(RuntimeUiText.Get("RemoveAllBridgesConfirm")),
                LocalizedString.Value(RuntimeUiText.Get("RemoveAllBridgesLabel")),
                LocalizedString.Value(RuntimeUiText.Get("Cancel")), Array.Empty<LocalizedString>());
            bindings.ShowConfirmationDialog(dialog, result =>
            {
                if (!ReferenceEquals(_pending, confirmation)) return;
                _pending = null;
                // Closing/cancelling, or leaving the ready main menu, never authorizes a move.
                if (result == 0 && BridgeStartupAssetSystem.CanCheck) RemoveConfirmed();
            });
        }
        catch (Exception exception)
        {
            _pending = null;
            Mod.Log.Warn(exception, "Could not display remove-all-bridges confirmation");
        }
    }

    private static void RemoveConfirmed()
    {
        try
        {
            var root = UnityEngine.Application.persistentDataPath;
            var audit = BridgeDiskAudit.ForAllBridges(root);
            if (!audit.Complete) { Failed(audit.Error); return; }
            if (audit.FileOwners.Count == 0)
            {
                Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RemoveAllBridgesEmpty"));
                return;
            }
            var owners = new HashSet<string>(audit.Failures.Keys, StringComparer.Ordinal);
            var backup = Path.Combine(BridgeRecoveryLocation.Path, "RemoveAll-" + Guid.NewGuid().ToString("N"));
            // Keep stale cached prefabs out of later self-check/catalogue passes, even after a partial move.
            // Files are unloaded by restarting; do not partially unregister the live PrefabSystem.
            BridgeSessionState.RequireRestart();
            BridgeStartupRecovery.Retired.UnionWith(owners);
            if (!audit.RetireFiles(owners, backup, out var error)) { Failed(error); return; }
            var remaining = BridgeDiskAudit.ForAllBridges(root);
            if (!remaining.Complete || remaining.FileOwners.Count != 0)
            { Failed(remaining.Complete ? "Matching bridge files remain after removal" : remaining.Error); return; }
            Mod.Log.Info($"User removed all Bridge Builder bridge assets; paths={audit.FileOwners.Count}; backup={backup}; restart required.");
            Mod.ShowRecoveryMessage(RuntimeUiText.Get("RemoveAllBridgesDone"));
        }
        catch (Exception exception) { Failed(exception.ToString()); }
    }

    private static void Failed(string error)
    {
        Mod.Log.Critical("Remove all Bridge Builder bridges failed: " + error);
        Mod.ShowRecoveryMessage(RuntimeUiText.Get("RemoveAllBridgesFailed"));
    }
}
