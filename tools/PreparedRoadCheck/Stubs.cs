// Production routing/ownership test doubles. Real native serialization is checked
// separately by PreparedNativeCheck in the offline Unity editor process.
namespace Colossal.AssetPipeline
{
    internal sealed class Surface(string name, string template) : IDisposable
    { internal string name = name, Template = template; public void Dispose() { } }
}
namespace Colossal.IO.AssetDatabase
{
    internal class AssetData { }
    internal sealed class TextureAsset { internal string id = ""; internal bool isDummy; }
    internal sealed class SurfaceAsset : AssetData
    {
        internal string name = "Native Road";
        internal int materialTemplateHash = 17, Leases, Saves;
        internal Dictionary<string, TextureAsset> textures = new();
        internal Dictionary<string, float> floats = new();
        internal HashSet<string> keywords = new();
        internal void LoadProperties(bool useVT) => Leases++;
        internal void UnloadProperties() => Leases--;
        internal bool TryGetTexture(string property, out TextureAsset? texture) => textures.TryGetValue(property, out texture);
        internal bool TryGetFloatProperty(string property, out float value) => floats.TryGetValue(property, out value);
        internal void UpdateTextures(Dictionary<string, TextureAsset> values) => textures = values;
        internal void UpdateFloats(Dictionary<string, float> values) => floats = values;
        internal void UpdateKeywords(HashSet<string> values) => keywords = values;
        internal bool Save(int mipBias, bool force, bool saveTextures, bool vt) { Saves++; return true; }
    }
    internal sealed class Database
    {
        internal static int Created;
        internal SurfaceAsset AddAsset(string path, Colossal.AssetPipeline.Surface data) { Created++; return new() { name = data.name, Leases = 1 }; }
    }
    internal static class AssetDatabase { internal static Database user = new(); internal static Global global = new(); }
    internal sealed class Global { internal Resources resources = new(); }
    internal sealed class Resources { internal Library materialLibrary = new(); }
    internal sealed class Library { internal Template GetMaterial(int id) => new(); }
    internal sealed class Template { internal float GetFloat(string property) => .125f; }
}
namespace BridgeBuilder.Runtime
{
    using Colossal.IO.AssetDatabase;
    internal static class BridgePreparedMaterials
    {
        internal static string Style = "";
        internal static SurfaceAsset Prepared = null!;
        internal static int Reads;
        internal static SurfaceAsset Road(string style)
        { if (style != Style) throw new Exception("Wrong material style"); Reads++; return Prepared; }
    }
    internal static class BridgePrefabPersistence { internal static string ResourcePath(string name, string category) => category + "/" + name; }
}
namespace BridgeBuilder.Bridges
{
    using Colossal.IO.AssetDatabase;
    internal sealed class PreviewGeometry { internal Database NativeAssets = new(); internal void RetainSurfaceProperties(SurfaceAsset surface) { } }
}
