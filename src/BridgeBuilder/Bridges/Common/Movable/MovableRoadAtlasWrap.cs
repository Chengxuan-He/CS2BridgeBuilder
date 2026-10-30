using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Consumes an evidenced prototype region AFTER appearance and motion partitioning.
// No source triangle discovery, texture generation or period inference occurs here.
internal static class MovableRoadAtlasWrap
{
    private readonly struct Point
    {
        internal readonly Vertex Vertex;
        internal readonly double U, V;
        internal readonly int SourceEdges;
        internal Point(Vertex vertex, double u, double v,int sourceEdges) { Vertex=vertex; U=u; V=v; SourceEdges=sourceEdges; }
        internal double Axis(int axis) => axis==0?U:V;
    }

    internal static Triangle[] Apply(Triangle source, MovableExistingRoadSurface.Patch patch)
    {
        if(!patch.SourceAtlasRepeat)throw new ArgumentException("Atlas-wrap requires an explicitly evidenced mapping.");
        Point Convert(Vertex v,int edges) => new(v,
            Coordinate(patch.U,v.Position,patch.Min.x,patch.Max.x,patch.RepeatU),
            Coordinate(patch.V,v.Position,patch.Min.y,patch.Max.y,patch.RepeatV),edges);
        var original=new List<Point>{Convert(source.A,3),Convert(source.B,5),Convert(source.C,6)};
        foreach(var p in original)
            if(!double.IsFinite(p.U) || !double.IsFinite(p.V))
                throw new InvalidOperationException("Non-finite original-atlas projection.");
        var polygons=new List<(List<Point> Points,int U,int V)>{(original,0,0)};
        for(int axis=0;axis<2;axis++)
        {
            if(!(axis==0?patch.RepeatU:patch.RepeatV))continue;
            var next=new List<(List<Point>,int,int)>();
            foreach(var polygon in polygons)
            {
                double min=polygon.Points.Min(p=>p.Axis(axis)),max=polygon.Points.Max(p=>p.Axis(axis));
                int first=checked((int)Math.Floor(min)),last=checked((int)Math.Ceiling(max));
                // Constant coordinates on an integer boundary do not require subdivision.
                if(first==last)last=checked(first+1);
                for(int cell=first;cell<last;cell++)
                {
                    var clipped=Clip(Clip(polygon.Points,axis,cell,true,original),axis,(double)cell+1,false,original);
                    if(clipped.Count>=3)next.Add((clipped,axis==0?cell:polygon.U,axis==1?cell:polygon.V));
                }
            }
            polygons=next;
        }
        var result=new List<Triangle>();
        foreach(var polygon in polygons)
        {
            Vertex Output(Point p)
            {
                var value=p.Vertex;var uv=value.UV;
                uv.x=(float)(patch.Min.x+(p.U-polygon.U)*((double)patch.Max.x-patch.Min.x));
                uv.y=(float)(patch.Min.y+(p.V-polygon.V)*((double)patch.Max.y-patch.Min.y));
                var tangent=patch.Tangent ?? value.Tangent;
                if(patch.ProjectTangent)
                {
                    tangent=ProjectedTangent(value.Normal,patch.U,patch.V);
                }
                return new Vertex(value.Position,value.Normal,tangent,uv,value.Color);
            }
            var a=Output(polygon.Points[0]);
            for(int i=1;i+1<polygon.Points.Count;i++)
            {
                var b=Output(polygon.Points[i]);var c=Output(polygon.Points[i+1]);
                if(Vector3.Cross(b.Position-a.Position,c.Position-a.Position).sqrMagnitude==0f)continue;
                result.Add(new Triangle(a,b,c,source.Material,source.Occurrence,source.SourceTriangle));
            }
        }
        return result.ToArray();
    }

    // Float32 dot products use three products and three sums. gamma(6) bounds their
    // accumulated rounding error (round-to-nearest, no overflow). An integer wrap
    // boundary inside this interval is numerically indistinguishable from the
    // already partitioned boundary. Canonicalize UV only, never physical positions.
    internal static double Coordinate(Vector4 p,Vector3 v,float min,float max,bool repeat)
    {
        double x=(double)p.x*v.x,y=(double)p.y*v.y,z=(double)p.z*v.z;
        double projected=x+y+z+p.w,extent=(double)max-min;
        double q=(projected-min)/extent;
        if(!repeat || !double.IsFinite(q))return q;
        const double unitRoundoff=1.0/16777216.0;
        const double gamma6=6*unitRoundoff/(1-6*unitRoundoff);
        double error=gamma6*(Math.Abs(x)+Math.Abs(y)+Math.Abs(z)+Math.Abs(p.w));
        double integer=Math.Round(q),boundary=min+integer*extent;
        // An interval spanning multiple cells cannot identify a unique boundary.
        if(error<extent*.5 && Math.Abs(projected-boundary)<=error)return integer;
        return q;
    }

    internal static Vector4 ProjectedTangent(Vector3 normal,Vector4 u,Vector4 v)
    {
        var direction=Vector3.Cross((Vector3)v,normal);
        float determinant=Vector3.Dot((Vector3)u,direction);
        if(!float.IsFinite(determinant) || determinant==0f)
            throw new InvalidOperationException("Evidenced projected atlas tangent has a singular receiver frame.");
        direction=(direction/determinant).normalized;
        return new Vector4(direction.x,direction.y,direction.z,determinant>0?1:-1);
    }

    private static List<Point> Clip(List<Point> input,int axis,double boundary,bool greater,List<Point> source)
    {
        var output=new List<Point>();
        void Add(Point p)
        {
            if(output.Count==0 || output[^1].U!=p.U || output[^1].V!=p.V ||
                !output[^1].Vertex.Position.Equals(p.Vertex.Position))output.Add(p);
        }
        for(int i=0;i<input.Count;i++)
        {
            var a=input[i];var b=input[(i+1)%input.Count];
            double x=a.Axis(axis),y=b.Axis(axis);
            bool insideA=greater?x>=boundary:x<=boundary,insideB=greater?y>=boundary:y<=boundary;
            if(insideA)Add(a);
            if(insideA==insideB)continue;
            // Adjacent triangles traverse their shared edge in opposite directions.
            // Use the same endpoint order so their Float32 seam position agrees exactly.
            var start=a;var end=b;
            int edges=a.SourceEdges&b.SourceEdges;
            // A neighbouring cell may reach this plane after an earlier clip.
            // Reuse the ORIGINAL edge, not its already rounded shorter segment.
            if((edges&1)!=0){start=source[0];end=source[1];}
            else if((edges&2)!=0){start=source[0];end=source[2];}
            else if((edges&4)!=0){start=source[1];end=source[2];}
            for(int component=0;component<3;component++)
            {
                int order=start.Vertex.Position[component].CompareTo(end.Vertex.Position[component]);
                if(order==0)continue;
                if(order>0){var swap=start;start=end;end=swap;}
                break;
            }
            double t=(boundary-start.Axis(axis))/(end.Axis(axis)-start.Axis(axis));
            var vertex=t==0?start.Vertex:t==1?end.Vertex:MovableRoadGeometry.Vertex.Interpolate(start.Vertex,end.Vertex,(float)t);
            if(t!=0 && t!=1)
            {
                var position=new Vector3(
                    (float)((double)start.Vertex.Position.x+((double)end.Vertex.Position.x-start.Vertex.Position.x)*t),
                    (float)((double)start.Vertex.Position.y+((double)end.Vertex.Position.y-start.Vertex.Position.y)*t),
                    (float)((double)start.Vertex.Position.z+((double)end.Vertex.Position.z-start.Vertex.Position.z)*t));
                vertex=new Vertex(position,vertex.Normal,vertex.Tangent,vertex.UV,vertex.Color);
            }
            Add(new Point(vertex,axis==0?boundary:start.U+(end.U-start.U)*t,
                axis==1?boundary:start.V+(end.V-start.V)*t,edges));
        }
        if(output.Count>1 && output[0].U==output[^1].U && output[0].V==output[^1].V &&
            output[0].Vertex.Position.Equals(output[^1].Vertex.Position))output.RemoveAt(output.Count-1);
        return output;
    }
}
