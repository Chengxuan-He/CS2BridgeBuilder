using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class GenerationWork156
{
 static Type gpu; static object current; static int phase; static double started;
 public static void Run()
 {
  try {
   var args=Environment.GetCommandLineArgs();
   string Arg(string key)=>args[Array.IndexOf(args,key)+1];
   var a=Assembly.LoadFile(Arg("-checkAssembly"));
   var result=a.GetType("GenerationWorkCheck").GetMethod("Run").Invoke(null,null);
   Debug.Log(result);File.WriteAllText(Arg("-report"),result.ToString());
   gpu=a.GetType("GenerationGpuCheck"); current=Activator.CreateInstance(gpu,new object[]{false});
   started=EditorApplication.timeSinceStartup;EditorApplication.update+=Update;
  } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
 static void Update()
 {
  try {
   if(EditorApplication.timeSinceStartup-started>60)throw new Exception("GPU test timeout");
   if(!(bool)gpu.GetMethod("Tick").Invoke(current,null))return;
   if(phase++==0){current=Activator.CreateInstance(gpu,new object[]{true});return;}
   Debug.Log("PASS actual Unity Mono scheduling and GPU asynchronous readback/cancel");EditorApplication.update-=Update;EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.update-=Update;EditorApplication.Exit(1);}
 }
}
