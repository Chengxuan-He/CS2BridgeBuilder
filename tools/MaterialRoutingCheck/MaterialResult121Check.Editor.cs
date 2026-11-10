using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class MaterialResult121Check {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll")); Type D(string n)=>db.GetType("Colossal.IO.AssetDatabase."+n,true);
 var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
 var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);var readHash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
 var hash=core.GetType("Colossal.Hash128",true);object Hash(string s)=>hash.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{s});
 // Read real native serialized descriptors, with identity-only TextureAsset handles.
 // No shader rendering or game database registration is asserted by this fixture.
 object Surface(string path,string cid,bool builtin){
 var o=FormatterServices.GetUninitializedObject(D("SurfaceAsset"));
 o.GetType().GetProperty("id").SetValue(o,New(D("Identifier"),Hash(cid)));
 if(builtin){var generic=D("AssetDatabase`1");var game=D("Game");var fixtureDb=FormatterServices.GetUninitializedObject(generic.MakeGenericType(game));var providerType=D("FileSystemDataSource");var fixtureProvider=New(providerType,"route-fixture",Path.Combine(Path.GetTempPath(),"BridgeBuilder-RouteFixture"),null,0L);Set(fixtureDb,"m_DataSource",fixtureProvider);o.GetType().GetProperty("database").SetValue(o,fixtureDb);}
 using(var r=new BinaryReader(File.OpenRead(path))){o.GetType().GetMethod("ReadHeader",F).Invoke(o,new object[]{r});
 foreach(var pair in new[]{("m_Floats","ReadStringFloatDictionary"),("m_Ints","ReadStringIntDictionary"),("m_Vectors","ReadStringVectorDictionary"),("m_Colors","ReadStringColorDictionary")})Set(o,pair.Item1,binary.GetMethod(pair.Item2).Invoke(null,new object[]{r}));
 Check(r.ReadByte()==255,"native texture map missing");var dict=(IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),D("TextureAsset")));
 for(int n=r.ReadInt32();n>0;n--){string k=r.ReadString();byte present=r.ReadByte();object texture=null;if(present==255){texture=FormatterServices.GetUninitializedObject(D("TextureAsset"));texture.GetType().GetProperty("id").SetValue(texture,New(D("Identifier"),readHash.Invoke(null,new object[]{r})));}else Check(present==0,"texture marker");dict.Add(k,texture);}
 Set(o,"m_Textures",dict);Set(o,"m_Keywords",binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{r}));Check(r.BaseStream.Position==r.BaseStream.Length,"native descriptor trailing data");}
 Set(o,"m_InstanceRefCount",1);return o;}
var descriptor=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
 var root=Arg("-outputRoot");Directory.CreateDirectory(root);var d=Activator.CreateInstance(descriptor,F,null,new object[]{root},null);var dbType=D("AssetDatabase`1").MakeGenericType(descriptor);var local=dbType.GetMethod("GetInstance",S).Invoke(null,new[]{d});
 object global;try{global=D("AssetDatabase").GetProperty("global",S).GetValue(null);}catch(TargetInvocationException){global=D("AssetDatabase").GetField("s_Instance",S).GetValue(null);Check(global!=null,"global bootstrap missing");}var resources=ScriptableObject.CreateInstance(D("AssetDatabaseResources"));var library=Activator.CreateInstance(D("MaterialLibrary"));var descriptions=D("MaterialLibrary").GetField("m_Materials",F);var list=(IList)Activator.CreateInstance(descriptions.FieldType);var descType=D("MaterialLibrary").GetNestedType("MaterialDescription");var md=Activator.CreateInstance(descType);descType.GetField("m_Hash",F).SetValue(md,0);var stacks=descType.GetField("m_Stacks",F);stacks.SetValue(md,Array.CreateInstance(stacks.FieldType.GetElementType(),0));var named=new Material(Shader.Find("Standard"));named.name="Default";descType.GetField("m_Material",F).SetValue(md,named);list.Add(md);descriptions.SetValue(library,list);resources.GetType().GetField("m_MaterialLibrary",F).SetValue(resources,library);D("AssetDatabase").GetField("m_Resources",F).SetValue(global,resources);


 var native=Surface(Arg("-sourceSurface"),"40dd255c63eb8f65f96c5333d7d28afb",true);
 var targetPath=Arg("-surfaceFile");var original=Surface(targetPath,File.ReadAllText(targetPath+".cid").Trim(),false);
 var lane=T("MovableLaneSurface").GetMethod("TryCreate",S);Check(lane!=null,"result API missing");
 var assetType=D("AssetData");object Owned()=>Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(assetType));
 int cases=0;
 foreach(string identity in new[]{"11111111222233334444555555555555","aaaaaaaa222233334444555555555555"}) {
 original.GetType().GetProperty("id").SetValue(original,New(D("Identifier"),Hash(identity)));
 var owned=Owned();object[] call={original,"tmp121","Draw02",null,owned,null,null};
 Check((bool)lane.Invoke(null,call) && ReferenceEquals(original,call[5]) && call[6]==null && ((IList)owned).Count==0,"unknown object identity must reuse complete source");cases++;
 native.GetType().GetProperty("id").SetValue(native,New(D("Identifier"),Hash(identity)));
 call=new object[]{native,"tmp121","Draw02",null,Owned(),null,null};
 Check(!(bool)lane.Invoke(null,call) && call[5]==null && ((string)call[6]).Contains("representation incomplete"),"unknown valid network source was disguised as success or corruption");cases++;
 }
 var pathType=D("AssetDataPath");var pathFactory=pathType.GetMethods(S).Single(m=>m.Name=="Create" && m.GetParameters().Length==3 && m.GetParameters()[0].ParameterType==typeof(string) && m.GetParameters()[1].ParameterType==typeof(string));
 var path=pathFactory.Invoke(null,new object[]{"BridgeBuilder","Malformed121",Enum.Parse(pathFactory.GetParameters()[2].ParameterType,"None")});
 var add=dbType.GetMethods(F).Single(m=>m.Name=="AddAsset" && !m.IsGenericMethod && m.GetParameters().Length==3 && m.GetParameters()[1].ParameterType==typeof(Type));
 var broken=add.Invoke(local,new[]{path,(object)D("SurfaceAsset"),Hash("bbbbbbbb222233334444555555555555")});
 var meta=broken.GetType().GetMethod("GetMeta",F).Invoke(broken,null);var actual=(string)meta.GetType().GetField("path",F)?.GetValue(meta)??(string)meta.GetType().GetProperty("path",F).GetValue(meta);
 Directory.CreateDirectory(Path.GetDirectoryName(actual));File.WriteAllBytes(actual,new byte[]{1});
 object[] failed={broken,"tmp121","Draw02",null,Owned(),null,null};
 Check(!(bool)lane.Invoke(null,failed) && failed[5]==null && ((string)failed[6]).Contains("source-header"),"actual native short stream must return failure");cases++;
 Debug.Log("PASS "+cases+" compiled production material results against native SurfaceAsset: two unknown object identities fully reused with no allocation; two valid network identities explicitly incomplete with zero thrown exceptions; malformed native descriptor read diagnosed. Arbitrary network representation remains unimplemented.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
