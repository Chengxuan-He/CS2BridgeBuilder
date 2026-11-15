using System.Reflection;
using System.Reflection.Emit;
using BridgeBuilder.Bridges;
using CS2Mods.Shared.Discovery;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;

// Catalogue/availability checks only: no mesh generation, game binaries or persistent assets.
static void Check(bool value, string message) { if (!value) throw new Exception(message); }
var road = new RoadPrefab { name = "RoadBuilder ordinary road name" };
var path = new PathwayPrefab { name = "path" };
var train = new TrackPrefab { name = "train", m_TrackType = Game.Net.TrackTypes.Train };
var builder = new RoadBuilder.Prefabs.BuilderRoad { name = "builder" };
var builderPath = new RoadBuilder.Prefabs.BuilderPath { name = "builder-path" };
var prefabs = new PrefabSystem { Items = new PrefabBase[] { road, path, train, builder, builderPath } };
var described = new[] { new RoadBuilderRoad(builder, "Configured road", "", "", 0, 0, Array.Empty<string>()) };

void VerifyAbsent(string scenario)
{
    Check(!RoadBuilderCompatibility.IsAvailable, scenario + ": integration enabled");
    DeckCatalog.Rebuild(prefabs, described); // Deliberately pass stale discovery results.
    Check(DeckCatalog.Decks.Select(deck => deck.Id).Order().SequenceEqual(new[] { road.name, path.name, train.name }.Order()),
        scenario + ": leaked builder prefab or lost ordinary network");
    Check(DeckCatalog.Find(builder.name) == null, scenario + ": stale selection resolves");
}
GameManager.instance = null;
VerifyAbsent("no game manager");
GameManager.instance = new();
VerifyAbsent("not installed");
var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("RoadBuilder"), AssemblyBuilderAccess.Run);
var mod = new ModManager.ModInfo { asset = new() { assembly = assembly } };
GameManager.instance.modManager.Add(mod);
foreach (var state in new[] { ModManager.ModInfo.State.Unknown, ModManager.ModInfo.State.GeneralError, ModManager.ModInfo.State.Disposed })
{
    mod.state = state;
    VerifyAbsent(state.ToString());
}
mod.state = ModManager.ModInfo.State.Loaded;
Check(RoadBuilderCompatibility.IsAvailable, "loaded optional mod not recognized");
DeckCatalog.Rebuild(prefabs, described);
Check(DeckCatalog.Find(builder.name)?.Kind == DeckKind.RoadBuilder, "configured builder road missing");
builder.Deleted = true;
DeckCatalog.Rebuild(prefabs, described);
Check(DeckCatalog.Find(builder.name) == null, "deleted Road Builder source leaked into catalogue");
builder.Deleted = false;
Check(!RoadBuilderCompatibility.TryOpen(new Unity.Entities.World()), "missing optional UI must fail safely");
DeckCatalog.Rebuild(prefabs, Array.Empty<RoadBuilderRoad>());
Check(DeckCatalog.Find(builder.name) == null, "broken builder road admitted as ordinary road");
mod.state = ModManager.ModInfo.State.Disposed;
VerifyAbsent("unloaded after successful scan");
mod.state = ModManager.ModInfo.State.Loaded;
mod.asset.assembly = typeof(Program).Assembly;
VerifyAbsent("unrelated loaded mod");
Console.WriteLine("PASS optional Road Builder: absent/disabled/failed/disposed/loaded states, stale catalogue exclusion, ordinary roads/paths/tracks retained.");

// Source-linked discovery, catalogue selection and shared selection must retain equal labels.
mod.asset.assembly = assembly;
var first = new RoadBuilder.Domain.Prefabs.RoadBuilderPrefab { name = "r11111111-1111-1111-1111-111111111111", Config = new() { Name = "Same" } };
var second = new RoadBuilder.Domain.Prefabs.RoadBuilderPrefab { name = "r22222222-2222-2222-2222-222222222222", Config = new() { Name = "Same" } };
var unnamed = new RoadBuilder.Domain.Prefabs.RoadBuilderPrefab { name = "r33333333-3333-3333-3333-333333333333", Config = new() { Name = "" } };
var native = new RoadPrefab { name = "Same" };
prefabs.Items = new PrefabBase[] { first, second, first, unnamed, native };
var found = RoadBuilderDiscovery.Find(prefabs).Roads;
Check(found.Count == 3, "same-label, native-label collision, duplicate ID or empty label handling");
Check(found.Single(item => item.Id == unnamed.name).Name == unnamed.name, "empty label should use actual ID for presentation");
DeckCatalog.Rebuild(prefabs, found);
Check(DeckCatalog.Decks.Count(deck => deck.Id == first.name) == 1, "duplicate identity appears twice");
Check(ReferenceEquals(DeckCatalog.Find(first.name)?.Prefab, first), "first same-label selection incorrect");
Check(ReferenceEquals(DeckCatalog.Find(second.name)?.Prefab, second), "second same-label selection incorrect");
Check(ReferenceEquals(DeckCatalog.Find(native.name)?.Prefab, native), "native label collision hid native road");
first.Config.Name = "Renamed";
DeckCatalog.Rebuild(prefabs, RoadBuilderDiscovery.Find(prefabs).Roads);
Check(DeckCatalog.Find(first.name)?.DisplayName == "Renamed", "rename changed identity");
var owner = new object();
CS2Mods.Shared.Infrastructure.RoadSelectionModel.PublishRoads(owner, new[] {
    new CS2Mods.Shared.Infrastructure.RoadListItem(first.name, "Same", CS2Mods.Shared.Infrastructure.RoadExportStatus.NotExported, "first"),
    new CS2Mods.Shared.Infrastructure.RoadListItem(second.name, "Same", CS2Mods.Shared.Infrastructure.RoadExportStatus.NotExported, "second") }, "");
CS2Mods.Shared.Infrastructure.RoadSelectionModel.SetSelected(second.name, true);
Check(CS2Mods.Shared.Infrastructure.RoadSelectionModel.SelectedRoadIds().SequenceEqual(new[] { second.name }), "same-label rows select together");
Check(CS2Mods.Shared.Infrastructure.RoadSelectionModel.DetailOf(first.name) == "first", "detail lookup uses label");
Console.WriteLine("PASS prefab identity: equal labels, native-label collision, duplicate ID, blank label, distinct selections and rename.");

var iconRoot = Path.Combine(Path.GetTempPath(), "BridgeIdentityCheck-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(iconRoot);
try
{
    File.WriteAllText(Path.Combine(iconRoot, "a.svg"), "<svg>first</svg>");
    File.WriteAllText(Path.Combine(iconRoot, "b.svg"), "<svg>second</svg>");
    Colossal.UI.UIManager.defaultUISystem.resourceHandler.Add(iconRoot);
    CS2Mods.Shared.Export.ExportPaths.IconsDirectory = Path.Combine(iconRoot, "out");
    var a = new RoadBuilderRoad(first, "Same", "coui://roadbuilderthumbnails/a.svg", "", 0, 0, Array.Empty<string>());
    var b = new RoadBuilderRoad(second, "Same", "coui://roadbuilderthumbnails/b.svg", "", 0, 0, Array.Empty<string>());
    var report = new CS2Mods.Shared.Export.ExportReport();
    var iconA = CS2Mods.Shared.Export.RoadBuilderIconExporter.Preserve(a, report, false);
    var iconB = CS2Mods.Shared.Export.RoadBuilderIconExporter.Preserve(b, report, false);
    Check(iconA != iconB, "same-label exported icons collide");
    Check(Directory.GetFiles(CS2Mods.Shared.Export.ExportPaths.IconsDirectory).Length == 2, "distinct icons not persisted");
    Check(File.ReadAllText(Path.Combine(CS2Mods.Shared.Export.ExportPaths.IconsDirectory, Path.GetFileName(new Uri(iconA).AbsolutePath))) == "<svg>first</svg>", "first icon overwritten");
}
finally { Directory.Delete(iconRoot, true); }
Console.WriteLine("PASS identity-based exported thumbnails: separate files and original content retained.");

namespace Game.Modding
{
    public class ModManager : List<ModManager.ModInfo>
    {
        public class ModInfo
        {
            public enum State { Unknown, Loaded, GeneralError, Disposed }
            public State state;
            public Executable asset = new();
        }
        public class Executable { public Assembly? assembly; }
    }
}
namespace Game.SceneFlow
{
    public class GameManager
    {
        public static GameManager? instance;
        public Game.Modding.ModManager modManager = new();
        public Localization localizationManager = new();
    }
    public class Localization { public Colossal.Localization.LocalizationDictionary activeDictionary = new(); }
}
namespace Colossal.Localization { public class LocalizationDictionary : Dictionary<string, string> { } }
namespace Game.Net { public enum TrackTypes { Train, Subway, Tram } }
namespace Game.Prefabs
{
    public class PrefabBase { public List<object> components = new(); public string name = ""; public T? GetComponent<T>() where T : class => null; }
    public class NetGeometryPrefab : PrefabBase { }
    public class RoadPrefab : NetGeometryPrefab { public string m_RoadType = ""; public float m_SpeedLimit, m_MaxSlopeSteepness; public bool m_TrafficLights, m_HighwayRules; public Section[]? m_Sections; }
    public class Section { public PrefabBase? m_Section; }
    public class UIObject { public string m_Icon = ""; }
    public class PathwayPrefab : NetGeometryPrefab { }
    public class TrackPrefab : NetGeometryPrefab { public Game.Net.TrackTypes m_TrackType; }
    public class Bridge { }
    public class PrefabSystem { public PrefabBase[] Items = Array.Empty<PrefabBase>(); }
}
namespace RoadBuilder.Prefabs
{
    public class BuilderRoad : RoadPrefab, RoadBuilder.Domain.Prefabs.INetworkBuilderPrefab
    { public bool Deleted { get; set; } }
    public class BuilderPath : PathwayPrefab { }
}
namespace RoadBuilder.Domain.Prefabs
{
    public class RoadBuilderPrefab : RoadPrefab, INetworkBuilderPrefab { public bool Deleted { get; set; } public Config Config { get; set; } = new(); }
    public class Config { public string Name { get; set; } = ""; }
    public interface INetworkBuilderPrefab { bool Deleted { get; } }
}
namespace Unity.Entities
{
    public class World { public object? GetExistingSystemManaged(Type type) => null; }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static Logger Log = new(); }
    internal sealed class Logger { internal void Warn(Exception exception, string message) { } }
}
namespace CS2Mods.Shared
{
    internal static class PrefabCatalog { internal static IEnumerable<PrefabBase> GetAll(PrefabSystem system) => system.Items; }
    internal static class NetWidth { internal static float Of(NetGeometryPrefab prefab) => 0f; }
    internal static class ModHost { internal static string Id = "test"; internal static Log Log = new(); internal static void RebuildOptionsPage() { } }
    internal class Log { internal void Info(string message) { } internal void Warn(Exception error, string message) { } }
}
namespace CS2Mods.Shared.Infrastructure
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceEqualityComparer<T> Instance = new();
        public bool Equals(T? left, T? right) => ReferenceEquals(left, right);
        public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}

namespace Colossal.Json { internal static class JSON { internal static string Dump(object value) => System.Text.Json.JsonSerializer.Serialize(value); } }
namespace BridgeBuilder.Runtime { internal static class BridgeDeferredRemoval { internal static bool ExcludedPrefab(Game.Prefabs.PrefabBase prefab) => false; } }

namespace CS2Mods.Shared.Export
{
    internal class ExportReport { internal void Warning(string value) { } }
    internal static class ExportPaths { internal static string IconsDirectory = ""; internal static void EnsureIconsDirectory() => Directory.CreateDirectory(IconsDirectory); }
}
namespace Colossal.UI
{
    public static class UIManager { public static UISystem defaultUISystem = new(); }
    public class UISystem { public DefaultResourceHandler resourceHandler { get; } = new(); public void AddHostLocation(string host, string path, bool flag) { } public void RemoveHostLocation(string host) { } }
    public class DefaultResourceHandler { private readonly IDictionary<string,List<(string,int)>> m_HostLocationsMap = new Dictionary<string,List<(string,int)>>(); public void Add(string path) => m_HostLocationsMap["roadbuilderthumbnails"] = new() { (path,0) }; }
}
