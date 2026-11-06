#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class BridgeVt105Editor {
 public static void Run(){
  try {
   string managed=Environment.GetEnvironmentVariable("BB105_MANAGED");
   AppDomain.CurrentDomain.AssemblyResolve+=(o,e)=>{string f=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(f)?Assembly.LoadFrom(f):null;};
   var assembly=Assembly.Load(File.ReadAllBytes(Environment.GetEnvironmentVariable("BB105_PROBE")));
   assembly.GetType("GameMonoProbe",true).GetMethod("Run").Invoke(null,null);EditorApplication.Exit(0);
  } catch(Exception e){Debug.LogError(e);EditorApplication.Exit(1);}
 }
}
#endif
