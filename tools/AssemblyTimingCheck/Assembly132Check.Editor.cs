using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Assembly132Check
{
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));var t=mod.GetType("BridgeBuilder.Bridges.MovableAssemblyTiming",true);
 const BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 foreach(bool success in new[]{true,false}){
 var x=Activator.CreateInstance(t,true);t.GetMethod("Stage",flags).Invoke(x,new object[]{"laneBind",0});
 t.GetMethod("Count",flags).Invoke(x,new object[]{"laneInputTriangles",42L});
 t.GetMethod("Stage",flags).Invoke(x,new object[]{"dispose",-1});
 t.GetMethod("Finish",flags).Invoke(x,new object[]{"isolated132","Draw01",success});
 if(!(bool)t.GetField("_finished",flags).GetValue(x))throw new Exception("not finished");
 }
 var allocation=(long)t.GetMethod("Allocated",flags).Invoke(null,null);
 Debug.Log("PASS actual compiled assembly timing; allocationCounter="+allocation+"; dll="+mod.Location+"; no scene or player assets touched");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
