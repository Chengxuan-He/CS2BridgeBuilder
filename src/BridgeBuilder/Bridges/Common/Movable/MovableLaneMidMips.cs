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
        if (levels == 0) return; // Empty/zero-level native sources do not require a cache.
        var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
        int tile = streaming.tileSize;
        string tag = AtlasMaterialsGrouper.GetAssetName(tile, levels);
        byte[]? payload = null;
        foreach (var cache in AssetDatabase.global.GetAssets<MidMipCacheAsset>())
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
        streaming.MarkVTAssetsDirty();
        streaming.RefreshVT(database);
        Mod.Log.Info($"Native lane MidMips retained: source={source.id}; target={target.id}; levels={levels}; bytes={payload.Length}.");
    }

    private static int RequiredLevels(SurfaceAsset source)
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
            var backing = Resolve(source, source.vtSurfaceAsset.id.guid);
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
            using var input = Resolve(source, cid).GetReadStream();
            int levels = BridgeMidMipReferences.RequiredLevels(input, false);
            // RequiredLevels returns zero both for an empty tail and for a real
            // zero-level tail. Only the latter participates in native agreement.
            input.Position -= 4;
            using var reader = new BinaryReader(input, System.Text.Encoding.UTF8, true);
            if (reader.ReadInt32() > 0) Observe(levels);
        }
        return required ?? 0;
    }

    private static AssetData Resolve(SurfaceAsset source, Colossal.Hash128 cid)
    {
        if (source.database.TryGetAsset(cid, out AssetData asset) && Permitted(asset)) return asset;
        if (AssetDatabase.global.TryGetAsset(cid, out asset) && Permitted(asset)) return asset;
        foreach (var candidate in AssetDatabase.global.GetAssets<AssetData>())
            if (candidate.id.guid == cid && Permitted(candidate)) return candidate;
        throw new InvalidDataException("Missing original native lane texture dependency: " + cid);
    }

    private static bool Permitted(AssetData asset)
    {
        if (asset.isBuiltin) return true;
        var meta = asset.GetMeta();
        // An empty owner deliberately excludes every generated bridge UUID. Only
        // original game/mod resources may feed this conversion, never saved results.
        return !BridgeAssetInfo.IsOtherBridgeAsset(meta.path, "",
            Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData"));
    }
}
