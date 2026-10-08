namespace UnityEngine
{
    internal static class Application { internal static string persistentDataPath = ""; }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal void Info(string message) { }
        internal void Warn(string message) { }
        internal void Warn(Exception exception, string message) { }
    }
}
namespace BridgeBuilder.Runtime
{
    internal static class BridgeStartupRecovery { internal static HashSet<string> Retired = new(); }
}
namespace Game.Prefabs
{
    public class PrefabBase
    {
        public string name = "";
        private object? data;
        public T? GetComponent<T>() where T : class => data as T;
        public T AddComponent<T>() where T : new() { var value = new T(); data = value; return value; }
        public T AddOrGetComponent<T>() where T : class, new() => GetComponent<T>() ?? AddComponent<T>();
    }
    public class UIObject { public string name = ""; }
}
namespace BridgeBuilder.Bridges
{
    public class BridgeConstructionCost
    {
        public string m_BridgeDisplayName = "", m_BridgeUpperDeckId = "", m_BridgeLowerDeckId = "", m_BridgeStyleId = "", m_BridgeCreatedUtc = "";
        public bool m_BridgeCreationPending;
    }
}

namespace BridgeBuilder.Runtime {
 internal static class BridgeNativePresentation {
   internal static bool Save(BridgeAssetInfo entry, out string error) { error = ""; return true; }
 }
}
