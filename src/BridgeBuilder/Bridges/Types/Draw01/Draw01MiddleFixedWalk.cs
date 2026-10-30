using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// Measured middle main sidewalk chart; edge strips retain the existing fallback.
// Internal prototype V-reset edges are absent from this domain outline.
internal static class Draw01MiddleFixedWalk
{
    internal static Partition Create(bool right, bool positive, float minX, float maxX, Matrix4x4 frame, SurfaceAsset native, Partition outside, List<MovableExistingRoadSurface.Patch> patches, List<Type> types, Func<Vector4, Partition, Partition, Partition> cut)
    {
        float side = right ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[0] : -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[1], sign = positive ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[2] : -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[3];
        float nativeMin = right ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[4] : -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[5];
        float scale = (Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[6] - Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[7]) / (maxX - minX), offset = nativeMin - scale * minX;
        outside=Draw01MiddleWalkEdges.Create(right,positive,scale,offset,frame,native,outside,patches,types,cut);
        var a = ((positive ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[0] : Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[1]) + (right ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[2] : Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[3])) switch
        {
            0 => new Vector3(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[8], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[9], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[10]),
            2 => new Vector3(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[11], -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[12], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[13]),
            1 => new Vector3(-Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[14], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[15], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[16]),
            3 => new Vector3(-Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[17], -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[18], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[19]),
            _ => throw new InvalidOperationException("Invalid Draw01 quadrant.")};
        var u = new Vector4(a.x * scale, Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[4], a.y, a.x * offset + a.z);
        float period = Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[20];
        var repeatedV = new Vector4(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[5], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[6], -sign * period / Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[7], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[21] + Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[8] * period / Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[9]);
        var nativeT = frame.inverse.MultiplyVector(new Vector3(right ? -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[10] : Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[11], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[12], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[13]));
        var tangent = new Vector4(nativeT.x, nativeT.y, nativeT.z, right ? (positive ? -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[14] : Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[15]) : (positive ? Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[16] : -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[17]));
        int repeat = patches.Count;
        patches.Add(Draw01PreparedRoadUnit.Create(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[18], u, repeatedV, frame, Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[22], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[23], tangent));
        types.Add(Type.Sidewalk);
        // The initial 3.849609375 m traversal shares this same physical phase;
        // AuditMiddleFixedWalk verifies every original V rounding cell. No
        // finite-versus-repeat boundary is inferred from its partial length.
        var inside = new Partition(repeat);
        // Absolute source X minus the measured inner boundary x(z).
        Vector4 Boundary(float z0, float x0, float slope) => new Vector4(side * scale, Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[19], -slope * sign, side * offset - x0 + slope * z0);
        Partition Require(Vector4 plane, Partition next) => cut(plane, outside, next);
        if (!right && positive)
            inside = cut(Boundary(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[20], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[24], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[21]), Require(Boundary(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[22], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[25], (Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[26] - Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[27]) / Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[23]), inside), inside);
        else
        {
            inside = Require(Boundary(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[24], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[28], (Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[29] - Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[30]) / Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[25]), inside);
            inside = Require(Boundary(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[26], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[31], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[27]), inside);
        }

        inside = Require(Boundary(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[32], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[33], (Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[34] - Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[35]) / (Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[28] - Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[36])), inside);
        inside = cut(new Vector4(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[29], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[30], sign, -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[31]), inside, outside);
        return cut(new Vector4(Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[32], Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.I32[33], sign, -Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs.Values.F32[37]), outside, inside);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01MiddleFixedWalk_cs
internal static class Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs
{
    static Numeric_Types_Draw01_Draw01MiddleFixedWalk_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01MiddleFixedWalk_cs");
}
