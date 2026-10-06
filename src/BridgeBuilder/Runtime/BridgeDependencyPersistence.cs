using Colossal.IO.AssetDatabase;
using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Runtime;

internal static class BridgeDependencyPersistence
{
    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error)
        => Save(owner, seeds, out count, out error, out _);

    internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error, out int writtenFiles)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var result = BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath, owner, seeds,
            cid => Resolve(cid, owner), out count, out error, out writtenFiles);
        Mod.Log.Info($"Bridge dependency persistence UUID={owner}: elapsedMs={timer.ElapsedMilliseconds}, copies={count}, writtenFiles={writtenFiles}, success={result}");
        return result;
    }

    private static BridgeDependencyCopies.Source? Resolve(string cid, string owner)
    {
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out AssetData asset)) return null;
        if (asset.isBuiltin) return new BridgeDependencyCopies.Source { Builtin = true };
        var meta = asset.GetMeta();
        var owned = !meta.packaged && !meta.path.Contains(owner + "_Dependencies")
            && (meta.path.Contains(owner) || meta.subPath?.Contains(owner) == true);
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
