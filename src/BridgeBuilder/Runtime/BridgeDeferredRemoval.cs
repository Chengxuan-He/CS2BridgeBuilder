using System.Collections.Generic;
using System;
using Game.SceneFlow;

namespace BridgeBuilder.Runtime;

// User requests exist only in this game session.
internal static class BridgeDeferredRemoval
{
    internal static readonly System.Collections.Generic.HashSet<string> ReadyOwners = new(StringComparer.Ordinal);
    private static readonly System.Collections.Generic.Dictionary<string,(string Name,int Count)> Results = new();
    internal static void Remember(string owner,string name,int count) => Results[owner]=(name,count);
    internal static (string Name,int Count) Result(string owner) => Results.TryGetValue(owner,out var info)?info:(owner,0);
    internal static readonly System.Collections.Generic.HashSet<string> Verified = new(StringComparer.Ordinal);
    private static readonly HashSet<string> HiddenByRemoval = new(StringComparer.Ordinal);
    private static int _saves;
    internal static bool Saving => System.Threading.Volatile.Read(ref _saves) > 0;
    private static GameManager? _game;
    internal static void Initialize(string installation)
    {
        HiddenByRemoval.Clear(); ReadyOwners.Clear(); Verified.Clear(); Results.Clear();
        _game=GameManager.instance;
        _game.onGameSaveLoad+=SaveChanged;
        foreach(string owner in BridgeLegacyRemoval.Load()) ReadyOwners.Add(owner);
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
        if(HiddenByRemoval.Remove(owner) && !success) BridgeStartupRecovery.Retired.Remove(owner);
        BridgeRuntimeRequests.Touch();
    }
    internal static bool Pending(string owner)=>ReadyOwners.Contains(owner);
    internal static void Prepare(string owner)=>BridgeRemovalFiles.Match(UnityEngine.Application.persistentDataPath,owner);
    internal static void Schedule(string owner)
    {
        Prepare(owner);
        Verified.Remove(owner);
        ReadyOwners.Add(owner);
        if(BridgeStartupRecovery.Retired.Add(owner)) HiddenByRemoval.Add(owner);
        BridgeRuntimeRequests.Touch();
    }
}
