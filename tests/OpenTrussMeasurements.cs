using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Bridges;
using Unity.Mathematics;
namespace BridgeBuilder.Tests;
// Independent offline measurements. No expected width or transformation profile enters this oracle.
internal sealed class OpenTrussMeasurements
{
    internal int SpanningPieces, RigidPieces, DegenerateBefore, DegenerateAfter, FlippedTriangles;
    internal float MeasuredWidthChange;
    internal bool Finite;
    internal static OpenTrussMeasurements Measure(float3[] source, float3[] output, IReadOnlyList<int> triangles)
    {
        var pieces = TowerWidening.PiecesOf(source, triangles, out _);
        var result = new OpenTrussMeasurements {
            SpanningPieces = pieces.Count(p => p.Left <= 0f && p.Right >= 0f),
            RigidPieces = pieces.Count(p => p.Left > 0f || p.Right < 0f),
            MeasuredWidthChange = (output.Max(p => p.x) - output.Min(p => p.x)) - (source.Max(p => p.x) - source.Min(p => p.x)),
            Finite = output.All(p => Valid(p.x) && Valid(p.y) && Valid(p.z))
        };
        for (int i = 0; i < triangles.Count; i += 3)
        {
            var before = Normal(source[triangles[i]],source[triangles[i+1]],source[triangles[i+2]]);
            var after = Normal(output[triangles[i]],output[triangles[i+1]],output[triangles[i+2]]);
            double a=Dot(before,before), b=Dot(after,after);
            if(a<=1e-12)result.DegenerateBefore++;
            if(b<=1e-12)result.DegenerateAfter++;
            if(a>1e-12&&b>1e-12&&Dot(before,after)<0)result.FlippedTriangles++;
        }
        return result;
    }
    private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static (double x,double y,double z) Normal(float3 a,float3 b,float3 c)
    {
        double ux=(double)b.x-a.x,uy=(double)b.y-a.y,uz=(double)b.z-a.z;
        double vx=(double)c.x-a.x,vy=(double)c.y-a.y,vz=(double)c.z-a.z;
        return (uy*vz-uz*vy,uz*vx-ux*vz,ux*vy-uy*vx);
    }
    private static double Dot((double x,double y,double z) a,(double x,double y,double z) b) => a.x*b.x+a.y*b.y+a.z*b.z;
}
