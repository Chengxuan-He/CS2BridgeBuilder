using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEditor;
public static class NativeSurfacePersistenceCheck {
 const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetProperty(n,I)?.GetValue(x)??x.GetType().GetField(n,I).GetValue(x);
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var n=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));Type T(string s)=>n.GetType("Colossal.IO.AssetDatabase."+s,true);
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));var descriptor=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
 var root=Arg("-outputRoot");Directory.CreateDirectory(root);var d=Activator.CreateInstance(descriptor,I,null,new object[]{root},null);var dbType=T("AssetDatabase`1").MakeGenericType(descriptor);var db=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{d});
 object global;try{global=T("AssetDatabase").GetProperty("global",S).GetValue(null);}catch(TargetInvocationException){global=T("AssetDatabase").GetField("s_Instance",S).GetValue(null);Need(global!=null,"global bootstrap missing");}var resources=ScriptableObject.CreateInstance(T("AssetDatabaseResources"));var library=Activator.CreateInstance(T("MaterialLibrary"));var descriptions=T("MaterialLibrary").GetField("m_Materials",I);var list=(IList)Activator.CreateInstance(descriptions.FieldType);var descType=T("MaterialLibrary").GetNestedType("MaterialDescription");var md=Activator.CreateInstance(descType);descType.GetField("m_Hash",I).SetValue(md,0);var stacks=descType.GetField("m_Stacks",I);stacks.SetValue(md,Array.CreateInstance(stacks.FieldType.GetElementType(),0));list.Add(md);descriptions.SetValue(library,list);resources.GetType().GetField("m_MaterialLibrary",I).SetValue(resources,library);T("AssetDatabase").GetField("m_Resources",I).SetValue(global,resources);
 // Native non-VT writer only needs stack metadata. This does not claim real shader/render setup.
 var pathType=T("AssetDataPath");var create=pathType.GetMethods(S).Single(m=>m.Name=="Create"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].ParameterType==typeof(string));
 object PathFor(string name)=>create.Invoke(null,new object[]{"BridgeBuilder",name,Enum.Parse(create.GetParameters()[2].ParameterType,"None")});
 var surfaceType=T("SurfaceAsset");var add=dbType.GetMethods(I).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));var hashType=add.GetParameters()[2].ParameterType;
 var bytes=File.ReadAllBytes(Arg("-surfaceFile"));var owner="b11111111-2222-4333-8444-555555555555";
 var relocate=mod.GetType("BridgeBuilder.Runtime.BridgePrefabPersistence").GetMethod("RelocateGenerated",S);
 for(int run=0;run<2;run++){
 var name=owner+" Road Surface Original Base "+(run==0?"Blob.cok@Road_0c1d8d6afba9b274ba12424460a2cdcc.Material":"1d7dad9f64c9fe55a809f1080c39fb92");
 var asset=add.Invoke(db,new[]{PathFor(name),(object)surfaceType,hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{run==0?"11111111222233334444555555555555":"11111111222233334444555555555556"})});var meta=asset.GetType().GetMethod("GetMeta",I).Invoke(asset,null);var initial=(string)Get(meta,"path");Directory.CreateDirectory(Path.GetDirectoryName(initial));File.WriteAllBytes(initial,bytes);
 surfaceType.GetMethod("LoadProperties",I).Invoke(asset,new object[]{true});
 var textures=(IDictionary)Get(asset,"textures");var identities=textures.Keys.Cast<object>().ToDictionary(k=>(string)k,k=>Get(textures[k],"id").ToString().Split(' ')[0]);
 var save=surfaceType.GetMethods(I).Single(m=>m.Name=="Save"&&m.GetParameters().Length==8);Need((bool)save.Invoke(asset,new object[]{0,true,false,false,null,null,null,null,null}.Take(save.GetParameters().Length).ToArray()),"save failed");
 Need(File.Exists(initial)&&File.Exists(initial+".cid"),"writer returned without payload/cid");using(var probe=File.Open(initial,FileMode.Open,FileAccess.Read,FileShare.None)){Need(probe.Length>0,"empty payload");}
 surfaceType.GetMethod("UnloadProperties",I).Invoke(asset,new object[]{true});try{relocate.Invoke(null,new object[]{asset,owner,"Appearance"});}catch(TargetInvocationException e) when(run==0 && e.InnerException is FileNotFoundException){Debug.Log("PASS exact production failure reproduced at native SetTimestamp after .Material -> .Surface registration mismatch");continue;}meta=asset.GetType().GetMethod("GetMeta",I).Invoke(asset,null);var final=(string)Get(meta,"path");
 if(run==0){Need(!File.Exists(final),"old mismatch did not reproduce");Debug.Log("PASS original failure reproduced: native SurfaceAsset registered .Surface while moved payload retained .Material");continue;}
 Need(final.EndsWith(".Surface")&&File.Exists(final),"correct Surface missing after relocation");surfaceType.GetMethod("LoadProperties",I).Invoke(asset,new object[]{true});var loaded=(IDictionary)Get(asset,"textures");Need(loaded.Count==identities.Count,"dependency count");foreach(var key in identities.Keys)Need(Get(loaded[key],"id").ToString().Split(' ')[0]==identities[key],"dependency identity changed "+key);
 Need(File.ReadAllBytes(final).SequenceEqual(bytes),"native save/relocation changed descriptor bytes");surfaceType.GetMethod("UnloadProperties",I).Invoke(asset,new object[]{true});
 Debug.Log("PASS native non-VT Save closes real file before unload; production relocation then native LoadProperties preserves "+loaded.Count+" dependencies and descriptor bytes. Actual native filesystem/database; minimal template stack metadata, no rendering asserted.");
 }
 EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
