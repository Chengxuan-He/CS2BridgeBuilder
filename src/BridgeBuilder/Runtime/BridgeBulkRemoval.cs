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
    private static readonly HashSet<string> _outstanding = new(StringComparer.Ordinal);
    private static DateTime _progress;
    private static readonly TimeSpan StallLimit = TimeSpan.FromMinutes(3);
    internal static bool Authorizes(long batch,string owner) => batch == 0 ||
        (_batch == batch && _running && _mapReady && _outstanding.Contains(owner));
    private static long _sequence, _batch;
    private static string[] _targets = Array.Empty<string>();
    internal static bool MapWasClearedFor(string owner) => _running && _mapReady && _outstanding.Contains(owner);
    private static bool _ask, _confirmed, _running, _mapReady;
    internal static Func<bool>? NativeReady;
    private static bool Ready => NativeReady?.Invoke()==true && GameManager.instance != null
        && GameManager.instance.state == GameManager.State.WorldReady
        && !GameManager.instance.isGameLoading && BridgeAssetLoading.Ready
        && !BridgeStartupAssetSystem.IsStartupInspection && !BridgeDeferredRemoval.Saving
        && (GameManager.instance.gameMode == GameMode.MainMenu || (GameManager.instance.gameMode & (GameMode.Game | GameMode.Editor)) != 0);
    // Include placed legacy instances without an owner directory, but exclude newly created owners.
    internal static bool IncludesMapOwner(string owner) => Array.IndexOf(_targets,owner)>=0;
    internal static Func<IEnumerable<string>>? PlacedOwners;

    internal static void Stop()
    {
        if(_batch!=0) LogFailure("session ended before verification");
        Context++;
        _pending = null;
        _current = null;
        _remaining.Clear();
        _targets = Array.Empty<string>();
        _batch = 0;
        _outstanding.Clear();
        _ask = _confirmed = _running = _mapReady = false;
    }

    internal static void WorldChanging()
    {
        Context++;
        _mapReady = false;
        // An open modal belongs to the old UI. Re-offer it in the new one.
        if (_pending != null) { _pending = null; _ask = true; }
        if (_current == null) return;
        // Discard world-specific work. The new world must inspect its own placed entities.
        BridgeDeferredRemoval.Complete(_current, false);
        _remaining.Enqueue(_current);
        _current = null;
    }

    internal static void RequestConfirmation()
    {
        if (_pending != null || _batch != 0) return;
        _ask = true;
        ShowConfirmation();
    }

    internal static bool Update(Func<int> finishMap)
    {
        if (_ask) ShowConfirmation();
        if (_batch!=0 && DateTime.UtcNow-_progress>StallLimit)
        {
            Failed("timeout while waiting for "+(!_mapReady ? "map/readiness" : "owner scheduling or native cleanup"));
            return false;
        }
        if (_current!=null && BridgeDeferredRemoval.Pending(_current))
        {
            _current=null;
            _progress=DateTime.UtcNow;
        }
        // Pause unfinished work while its result cannot be presented in this UI.
        if (_batch != 0 && GameManager.instance?.userInterface?.appBindings == null) return false;
        if (!Ready || _current != null || (!_running && !_confirmed)) return true;
        int removed = 0;
        if (!_mapReady && GameManager.instance.gameMode != GameMode.MainMenu)
        {
            try
            {
                removed = finishMap();
                if (removed < 0) return false;
            }
            catch (Exception e)
            {
                _confirmed = _running = false;
                _remaining.Clear();
                Failed("map cleanup: "+e);
                return false;
            }
        }
        _mapReady = true;
        if (_running) { if (_remaining.Count != 0) Next(); }
        else
        {
            _confirmed = false;
            RemoveConfirmed(removed);
        }
        return true;
    }

    private static void ShowConfirmation()
    {
        var bindings = GameManager.instance?.userInterface?.appBindings;
        if (bindings == null) return;
        var confirmation = _pending = new object();
        _ask = false;
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
                if (result == 0)
                {
                    _batch = ++_sequence;
                    _progress=DateTime.UtcNow;
                    try
                    {
                        _targets=BridgeOwnedDirectoryRemoval.Owners(UnityEngine.Application.persistentDataPath)
                            .Concat(PlacedOwners?.Invoke() ?? Array.Empty<string>())
                            .Distinct(StringComparer.Ordinal).OrderBy(owner=>owner,StringComparer.Ordinal).ToArray();
                        foreach(var owner in _targets) BridgeDeferredRemoval.Prepare(owner);
                    }
                    catch(Exception e){ Failed("target discovery: "+e); return; }
                    _confirmed = true;
                    Mod.Log.Info("Confirmed remove-all batch " + _batch);
                }
            });
        }
        catch (Exception exception)
        {
            _pending = null;
            Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("RemoveAllBridgesFailed"));
            Mod.Log.Warn(exception, "Could not display remove-all-bridges confirmation");
        }
    }

    private static void RemoveConfirmed(int removedFromMap)
    {
        try
        {
            if (_targets.Length == 0)
            {
                FinishBatch(removedFromMap == 0 ? "RemoveAllBridgesEmpty" : "RemoveAllBridgesDone");
                return;
            }
            _running = true;
            _outstanding.UnionWith(_targets);
            _progress=DateTime.UtcNow;
            foreach (var owner in _targets) _remaining.Enqueue(owner);
            Next();
        }
        catch (Exception exception) { _mapReady = false; Failed(exception.ToString()); }
    }

    internal static void Completed(string owner, bool success)
    {
        if (!_running || !_outstanding.Remove(owner)) return;
        _progress=DateTime.UtcNow;
        if (_current==owner) _current=null;
        if (!success) LogFailure("owner retirement "+owner);
        if (_outstanding.Count!=0) return;
        try
        {
            var remaining=Remaining();
            if(remaining.Length!=0) { Failed("final logical/index/management verification"); return; }
            FinishBatch("RemoveAllBridgesDone");
        }
        catch(Exception e){ Failed("terminal verification: "+e); }
    }

    private static string[] Remaining()
    {
        var root=System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData");
        var visible=BridgeBuilder.UI.BridgeBuilderUISystem.SyncVerifiedRemoval(_targets);
        return _targets.Where(owner=>!BridgeDeferredRemoval.Verified.Contains(owner) ||
            !BridgeDeferredRemoval.Excluded(owner) || visible.Contains(owner)).ToArray();
    }

    private static void LogFailure(string stage)
    {
        string[] remaining;
        try { remaining=Remaining(); } catch(Exception e) { remaining=_targets; stage+="; verification failed: "+e.Message; }
        Mod.Log.Error($"Remove-all batch {_batch} failed: stage={stage}; remainingCount={remaining.Length}; remainingUUIDs=[{string.Join(", ",remaining)}].");
    }

    private static void Next()
    {
        _current = _remaining.Dequeue();
        if(GameManager.instance.gameMode==GameMode.MainMenu || BridgeDeferredRemoval.Pending(_current))
        {
            try { BridgeDeferredRemoval.Schedule(_current); }
            catch(Exception e) { Mod.Log.Error("Could not schedule bridge removal: "+e); Completed(_current,false); }
            return;
        }
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest { Action = BridgeRuntimeAction.Delete,
            PrefabName = _current, RemovalBatch = _batch }, "");
    }

    private static void FinishBatch(string key)
    {
        long batch = _batch;
        int count = _targets.Length;
        _batch = 0;
        _confirmed = _running = _mapReady = false;
        _targets = Array.Empty<string>();
        _remaining.Clear();
        _outstanding.Clear();
        _current = null;
        if (batch == 0) return;
        string messageKey = key == "RemoveAllBridgesEmpty" ? "RemoveAllBridgesDone" : key;
        bool sent = Mod.TryShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get(messageKey));
        Mod.Log.Info($"Remove-all batch {batch} finished: targets={count}, result={key}, nativeDialogSent={sent}");
        // An unavailable UI cannot receive this event. Never replay a terminal result.
    }

    private static void Failed(string error)
    {
        LogFailure(error);
        foreach(var owner in _outstanding)
            if(BridgeDeferredRemoval.Pending(owner)) BridgeDeferredRemoval.Complete(owner,false);
        FinishBatch("RemoveAllBridgesFailed");
    }
}
