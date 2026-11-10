namespace Game.UI.Localization {
    public class LocalizedString { public string Text = ""; public static LocalizedString Value(string text) => new() { Text = text }; }
}
namespace Game.UI {
    public class ConfirmationDialog {
        public ConfirmationDialog(Localization.LocalizedString title, Localization.LocalizedString message,
            Localization.LocalizedString confirm, Localization.LocalizedString cancel, Localization.LocalizedString[] extra) { }
    }
    public class Bindings {
        public int Calls; public bool Fail; public Action<int>? Reply;
        public void ShowConfirmationDialog(ConfirmationDialog dialog, Action<int> reply) {
            if (Fail) throw new IOException("UI fixture failure"); Calls++; Reply = reply;
        }
    }
}
namespace Game.SceneFlow {
    public class GameManager { public event Action<string,string,bool,bool>? onGameSaveLoad; public void Saving(bool start)=>onGameSaveLoad?.Invoke("fixture","",start,true); public enum State { WorldReady, Loading, Quitting, Terminated } public State state = State.WorldReady; public bool isGameLoading; public Game.GameMode gameMode = Game.GameMode.MainMenu; public static GameManager instance = new(); public Interface? userInterface = new(); }
    public class Interface { public Game.UI.Bindings? appBindings = new(); }
}
namespace UnityEngine { public static class Application { public static string persistentDataPath = ""; public static string dataPath=""; public static event Action? quitting; public static int Subscribers=>quitting?.GetInvocationList().Length??0; public static void QuitEvent()=>quitting?.Invoke(); } }
namespace BridgeBuilder.Systems { public static class BridgeStartupAssetSystem { public static bool IsStartupInspection; } }
namespace BridgeBuilder.Settings {
    public static class RuntimeUiText { public static string Get(string key) => key; }
    public class UiStringCatalog { public static UiStringCatalog Current = new(); public string Title = "Bridge Builder"; }
    public static class BridgeRecoveryLocation { public static string Path = ""; }
}
namespace BridgeBuilder.Runtime {
    public static class BridgeSessionState { public static bool Restart; public static void RequireRestart() => Restart = true; }
    public static class BridgeStartupRecovery { public static HashSet<string> Retired = new(); }
}
namespace BridgeBuilder {
    public static class Mod {
        public static Logger Log = new(); public static List<string> Messages = new();
        public static void ShowMessage(string title, string message) => TryShowMessage(title, message);
        public static bool TryShowMessage(string title,string message) {
            if(Game.SceneFlow.GameManager.instance.userInterface?.appBindings==null)return false;
            Messages.Add(message);return true;
        }
        public static void ShowRecoveryMessage(string message) => Messages.Add(message);
    }
    public class Logger {
        public List<string> Lines=new();public int Errors; public void Info(string message) { }
        public void Warn(Exception exception, string message) { }
        public void Error(string message) {Errors++;Lines.Add(message);}
    }
}

namespace Game { [Flags] public enum GameMode { Other=0, MainMenu=1, Game=2, Editor=4 } }
namespace Unity.Entities { public class World { public static Worlds All=new(); public bool IsCreated; public static World? DefaultGameObjectInjectionWorld; public T? GetExistingSystemManaged<T>() where T:class => null; } }
namespace Game.Prefabs { public class PrefabSystem { private readonly Dictionary<object,object> m_UpdateMap=new(); public void Queue(){m_UpdateMap[new object()]=new object();} public void Drain()=>m_UpdateMap.Clear(); } public struct PrefabData {} public class PrefabBase { public string name=""; public Asset? asset; } public class Asset { public string path=""; } }
namespace CS2Mods.Shared.Infrastructure { public static class PrefabCatalog { public static IEnumerable<Game.Prefabs.PrefabBase> GetAll(Game.Prefabs.PrefabSystem s) => Array.Empty<Game.Prefabs.PrefabBase>(); } }
namespace BridgeBuilder.Runtime {

 public static class BridgeAssetLoading { public static bool Ready = true; }
 public class Entry { public string PrefabName=""; }
 public static class BridgeAssetCatalog { public static Entry[] Load()=>Array.Empty<Entry>(); }
 public enum BridgeRuntimeAction { Delete }
 public class BridgeRuntimeRequest { public long RemovalBatch;public BridgeRuntimeAction Action; public string PrefabName=""; }
 public static class BridgeRuntimeRequests { public static void Touch(){} public static Queue<BridgeRuntimeRequest> Requests=new(); public static void Enqueue(BridgeRuntimeRequest r,string s)=>Requests.Enqueue(r); }
}

namespace BridgeBuilder.UI {public static class BridgeBuilderUISystem {public static string[] Residual=Array.Empty<string>();public static string[] SyncVerifiedRemoval(string[] targets)=>Residual.Intersect(targets).ToArray();}}

namespace Colossal.Logging {
 public interface ILog { ILog SetShowsErrorsInUI(bool value); void Error(string message); void Info(string message); }
 public static class LogManager { public static ILog GetLogger(string name)=>new Quiet(); }
 public class Quiet:ILog { public ILog SetShowsErrorsInUI(bool value)=>this; public void Error(string message) {} public void Info(string message) {} }
}

namespace Colossal.IO.AssetDatabase { public class AssetDatabase { public static AssetDatabase global=new(); public static bool exists=true; public List<object> databases=new(){new object()}; public void Dispose(){databases.Clear();exists=false;} } }
namespace Colossal { public class TaskManager { public static TaskManager instance=new(); public QueueState taskQueue=new(); public Dictionary<string,Task> taskRegistry=new(); public Task CompleteAndClear()=>Task.CompletedTask; } public class QueueState { public int count; public Task Completion=Task.CompletedTask; public Task Complete()=>Completion; } }
namespace HarmonyLib { public class Harmony { public string Id; public Harmony(string id){Id=id;} public void Patch(System.Reflection.MethodInfo? m,HarmonyMethod? postfix){} public void UnpatchAll(string id){} } public class HarmonyMethod { public HarmonyMethod(Type t,string n){} } }
namespace Game.Common { public struct Created{} public struct Updated{} }
namespace Unity.Entities {
 public class EntityManager {
  public bool Created,Updated; public int Queries;
  public EntityQuery CreateEntityQuery(EntityQueryDesc d){Queries++;if(d.All.Single().Type!=typeof(Game.Prefabs.PrefabData)||!d.Any.Any(t=>t.Type==typeof(Game.Common.Created))||!d.Any.Any(t=>t.Type==typeof(Game.Common.Updated)))throw new Exception("Unexpected lifecycle query");return new EntityQuery{IsEmptyIgnoreFilter=!Created&&!Updated};}
 }
 public class EntityQuery:IDisposable { public bool IsEmptyIgnoreFilter; public void Dispose(){} }
 public class EntityQueryDesc { public ComponentType[] All=Array.Empty<ComponentType>(),Any=Array.Empty<ComponentType>(); }
 public struct ComponentType { public Type Type;public static ComponentType ReadOnly<T>()=>new(){Type=typeof(T)}; }
}
namespace Unity.Entities {
 public readonly struct Worlds : IEnumerable<World> {
  private static readonly List<World> Items=new();
  public List<World>.Enumerator GetEnumerator()=>Items.GetEnumerator();
  IEnumerator<World> IEnumerable<World>.GetEnumerator()=>throw new NotSupportedException("Native collection forbids interface enumeration");
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>throw new NotSupportedException("Native collection forbids interface enumeration");
  public void Add(World world)=>Items.Add(world);
  public void Clear()=>Items.Clear();
 }
}
