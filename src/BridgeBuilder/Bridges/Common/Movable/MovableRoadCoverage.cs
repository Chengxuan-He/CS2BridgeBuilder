using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using UnityEngine;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Native piece meshes may extend past their nominal width. A sidewalk running
// surface owns its footprint where an adjoining carriageway also supplies it.
internal static class MovableRoadCoverage
{
    internal static Triangle[] Resolve(IReadOnlyList<Triangle> source, bool[] walks)
        => ResolveFaces(source,t => walks[t.Occurrence],_ => false);

    internal static Triangle[] ResolveFaces(IReadOnlyList<Triangle> source, Func<Triangle,bool> walk,
        Func<Triangle,bool> curb)
    {
        var sides = source.Where(t => walk(t) && Up(t)).ToArray();
        if (sides.Length == 0) return source.ToArray();
        var result = new List<Triangle>(source.Count);
        foreach (var face in source)
        {
            if (walk(face) || curb(face) || !Up(face)) { result.Add(face); continue; }
            List<Triangle>? fragments = null;
            foreach (var side in sides)
            {
                if (!IntersectsBounds(face, side) || !SamePlane(face, side)) continue;
                var next = new List<Triangle>();
                if (fragments == null)
                {
                    if (!Subtract(face, side, next)) continue;
                }
                else
                    foreach (var part in fragments)
                        if (!Subtract(part, side, next)) next.Add(part);
                fragments = next;
                if (fragments.Count == 0) break;
            }
            if (fragments == null) result.Add(face); else result.AddRange(fragments);
        }
        return result.ToArray();
    }

    private static bool Up(Triangle t) =>
        t.A.Normal.y > .7f && t.B.Normal.y > .7f && t.C.Normal.y > .7f &&
        Cross(t.A.Position,t.B.Position,t.C.Position) != 0;

    private static bool IntersectsBounds(Triangle a, Triangle b)
    {
        bool Overlap(float x,float y,float z,float p,float q,float r) =>
            Math.Max(x,Math.Max(y,z)) > Math.Min(p,Math.Min(q,r)) &&
            Math.Max(p,Math.Max(q,r)) > Math.Min(x,Math.Min(y,z));
        return Overlap(a.A.Position.x,a.B.Position.x,a.C.Position.x,b.A.Position.x,b.B.Position.x,b.C.Position.x) &&
            Overlap(a.A.Position.z,a.B.Position.z,a.C.Position.z,b.A.Position.z,b.B.Position.z,b.C.Position.z);
    }

    private static bool SamePlane(Triangle a, Triangle b)
    {
        var n = Vector3.Cross(a.B.Position-a.A.Position,a.C.Position-a.A.Position).normalized;
        // Native composition introduces sub-0.1 mm differences between coincident
        // edge/node meshes. Distinct elevations and curb sidewalls retain both surfaces.
        bool On(Vector3 p) => Math.Abs(Vector3.Dot(n,p-a.A.Position)) <= .0001f;
        return Math.Abs(n.y) > .7f && On(b.A.Position) && On(b.B.Position) && On(b.C.Position);
    }

    private static double Cross(Vector3 a,Vector3 b,Vector3 p) =>
        ((double)b.x-a.x)*((double)p.z-a.z)-((double)b.z-a.z)*((double)p.x-a.x);

    private static bool Subtract(Triangle t, Triangle cover, List<Triangle> output)
    {
        if (!IntersectsBounds(t,cover)) return false;
        var edge = new[]{cover.A.Position,cover.B.Position,cover.C.Position};
        return Clip(t,edge,output,null);
    }

    internal static IEnumerable<(BoundTriangle Face,bool Colored)> SplitColors(BoundTriangle face,
        MovableNativeDecals.Region[] regions)
    {
        if (regions.Length==0 || !Up(face.Geometry)) { yield return (face,false); yield break; }
        var pending=new List<Triangle>{face.Geometry};
        foreach (var r in regions)
        {
            var next=new List<Triangle>();
            foreach (var part in pending)
            {
                if (!r.Overlaps(part)) {next.Add(part);continue;}
                var x=r.Right*r.Half.x; var z=r.Forward*r.Half.z;
                var corners=new[]{r.Center-x-z,r.Center+x-z,r.Center+x+z,r.Center-x+z};
                var colored=new List<Triangle>();
                if (!Clip(part,corners,next,colored)) next.Add(part);
                else foreach (var cut in colored) yield return (new BoundTriangle(cut,face.Bone),true);
            }
            pending=next;
        }
        foreach(var part in pending) yield return (new BoundTriangle(part,face.Bone),false);
    }

    private static bool Clip(Triangle t,Vector3[] edge,List<Triangle> output,List<Triangle>? covered)
    {
        var sign = Math.Sign(Cross(edge[0],edge[1],edge[2]));
        var inside = new List<Vertex>{t.A,t.B,t.C};
        var pieces = new List<List<Vertex>>(3);
        for (int i=0;i<edge.Length;i++)
        {
            var left = new List<Vertex>(); var right = new List<Vertex>();
            var a = edge[i]; var b = edge[(i+1)%edge.Length];
            var prev=inside[inside.Count-1]; double before=sign*Cross(a,b,prev.Position);
            foreach (var current in inside)
            {
                double after=sign*Cross(a,b,current.Position);
                if ((before<0 && after>0)||(before>0 && after<0))
                {
                    var point=Vertex.Interpolate(prev,current,(float)(before/(before-after)));
                    left.Add(point); right.Add(point);
                }
                if (after>=0) left.Add(current);
                if (after<=0) right.Add(current);
                prev=current; before=after;
            }
            if (left.Count<3) return false;
            if (right.Count>=3) pieces.Add(right);
            inside=left;
        }
        if (!Enumerable.Range(1,inside.Count-2).Any(i=>Cross(inside[0].Position,inside[i].Position,inside[i+1].Position)!=0)) return false;
        if (covered!=null) Emit(inside,covered);
        foreach(var polygon in pieces) Emit(polygon,output);
        return true;

        void Emit(List<Vertex> polygon,List<Triangle> result)
        {
            for (int i=1;i+1<polygon.Count;i++)
                if (Cross(polygon[0].Position,polygon[i].Position,polygon[i+1].Position)!=0)
                    result.Add(new Triangle(polygon[0],polygon[i],polygon[i+1],t.Material,t.Occurrence,t.SourceTriangle));
        }
    }
}
