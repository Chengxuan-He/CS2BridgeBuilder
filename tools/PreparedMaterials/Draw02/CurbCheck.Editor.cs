using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.Rendering;
public static class Draw02CurbCheck {
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr LoadLibraryW(string path);
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static Array Arr(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
 [Serializable] public class Root {public Case[] cases;}
 [Serializable] public class Case {public int lod,sourceTriangle;public bool moving;public uint[] normalBits,tangentBits;public Corner[] vertices;}
 [Serializable] public class Corner {public float[] P,N,T,UV,Color;}
 public static void Run(){try {
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");Check(LoadLibraryW(Path.Combine(Path.GetDirectoryName(managed),"Plugins/x86_64/PipelinePlugin.dll"))!=IntPtr.Zero,"native decoder load");
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
 var keys=new[]{"MovingWalkEdge","FixedWalkEdge"};
 foreach(var k in keys){var path=Path.Combine(Arg("-assetRoot"),"Materials/Draw02/RepeatUnits",k,"Repeat.Surface");((IDictionary)catalog.GetField("Surfaces",S).GetValue(null))["Draw02/RepeatUnits/"+k+"/Repeat.Surface"]=Surface(path,File.ReadAllText(path+".cid").Trim(),false);}
 var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var patch=T("MovableExistingRoadSurface+Patch");var kind=T("MovableRoadAppearance+Type");
 var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var sectionType=game.GetType("Game.Prefabs.NetSectionPrefab",true);var section=ScriptableObject.CreateInstance(sectionType);section.name="Sidewalk 3.5";
 var pieceType=mod.GetType("BridgeBuilder.Runtime.BridgePreviewComposition+Piece",true);var pc=pieceType.GetConstructors(F).Single();var pv=pc.GetParameters().Select(p=>p.ParameterType.IsValueType?Activator.CreateInstance(p.ParameterType):null).ToArray();pv[2]="fixture";pv[3]=section;var piece=pc.Invoke(pv);
 var road=FormatterServices.GetUninitializedObject(T("MovableClosedRoad+Result"));Set(road,"Surfaces",Arr(D("SurfaceAsset"),original));Set(road,"Occurrences",Arr(pieceType,piece));
 Vector4 V4(float[] v)=>new Vector4(v[0],v[1],v[2],v[3]);
 Vector4[] Unpack(uint[] bits,string type,VertexAttributeFormat format,int dimension){
 var meshType=game.GetType("Game.Prefabs."+type,true);var arrayType=typeof(NativeArray<>).MakeGenericType(meshType);
 object values=Activator.CreateInstance(arrayType,new object[]{bits.Length,Allocator.Temp,NativeArrayOptions.ClearMemory});
 var bytes=new byte[bits.Length*4];Buffer.BlockCopy(bits,0,bytes,0,bytes.Length);using var input=new NativeArray<byte>(bytes,Allocator.Temp);
 try{meshType.GetMethods(S).Single(m=>m.Name=="Unpack"&&m.GetParameters().Length==5&&m.GetParameters()[1].ParameterType==arrayType).Invoke(null,new object[]{new NativeSlice<byte>(input),values,bits.Length,format,dimension});
 var result=new Vector4[bits.Length];for(int i=0;i<bits.Length;i++){var element=arrayType.GetProperty("Item").GetValue(values,new object[]{i});var v=Get(element,type=="MeshNormal"?"m_Normal":"m_Tangent");float Axis(string n)=>(float)v.GetType().GetField(n).GetValue(v);result[i]=new Vector4(Axis("x"),Axis("y"),Axis("z"),type=="MeshNormal"?0:Axis("w"));}return result;
 }finally{((IDisposable)values).Dispose();}}
 var apply=T("Draw02CurbAppearance").GetMethod("TryApply",S);var prepared=T("MovableRoadProjection").GetMethod("Prepared",S);int count=0,negative=0;uint movingFlag=(uint)T("MovableSurfaceAtlas").GetField("MovingSurface",S).GetValue(null);
 foreach(var c in JsonUtility.FromJson<Root>(File.ReadAllText(Arg("-fixtures"))).cases){
 var ns=Unpack(c.normalBits,"MeshNormal",VertexAttributeFormat.SNorm16,2);var ts=Unpack(c.tangentBits,"MeshTangent",VertexAttributeFormat.Float32,1);
 var vs=c.vertices.Select((p,i)=>New(vertex,new Vector3(p.P[0],p.P[1],p.P[2]),(Vector3)ns[i],ts[i],V4(p.UV),Color.white)).ToArray();
 for(int scenario=0;scenario<6;scenario++){
 section.name=scenario==1?"Highway Shoulder 2":scenario==4?"Median 2":"Sidewalk With Parking 6";
 pv[3]=scenario==3?null:section;Set(road,"Occurrences",Arr(pieceType,pc.Invoke(pv)));
 uint bone=c.moving?movingFlag:0u;var selected=scenario==5?vs.Select(o=>New(vertex,Get(o,"Position"),Get(o,"Normal"),Get(o,"Tangent"),new Vector4(.4f,.5f,0,0),Get(o,"Color"))).ToArray():vs;var face=New(triangle,selected[0],selected[1],selected[2],0,0,c.sourceTriangle);var faces=Arr(bound,New(bound,face,bone));var patches=Array.CreateInstance(patch,1);var types=Arr(kind,Enum.ToObject(kind,scenario==2?0:2));object[] call={Pose.identity,c.lod,road,faces,patches,types,null};Check((bool)apply.Invoke(null,call),"curb result failure: "+call[6]);var output=(Array)call[4];
 if(output.Length!=(scenario==0?2:1)){object[] routeCall={original,null,null};Check((bool)T("MovableRoadRoute").GetMethod("TryRead",S).Invoke(null,routeCall),"route read failure");var route=routeCall[1];var receiver=route.GetType().GetMethod("Receiver",F).Invoke(route,new[]{face});throw new Exception($"physical/source/provenance gate lod={c.lod} face={c.sourceTriangle} scenario={scenario} output={output.Length} receiver={receiver} section={section.name} Z={c.vertices[0].P[2]}");}var assigned=((Array)call[3]).GetValue(0);Check((((uint)Get(assigned,"Bone"))&movingFlag)==bone,"motion flag changed");Check(Get(assigned,"Geometry").Equals(face),"receiver geometry changed");
 if(scenario!=0){negative++;continue;}var actual=output.GetValue(1);double period=c.moving?.14990234375:.199951171875;
 foreach(var before in vs){var after=prepared.Invoke(null,new[]{before,actual});foreach(var attr in new[]{"Position","Normal","Color"})Check(Get(before,attr).Equals(Get(after,attr)),"changed "+attr);var uv=(Vector4)Get(after,"UV");var old=(Vector4)Get(before,"UV");var p=(Vector3)Get(before,"Position");double eu=(old.x-.28173828125)*.20001220703125/.0146484375/period,ev=-Math.Abs(p.z)/4+(c.moving?0:.5);Check(Math.Abs(uv.x-eu)<.0001&&Math.Abs(uv.y-ev)<.0001,"density phase");Check(uv.z==old.z&&uv.w==old.w,"UV zw changed");var tangent=(Vector4)Get(after,"Tangent");Check(Math.Abs(Vector3.Dot((Vector3)Get(after,"Normal"),(Vector3)tangent))<.0001,"normal frame");count++;}
 }}
 string report="PASS Draw02 actual Curb.Apply/Prepared: "+count+" vertices; "+negative+" shoulder/unmatched/null-provenance/median/non-curb-source negative cases. All three actual owner LODs, native decoded packed N/T, controlled occurrence/domain and synthetic white color; no game rendering assertion.";
 Debug.Log(report);File.WriteAllText(Arg("-report"),report);
 UnityEngine.Object.DestroyImmediate(section);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
