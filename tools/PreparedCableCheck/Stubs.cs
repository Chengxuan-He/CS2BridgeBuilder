using UnityEngine;
// These doubles test production routing/lifetime only. The Unity integration probe
// separately invokes the installed game's native binary readers and texture upload.
namespace Colossal.AssetPipeline
{
    internal sealed class Surface : IDisposable
    {
        internal readonly string name;
        internal Surface(string name, string template) => this.name = name;
        public void Dispose() { }
    }
}
namespace Colossal.IO.AssetDatabase
{
    internal class AssetData { }
    internal sealed class TextureAsset : AssetData { }
    internal sealed class SurfaceAsset : AssetData
    {
        internal string name = "", id = "";
        internal int materialTemplateHash = 30, Leases, SaveCount;
        internal bool isVTMaterial;
        internal Dictionary<string, float> floats = new();
        internal Dictionary<string, Color> colors = new();
        internal Dictionary<string, TextureAsset> textures = new();
        internal HashSet<string> keywords = new();
        internal void LoadProperties(bool useVT) => Leases++;
        internal void UnloadProperties() => Leases--;
        internal void UpdateTextures(Dictionary<string, TextureAsset> data) => textures = data;
        internal void UpdateColors(Dictionary<string, Color> data) => colors = data;
        internal void UpdateFloats(Dictionary<string, float> data) => floats = data;
        internal void UpdateKeywords(HashSet<string> data) => keywords = data;
        internal bool Save(int mipBias, bool force, bool saveTextures, bool vt) { SaveCount++; return true; }
    }
    internal sealed class Database
    {
        internal static int Created;
        internal SurfaceAsset AddAsset(string path, Colossal.AssetPipeline.Surface data)
        { Created++; return new SurfaceAsset { name = data.name, Leases = 1 }; }
    }
    internal static class AssetDatabase
    {
        internal static readonly Database user = new();
        internal static readonly Global global = new();
    }
    internal sealed class Global { internal readonly Resources resources = new(); }
    internal sealed class Resources { internal readonly Library materialLibrary = new(); }
    internal sealed class Library
    {
        internal readonly Template Template = new();
        internal Template GetMaterial(int id) => Template;
    }
    internal sealed class Template
    {
        internal Color Color;
        internal Dictionary<string, float> Floats = new();
        internal Color GetColor(string property) => Color;
        internal float GetFloat(string property) => Floats[property];
    }
}
namespace BridgeBuilder.Runtime
{
    using Colossal.IO.AssetDatabase;
    internal static class BridgePreparedMaterials
    {
        internal static string Style = "";
        internal static SurfaceAsset Prepared = null!;
        internal static SurfaceAsset Cable(string style)
        { if (style != Style) throw new Exception("Wrong style catalogue."); return Prepared; }
    }
    internal static class BridgePrefabPersistence { internal static string ResourcePath(string name, string folder) => folder + "/" + name; }
}
namespace BridgeBuilder.Bridges
{
    using Colossal.IO.AssetDatabase;
    internal sealed class PreviewGeometry
    {
        internal readonly Database NativeAssets = new();
        internal void RetainSurfaceProperties(SurfaceAsset surface) { }
    }
}
