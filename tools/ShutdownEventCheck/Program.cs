using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class GameMonoProbe { public static void Run() {
var args=new[]{Environment.GetEnvironmentVariable("BB_QUIT_UNITY"),Environment.GetEnvironmentVariable("BB_QUIT_HARMONY")};
try {
var entities=Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(args[0])!,"Unity.Entities.dll"));
var world=entities.GetType("Unity.Entities.World",true)!;
var collection=world.GetNestedType("NoAllocReadOnlyCollection`1")!.MakeGenericType(typeof(int));
if(world.GetProperty("All")!.PropertyType.GetGenericTypeDefinition()!=collection.GetGenericTypeDefinition())
 throw new Exception("World.All no longer uses the inspected collection");
var values=Activator.CreateInstance(collection,new List<int>{2,3,5})!;
try { ((IEnumerable<int>)values).GetEnumerator();throw new Exception("Native interface unexpectedly supported"); }
catch(NotSupportedException) { }
using(var enumerator=(IEnumerator<int>)collection.GetMethod("GetEnumerator")!.Invoke(values,null)!) {
 int sum=0;while(enumerator.MoveNext())sum+=enumerator.Current;
 if(sum!=10)throw new Exception("Native typed enumeration failed");
}
Console.WriteLine("PASS actual World.All collection type: interface enumeration rejects; typed GetEnumerator preserves all items.");
if(args.Length!=2)throw new ArgumentException("Expected UnityEngine.CoreModule.dll and 0Harmony.dll paths");
var unity=Assembly.LoadFrom(Path.GetFullPath(args[0]));
var app=unity.GetType("UnityEngine.Application",true)!;
var quitting=app.GetEvent("quitting",BindingFlags.Public|BindingFlags.Static)!;
int calls=0;
Action callback=()=>calls++;
quitting.AddEventHandler(null,callback);
var harmonyAssembly=Assembly.LoadFrom(Path.GetFullPath(args[1]));
var harmonyType=harmonyAssembly.GetType("HarmonyLib.Harmony",true)!;
var harmonyMethod=harmonyAssembly.GetType("HarmonyLib.HarmonyMethod",true)!;
object harmony=Activator.CreateInstance(harmonyType,"BridgeBuilder.ShutdownEventCheck")!;
var target=typeof(Probe).GetMethod(nameof(Probe.Target))!;
var postfix=Activator.CreateInstance(harmonyMethod,typeof(Probe).GetMethod(nameof(Probe.After)))!;
harmonyType.GetMethod("Patch")!.Invoke(harmony,new object?[]{target,null,postfix,null,null});
Probe.Target();
if(Probe.Patches!=1)throw new Exception("Control postfix was not installed");
harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,new object?[]{null});
Probe.Target();
if(Probe.Patches!=1)throw new Exception("Ownerless unpatch did not remove control postfix");
app.GetMethod("Internal_ApplicationQuit",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,null);
if(calls!=1)throw new Exception("Native Unity quit event did not survive global unpatch");
quitting.RemoveEventHandler(null,callback);
app.GetMethod("Internal_ApplicationQuit",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,null);
if(calls!=1)throw new Exception("Event removal was ineffective");
Console.WriteLine("PASS actual Unity Application.quitting delivery survives actual ownerless Harmony.UnpatchAll; control postfix removed, event delivered once, unsubscribe effective.");
Console.WriteLine("Managed native-assembly event test only; no game or Unity Player process was launched and native shutdown ordering was not executed.");
File.WriteAllText(Environment.GetEnvironmentVariable("BB_QUIT_REPORT"),"PASS actual game Mono, native World.All collection enumeration, Unity event and Harmony global unpatch; no game process launched.");
} catch(Exception e){File.WriteAllText(Environment.GetEnvironmentVariable("BB_QUIT_REPORT"),e.ToString());throw;} } }
public static class Probe {
 public static int Patches;
 [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)] public static void Target() { }
 public static void After()=>Patches++;
}
