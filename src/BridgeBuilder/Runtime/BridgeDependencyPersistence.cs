using Colossal.IO.AssetDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stage = BridgeBuilder.Runtime.DependencySaveTimings.Stage;

namespace BridgeBuilder.Runtime;

internal static class BridgeDependencyPersistence
{
    internal static bool Save(string owner, IEnumerable<string> seeds, BridgeCreationDependencies creation,
        out int count, out string error, out int writtenFiles)
    {
        var timing = new DependencySaveTimings();
        var footprint = new BridgeMaterialFootprint();
        var result = BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath, owner, seeds,
            cid => creation.Resolve(cid, owner, timing), out count, out error, out writtenFiles,
            (cid, source) => { footprint.Observe(cid, source); creation.ObservePath(owner, cid, source); }, exception => Mod.Log.Error(exception), timing);
        if(result)
        {
            try { creation.SaveRelations(owner); }
            catch (Exception exception) { result = false; error = "Cache relation persistence failed: " + exception.Message; }
        }
        if(result)
        {
            using var measure = timing.Measure(Stage.Footprint);
            try
            {
                var report=footprint.Write(UnityEngine.Application.persistentDataPath,owner,
                    typeof(BridgeDependencyPersistence).Assembly.ManifestModule.ModuleVersionId.ToString());
                Mod.Log.Info("Saved-owner material footprint: "+report);
            }
            catch(Exception exception){Mod.Log.Warn("Saved-owner material measurement unavailable: "+exception.Message);}
        }
        string stages = timing.Summary(out long elapsedMs);
        Mod.Log.Info($"Bridge dependency persistence UUID={owner}: elapsedMs={elapsedMs}, copies={count}, writtenFiles={writtenFiles}, success={result}, {stages}");
        return result;
    }

    internal static string CacheFileName(string fileName, string extension, string physicalPath)
        => !string.IsNullOrEmpty(fileName) ? Path.GetFileName(fileName + extension) : Path.GetFileName(physicalPath);

    internal static void ValidateCacheName(string cid, BridgeDependencyCopies.Source source)
    {
        BridgeDependencyCopies.Destination(".", "unused", cid, source);
        if (source.Bytes.Length < 8 || source.NativeFileName !=
            $"TS{BitConverter.ToInt32(source.Bytes, 0)}_MB{BitConverter.ToInt32(source.Bytes, 4)}.MidMips")
            throw new InvalidDataException("Native MidMips filename/header mismatch: " + cid);
    }

    internal static BridgeDependencyCopies.Source? Read(AssetData asset, string owner, DependencySaveTimings? timing = null)
    {
        using var measure = timing?.Measure(Stage.SourceRead);
        try
        {
            var source = ReadSource(asset, owner);
            if (timing != null && source != null) timing.SourceBytesRead += source.Bytes.LongLength;
            return source;
        }
        catch (FileNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
        catch (DirectoryNotFoundException exception) { Mod.Log.Warn($"Bridge dependency source missing: {exception.Message}"); return null; }
        catch (Exception exception)
        {
            throw new IOException($"Dependency source read failed: owner={owner}; CID={asset.id.guid}; assetType={asset.GetType().FullName}; {exception.Message}", exception);
        }
    }

    private static BridgeDependencyCopies.Source? ReadSource(AssetData asset, string owner)
    {
        if (asset.isBuiltin) return new BridgeDependencyCopies.Source { Builtin = true, Extension = asset.GetMeta().extension ?? "" };
        var meta = asset.GetMeta();
        if (string.IsNullOrEmpty(meta.extension))
            throw new InvalidDataException("Required dependency has no native file metadata: " + asset.id.guid);
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
                ? CacheFileName(meta.fileName, meta.extension, meta.path) : "",
            Bytes = payload,
            SerializedReferences = surface ? ReadSurfaceReferences(payload) : null,
        };
    }

    internal static string[] ReadSurfaceReferences(byte[] payload) => BridgeSurfaceReferences.Read(payload, (template, property) =>
                (AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(template)
                    ?? throw new InvalidDataException("Unknown native surface material template " + template))
                    .GetStackConfigIndex(property) != -1);
}

// A single generation owns these handles. Native cache edges can point backwards
// on disk, so their producers declare them in the forward direction here.
internal sealed class BridgeCreationDependencies
{
    private readonly Dictionary<string, AssetData> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string[]> _cacheEdges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<AssetData, IEnumerable<AssetData>> _nativeCaches;
    private readonly Func<string, string?>? _preparedCache;
    private readonly Func<AssetData, string?>? _localCache;
    internal readonly HashSet<string> Files = new(StringComparer.OrdinalIgnoreCase);
    internal void ObservePath(string owner, string cid, BridgeDependencyCopies.Source source)
    {
        if (source.Builtin) return;
        string path = source.Owned ? source.PhysicalPath
            : BridgeDependencyCopies.Destination(UnityEngine.Application.persistentDataPath, owner, cid, source);
        if (string.IsNullOrEmpty(path)) return;
        Files.Add(path); Files.Add(path + ".cid");
    }
    private readonly Dictionary<string, string> _cachePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _visitedSurfaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _checkedMembership = new(StringComparer.OrdinalIgnoreCase);

    private static string RelationsPath(string owner) => Path.Combine(UnityEngine.Application.persistentDataPath,
        "ImportedData", owner, "BridgeCacheEdges.txt");

    private bool _inspectSnapshots;
    internal bool Unconfirmed { get; private set; }

    private BridgeDependencyCopies.Source? ReadSnapshot(string cid, string owner, DependencySaveTimings timing)
    {
        if (!_inspectSnapshots || !ValidCid(cid)) return null;
        string stem = Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath, owner), cid);
        BridgeDependencyCopies.Source? result = null;
        foreach (string extension in new[] { ".Surface", ".Texture", ".Material", ".Prefab", ".Geometry", ".VTTexture", ".VTSurface", ".loc" })
        {
            string path = stem + extension;
            if (!BridgeFileAccess.Exists(path)) continue;
            if (result != null || !BridgeFileAccess.Exists(path + ".cid") ||
                !BridgeFileAccess.ReadText(path + ".cid").Trim().Equals(cid, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Ambiguous or invalid owner dependency snapshot: " + path);
            using var measure = timing.Measure(Stage.SourceRead);
            var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path));
            timing.SourceBytesRead += bytes.LongLength;
            result = new BridgeDependencyCopies.Source { Extension = extension, PhysicalPath = path, Bytes = bytes,
                SerializedReferences = extension == ".Surface" || extension == ".Material"
                    ? BridgeDependencyPersistence.ReadSurfaceReferences(bytes) : null };
        }
        return result;
    }

    internal void ReadRelations(string owner)
    {
        _inspectSnapshots = true;
        string path = RelationsPath(owner);
        if (!BridgeFileAccess.Exists(path)) return;
        var lines = BridgeFileAccess.ReadText(path).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || lines[0] != "BridgeCacheEdges/1") throw new InvalidDataException("Unsupported bridge cache relations.");
        foreach (string line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length < 2 || !ValidCid(fields[1])) throw new InvalidDataException("Invalid bridge cache relation.");
            if (fields[0] == "E")
            {
                var edges = fields.Skip(2).ToArray();
                if (edges.Any(id => !ValidCid(id)) || _cacheEdges.ContainsKey(fields[1]))
                    throw new InvalidDataException("Invalid or duplicate bridge cache edge.");
                _cacheEdges.Add(fields[1], edges);
            }
            else if (fields[0] == "P" && fields.Length == 3)
            {
                string local = LocalPath(owner, fields[2]);
                if (_cachePaths.ContainsKey(fields[1])) throw new InvalidDataException("Duplicate bridge cache path.");
                _cachePaths.Add(fields[1], local);
            }
            else throw new InvalidDataException("Invalid bridge cache relation record.");
        }
    }

    private static bool ValidCid(string cid) => System.Text.RegularExpressions.Regex.IsMatch(cid, @"\A[a-fA-F0-9]{32}\z");
    private static string LocalPath(string owner, string relative)
    {
        string root = Path.GetFullPath(UnityEngine.Application.persistentDataPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (Path.IsPathRooted(relative) || !path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || !BridgeAssetInfo.MatchesOwner(path.Substring(root.Length), owner)
            || BridgeAssetInfo.IsOtherBridgeAsset(path, owner, Path.Combine(root, "ImportedData"))
            || !path.EndsWith(".MidMips", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Cache relation path is outside its bridge owner.");
        return path;
    }

    internal void SaveRelations(string owner)
    {
        string root = Path.GetFullPath(UnityEngine.Application.persistentDataPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        var lines = new List<string> { "BridgeCacheEdges/1" };
        var caches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string surface in _visitedSurfaces.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var edges = _cacheEdges[surface]; caches.UnionWith(edges);
            lines.Add("E\t" + surface + (edges.Length == 0 ? "" : "\t" + string.Join("\t", edges)));
        }
        foreach (string cid in caches.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            if (!_cachePaths.TryGetValue(cid, out var path)) throw new InvalidDataException("Missing persisted cache path: " + cid);
            string relative = Path.GetFullPath(path).Substring(root.Length);
            LocalPath(owner, relative);
            lines.Add("P\t" + cid + "\t" + relative);
        }
        string destination = RelationsPath(owner), text = string.Join("\n", lines) + "\n";
        Files.Add(destination);
        if (BridgeFileAccess.Exists(destination) && BridgeFileAccess.ReadText(destination) == text) return;
        Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(destination)!));
        string pending = destination + ".pending";
        Files.Add(pending);
        File.WriteAllText(BridgeFileAccess.Native(pending), text);
        if (BridgeFileAccess.Exists(destination)) File.Replace(BridgeFileAccess.Native(pending), BridgeFileAccess.Native(destination), null);
        else File.Move(BridgeFileAccess.Native(pending), BridgeFileAccess.Native(destination));
    }

    private BridgeDependencyCopies.Source? LocalCache(string cid, string owner, DependencySaveTimings timing)
    {
        if (!_cachePaths.TryGetValue(cid, out var path) || !BridgeFileAccess.Exists(path) || !BridgeFileAccess.Exists(path + ".cid")) return null;
        if (!BridgeFileAccess.ReadText(path + ".cid").Trim().Equals(cid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Cache relation CID sidecar mismatch: " + cid);
        using var measure = timing.Measure(Stage.SourceRead);
        var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path)); timing.SourceBytesRead += bytes.LongLength;
        var source = new BridgeDependencyCopies.Source { Bytes = bytes, Extension = ".MidMips", PhysicalPath = path,
            Owned = !BridgeAssetInfo.IsDependencyPath(path, owner), NativeFileName = Path.GetFileName(path) };
        BridgeDependencyPersistence.ValidateCacheName(cid, source);
        ValidateMembership(cid, source);
        return source;
    }

    private void ValidateMembership(string cid, BridgeDependencyCopies.Source source)
    {
        var membership = new HashSet<string>(BridgeMidMipReferences.Read(source.Bytes), StringComparer.OrdinalIgnoreCase);
        _checkedMembership[cid] = membership;
        foreach (string surface in _visitedSurfaces)
            if (_cacheEdges[surface].Contains(cid, StringComparer.OrdinalIgnoreCase) && !membership.Contains(surface))
                throw new InvalidDataException("Recorded cache lacks required Surface membership: " + surface);
    }

    internal BridgeCreationDependencies(Func<AssetData, IEnumerable<AssetData>> nativeCaches, Func<string, string?>? preparedCache = null,
        Func<AssetData, string?>? localCache = null)
    { _nativeCaches = nativeCaches; _preparedCache = preparedCache; _localCache = localCache; }

    internal void Relocate(string before, string after)
    {
        foreach (string cid in _cachePaths.Where(p => Path.GetFullPath(BridgeFileAccess.Logical(p.Value))
            .Equals(Path.GetFullPath(BridgeFileAccess.Logical(before)), StringComparison.OrdinalIgnoreCase)).Select(p => p.Key).ToArray())
            _cachePaths[cid] = after;
    }

    internal string Record(AssetData asset)
    {
        string cid = asset.id.guid.ToString();
        _sources[cid] = asset;
        return cid;
    }

    internal void Material(AssetData surface, IEnumerable<AssetData> caches)
    {
        string cid = Record(surface);
        string[] edges = caches.Select(Record).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        if (_cacheEdges.TryGetValue(cid, out var previous) && !previous.SequenceEqual(edges))
            throw new InvalidDataException("Conflicting generated Surface cache declarations: " + cid);
        _cacheEdges[cid] = edges;
    }

    internal BridgeDependencyCopies.Source? Resolve(string cid, string owner, DependencySaveTimings timing)
    {
        Unconfirmed = false;
        bool inaccessibleHandle = false;
        var attempted = new List<AssetData>();
        IEnumerable<AssetData> Candidates()
        {
            if (_sources.TryGetValue(cid, out var recorded)) yield return recorded;
            if (BridgePreparedMaterials.TryGet(cid, out var prepared)) yield return prepared;
            var hash = Colossal.Hash128.Parse(cid);
            foreach (var database in AssetDatabase.global.databases)
                if (database.TryGetAsset(hash, out AssetData candidate)) yield return candidate;
        }
        foreach (var asset in Candidates())
        {
            if (attempted.Any(previous => ReferenceEquals(previous, asset))) continue;
            attempted.Add(asset);
            var source = BridgeDependencyPersistence.Read(asset, owner, timing);
            if (source == null) { inaccessibleHandle = true; continue; }
            return Complete(source, asset);
        }
        var snapshot = ReadSnapshot(cid, owner, timing);
        if (snapshot != null) return Complete(snapshot, null);
        var cacheSource = LocalCache(cid, owner, timing);
        if (cacheSource == null && _inspectSnapshots && inaccessibleHandle) Unconfirmed = true;
        return cacheSource;

        BridgeDependencyCopies.Source Complete(BridgeDependencyCopies.Source source, AssetData? asset)
        {
            if (source.Builtin) return source;
            if (source.Extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase))
                {
                BridgeDependencyPersistence.ValidateCacheName(cid, source);
                ValidateMembership(cid, source);
                _cachePaths[cid] = source.Owned ? source.PhysicalPath
                    : BridgeDependencyCopies.Destination(UnityEngine.Application.persistentDataPath, owner, cid, source);
            }
            if (!source.Extension.Equals(".Surface", StringComparison.OrdinalIgnoreCase) &&
                !source.Extension.Equals(".Material", StringComparison.OrdinalIgnoreCase)) return source;

            if (!_cacheEdges.TryGetValue(cid, out var caches))
            {
                if (BridgePreparedMaterials.TryMidMipDependencies(cid, out var preparedCaches))
                    caches = _cacheEdges[cid] = preparedCaches;
                else if (_preparedCache?.Invoke(cid) is string knownCache)
                {
                    caches = _cacheEdges[cid] = new[] { knownCache };
                    _cachePaths[knownCache] = Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath, owner),
                        knownCache, "TS512_MB3.MidMips");
                }
                else
                {
                    if (asset == null && source.Bytes[6] != 0)
                    {
                        Unconfirmed = true;
                        throw new InvalidDataException("Virtual snapshot cache relations unavailable: " + cid);
                    }
                    string? path = asset == null ? null : _localCache?.Invoke(asset);
                    if (path != null)
                    {
                        string cacheCid = BridgeFileAccess.ReadText(path + ".cid").Trim();
                        if (!ValidCid(cacheCid)) throw new InvalidDataException("Invalid historical cache CID.");
                        _cachePaths[cacheCid] = path;
                        _cacheEdges[cid] = new[] { cacheCid };
                    }
                    else if (asset == null) _cacheEdges[cid] = Array.Empty<string>();
                    else Material(asset, _nativeCaches(asset));
                    caches = _cacheEdges[cid];
                }
            }
            _visitedSurfaces.Add(cid);
            foreach (string cache in caches)
                if (_checkedMembership.TryGetValue(cache, out var membership) && !membership.Contains(cid))
                    throw new InvalidDataException("Recorded cache lacks required Surface membership: " + cid);
            source.SerializedReferences = BridgeDependencyCopies.References(source).Concat(caches)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return source;
        }
    }
}
