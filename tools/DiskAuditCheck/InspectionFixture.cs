namespace UnityEngine { static class Application { internal static string persistentDataPath="unused"; } }
namespace BridgeBuilder.Runtime {
 static class BridgeAssetCatalog { internal static string Root="unused"; }
 static class BridgeInspectionLog { internal static void Error(object value)=>System.Console.WriteLine(value); }
}
