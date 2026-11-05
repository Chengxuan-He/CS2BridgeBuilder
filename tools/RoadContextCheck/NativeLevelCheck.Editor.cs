using System;using System.IO;using System.Reflection;using System.Runtime.Serialization;using UnityEditor;using UnityEngine;
public static class NativeLevelCheck {
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
 public static void Run(){try{
  var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var mod=Assembly.LoadFrom(Arg("-modAssembly"));var context=mod.GetType("BridgeBuilder.Bridges.BridgeRoadContext",true);
  var geometry=game.GetType("Game.Prefabs.NetGeometryData",true);var flagsField=geometry.GetField("m_Flags",I);var limit=geometry.GetField("m_ElevationLimit",I);var gf=flagsField.FieldType;
  var helper=game.GetType("Game.Prefabs.NetCompositionHelpers",true).GetMethod("GetElevationFlags",S);var elevation=helper.GetParameters()[0].ParameterType;var value=elevation.GetField("m_Elevation",I);var float2=value.FieldType;
  object Elev(float f){var v=Activator.CreateInstance(elevation);value.SetValue(v,Activator.CreateInstance(float2,new object[]{f,f}));return v;}
  int cases=0;
  foreach(var names in new[]{"0","RequireElevated","ElevatedIsRaised","RaisedIsElevated","LoweredIsTunnel","RequireElevated, ElevatedIsRaised"})
  foreach(float height in new[]{-100f,-3f,0f,1f,10f,100f}){
   var source=Activator.CreateInstance(geometry);flagsField.SetValue(source,Enum.Parse(gf,names));limit.SetValue(source,1f);
   var selected=context.GetMethod("SelectionGeometry",S).Invoke(null,new[]{source});
   foreach(var f in geometry.GetFields(I))if(f.Name!="m_Flags")Need(Equals(f.GetValue(source),f.GetValue(selected)),"nonlevel geometry metadata changed: "+f.Name);
   Need(flagsField.GetValue(source).Equals(Enum.Parse(gf,names)),"source metadata mutated");
   var elev=Elev(height);var before=value.GetValue(elev);var actual=helper.Invoke(null,new[]{elev,elev,elev,selected});var gen=actual.GetType().GetField("m_General",I).GetValue(actual);
   Need(gen.Equals(Enum.Parse(gen.GetType(),"Elevated")),"native selection did not yield exclusively Elevated for "+names+" height="+height);
   Need(before.Equals(value.GetValue(elev)),"physical elevation mutated");cases++;
  }
  var instance=FormatterServices.GetUninitializedObject(context);var composition=helper.ReturnType;var general=composition.GetField("m_General",I);var original=Activator.CreateInstance(composition);general.SetValue(original,Enum.Parse(general.FieldType,"Tunnel, Node"));
  var locked=context.GetMethod("ApplyLevel",I).Invoke(instance,new[]{original});Need(general.GetValue(locked).Equals(Enum.Parse(general.FieldType,"Elevated, Node")),"context lost native Node flag");
  var elevated=Activator.CreateInstance(composition);general.SetValue(elevated,Enum.Parse(general.FieldType,"Elevated"));context.GetField("Flags",I).SetValue(instance,elevated);
  var requirement=game.GetType("Game.Prefabs.NetPieceRequirements",true);
  Array Req(params string[] names){var list=Array.CreateInstance(requirement,names.Length);for(int j=0;j<names.Length;j++)list.SetValue(Enum.Parse(requirement,names[j]),j);return list;}
  bool Allows(Array all,Array any,Array none)=>(bool)context.GetMethod("AllowsLevel",I).Invoke(instance,new object[]{all,any,none});
  Need(!Allows(Req("Tunnel"),Req(),Req()),"native AllTunnel not rejected");Need(!Allows(Req(),Req(),Req("Elevated")),"native NoneElevated not rejected");
  Need(!Allows(Req(),Req("Tunnel"),Req()),"native AnyTunnel not rejected");Need(Allows(Req(),Req("Tunnel","Node"),Req()),"native local Node discarded");Need(Allows(Req("Node"),Req(),Req("Edge")),"native physical conditions evaluated prematurely");
  var sessionType=mod.GetType("BridgeBuilder.Runtime.BridgePreviewSession",true);var session=Activator.CreateInstance(sessionType,I,null,Array.Empty<object>(),null);
  var rootType=game.GetType("Game.Prefabs.RoadPrefab",true);var root=ScriptableObject.CreateInstance(rootType);var dictionaryType=typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(game.GetType("Game.Prefabs.NetGeometryPrefab",true),context);var map=(System.Collections.IDictionary)Activator.CreateInstance(dictionaryType);map.Add(root,instance);
  sessionType.GetMethod("SetResult",I).Invoke(session,new object[]{root,null,map});map.Clear();Need(ReferenceEquals(sessionType.GetMethod("RoadContext",I).Invoke(session,new object[]{root}),instance),"preview did not retain an independent context snapshot");
  ((IDisposable)session).Dispose();Need(sessionType.GetMethod("RoadContext",I).Invoke(session,new object[]{root})==null,"disposed preview retained context");UnityEngine.Object.DestroyImmediate(root);
  Debug.Log("PASS actual DLL/native requirement level partial evaluation plus preview SetResult snapshot retention and Dispose release; no GameDB render asserted.");
  Debug.Log("PASS actual DLL/native GetElevationFlags "+cases+" combinations: negative/zero/positive physical heights, source flag variations, elevated-only selection, original geometry metadata and elevation retained; ApplyLevel preserves Node. Full private ECS state jobs and placed rendering remain in-game acceptance.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
