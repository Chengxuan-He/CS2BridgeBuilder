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
        internal PrefabAsset Asset = null!;
    }

    internal static IEnumerable<Entry> Read(Func<string, bool>? includeOwner = null)
    {
        var entries = new List<Entry>();

        try
        {
            var root = Path.GetFullPath(BridgeAssetCatalog.Root).TrimEnd(Path.DirectorySeparatorChar);
            foreach (var asset in AssetDatabase.user.GetAssets<PrefabAsset>())
            {
                string owner = "";
                Entry? entry = null;
                try
                {
                    var meta = asset.GetMeta();
                    var nativePath = AssetDataPath.Create(meta.subPath, meta.fileName, EscapeStrategy.None);
                    var path = Path.GetFullPath(Path.Combine(asset.database.rootPath,
                        nativePath.ToPath(new FileSystemDataSource.PathEscapePolicy()) + meta.extension));
                    if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!BridgeAssetInfo.TryFileOwner(path.Substring(root.Length + 1), out owner)
                        || BridgeStartupRecovery.Retired.Contains(owner) || (includeOwner != null && !includeOwner(owner))) continue;
                    entry = new Entry { Owner = owner, Path = path, Cid = asset.id.guid.ToString(), Asset = asset };
                    entries.Add(entry);
                    entry.Prefab = asset.GetInstance<PrefabBase>();
                }
                catch (Exception error) when (error is not UnauthorizedAccessException)
                { BridgeInspectionLog.Error("Inspection asset unresolved; confirmed owner will enter repair: " + owner + ": " + error); }
            }
        }
        catch (Exception error) when (error is not UnauthorizedAccessException)
        { BridgeInspectionLog.Error("Inspection discovery unavailable; known entries retained: " + error); }
        return entries;
    }
}
