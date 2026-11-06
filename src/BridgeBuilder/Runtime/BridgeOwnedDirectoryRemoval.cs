using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Manual deletion only. Startup inspection retains its legacy-aware disk audit.
internal static class BridgeOwnedDirectoryRemoval
{
    internal static string? AssetPath(string gameRoot, string owner, string? path)
    {
        // The native database also contains transient/dummy entries without files.
        // They have no physical owner and must not enter filesystem normalization.
        if (string.IsNullOrWhiteSpace(path) || !BridgeAssetInfo.IsPrefabName(owner)) return null;
        var directory = Path.GetFullPath(Path.Combine(gameRoot, "ImportedData", owner))
            + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(BridgeFileAccess.Logical(path));
        return full.StartsWith(directory, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    private static readonly System.Text.RegularExpressions.Regex OwnerName = new(
        @"\Ab[0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}\z");

    internal static IEnumerable<string> Owners(string gameRoot)
    {
        var root = Path.Combine(Path.GetFullPath(gameRoot), "ImportedData");
        if (!BridgeFileAccess.Exists(root)) return Array.Empty<string>();
        BridgeRemovalFiles.RejectLink(gameRoot);
        BridgeRemovalFiles.RejectLink(root);
        return Directory.GetDirectories(BridgeFileAccess.Native(root))
            .Select(Path.GetFileName).Where(name => name != null && OwnerName.IsMatch(name)).ToArray()!;
    }

}
