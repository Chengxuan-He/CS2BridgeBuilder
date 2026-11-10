using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Source131Check {
 const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];string managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
 Type N(string n)=>native.GetType("Colossal.IO.AssetDatabase."+n,true);Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 string root=Arg("-outputRoot");Directory.CreateDirectory(root);
 var descriptorType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);var descriptor=Activator.CreateInstance(descriptorType,I,null,new object[]{root},null);
 var dbType=N("AssetDatabase`1").MakeGenericType(descriptorType);var db=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{descriptor});
 var pathType=N("AssetDataPath");var create=pathType.GetMethods(S).Single(m=>m.Name=="Create"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].ParameterType==typeof(string));
 var add=dbType.GetMethods(I).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));
 var prefabType=game.GetType("Game.Prefabs.PrefabBase",true);var renderType=game.GetType("Game.Prefabs.RenderPrefab",true);
 var checker=T("MovableNetworkPrefabs");var instance=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(checker);checker.GetField("_owner",I).SetValue(instance,"b11111111-1111-4111-8111-111111111111");var check=checker.GetMethod("CheckSource",I);
 int count=0;
 foreach(string location in new[]{"ImportedData/b22222222-2222-4222-8222-222222222222/Prefabs","ImportedData/b22222222-2222-4222-8222-222222222222/Dependencies","ImportedData/b22222222-2222-4222-8222-222222222222_Dependencies","OtherMod"}){
 var path=create.Invoke(null,new object[]{location,"Source"+count,Enum.Parse(create.GetParameters()[2].ParameterType,"None")});var hash=add.GetParameters()[2].ParameterType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{(100+count).ToString("x32")});var asset=add.Invoke(db,new[]{path,(object)N("PrefabAsset"),hash});
 var prefab=ScriptableObject.CreateInstance(renderType);prefab.name="Source"+count;
 var assetProperty=prefabType.GetProperty("asset",I);if(assetProperty?.GetSetMethod(true)!=null)assetProperty.SetValue(prefab,asset);else {var field=prefabType.GetFields(I).First(f=>f.FieldType==N("PrefabAsset"));field.SetValue(prefab,asset);}
 Need(!(bool)N("AssetData").GetProperty("isBuiltin",I).GetValue(asset),"fixture unexpectedly builtin");
 check.Invoke(instance,new object[]{prefab});
 var section=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(T("MovableSectionObjects"));T("MovableSectionObjects").GetMethod("CheckSource",I).Invoke(section,new object[]{prefab});
 T("MovableStreetLights").GetMethod("CheckSource",S).Invoke(null,new object[]{"b11111111-1111-4111-8111-111111111111",prefab});
 UnityEngine.Object.DestroyImmediate(prefab);count+=3;
 }
 try{check.Invoke(instance,new object[]{null});throw new Exception("null accepted");}catch(TargetInvocationException e){Need(e.InnerException is InvalidOperationException,"wrong null failure");count++;}
 Debug.Log("PASS "+count+" actual compiled native Prefab source checks; other-owner and non-builtin handles accepted, null rejected. No game scene, player assets or geometry changed.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
