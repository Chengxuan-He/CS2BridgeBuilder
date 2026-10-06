using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Runtime;

/// <summary>Includes cached objects rejected before PrefabSystem registered them; never calls Load.</summary>
internal static class BridgeInspectionAssets
{
    internal sealed class Entry
    {
        internal string Owner = "", Path = "", Cid = "";
        internal PrefabBase? Prefab;
    }

    internal static IEnumerable<Entry> Read()
    {
        var root = Path.GetFullPath(BridgeAssetCatalog.Root).TrimEnd(Path.DirectorySeparatorChar);
        foreach (var asset in AssetDatabase.user.GetAssets<PrefabAsset>())
        {
            var path = Path.GetFullPath(BridgeFileAccess.Logical(asset.path));
            var directory = Path.GetDirectoryName(path)!;
            // Exact ImportedData child ownership, not a name match in another package or backup.
            if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase)) continue;
            var stem = Path.GetFileName(directory);
            if (!BridgeSessionState.TryOwner(stem, out var owner)
                || stem == owner + "_Dependencies"
                || !string.Equals(Path.GetFileName(path), stem + ".Prefab", StringComparison.OrdinalIgnoreCase)
                || BridgeStartupRecovery.Retired.Contains(owner)) continue;
            yield return new Entry { Owner = owner, Path = path, Cid = asset.id.guid.ToString(),
                Prefab = asset.GetInstance<PrefabBase>() };
        }
    }
}
