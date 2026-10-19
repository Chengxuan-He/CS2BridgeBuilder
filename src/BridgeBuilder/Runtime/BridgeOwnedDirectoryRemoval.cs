using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Manual deletion only. Startup inspection retains its legacy-aware disk audit.
internal static class BridgeOwnedDirectoryRemoval
{
    internal static IEnumerable<string> Owners(string gameRoot)
    {
        var root = Path.Combine(Path.GetFullPath(gameRoot), "ImportedData");
        if (!BridgeFileAccess.Exists(root)) return Array.Empty<string>();
        return Directory.GetDirectories(BridgeFileAccess.Native(root))
            .Select(Path.GetFileName).Where(BridgeAssetInfo.IsPrefabName).ToArray()!;
    }

    internal static bool Delete(string gameRoot, string owner, out string error)
    {
        error = "";
        try
        {
            if (!BridgeAssetInfo.IsPrefabName(owner)) throw new IOException("Invalid bridge directory name: " + owner);
            var root = Path.GetFullPath(Path.Combine(gameRoot, "ImportedData"));
            var target = Path.GetFullPath(Path.Combine(root, owner));
            if (!string.Equals(Path.GetDirectoryName(target), root, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(target), owner, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Bridge directory is outside ImportedData.");
            if (!BridgeFileAccess.Exists(target)) return true;
            // Do not recursively follow linked directories outside this exact owner.
            CheckDirectory(target);
            Directory.Delete(BridgeFileAccess.Native(target), true);
            if (BridgeFileAccess.Exists(target)) throw new IOException("Bridge directory remains: " + target);
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    private static void CheckDirectory(string path)
    {
        if ((File.GetAttributes(BridgeFileAccess.Native(path)) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Refusing to remove linked bridge directory: " + path);
        foreach (var child in Directory.GetDirectories(BridgeFileAccess.Native(path))) CheckDirectory(child);
    }
}
