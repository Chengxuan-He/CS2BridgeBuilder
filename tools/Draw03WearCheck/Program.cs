using BridgeBuilder.Bridges;
using UnityEngine;
using Patch=BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
foreach(var centers in new[]{new float[0],new[]{0f},new[]{-5f,-1.5f,2f,5.5f},new[]{-8f,-5.5f,1f,4f,7f},new[]{10f,13.5f}})
{
 var patches=Enumerable.Range(0,24).Select(_=>new Patch(new Vector2(.75f,.3f),new Vector2(.99f,.36f),new Vector4(0,0,.1f,.2f),new Vector4(.2f,0,0,0),repeat:true)).ToArray();
 var output=Draw03RoadWear.Apply(patches,centers,new Matrix4x4(),new Vector2(-22,22));
 if(centers.Length==0){if(output.Any(p=>p.Bands!=null))throw new Exception("No traffic must remain unchanged");continue;}
 for(int i=0;i<24;i++)
 {
  var p=output[i];if(i%4>=2){if(p.Bands!=null)throw new Exception("Sidewalk changed");continue;}
  if(!p.BandRows || p.Bands==null || p.Bands[0].Start!=0 || p.Bands[^1].End!=1)throw new Exception("Incomplete texture coverage");
  float last=0; foreach(var b in p.Bands){
   if(b.Start!=last||b.End<=b.Start)throw new Exception("Gap or overlap");last=b.End;
   float density=(b.Max.y-b.Min.y)*b.RepeatY/(b.End-b.Start)*p.V.x;
   if(Math.Abs(density-376f/4096/3.10781622f)>1e-6)throw new Exception("Gap texture stretched instead of tiled");
   if(b.RepeatY!=1 && (Math.Abs(b.Min.y-802f/4096)>1e-6 || Math.Abs(b.Max.y-834f/4096)>1e-6))
    throw new Exception("Gap must use the complete measured dark asphalt unit, not a four-row sliver");
  }
  if(!p.Bands.Any(b=>b.RepeatY>1))throw new Exception("Wide gap must repeat");
  foreach(float x in centers)
  {
   float v=x*p.V.x+p.V.w;var b=p.Bands.Single(b=>v>=b.Start&&v<b.End);
   float sampled=b.Min.y+(v-b.Start)/(b.End-b.Start)*(b.Max.y-b.Min.y);
   if(Math.Abs(sampled-1006f/4096)>1e-6)throw new Exception("Wear centre drift");
   float gradient=(b.Max.y-b.Min.y)/(b.End-b.Start)*p.V.x;
   if(Math.Abs(gradient-(376f/4096/3.10781622f))>1e-6)throw new Exception("Native wear width was rescaled");
  }
 }
 Console.WriteLine($"PASS {centers.Length} lanes: native width and centres, continuous rows, 24 domains, sidewalks excluded.");
}
namespace Colossal.IO.AssetDatabase { public class SurfaceAsset {} }
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 operator*(Vector4 a,float b)=>new(a.x*b,a.y*b,a.z*b,a.w*b);}
 public struct Matrix4x4 {public static Vector4 operator*(Matrix4x4 a,Vector4 b)=>b;}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
}
