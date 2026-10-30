using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using G=BridgeBuilder.Bridges.MovableRoadGeometry;
public static class CombinedPartitionCheck
{
 public static void Run()
 {try{
// Two physical/material cuts must clip a polygon before its only triangulation.
G.Vertex JointVertex(float x,float z)=>new(new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,-1),new Vector4(x,z,7,9),Color.gray);
var jointSource=new[]{new G.Triangle(JointVertex(-2,-2),JointVertex(2,-2),JointVertex(0,2),0,0,0)};
var physFrame=Matrix4x4.identity;physFrame.SetRow(0,new Vector4(0,0,-1,0));physFrame.SetRow(2,new Vector4(1,0,0,-.5f));
var appFrame=Matrix4x4.identity;appFrame[2,3]=-.25f;
var physicalTree=new G.Partition(physFrame,new G.Partition(0),new G.Partition(1));
var appearanceTree=new G.Partition(appFrame,new G.Partition(0),new G.Partition(1));
var jointTree=G.CombinePartitions(physicalTree,appearanceTree,Matrix4x4.identity,new[]{false,true},2);
if(!G.TryBind(jointSource,jointTree,4,1,out var singlePass,out var singleError))throw new Exception(singleError);
if(!G.TryBind(jointSource,physicalTree,2,1,out var firstPass,out var firstError))throw new Exception(firstError);
if(!G.TryBind(firstPass.Select(t=>t.Geometry).ToArray(),appearanceTree,2,1,out var twoPass,out var secondError))throw new Exception(secondError);
if(singlePass.Length>=twoPass.Length)throw new Exception("Combined partition did not remove intermediate fan diagonals.");
double jointArea=0;
foreach(var t in singlePass)
{
    var a=t.Geometry.A;var b=t.Geometry.B;var c=t.Geometry.C;
    var center=(a.Position+b.Position+c.Position)/3;
    uint expected=(uint)((center.z>.25f?1:0)+(center.x>.5f?2:0));
    if(t.Bone!=expected)throw new Exception("Joint physical/appearance label changed.");
    foreach(var v in new[]{a,b,c})if(v.Color!=Color.gray||v.Normal!=Vector3.up||v.Tangent!=new Vector4(1,0,0,-1)||v.UV.z!=7||v.UV.w!=9)
        throw new Exception("Joint partition changed retained attributes.");
    jointArea+=Vector3.Cross(b.Position-a.Position,c.Position-a.Position).magnitude*.5;
}
if(Math.Abs(jointArea-8)>1e-5)throw new Exception("Joint clipping lost original geometry area.");
var gapTree=G.CombinePartitions(new G.Partition(physFrame,G.Partition.Gap,new G.Partition(1)),appearanceTree,Matrix4x4.identity,new[]{false,true},2);
if(!G.TryBind(jointSource,gapTree,4,1,out var withGap,out var gapError)||withGap.Any(t=>t.Bone<2))throw new Exception("Joint binding filled a measured physical gap: "+gapError);
Debug.Log($"PASS single-polygon physical/appearance clipping: {twoPass.Length} intermediate-fan triangles -> {singlePass.Length}; labels, area, source attributes and gap exclusion retained.");

foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(0,31,0),Quaternion.Euler(13,31,7)})
{
    var pose=Matrix4x4.TRS(new Vector3(17,-3,21),rotation,Vector3.one);
    var measuredPhysical=new G.Partition(physFrame*pose.inverse,new G.Partition(0),new G.Partition(1));
    var combinedPose=G.CombinePartitions(measuredPhysical,appearanceTree,pose,new[]{false,true},2);
    if(!G.TryBind(jointSource,combinedPose,4,1,out var projected,out var error))throw new Exception(error);
    var physicalInput=G.InFrame(projected.Select(t=>t.Geometry).ToArray(),new Pose(new Vector3(17,-3,21),rotation));
    if(!G.TryBind(physicalInput,measuredPhysical,2,1,out var rebound,out error))throw new Exception(error);
    if(rebound.Length!=projected.Length)throw new Exception("Final physical rebind introduced new diagonals.");
    if(projected.Any(t=>t.Geometry.Occurrence!=0||t.Geometry.SourceTriangle!=0))throw new Exception("Joint binding lost source provenance.");
}
Debug.Log("PASS combined tree rigid poses and final physical rebind: no additional triangles, provenance retained.");
EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
