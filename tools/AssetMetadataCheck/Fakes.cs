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
    }
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
 internal static class BridgeDependencyPersistence {
   internal static bool Fail; internal static int Calls, WrittenFiles;
   internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error) {
     return Save(owner, seeds, out count, out error, out _);
   }
   internal static bool Save(string owner, IEnumerable<string> seeds, out int count, out string error, out int writtenFiles) {
     Calls++; count = 2; writtenFiles = WrittenFiles; error = "copy failed"; return !Fail;
   }
 }
}
