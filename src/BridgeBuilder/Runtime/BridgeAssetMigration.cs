using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Runtime;

/// <summary>Version existing healthy assets without regenerating or serializing their live graph.</summary>
internal static class BridgeAssetMigration
{
    internal const int CurrentVersion = 1;

    internal static bool Run(string owner, IEnumerable<string> seeds, string backup,
        out bool changed, out string error)
    {
        changed = false;
        error = "";
        var root = BridgeAssetCatalog.Root;
        var directory = Path.Combine(root, owner);
        var path = Path.Combine(directory, owner + ".Prefab");
        foreach (var item in new[] { root, directory, path, path + ".cid" })
            if (!BridgeFileAccess.Exists(item) || (BridgeFileAccess.Attributes(item) & FileAttributes.ReparsePoint) != 0)
            { error = "Missing or unsafe migration source: " + item; return false; }
        var before = BridgeFileAccess.ReadText(path);
        if (!BridgeAssetMetadata.TryRead(before, out var info) || info.PrefabName != owner || info.Pending
            || !BridgeSerializedReferences.TryRead(before, out var document))
        { error = "Committed bridge metadata unavailable: " + owner; return false; }
        // Version markers do not exempt assets from dependency checks (including geometry).
        if (!BridgeDependencyPersistence.Save(owner, seeds, out var count, out error, out var writtenFiles)) return false;
        changed = writtenFiles != 0;
        var data = document.Component("BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder");
        if (data != null && data.Fields.TryGetValue("m_BridgePersistenceVersion", out var version)
            && int.TryParse(version.Text, out var number) && number >= CurrentVersion) return true;
        // Copy dependencies first; failures retain the original asset and never mark migration complete.
        if (BridgeFileAccess.ReadText(path) != before)
        { error = "Bridge changed during migration: " + owner; return false; }
        var target = Path.Combine(Path.GetFullPath(backup), "Migration", owner);
        var gameRoot = Path.GetDirectoryName(Path.GetFullPath(root))!;
        if (target.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        { error = "Migration backup must be outside game data"; return false; }
        for (var parent = new DirectoryInfo(target); parent != null; parent = parent.Parent)
            if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
            { error = "Reparse-point migration backup"; return false; }
        Directory.CreateDirectory(BridgeFileAccess.Native(target));
        File.Copy(BridgeFileAccess.Native(path), BridgeFileAccess.Native(Path.Combine(target, owner + ".Prefab")), false);
        File.Copy(BridgeFileAccess.Native(path + ".cid"), BridgeFileAccess.Native(Path.Combine(target, owner + ".Prefab.cid")), false);
        if (!BridgeAssetMetadata.Write(path, info, CurrentVersion))
        { error = "Migration metadata commit failed: " + owner; return false; }
        changed = true;
        Mod.Log.Info($"Bridge migration committed UUID={owner}, version={CurrentVersion}, dependencyCopies={count}; identity and geometry preserved; backup={target}");
        return true;
    }
}
