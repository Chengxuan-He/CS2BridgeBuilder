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
    internal static int Context { get; private set; }
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
    internal static void Stop() { Context++; _pending = null; _current = null; _remaining.Clear(); _failed = false; }

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
            var owners = new HashSet<string>(BridgeOwnedDirectoryRemoval.Owners(root), StringComparer.Ordinal);
            if (owners.Count == 0)
            {
                Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RemoveAllBridgesEmpty"));
                return;
            }
            _failed = false;
            foreach (var owner in owners.OrderBy(o=>o)) _remaining.Enqueue(owner);
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
            if(BridgeDeferredRemoval.Verified.Contains(_current) && !BridgeFileAccess.Exists(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",_current)))
            { Completed(_current,true); return; }
            if(GameManager.instance.gameMode==GameMode.MainMenu || BridgeDeferredRemoval.Pending(_current))
            {
                try { BridgeDeferredRemoval.Schedule(_current); }
                catch(Exception e) { Failed(e.ToString()); Completed(_current,false); }
                return;
            }
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
