using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

// Read only tickets written by version 95. Normal deletion never creates or looks for consent files.
internal static class BridgeLegacyRemoval
{
    private static readonly Dictionary<string,string> Tickets=new(StringComparer.Ordinal);
    private static string Inbox=>Path.Combine(UnityEngine.Application.persistentDataPath,"ModsData","BridgeBuilder","PendingRemoval");

    internal static IEnumerable<string> Load()
    {
        Tickets.Clear();
        if(!Directory.Exists(Inbox)) yield break;
        BridgeRemovalFiles.RejectLink(Inbox);
        foreach(string ticket in Directory.GetFiles(Inbox,"*.ticket"))
        {
            var owner=Path.GetFileNameWithoutExtension(ticket);
            if(!BridgeAssetInfo.IsPrefabName(owner) || (File.GetAttributes(ticket)&FileAttributes.ReparsePoint)!=0) continue;
            string token=File.ReadAllText(ticket);
            if(!Regex.IsMatch(token,@"\A[0-9a-f]{32}\z")) continue;
            string directory=BridgeRemovalFiles.Match(UnityEngine.Application.persistentDataPath,owner);
            string proof=Path.Combine(directory,"BridgeRemoval.pending");
            string detached=Path.Combine(Inbox,owner+"."+token+".detached");
            if(BridgeFileAccess.Exists(directory) && !BridgeFileAccess.Exists(detached)
                && (!File.Exists(proof) || (File.GetAttributes(proof)&FileAttributes.ReparsePoint)!=0 || File.ReadAllText(proof)!=token))
            { Mod.Log.Error("Conflicting legacy bridge removal ticket: "+owner); continue; }
            Tickets.Add(owner,token);
            yield return owner;
        }
    }

    internal static bool IsDetached(string owner,out string path)
    {
        path="";
        if(!Tickets.TryGetValue(owner,out var token)) return false;
        path=Path.Combine(Inbox,owner+"."+token+".detached");
        return BridgeFileAccess.Exists(path);
    }

    internal static void Finish(string owner)
    {
        if(!Tickets.TryGetValue(owner,out var token)) return;
        string ticket=Path.Combine(Inbox,owner+".ticket");
        if(File.ReadAllText(ticket)!=token) throw new IOException("Legacy ticket changed.");
        File.Delete(ticket);
        if(File.Exists(ticket)) throw new IOException("Legacy ticket remains: "+owner);
        Tickets.Remove(owner);
    }
}
