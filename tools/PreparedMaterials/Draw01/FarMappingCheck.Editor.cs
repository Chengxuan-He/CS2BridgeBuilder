using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using BridgeBuilder.Bridges;
public static class Draw01FarMappingCheck
{
 public static void Run(){try{
  int count=0;
  foreach(float width in new[]{1f,1.7f})
  foreach(var pose in new[]{Pose.identity,new Pose(new Vector3(2,3,-4),Quaternion.Euler(0,37,0))}){
   var road=new MovableClosedRoad.Result();road.Surfaces=new[]{new Colossal.IO.AssetDatabase.SurfaceAsset()};
   road.Occurrences=new[]{new MovableClosedRoad.Piece("Carriageway"),new MovableClosedRoad.Piece("Sidewalk"),new MovableClosedRoad.Piece("Sidewalk"),new MovableClosedRoad.Piece("Carriageway")};
   road.Occurrences[3].RootSection=road.Occurrences[0].RootSection;
   var faces=new List<MovableRoadGeometry.Triangle>();
   void Quad(float l,float r,int occurrence,float zmin=-90.099609375f,float zmax=90.099609375f){
    MovableRoadGeometry.Vertex V(float x,float z)=>new MovableRoadGeometry.Vertex(new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(x,z,0,0),Color.white);
    var a=V(l,zmin);var b=V(r,zmin);var c=V(r,zmax);var d=V(l,zmax);
    faces.Add(new MovableRoadGeometry.Triangle(a,c,b,0,occurrence,0));faces.Add(new MovableRoadGeometry.Triangle(a,d,c,0,occurrence,1));
   }
   Quad(-3.4000244140625f*width,3.4000244140625f*width,0);
   Quad(-6.5069580078125f*width,-3.4000244140625f*width,1);
   Quad(3.4000244140625f*width,6.5069580078125f*width,2);
   Quad(-3.4000244140625f*width,3.4000244140625f*width,3,83.740234375f,86f);
   var inverseRotation=Quaternion.Inverse(pose.rotation);var inverse=new Pose(inverseRotation * -pose.position,inverseRotation);
   road.Triangles=MovableRoadGeometry.InFrame(faces.ToArray(),inverse);var mapped=Draw01FarAppearance.Create(road,pose,road.Surfaces[0]);
   var physical=road.Occurrences.Select(p=>Draw01RoadPartitions.RoadSurface(p.RootSection.name=="Sidewalk")).ToArray();
   if(!MovableRoadGeometry.TryBind(MovableRoadGeometry.InFrame(road.Triangles,pose),Draw01RoadPartitions.Carriageway,3,1,out var receivers,out var physicalError,occurrencePartitions:physical))throw new Exception(physicalError);
   if(!MovableRoadGeometry.TryBind(MovableRoadGeometry.InFrame(receivers.Select(r=>r.Geometry).ToArray(),inverse),MovableRoadGeometry.Partition.Gap,mapped.Patches.Length,1,out var result,out var error,occurrencePartitions:mapped.Occurrences))throw new Exception(error);
   if(result.Length==0)throw new Exception("Finite mapping removed every face");
   foreach(var f in result){var p=mapped.Patches[f.Bone];if(!p.SourceUv||p.Repeat||!ReferenceEquals(p.Surface,road.Surfaces[0]))throw new Exception("Lost original source");
    if(mapped.SupportedTypes[f.Bone]!=((f.Geometry.Occurrence==0||f.Geometry.Occurrence==3)?MovableRoadAppearance.Type.Asphalt:MovableRoadAppearance.Type.Sidewalk))throw new Exception("Layer type mismatch");
    if(f.Geometry.Occurrence==3&&!result.Where(t=>t.Geometry.Occurrence==0).Any(t=>{var other=mapped.Patches[t.Bone];return other.Min==p.Min&&other.Max==p.Max&&other.U.Equals(p.U)&&other.V.Equals(p.V);}))throw new Exception("One section restarts its finite mapping at a new piece occurrence.");
    foreach(var v in new[]{f.Geometry.A,f.Geometry.B,f.Geometry.C}){
     var q=new Vector4(v.Position.x,v.Position.y,v.Position.z,1);var uv=new Vector2(Vector4.Dot(p.U,q),Vector4.Dot(p.V,q));
     // Bound checks use each coordinate magnitude's binary16 half-ULP.
     if(!float.IsFinite(uv.x)||!float.IsFinite(uv.y))throw new Exception("Nonfinite UV");
     for(int axis=0;axis<2;axis++){float margin=Mathf.Pow(2,Mathf.Floor(Mathf.Log(Mathf.Max(Mathf.Abs(p.Min[axis]),Mathf.Abs(p.Max[axis])),2))-11);if(uv[axis]<p.Min[axis]-margin||uv[axis]>p.Max[axis]+margin)throw new Exception("Finite chart extrapolation: patch "+f.Bone+" axis "+axis+" value "+uv[axis]+" position "+v.Position+" bounds "+p.Min[axis]+".."+p.Max[axis]);}count++;
    }
   }
  }
  File.WriteAllText(Draw01AppearancePartitionCheck.Argument("-report"),"{\"status\":\"PASS\",\"vertexChecks\":"+count+",\"widthScenarios\":2,\"poseScenarios\":2,\"nativeRenderingVerified\":false}");Debug.Log("PASS Draw01 finite far mapping "+count);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
namespace Colossal.IO.AssetDatabase {public sealed class SurfaceAsset{}}
namespace BridgeBuilder.Bridges {
 internal static class MovableClosedRoad {
  internal sealed class Section{internal string name;internal Section(string n){name=n;}}
  internal sealed class Piece{internal Section RootSection;internal Piece(string n){RootSection=new Section(n);}}
  internal sealed class Result{internal MovableRoadGeometry.Triangle[] Triangles;internal Colossal.IO.AssetDatabase.SurfaceAsset[] Surfaces;internal Piece[] Occurrences;}
 }
}
