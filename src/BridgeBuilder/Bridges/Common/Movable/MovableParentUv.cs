using System;
using System.Linq;
using UnityEngine;

// Evaluate a constructed face in its immutable parent sampler. Integer repeat
// phases condition Float32 coordinates without changing the authored pattern.
namespace BridgeBuilder.Bridges;

internal static class MovableParentUv
{
    internal sealed class Result
    {
        internal readonly Vector2[] Points;
        internal readonly double[][] Exact;
        internal readonly double OffsetX,OffsetY;
        internal Result(Vector2[] points,double[][] exact,double offsetX,double offsetY)
        {Points=points;Exact=exact;OffsetX=offsetX;OffsetY=offsetY;}

        internal Vector2[] Select(MovableStructurePacking.Unit unit)
        {
            // Check full original-material reuse before changing repeat phase.
            var original=Exact.Select(q=>new Vector2((float)q[0],(float)q[1])).ToArray();
            unit.ExactPoints=Exact;
            unit.ExactSourceUvs=new();
            bool consistent=true;
            for(int i=0;i<original.Length;i++)
            {
                var uv=unit.SourceUv(Exact[i][0],Exact[i][1]);
                if(unit.ExactSourceUvs.TryGetValue(original[i],out var previous) && !previous.Equals(uv))consistent=false;
                unit.ExactSourceUvs[original[i]]=uv;
            }
            unit.Points.AddRange(original);
            bool reuse=consistent && unit.CanUseSource;
            unit.Points.Clear();
            unit.RepeatOnly=!reuse;
            if(!reuse){unit.ExactPoints=null;unit.ExactSourceUvs=null;}
            var selected=reuse?original:Points;
            unit.ExactCoordinates=new();
            for(int i=0;i<selected.Length;i++)
            {
                if(unit.ExactCoordinates.TryGetValue(selected[i],out var previous) &&
                    (previous[0]!=Exact[i][0] || previous[1]!=Exact[i][1]))
                { unit.ExactCoordinates=null;break; }
                unit.ExactCoordinates[selected[i]]=Exact[i];
            }
            return selected;
        }
    }

    internal static Result Map(Vector3[] source, Vector2[] uv, Vector3[] target,
        Vector3[] parent, Vector2[] parentUv, bool triangle, Vector4 bounds)
    {
        int pivot=MovableUvFrame.TrianglePivot(source[0],source[1],source[2]);
        var order=new[]{pivot,(pivot+1)%3,(pivot+2)%3};
        double[] Difference(Vector3 a,Vector3 b)=>Enumerable.Range(0,3).Select(i=>(double)a[i]-b[i]).ToArray();
        double Dot(double[] a,double[] b)=>a.Zip(b,(x,y)=>x*y).Sum();
        var e=Difference(parent[1],parent[0]);var f=Difference(parent[2],parent[0]);
        // Cross products avoid cancellation in a thin parent's Gram determinant.
        double[] Cross(double[] a,double[] b)=>new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
        double[] Normalize(double[] a)=>a.Select(x=>x/Math.Sqrt(Dot(a,a))).ToArray();
        var sourceEdge=Difference(source[order[1]],source[order[0]]);
        var sourceOther=Difference(source[order[2]],source[order[0]]);
        double sourceLength=Math.Sqrt(Dot(sourceEdge,sourceEdge));
        double sourceCx=Dot(sourceOther,sourceEdge)/sourceLength;
        var sourceNormal=Cross(sourceEdge,sourceOther);
        double sourceCy=Math.Sqrt(Dot(sourceNormal,sourceNormal))/sourceLength;
        var targetEdge=Difference(target[order[1]],target[order[0]]);
        var targetOther=Difference(target[order[2]],target[order[0]]);
        var targetX=Normalize(targetEdge);
        var targetY=Cross(Normalize(Cross(targetEdge,targetOther)),targetX);
        MovableUvFrame.PolarRotation(sourceLength,sourceCx,sourceCy,
            Math.Sqrt(Dot(targetEdge,targetEdge)),Dot(targetOther,targetX),Dot(targetOther,targetY),
            out double cosine,out double sine);
        var sampleX=targetX.Zip(targetY,(x,y)=>x*cosine+y*sine).ToArray();
        var sampleY=targetY.Zip(targetX,(y,x)=>y*cosine-x*sine).ToArray();
        var n=Cross(e,f);double determinant=Dot(n,n);
        var mapped=target.Select(p=>
        {
            var offset=Difference(p,target[order[0]]);
            double qy=Dot(offset,sampleY)/sourceCy;
            double qx=(Dot(offset,sampleX)-qy*sourceCx)/sourceLength;
            double U(int axis)=>uv[order[0]][axis]+((double)uv[order[1]][axis]-uv[order[0]][axis])*qx+
                ((double)uv[order[2]][axis]-uv[order[0]][axis])*qy;
            if(!triangle)
            {
                return new[]{(U(0)-bounds.x)/((double)bounds.z-bounds.x),
                    (U(1)-bounds.y)/((double)bounds.w-bounds.y)};
            }
            double ux=(double)parentUv[1].x-parentUv[0].x,uy=(double)parentUv[1].y-parentUv[0].y;
            double vx=(double)parentUv[2].x-parentUv[0].x,vy=(double)parentUv[2].y-parentUv[0].y;
            double dx=U(0)-parentUv[0].x,dy=U(1)-parentUv[0].y,uvDeterminant=ux*vy-uy*vx;
            if(uvDeterminant!=0)
                return new[]{(dx*vy-dy*vx)/uvDeterminant,(ux*dy-uy*dx)/uvDeterminant};
            var r=Enumerable.Range(0,3).Select(i=>(double)source[order[0]][i]-parent[0][i]+
                ((double)source[order[1]][i]-source[order[0]][i])*qx+
                ((double)source[order[2]][i]-source[order[0]][i])*qy).ToArray();
            return new[]{Dot(Cross(r,f),n)/determinant,Dot(Cross(e,r),n)/determinant};
        }).ToArray();
        // Compare equivalent integer Repeat phases before Float32 storage. A
        // blanket origin shift improves some thin faces but worsens others.
        // Choose the representation with the smallest physical gradient error;
        // this is numerical conditioning, not a change in sampling or density.
        double targetLength=Math.Sqrt(Dot(targetEdge,targetEdge));
        double targetCx=Dot(targetOther,targetX),targetCy=Dot(targetOther,targetY);
        double[] Gradient(double[][] points)
        {
            var result=new double[4];
            for(int axis=0;axis<2;axis++)
            {
                double du=triangle?(double)parentUv[1][axis]-parentUv[0][axis]:axis==0?(double)bounds.z-bounds.x:0;
                double dv=triangle?(double)parentUv[2][axis]-parentUv[0][axis]:axis==1?(double)bounds.w-bounds.y:0;
                double DifferenceAt(int i)=>du*(points[order[i]][0]-points[order[0]][0])+dv*(points[order[i]][1]-points[order[0]][1]);
                double x=DifferenceAt(1)/targetLength;
                result[axis*2]=x;result[axis*2+1]=(DifferenceAt(2)-x*targetCx)/targetCy;
            }
            return result;
        }
        var expected=Gradient(mapped);
        Vector2[]? best=null;double bestError=double.PositiveInfinity,bestX=0,bestY=0;
        foreach(double ox in new[]{0,Math.Round(mapped[0][0])}.Distinct())
        foreach(double oy in new[]{0,Math.Round(mapped[0][1])}.Distinct())
        {
            var candidate=mapped.Select(q=>new Vector2((float)(q[0]-ox),(float)(q[1]-oy))).ToArray();
            var gradient=Gradient(candidate.Select(q=>new[]{(double)q.x,q.y}).ToArray());
            double error=gradient.Zip(expected,(a,b)=>(a-b)*(a-b)).Sum();
            if(error<bestError){best=candidate;bestError=error;bestX=ox;bestY=oy;}
        }
        return new Result(best??throw new InvalidOperationException("Non-finite parent sampler mapping."),mapped,bestX,bestY);
    }
}
