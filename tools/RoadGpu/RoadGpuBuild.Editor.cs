using System;using System.IO;using UnityEditor;
public static class RoadGpuBuild {
 public static void Build(){
  Directory.CreateDirectory("BuildRoadGpu");
  var b=new AssetBundleBuild{assetBundleName="roadgpu",assetNames=new[]{"Assets/RoadGpu136/RoadClip.compute","Assets/RoadGpu136/RoadEncode.compute"}};
  if(BuildPipeline.BuildAssetBundles("BuildRoadGpu",new[]{b},BuildAssetBundleOptions.ForceRebuildAssetBundle|BuildAssetBundleOptions.StrictMode,BuildTarget.StandaloneWindows64)==null)throw new Exception("Road compute bundle build failed");
  EditorApplication.Exit(0);
 }
}
