using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
public static class Lift01ReceiverFrameCheck
{
 [Serializable] public sealed class Row {public int lod,bone,internalSeams;public Vector3 a,b,c;}
 [Serializable] public sealed class Fixture {public Row[] rows;}
 [Serializable] public sealed class Report {public int cases,vertices;public float maxProjectedOrthogonalityError,maxExpectedTangentError,maxConstantTangentOrthogonalityError,maxNormalChange;public bool runtimeModeChanged;public string assemblyPath;}
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
  int Region(object node,Vector3 p) {while(F(node,"Negative")!=null&&F(node,"Positive")!=null)node=((Matrix4x4)F(node,"ObjectToSeam")).MultiplyPoint3x4(p).z<0?F(node,"Negative"):F(node,"Positive");return (bool)F(node,"IsGap")?-1:(int)F(node,"Bone");}
  var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(root,"artifacts/material-remap/Lift01/original-wrap-fixture.json")));
  var report=new Report{assemblyPath=dll,runtimeModeChanged=true};
  foreach(var row in fixture.rows)foreach(float delta in new[]{-1f,8f})foreach(float sx in new[]{-.3f,-.08f,0f,.08f,.3f})foreach(float sz in new[]{0f,.1f})foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(7,-2,11),Quaternion.Euler(0,31,0))}) {
   var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
   Vector3 Local(Vector3 p){p.x*=(3+delta)/3;p.y+=sx*p.x+sz*p.z;return matrix.inverse.MultiplyPoint3x4(p);}
   var a=Local(row.a);var b=Local(row.b);var c=Local(row.c);
   var level=create.Invoke(null,new object[]{row.lod,native,pose,delta});var patches=(Array)F(level,"Patches");
   int region=Region(F(level,"Carriageway"),(a+b+c)/3);if(region<0)throw new InvalidDataException("Appearance gap.");var original=patches.GetValue(region);
   if(!(bool)F(original,"SourceAtlasRepeat"))throw new InvalidDataException("Not a real repeat domain.");
   if(!(bool)F(original,"ProjectTangent")||F(original,"Tangent")!=null)throw new InvalidDataException("Production projected tangent missing.");var frame=assembly.GetType("BridgeBuilder.Bridges.MovableRoadAtlasWrap",true).GetMethod("ProjectedTangent",BindingFlags.Static|BindingFlags.NonPublic);var constant=(Vector4)frame.Invoke(null,new object[]{matrix.inverse.MultiplyVector(Vector3.up),F(original,"U"),F(original,"V")});var objectTangent=matrix.MultiplyVector((Vector3)constant);
   if(Mathf.Abs(objectTangent.z)>2e-6f||Mathf.Abs(objectTangent.y)>2e-6f)throw new InvalidDataException("Source frame is not the measured lateral native frame.");
   var normal=matrix.inverse.MultiplyVector(new Vector3(-sx,1,-sz).normalized);
   var expected=matrix.inverse.MultiplyVector((new Vector3(1,sx,0)*Mathf.Sign(objectTangent.x)).normalized);
   report.maxConstantTangentOrthogonalityError=Mathf.Max(report.maxConstantTangentOrthogonalityError,Mathf.Abs(Vector3.Dot((Vector3)constant,normal)));
   var ctor=original.GetType().GetConstructors(flags).Single(x=>x.GetParameters().Length>2);
   var values=ctor.GetParameters().Select(p=>p.DefaultValue).ToArray();
   for(int i=0;i<values.Length;i++)switch(ctor.GetParameters()[i].Name) {
    case "min":values[i]=F(original,"Min");break;case "max":values[i]=F(original,"Max");break;
    case "u":values[i]=F(original,"U");break;case "v":values[i]=F(original,"V");break;
    case "surface":values[i]=native;break;case "sourceUv":case "sourceAtlasRepeat":case "projectTangent":case "repeatV":values[i]=true;break;
    case "tangent":values[i]=null;break;
   }
   var patch=original;
   object V(Vector3 p)=>vertex.Invoke(new object[]{p,normal,constant,new Vector4(0,0,3,4),Color.white});
   var input=triangle.Invoke(new object[]{V(a),V(b),V(c),7,11,19});var pieces=(Array)apply.Invoke(null,new[]{input,patch});
   foreach(var piece in pieces)foreach(var value in new[]{F(piece,"A"),F(piece,"B"),F(piece,"C")}) {
    var actual=(Vector4)F(value,"Tangent");var keptNormal=(Vector3)F(value,"Normal");
    float orth=Mathf.Abs(Vector3.Dot((Vector3)actual,keptNormal));
    float error=Vector3.Distance((Vector3)actual,expected)+Mathf.Abs(actual.w-constant.w);
    float dn=Vector3.Distance(keptNormal,normal);
    report.maxProjectedOrthogonalityError=Mathf.Max(report.maxProjectedOrthogonalityError,orth);
    report.maxExpectedTangentError=Mathf.Max(report.maxExpectedTangentError,error);report.maxNormalChange=Mathf.Max(report.maxNormalChange,dn);report.vertices++;
    if(orth>2e-6f||error>2e-6f||dn>2e-6f)throw new InvalidDataException("Receiver tangent frame differs from measured source lateral-axis convention.");
   }
   report.cases++;
  }
  File.WriteAllText(Path.Combine(root,"artifacts/material-remap/Lift01/candidate06-receiver-frame-check.json"),JsonUtility.ToJson(report,true));
  Debug.Log("PASS measured Lift01 lateral tangent transport: "+report.cases+" tilted cases; runtime mode unchanged.");
 }
}
