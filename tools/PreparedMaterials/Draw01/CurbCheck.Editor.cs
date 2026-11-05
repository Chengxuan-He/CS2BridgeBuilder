using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
public static class Draw01CurbCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
 [Serializable] public class Root {public Case[] cases;}
 [Serializable] public class Case {public string style,supported;public int occurrence,sourceTriangle;public Corner[] vertices;}
 [Serializable] public class Corner {public float[] P,N,T,UV,Color;}
 public static void Run(){try {
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
 var original=Surface(Arg("-sourceSurface"),"1d7dad9f64c9fe55a809f1080c39fb92",true);

 var dbType=D("AssetDatabase");var dbObject=FormatterServices.GetUninitializedObject(dbType);Set(dbObject,"m_Databases",Activator.CreateInstance(dbType.GetField("m_Databases",F).FieldType));dbType.GetField("s_Instance",S).SetValue(null,dbObject);
 var catalog=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);catalog.GetMethod("Initialize",S).Invoke(null,new object[]{Arg("-assetRoot")});
 var keys=new[]{"17426aa290b6e326ec517916b4e1db8bb0fefb240e34feae2fb214dfa1ccf1bf","d469e9228140bbb52175544326369c6cacd9a6ab1c5dee3aa490ff4bbc3e25b5"};
 foreach(var k in keys){var path=Path.Combine(Arg("-assetRoot"),"Materials/Draw01/RepeatUnits",k,"Repeat.Surface");((IDictionary)catalog.GetField("Surfaces",S).GetValue(null))["Draw01/RepeatUnits/"+k+"/Repeat.Surface"]=Surface(path,File.ReadAllText(path+".cid").Trim(),false);}
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var patch=T("MovableExistingRoadSurface+Patch");var kind=T("MovableRoadAppearance+Type");
 var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var sectionType=game.GetType("Game.Prefabs.NetSectionPrefab",true);var section=ScriptableObject.CreateInstance(sectionType);section.name="Sidewalk 3.5";
 var pieceType=mod.GetType("BridgeBuilder.Runtime.BridgePreviewComposition+Piece",true);var pc=pieceType.GetConstructors(F).Single();var pv=pc.GetParameters().Select(p=>p.ParameterType.IsValueType?Activator.CreateInstance(p.ParameterType):null).ToArray();pv[2]="fixture";pv[3]=section;var piece=pc.Invoke(pv);
 var road=FormatterServices.GetUninitializedObject(T("MovableClosedRoad+Result"));Set(road,"Surfaces",Arr(D("SurfaceAsset"),original));Set(road,"Occurrences",Arr(pieceType,piece));
 Vector4 V4(float[] v)=>new Vector4(v[0],v[1],v[2],v[3]);
 object V(Corner c)=>New(vertex,new Vector3(c.P[0],c.P[1],c.P[2]),new Vector3(c.N[0],c.N[1],c.N[2]),V4(c.T),V4(c.UV),Color.white);
 var apply=T("Draw01CurbAppearance").GetMethod("Apply",S);var prepared=T("MovableRoadProjection").GetMethod("Prepared",S);int count=0;
 foreach(var c in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-fixtures"))).cases)foreach(int domain in new[]{1,2,4,0}){
 var vs=c.vertices.Select(V).ToArray();var face=New(triangle,vs[0],vs[1],vs[2],0,0,c.sourceTriangle);var faces=Arr(bound,New(bound,face,(uint)domain));var patches=Array.CreateInstance(patch,14);var types=Array.CreateInstance(kind,14);object[] call={road,Pose.identity,faces,patches,types};apply.Invoke(null,call);var output=(Array)call[3];Check(output.Length==(domain==0?14:15),"physical part gate");
 if(domain==0)continue;var actual=output.GetValue(14);var period=domain==2?.1845703125:.280849358974359;var periodZ=domain==2?2.8729578864307217:3.361050328227571;
 foreach(var before in vs){var after=prepared.Invoke(null,new[]{before,actual});foreach(var attr in new[]{"Position","Normal","Color"})Check(Get(before,attr).Equals(Get(after,attr)),"changed "+attr);var uv=(Vector4)Get(after,"UV");var old=(Vector4)Get(before,"UV");var p=(Vector3)Get(before,"Position");double eu=(old.x-.28173828125)*.20001220703125/.0146484375/period;Check(Math.Abs(uv.x-eu)<.0001&&Math.Abs(uv.y+p.z/periodZ)<.0001,"density phase");var t=(Vector4)Get(after,"Tangent");Check(Math.Abs(Vector3.Dot((Vector3)Get(after,"Normal"),(Vector3)t))<.0001,"normal frame");count++;}
 }

 Set(road,"Occurrences",Arr(pieceType,piece,piece));var layoutFaces=new System.Collections.Generic.List<object>();
 foreach(var c in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-fixtures"))).cases){var vs=c.vertices.Select(V).ToArray();layoutFaces.Add(New(triangle,vs[0],vs[1],vs[2],0,c.vertices[0].P[0]<0?0:1,c.sourceTriangle));}
 object Top(float x,float z)=>New(vertex,new Vector3(x,10,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(.4f,.5f,0,0),Color.white);
 layoutFaces.Add(New(triangle,Top(-14,5),Top(-10.6f,5),Top(-14,6),0,0,99999));layoutFaces.Add(New(triangle,Top(10.6f,5),Top(13.5f,5),Top(13.5f,6),0,1,99999));Set(road,"Triangles",Arr(triangle,layoutFaces.ToArray()));
 var layout=New(T("Draw01UndersideLayout"),road,Pose.identity);var spans=(Vector2[])Get(layout,"Spans");Check(spans[0]==new Vector2(-10.6f,10.6f)&&spans[1]==new Vector2(-14,-10.6f)&&spans[2]==new Vector2(10.6f,13.5f),"mixed curb polluted sidewalk layout");
 Debug.Log("PASS Draw01 actual Curb.Apply/Prepared "+count+" vertices; controlled occurrence/domain, geometric normals, synthetic color. No game rendering assertion.");
 File.WriteAllText(Arg("-report"),"PASS "+count+" vertices; actual Curb.Apply/Prepared; controlled occurrence/domain; geometric normals and synthetic color; no full game generation.");
 UnityEngine.Object.DestroyImmediate(section);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
