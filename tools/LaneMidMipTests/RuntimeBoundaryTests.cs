using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;

// Engine boundary doubles exercise production owner selection and zero-level
// dispatch. Native reservation/load is verified separately by the Unity fixture.
internal static class RuntimeBoundaryTests
{
    internal static void Run(byte[] nativeCache, string sourceCid)
    {
        const string owner = "b00000000-0000-0000-0000-000000000001";
        var db = new Database(); AssetDatabase.global = db; AssetDatabase.user = new Database();
        var source = new SurfaceAsset { id = new(sourceCid), database = db, isBuiltin = true };
        var target = new SurfaceAsset { id = new(new string('b', 32)), database = AssetDatabase.user };
        var texture = new AssetData { id = new(new string('a', 32)), database = db, isBuiltin = true };
        source.preProcessedTextureGuids = new[] { texture.id.guid }; db.Items.Add(texture);
        var forbidden = new MidMipCacheAsset { id = new(new string('c', 32)), database = db,
            Path = Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData", owner, "Appearance", "TS512_MB3.MidMips"),
            Bytes = nativeCache, ForbidRead = true };
        forbidden.tags.Add("TS512_MB3"); db.Items.Add(forbidden);
        var stale = new MidMipCacheAsset { id = new(new string('d', 32)), database = db,
            Path = Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData", "b00000000-0000-0000-0000-000000000002", "Dependencies", "TS512_MB3.MidMips"),
            ForbidRead = true };
        stale.tags.Add("TS512_MB3"); db.Items.Add(stale);
        var assets = new List<AssetData>();
        foreach (var test in new[] { (0, 7), (3, 0) })
        {
            texture.Bytes = Header(test.Item1, test.Item2);
            MovableLaneMidMips.Complete(source, target, owner, null, assets);
            if (assets.Count != 0 || forbidden.ReadCount != 0 || stale.ReadCount != 0)
                throw new Exception("Zero/empty source touched caches.");
        }
        texture.Bytes = Header(3, 7);
        ExpectFailure(() => MovableLaneMidMips.Complete(source, target, owner, null, assets));
        if (forbidden.ReadCount != 0 || stale.ReadCount != 0) throw new Exception("Generated owner was opened.");
        var original = new MidMipCacheAsset { id = new(new string('e', 32)), database = db, isBuiltin = true, Bytes = nativeCache };
        original.tags.Add("TS512_MB3"); db.Items.Add(original);
        MovableLaneMidMips.Complete(source, target, owner + " Moving Road Piece 2", null, assets);
        if (assets.Count != 1 || !assets[0].Path.Contains(owner + "/Appearance/MidMips/" + target.id.guid) ||
            !assets[0].Path.EndsWith("/TS512_MB3") || forbidden.ReadCount != 0 || stale.ReadCount != 0)
            throw new Exception("Owner-local native cache output mismatch.");
        var streaming = Unity.Entities.World.DefaultGameObjectInjectionWorld.Streaming;
        if (streaming.Refreshes != 1 || !ReferenceEquals(streaming.LastDatabase, AssetDatabase.user)) throw new Exception("Permanent refresh mismatch.");
        var preview = new PreviewGeometry(); var previewAssets = new List<AssetData>();
        MovableLaneMidMips.Complete(source, target, "tmp00000000-0000-0000-0000-000000000003 Moving Road Piece 2", preview, previewAssets);
        if (previewAssets.Count != 1 || !ReferenceEquals(previewAssets[0].database, preview.NativeAssets) ||
            !ReferenceEquals(streaming.LastDatabase, preview.NativeAssets)) throw new Exception("Preview database isolation mismatch.");
        var zero = new AssetData { id = new(new string('f', 32)), database = db, isBuiltin = true, Bytes = Header(0, 7) };
        db.Items.Add(zero); source.preProcessedTextureGuids = new[] { texture.id.guid, zero.id.guid };
        int priorReads = original.ReadCount;
        ExpectFailure(() => MovableLaneMidMips.Complete(source, target, owner, null, new()));
        if (original.ReadCount != priorReads) throw new Exception("Mixed zero/3 source reached cache loading.");
        Console.WriteLine("PASS runtime boundary: zero/empty, mixed zero/3, missing original, current/other/stale-owner exclusion before read, permanent and private-preview refresh.");
    }
    static void ExpectFailure(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected native boundary failure."); }
    static byte[] Header(int levels, int tail)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write((ushort)1); writer.Write((byte)0); writer.Write(new byte[8]); writer.Write((ushort)levels);
        writer.Write(new byte[5]); writer.Write(tail); return stream.ToArray();
    }
}

namespace Colossal
{
    public readonly record struct Hash128(string Value) { public bool isValid => Value != null && Value.Any(c => c != '0'); public override string ToString() => Value; }
}
namespace Colossal.IO.AssetDatabase
{
    public readonly record struct Identifier(Colossal.Hash128 guid) { public Identifier(string value) : this(new Colossal.Hash128(value)) { } }
    public enum EscapeStrategy { None }
    public record AssetDataPath(string Directory, string Name) { public static AssetDataPath Create(string d, string n, EscapeStrategy _) => new(d, n); }
    public class AssetData
    {
        public Identifier id; public Database database = null!; public bool isBuiltin, ForbidRead; public string Path = "original";
        public byte[] Bytes = Array.Empty<byte>(); public int ReadCount; public readonly List<string> tags = new();
        public (string path, string subPath) GetMeta() => (Path, Path);
        public Stream GetReadStream() { ReadCount++; if (ForbidRead) throw new Exception("Forbidden source was read."); return new MemoryStream(Bytes, false); }
        public Stream GetWriteStream() => new SavedStream(bytes => Bytes = bytes);
        sealed class SavedStream : MemoryStream { readonly Action<byte[]> save; internal SavedStream(Action<byte[]> save) { this.save = save; } protected override void Dispose(bool disposing) { if (disposing) save(ToArray()); base.Dispose(disposing); } }
    }
    public class MidMipCacheAsset : AssetData { }
    public class SurfaceAsset : AssetData { public bool hasVTSurfaceAsset; public AssetData vtSurfaceAsset = null!; public IEnumerable<Colossal.Hash128> preProcessedTextureGuids = Array.Empty<Colossal.Hash128>(); }
    public class Database
    {
        public readonly List<AssetData> Items = new();
        public IEnumerable<T> GetAssets<T>() where T : AssetData => Items.OfType<T>();
        public bool TryGetAsset(Colossal.Hash128 cid, out AssetData asset) { asset = Items.FirstOrDefault(a => a.id.guid == cid)!; return asset != null; }
        public T AddAsset<T>(AssetDataPath path) where T : AssetData, new() { var asset = new T { database = this, Path = path.Directory + "/" + path.Name }; Items.Add(asset); return asset; }
    }
    public static class AssetDatabase { public static Database global = new(), user = new(); }
}
namespace Colossal.IO.AssetDatabase.VirtualTexturing
{
    public static class AtlasMaterialsGrouper { public static string GetAssetName(int tile, int levels) => $"TS{tile}_MB{levels}"; }
    public class TextureStreamingSystem { public int tileSize = 512, Refreshes; public Database? LastDatabase; public void MarkVTAssetsDirty() { } public void RefreshVT(Database db) { Refreshes++; LastDatabase = db; } }
}
namespace Unity.Entities
{
    public class World
    {
        public static World DefaultGameObjectInjectionWorld = new();
        public readonly Colossal.IO.AssetDatabase.VirtualTexturing.TextureStreamingSystem Streaming = new();
        public T GetExistingSystemManaged<T>() => (T)(object)Streaming;
    }
}
namespace UnityEngine { public static class Application { public static string persistentDataPath = System.IO.Path.GetFullPath("runtime-boundary-fixture"); } }
namespace BridgeBuilder.Bridges { internal sealed class PreviewGeometry { internal Database NativeAssets = new(); } }
namespace BridgeBuilder.Runtime { internal static class BridgeFileAccess { internal static string Logical(string path) => path; } }
namespace BridgeBuilder { internal static class Mod { internal static Logger Log = new(); internal sealed class Logger { internal void Info(string _) { } } } }
