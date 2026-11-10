using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.IO.AssetDatabase;
using BridgeBuilder.Bridges;

namespace BridgeBuilder.Runtime;

// One inspection owns one narrow path inventory. No CID/content index is constructed.
internal sealed class BridgeInspectionScope
{
    private readonly string _root;
    private readonly BridgeInspectionCaches _caches;
    private readonly Dictionary<string, HashSet<string>> _paths;
    internal BridgeInspectionScope(string root, IEnumerable<string> owners)
    {
        _root = root;
        _paths = owners.ToDictionary(o => o, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.Ordinal);
        _caches=new BridgeInspectionCaches(()=>AssetDatabase.global.GetAssets(default(SearchFilter<MidMipCacheAsset>)),()=>_paths.Values.SelectMany(x=>x));
        foreach (string category in new[] { "ImportedData", "BridgeBuilder", "StreamingData~/VT" })
        {
            string folder = Path.Combine(root, category);
            if (!BridgeFileAccess.Exists(folder)) continue;
            // Historical owner files/directories live immediately below these native roots.
            foreach (string native in Directory.GetFileSystemEntries(BridgeFileAccess.Native(folder)))
            {
                string path = BridgeFileAccess.Logical(native);
                if (!BridgeAssetInfo.TryFileOwner(Path.GetFileName(path), out var owner) || !_paths.TryGetValue(owner, out var files)) continue;
                if (Directory.Exists(native)) files.UnionWith(BridgeFileAccess.EnumerateFiles(path));
                else files.Add(path);
            }
        }
    }

    internal bool PrepareCaches(out string? error) => _caches.Prepare(out error);

    internal int CacheTraversals => _caches.Traversals;
    internal int CacheHeaders => _caches.Headers;

    internal HashSet<string> Paths(string owner) => _paths[owner];

    internal string? Find(string cid, string owner)
    {
        foreach (var database in AssetDatabase.global.databases)
        {
            if (!database.TryGetAsset(Colossal.Hash128.Parse(cid), out AssetData asset) || asset.isBuiltin) continue;
            var meta = asset.GetMeta();
            if (meta.packaged || string.IsNullOrEmpty(meta.path)) continue;
            string path = BridgeFileAccess.Logical(meta.path);
            if (!BridgeFileAccess.Exists(path)) continue;
            if (BridgeAssetInfo.MatchesOwner(path, owner)) _paths[owner].Add(path);
            return path;
        }
        return null;
    }

    internal BridgeCreationDependencies Dependencies(string owner, IEnumerable<BridgeInspectionAssets.Entry> assets)
    {
        var result = new BridgeCreationDependencies((AssetData asset, out IEnumerable<AssetData> caches, out string? error) => MovableLaneMidMips.TryRequiredCaches(asset, owner, out caches, out error),
            BridgePreparedMaterialFiles.CacheFor, (AssetData asset,out string? path,out string? error) => MovableLaneMidMips.TryOwnedCachePath(asset, owner,out path,out error));
        result.Inspect(_caches,MovableLaneMidMips.TryCachesByIdentity);
        foreach (var entry in assets) result.Record(entry.Asset);
        return result;
    }
}
