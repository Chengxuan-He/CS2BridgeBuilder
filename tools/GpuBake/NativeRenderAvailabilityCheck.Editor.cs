using System;
using UnityEngine;
using UnityEditor;
public static class NativeRenderAvailabilityCheck
{
 public static void Run(){try{
 var resources=Resources.Load("AssetDatabase/AssetDatabaseResources");
 var objectShader=Shader.Find("BH/SG_DefaultShader");var networkShader=Shader.Find("BH/NetCompositionMeshLitShader");
 Debug.Log($"NATIVE RENDER AVAILABILITY: AssetDatabaseResources={resources!=null}; actual shader571={objectShader!=null}; actual shader581={networkShader!=null}. No global AssetDatabase constructor, database registration, shader substitute or render request was invoked.");
 EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
