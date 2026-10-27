// Native API doubles exercise production routing and ownership. The Unity native
// reader probe independently checks actual Surface/VT file deserialization.
namespace Colossal.IO.AssetDatabase
{
    internal class AssetData { }
    internal sealed class TextureAsset { internal string id = ""; }
    internal sealed class SurfaceAsset : AssetData
    {
        internal string id = "", name = "";
        internal byte[] Bytes = Array.Empty<byte>();
        internal int materialTemplateHash, Leases, Reads, Saves;
        internal Dictionary<string, float> floats = new();
        internal Dictionary<string, int> ints = new();
        internal Dictionary<string, string> vectors = new(), colors = new();
        internal Dictionary<string, TextureAsset?> textures = new();
        internal HashSet<string> keywords = new();
        internal void LoadHeader() => materialTemplateHash = BitConverter.ToInt32(Bytes, 2);
        internal Stream GetReadStream() { Reads++; return new MemoryStream(Bytes, false); }
        internal Stream GetWriteStream() => new Written(b => { Bytes = b; Parse(); });
        internal void LoadProperties(bool useVT) { Leases++; }
        internal void UnloadProperties() => Leases--;
        internal void UpdateFloats(Dictionary<string, float> updates) { foreach (var p in updates) floats[p.Key] = p.Value; }
        internal void UpdateKeywords(HashSet<string> updates) => keywords = updates;
        internal void SaveData(bool force) => Saves++;
        internal static SurfaceAsset Read(byte[] bytes) { var asset = new SurfaceAsset { Bytes = bytes }; asset.Parse(); return asset; }
        internal void Parse()
        {
            using var r = new BinaryReader(new MemoryStream(Bytes));
            if (r.ReadUInt16() != 1) throw new Exception("Version"); materialTemplateHash = r.ReadInt32();
            if (r.ReadBoolean()) { r.BaseStream.Position += 276; if (r.ReadByte() == 255) r.BaseStream.Position += 16; }
            int Count() => r.ReadByte() == 255 ? r.ReadInt32() : 0;
            for (int n = Count(); n > 0; n--) floats.Add(r.ReadString(), r.ReadSingle());
            for (int n = Count(); n > 0; n--) ints.Add(r.ReadString(), r.ReadInt32());
            foreach (var map in new[] { vectors, colors })
                for (int n = Count(); n > 0; n--) map.Add(r.ReadString(), Convert.ToHexString(r.ReadBytes(16)));
            for (int n = Count(); n > 0; n--) textures.Add(r.ReadString(), r.ReadByte() == 255 ? new TextureAsset { id = Convert.ToHexString(r.ReadBytes(16)) } : null);
            for (int n = Count(); n > 0; n--) keywords.Add(r.ReadString());
            if (r.BaseStream.Position != Bytes.Length) throw new Exception("Unconsumed descriptor");
        }
        private sealed class Written(Action<byte[]> done) : MemoryStream
        {
            private bool completed;
            protected override void Dispose(bool disposing)
            { if (disposing && !completed) { completed = true; done(ToArray()); } base.Dispose(disposing); }
        }
    }
    internal sealed class Database
    {
        internal static int Created;
        internal T AddAsset<T>(string path) where T : new() { Created++; return new T(); }
    }
    internal static class AssetDatabase { internal static Database user = new(); internal static Global global = new(); }
    internal sealed class Global { internal Resources resources = new(); }
    internal sealed class Resources { internal Library materialLibrary = new(); }
    internal sealed class Stack { internal string[] m_PropertyNames = Array.Empty<string>(); }
    internal sealed class Description { internal Stack[] m_Stacks = Array.Empty<Stack>(); }
    internal sealed class Library
    {
        internal Dictionary<int, Description> Descriptions = new(); internal Dictionary<int, Template> Templates = new();
        internal Description GetMaterialDescription(int id) => Descriptions[id];
        internal Template GetMaterial(int id) => Templates[id];
    }
    internal sealed class Template
    {
        internal Dictionary<string, float> Floats = new();
        internal bool HasProperty(string key) => Floats.ContainsKey(key);
        internal float GetFloat(string key) => Floats[key];
    }
}
namespace BridgeBuilder.Runtime
{
    using Colossal.IO.AssetDatabase;
    internal static class BridgePreparedMaterials
    {
        internal static Func<string, byte[], SurfaceAsset?> Lookup = null!;
        internal static SurfaceAsset? Lane(string style, byte[] bytes) => Lookup(style, bytes);
    }
    internal static class BridgePrefabPersistence { internal static string ResourcePath(string name, string category) => category + "/" + name; }
}
namespace BridgeBuilder.Bridges
{
    using Colossal.IO.AssetDatabase;
    internal sealed class PreviewGeometry { internal Database NativeAssets = new(); internal void RetainSurfaceProperties(SurfaceAsset s) { } }
    internal static class MovableSurfaceAssets { internal static bool IsObjectSurface(SurfaceAsset s) => s.materialTemplateHash is 0 or 1 or 35; }
    internal static class MovableCableSurface
    {
        internal static SurfaceAsset Create(SurfaceAsset s, string owner, string style, PreviewGeometry? preview, List<AssetData> assets)
            => throw new Exception("Lane test must not dispatch to cable");
    }
}
