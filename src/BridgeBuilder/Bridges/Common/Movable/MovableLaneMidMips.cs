using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal static class MovableLaneMidMips
{
    internal static bool TryCachesByIdentity(string cid,out IEnumerable<AssetData> caches,out string? error)
    {
        caches=Array.Empty<AssetData>();error=null;
        try
        {
            var streaming=World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<TextureStreamingSystem>();
            if(streaming==null){error="Native texture streaming is not ready.";return false;}
            if(!NativeSurfaceCaches.TryFindIdentity(cid,streaming,out var found,out error))return false;
            caches=found;return true;
        }
        catch(Exception exception){error="Native cache identity lookup: "+exception.Message;return false;}
    }
    internal static bool TryCompletePrepared(SurfaceAsset source, SurfaceAsset target, out string? error)
    {
        error = null;
        try
        {
        // Prepared positive-level surfaces are registered by their native cache.
        // A zero-level lane has no such cache and must not skip initialization.
        if (!target.isVTMaterial) return true;
        if(!TryRequiredLevels(source,"",out var levels,out error))return false;
        if(levels!=0)return true;
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        InitializeZeroMid(target, streaming);

        return true;
        }
        catch (Exception exception) { error = "Native mip completion: " + exception.ToString(); return false; }
    }

    private static void InitializeZeroMid(SurfaceAsset target, TextureStreamingSystem streaming)
    {
        // Repeated prepared uses and LODs share this native reservation. Do not
        // allocate another region or queue duplicate reads for an existing one.
        if (target.VTAtlassingInfos == null && target.PreReservedAtlassingInfos == null)
        {
            target.RegisterToVT(streaming);
            target.LoadVTAsync(streaming, streaming.mipBias, streaming.tileSize, 0);
        }
        // Native last-ref property unload clears VTAtlassingInfos; property
        // reload does not restore it. This native cache API preserves the same
        // reservation for ManagedBatchSystem without retaining property leases.
        if (target.VTAtlassingInfos != null)
            target.PreRegisterToVT(target.VTAtlassingInfos);
    }

    // Descriptor conversion assigns a new Surface CID. Native caches index the old
    // CID implicitly, so retaining its VT header alone cannot retain its mip chain.
    internal static bool TryComplete(SurfaceAsset source, SurfaceAsset target, string owner,
        PreviewGeometry? preview, List<AssetData> assets, out string? error)
    {
        error = null;
        try
        {
        // Lane conversion receives the render graph's qualified name. Keep that
        // Surface naming contract, but place its cache under the actual owner.
        if (preview == null && BridgeAssetInfo.TryFileOwner(owner, out var permanent)) owner = permanent;
        else if (preview != null)
        {
            int at = owner.IndexOf("tmp", StringComparison.Ordinal);
            if (at >= 0 && owner.Length >= at + 39) owner = owner.Substring(at, 39);
        }
        if (!MovableAssetOwnership.Matches(owner, owner, preview))
            { error = "Native lane MidMips require an owned generation identity."; return false; }
        if(!TryRequiredLevels(source,"",out var levels,out error))return false;
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        if (levels == 0)
        {
            // No MidMips cache is needed, but the new Surface CID still needs its
            // own native VT reservation and parameter blocks before publication.
            // The descriptor has already been written by MovableLaneSurface.
            InitializeZeroMid(target, streaming);
            if(BridgeRoadGeneration.CurrentDependencies is {} dependencies && !dependencies.Material(target,Array.Empty<AssetData>())){error=dependencies.ResolutionError;return false;}
            Mod.Log.Info($"Native lane VT initialized: source={source.id}; target={target.id}; levels=0.");
            return true;
        }
        int tile = streaming.tileSize;
        string tag = AtlasMaterialsGrouper.GetAssetName(tile, levels);
        byte[]? payload = null;
        if (!NativeSurfaceCaches.TryFind(source, streaming, levels, out var caches, out error, owner)) return false;
        foreach (var cache in caches)
        {
            // Validate the required native cache tag before opening the stream.
            if (cache.tags.Count != 1 || cache.tags[0] != tag) continue;
            using var input = cache.GetReadStream();
            if (!MovableMidMipSubset.TryCopy(input, source.id.guid.ToString(), target.id.guid.ToString(), tile, levels, out var candidate, out error)) return false;
            if (candidate == null) continue;
            if (payload != null && !payload.SequenceEqual(candidate))
                { error = "Conflicting native lane MidMips sources: " + source.id; return false; }
            payload = candidate;
        }
        if (payload == null) { error = "Missing native lane MidMips membership: " + source.id; return false; }
        var database = preview?.NativeAssets ?? AssetDatabase.user;
        var result = database.AddAsset<MidMipCacheAsset>(AssetDataPath.Create(
            BridgeAssetInfo.OwnedDirectory(owner, "Appearance/MidMips/" + target.id.guid), tag, EscapeStrategy.None));
        assets.Add(result);
        using (var output = result.GetWriteStream()) output.Write(payload, 0, payload.Length);
        if(BridgeRoadGeneration.CurrentDependencies is {} populated && !populated.Material(target,new[]{result})){error=populated.ResolutionError;return false;}
        streaming.MarkVTAssetsDirty();
        streaming.RefreshVT(database);
        Mod.Log.Info($"Native lane MidMips retained: source={source.id}; target={target.id}; levels={levels}; bytes={payload.Length}.");

        return true;
        }
        catch (Exception exception) { error = "Native mip completion: " + exception.ToString(); return false; }
    }

    internal static bool TryRequiredCaches(AssetData asset, out IEnumerable<AssetData> caches, out string? error)
        => TryRequiredCaches(asset, "", out caches, out error);

    internal static bool TryRequiredCaches(AssetData asset, string owner, out IEnumerable<AssetData> caches, out string? error)
    {
        caches = Array.Empty<AssetData>(); error = null;
        try
        {
            if (asset is not SurfaceAsset surface) return true; // Other asset kinds have no Surface-to-MidMips edge.
            surface.LoadHeader();
            if (!surface.isVTMaterial) return true;
            if(!TryRequiredLevels(surface,owner,out var levels,out error))return false;
            if (levels == 0) return true;
            var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
            if (!NativeSurfaceCaches.TryFind(surface, streaming, levels, out var found, out error, owner)) return false;
            caches = found; return true;
        }
        catch (Exception exception) { error = "Native material cache requirements: " + exception.ToString(); return false; }
    }

    internal static bool TryOwnedCachePath(AssetData asset,string owner,out string? pathResult,out string? error)
    {
        pathResult=null;error=null;
        try
        {
        if (asset is not SurfaceAsset surface) return true;
        surface.LoadHeader();
        if (!surface.isVTMaterial) return true;
        if(!TryRequiredLevels(surface,owner,out var levels,out error))return false;
        if (levels == 0) return true;
        var streaming = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<TextureStreamingSystem>();
        if(streaming==null){error="Native VT streaming unavailable for historical cache lookup.";return false;}
        string tag = AtlasMaterialsGrouper.GetAssetName(streaming.tileSize, levels) + ".MidMips";
        string root = UnityEngine.Application.persistentDataPath;
        // Original generated layout recorded the target Surface CID in its folder.
        // These are exact historical paths, not a search through other caches.
        foreach (string folder in new[] { BridgeAssetInfo.OwnedDirectory(owner, "Appearance/MidMips/" + surface.id.guid),
            BridgeAssetInfo.OwnedDirectory(owner, "Appearance/MidMips"), "BridgeBuilder/" + owner + " Road Surface" })
        {
            string path = Path.Combine(root, folder, tag);
            if (!BridgeFileAccess.Exists(path) || !BridgeFileAccess.Exists(path + ".cid")) continue;
            using var input = BridgeFileAccess.OpenRead(path);
            if(!BridgeMidMipReferences.TryRead(input,out var members,out error))return false;
            if(members.Contains(surface.id.guid.ToString(),StringComparer.OrdinalIgnoreCase)){pathResult=path;return true;}
        }
        return true;
        }
        catch(Exception exception){error=$"Historical cache lookup: owner={owner}; {exception}";return false;}
    }

    private static bool TryRequiredLevels(SurfaceAsset source,string owner,out int result,out string? error)
    {
        result=0;error=null;string? failure=null;
        bool Fail(string text){failure=text;return false;}
        try
        {
        byte[]? header = null;
        int? required = null;
        bool Observe(int levels)
        {
            if (levels < 0 || levels > 8 || (required.HasValue && required.Value != levels))
                return Fail("Native lane texture mid-mip levels disagree.");
            required = levels;return true;
        }
        if (source.hasVTSurfaceAsset)
        {
            if(!TryResolve(source,source.vtSurfaceAsset.id.guid,owner,out var backing,out var resolutionError))return Fail(resolutionError!);
            using var input = backing.GetReadStream();
            using var reader = new BinaryReader(input);
            header = reader.ReadBytes(498);
            using var bytes = new MemoryStream(header, false);
            if(!BridgeMidMipReferences.TryRequiredLevels(bytes,true,out _,out var headerError))return Fail(headerError!);
            for (int stack = 0; stack < BitConverter.ToInt32(header, 490); stack++)
            for (int layer = 0; layer < BitConverter.ToInt32(header, 26 + stack * 40 + 24); layer++)
            {
                int at = 106 + (stack * 4 + layer) * 48;
                if (header[at] != 0 && BitConverter.ToInt32(header, at + 16) > 0)
                    if(!Observe(BitConverter.ToInt32(header, at + 20)))return false;
            }
        }
        int index = 0;
        foreach (var cid in source.preProcessedTextureGuids)
        {
            int stack = index / 4, layer = index++ % 4;
            // Native VTSurface layers take precedence over individual VTTexture references.
            if (header != null && stack < BitConverter.ToInt32(header, 490) &&
                layer < BitConverter.ToInt32(header, 26 + stack * 40 + 24) && header[106 + (stack * 4 + layer) * 48] != 0) continue;
            if (!cid.isValid) continue;
            if(!TryResolve(source,cid,owner,out var backing,out var resolutionError))return Fail(resolutionError!);
            using var input=backing.GetReadStream();
            if(!BridgeMidMipReferences.TryRequiredLevels(input,false,out var levels,out var headerError))return Fail(headerError!);
            // RequiredLevels returns zero both for an empty tail and for a real
            // zero-level tail. Only the latter participates in native agreement.
            input.Position -= 4;
            using var reader = new BinaryReader(input, System.Text.Encoding.UTF8, true);
            if(reader.ReadInt32()>0 && !Observe(levels))return false;
        }
        result=required??0;return true;
        }
        catch(Exception exception){return Fail("Native lane mip requirements: "+exception);}
        finally{error=failure;if(error!=null)result=0;}
    }

    private static bool TryResolve(SurfaceAsset source,Colossal.Hash128 cid,string owner,out AssetData result,out string? error)
    {
        result=null!;error=null;
        try
        {
            if(source.database.TryGetAsset(cid,out AssetData asset)){result=asset;return true;}
            if(AssetDatabase.global.TryGetAsset(cid,out asset)){result=asset;return true;}
            foreach(var database in AssetDatabase.global.databases)
                if(database.TryGetAsset(cid,out AssetData candidate)){result=candidate;return true;}
            error="Missing original native lane texture dependency: "+cid;return false;
        }
        catch(Exception exception){error="Native lane dependency lookup: "+cid+"; "+exception;return false;}
    }

}
