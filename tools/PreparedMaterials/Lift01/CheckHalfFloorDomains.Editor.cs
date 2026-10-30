using System;
using System.IO;
using System.Linq;
using UnityEngine;
using BridgeBuilder.Bridges;

// Standalone native-Unity math check. Production mapping/partition files are
// copied unchanged except namespace syntax; no game surface loading is claimed.
public static class Lift01HalfFloorDomainCheck
{
 [Serializable] public sealed class Row {public int lod,bone;public Vector3 position;public Vector2 uv;public Vector4 tangent;}
 [Serializable] public sealed class Fixture {public Row[] rows;}
 [Serializable] public sealed class Report {public int cases;public float maxUError,maxVError,maxTangentError;public bool originalBonePartitions,physicalPanelsOnly;}
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();var root=args[Array.IndexOf(args,"-domainRoot")+1];
  HardcodedNumbers.Root=root;
  var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(root,"artifacts/material-remap/Lift01/half-floor-domain-fixture.json")));
  var native=new Colossal.IO.AssetDatabase.SurfaceAsset();var finished=new Colossal.IO.AssetDatabase.SurfaceAsset();
  Vector2 min=new Vector2(.123f,0),max=new Vector2(.789f,1);var result=new Report{originalBonePartitions=true,physicalPanelsOnly=true};
  int Region(MovableRoadGeometry.Partition node,Vector3 p) {
   while(node.Negative!=null&&node.Positive!=null)node=node.ObjectToSeam.MultiplyPoint3x4(p).z<0?node.Negative:node.Positive;
   return node.IsGap?-1:node.Bone;
  }
  foreach(var delta in new[]{-1f,0f,2f,8f})foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(7,-2,11),Quaternion.Euler(0,31,0))}) {
   var transform=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
   foreach(var row in fixture.rows) {
    var level=Lift01HalfFloorAppearance.Create(row.lod,native,finished,min,max,pose,delta);
    var p=row.position;p.x*= (3+delta)/3;var local=transform.inverse.MultiplyPoint3x4(p);
    if(Region(Lift01RoadPartitions.Carriageway,p)!=row.bone)throw new InvalidDataException("Physical bone fixture differs.");
    int region=Region(level.Carriageway,local);if(region<0||region>=level.Patches.Length)throw new InvalidDataException("Appearance coverage gap.");
    var patch=level.Patches[region];var point=new Vector4(local.x,local.y,local.z,1);
    float u=Vector4.Dot(patch.U,point),v=Vector4.Dot(patch.V,point);
    float expectedU=patch.SourceUv?row.uv.x:min.x+(row.uv.x-.57421875f)/(.73193359375f-.57421875f)*(max.x-min.x);
    float expectedV=patch.SourceUv?row.uv.y:(row.uv.y-.260009765625f)/(.469970703125f-.260009765625f);
    float du=Mathf.Abs(u-expectedU),dv=patch.SourceUv?Mathf.Abs(v-expectedV):Mathf.Abs(Mathf.Repeat(v-expectedV+.5f,1)-.5f);
    var tangent=transform.inverse.MultiplyVector((Vector3)row.tangent);var actual=patch.Tangent.Value;
    float dt=Vector3.Distance((Vector3)actual,tangent)+Mathf.Abs(actual.w-row.tangent.w);
    result.maxUError=Mathf.Max(result.maxUError,du);result.maxVError=Mathf.Max(result.maxVError,dv);result.maxTangentError=Mathf.Max(result.maxTangentError,dt);result.cases++;
    if(du>2e-6f||dv>8e-6f||dt>2e-6f||level.SupportedTypes[region]!=MovableRoadAppearance.Type.Ballast)throw new InvalidDataException("Measured projection or frame changed.");
    if(patch.SourceUv&&!ReferenceEquals(patch.Surface,native)||!patch.SourceUv&&!ReferenceEquals(patch.Surface,finished))throw new InvalidDataException("Material identity routing changed.");
   }
  }
  File.WriteAllText(Path.Combine(root,"artifacts/material-remap/Lift01/half-floor-domain-check.json"),JsonUtility.ToJson(result,true));
  Debug.Log("PASS Lift01 original U/V, native tangents and bone/domain routing: "+result.cases);
 }
}
