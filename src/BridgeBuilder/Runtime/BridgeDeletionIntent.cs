using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Durable consent, independent of native registrations and process-local removal queues.
internal static class BridgeDeletionIntent
{
    internal const string Marker = ".bridgebuilderdeleted";
    internal static string Ledger(string root) => Path.Combine(root, "ModsData", "BridgeBuilder", "DeletionPending");
    internal static string Record(string root, string owner) => Path.Combine(Ledger(root), owner + ".pending");

    internal static bool Contains(string root, string owner)
    {
        if (!BridgeAssetInfo.IsPrefabName(owner)) return false;
        // Even malformed intent is excluded, but cannot authorize map/file mutation.
        return BridgeFileAccess.Exists(Path.Combine(root, "ImportedData", owner, Marker))
            || BridgeFileAccess.Exists(Record(root, owner));
    }

    internal static IEnumerable<string> Owners(string root)
    {
        var owners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var owner in BridgeOwnedDirectoryRemoval.Owners(root))
            if (Contains(root, owner)) owners.Add(owner);
        var ledger = Ledger(root);
        if (Directory.Exists(ledger))
        {
            BridgeRemovalFiles.RejectLink(ledger);
            foreach (var path in Directory.GetFileSystemEntries(ledger, "*.pending"))
            {
                var owner = Path.GetFileNameWithoutExtension(path);
                if (BridgeAssetInfo.IsPrefabName(owner)) owners.Add(owner);
            }
        }
        return owners;
    }

    internal static void Accept(string root, string owner)
    {
        string directory = BridgeRemovalFiles.Match(root, owner);
        if (Directory.Exists(directory)) CreateRecord(Path.Combine(directory, Marker));
        else CommitLedger(root, owner); // Map-only intent must not manufacture a bridge folder.
    }

    internal static void Verify(string root, string owner)
    {
        var directory = BridgeRemovalFiles.Match(root, owner);
        var marker = Path.Combine(directory, Marker);
        var record = Record(root, owner);
        if (File.Exists(marker)) VerifyRecord(marker);
        else if (File.Exists(record)) VerifyRecord(record);
        else throw new IOException("No valid durable bridge deletion intent: " + owner);
    }

    internal static void CommitLedger(string root, string owner)
    {
        BridgeRemovalFiles.Match(root, owner);
        string ledger = Ledger(root);
        // Reject any existing linked parent before creating the outside record directory.
        for (string? p = ledger; p != null && p.Length >= Path.GetFullPath(root).Length; p = Path.GetDirectoryName(p))
            if (Directory.Exists(p)) BridgeRemovalFiles.RejectLink(p);
        Directory.CreateDirectory(ledger);
        CreateRecord(Record(root, owner));
    }

    // Only the verified normal-shutdown caller may invoke this primitive. Tests use isolated roots.
    internal static void Cleanup(string root,string owner)
    {
        Verify(root,owner);
        string directory = BridgeRemovalFiles.Match(root,owner);
        if(Directory.Exists(directory)) RejectTreeLinks(directory);
        CommitLedger(root,owner); // Survives deletion of the in-directory marker.
        BridgeRemovalFiles.Clear(root,owner);
        if(BridgeFileAccess.Exists(directory)) throw new IOException("Bridge directory remains: "+owner);
        File.Delete(Record(root,owner));
        if(BridgeFileAccess.Exists(Record(root,owner))) throw new IOException("Cleanup record remains: "+owner);
    }

    private static void RejectTreeLinks(string directory)
    {
        BridgeRemovalFiles.RejectLink(directory);
        // Path safety only: do not enumerate/read payloads or perform per-file removal.
        foreach(var child in Directory.EnumerateDirectories(directory)) RejectTreeLinks(child);
    }

    private static void CreateRecord(string path)
    {
        if (BridgeFileAccess.Exists(path)) { VerifyRecord(path); return; }
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            1, FileOptions.WriteThrough)) file.Flush(flushToDisk: true);
        VerifyRecord(path);
    }

    private static void VerifyRecord(string path)
    {
        if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0
            || new FileInfo(path).Length != 0)
            throw new IOException("Invalid bridge deletion intent record: " + path);
    }
}
