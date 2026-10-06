// Narrow game/disk adapters. The lifecycle under test is the actual production system.
namespace Colossal.Serialization.Entities { public enum Purpose { Cleanup } }
namespace Game
{
    public enum GameMode { Other = 0, MainMenu = 1, Game = 2, Editor = 4 }
    public class GameSystemBase
    {
        public bool Enabled;
        protected readonly FakeWorld World = new();
        protected readonly FakeManager EntityManager = new();
        protected virtual void OnCreate() { }
        protected virtual void OnGamePreload(Colossal.Serialization.Entities.Purpose p, GameMode m) { }
        protected virtual void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose p, GameMode m) { }
        protected virtual void OnUpdate() { }
    }
    public class FakeWorld { public T GetOrCreateSystemManaged<T>() where T : new() => new(); }
    public class FakeManager { public void CompleteAllTrackedJobs() { } }
}
namespace Game.SceneFlow
{
    public class GameManager
    {
        public static GameManager instance = new();
        public Game.GameMode gameMode;
        public bool isGameLoading; public enum State { Booting, WorldReady } public State state;
        public FakeUI userInterface = new();
        public FakeConfig configuration = new();
        public FakeModManager modManager = new();
        private System.Threading.Tasks.Task m_CachingPdxDatabaseTask = System.Threading.Tasks.Task.CompletedTask;
        public void SetCachingTask(System.Threading.Tasks.Task task) => m_CachingPdxDatabaseTask = task;
    }
    public class FakeConfig { public bool disablePDXSDK, disableModding; }
    public class FakeModManager { public bool isInitialized = true; }
    public class FakeUI { public object appBindings = new(); }
}
namespace Game.Prefabs
{
    public class PrefabBase
    {
        public string name = "";
        public bool isBuiltin, isReadOnly; public bool Registered = true, Available = true;
        public Asset? asset;
        public T? GetComponent<T>() where T : class => null;
    }
    public class Asset { public string path = ""; }
    public class NetGeometryPrefab : PrefabBase { }
    public class Unlockable { public bool active; }
    public class PrefabSystem
    {
        public static List<PrefabBase> Loaded = new();
        public bool TryGetEntity(PrefabBase p, out int e) { e = 0; return p.Registered; }
        public bool IsAvailable(PrefabBase p) => p.Available;
        public bool AddPrefab(PrefabBase p) => true;
    }
}
namespace CS2Mods.Shared.Infrastructure
{
    public static class PrefabCatalog
    { public static IEnumerable<Game.Prefabs.PrefabBase> GetAll(Game.Prefabs.PrefabSystem p) => Game.Prefabs.PrefabSystem.Loaded; }
}
namespace UnityEngine { public static class Application { public static string persistentDataPath = "unused"; } }
namespace Colossal.IO.AssetDatabase
{
    public class PrefabAsset { public string path = ""; public AssetId id = new(); public Game.Prefabs.PrefabBase? Instance;
        public T? GetInstance<T>() where T : class => Instance as T; }
    public class AssetId { public string guid = "01234567890123456789012345678901"; }
    public class ParadoxMods { }
    public class AssetDatabase<T> { public static AssetDatabase<T> instance = new(); public object dataSource = new ParadoxModsDataSource(); }
    public class FileSystemDataSource { public bool Cached; protected bool IsDataCached(bool priorityData) => Cached; }
    public class ParadoxModsDataSource : FileSystemDataSource
    {
        public event Func<IReadOnlyCollection<Colossal.Hash128>, bool, System.Threading.Tasks.Task>? onEntryIsInActivePlaysetChanged;
        public void Begin() => onEntryIsInActivePlaysetChanged?.Invoke(Array.Empty<Colossal.Hash128>(), true);
        public event Action? onAfterActivePlaysetOrModStatusChanged;
        public void Complete() => onAfterActivePlaysetOrModStatusChanged?.Invoke();
    }
    public class GeometryAsset { public object database = new(); public string path = ""; }
    public class AssetDatabase
    {
        public static AssetDatabase global = new(), user = new();
        public List<PrefabAsset> Assets = new();
        public IEnumerable<T> GetAssets<T>() => Assets.Cast<T>();
        public bool TryGetAsset<T>(string cid, out GeometryAsset g) { g = new(); return true; }
    }
}
namespace BridgeBuilder
{
    public static class Mod
    {
        public static List<string> Messages = new(); public static Logger Log = new();
        public static void ShowMessage(string title, string message) => Messages.Add(message);
        public static void ShowRecoveryMessage(string message) => Messages.Add(message);
    }
    public class Logger { public void Info(string s) { } public void Warn(string s) { } public void Warn(Exception e, string s) { } }
}
namespace BridgeBuilder.Settings
{
    public static class RuntimeUiText { public static string Get(string k, params object[] a) => k; }
    public class UiStringCatalog { public static UiStringCatalog Current = new(); public string Title = "Bridge Builder"; }
    public static class BridgeRecoveryLocation { public static string Path => "backup"; }
}
namespace BridgeBuilder.Runtime
{
    public static class BridgeSessionState { public static bool TryOwner(string n, out string owner) { owner = n; return n == "bridge"; } }
    public static class BridgeStartupRecovery { public static HashSet<string> Retired = new(); }
    public static class BridgeAssetCatalog
    { public static string Root => System.IO.Path.GetFullPath("unused/ImportedData");  }
    public static class BridgeNetworkValidation
    {
        public static int Calls;
        public static bool Invalid, Raise;
        public static bool IsInvalid(Game.Prefabs.PrefabBase p, out string r)
        { Calls++; if (Raise) throw new InvalidOperationException("fixture"); r = "null required reference"; return Invalid; }
    }
    public static class BridgeFileAccess { public static string Logical(string p) => p; }
    public static class BridgeAssetInfo { public static bool IsPrefabName(string? n) => n?.StartsWith("b") == true; }
    public class BridgeDiskAudit
    {
        public static int Calls; public static bool Incomplete, Raise;
        public bool RetireFiles(ISet<string> owners, string backup, out string error) { error = ""; return true; }
        public bool Complete => !Incomplete; public string Error => "fixture failure";
        public Dictionary<string, string> Failures = new();
        public static BridgeDiskAudit ForMemoryFailures(string p, IDictionary<string,string> failures)
        { Calls++; if (Raise) throw new InvalidOperationException("fixture"); return new BridgeDiskAudit { Failures = new(failures) }; }

    }
}
namespace BridgeBuilder.Runtime
{
    public static class BridgeAssetMigration {
        public static int Calls; public static bool Fail, Changed;
        public static bool Run(string owner, IEnumerable<string> seeds, string backup, out bool changed, out string error) {
            Calls++; changed = Changed; error = "fixture"; return !Fail;
        }
    }
}

namespace Colossal { public class Hash128 { } }
namespace BridgeBuilder.Runtime {
 public static class BridgeLoadedCidRecovery {
  public enum Result { Recoverable, Broken, Incomplete }
  public static Result Outcome;
  public sealed class LoadedIndex { }
  public static Result Inspect(Game.Prefabs.PrefabBase p, out HashSet<string> copies, out string reason, LoadedIndex? lookup = null) {
   copies = new() { "01234567890123456789012345678901" }; reason = "test CID"; return Outcome;
  }
 }
 public static class BridgeDependencyPersistence {
  public static int Calls; public static bool Fail;
  public static bool Save(string owner,IEnumerable<string> seeds,out int count,out string error) { Calls++;count=1;error="copy failed";return !Fail; }
 }
}
