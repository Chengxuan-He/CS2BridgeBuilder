using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured domains describe UV frames, not evidence of periodic texture content.
// Only separately evidenced semantic units may supply a repeating native material.
internal static class MovableStructurePacking
{
    internal sealed class Unit
    {
        internal int Id;
        internal bool Triangle;
        // A numerically conditioned integer phase is valid for the repeat
        // texture, not for direct atlas lookup. Preserve that distinction.
        internal bool RepeatOnly;
        internal int RetainedAxes;
        internal MovableUvChart[] Charts=Array.Empty<MovableUvChart>();
        // Constructed faces retain their unconditioned double-precision parent
        // coordinates. A phase chosen for a Repeat page is never an atlas phase.
        internal double[][]? ExactPoints;
        internal Dictionary<Vector2,Vector2>? ExactSourceUvs;
        // Unconditioned parent coordinates survive even when direct atlas reuse
        // fails. A genuine region may have a different period from this domain.
        internal Dictionary<Vector2,double[]>? ExactCoordinates;
        internal Vector4 Bounds;
        internal Vector2 A,B,C,Min,Max;
        internal readonly List<Vector2> Points=new();
        // An affine triangle whose corners stay inside its authored sampler needs
        // no repetition. The original atlas already contains every sample: keep
        // that material and express the density-preserving coordinates in its UVs.
        internal bool CanUseSource
        {
            get
            {
                if(RepeatOnly || Points.Count==0 || Points.Count%3!=0 || !Points.All(p =>
                    float.IsFinite(p.x) && float.IsFinite(p.y)))return false;
                if(Charts.Length==0 && !Points.All(p=>p.x>=0 && p.y>=0 &&
                    (Triangle ? (double)p.x+p.y<=1 : p.x<=1 && p.y<=1)))return false;
                // Absolute atlas coordinates can quantize away the gradient of a
                // tiny authored face. Test the stored Float32 mapping, not merely
                // containment. Retain local repeat coordinates when that mapping
                // cannot represent the source density in both directions.
                for(int at=0;at<Points.Count;at+=3)
                {
                    var a=Points[at];var b=Points[at+1];var c=Points[at+2];
                    if(Charts.Length!=0 && !Charts.Any(chart=>chart.ContainsTriangle(SourceUv(a),SourceUv(b),SourceUv(c))))return false;
                    if(!PreservesGradient(a,b,c,at))return false;
                }
                return true;
            }
        }
        private bool PreservesGradient(Vector2 a,Vector2 b,Vector2 c,int at)
        {
            double ux=Triangle?(double)B.x-A.x:(double)Bounds.z-Bounds.x;
            double uy=Triangle?(double)B.y-A.y:0;
            double vx=Triangle?(double)C.x-A.x:0;
            double vy=Triangle?(double)C.y-A.y:(double)Bounds.w-Bounds.y;
            double bx=(double)b.x-a.x,by=(double)b.y-a.y,cx=(double)c.x-a.x,cy=(double)c.y-a.y;
            if(ExactPoints!=null)
            {
                bx=ExactPoints[at+1][0]-ExactPoints[at][0];by=ExactPoints[at+1][1]-ExactPoints[at][1];
                cx=ExactPoints[at+2][0]-ExactPoints[at][0];cy=ExactPoints[at+2][1]-ExactPoints[at][1];
            }
            double ex=ux*bx+vx*by,ey=uy*bx+vy*by,fx=ux*cx+vx*cy,fy=uy*cx+vy*cy;
            var sa=SourceUv(a);var sb=SourceUv(b);var sc=SourceUv(c);
            double rx=(double)sb.x-sa.x,ry=(double)sb.y-sa.y,sx=(double)sc.x-sa.x,sy=(double)sc.y-sa.y;
            double determinant=ex*fy-fx*ey;
            if(determinant==0)return rx==ex && ry==ey && sx==fx && sy==fy;
            // Rounded mapping times inverse intended mapping must be identity.
            // This bounds relative gradient error independently of face scale;
            // it does not classify parts or change their geometry.
            double a00=(rx*fy-sx*ey)/determinant-1,a01=(sx*ex-rx*fx)/determinant;
            double a10=(ry*fy-sy*ey)/determinant,a11=(sy*ex-ry*fx)/determinant-1;
            return a00*a00+a01*a01+a10*a10+a11*a11<=1e-8;
        }
        internal Vector2 SourceUv(Vector2 value) => ExactSourceUvs!=null && ExactSourceUvs.TryGetValue(value,out var exact)
            ? exact : SourceUv(value.x,value.y);
        internal Vector2 SourceUv(double x,double y) => Triangle
            ? new Vector2((float)(A.x+((double)B.x-A.x)*x+((double)C.x-A.x)*y),
                (float)(A.y+((double)B.y-A.y)*x+((double)C.y-A.y)*y))
            : new Vector2((float)(Bounds.x+((double)Bounds.z-Bounds.x)*x),
                (float)(Bounds.y+((double)Bounds.w-Bounds.y)*y));
    }
    internal sealed class Cell
    {
        internal readonly Unit Unit;
        internal int[] Ids=Array.Empty<int>();
        internal int X=>0;
        internal int Y=>0;
        internal int W,H;
        internal bool Repeat=>true;
        internal Vector2Int RepeatPixels=>new(W,H);
        internal Cell(Unit unit){Unit=unit;}
        // Continuous repeat coordinates: modulo belongs in the native sampler,
        // not at vertices or at newly cut geometry boundaries.
        internal Vector2 Map(Vector2 uv,int width,int height)=>uv;
        internal Vector2 Decode(Vector2 uv,int width,int height)=>uv;
        internal Vector2 SampleOrigin=>Vector2.zero;
        internal Vector2 SampleX=>new(1f/W,0);
        internal Vector2 SampleY=>new(0,1f/H);
    }
    internal sealed class Page
    {
        internal readonly List<Cell> Cells=new();
        internal int Width,Height;
        internal bool Repeat=>true;
    }
    internal static List<Page> Pack(IEnumerable<Unit> units,IReadOnlyList<Vector2Int> channels,int limit)
    {
        if(channels.Count==0 || channels.Any(s=>s.x<=0 || s.y<=0) || limit<4)
            throw new ArgumentException("Structural repeats require valid source texture dimensions.");
        var pages=new List<Page>();
        foreach(var group in units.GroupBy(u=>(u.Triangle,u.Bounds,u.A,u.B,u.C)))
        {
            var first=group.First();
            var unit=new Unit{Id=first.Id,Triangle=first.Triangle,Bounds=first.Bounds,A=first.A,B=first.B,C=first.C,
                Min=group.Select(u=>u.Min).Aggregate(Vector2.Min),Max=group.Select(u=>u.Max).Aggregate(Vector2.Max)};
            unit.Points.AddRange(group.SelectMany(u=>u.Points).Distinct());
            var x=unit.Triangle?unit.B-unit.A:new Vector2(unit.Bounds.z-unit.Bounds.x,0);
            var y=unit.Triangle?unit.C-unit.A:new Vector2(0,unit.Bounds.w-unit.Bounds.y);
            var footprint=new MovableTextureFootprint();
            foreach(var size in channels)
            {
                var pixels=new Vector2(size.x,size.y);
                footprint.Include(Vector2.Scale(x,pixels),Vector2.Scale(y,pixels));
            }
            // Bound the complete source-pixel metric, including oblique triangular
            // units. Round upward for BC7 block alignment; never reduce density.
            int Size(float pixels)
            {
                if(!float.IsFinite(pixels) || pixels<0)
                    throw new InvalidOperationException("Invalid source structural texture footprint.");
                return checked(Math.Max(4,4*(int)Math.Ceiling(pixels/4)));
            }
            var density=footprint.NativeDensity;
            int width=Size(density.x),height=Size(density.y);
            if(width>limit || height>limit)
                throw new InvalidOperationException("A source structural unit exceeds native texture dimensions.");
            var cell=new Cell(unit){W=width,H=height,Ids=group.Select(u=>u.Id).ToArray()};
            var page=new Page{Width=width,Height=height};
            page.Cells.Add(cell);pages.Add(page);
        }
        return pages;
    }
}
