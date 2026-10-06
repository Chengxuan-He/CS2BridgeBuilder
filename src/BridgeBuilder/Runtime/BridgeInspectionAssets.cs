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
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            var relative = path.Substring(root.Length + 1);
            if (!BridgeAssetInfo.TryFileOwner(relative, out var owner)
                || BridgeStartupRecovery.Retired.Contains(owner)) continue;
            yield return new Entry { Owner = owner, Path = path, Cid = asset.id.guid.ToString(),
                Prefab = asset.GetInstance<PrefabBase>() };
        }
    }
}
