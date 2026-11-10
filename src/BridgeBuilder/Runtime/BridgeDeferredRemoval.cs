using System.Collections.Generic;
using System;
using Game.SceneFlow;

namespace BridgeBuilder.Runtime;

// Native work is session-local; consent survives in the marker/outside ledger.
internal static class BridgeDeferredRemoval
{
    internal static readonly System.Collections.Generic.HashSet<string> ReadyOwners = new(StringComparer.Ordinal);
    private static readonly System.Collections.Generic.Dictionary<string,(string Name,int Count)> Results = new();
    internal static void Remember(string owner,string name,int count) => Results[owner]=(name,count);
    internal static (string Name,int Count) Result(string owner) => Results.TryGetValue(owner,out var info)?info:(owner,0);
    internal static readonly System.Collections.Generic.HashSet<string> Verified = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Silent = new(StringComparer.Ordinal);
    internal static readonly Colossal.Logging.ILog SilentLog = Colossal.Logging.LogManager.GetLogger("BridgeBuilder.Deletion").SetShowsErrorsInUI(false);
    internal static bool IsSilent(string owner) => Silent.Contains(owner);
    internal static bool Excluded(string owner) => BridgeDeletionIntent.Contains(UnityEngine.Application.persistentDataPath, owner);
    internal static bool ExcludedPrefab(Game.Prefabs.PrefabBase prefab) =>
        (BridgeAssetInfo.TryFileOwner(prefab.name, out var owner) || BridgeAssetInfo.TryFileOwner(prefab.asset?.path, out owner)) && Excluded(owner);
    private static int _saves;
    internal static bool Saving => System.Threading.Volatile.Read(ref _saves) > 0;
    private static GameManager? _game;
    internal static void Initialize(string installation)
    {
        Silent.Clear(); ReadyOwners.Clear(); Verified.Clear(); Results.Clear();
        _game=GameManager.instance;
        _game.onGameSaveLoad+=SaveChanged;
        foreach(string owner in BridgeLegacyRemoval.Load())
        {
            if(BridgeLegacyRemoval.IsDetached(owner,out _))
            { SilentLog.Error("Legacy detached deletion requires separate recovery; files retained: "+owner); continue; }
            try { Prepare(owner); } catch(Exception e) { SilentLog.Error("Legacy deletion intent migration failed: "+owner+"; "+e); }
        }
        Resume();
    }
    internal static void Resume()
    {
        try
        {
            foreach(var owner in BridgeDeletionIntent.Owners(UnityEngine.Application.persistentDataPath))
            {
                if(ReadyOwners.Contains(owner) || Verified.Contains(owner)) continue;
                Silent.Add(owner);
                ReadyOwners.Add(owner);
            }
        }
        catch(Exception e) { SilentLog.Error("Tombstone discovery failed; pending files retained: "+e); }
    }
    private static void SaveChanged(string name,string preview,bool start,bool success)
    {
        if(start) System.Threading.Interlocked.Increment(ref _saves);
        else if(Saving) System.Threading.Interlocked.Decrement(ref _saves);
    }
    internal static void Stop()
    {
        if(_game!=null) _game.onGameSaveLoad-=SaveChanged;
        _game=null; _saves=0;
        ReadyOwners.Clear();
    }
    internal static void Complete(string owner,bool success)
    {
        ReadyOwners.Remove(owner); Results.Remove(owner);
        if(success) Verified.Add(owner);
        // Never make durable deletion intent selectable again on a failed native phase.
        BridgeRuntimeRequests.Touch();
    }
    internal static bool Pending(string owner)=>ReadyOwners.Contains(owner);
    internal static void Prepare(string owner)=>BridgeDeletionIntent.Accept(UnityEngine.Application.persistentDataPath,owner);
    internal static void Schedule(string owner)
    {
        Prepare(owner);
        Verified.Remove(owner);
        ReadyOwners.Add(owner);
        BridgeRuntimeRequests.Touch();
    }
}
