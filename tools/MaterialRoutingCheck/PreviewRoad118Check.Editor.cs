using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class PreviewRoad118Check {
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

 // Preseed only the isolated identifier so an Editor-only Burst JIT is unnecessary.
 var isolatedFolder=Path.Combine(root,"BridgeBuilder");Directory.CreateDirectory(isolatedFolder);
 File.WriteAllText(Path.Combine(isolatedFolder,"tmp118 Road Surface Original Base 40dd255c63eb8f65f96c5333d7d28afb.Surface.cid"),"11111111222233334444555555555555");
 var native=Surface(Arg("-sourceSurface"),"40dd255c63eb8f65f96c5333d7d28afb",true);
 var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));
 var roadPath=Arg("-surfaceFile");
 var prepared=Surface(roadPath,File.ReadAllText(roadPath+".cid").Trim(),false);
 Set(owner,"<Asset>k__BackingField",prepared);Set(owner,"<StyleId>k__BackingField","Draw02");
 Set(owner,"_name","tmp118 Road Surface");Set(owner,"_composed",Activator.CreateInstance(owner.GetType().GetField("_composed",F).FieldType));
 Set(owner,"_originalBase",Activator.CreateInstance(owner.GetType().GetField("_originalBase",F).FieldType));
 var preview=New(T("PreviewGeometry"));Set(preview,"_nativeAssets",local);Set(owner,"_preview",preview);
 var pm=descriptor.DeclaringType;pm.GetField("_root",S).SetValue(null,root);
 ((IDictionary)pm.GetField("Surfaces",S).GetValue(null)).Add("Draw02/Road/NativeAsphalt.Surface",prepared);
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var input=T("MovableSurfaceAtlas+Input");
 object Corner(float x,float y,float z,float u,float v,float w)=>New(vertex,new Vector3(x,y,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(u,v,0,w),Color.white);
 var tri=New(triangle,Corner(-7,-.200008392f,-78.75f,.7495117f,.3515625f,2.5f),Corner(-7.00069427f,-.200008392f,-78.74219f,.7495985f,.351806641f,2.50173616f),Corner(-7,-.200008392f,-78.74219f,.7495117f,.351806641f,2.50173616f),0,1,12);
 var level=New(input,true);Set(level,"Faces",Arr(bound,New(bound,tri,0u)));Set(level,"Materials",Arr(D("SurfaceAsset"),native));
 var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
 object[] call={Arr(input,level,level,level),owner,paint,null,null};
 var result=(Array)T("MovableSurfaceAtlas").GetMethod("Compose",S).Invoke(null,call);
 bool baseline=args.Contains("-baseline");object material=null;
 for(int l=0;l<3;l++){
 var face=((Array)result.GetValue(l)).GetValue(0);var slots=(Array)((Array)call[3]).GetValue(l);
 var m=slots.GetValue((int)Get(face,"Material"));if(material!=null)Check(ReferenceEquals(m,material),"duplicated descriptor");material=m;
 Check((int)m.GetType().GetProperty("materialTemplateHash").GetValue(m)==(baseline?17:0),"wrong native template");
 foreach(string field in new[]{"A","B","C","Occurrence","SourceTriangle"})Check(Get(face,field).Equals(Get(tri,field)),"changed "+field);
 }
 if(baseline){Debug.Log("PASS baseline reproduces unskinnable template 17 after Compose");EditorApplication.Exit(0);return;}
 var a=(IDictionary)Get(native,"m_Textures");var b=(IDictionary)Get(material,"m_Textures");foreach(var key in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})Check(ReferenceEquals(a[key],b[key]),"changed PBR "+key);
 Check(Directory.GetFiles(root,"*.Surface",SearchOption.AllDirectories).Length==1 && Directory.GetFiles(root,"*.Texture",SearchOption.AllDirectories).Length==0,"expected one descriptor and zero generated textures");
 var read=T("MovableRoadRoute").GetMethod("Read",S);var identity=read.Invoke(null,new[]{native});Check((bool)Get(identity,"TiledPaving"),"missing exact family");
 a["_WorldspaceNormalMap"]=a["_NormalMap"];Check(!(bool)Get(read.Invoke(null,new[]{native}),"TiledPaving"),"different native family admitted");
 var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));Type G(string name)=>game.GetType("Game.Prefabs."+name,true);
 var fixture=ScriptableObject.CreateInstance(G("StaticObjectPrefab"));fixture.name="StreetlightSingle02";
 var mount=T("MovableRoadObjects").GetMethod("MountBone",S);using((IDisposable)T("GenerationParameters").GetMethod("Begin",S).Invoke(null,null)){
 var family=T("PedestrianDraw01RoadPartitions");var tree=family.GetProperty("Carriageway",S).GetValue(null);
 object Bind(float z)=>mount.Invoke(null,mount.GetParameters().Length==3?new object[]{new Vector3(4,10,z),tree,4}:new object[]{fixture,new Vector3(4,10,z),tree,4});
 if(mount.GetParameters().Length==3)Check(Bind(0)==null,"generic furniture gap not skipped");
 else try{Bind(0);throw new Exception("unclassified gap fixture disappeared");}catch(TargetInvocationException e){Check(e.InnerException is InvalidOperationException,"unexpected gap error");}
 fixture.GetType().GetMethods(F).Single(m=>m.Name=="AddComponent" && m.IsGenericMethod && m.GetParameters().Length==0).MakeGenericMethod(G("StreetLightObject")).Invoke(fixture,null);
 float offset=((float)family.GetProperty("MovingEnd",S).GetValue(null)+(float)family.GetProperty("CentreGapHalf",S).GetValue(null))/2;
 foreach(string name in new[]{"StreetlightSingle01","StreetlightSingle02"}){fixture.name=name;var neg=Bind(-offset);var pos=Bind(offset);Debug.Log("Mounts gap="+Bind(0)+" negative="+neg+" positive="+pos+" z="+offset);Check(Bind(0)==null && ((int)neg==2 && (int)pos==1),"gap/leaf fixture "+name);}
 }
 Debug.Log("PASS actual serialized native TiledRoad descriptor through three controlled Compose LODs, one object Surface, original 3 PBR identities and source UV4/frames/provenance, zero generated textures, changed graph rejected; PedestrianDraw01 actual partition applies the build-specific gap policy to furniture and StreetLightObject, preserves both leaf mounts.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
