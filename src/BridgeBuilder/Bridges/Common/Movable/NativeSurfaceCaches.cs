using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;

namespace BridgeBuilder.Bridges;

internal static class NativeSurfaceCaches
{
    // ObjectEntry is the game's indexed physical tile -> cache relation. Only
    // its grouper accessor is internal in the installed game API.
    private static readonly FieldInfo? Grouper = typeof(ObjectEntry).GetField("m_Grouper", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static IEnumerable<MidMipCacheAsset> Find(SurfaceAsset source, TextureStreamingSystem streaming, int levels, string owner = "")
    {
        var found = new List<MidMipCacheAsset>();
        var attempted = new HashSet<MidMipCacheAsset>();
        string tag = AtlasMaterialsGrouper.GetAssetName(streaming.tileSize, levels);
        string cid = source.id.guid.ToString();

        void Check(MidMipCacheAsset cache)
        {
            if (!attempted.Add(cache) || !MovableLaneMidMips.Permitted(cache, owner) || cache.tags.Count != 1 || cache.tags[0] != tag) return;
            using var input = cache.GetReadStream();
            if (BridgeMidMipReferences.Read(input).Contains(cid, StringComparer.OrdinalIgnoreCase)) found.Add(cache);
        }

        var infos = source.PreReservedAtlassingInfos ?? source.VTAtlassingInfos;
        if (infos != null)
        {
            for (int stack = 0; stack < infos.Length; stack++)
            {
                var info = infos[stack];
                if (info.stackGlobalIndex < 0 || info.indexInStack < 0) continue;
                int bias = source.GetMipBiasOverride();
                var size = AtlassingUtils.GetBiasedSize(source.GetUnbiasedStackTextureSize(stack),
                    bias >= 0 ? bias : streaming.mipBias, streaming.tileSize);
                int tiles = Math.Min(size.x, size.y) / streaming.tileSize;
                if (tiles < 1) continue;
                int level = 1;
                while (tiles > 1) { tiles >>= 1; level++; }
                var point = ZOrderer.GetCoords(info.indexInStack);
                int index = PVTTileIndexer.GetUniversalTileIndex(point.x >> level, point.y >> level, level);
                if (streaming.TryGetMidMipMask(info.stackGlobalIndex, index, out var entry)
                    && Grouper?.GetValue(entry) is AtlasMaterialsGrouper group && group.asset != null) Check(group.asset);
            }
        }

        // Before the texture streaming system reserves a tile, an original
        // package may supply its own tagged entry. Game content may instead
        // pair Prefabs.cok with MidMips.cok in the same content directory.
        string? uri = source.uri;
        if (found.Count == 0 && !string.IsNullOrEmpty(uri))
        {
            int at = uri.IndexOf('@');
            string container = at >= 0 ? uri.Substring(0, at) : uri;
            int slash = container.LastIndexOf('/');
            string directory = slash < 0 ? "" : container.Substring(0, slash + 1);
            void Lookup(string exactUri)
            {
                if (source.database.TryGetAsset(exactUri, out MidMipCacheAsset cache)) Check(cache);
            }
            if (at >= 0)
            {
                Lookup(container + "@StreamingData~/" + tag + ".MidMips");
                if (found.Count == 0) Lookup(container + "@" + tag + ".MidMips");
                if (found.Count == 0) Lookup(directory + "MidMips.cok@" + tag + ".MidMips");
            }
            else Lookup(directory + "StreamingData~/" + tag + ".MidMips");
        }
        if (found.Count == 0) throw new InvalidDataException("Missing native MidMips membership for selected Surface " + cid);
        return found;
    }
}
