using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// A measured planar appearance domain. Membership and frame vertices are chosen
// offline; this class only evaluates the supplied source/generated coordinates.
// Its output is in source-atlas UV units, before normalizing a repeating crop.
internal readonly struct MovableUvFrame
{
    private readonly D3 _sourceOrigin, _sourceX, _sourceY;
    private readonly D3 _targetOrigin, _targetX, _targetY;
    private readonly Vector2 _uvX, _uvY;
    private readonly double _sourceLength, _sourceCx, _sourceCy;
    internal readonly bool HasStretch;

    internal MovableUvFrame(Vector3 sourceA, Vector3 sourceB, Vector3 sourceC,
        Vector2 uvA, Vector2 uvB, Vector2 uvC,
        Vector3 targetA, Vector3 targetB, Vector3 targetC)
    {
        _sourceOrigin = sourceA; _targetOrigin = targetA;
        Frame(sourceA, sourceB, sourceC, out _sourceX, out _sourceY, out var length, out var x, out var y);
        Frame(targetA, targetB, targetC, out var targetAxisX, out var targetAxisY, out var targetLength, out var targetX, out var targetY);
        PolarRotation(length,x,y,targetLength,targetX,targetY,out var cosine,out var sine);
        _targetX=targetAxisX*cosine+targetAxisY*sine;
        _targetY=targetAxisY*cosine-targetAxisX*sine;
        _sourceLength=length; _sourceCx=x; _sourceCy=y;
        HasStretch=Math.Abs(targetLength-length)>length*1e-7 ||
            Math.Abs(targetX-x)>length*1e-7 || Math.Abs(targetY-y)>y*1e-7;
        _uvX = (uvB - uvA) / (float)length;
        _uvY = (uvC - uvA - _uvX * (float)x) / (float)y;
        if (!Finite(_uvX) || !Finite(_uvY))
            throw new ArgumentException("A measured UV frame has non-finite source gradients.");
    }

    // Retain authored per-vertex residuals instead of replacing a quantized native
    // UV channel with a fitted plane. A rigid translation is exactly a zero delta.
    internal Vector2 Map(Vector3 source, Vector3 target, Vector2 uv)
    {
        var a = (D3)source - _sourceOrigin;
        var b = (D3)target - _targetOrigin;
        return uv + _uvX * (float)(D3.Dot(b, _targetX) - D3.Dot(a, _sourceX))
                  + _uvY * (float)(D3.Dot(b, _targetY) - D3.Dot(a, _sourceY));
    }

    internal Vector3 Rotate(Vector3 direction) =>
        (Vector3)(_targetX * D3.Dot(direction, _sourceX) +
        _targetY * D3.Dot(direction, _sourceY) +
        D3.Cross(_targetX, _targetY) * D3.Dot(direction, D3.Cross(_sourceX, _sourceY)));

    // A mixed sampler changes the UV Jacobian independently of the geometric
    // polar rotation. Rotate the authored tangent's residual relative to dP/dU
    // into the final sampler basis, preserving its smooth-normal convention.
    internal static Vector4 MixedTangent(MovableUvFrame frame,Vector3 tangent,Vector3 normal,float handedness,
        Vector3[] source,Vector2[] sourceUv,Vector3[] target,Vector2[] targetUv)
    {
        Vector3 Derivative(Vector3[] p,Vector2[] uv,out double determinant)
        {
            double ux=(double)uv[1].x-uv[0].x,uy=(double)uv[1].y-uv[0].y;
            double vx=(double)uv[2].x-uv[0].x,vy=(double)uv[2].y-uv[0].y;
            determinant=ux*vy-uy*vx;
            if(!double.IsFinite(determinant)||determinant==0)
                throw new InvalidOperationException("Independent UV axes require a nonsingular final tangent basis.");
            return (Vector3)(((D3)p[1]-p[0])*(vy/determinant)-((D3)p[2]-p[0])*(uy/determinant));
        }
        var oldDirection=frame.Rotate(Derivative(source,sourceUv,out var oldDet));
        var newDirection=Derivative(target,targetUv,out var newDet);
        oldDirection=(oldDirection-normal*Vector3.Dot(normal,oldDirection)).normalized;
        newDirection=(newDirection-normal*Vector3.Dot(normal,newDirection)).normalized;
        if(oldDirection.sqrMagnitude==0||newDirection.sqrMagnitude==0)
            throw new InvalidOperationException("Independent UV tangent is parallel to its native normal.");
        float cosine=Vector3.Dot(oldDirection,newDirection);
        float sine=Vector3.Dot(normal,Vector3.Cross(oldDirection,newDirection));
        var result=(tangent*cosine+Vector3.Cross(normal,tangent)*sine+
            normal*Vector3.Dot(normal,tangent)*(1-cosine)).normalized;
        return new Vector4(result.x,result.y,result.z,oldDet*newDet<0?-handedness:handedness);
    }

    // Unit coordinates of the source physical triangle, evaluated in the target
    // orthonormal frame. No UV inverse is needed: rank-one source UVs remain valid.
    internal Vector2 TriangleUnit(Vector3 target)
    {
        var p=(D3)target-_targetOrigin;
        double v=D3.Dot(p,_targetY)/_sourceCy;
        return new Vector2((float)((D3.Dot(p,_targetX)-v*_sourceCx)/_sourceLength),(float)v);
    }

    // Pivot an existing triangle's parameterization onto its shortest edge. For
    // thin recorded fragments this keeps adjacent output UVs distinct in Float32
    // instead of subtracting nearly equal long-edge barycentric coordinates.
    // This changes neither geometry nor membership nor the source sampling map.
    internal static int TrianglePivot(Vector3 a,Vector3 b,Vector3 c)
    {
        double ab=((D3)b-a).Length,bc=((D3)c-b).Length,ca=((D3)a-c).Length;
        return bc<ab && bc<=ca?1:ca<ab && ca<bc?2:0;
    }

    // The target edge's QR frame rotates when an oblique edge is stretched.
    // Remove that arbitrary rotation using the polar factor of the measured
    // in-plane deformation. Pure axis scaling then retains source orientation;
    // rigid motion follows its rotation, and the oriented target normal remains.
    internal static void PolarRotation(double sourceLength,double sourceX,double sourceY,
        double targetLength,double targetX,double targetY,out double cosine,out double sine)
    {
        double xx=targetLength/sourceLength,xy=(targetX-xx*sourceX)/sourceY,yy=targetY/sourceY;
        double real=xx+yy,imaginary=-xy;
        double magnitude=Math.Sqrt(real*real+imaginary*imaginary);
        if(!double.IsFinite(magnitude)||magnitude==0)
            throw new ArgumentException("A measured UV deformation requires a finite polar frame.");
        cosine=real/magnitude;sine=imaginary/magnitude;
    }

    internal static Vector2Int TrianglePixels(Vector2 a,Vector2 b,Vector2 c,int width,int height)
    {
        var u=Vector2.Scale(b-a,new Vector2(width,height));
        var v=Vector2.Scale(c-a,new Vector2(width,height));
        float x=u.magnitude,y=v.magnitude;
        // Column-length sizing alone loses diagonal detail when the two sample
        // axes are oblique. Bound the largest singular footprint by one texel.
        float scale=x>0 && y>0?Mathf.Sqrt(1+Mathf.Abs(Vector2.Dot(u,v)/(x*y))):1;
        return new Vector2Int(Math.Max(4,4*Mathf.CeilToInt(x*scale/4)),Math.Max(4,4*Mathf.CeilToInt(y*scale/4)));
    }

    private static bool Finite(Vector2 v) => float.IsFinite(v.x) && float.IsFinite(v.y);
    private static void Frame(D3 a, D3 b, D3 c,
        out D3 xAxis, out D3 yAxis, out double length, out double x, out double y)
    {
        var edge = b - a; var other = c - a;
        length = edge.Length;
        var normal = D3.Cross(edge, other);
        var area = normal.Length;
        if (!double.IsFinite(length) || !double.IsFinite(area) || length <= 0 || area <= 0)
            throw new ArgumentException("A measured UV frame requires a finite nondegenerate source and target plane.");
        xAxis = edge / length;
        yAxis = D3.Cross(normal / area, xAxis);
        x = D3.Dot(other, xAxis);
        y = area / length;
    }

    // Very narrow recorded fragments need double intermediates. Output vertex
    // channels remain Float32; no source coordinate or numeric resource changes.
    private readonly struct D3
    {
        private readonly double X,Y,Z;
        private D3(double x,double y,double z){X=x;Y=y;Z=z;}
        internal double Length=>Math.Sqrt(Dot(this,this));
        internal static double Dot(D3 a,D3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        internal static D3 Cross(D3 a,D3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        public static implicit operator D3(Vector3 p)=>new(p.x,p.y,p.z);
        public static explicit operator Vector3(D3 p)=>new((float)p.X,(float)p.Y,(float)p.Z);
        public static D3 operator +(D3 a,D3 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static D3 operator -(D3 a,D3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static D3 operator *(D3 a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
        public static D3 operator /(D3 a,double b)=>a*(1/b);
    }
}
