using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

internal static class MovableRigidDecalVolume
{
    private sealed class Face : List<Vector3>
    {
        internal readonly Vector3 Normal;
        internal Face(IEnumerable<Vector3> points, Vector3 normal) : base(points) { Normal=normal; }
    }

    internal sealed class Part
    {
        internal readonly int Bone;
        internal readonly Vector3 Center, Size;
        internal readonly Quaternion Rotation;
        internal readonly Vector4 Area;
        internal readonly BoundTriangle[] Triangles;
        internal Part(int bone, Vector3 center, Vector3 size, Quaternion rotation, Vector4 area, BoundTriangle[] triangles)
        {Bone=bone;Center=center;Size=size;Rotation=rotation;Area=area;Triangles=triangles;}
    }

    internal static List<Part> Build(MovableNativeDecals.Region region, Partition partition)
    {
        var result=new List<Part>();
        if(region.Half.x<=0 || region.Half.y<=0 || region.Half.z<=0)return result;
        var rotation=Quaternion.LookRotation(region.Forward,region.Up);
        var frame=Matrix4x4.TRS(region.Center,rotation,Vector3.one);
        var half=region.Half;
        var corners=new Vector3[8];
        for(int i=0;i<8;i++)corners[i]=new Vector3((i&1)==0?-half.x:half.x,(i&2)==0?-half.y:half.y,(i&4)==0?-half.z:half.z);
        var faces=new List<Face>();
        foreach(var indices in new[]{new[]{0,2,6,4},new[]{1,5,7,3},new[]{0,4,5,1},new[]{2,3,7,6},new[]{0,1,3,2},new[]{4,6,7,5}})
        {
            var face=indices.Select(i=>corners[i]).ToList();
            var center=face.Aggregate(Vector3.zero,(a,b)=>a+b)/4f;
            if(Vector3.Dot(Vector3.Cross(face[1]-face[0],face[2]-face[0]),center)<0)face.Reverse();
            var normal=face.All(p=>p.x==face[0].x)?new Vector3(Math.Sign(center.x),0,0):
                face.All(p=>p.y==face[0].y)?new Vector3(0,Math.Sign(center.y),0):new Vector3(0,0,Math.Sign(center.z));
            faces.Add(new Face(face,normal));
        }
        Visit(partition,faces);
        return result;

        void Visit(Partition node,List<Face> polygons)
        {
            if(node.IsGap || polygons.Count==0)return;
            if(node.Bone>=0)
            {
                var points=polygons.SelectMany(p=>p).ToArray();
                var lo=points.Aggregate(Vector3.Min);var hi=points.Aggregate(Vector3.Max);
                var size=hi-lo;if(size.x<=0||size.y<=0||size.z<=0)return;
                var center=(lo+hi)*.5f;
                // Native projection uses local bounds, not vertex UV. Compose the
                // cropped texture area with those bounds, retaining source UV/metre.
                var a=region.SourceUv(frame.MultiplyPoint3x4(lo));
                var b=region.SourceUv(frame.MultiplyPoint3x4(hi));
                var triangles=new List<BoundTriangle>();
                foreach(var polygon in polygons)
                {
                    // Face planes survive clipping, including duplicate/collinear seam vertices.
                    var normal=polygon.Normal;
                    var tangent=Vector3.Cross(Math.Abs(normal.y)<.9f?Vector3.up:Vector3.forward,normal).normalized;
                    Vertex V(Vector3 p)=>new(p-center,normal,new Vector4(tangent.x,tangent.y,tangent.z,1),Vector4.zero,Color.white);
                    for(int i=1;i+1<polygon.Count;i++)
                        triangles.Add(new BoundTriangle(new Triangle(V(polygon[0]),V(polygon[i]),V(polygon[i+1]),0,0,0),0));
                }
                result.Add(new Part(node.Bone,frame.MultiplyPoint3x4(center),size,rotation,new Vector4(a.x,a.y,b.x,b.y),triangles.ToArray()));
                return;
            }
            var seam=node.ObjectToSeam*frame;
            var plane=new Vector4(seam.m20,seam.m21,seam.m22,seam.m23);
            Visit(node.Negative!,Clip(polygons,plane,false));
            Visit(node.Positive!,Clip(polygons,plane,true));
        }
    }

    private static List<Face> Clip(List<Face> source,Vector4 plane,bool positive)
    {
        float D(Vector3 p)=>(plane.x*p.x+plane.y*p.y+plane.z*p.z+plane.w)*(positive?1:-1);
        var result=new List<Face>();var cut=new List<Vector3>();
        foreach(var face in source)
        {
            var output=new List<Vector3>();var previous=face[face.Count-1];var pd=D(previous);
            foreach(var point in face)
            {
                var d=D(point);
                if((d>=0)!=(pd>=0))
                {
                    var at=Vector3.LerpUnclamped(previous,point,pd/(pd-d));
                    output.Add(at);if(!cut.Contains(at))cut.Add(at);
                }
                if(d>=0)output.Add(point);
                previous=point;pd=d;
            }
            if(output.Count>=3)result.Add(new Face(output,face.Normal));
        }
        if(cut.Count>=3)
        {
            var center=cut.Aggregate(Vector3.zero,(a,b)=>a+b)/cut.Count;
            var normal=((Vector3)plane).normalized*(positive?-1:1);
            var u=Vector3.Cross(Math.Abs(normal.y)<.9f?Vector3.up:Vector3.forward,normal).normalized;
            var v=Vector3.Cross(normal,u);
            cut=cut.OrderBy(p=>Math.Atan2(Vector3.Dot(p-center,v),Vector3.Dot(p-center,u))).ToList();
            result.Add(new Face(cut,normal));
        }
        return result;
    }
}
