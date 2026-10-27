using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BridgeBuilder.Bridges;

// Independent offline containment oracle, not a replacement production renderer.
internal sealed class MovableUvChart
{
    internal readonly double[][][] Loops;
    private MovableUvChart(double[][][] loops) => Loops=loops;
    internal static MovableUvChart Read(int[] data,double[] values,int offset)
    {
        var loops=new double[data[offset++] ][][];
        for(int i=0;i<loops.Length;i++)
        {
            int at=data[offset++],count=data[offset++];loops[i]=new double[count][];
            for(int j=0;j<count;j++)loops[i][j]=new[]{values[at+j*2],values[at+j*2+1]};
        }
        return new MovableUvChart(loops);
    }
    static double Cross(double[] a,double[] b,double[] p)=>(b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0]);
    bool Point(double[] p)
    {
        bool inside=false;
        foreach(var loop in Loops)
        for(int i=0;i<loop.Length;i++)
        {
            var a=loop[i];var b=loop[(i+1)%loop.Length];
            if(Math.Abs(Cross(a,b,p))<=1e-12&&p[0]>=Math.Min(a[0],b[0])-1e-12&&p[0]<=Math.Max(a[0],b[0])+1e-12&&p[1]>=Math.Min(a[1],b[1])-1e-12&&p[1]<=Math.Max(a[1],b[1])+1e-12)return true;
            if((a[1]>p[1])!=(b[1]>p[1])&&p[0]<(b[0]-a[0])*(p[1]-a[1])/(b[1]-a[1])+a[0])inside=!inside;
        }
        return inside;
    }
    internal bool Contains(double[][] q)
    {
        if(q.Any(p=>!double.IsFinite(p[0])||!double.IsFinite(p[1])||!Point(p)))return false;
        for(int side=0;side<3;side++)
        {
            var a=q[side];var b=q[(side+1)%3];double dx=b[0]-a[0],dy=b[1]-a[1];var ts=new List<double>{0,1};
            foreach(var loop in Loops)
            for(int i=0;i<loop.Length;i++)
            {
                var c=loop[i];var d=loop[(i+1)%loop.Length];double ex=d[0]-c[0],ey=d[1]-c[1],det=dx*ey-dy*ex;
                if(det==0)continue;
                double t=((c[0]-a[0])*ey-(c[1]-a[1])*ex)/det,u=((c[0]-a[0])*dy-(c[1]-a[1])*dx)/det;
                if(t>0&&t<1&&u>=0&&u<=1)ts.Add(t);
            }
            ts.Sort();for(int i=1;i<ts.Count;i++){double t=(ts[i-1]+ts[i])*.5;if(!Point(new[]{a[0]+dx*t,a[1]+dy*t}))return false;}
        }
        // A hole fully enclosed by the receiver must not be missed by edge tests.
        foreach(var p in Loops.SelectMany(x=>x))
        {
            double a=Cross(q[0],q[1],p),b=Cross(q[1],q[2],p),c=Cross(q[2],q[0],p);
            if(a>1e-12&&b>1e-12&&c>1e-12||a< -1e-12&&b< -1e-12&&c< -1e-12)return false;
        }
        return true;
    }
}
