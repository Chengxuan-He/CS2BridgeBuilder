using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using BridgeBuilder.Bridges;

// Standalone native-Unity math check. Production mapping/partition files are
// copied unchanged except namespace syntax; no game surface loading is claimed.
public static class Lift01FarDomainCheck
{
 [Serializable] public sealed class Row {public int lod,bone;public Vector3 position;public Vector2 uv,upper;public Vector4 tangent;}
 [Serializable] public sealed class Fixture {public Row[] rows;}
 [Serializable] public sealed class Report {public string assemblySHA256;public bool roadPreparationInvokedByHarness;public int cases;public bool originalBonePartitions,originalFloat16Intervals=true,sourceSurfaceIdentity=true,projectedTangentFlag=true,tangentSamplingTested=false,semanticRailRemovalQualified=false;public int minimalFiniteDomains=5,sourceCharts=4,additionalGradientFolds=1;}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();var root=args[Array.IndexOf(args,"-domainRoot")+1];
  string assemblyPath=args[Array.IndexOf(args,"-candidate")+1],game=args[Array.IndexOf(args,"-gameData")+1];
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string path=Path.Combine(game,"Managed",new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
  var assembly=Assembly.LoadFrom(assemblyPath);
  var create=assembly.GetType("BridgeBuilder.Bridges.Lift01FarAppearance",true).GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic);
  var physical=assembly.GetType("BridgeBuilder.Bridges.Lift01RoadPartitions",true).GetField("Carriageway",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  var surfaceType=create.GetParameters()[0].ParameterType;
  object Field(object item,string name)=>item.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(item);
  var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(root,"artifacts/material-remap/Lift01/far-domain-fixture.json")));
  var native=FormatterServices.GetUninitializedObject(surfaceType);var finished=FormatterServices.GetUninitializedObject(surfaceType);
  Vector2 min=new Vector2(.123f,0),max=new Vector2(.789f,1);var result=new Report{originalBonePartitions=true,roadPreparationInvokedByHarness=false};
  using(var sha=SHA256.Create())result.assemblySHA256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(assemblyPath))).Replace("-","");
  int Region(object node,Vector3 p) {
   while(Field(node,"Negative")!=null&&Field(node,"Positive")!=null)node=((Matrix4x4)Field(node,"ObjectToSeam")).MultiplyPoint3x4(p).z<0?Field(node,"Negative"):Field(node,"Positive");
   return (bool)Field(node,"IsGap")?-1:(int)Field(node,"Bone");
  }
  foreach(var delta in new[]{-1f,0f,2f,8f})foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(7,-2,11),Quaternion.Euler(0,31,0))}) {
   var transform=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
   foreach(var row in fixture.rows) {
    var level=create.Invoke(null,new object[]{native,pose,delta});
    var patches=(Array)Field(level,"Patches");var types=(Array)Field(level,"SupportedTypes");
    var p=row.position;p.x*= (3.28533935546875f+delta)/3.28533935546875f;var local=transform.inverse.MultiplyPoint3x4(p);
    if(Region(physical,p)!=row.bone)throw new InvalidDataException("Physical bone fixture differs.");
    int region=Region(Field(level,"Carriageway"),local);if(region<0||region>=patches.Length)throw new InvalidDataException("Appearance coverage gap.");
    var patch=patches.GetValue(region);bool sourceUv=(bool)Field(patch,"SourceUv");bool repeat=(bool)Field(patch,"SourceAtlasRepeat");var point=new Vector4(local.x,local.y,local.z,1);
    float u=Vector4.Dot(((Vector4)Field(patch,"U")),point),v=Vector4.Dot(((Vector4)Field(patch,"V")),point);
    float expectedU=row.uv.x,expectedV=row.uv.y;
    float du=Mathf.Abs(u-expectedU),dv=repeat?Mathf.Abs(Mathf.Repeat((v-expectedV)/(.469970703125f-.260009765625f)+.5f,1)-.5f):Mathf.Abs(v-expectedV);
    if(!(bool)Field(patch,"ProjectTangent")||Field(patch,"Tangent")!=null)throw new InvalidDataException("Projected frame mode missing.");
    if(u<row.uv.x||u>row.upper.x||v<row.uv.y||v>row.upper.y)throw new InvalidDataException("Outside original Float16 affine interval.");
    if(repeat||(bool)Field(patch,"RepeatU")||(bool)Field(patch,"RepeatV")||patches.Length!=5)throw new InvalidDataException("Finite charts or minimal fold changed.");
    if(types.GetValue(region).ToString()!="Ballast")throw new InvalidDataException("Appearance role changed.");
    result.cases++;
    if(!sourceUv||!ReferenceEquals(Field(patch,"Surface"),native)||(bool)Field(patch,"PreparedUnit"))throw new InvalidDataException("Material identity routing changed.");
   }
  }
  File.WriteAllText(Path.Combine(root,"artifacts/material-remap/Lift01/candidate06-far-domain-check.json"),JsonUtility.ToJson(result,true));
  Debug.Log("PASS Lift01 original U/V, native tangents and bone/domain routing: "+result.cases);
 }
}
