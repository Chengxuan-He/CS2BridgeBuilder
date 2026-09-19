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
var registry = new PrefabSystem { Items = new PrefabBase[] { road, path, train, builder, builderPath } };
var described = new[] { new RoadBuilderRoad { Prefab = builder, Name = "Configured road" } };

void VerifyAbsent(string scenario)
{
    Check(!RoadBuilderCompatibility.IsAvailable, scenario + ": integration enabled");
    DeckCatalog.Rebuild(registry, described); // Deliberately pass stale discovery results.
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
DeckCatalog.Rebuild(registry, described);
Check(DeckCatalog.Find(builder.name)?.Kind == DeckKind.RoadBuilder, "configured builder road missing");
DeckCatalog.Rebuild(registry, Array.Empty<RoadBuilderRoad>());
Check(DeckCatalog.Find(builder.name) == null, "broken builder road admitted as ordinary road");
mod.state = ModManager.ModInfo.State.Disposed;
VerifyAbsent("unloaded after successful scan");
mod.state = ModManager.ModInfo.State.Loaded;
mod.asset.assembly = typeof(Program).Assembly;
VerifyAbsent("unrelated loaded mod");
Console.WriteLine("PASS optional Road Builder: absent/disabled/failed/disposed/loaded states, stale catalogue exclusion, ordinary roads/paths/tracks retained.");

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
    public class PrefabBase { public string name = ""; public T? GetComponent<T>() where T : class => null; }
    public class NetGeometryPrefab : PrefabBase { }
    public class RoadPrefab : NetGeometryPrefab { }
    public class PathwayPrefab : NetGeometryPrefab { }
    public class TrackPrefab : NetGeometryPrefab { public Game.Net.TrackTypes m_TrackType; }
    public class Bridge { }
    public class PrefabSystem { public PrefabBase[] Items = Array.Empty<PrefabBase>(); }
}
namespace RoadBuilder.Prefabs
{
    public class BuilderRoad : RoadPrefab { }
    public class BuilderPath : PathwayPrefab { }
}
namespace CS2Mods.Shared.Discovery
{
    internal class RoadBuilderRoad { internal RoadPrefab Prefab = new(); internal string Name = ""; internal float Width => 0f; }
}
namespace CS2Mods.Shared
{
    internal static class PrefabCatalog { internal static IEnumerable<PrefabBase> GetAll(PrefabSystem system) => system.Items; }
    internal static class NetWidth { internal static float Of(NetGeometryPrefab prefab) => 0f; }
    internal static class ModHost { internal static Log Log = new(); }
    internal class Log { internal void Info(string message) { } }
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
