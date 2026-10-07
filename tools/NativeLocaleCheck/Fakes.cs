namespace UnityEngine
{
    public enum SystemLanguage { Unknown, English, ChineseSimplified, ChineseTraditional, German, Spanish, French, Italian, Japanese, Korean, Polish, Portuguese, Russian }
}
namespace Game.SceneFlow
{
    public class GameManager
    {
        public static GameManager? instance = new();
        public Localization localizationManager = new();
    }
    public class Localization
    {
        public string Missing = "";
        public static readonly Dictionary<string, UnityEngine.SystemLanguage> Languages = new()
        {
            ["en-US"] = UnityEngine.SystemLanguage.English, ["zh-HANS"] = UnityEngine.SystemLanguage.ChineseSimplified,
            ["zh-HANT"] = UnityEngine.SystemLanguage.ChineseTraditional, ["de-DE"] = UnityEngine.SystemLanguage.German,
            ["es-ES"] = UnityEngine.SystemLanguage.Spanish, ["fr-FR"] = UnityEngine.SystemLanguage.French,
            ["it-IT"] = UnityEngine.SystemLanguage.Italian, ["ja-JP"] = UnityEngine.SystemLanguage.Japanese,
            ["ko-KR"] = UnityEngine.SystemLanguage.Korean, ["pl-PL"] = UnityEngine.SystemLanguage.Polish,
            ["pt-BR"] = UnityEngine.SystemLanguage.Portuguese, ["ru-RU"] = UnityEngine.SystemLanguage.Russian
        };
        public UnityEngine.SystemLanguage LocaleIdToSystemLanguage(string id) => id == Missing ? UnityEngine.SystemLanguage.Unknown : Languages[id];
        public string GetLocalizedName(string id) => "Native " + id;
    }
}
namespace Colossal
{
    public struct Hash128 { public static Hash128 Parse(string _) => new(); }
}
namespace Colossal.IO.AssetDatabase
{
    public class AssetData
    {
        public string path = "", name = "";
        public bool isBuiltin;
        public (Colossal.Hash128 guid, int unused) id;
        public Meta GetMeta() => new();
        public Stream GetReadStream() => new MemoryStream();
    }
    public class Meta { public string extension = ".Prefab"; }
    public class PrefabAsset : AssetData { public T? GetInstance<T>() => default; }
    public class LocaleData(string locale, Dictionary<string, string> strings, Dictionary<string, int> counts)
    {
        public string localeId = locale;
        public Dictionary<string, string> entries = strings;
        public Dictionary<string, int> indexCounts = counts;
    }
    public class LocaleAsset : AssetData
    {
        public LocaleData Data = null!;
        public LocaleData data => Data;
        public int Saves;
        public UnityEngine.SystemLanguage Language;
        public string LocalizedName = "";
        public byte[] Bytes = [];
        public void SetData(LocaleData data, UnityEngine.SystemLanguage language, string name)
        { Data = data; Language = language; LocalizedName = name; }
        // Same binary header/body order as native LocaleAsset.Save, inspected from the game DLL.
        public void Save(bool force)
        {
            Saves++;
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output);
            writer.Write((ushort)1); writer.Write(Language.ToString()); writer.Write(Data.localeId); writer.Write(LocalizedName);
            writer.Write(Data.entries.Count);
            foreach (var pair in Data.entries) { writer.Write(pair.Key); writer.Write(pair.Value); }
            writer.Write(Data.indexCounts.Count);
            Bytes = output.ToArray();
        }
    }
    public class SearchFilter<T> { public Func<T, bool> Predicate = _ => true; public static SearchFilter<T> ByCondition(Func<T, bool> predicate) => new() { Predicate = predicate }; }
    public class AssetDataPath { public string Name = "", Path = ""; public static AssetDataPath Create(string path, string name) => new() { Name = name, Path = path }; }
    public class Database
    {
        public List<AssetData> Items = new();
        public IEnumerable<T> GetAssets<T>(SearchFilter<T> filter) => Items.OfType<T>().Where(filter.Predicate);
        public T AddAsset<T>(AssetDataPath path) where T : AssetData, new() { var a = new T { name = path.Name, path = path.Path }; Items.Add(a); return a; }
        public bool TryGetAsset(Colossal.Hash128 id, out AssetData asset) { asset = null!; return false; }
    }
    public static class AssetDatabase { public static Database user = new(), global = new(); }
}
namespace Game.Prefabs
{
    public class ComponentBase { }
    public class UIObject : ComponentBase { public string m_Icon = ""; }
    public class PrefabBase
    {
        public Colossal.IO.AssetDatabase.PrefabAsset? asset;
        public string uiTag = "";
        public List<ComponentBase> components = new();
        public bool TryGet<T>(out T item) { item = components.OfType<T>().FirstOrDefault()!; return item != null; }
    }
    public class NetGeometryPrefab : PrefabBase { }
}
namespace BridgeBuilder.Settings
{
    internal static class UiStringCatalog
    {
        internal static string[] LocaleIds = Game.SceneFlow.Localization.Languages.Keys.ToArray();
        internal static string Resolve(string? locale) => LocaleIds.Contains(locale) ? locale! : "en-US";
        internal static UiStrings Current = new();
    }
    internal class UiStrings { internal string LocaleId = "en-US"; }
}
namespace BridgeBuilder.Runtime
{
    internal class BridgeAssetInfo(string id, string name)
    {
        internal string PrefabName = id, DisplayName = name;
        internal static bool IsPrefabName(string name) => name.Length == 37 && name[0] == 'b' && Guid.TryParse(name[1..], out _);
        internal static bool MatchesOwner(string path, string owner) => path.Contains(owner);
    }
    internal static class BridgeDependencyCopies { internal static IEnumerable<string> References(byte[] bytes) => Array.Empty<string>(); }
}
namespace CS2Mods.Shared.Conversion { public static class PrefabGraphCloner { public static bool ShouldStripComponent(Game.Prefabs.ComponentBase c, bool root) => false; } }
