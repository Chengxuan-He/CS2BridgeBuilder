using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// LiftBridge03 native mesh-0 Surface d5708265307c0c422270f918fb8ca8e8.
// Source atlas 4096 square, VT 8c8c7c348b3a3114bc610b7dcc7f305f.
// Upper clean asphalt excludes baked white lines; lower unit retains native ballast.
internal static class Lift03RoadAppearance
{
    internal static Patch[] Patches(Lift03RoadSampling.Deck deck, Pose roadToObject)
    {
        Vector2 min, max;
        float du, dv;
        switch (deck)
        {
            case Lift03RoadSampling.Deck.Main:
                min = new Vector2(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[0] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[1], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[2] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[3]);
                max = new Vector2(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[4] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[5], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[6] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[7]);
                // Native face 23857: 124 and 124.359375 source pixels per metre.
                du = Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[8];
                dv = Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[9];
                break;
            case Lift03RoadSampling.Deck.Auxiliary:
                min = new Vector2(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[10] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[11], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[12] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[13]);
                max = new Vector2(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[14] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[15], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[16] / Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[17]);
                // Native face 27296: 249 source pixels per metre on both axes.
                du = -Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[18];
                dv = Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[19];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(deck));
        }

        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var uScale = du / (max.x - min.x);
        var vScale = dv / (max.y - min.y);
        Patch Unit(float xSign, float zSign) => new(min, max, frame * new Vector4(xSign * uScale, Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[20], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[21], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[22]), frame * new Vector4(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[23], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[24], zSign * vScale, Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[25]), repeat: true);
        // Left-negative Z, left-positive Z, right-negative Z, right-positive Z.
        return new[]
        {
            Unit(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[26], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[27]),
            Unit(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[28], -Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[29]),
            Unit(-Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[30], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[31]),
            Unit(-Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[32], -Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[33])
        };
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var lateral = Matrix4x4.Rotate(Quaternion.Euler(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[34], -Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[35], Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32[36]));
        return new Partition(lateral * pose, new Partition(pose, new Partition(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.I32[0]), new Partition(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.I32[1])), new Partition(pose, new Partition(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.I32[2]), new Partition(Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.I32[3])));
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03RoadAppearance_cs
internal static class Numeric_Types_Lift03_Lift03RoadAppearance_cs
{
    static Numeric_Types_Lift03_Lift03RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03RoadAppearance_cs");
}
