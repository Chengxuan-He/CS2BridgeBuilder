using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// Five finite native edge charts, not cropped or repeated texture products.
internal static class Draw01MiddleWalkEdges
{
    private readonly struct Chart
    {
        internal readonly Vector3 U, V, Tz, Tc;
        internal readonly float Hand;
        internal readonly Vector2 Min, Max;
        internal Chart(Vector3 u, Vector3 v, Vector3 tz, Vector3 tc, float hand, Vector2 min, Vector2 max)
        {
            U = u;
            V = v;
            Tz = tz;
            Tc = tc;
            Hand = hand;
            Min = min;
            Max = max;
        }
    }

    internal static Partition Create(bool right, bool positive, float scale, float offset, Matrix4x4 frame, SurfaceAsset native, Partition outside, List<MovableExistingRoadSurface.Patch> patches, List<Type> types, Func<Vector4, Partition, Partition, Partition> cut)
    {
        float side = right ? Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[0] : -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[1], sign = positive ? Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[2] : -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[3];
        Partition Add(int id)
        {
            var c = id switch
            {
                0 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[0], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[1], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[2]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[3], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[4], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[5]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[6], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[7], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[8]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[9], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[10], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[11]), -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[12], new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[13], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[14]), new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[15], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[16])),
                1 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[17], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[18], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[19]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[20], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[21], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[22]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[23], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[24], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[25]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[26], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[27], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[28]), Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[29], new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[30], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[31]), new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[32], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[33])),
                2 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[34], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[35], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[36]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[37], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[38], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[39]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[40], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[41], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[42]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[43], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[44], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[45]), Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[46], new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[47], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[48]), new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[49], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[50])),
                3 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[51], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[52], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[53]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[54], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[55], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[56]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[57], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[58], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[59]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[60], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[61], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[62]), Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[63], new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[64], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[65]), new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[66], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[67])),
                4 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[68], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[69], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[70]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[71], -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[72], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[73]), new Vector3(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[74], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[75], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[76]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[77], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[78], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[79]), -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[80], new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[81], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[82]), new Vector2(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[83], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[84])),
                _ => throw new InvalidOperationException("Invalid Draw01 edge chart.")};
            Vector4 UV(Vector3 a) => frame.transpose * new Vector4(a.x * scale, Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[4], a.y, a.x * offset + a.z);
            var derivative = frame.inverse.MultiplyVector(c.Tz);
            var constant = frame.inverse.MultiplyVector(c.Tc);
            Vector4 Field(int axis)
            {
                var r = frame.GetRow(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[5]) * derivative[axis];
                r.w += constant[axis];
                return r;
            }

            int index = patches.Count;
            patches.Add(new MovableExistingRoadSurface.Patch(c.Min, c.Max, UV(c.U), UV(c.V), native, sourceUv: true).WithSourceTangentField(Field(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[6]), Field(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[7]), Field(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[8]), c.Hand));
            types.Add(Type.Sidewalk);
            return new Partition(index);
        }

        Vector4 Boundary(float z0, float x0, float slope) => new Vector4(side * scale, Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[9], -slope * sign, side * offset - x0 + slope * z0);
        float inner = Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[85], outer = Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.F32[86];
        Partition edge;
        if (!right && positive)
        {
            var triangle = cut(Boundary(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[10], inner, (outer - inner) / Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[11]), Add(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[12]), outside);
            var rectangle = cut(Boundary(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[13], outer, Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[14]), Add(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[15]), outside);
            // Z=86 separates different original UV/tangent charts, not merely a reset.
            edge = cut(new Vector4(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[16], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[17], sign, -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[18]), triangle, rectangle);
        }
        else
            edge = cut(Boundary(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[19], inner, (outer - inner) / Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[20]), Add(right ? (positive ? Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[21] : Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[22]) : Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[23]), outside);
        edge = cut(new Vector4(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[24], Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[25], sign, -Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[26]), edge, outside);
        return cut(Boundary(Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[27], inner, Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs.Values.I32[28]), outside, edge);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01MiddleWalkEdges_cs
internal static class Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs
{
    static Numeric_Types_Draw01_Draw01MiddleWalkEdges_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01MiddleWalkEdges_cs");
}
