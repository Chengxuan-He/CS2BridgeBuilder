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
        count=writtenFiles=0;error="";
        if(creation.ResolutionError!=null){error=creation.ResolutionError;return false;}
        var timing = new DependencySaveTimings();
        var footprint = new BridgeMaterialFootprint();
        var result = BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath, owner, seeds,
            cid => creation.Resolve(cid, owner, timing), out count, out error, out writtenFiles,
            (cid, source) => { footprint.Observe(cid, source); creation.ObservePath(owner, cid, source); }, exception => Mod.Log.Error(exception), timing);
        if (!result && creation.ResolutionError != null) error = creation.ResolutionError;
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

    internal static bool TryValidateCacheName(string cid,BridgeDependencyCopies.Source source,out string? error)
    {
        if(!BridgeDependencyCopies.TryDestination(".","unused",cid,source,out _,out error))return false;
        if(source.Bytes.Length<8 || source.NativeFileName!=$"TS{BitConverter.ToInt32(source.Bytes,0)}_MB{BitConverter.ToInt32(source.Bytes,4)}.MidMips")
        {error="Native MidMips filename/header mismatch: "+cid;return false;}
        return true;
    }

    internal static bool TryRead(AssetData asset,string owner,out BridgeDependencyCopies.Source? source,out string? error,DependencySaveTimings? timing=null)
    {
        source=null;error=null;
        using var measure=timing?.Measure(Stage.SourceRead);
        try
        {
            if(asset.isBuiltin){source=new BridgeDependencyCopies.Source{Builtin=true,Extension=asset.GetMeta().extension??""};return true;}
            var meta=asset.GetMeta();
            if(string.IsNullOrEmpty(meta.extension)){error="Required dependency has no native file metadata: "+asset.id.guid;return false;}
            var owned=!meta.packaged && !BridgeAssetInfo.IsDependencyPath(meta.path,owner) &&
                (BridgeAssetInfo.MatchesOwner(meta.path,owner)||BridgeAssetInfo.MatchesOwner(meta.subPath,owner));
            using var stream=asset.GetReadStream();
            bool surface=meta.extension.Equals(".Surface",StringComparison.OrdinalIgnoreCase)||meta.extension.Equals(".Material",StringComparison.OrdinalIgnoreCase);
            if(owned && !meta.extension.Equals(".Prefab",StringComparison.OrdinalIgnoreCase) && !surface && !meta.extension.Equals(".MidMips",StringComparison.OrdinalIgnoreCase))
            {source=new BridgeDependencyCopies.Source{Owned=true,Extension=meta.extension,PhysicalPath=meta.path};return true;}
            using var bytes=new MemoryStream();stream.CopyTo(bytes);var payload=bytes.ToArray();
            if(timing!=null)timing.SourceBytesRead+=payload.LongLength;
            string[]? references=null;
            if(surface && !TryReadSurfaceReferences(payload,out references,out error))return false;
            source=new BridgeDependencyCopies.Source{Extension=meta.extension,Owned=owned,PhysicalPath=meta.path,
                NativeFileName=meta.extension.Equals(".MidMips",StringComparison.OrdinalIgnoreCase)?CacheFileName(meta.fileName,meta.extension,meta.path):"",
                Bytes=payload,SerializedReferences=references};return true;
        }
        catch(FileNotFoundException){return true;}
        catch(DirectoryNotFoundException){return true;}
        catch(Exception exception){error=$"Dependency source read failed: owner={owner}; {exception.Message}";source=null;return false;}
    }

    internal static bool TryReadSurfaceReferences(byte[] payload,out string[] references,out string? error)
    {
        string? missing=null;
        bool parsed=BridgeSurfaceReferences.TryRead(payload,(template,property)=>
        {
            var description=AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(template);
            if(description==null){missing="Unknown native surface material template "+template;return false;}
            return description.GetStackConfigIndex(property)!=-1;
        },out references,out error);
        if(missing!=null){references=Array.Empty<string>();error=missing;return false;}
        return parsed;
    }

}

// A single generation owns these handles. Native cache edges can point backwards
// on disk, so their producers declare them in the forward direction here.
internal sealed class BridgeCreationDependencies
{
    private readonly Dictionary<string, AssetData> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string[]> _cacheEdges = new(StringComparer.OrdinalIgnoreCase);
    internal delegate bool NativeCacheLookup(AssetData asset, out IEnumerable<AssetData> caches, out string? error);
    private readonly NativeCacheLookup _nativeCaches;
    internal string? ResolutionError { get; private set; }
    private readonly Func<string, string?>? _preparedCache;
    internal delegate bool LocalCacheLookup(AssetData asset,out string? path,out string? error);
    private readonly LocalCacheLookup? _localCache;
    internal readonly HashSet<string> Files = new(StringComparer.OrdinalIgnoreCase);
    internal void ObservePath(string owner, string cid, BridgeDependencyCopies.Source source)
    {
        if (source.Builtin) return;
        string path=source.PhysicalPath;
        if(!source.Owned && !BridgeDependencyCopies.TryDestination(UnityEngine.Application.persistentDataPath,owner,cid,source,out path,out var cause))
        {ResolutionError=cause;return;}
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
                {ResolutionError="Ambiguous or invalid owner dependency snapshot: "+path;return null;}
            using var measure = timing.Measure(Stage.SourceRead);
            var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path));
            timing.SourceBytesRead += bytes.LongLength;
            string[]? references=null;
            if((extension==".Surface" || extension==".Material") && !BridgeDependencyPersistence.TryReadSurfaceReferences(bytes,out references,out var cause))
            {ResolutionError="Owner dependency snapshot: "+path+"; "+cause;return null;}
            result=new BridgeDependencyCopies.Source{Extension=extension,PhysicalPath=path,Bytes=bytes,SerializedReferences=references};
        }
        return result;
    }

    private BridgeInspectionCaches? _inspectionCaches;
    internal delegate bool IdentityCacheLookup(string cid,out IEnumerable<AssetData> caches,out string? error);
    private IdentityCacheLookup? _identityCaches;
    internal void Inspect(BridgeInspectionCaches caches,IdentityCacheLookup? native=null)
    { _inspectSnapshots=true;_inspectionCaches=caches;_identityCaches=native; }

    private bool Fail(string message){ResolutionError??=message;return false;}
    private static bool ValidCid(string cid)=>System.Text.RegularExpressions.Regex.IsMatch(cid,@"\A[a-fA-F0-9]{32}\z");

    internal static bool RemoveObsoleteRelations(string owner,string backup,out bool changed,out string? error)
    {
        changed=false;error=null;
        try
        {
            string path=RelationsPath(owner);
            if(!BridgeFileAccess.Exists(path))return true;
            string destination=Path.Combine(backup,"ObsoleteCacheRelations",owner,"BridgeCacheEdges.txt");
            Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(destination)!));
            if(BridgeFileAccess.Exists(destination))destination+="."+Guid.NewGuid().ToString("N");
            File.Copy(BridgeFileAccess.Native(path),BridgeFileAccess.Native(destination));
            File.Delete(BridgeFileAccess.Native(path));changed=true;return true;
        }
        catch(Exception exception){error="Obsolete cache metadata cleanup: "+exception.Message;return false;}
    }

    private bool InspectCacheEdges(string cid,string owner,out string[] caches)
    {
        caches=Array.Empty<string>();
        if(_identityCaches!=null && _identityCaches(cid,out var native,out _) )
        {
            caches=native.Select(Record).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(caches.Length!=0)return true;
        }
        if(_inspectionCaches==null)return false;
        if(!_inspectionCaches.TryFind(cid,owner,out var entries,out var error))
        {ResolutionError=error;return false;}
        foreach(var entry in entries)
        {
            if(entry.Asset!=null)Record(entry.Asset);
            if(!string.IsNullOrEmpty(entry.Path))_cachePaths[entry.Cid]=entry.Path;
        }
        caches=entries.Select(x=>x.Cid).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return caches.Length!=0;
    }

    private bool SnapshotHasMidMips(BridgeDependencyCopies.Source source,string owner,DependencySaveTimings timing,out bool required)
    {
        required=false;
        byte[]? Read(string cid,string extension)
        {
            var local=ReadSnapshot(cid,owner,timing);
            if(ResolutionError!=null)return null;
            if(local!=null)
            {
                if(local.Extension!=extension){Fail("VT snapshot dependency kind mismatch: "+cid);return null;}
                return local.Bytes;
            }
            foreach(var db in AssetDatabase.global.databases)
                if(db.TryGetAsset(Colossal.Hash128.Parse(cid),out AssetData asset))
                {
                    if(asset.GetMeta().extension!=extension){Fail("VT native dependency kind mismatch: "+cid);return null;}
                    using var measure=timing.Measure(Stage.SourceRead);
                    using var input=asset.GetReadStream();
                    using var headerReader=new BinaryReader(input);
                    var header=headerReader.ReadBytes(498);timing.SourceBytesRead+=header.Length;return header;
                }
            Fail("VT snapshot dependency unavailable: "+cid);return null;
        }
        using var stream=new MemoryStream(source.Bytes,false);
        using var reader=new BinaryReader(stream);
        int version=reader.ReadUInt16();reader.ReadInt32();
        if(!reader.ReadBoolean())return true;
        int stacks=reader.ReadInt32();
        var textures=new string[8];
        for(int stack=0;stack<2;stack++)
        {
            stream.Position+=72;
            for(int layer=0;layer<4;layer++)textures[stack*4+layer]=BridgeSurfaceReferences.ReadHash(reader);
        }
        string surface="";
        if(version!=1 || reader.ReadByte()!=0)surface=BridgeSurfaceReferences.ReadHash(reader);
        bool Valid(string id)=>id.Length==32 && id.Any(c=>c!='0');
        byte[]? header=null;
        if(Valid(surface))
        {
            header=Read(surface,".VTSurface");if(header==null)return false;
            using var input=new MemoryStream(header,false);
            if(!BridgeMidMipReferences.TryRequiredLevels(input,true,out int levels,out var cause))return Fail(cause!);
            required=levels>0;
        }
        for(int stack=0;stack<stacks;stack++)
        for(int layer=0;layer<4;layer++)
        {
            // Match the native loader: present VTSurface layers override individual VTTexture tails.
            if(header!=null && stack<BitConverter.ToInt32(header,490) && layer<BitConverter.ToInt32(header,50+stack*40)
                && header[106+(stack*4+layer)*48]!=0)continue;
            string cid=textures[stack*4+layer];if(!Valid(cid))continue;
            var bytes=Read(cid,".VTTexture");if(bytes==null)return false;
            using var input=new MemoryStream(bytes,false);
            if(!BridgeMidMipReferences.TryRequiredLevels(input,false,out int levels,out var cause))return Fail(cause!);
            required|=levels>0;
        }
        return true;
    }

    private BridgeDependencyCopies.Source? LocalCache(string cid, string owner, DependencySaveTimings timing)
    {
        if (!_cachePaths.TryGetValue(cid, out var path) || !BridgeFileAccess.Exists(path) || !BridgeFileAccess.Exists(path + ".cid")) return null;
        if (!BridgeFileAccess.ReadText(path + ".cid").Trim().Equals(cid, StringComparison.OrdinalIgnoreCase))
            {ResolutionError="Cache relation CID sidecar mismatch: "+cid;return null;}
        using var measure = timing.Measure(Stage.SourceRead);
        var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path)); timing.SourceBytesRead += bytes.LongLength;
        var source = new BridgeDependencyCopies.Source { Bytes = bytes, Extension = ".MidMips", PhysicalPath = path,
            Owned = !BridgeAssetInfo.IsDependencyPath(path, owner), NativeFileName = Path.GetFileName(path) };
        if(!BridgeDependencyPersistence.TryValidateCacheName(cid,source,out var nameError)){ResolutionError=nameError;return null;}
        if(!ValidateMembership(cid, source))return null;
        return source;
    }

    private bool ValidateMembership(string cid, BridgeDependencyCopies.Source source)
    {
        if(!BridgeMidMipReferences.TryRead(source.Bytes,out var members,out var cause)){ResolutionError=$"Dependency cache membership: CID={cid}; {cause}";return false;}
        var membership = new HashSet<string>(members, StringComparer.OrdinalIgnoreCase);
        _checkedMembership[cid] = membership;
        foreach (string surface in _visitedSurfaces)
            if (_cacheEdges[surface].Contains(cid, StringComparer.OrdinalIgnoreCase) && !membership.Contains(surface))
                {ResolutionError="Recorded cache lacks required Surface membership: " + surface;return false;}
        return true;
    }

    internal BridgeCreationDependencies(Func<AssetData, IEnumerable<AssetData>> nativeCaches, Func<string, string?>? preparedCache = null,
        LocalCacheLookup? localCache = null)
    {
        _nativeCaches = (AssetData asset, out IEnumerable<AssetData> caches, out string? error) =>
        {
            caches = Array.Empty<AssetData>(); error = null;
            try { caches = nativeCaches(asset).ToArray(); return true; }
            catch (Exception exception) { error = exception.ToString(); return false; }
        };
        _preparedCache = preparedCache; _localCache = localCache;
    }
    internal BridgeCreationDependencies(NativeCacheLookup nativeCaches, Func<string, string?>? preparedCache = null,
        LocalCacheLookup? localCache = null)
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

    internal bool Material(AssetData surface, IEnumerable<AssetData> caches)
    {
        try
        {
        string cid = Record(surface);
        string[] edges = caches.Select(Record).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        if (_cacheEdges.TryGetValue(cid, out var previous) && !previous.SequenceEqual(edges))
            return Fail("Conflicting generated Surface cache declarations: " + cid);
        _cacheEdges[cid] = edges;return true;
        }
        catch(Exception exception){return Fail("Material cache declaration: "+exception.Message);}
    }

    internal BridgeDependencyCopies.Source? Resolve(string cid, string owner, DependencySaveTimings timing)
    {
        if(ResolutionError!=null)return null;
        try
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
            if(!BridgeDependencyPersistence.TryRead(asset,owner,out var source,out var sourceError,timing))
            {ResolutionError=sourceError;return null;}
            if (source == null) { inaccessibleHandle = true; continue; }
            return Complete(source, asset);
        }
        var snapshot = ReadSnapshot(cid, owner, timing);
        if(ResolutionError!=null)return null;
        if (snapshot != null) return Complete(snapshot, null);
        var cacheSource = LocalCache(cid, owner, timing);
        if (cacheSource == null && _inspectSnapshots && inaccessibleHandle) Unconfirmed = true;
        return cacheSource;

        BridgeDependencyCopies.Source? Complete(BridgeDependencyCopies.Source source, AssetData? asset)
        {
            if (source.Builtin) return source;
            if (source.Extension.Equals(".MidMips", StringComparison.OrdinalIgnoreCase))
                {
                if(!BridgeDependencyPersistence.TryValidateCacheName(cid,source,out var nameError)){ResolutionError=nameError;return null;}
                if(!ValidateMembership(cid, source))return null;
                string destination=source.PhysicalPath;
                if(!source.Owned && !BridgeDependencyCopies.TryDestination(UnityEngine.Application.persistentDataPath,owner,cid,source,out destination,out var pathError)){ResolutionError=pathError;return null;}
                _cachePaths[cid]=destination;
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
                    if(_inspectSnapshots && source.Bytes[6]!=0)
                    {
                        if(!SnapshotHasMidMips(source,owner,timing,out bool required))return null;
                        if(!required)_cacheEdges[cid]=Array.Empty<string>();
                        else if(InspectCacheEdges(cid,owner,out var indexed))_cacheEdges[cid]=indexed;
                        else
                        {
                            ResolutionError??="Required native cache provenance unavailable: "+cid;return null;
                        }
                    }
                    string? path=null;
                    if(asset!=null && !_cacheEdges.ContainsKey(cid) && _localCache!=null && !_localCache(asset,out path,out var localError))
                    {ResolutionError=$"Historical material cache resolution: owner={owner}; CID={cid}; {localError}";return null;}
                    if (path != null)
                    {
                        string cacheCid = BridgeFileAccess.ReadText(path + ".cid").Trim();
                        if(!ValidCid(cacheCid)){ResolutionError="Invalid historical cache CID.";return null;}
                        _cachePaths[cacheCid] = path;
                        _cacheEdges[cid] = new[] { cacheCid };
                    }
                    else if (asset == null) { if(!_cacheEdges.ContainsKey(cid))_cacheEdges[cid] = Array.Empty<string>(); }
                    else if(!_cacheEdges.ContainsKey(cid))
                    {
                        if (!_nativeCaches(asset, out var resolved, out var cause))
                        {
                            if(_inspectSnapshots && InspectCacheEdges(cid,owner,out var indexed))
                            { _cacheEdges[cid]=indexed;resolved=Array.Empty<AssetData>(); }
                            else
                            {
                                ResolutionError ??= $"Dependency material resolution failed: owner={owner}; CID={cid}; {cause}";
                                Mod.Log.Error(ResolutionError); return null;
                            }
                        }
                        if(!_cacheEdges.ContainsKey(cid) && !Material(asset,resolved))return null;
                    }
                    caches = _cacheEdges[cid];
                }
            }
            _visitedSurfaces.Add(cid);
            foreach (string cache in caches)
                if (_checkedMembership.TryGetValue(cache, out var membership) && !membership.Contains(cid))
                    {ResolutionError="Recorded cache lacks required Surface membership: "+cid;return null;}
            source.SerializedReferences = BridgeDependencyCopies.References(source).Concat(caches)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return source;
        }
        }
        catch(Exception exception){ResolutionError=$"Dependency resolution: owner={owner}; CID={cid}; {exception.Message}";return null;}
    }
}
