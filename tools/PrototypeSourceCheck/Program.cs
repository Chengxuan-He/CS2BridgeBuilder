using Colossal.PSI.Common;
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
var dlc = new ContentPrefab { name = "BridgesAndPorts" };
dlc.Put(new DlcRequirement { m_Dlc = new DlcId(5), m_BaseGameRequiresDatabase = true });
AssetDatabase.global.Databases[dlc.name] = new NativeDatabase { dlcId = new DlcId(5) };
portsBridge.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc });
var b = BridgePrototypeSource.Inspect(baseBridge);
var p = BridgePrototypeSource.Inspect(portsBridge);
Check(b.Key != p.Key, "BXP owners merged");

Check(b.Label == "Mod: Bridge Expansion Pack", "base label");
Check(p.Label == "Mod: Bridge Expansion Pack: B&P" && !p.LocalizedLabel.Contains("SourceDlc"), "only direct mod source is displayed");
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
    Check(p.IsAvailable == portsEnabled, "direct mod source must not expand transitive DLC requirements");
}
active.Active.UnionWith(new[] { "92245", "124160" }); dlc.Available = true;
shared.asset!.Meta.platformID = "92245";
active.Active.Remove("92245");
Check(p.IsAvailable, "shared category chosen from disabled base pack hides ports pack");
portsBridge.active = false;
Check(!p.IsAvailable, "inactive donor admitted"); portsBridge.active = true;
portsBridge.Put(new ContentPrerequisite());
Check(BridgePrototypeSource.Inspect(portsBridge).IsAvailable, "direct active mod must not inspect inherited prerequisite metadata");
portsBridge.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc });
portsBridge.asset!.Meta.platformID = "";
Check(!BridgePrototypeSource.Inspect(portsBridge).IsAvailable, "missing owner ID admitted");
Check(!BridgePrototypeSource.Inspect(new PrefabBase { isBuiltin = true }, true).IsAvailable, "unknown Golden Gate owner admitted");
Check(BridgePrototypeSource.Inspect(new PrefabBase { isBuiltin = true }).IsAvailable, "native base game lost");
var nativeDlc = new PrefabBase { isBuiltin = true };
nativeDlc.Put(new ContentPrerequisite { m_ContentPrerequisite = dlc }); dlc.Available = false;
Check(!BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "native DLC bypass");
dlc.Available = true;
AssetDatabase.global.Databases.Clear();
Check(!BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "DLC ownership without its database must not admit resources");
AssetDatabase.global.Databases[dlc.name] = new NativeDatabase { dlcId = new DlcId(1) };
Check(!BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "Mismatched DLC database must not admit resources");
AssetDatabase.global.Databases[dlc.name].dlcId = new DlcId(5);
Check(BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "Matching available DLC database restores resources");
baseBridge.Put(new AssetPackItem { m_Packs = new AssetPackPrefab[] { null! } });
active.Active.Add("92245");
Check(BridgePrototypeSource.Inspect(baseBridge).IsAvailable, "direct active mod must not inspect category metadata");
var go = Donor("166012");
active.Active.Add("166012"); dlc.Available = false; AssetDatabase.global.Databases.Clear();
Check(BridgePrototypeSource.Inspect(go).IsAvailable, "GO requires its own active source, not B&P");
Check(BridgePrototypeSource.Inspect(new PrefabBase { isBuiltin = true }).IsAvailable, "base game remains available without B&P");
Check(!BridgePrototypeSource.Inspect(nativeDlc).IsAvailable, "direct DLC remains unavailable");
active.Active.Remove("166012");
Check(!BridgePrototypeSource.Inspect(go).IsAvailable, "cached GO donor without active mod rejected");
var indirectMod = new ContentPrefab { Available = false };
indirectMod.Put(new ModRequirement { m_ModId = "missing-other-mod" });
portsBridge.asset!.Meta.platformID = "124160";
portsBridge.Put(new ContentPrerequisite { m_ContentPrerequisite = indirectMod });
portsBridge.Put(new AssetPackItem { m_Packs = new[] { new AssetPackPrefab { active = false } } });
Check(BridgePrototypeSource.Inspect(portsBridge).IsAvailable, "active direct mod must not expand indirect mod/content/category requirements");
// Full publisher names, every source branch and name failure independent of ownership.
var unknown = Donor("987654"); active.Active.Add("987654");
var unknownSource = BridgePrototypeSource.Inspect(unknown);
Check(unknownSource.LocalizedLabel.Contains("SourceModNameUnavailable") && !unknownSource.LocalizedLabel.Contains("987654"), "numeric ID escaped into UI");
Check(unknownSource.IsAvailable, "missing title must not block source");
unknown.asset!.Meta.displayName = "Complete Published Bridge Collection";
Check(unknownSource.LocalizedLabel.Contains("Complete Published Bridge Collection"), "native owning asset title");
active.Titles.Add(new() { id = "987654", displayName = "Complete Published Bridge Collection - Revised" });
Check(unknownSource.LocalizedLabel.Contains("Collection - Revised"), "published title refresh");
var required = new ContentPrefab { name = "TechnicalPrerequisite" };
required.Put(new ModRequirement { m_ModId = "987654" });
var requiredRoot = new PrefabBase { isBuiltin = true }; requiredRoot.Put(new ContentPrerequisite { m_ContentPrerequisite = required });
Check(BridgePrototypeSource.Inspect(requiredRoot).LocalizedLabel.Contains("Collection - Revised"), "ModRequirement uses full mod name");
var externalPack = new AssetPackPrefab { name = "ShortInternalAlias", isSubscribedMod = true,
    asset = new() { Meta = new() { platformID = "987654" } } };
var packRoot = new PrefabBase(); packRoot.Put(new AssetPackItem { m_Packs = new[] { externalPack } });
Check(BridgePrototypeSource.Inspect(packRoot).LocalizedLabel.Contains("Collection - Revised"), "asset pack uses owner title, not internal category");
var local = new PrefabBase { name = "PrivatePrefab", asset = new() { path = "mods/AbbreviatedFolder/Prefab" } };
var localSource = BridgePrototypeSource.Inspect(local);
Check(localSource.LocalizedLabel.Contains("SourceModNameUnavailable") && !localSource.LocalizedLabel.Contains("AbbreviatedFolder"), "folder fallback leaked into source UI");
Check(localSource.Key == "Mod: AbbreviatedFolder", "technical fallback source identity changed");
active.Titles.Clear(); unknown.asset.Meta.displayName = "987654";
Check(BridgePrototypeSource.Inspect(Donor("166012")).LocalizedLabel.Contains("GO 224m Viaduct Bridge"), "GO full published source title is retained");
foreach(var item in new[]{(Id:"162995",Title:"上海南浦大桥"),(Id:"151335",Title:"卢浦大桥")}) {
 active.Active.Add(item.Id);
 var source=BridgePrototypeSource.Inspect(Donor(item.Id));
 Check(source.IsAvailable&&source.LocalizedLabel.Contains(item.Title),"Named bridge active source and full official title: "+item.Id);
 active.Active.Remove(item.Id);
 Check(!source.IsAvailable,"Named bridge inactive direct source: "+item.Id);
}
Check(unknownSource.LocalizedLabel.Contains("SourceModNameUnavailable"), "numeric metadata is not a published title");
// Ordered declared prerequisites resolve native content identity and active PDX IDs.
// Actual resources.assets: SanFranciscoSet requirement pathID 127912 (DlcId 1, database=false);
// BridgesAndPorts pathID 127909 (DlcId 5, database=true). Native method body below.
var sf = new ContentPrefab { name = "SanFranciscoSet", isBuiltin = true };
sf.Put(new DlcRequirement { m_Dlc = new DlcId(1) });
AssetDatabase.global.Databases[sf.name] = new NativeDatabase { dlcId = new DlcId(1) };
var keys = BridgeStyleSources.For("GoldenGateDouble");
Check(keys.SequenceEqual(new[]{"mod:92245", "content:SanFranciscoSet"}), "registered order and exact Golden Gate identities");
Check(BridgeStyleSources.For("ArtDecoViaduct").SequenceEqual(keys), "Art Deco source pylon retains its native San Francisco Set prerequisite");
var requirements = keys.Select(key => BridgePrototypeSource.Required(key,new PrefabBase[]{sf})).ToArray();
foreach(var modOn in new[]{false,true}) foreach(var dlcOn in new[]{false,true}) {
 active.Active.Clear(); if(modOn) active.Active.Add("92245"); sf.Available=dlcOn;
 Check(requirements.All(source=>source.IsAvailable)==(modOn&&dlcOn), "every explicit requirement gates availability");
}
active.Active.Add("92245"); sf.Available=true;
Check(requirements[0].LocalizedLabel.Contains("Bridge Expansion Pack") && requirements[1].LocalizedLabel.Contains("SanFranciscoSet"), "ordered localized labels");
sf.active=false;Check(!requirements.All(source=>source.IsAvailable),"disabled DLC");sf.active=true;
AssetDatabase.global.Databases.Remove(sf.name);Check(requirements.All(source=>source.IsAvailable),"native SF content does not require a separate database");
Check(!BridgePrototypeSource.Required("content:SanFranciscoSet",Array.Empty<PrefabBase>()).IsAvailable,"unregistered DLC");
foreach (var style in new[] { "GoldenGate", "GoldenGateDouble", "ArtDecoViaduct" })
{
    var declared = BridgeStyleSources.For(style).Select(key => BridgePrototypeSource.Required(key, new PrefabBase[] { sf })).ToArray();
    Check(declared.All(source => source.IsAvailable), "Actual SF requirement without database: " + style);
    sf.Available = false;
    Check(!declared.All(source => source.IsAvailable), "Unowned SF is rejected: " + style);
    sf.Available = true;
    if (style != "GoldenGate")
    {
        active.Active.Remove("92245");
        Check(!declared.All(source => source.IsAvailable), "SF ownership does not replace required mod: " + style);
        active.Active.Add("92245");
    }
}

Check(BridgeStyleSources.For("Grand").Length==0,"base has no synthetic source entry");
Check(BridgeStyleSources.For("Lupu").SequenceEqual(new[]{"mod:151335"}),"single mod unaffected");
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
    public class ContentPrefab : PrefabBase
    {
        public bool Available = true;
        public bool IsAvailable() => Available && (GetComponent<DlcRequirement>()?.CheckRequirement(this) ?? true);
    }
    public class AssetPackPrefab : PrefabBase { }
    public class ContentPrerequisite { public ContentPrefab? m_ContentPrerequisite; }
    public class AssetPackItem { public AssetPackPrefab[]? m_Packs; }
    // Native Game.Prefabs.DlcRequirement.CheckRequirement control flow, with platform
    // ownership supplied by ContentPrefab.Available. This is an offline API boundary,
    // not a running-game acceptance claim. See work/dlc-availability-20261117 evidence.
    public class DlcRequirement
    {
        public DlcId m_Dlc;
        public bool m_BaseGameRequiresDatabase;
        public bool CheckRequirement(ContentPrefab prefab)
        {
            if (!m_BaseGameRequiresDatabase || (AssetDatabase.global.TryGetDatabase(prefab.name, out var database) && database.dlcId == m_Dlc))
                return prefab.Available;
            return false;
        }
    }
    public class ModRequirement { public string m_ModId = ""; }
}
namespace Colossal.IO.AssetDatabase
{
    public struct Meta { public string platformID = "", displayName = ""; public Meta() { } }
    public class Asset { public Meta Meta = new(); public string path = ""; public Meta GetMeta() => Meta; }
    public class NativeDatabase { public DlcId dlcId; }
    public class AssetDatabase
    {
        public static AssetDatabase global = new();
        public Dictionary<string, NativeDatabase> Databases = new();
        public bool TryGetDatabase(string name, out NativeDatabase database) => Databases.TryGetValue(name, out database!);
    }
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
        public struct PublishedMod { public string id, displayName; }
        public List<PublishedMod> Titles = new();
        public IReadOnlyCollection<PublishedMod> GetActiveMods() => Titles;
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
