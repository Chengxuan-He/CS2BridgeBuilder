using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
public static class Draw01CandidateFarCheck
{
 const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object F(object o,string n)=>o.GetType().GetField(n,Fields).GetValue(o);
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,a,null);
 static string Arg(string key){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,key)+1];}
 public static void Run(){try{
  string dll=Arg("-candidate"),managed=Arg("-gameManaged");using var sha=SHA256.Create();string digest=string.Concat(sha.ComputeHash(File.ReadAllBytes(dll)).Select(b=>b.ToString("X2")));
  if(digest!="31EA53D5347CFE9A2DF5D9607395425A4BA1DDD18C10746E028527A811889253")throw new Exception("Candidate hash differs.");
  AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=>{string name=new AssemblyName(e.Name).Name+".dll";foreach(var dir in new[]{Path.GetDirectoryName(dll),managed}){string path=Path.Combine(dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;};
  var assembly=Assembly.LoadFrom(dll);var geometry=assembly.GetType("BridgeBuilder.Bridges.MovableRoadGeometry",true);var vertex=geometry.GetNestedType("Vertex",Fields);var triangle=geometry.GetNestedType("Triangle",Fields);var partition=geometry.GetNestedType("Partition",Fields);
  var roadType=assembly.GetType("BridgeBuilder.Bridges.MovableClosedRoad",true).GetNestedType("Result",Fields);var piece=roadType.GetField("Occurrences",Fields).FieldType.GetElementType();var sectionType=piece.GetProperty("RootSection",Fields).PropertyType;var surfaceType=roadType.GetField("Surfaces",Fields).FieldType.GetElementType();
  var source=FormatterServices.GetUninitializedObject(surfaceType);var surfaces=Array.CreateInstance(surfaceType,1);surfaces.SetValue(source,0);
  var create=assembly.GetType("BridgeBuilder.Bridges.Draw01FarAppearance",true).GetMethod("Create",Fields);
  var motion=assembly.GetType("BridgeBuilder.Bridges.Draw01RoadPartitions",true);var roadSurface=motion.GetMethod("RoadSurface",Fields);var carriageway=motion.GetField("Carriageway",Fields).GetValue(null);var gap=partition.GetField("Gap",Fields).GetValue(null);
  var inFrame=geometry.GetMethods(Fields).Single(m=>m.Name=="InFrame"&&m.ReturnType==triangle.MakeArrayType());var bind=geometry.GetMethod("TryBind",Fields);
  Array Frame(Array t,Pose p)=>(Array)inFrame.Invoke(null,new object[]{t,p});
  Array Bind(Array t,object p,int count,Array occurrences){var a=new object[]{t,p,count,1,null,null,false,occurrences,null};if(!(bool)bind.Invoke(null,a))throw new Exception((string)a[5]);return (Array)a[4];}
  Array AsArray(Type type,IEnumerable<object> items){var a=items.ToArray();var result=Array.CreateInstance(type,a.Length);for(int i=0;i<a.Length;i++)result.SetValue(a[i],i);return result;}
  int checks=0,scenarios=0;
  foreach(float width in new[]{1f,1.7f})foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(2,3,-4),Quaternion.Euler(0,37,0))}){
   var sectionNames=new[]{"Carriageway","Sidewalk","Sidewalk"};var sections=sectionNames.Select(n=>{var s=ScriptableObject.CreateInstance(sectionType);s.name=n;return s;}).ToArray();
   try{
    var pieces=Array.CreateInstance(piece,4);var ctor=piece.GetConstructors(Fields).Single();var compositionType=ctor.GetParameters()[1].ParameterType;
    for(int i=0;i<4;i++)pieces.SetValue(New(piece,null,Activator.CreateInstance(compositionType),"fixture",sections[i==3?0:i]),i);
    var faces=new List<object>();object V(float x,float z)=>New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,0,0),Color.white);
    void Quad(float l,float r,int occurrence,float zmin=-90.099609375f,float zmax=90.099609375f){var a=V(l,zmin);var b=V(r,zmin);var c=V(r,zmax);var d=V(l,zmax);faces.Add(New(triangle,a,c,b,0,occurrence,0));faces.Add(New(triangle,a,d,c,0,occurrence,1));}
    Quad(-3.4000244140625f*width,3.4000244140625f*width,0);Quad(-6.5069580078125f*width,-3.4000244140625f*width,1);Quad(3.4000244140625f*width,6.5069580078125f*width,2);Quad(-3.4000244140625f*width,3.4000244140625f*width,3,83.740234375f,86f);
    var rotation=Quaternion.Inverse(pose.rotation);var inverse=new Pose(rotation*-pose.position,rotation);var selected=Frame(AsArray(triangle,faces),inverse);var road=FormatterServices.GetUninitializedObject(roadType);roadType.GetField("Triangles",Fields).SetValue(road,selected);roadType.GetField("Surfaces",Fields).SetValue(road,surfaces);roadType.GetField("Occurrences",Fields).SetValue(road,pieces);
    var mapped=create.Invoke(null,new[]{road,(object)pose,source});var patches=(Array)F(mapped,"Patches");var types=(Array)F(mapped,"SupportedTypes");var domains=(Array)F(mapped,"Occurrences");
    var physical=AsArray(partition,Enumerable.Range(0,4).Select(i=>roadSurface.Invoke(null,new object[]{i==1||i==2})));var receivers=Bind(Frame(selected,pose),carriageway,3,physical);var actual=Bind(Frame(AsArray(triangle,receivers.Cast<object>().Select(f=>F(f,"Geometry"))),inverse),gap,patches.Length,domains);
    if(actual.Length==0)throw new Exception("Empty output.");
    foreach(var face in actual){var t=F(face,"Geometry");int occurrence=(int)F(t,"Occurrence");var p=patches.GetValue((uint)F(face,"Bone"));int supported=Convert.ToInt32(types.GetValue((uint)F(face,"Bone")));if(!(bool)F(p,"SourceUv")||(bool)F(p,"Repeat")||!ReferenceEquals(F(p,"Surface"),source)||supported!=((occurrence==0||occurrence==3)?1:2))throw new Exception("Native binding/type differs.");
     var min=(Vector2)F(p,"Min");var max=(Vector2)F(p,"Max");var u=(Vector4)F(p,"U");var v=(Vector4)F(p,"V");
     if(occurrence==3&&!actual.Cast<object>().Where(f=>(int)F(F(f,"Geometry"),"Occurrence")==0).Any(f=>{var q=patches.GetValue((uint)F(f,"Bone"));return min.Equals(F(q,"Min"))&&max.Equals(F(q,"Max"))&&u.Equals(F(q,"U"))&&v.Equals(F(q,"V"));}))throw new Exception("Piece occurrence resets finite mapping.");
     foreach(string n in new[]{"A","B","C"}){var pos=(Vector3)F(F(t,n),"Position");var q=new Vector4(pos.x,pos.y,pos.z,1);var uv=new Vector2(Vector4.Dot(u,q),Vector4.Dot(v,q));for(int axis=0;axis<2;axis++){float margin=Mathf.Pow(2,Mathf.Floor(Mathf.Log(Mathf.Max(Mathf.Abs(min[axis]),Mathf.Abs(max[axis])),2))-11);if(!float.IsFinite(uv[axis])||uv[axis]<min[axis]-margin||uv[axis]>max[axis]+margin)throw new Exception("Finite projection escaped source chart.");}checks++;}
    }
    scenarios++;
   }finally{foreach(var s in sections)UnityEngine.Object.DestroyImmediate(s);}
  }
  File.WriteAllText(Arg("-report"),"{\"status\":\"PASS\",\"candidateSha256\":\""+digest+"\",\"scenarios\":"+scenarios+",\"vertexChecks\":"+checks+",\"actualCandidateCreateAndTryBind\":true,\"embeddedMeasurements\":true,\"nativeRenderingVerified\":false}");Debug.Log("PASS actual candidate Draw01 far: "+checks+" vertices.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
