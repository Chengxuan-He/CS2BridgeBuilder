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

    internal static bool TryFindIdentity(string cid, TextureStreamingSystem streaming,
        out IEnumerable<MidMipCacheAsset> caches, out string? error)
    {
        caches=Array.Empty<MidMipCacheAsset>();error=null;
        try
        {
            var database=typeof(TextureStreamingSystem).GetField("m_AtlasMaterialsDatabase",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(streaming);
            var registrations=database?.GetType().GetField("m_RegisteredTextures",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(database)
                as Dictionary<Colossal.Hash128,VTAtlassingInfo>[];
            if(registrations==null || Grouper==null){error="Native cache CID registrations are not ready.";return false;}
            var found=new HashSet<MidMipCacheAsset>();
            var attempted=new HashSet<MidMipCacheAsset>();
            foreach(var stack in registrations)
            {
                if(stack==null || !stack.TryGetValue(Colossal.Hash128.Parse(cid),out var info))continue;
                if(info.stackGlobalIndex<0 || info.indexInStack<0)continue;
                var point=ZOrderer.GetCoords(info.indexInStack);
                // Probe only the bounded ancestors of this exact registered tile.
                // Cache header membership disambiguates neighbouring atlas regions.
                for(int level=1;level<12;level++)
                {
                    int index=PVTTileIndexer.GetUniversalTileIndex(point.x>>level,point.y>>level,level);
                    if(!streaming.TryGetMidMipMask(info.stackGlobalIndex,index,out var entry)
                        || Grouper.GetValue(entry) is not AtlasMaterialsGrouper group || group.asset==null
                        || !attempted.Add(group.asset))continue;
                    using var input=group.asset.GetReadStream();
                    if(!BridgeMidMipReferences.TryRead(input,out var members,out error))return false;
                    if(members.Contains(cid,StringComparer.OrdinalIgnoreCase))found.Add(group.asset);
                }
            }
            caches=found.ToArray();return true;
        }
        catch(Exception exception){error="Native cache CID lookup: "+exception.Message;return false;}
    }

    internal static bool TryFind(SurfaceAsset source, TextureStreamingSystem streaming, int levels,
        out IEnumerable<MidMipCacheAsset> caches, out string? error, string owner = "")
    {
        caches = Array.Empty<MidMipCacheAsset>(); error = null;
        try
        {
        var found = new List<MidMipCacheAsset>();
        var attempted = new HashSet<MidMipCacheAsset>();
        string tag = AtlasMaterialsGrouper.GetAssetName(streaming.tileSize, levels);
        string cid = source.id.guid.ToString();

        string? parseError=null;
        void Check(MidMipCacheAsset cache)
        {
            if (!attempted.Add(cache) || cache.tags.Count != 1 || cache.tags[0] != tag) return;
            using var input = cache.GetReadStream();
            if(!BridgeMidMipReferences.TryRead(input,out var members,out var cause)){parseError??=cause;return;}
            if(members.Contains(cid,StringComparer.OrdinalIgnoreCase))found.Add(cache);
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
        if(parseError!=null){error=parseError;return false;}
        if (found.Count == 0) { error = "Missing native MidMips membership for selected Surface " + cid; return false; }
        caches = found; return true;
        }
        catch (Exception exception) { error = exception.GetType().Name + ": " + exception.Message; return false; }
        finally { if (error != null) error = $"Native cache lookup: owner={owner}; source={source?.name}; levels={levels}; {error}"; }
    }
}
