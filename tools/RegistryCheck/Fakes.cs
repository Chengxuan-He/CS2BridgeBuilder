namespace UnityEngine
{
    internal static class Application { internal static string persistentDataPath = ""; }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal void Warn(string message) { }
        internal void Warn(Exception exception, string message) { }
    }
}
