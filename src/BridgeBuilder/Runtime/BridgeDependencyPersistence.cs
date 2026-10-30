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
        MidMipIndex? midMips = null;
        var footprint = new BridgeMaterialFootprint();
        var result = BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath, owner, seeds,
            cid => Resolve(cid, owner, ref alternatives, ref midMips), out count, out error, out writtenFiles, footprint.Observe);
        if(result)
        {
            try
            {
                var report=footprint.Write(UnityEngine.Application.persistentDataPath,owner,
                    typeof(BridgeDependencyPersistence).Assembly.ManifestModule.ModuleVersionId.ToString());
                Mod.Log.Info("Saved-owner material footprint: "+report);
            }
            catch(Exception exception){Mod.Log.Warn("Saved-owner material measurement unavailable: "+exception.Message);}
        }
        Mod.Log.Info($"Bridge dependency persistence UUID={owner}: elapsedMs={timer.ElapsedMilliseconds}, copies={count}, writtenFiles={writtenFiles}, success={result}");
        return result;
    }

    internal static BridgeDependencyCopies.Source? Resolve(string cid, string owner, ref ILookup<string, AssetData>? alternatives)
    {
        MidMipIndex? midMips = null;
        return Resolve(cid, owner, ref alternatives, ref midMips);
    }

    internal static BridgeDependencyCopies.Source? Resolve(string cid, string owner,
        ref ILookup<string, AssetData>? alternatives, ref MidMipIndex? midMips)
    {
        // Packaged source bytes take precedence over identical-CID snapshots in
        // other bridges, including stale registrations after those bridges delete.
        if (BridgePreparedMaterials.TryGet(cid, out var prepared))
            return IncludeMidMips(cid, Read(prepared, owner), owner, ref alternatives, ref midMips);
        if (midMips != null && midMips.Sources.TryGetValue(cid, out var cache)) return cache;
        AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out AssetData asset);
        if (asset != null)
        {
            var source = Read(asset, owner);
            if (source != null) return IncludeMidMips(cid, source, owner, ref alternatives, ref midMips);
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
            return IncludeMidMips(cid, source, owner, ref alternatives, ref midMips);
        }
        return null;
    }

    private static BridgeDependencyCopies.Source? IncludeMidMips(string cid, BridgeDependencyCopies.Source? source,
        string owner, ref ILookup<string, AssetData>? alternatives, ref MidMipIndex? midMips)
    {
        if (source == null || source.Builtin ||
            !(source.Extension.Equals(".Surface", StringComparison.OrdinalIgnoreCase)
            || source.Extension.Equals(".Material", StringComparison.OrdinalIgnoreCase))) return source;
        // MidMips records the reverse Surface CID relation, outside Surface's serialized fields.
        // Index permitted native caches once per traversal, not only the installed prepared map.
        if (midMips == null)
        {
            alternatives ??= AssetDatabase.global.GetAssets<AssetData>()
                .ToLookup(a => a.id.guid.ToString(), StringComparer.OrdinalIgnoreCase);
            midMips = new MidMipIndex();
            foreach (var asset in alternatives.SelectMany(group => group))
            {
                if (asset.isBuiltin || !asset.GetMeta().extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase)) continue;
                var candidate = Read(asset, owner); // Reject another owner's path before opening it.
                if (candidate != null) midMips.Add(asset.id.guid.ToString(), candidate);
            }
            // Native registration may not yet include an existing owner snapshot during self-check.
            // Enumerate only the current owner's files; never open another owner's same-CID cache.
            string root = UnityEngine.Application.persistentDataPath;
            string imported = Path.Combine(root, "ImportedData");
            foreach (string path in BridgeFileAccess.EnumerateFiles(imported)
                .Concat(BridgeFileAccess.EnumerateFiles(Path.Combine(root, "BridgeBuilder"))))
            {
                if (!path.EndsWith(".MidMips", StringComparison.OrdinalIgnoreCase)
                    || !BridgeAssetInfo.MatchesOwner(path, owner)
                    || BridgeAssetInfo.IsOtherBridgeAsset(path, owner, imported)) continue;
                if (!BridgeFileAccess.Exists(path + ".cid") && midMips.Sources.Any(entry =>
                    string.Equals(BridgeDependencyCopies.Destination(root, owner, entry.Key, entry.Value),
                        path, StringComparison.OrdinalIgnoreCase))) continue;
                string cacheCid = BridgeFileAccess.ReadText(path + ".cid").Trim();
                using var stream = BridgeFileAccess.OpenRead(path);
                using var bytes = new MemoryStream(); stream.CopyTo(bytes);
                midMips.Add(cacheCid, new BridgeDependencyCopies.Source
                {
                    Owned = !BridgeAssetInfo.IsDependencyPath(path, owner), Extension = ".MidMips",
                    PhysicalPath = path, NativeFileName = Path.GetFileName(path), Bytes = bytes.ToArray()
                });
            }
        }
        source.SerializedReferences = BridgeDependencyCopies.References(source)
            .Concat(BridgePreparedMaterials.MidMipDependencies(cid))
            .Concat(midMips.BySurface.TryGetValue(cid, out var caches) ? caches : Enumerable.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return source;
    }

    internal sealed class MidMipIndex
    {
        internal readonly Dictionary<string, BridgeDependencyCopies.Source> Sources = new(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, HashSet<string>> BySurface = new(StringComparer.OrdinalIgnoreCase);
        internal void Add(string cid, BridgeDependencyCopies.Source source)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(cid, @"\A[a-fA-F0-9]{32}\z"))
                throw new InvalidDataException("Invalid MidMips cache CID.");
            if (Sources.TryGetValue(cid, out var prior))
            {
                if (!prior.Bytes.SequenceEqual(source.Bytes) || prior.NativeFileName != source.NativeFileName)
                    throw new InvalidDataException("Conflicting MidMips cache CID: " + cid);
                return;
            }
            // Also validate the native tag-bearing basename before accepting an implicit edge.
            BridgeDependencyCopies.Destination(".", "unused", cid, source);
            foreach (string surface in BridgeMidMipReferences.Read(source.Bytes))
            {
                if (!BySurface.TryGetValue(surface, out var entries)) BySurface.Add(surface, entries = new(StringComparer.OrdinalIgnoreCase));
                entries.Add(cid);
            }
            Sources.Add(cid, source);
        }
    }

    private static BridgeDependencyCopies.Source? Read(AssetData asset, string owner)
    {
        try { return ReadSource(asset, owner); }
        catch (FileNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
        catch (DirectoryNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
    }

    private static BridgeDependencyCopies.Source? ReadSource(AssetData asset, string owner)
    {
        if (asset.isBuiltin) return new BridgeDependencyCopies.Source { Builtin = true, Extension = asset.GetMeta().extension };
        var meta = asset.GetMeta();
        if (BridgeAssetInfo.IsOtherBridgeAsset(meta.path, owner,
            Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData"))) return null;
        var owned = !meta.packaged && !BridgeAssetInfo.IsDependencyPath(meta.path, owner)
            && (BridgeAssetInfo.MatchesOwner(meta.path, owner) || BridgeAssetInfo.MatchesOwner(meta.subPath, owner));
        using var stream = asset.GetReadStream(); // Still open: missing/inaccessible owned files remain failures.
        var surface = meta.extension.Equals(".Surface", StringComparison.OrdinalIgnoreCase)
            || meta.extension.Equals(".Material", StringComparison.OrdinalIgnoreCase);
        if (owned && !meta.extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
            && !surface && !meta.extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase))
            return new BridgeDependencyCopies.Source { Owned = true, Extension = meta.extension, PhysicalPath = meta.path };
        // Private geometry is a terminal owned asset: its payload never needs copying or parsing.
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var payload = bytes.ToArray();
        return new BridgeDependencyCopies.Source
        {
            Extension = meta.extension,
            Owned = owned,
            PhysicalPath = meta.path,
            NativeFileName = meta.extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(meta.path) : "",
            Bytes = payload,
            SerializedReferences = surface ? BridgeSurfaceReferences.Read(payload, (template, property) =>
                (AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(template)
                    ?? throw new InvalidDataException("Unknown native surface material template " + template))
                    .GetStackConfigIndex(property) != -1) : null,
        };
    }
}
