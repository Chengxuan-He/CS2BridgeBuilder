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
    public class GameManager { public enum State { WorldReady, Loading } public State state = State.WorldReady; public bool isGameLoading; public Game.GameMode gameMode = Game.GameMode.MainMenu; public static GameManager instance = new(); public Interface? userInterface = new(); }
    public class Interface { public Game.UI.Bindings? appBindings = new(); }
}
namespace UnityEngine { public static class Application { public static string persistentDataPath = ""; } }
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
        public static void ShowMessage(string title, string message) => Messages.Add(message);
        public static void ShowRecoveryMessage(string message) => Messages.Add(message);
    }
    public class Logger {
        public int Errors; public void Info(string message) { }
        public void Warn(Exception exception, string message) { }
        public void Error(string message) => Errors++;
    }
}

namespace Game { [Flags] public enum GameMode { MainMenu=1, Game=2 } }
namespace Unity.Entities { public class World { public static World? DefaultGameObjectInjectionWorld; public T? GetExistingSystemManaged<T>() where T:class => null; } }
namespace Game.Prefabs { public class PrefabSystem {} public class PrefabBase { public string name=""; } }
namespace CS2Mods.Shared.Infrastructure { public static class PrefabCatalog { public static IEnumerable<Game.Prefabs.PrefabBase> GetAll(Game.Prefabs.PrefabSystem s) => Array.Empty<Game.Prefabs.PrefabBase>(); } }
namespace BridgeBuilder.Runtime {
 public static class BridgeAssetLoading { public static bool Ready = true; }
 public class Entry { public string PrefabName=""; }
 public static class BridgeAssetCatalog { public static Entry[] Load()=>Array.Empty<Entry>(); }
 public enum BridgeRuntimeAction { Delete }
 public class BridgeRuntimeRequest { public BridgeRuntimeAction Action; public string PrefabName=""; }
 public static class BridgeRuntimeRequests { public static Queue<BridgeRuntimeRequest> Requests=new(); public static void Enqueue(BridgeRuntimeRequest r,string s)=>Requests.Enqueue(r); }
}
