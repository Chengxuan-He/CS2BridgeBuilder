using Colossal.IO.AssetDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

internal static class BridgeDependencyPersistence
{
    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error)
        => Save(owner, seeds, out count, out error, out _);

    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error, out int writtenFiles)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        ILookup<string, AssetData>? alternatives = null;
        var result = BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath, owner, seeds,
            cid => Resolve(cid, owner, ref alternatives), out count, out error, out writtenFiles);
        Mod.Log.Info($"Bridge dependency persistence UUID={owner}: elapsedMs={timer.ElapsedMilliseconds}, copies={count}, writtenFiles={writtenFiles}, success={result}");
        return result;
    }

    internal static BridgeDependencyCopies.Source? Resolve(string cid, string owner, ref ILookup<string, AssetData>? alternatives)
    {
        AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out AssetData asset);
        if (asset != null)
        {
            var source = Read(asset, owner);
            if (source != null) return source;
        }
        // Deleted bridge snapshots can remain the first global CID match. Enumerate all
        // registered sources only on a miss, once per save; never force-load a prefab.
        alternatives ??= AssetDatabase.global.GetAssets<AssetData>()
            .ToLookup(a => a.id.guid.ToString(), StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in alternatives[cid])
        {
            if (ReferenceEquals(candidate, asset)) continue;
            var source = Read(candidate, owner);
            if (source == null) continue;
            Mod.Log.Info($"Bridge dependency alternate source CID={cid}; owner={owner}");
            return source;
        }
        return null;
    }

    private static BridgeDependencyCopies.Source? Read(AssetData asset, string owner)
    {
        try { return ReadSource(asset, owner); }
        catch (FileNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
        catch (DirectoryNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
    }

    private static BridgeDependencyCopies.Source? ReadSource(AssetData asset, string owner)
    {
        if (asset.isBuiltin) return new BridgeDependencyCopies.Source { Builtin = true };
        var meta = asset.GetMeta();
        if (BridgeAssetInfo.IsOtherBridgePrefab(meta.path, owner,
            Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData"))) return null;
        var owned = !meta.packaged && !meta.path.Contains(owner + "_Dependencies")
            && (BridgeAssetInfo.MatchesOwner(meta.path, owner) || BridgeAssetInfo.MatchesOwner(meta.subPath, owner));
        using var stream = asset.GetReadStream(); // Still open: missing/inaccessible owned files remain failures.
        if (owned && !meta.extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
            && !meta.extension.Equals(".Material", StringComparison.OrdinalIgnoreCase))
            return new BridgeDependencyCopies.Source { Owned = true, Extension = meta.extension };
        // Private geometry is a terminal owned asset: its payload never needs copying or parsing.
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return new BridgeDependencyCopies.Source
        {
            Extension = meta.extension,
            Owned = owned,
            Bytes = bytes.ToArray(),
        };
    }
}
