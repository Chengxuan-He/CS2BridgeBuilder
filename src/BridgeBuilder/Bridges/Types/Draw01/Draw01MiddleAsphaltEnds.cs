using System;
using System.Collections.Generic;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
internal static class Draw01MiddleAsphaltEnds
{
    private readonly struct Chart
    {
        internal readonly Vector3 U, V, T;
        internal readonly float Hand, End, Edge, Width;
        internal readonly Vector2 Min, Max;
        internal Chart(Vector3 u, Vector3 v, Vector3 t, float hand, float end, float edge, float width, Vector2 min, Vector2 max)
        {
            U = u;
            V = v;
            T = t;
            Hand = hand;
            End = end;
            Edge = edge;
            Width = width;
            Min = min;
            Max = max;
        }
    }

    internal static Partition Create(bool right, bool positive, float minX, float maxX, Matrix4x4 frame, Partition outside, List<MovableExistingRoadSurface.Patch> patches, List<Type> types, Func<Vector4, Partition, Partition, Partition> cut)
    {
        float side = right ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[0] : -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[1], sign = positive ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[2] : -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[3];
        Partition End(bool last, Partition fallback)
        {
            var c = ((positive ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[4] : Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[5]) + (right ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[6] : Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[7]) + (last ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[8] : Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[9])) switch
            {
                0 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[0], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[1], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[2]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[3], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[4], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[5]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[6], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[7], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[8]), Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[9], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[10], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[11], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[12], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[13], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[14]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[15], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[16])),
                1 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[17], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[18], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[19]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[20], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[21], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[22]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[23], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[24], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[25]), Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[26], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[27], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[28], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[29], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[30], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[31]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[32], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[33])),
                4 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[34], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[35], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[36]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[37], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[38], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[39]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[40], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[41], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[42]), -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[43], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[44], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[45], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[46], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[47], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[48]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[49], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[50])),
                5 => new Chart(new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[51], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[52], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[53]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[54], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[55], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[56]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[57], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[58], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[59]), -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[60], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[61], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[62], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[63], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[64], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[65]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[66], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[67])),
                2 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[68], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[69], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[70]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[71], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[72], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[73]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[74], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[75], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[76]), Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[77], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[78], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[79], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[80], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[81], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[82]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[83], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[84])),
                3 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[85], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[86], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[87]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[88], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[89], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[90]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[91], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[92], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[93]), Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[94], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[95], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[96], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[97], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[98], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[99]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[100], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[101])),
                6 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[102], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[103], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[104]), new Vector3(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[105], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[106], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[107]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[108], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[109], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[110]), -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[111], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[112], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[113], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[114], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[115], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[116]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[117], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[118])),
                7 => new Chart(new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[119], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[120], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[121]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[122], -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[123], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[124]), new Vector3(-Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[125], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[126], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[127]), -Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[128], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[129], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[130], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[131], new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[132], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[133]), new Vector2(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[134], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.F32[135])),
                _ => throw new InvalidOperationException("Invalid Draw01 finite asphalt end.")};
            float scale = c.Width / (maxX - minX), offset = (right ? Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[10] : -c.Width) - scale * minX;
            Vector4 UV(Vector3 a) => new Vector4(a.x * scale, Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[11], a.y, a.x * offset + a.z);
            var t = frame.inverse.MultiplyVector(c.T);
            var tangent = new Vector4(t.x, t.y, t.z, c.Hand);
            int index = patches.Count;
            patches.Add(Draw01FiniteAsphalt.Create(UV(c.U), UV(c.V), frame, c.Min, c.Max, tangent));
            types.Add(Type.Asphalt);
            float slope = (c.Edge - c.End) / c.Width;
            var diagonal = new Vector4(-slope * side * scale, Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[12], sign, -c.End - slope * side * offset);
            var terminal = new Vector4(Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[13], Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs.Values.I32[14], sign, -c.End);
            return last ? cut(terminal, cut(diagonal, fallback, new Partition(index)), fallback) : cut(terminal, fallback, cut(diagonal, new Partition(index), fallback));
        }

        return End(false, End(true, outside));
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01MiddleAsphaltEnds_cs
internal static class Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs
{
    static Numeric_Types_Draw01_Draw01MiddleAsphaltEnds_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01MiddleAsphaltEnds_cs");
}
