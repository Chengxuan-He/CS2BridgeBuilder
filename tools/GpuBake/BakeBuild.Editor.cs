using UnityEditor;
using System.IO;
public static class BakeBuild {
 public static void Build() {
  Directory.CreateDirectory("Build");
  var bundle = new AssetBundleBuild { assetBundleName="bridgebake", assetNames=new[]{"Assets/BridgeBake.compute"} };
  if(BuildPipeline.BuildAssetBundles("Build", new[]{bundle}, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64)==null) throw new System.Exception("GPU bundle build failed");
 }
}
