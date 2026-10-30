using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
public static class Lift01OriginalAtlasWrapCheck
{
 [Serializable] public sealed class Row {public int lod,bone,internalSeams;public Vector3 a,b,c;}
 [Serializable] public sealed class Fixture {public Row[] rows;}
 [Serializable] public sealed class Report {public int cases,outputTriangles;public float maxRelativeAreaError,maxProjectionError;public bool roadPreparationInvokedByHarness;public string assemblyPath;}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string root=args[Array.IndexOf(args,"-domainRoot")+1],dll=args[Array.IndexOf(args,"-candidate")+1],game=args[Array.IndexOf(args,"-gameData")+1];
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(game,"Managed",new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var assembly=Assembly.LoadFrom(dll);const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  object F(object o,string n)=>o.GetType().GetField(n,flags).GetValue(o);
  var create=assembly.GetType("BridgeBuilder.Bridges.Lift01HalfFloorAppearance",true).GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic);
  var apply=assembly.GetType("BridgeBuilder.Bridges.MovableRoadAtlasWrap",true).GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic);
  var vertex=assembly.GetType("BridgeBuilder.Bridges.MovableRoadGeometry+Vertex",true).GetConstructors(flags).Single();
  var triangle=assembly.GetType("BridgeBuilder.Bridges.MovableRoadGeometry+Triangle",true).GetConstructors(flags).Single();
  var native=FormatterServices.GetUninitializedObject(create.GetParameters()[1].ParameterType);
  var physical=assembly.GetType("BridgeBuilder.Bridges.Lift01RoadPartitions",true).GetField("Carriageway",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  int Region(object node,Vector3 p) {while(F(node,"Negative")!=null&&F(node,"Positive")!=null)node=((Matrix4x4)F(node,"ObjectToSeam")).MultiplyPoint3x4(p).z<0?F(node,"Negative"):F(node,"Positive");return (bool)F(node,"IsGap")?-1:(int)F(node,"Bone");}
  var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(root,"artifacts/material-remap/Lift01/original-wrap-fixture.json")));
  var report=new Report{assemblyPath=dll,roadPreparationInvokedByHarness=false};
  foreach(var row in fixture.rows)foreach(float delta in new[]{-1f,0f,2f,8f})foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(7,-2,11),Quaternion.Euler(0,31,0))}) {
   var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
   Vector3 Local(Vector3 p){p.x*=(3+delta)/3;return matrix.inverse.MultiplyPoint3x4(p);}
   var a=Local(row.a);var b=Local(row.b);var c=Local(row.c);
   var level=create.Invoke(null,new object[]{row.lod,native,pose,delta});var patches=(Array)F(level,"Patches");
   int region=Region(F(level,"Carriageway"),(a+b+c)/3);if(region<0)throw new InvalidDataException("Appearance gap.");var patch=patches.GetValue(region);
   if(!(bool)F(patch,"SourceAtlasRepeat")||!ReferenceEquals(F(patch,"Surface"),native))throw new InvalidDataException("Wrong original repeat routing.");
   var normal=matrix.inverse.MultiplyVector(Vector3.up);var frame=assembly.GetType("BridgeBuilder.Bridges.MovableRoadAtlasWrap",true).GetMethod("ProjectedTangent",BindingFlags.Static|BindingFlags.NonPublic);var tangent=(Vector4)frame.Invoke(null,new object[]{normal,F(patch,"U"),F(patch,"V")});var color=new Color(.25f,.5f,.75f,1);
   object V(Vector3 p)=>vertex.Invoke(new object[]{p,normal,tangent,new Vector4(0,0,3,4),color});
   var input=triangle.Invoke(new object[]{V(a),V(b),V(c),7,11,19});var pieces=(Array)apply.Invoke(null,new[]{input,patch});
   float area=Vector3.Cross(b-a,c-a).magnitude,total=0;var cells=new HashSet<int>();
   var u=(Vector4)F(patch,"U");var v=(Vector4)F(patch,"V");var min=(Vector2)F(patch,"Min");var max=(Vector2)F(patch,"Max");
   foreach(var piece in pieces) {
    var vertices=new[]{F(piece,"A"),F(piece,"B"),F(piece,"C")};var positions=vertices.Select(x=>(Vector3)F(x,"Position")).ToArray();
    total+=Vector3.Cross(positions[1]-positions[0],positions[2]-positions[0]).magnitude;
    var centre=(positions[0]+positions[1]+positions[2])/3;
    int cell=Mathf.FloorToInt((Vector4.Dot(v,new Vector4(centre.x,centre.y,centre.z,1))-min.y)/(max.y-min.y));cells.Add(cell);
    if(Region(physical,matrix.MultiplyPoint3x4(centre))!=row.bone)throw new InvalidDataException("Motion ownership changed.");
    if((int)F(piece,"Material")!=7||(int)F(piece,"Occurrence")!=11||(int)F(piece,"SourceTriangle")!=19)throw new InvalidDataException("Source occurrence identity changed.");
    foreach(var value in vertices) {
     var p=(Vector3)F(value,"Position");var uv=(Vector4)F(value,"UV");var point=new Vector4(p.x,p.y,p.z,1);
     float du=Mathf.Abs(uv.x-Vector4.Dot(u,point));float phase=(Vector4.Dot(v,point)-uv.y)/(max.y-min.y);float dv=Mathf.Abs(phase-Mathf.Round(phase));
     report.maxProjectionError=Mathf.Max(report.maxProjectionError,Mathf.Max(du,dv));
     if(du>2e-6f||dv>1e-5f||uv.y<min.y-2e-6f||uv.y>max.y+2e-6f||uv.z!=3||uv.w!=4)throw new InvalidDataException("Atlas projection or retained UV channels changed.");
     if(Vector3.Distance((Vector3)F(value,"Normal"),normal)>2e-6f||Vector4.Distance((Vector4)F(value,"Tangent"),tangent)>2e-6f||(Color)F(value,"Color")!=color)throw new InvalidDataException("Selected geometry attributes changed.");
    }
   }
   float error=Mathf.Abs(total-area)/area;report.maxRelativeAreaError=Mathf.Max(report.maxRelativeAreaError,error);
   if(error>1e-5f||cells.Count!=row.internalSeams+1)throw new InvalidDataException("Area or minimum period cells changed.");
   report.cases++;report.outputTriangles+=pieces.Length;
  }
  File.WriteAllText(Path.Combine(root,"artifacts/material-remap/Lift01/candidate06-wrap-check.json"),JsonUtility.ToJson(report,true));
  Debug.Log("PASS actual Lift01 atlas wrap: "+report.cases+" cases; original surfaces, minimum cells, geometry attributes and bones.");
 }
}
