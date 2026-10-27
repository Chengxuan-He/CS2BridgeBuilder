using BridgeBuilder.Bridges;
using UnityEngine;
var d=HardcodedNumbers.Load("Types_Draw01_Draw01MechanismData_g_cs").F32;
var stations=new[]{-d[36],-d[43],d[50],d[57]};
foreach(float delta in new[]{-3f,0f,5f})
foreach(float x in new[]{-4.7f,0f,4.7f})
{
 var tree=Draw01RoadPartitions.Surface(delta);
 var point=new Vector3(x,9.8f,0);
 var ends=MovableAttachmentStations.Ends(point,tree,bone=>bone!=0,0.5f);
 if(ends.Length!=2 || ends[0]>=0 || ends[1]<=0 || Math.Abs(ends[0]+ends[1])>0.001f)
  throw new Exception("Moving span must have two symmetric outer end supports");
 foreach(var end in ends)
  if(!MovableAttachmentStations.Supported(new Vector3(x,9.8f,end),tree))
   throw new Exception("End support is in a gap");
 if(MovableAttachmentStations.Supported(point,tree))throw new Exception("Central gap pole must be omitted");
 foreach(float station in stations)
 {
  var supported=new Vector3(x,9.8f,station);
  if(!MovableAttachmentStations.Supported(supported,tree))throw new Exception("Native supported station lost");
  if(supported.x!=x||supported.y!=9.8f||supported.z!=station)throw new Exception("Support moved");
  if(!MovableAttachmentStations.Supported(new Vector3(-x,9.8f,-station),tree))throw new Exception("Symmetric station lost");
 }
 Console.WriteLine($"PASS delta={delta}, X={x}: centre gap omitted; supported and mirrored stations preserved without relocation.");
}
if(MovableAttachmentStations.Supported(new Vector3(0,0,0),MovableRoadGeometry.Partition.Gap))throw new Exception("Gap must be unsupported");
if(MovableAttachmentStations.Ends(new Vector3(),new MovableRoadGeometry.Partition(0),_=>false,0.5f).Length!=0)
 throw new Exception("Fixed domain must not acquire moving-end supports");
namespace BridgeBuilder.Bridges {
 internal static class MovableNodeObjects {internal class Result {internal object[] Placements=Array.Empty<object>();}}
 internal static class MovableRoadGeometry {internal sealed class Partition {
  internal int Bone;internal bool IsGap;internal Matrix4x4 ObjectToSeam;internal Partition? Negative,Positive;
  internal Partition(int bone){Bone=bone;}
  internal Partition(Matrix4x4 frame,Partition a,Partition b){Bone=-1;ObjectToSeam=frame;Negative=a;Positive=b;}
  internal static readonly Partition Gap=new(-1){IsGap=true};
 }}
}
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new(1,1,1);}
 public struct Quaternion {}
 public struct Pose {public Vector3 position;public Quaternion rotation;}
 public struct Matrix4x4 {public float m00,m02,m20,m22,m23;public static Matrix4x4 identity=>new(){m00=1,m22=1};public Vector3 MultiplyPoint3x4(Vector3 p)=>new(m00*p.x+m02*p.z,p.y,m20*p.x+m22*p.z+m23);public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s)=>identity;public static Matrix4x4 operator*(Matrix4x4 a,Matrix4x4 b)=>a;}
}
