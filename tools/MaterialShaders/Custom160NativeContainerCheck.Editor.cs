using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class Custom160NativeContainerCheck
{
 public static void Run()
 {
  try
  {
   var bytes=File.ReadAllBytes("E:/CS2BridgeBuilder/work/custom160/native-layered-all-dots.bundle");
   var bundle=AssetBundle.LoadFromMemory(bytes);if(bundle==null)throw new Exception("generated native bundle could not load");
   var shaders=bundle.LoadAllAssets<Shader>();if(shaders.Length!=1)throw new Exception("shader count "+shaders.Length);
   var shader=shaders[0];var material=new Material(shader);int pass=material.FindPass("GBuffer");
   if(pass<0||!shader.isSupported)throw new Exception("native generated shader unsupported");
   material.EnableKeyword("DOTS_INSTANCING_ON");material.EnableKeyword("DECALS_4RT");material.EnableKeyword("PROBE_VOLUMES_OFF");
   foreach(bool dots in new[]{true,false})
   {
    if(dots)material.EnableKeyword("DOTS_INSTANCING_ON");else material.DisableKeyword("DOTS_INSTANCING_ON");
    foreach(var name in new[]{"ShadowCaster","MotionVectors","FullScreenDebug","DepthOnly","GBuffer","Forward"})
    {
     int index=material.FindPass(name);
     if(index<0||!material.SetPass(index))throw new Exception("generated native pass did not bind: "+name+" dots="+dots);
    }
   }
   var messages=ShaderUtil.GetShaderMessages(shader);foreach(var message in messages)if(message.severity.ToString()=="Error")throw new Exception(message.message);
   var text="PASS generated native Shader container loaded; shader="+shader.name+" passes="+material.passCount+"; all six DOTS and non-DOTS passes bound; compiled native pixel programs retained; actual scene rendering not tested";
   File.WriteAllText("E:/CS2BridgeBuilder/work/custom160/native-container-result.txt",text);Debug.Log(text);
   UnityEngine.Object.DestroyImmediate(material);bundle.Unload(true);EditorApplication.Exit(0);
  }
  catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
