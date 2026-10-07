using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using BridgeBuilder.Settings;
using BridgeBuilder.Systems;
using Game.SceneFlow;
using Game.UI.Localization;
using Game.UI;

namespace BridgeBuilder.Runtime;

internal static class BridgeBulkRemoval
{
    private static object? _pending;
    private static readonly Queue<string> _remaining = new();
    private static string? _current;
    private static bool _failed;
    private static bool Ready => GameManager.instance != null
        && GameManager.instance.state == GameManager.State.WorldReady
        && !GameManager.instance.isGameLoading && BridgeAssetLoading.Ready
        && !BridgeStartupAssetSystem.IsStartupInspection
        && (GameManager.instance.gameMode == GameMode.MainMenu || (GameManager.instance.gameMode & GameMode.Game) != 0);
    internal static bool CanRequest => _pending == null && _current == null && Ready;
    internal static void Stop() { _pending = null; _current = null; _remaining.Clear(); _failed = false; }

    internal static void RequestConfirmation()
    {
        if (!CanRequest) return;
        var bindings = GameManager.instance?.userInterface?.appBindings;
        if (bindings == null) return;
        var confirmation = _pending = new object();
        var mode = GameManager.instance!.gameMode;
        try
        {
            var dialog = new ConfirmationDialog(
                LocalizedString.Value(RuntimeUiText.Get("Warning")),
                LocalizedString.Value(RuntimeUiText.Get("RemoveAllBridgesConfirm")),
                LocalizedString.Value(RuntimeUiText.Get("ContinueRemoval")),
                LocalizedString.Value(RuntimeUiText.Get("Cancel")), Array.Empty<LocalizedString>());
            bindings.ShowConfirmationDialog(dialog, result =>
            {
                if (!ReferenceEquals(_pending, confirmation)) return;
                _pending = null;
                // Confirmation is valid only in the same ready game context.
                if (result == 0 && Ready && GameManager.instance.gameMode == mode) RemoveConfirmed();
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
            var owners = new HashSet<string>(audit.Failures.Keys, StringComparer.Ordinal);
            foreach (var entry in BridgeAssetCatalog.Load()) owners.Add(entry.PrefabName);
            var prefabs = Unity.Entities.World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<Game.Prefabs.PrefabSystem>();
            if (prefabs != null)
                foreach (var prefab in CS2Mods.Shared.Infrastructure.PrefabCatalog.GetAll(prefabs))
                    if (BridgeAssetInfo.TryFileOwner(prefab.name, out var owner)) owners.Add(owner);
            if (owners.Count == 0)
            {
                Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RemoveAllBridgesEmpty"));
                return;
            }
            _failed = false;
            foreach (var owner in owners.OrderBy(o => o)) _remaining.Enqueue(owner);
            Next();
        }
        catch (Exception exception) { Failed(exception.ToString()); }
    }

    internal static void Completed(string owner, bool success)
    {
        if (_current != owner) return;
        _failed |= !success;
        _current = null;
        Next();
    }

    private static void Next()
    {
        if (_remaining.Count != 0)
        {
            _current = _remaining.Dequeue();
            BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest { Action = BridgeRuntimeAction.Delete,
                PrefabName = _current }, "");
            return;
        }
        Mod.ShowMessage(UiStringCatalog.Current.Title,
            RuntimeUiText.Get(_failed ? "RemoveAllBridgesFailed" : "RemoveAllBridgesDone"));
    }

    private static void Failed(string error)
    {
        Mod.Log.Error("Remove all Bridge Builder bridges failed: " + error);
        Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RemoveAllBridgesFailed"));
    }
}
