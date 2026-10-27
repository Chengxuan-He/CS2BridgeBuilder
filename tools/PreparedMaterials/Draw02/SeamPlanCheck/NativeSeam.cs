using System;
using System.Collections.Generic;
using System.Linq;

// Authoring/integration implementation; not installed or invoked by runtime yet.
// Every output vertex stores barycentric coordinates of its original physical face.
internal static class NativeSeam
{
    internal readonly record struct Vertex(double X, double Y, double A, double B, double C)
    {
        internal static Vertex Mix(Vertex a, Vertex b, double t) => new(
            a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,
            a.A+(b.A-a.A)*t,a.B+(b.B-a.B)*t,a.C+(b.C-a.C)*t);
    }
    internal readonly record struct Face(Vertex A, Vertex B, Vertex C, int CellX, int CellY, bool Reflected)
    {
        internal (double X,double Y) Local(Vertex p) => Reflected
            ? (1-(p.X-CellX),1-(p.Y-CellY)) : (p.X-CellX,p.Y-CellY);
    }
    static List<Vertex> Clip(List<Vertex> p, Func<Vertex,double> distance, bool positive)
    {
        var result=new List<Vertex>();
        for(int i=0;i<p.Count;i++)
        {
            var a=p[i];var b=p[(i+1)%p.Count];double da=distance(a),db=distance(b);
            bool ia=positive?da>=0:da<=0,ib=positive?db>=0:db<=0;
            if(ia)result.Add(a);
            if(ia!=ib)result.Add(Vertex.Mix(a,b,da/(da-db)));
        }
        // Exact duplicate endpoints occur where a cut crosses an authored vertex.
        for(int i=result.Count-1;i>=0 && result.Count>1;i--)
            if(result[i].Equals(result[(i+1)%result.Count]))result.RemoveAt(i);
        return result;
    }
    static IEnumerable<List<Vertex>> Slices(List<Vertex> p,int axis)
    {
        double Coord(Vertex v)=>axis==0?v.X:v.Y;
        double lo=p.Min(Coord),hi=p.Max(Coord);
        var rest=p;
        for(double line=Math.Floor(lo)+1;line<hi;line++)
        {
            var low=Clip(rest,v=>Coord(v)-line,false);
            if(low.Count>=3)yield return low;
            rest=Clip(rest,v=>Coord(v)-line,true);
            if(rest.Count<3)yield break;
        }
        yield return rest;
    }
    internal static List<Face> Split(double[][] q,bool triangle,int maximumFaces)
    {
        if(q.Length!=3 || q.Any(v=>v.Length!=2 || v.Any(x=>!double.IsFinite(x))))
            throw new ArgumentException("Three finite source-unit corners are required.");
        // Validate work before enumeration; callers choose their explicit geometry budget.
        if(q.Any(v=>v.Any(x=>Math.Abs(x)>int.MaxValue-2)))throw new ArgumentOutOfRangeException(nameof(q));
        var input=new List<Vertex>{new(q[0][0],q[0][1],1,0,0),new(q[1][0],q[1][1],0,1,0),new(q[2][0],q[2][1],0,0,1)};
        var result=new List<Face>();
        void Emit(List<Vertex> p,int x,int y,bool reflected)
        {
            for(int i=1;i+1<p.Count;i++)
            {
                var a=p[0];var b=p[i];var c=p[i+1];
                // Barycentric area avoids misclassifying narrow valid UV triangles.
                double area=(b.B-a.B)*(c.C-a.C)-(b.C-a.C)*(c.B-a.B);
                if(Math.Abs(area)<=1e-14)continue;
                result.Add(new(a,b,c,x,y,reflected));
                if(maximumFaces>0 && result.Count>maximumFaces)
                    throw new InvalidOperationException("Native seam geometry budget exceeded; no partial result is returned.");
            }
        }
        foreach(var sx in Slices(input,0))foreach(var cell in Slices(sx,1))
        {
            int x=(int)Math.Floor(cell.Average(v=>v.X)),y=(int)Math.Floor(cell.Average(v=>v.Y));
            if(!triangle)Emit(cell,x,y,false);
            else
            {
                Emit(Clip(cell,v=>v.X+v.Y-x-y-1,false),x,y,false);
                Emit(Clip(cell,v=>v.X+v.Y-x-y-1,true),x,y,true);
            }
        }
        return result;
    }
}
