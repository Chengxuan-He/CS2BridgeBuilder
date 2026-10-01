using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Game.Modding;

// Catalogue-only tests: compile the real source policy with metadata/API doubles, no geometry.
var active = new ParadoxModsDataSource();
AssetDatabase<ParadoxMods>.instance.dataSource = active;
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
PrefabBase Donor(string id) => new() { isSubscribedMod = true, asset = new() { Meta = new() { platformID = id } } };
var baseBridge = Donor("92245");
var portsBridge = Donor("124160");
var shared = new AssetPackPrefab { name = "Bridge Asset Pack Filter", isSubscribedMod = true,
    asset = new() { Meta = new() { platformID = "124160" } } };
baseBridge.Put(new AssetPackItem { m_Packs = new[] { shared } });
portsBridge.Put(new AssetPackItem { m_Packs = new[] { shared } });
var dlc = new ContentPrefab { name = "Bridges & Ports" };
dlc.Put(new DlcRequirement());
portsBridge.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc });
var b = BridgePrototypeSource.Inspect(baseBridge);
var p = BridgePrototypeSource.Inspect(portsBridge);
Check(b.Key != p.Key, "BXP owners merged");
Check(b.HasSingleSource && p.HasSingleSource, "valid donors rejected");
Check(b.Label == "Mod: Bridge Expansion Pack", "base label");
Check(p.Label.Contains("Bridge Expansion Pack: B&P") && p.Label.Contains("DLC: Bridges & Ports"), "ports label");
Check(p.LocalizedLabel.Contains("Bridge Expansion Pack: B&P"), "localized ports label");
foreach (var baseEnabled in new[] { false, true })
foreach (var portsEnabled in new[] { false, true })
foreach (var dlcEnabled in new[] { false, true })
{
    active.Active.Clear();
    if (baseEnabled) active.Active.Add("92245");
    if (portsEnabled) active.Active.Add("124160");
    dlc.Available = dlcEnabled;
    Check(b.IsAvailable == baseEnabled, "shared category incorrectly gates base pack");
    Check(p.IsAvailable == (portsEnabled && dlcEnabled), "ports owner/DLC gate bypass");
}
active.Active.UnionWith(new[] { "92245", "124160" }); dlc.Available = true;
shared.asset!.Meta.platformID = "92245";
active.Active.Remove("92245");
Check(p.IsAvailable, "shared category chosen from disabled base pack hides ports pack");
portsBridge.active = false;
Check(!p.IsAvailable, "inactive donor admitted"); portsBridge.active = true;
portsBridge.Put(new ContentPrerequisite());
Check(!BridgePrototypeSource.Inspect(portsBridge).IsAvailable, "null DLC reference admitted");
portsBridge.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc });
portsBridge.asset!.Meta.platformID = "";
Check(!BridgePrototypeSource.Inspect(portsBridge).IsAvailable, "missing owner ID admitted");
Check(!BridgePrototypeSource.Inspect(new PrefabBase { isBuiltin = true }, true).IsAvailable, "unknown Golden Gate owner admitted");
Check(BridgePrototypeSource.Inspect(new PrefabBase { isBuiltin = true }).IsAvailable, "native base game lost");
var nativeDlc = new PrefabBase { isBuiltin = true };
nativeDlc.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc }); dlc.Available = false;
Check(!BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "native DLC bypass");
baseBridge.Put(new AssetPackItem { m_Packs = new AssetPackPrefab[] { null! } });
Check(!BridgePrototypeSource.Inspect(baseBridge).IsAvailable, "null pack admitted");
Console.WriteLine($"PASS {checks} prototype source checks (separate BXP IDs, shared category collision, DLC, inactive/stale/invalid metadata).");

namespace Game.Prefabs
{
    public class PrefabBase
    {
        public string name = "test";
        public bool active = true, isBuiltin, isSubscribedMod;
        public Colossal.IO.AssetDatabase.Asset? asset;
        private readonly Dictionary<Type, object> components = new();
        public void Put<T>(T component) where T : class => components[typeof(T)] = component;
        public T? GetComponent<T>() where T : class => components.TryGetValue(typeof(T), out var value) ? (T)value : null;
    }
    public class ContentPrefab : PrefabBase { public bool Available = true; public bool IsAvailable() => Available; }
    public class AssetPackPrefab : PrefabBase { }
    public class ContentPrerequisite { public ContentPrefab? m_ContentPrerequisite; }
    public class AssetPackItem { public AssetPackPrefab[]? m_Packs; }
    public class DlcRequirement { }
    public class ModRequirement { public string m_ModId = ""; }
}
namespace Colossal.IO.AssetDatabase
{
    public class Meta { public string platformID = ""; }
    public class Asset { public Meta Meta = new(); public string path = ""; public Meta GetMeta() => Meta; }
    public class ParadoxMods { }
    public class AssetDatabase<T> { public static AssetDatabase<T> instance = new(); public object? dataSource; }
}
namespace Game.Modding
{
    public class ModManager : List<ModManager.ModInfo>
    {
        public class ModInfo { public enum State { Loaded } public State state; public Executable? asset; }
        public class Executable { public System.Reflection.Assembly? assembly; }
    }
    public class ParadoxModsDataSource
    {
        public HashSet<string> Active = new();
        public bool ContainsActiveMod(string id) => Active.Contains(id);
    }
}
namespace Game.SceneFlow
{
    public class GameManager { public static GameManager? instance; public Game.Modding.ModManager? modManager; }
}
namespace BridgeBuilder.Settings
{
    public static class RuntimeUiText { public static string Get(string key, params object[] args) => key + ": " + string.Join(", ", args); }
}
namespace BridgeBuilder.Bridges
{
    public static class DeckCatalog { public static string DisplayNameOf(PrefabBase prefab) => prefab.name; }
}
