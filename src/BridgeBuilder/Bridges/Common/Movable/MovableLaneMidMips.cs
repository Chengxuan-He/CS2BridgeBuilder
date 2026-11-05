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
    internal static void CompletePrepared(SurfaceAsset source, SurfaceAsset target)
    {
        // Prepared positive-level surfaces are registered by their native cache.
        // A zero-level lane has no such cache and must not skip initialization.
        if (!target.isVTMaterial || RequiredLevels(source) != 0) return;
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        InitializeZeroMid(target, streaming);
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
    internal static void Complete(SurfaceAsset source, SurfaceAsset target, string owner,
        PreviewGeometry? preview, List<AssetData> assets)
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
            throw new ArgumentException("Native lane MidMips require an owned generation identity.");
        if (!Permitted(source)) throw new InvalidDataException("Native lane source belongs to a generated bridge.");
        int levels = RequiredLevels(source);
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        if (levels == 0)
        {
            // No MidMips cache is needed, but the new Surface CID still needs its
            // own native VT reservation and parameter blocks before publication.
            // The descriptor has already been written by MovableLaneSurface.
            InitializeZeroMid(target, streaming);
            BridgeRoadGeneration.CurrentDependencies?.Material(target, Array.Empty<AssetData>());
            Mod.Log.Info($"Native lane VT initialized: source={source.id}; target={target.id}; levels=0.");
            return;
        }
        int tile = streaming.tileSize;
        string tag = AtlasMaterialsGrouper.GetAssetName(tile, levels);
        byte[]? payload = null;
        foreach (var cache in NativeSurfaceCaches.Find(source, streaming, levels))
        {
            // Ownership is checked before any stream is opened, including stale registrations.
            if (!Permitted(cache) || cache.tags.Count != 1 || cache.tags[0] != tag) continue;
            using var input = cache.GetReadStream();
            var candidate = MovableMidMipSubset.Copy(input, source.id.guid.ToString(), target.id.guid.ToString(), tile, levels);
            if (candidate == null) continue;
            if (payload != null && !payload.SequenceEqual(candidate))
                throw new InvalidDataException("Conflicting native lane MidMips sources: " + source.id);
            payload = candidate;
        }
        if (payload == null) throw new InvalidDataException("Missing native lane MidMips membership: " + source.id);
        var database = preview?.NativeAssets ?? AssetDatabase.user;
        var result = database.AddAsset<MidMipCacheAsset>(AssetDataPath.Create(
            BridgeAssetInfo.OwnedDirectory(owner, "Appearance/MidMips/" + target.id.guid), tag, EscapeStrategy.None));
        assets.Add(result);
        using (var output = result.GetWriteStream()) output.Write(payload, 0, payload.Length);
        BridgeRoadGeneration.CurrentDependencies?.Material(target, new[] { result });
        streaming.MarkVTAssetsDirty();
        streaming.RefreshVT(database);
        Mod.Log.Info($"Native lane MidMips retained: source={source.id}; target={target.id}; levels={levels}; bytes={payload.Length}.");
    }

    internal static IEnumerable<AssetData> RequiredCaches(AssetData asset) => RequiredCaches(asset, "");

    internal static IEnumerable<AssetData> RequiredCaches(AssetData asset, string owner)
    {
        if (asset is not SurfaceAsset surface) throw new InvalidDataException("Unsupported native material asset: " + asset.id);
        surface.LoadHeader();
        if (!surface.isVTMaterial) return Array.Empty<AssetData>();
        int levels = RequiredLevels(surface, owner);
        if (levels == 0) return Array.Empty<AssetData>();
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        return NativeSurfaceCaches.Find(surface, streaming, levels, owner);
    }

    internal static string? OwnedCachePath(AssetData asset, string owner)
    {
        if (asset is not SurfaceAsset surface) return null;
        surface.LoadHeader();
        if (!surface.isVTMaterial) return null;
        int levels = RequiredLevels(surface, owner);
        if (levels == 0) return null;
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
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
            if (BridgeMidMipReferences.Read(input).Contains(surface.id.guid.ToString(), StringComparer.OrdinalIgnoreCase)) return path;
        }
        return null;
    }

    private static int RequiredLevels(SurfaceAsset source, string owner = "")
    {
        byte[]? header = null;
        int? required = null;
        void Observe(int levels)
        {
            if (levels < 0 || levels > 8 || (required.HasValue && required.Value != levels))
                throw new InvalidDataException("Native lane texture mid-mip levels disagree.");
            required = levels;
        }
        if (source.hasVTSurfaceAsset)
        {
            var backing = Resolve(source, source.vtSurfaceAsset.id.guid, owner);
            using var input = backing.GetReadStream();
            using var reader = new BinaryReader(input);
            header = reader.ReadBytes(498);
            using var bytes = new MemoryStream(header, false);
            BridgeMidMipReferences.RequiredLevels(bytes, true); // Validate the fixed native header first.
            for (int stack = 0; stack < BitConverter.ToInt32(header, 490); stack++)
            for (int layer = 0; layer < BitConverter.ToInt32(header, 26 + stack * 40 + 24); layer++)
            {
                int at = 106 + (stack * 4 + layer) * 48;
                if (header[at] != 0 && BitConverter.ToInt32(header, at + 16) > 0)
                    Observe(BitConverter.ToInt32(header, at + 20));
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
            using var input = Resolve(source, cid, owner).GetReadStream();
            int levels = BridgeMidMipReferences.RequiredLevels(input, false);
            // RequiredLevels returns zero both for an empty tail and for a real
            // zero-level tail. Only the latter participates in native agreement.
            input.Position -= 4;
            using var reader = new BinaryReader(input, System.Text.Encoding.UTF8, true);
            if (reader.ReadInt32() > 0) Observe(levels);
        }
        return required ?? 0;
    }

    private static AssetData Resolve(SurfaceAsset source, Colossal.Hash128 cid, string owner)
    {
        if (source.database.TryGetAsset(cid, out AssetData asset) && Permitted(asset, owner)) return asset;
        if (AssetDatabase.global.TryGetAsset(cid, out asset) && Permitted(asset, owner)) return asset;
        foreach (var database in AssetDatabase.global.databases)
            if (database.TryGetAsset(cid, out AssetData candidate) && Permitted(candidate, owner)) return candidate;
        throw new InvalidDataException("Missing original native lane texture dependency: " + cid);
    }

    internal static bool Permitted(AssetData asset, string owner = "")
    {
        if (asset.isBuiltin) return true;
        var meta = asset.GetMeta();
        // An empty owner deliberately excludes every generated bridge UUID. Only
        // original game/mod resources may feed this conversion, never saved results.
        return !BridgeAssetInfo.IsOtherBridgeAsset(meta.path, owner,
            Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData"));
    }
}
