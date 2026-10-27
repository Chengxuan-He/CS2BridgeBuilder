using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Offline-measured atlas boundaries, not prototype geometry. Loop orientation is
// irrelevant: nested loops alternate filled regions and holes. No UV slack is
// added at boundaries, since that could admit texels from a neighbouring part.
internal sealed class MovableUvChart
{
    private readonly struct Point
    {
        internal readonly double X,Y;
        internal Point(double x,double y){X=x;Y=y;}
    }
    private readonly Point[][] _loops;
    private readonly double _minX=double.PositiveInfinity,_minY=double.PositiveInfinity;
    private readonly double _maxX=double.NegativeInfinity,_maxY=double.NegativeInfinity;
    private MovableUvChart(Point[][] loops)
    {
        _loops=loops;
        foreach(var loop in loops)
            if(loop.Length>=3)foreach(var point in loop)
            {
                _minX=Math.Min(_minX,point.X);_minY=Math.Min(_minY,point.Y);
                _maxX=Math.Max(_maxX,point.X);_maxY=Math.Max(_maxY,point.Y);
            }
    }

    internal static MovableUvChart Read(int[] integers,double[] coordinates,int offset)
    {
        if(offset<0 || offset>=integers.Length)throw new ArgumentException("Invalid UV chart offset.");
        int count=integers[offset++];
        if(count<0 || count>(integers.Length-offset)/2)throw new ArgumentException("Invalid UV chart loop table.");
        var loops=new Point[count][];
        for(int loop=0;loop<count;loop++)
        {
            int start=integers[offset++],length=integers[offset++];
            if(start<0 || start>coordinates.Length || length<0 || length>(coordinates.Length-start)/2)
                throw new ArgumentException("Invalid UV chart coordinate range.");
            var points=loops[loop]=new Point[length];
            for(int i=0;i<length;i++)
            {
                double x=coordinates[start+i*2],y=coordinates[start+i*2+1];
                if(!double.IsFinite(x) || !double.IsFinite(y))throw new ArgumentException("Non-finite UV chart coordinate.");
                points[i]=new Point(x,y);
            }
        }
        return new MovableUvChart(loops);
    }

    internal bool ContainsTriangle(Vector2 a,Vector2 b,Vector2 c)
    {
        if(!float.IsFinite(a.x) || !float.IsFinite(a.y) || !float.IsFinite(b.x) ||
            !float.IsFinite(b.y) || !float.IsFinite(c.x) || !float.IsFinite(c.y))return false;
        var p=new Point(a.x,a.y);var q=new Point(b.x,b.y);var r=new Point(c.x,c.y);
        if(!Contains(p) || !Contains(q) || !Contains(r) ||
            !ContainsSegment(p,q) || !ContainsSegment(q,r) || !ContainsSegment(r,p))return false;
        double direction=Cross(p,q,r);
        if(direction==0)return true;
        // Edge containment alone misses a hole entirely enclosed by the receiver.
        // No boundary of a filled region may cross its strict triangle interior.
        foreach(var loop in _loops)
        {
            if(loop.Length<3)continue;
            for(int i=0;i<loop.Length;i++)
                if(EntersTriangle(loop[i],loop[(i+1)%loop.Length],p,q,r,direction>0?1:-1))return false;
        }
        return true;
    }

    private bool Contains(Point p)
    {
        if(p.X<_minX || p.X>_maxX || p.Y<_minY || p.Y>_maxY)return false;
        bool inside=false;
        foreach(var loop in _loops)
        {
            // Two-point measured domains contain no area. Keep their data valid,
            // but conservatively leave them on the existing material path.
            if(loop.Length<3)continue;
            for(int i=0;i<loop.Length;i++)
            {
                var a=loop[i];var b=loop[(i+1)%loop.Length];
                double cross=Cross(a,b,p);
                if(cross==0 && Between(p.X,a.X,b.X) && Between(p.Y,a.Y,b.Y))return true;
                if((a.Y>p.Y)!=(b.Y>p.Y) && (cross>0)==(b.Y>a.Y))inside=!inside;
            }
        }
        return inside;
    }

    private bool ContainsSegment(Point a,Point b)
    {
        if(a.X==b.X && a.Y==b.Y)return true;
        var cuts=new List<double>{0,1};
        double dx=b.X-a.X,dy=b.Y-a.Y;
        foreach(var loop in _loops)
        {
            if(loop.Length<3)continue;
            for(int i=0;i<loop.Length;i++)
            {
                var p=loop[i];var q=loop[(i+1)%loop.Length];
                double ex=q.X-p.X,ey=q.Y-p.Y,det=dx*ey-dy*ex;
                if(det!=0)
                {
                    double t=((p.X-a.X)*ey-(p.Y-a.Y)*ex)/det;
                    double u=((p.X-a.X)*dy-(p.Y-a.Y)*dx)/det;
                    if(t>0 && t<1 && u>=0 && u<=1)cuts.Add(t);
                }
                else if(Cross(a,b,p)==0)
                {
                    double t=Math.Abs(dx)>=Math.Abs(dy)?(p.X-a.X)/dx:(p.Y-a.Y)/dy;
                    double u=Math.Abs(dx)>=Math.Abs(dy)?(q.X-a.X)/dx:(q.Y-a.Y)/dy;
                    if(t>0 && t<1)cuts.Add(t);
                    if(u>0 && u<1)cuts.Add(u);
                }
            }
        }
        cuts.Sort();
        for(int i=1;i<cuts.Count;i++)
            if(cuts[i]>cuts[i-1] && !Contains(At(a,b,cuts[i-1]+(cuts[i]-cuts[i-1])*0.5)))return false;
        return true;
    }

    private static bool EntersTriangle(Point p,Point q,Point a,Point b,Point c,int direction)
    {
        double lo=0,hi=1;
        bool Clip(Point x,Point y)
        {
            double v=direction*Cross(x,y,p),w=direction*Cross(x,y,q);
            if(v<=0 && w<=0)return false; // Includes a segment on the boundary.
            if(v<0)lo=Math.Max(lo,v/(v-w));
            if(w<0)hi=Math.Min(hi,v/(v-w));
            return lo<hi;
        }
        if(!Clip(a,b) || !Clip(b,c) || !Clip(c,a))return false;
        var middle=At(p,q,lo+(hi-lo)*0.5);
        return direction*Cross(a,b,middle)>0 && direction*Cross(b,c,middle)>0 && direction*Cross(c,a,middle)>0;
    }
    private static Point At(Point a,Point b,double t)=>new(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
    private static double Cross(Point a,Point b,Point p)=>(b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
    private static bool Between(double x,double a,double b)=>x>=Math.Min(a,b) && x<=Math.Max(a,b);
}
