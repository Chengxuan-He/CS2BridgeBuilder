namespace Game.Prefabs {
 public class AssetPackPrefab { public Asset asset=new(); public bool isReadOnly; }
 public class Asset { public string path=""; public (string guid,int unused) id; }
}
namespace UnityEngine { public static class Application { public static string persistentDataPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BBPackIcon-"+Guid.NewGuid().ToString("N")); } }
namespace BridgeBuilder.Settings { internal static class BridgeRecoveryLocation { public static string Path=UnityEngine.Application.persistentDataPath+"-backup"; } }
namespace BridgeBuilder.Runtime {
 internal static class BridgeAssetPack { internal const string PrefabName="BridgeBuilder Native Asset Pack"; }
 internal static class BridgeAssetInfo { internal static bool TryFileOwner(string path,out string owner){var m=System.Text.RegularExpressions.Regex.Match(path,@"b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}");owner=m.Value;return m.Success;} }
}
