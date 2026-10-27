using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// DrawBridge03 mesh 0, game 1.6.2f1. Geometry SHA-256:
// f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f.
// Native top faces 58046/58054, 58174/58180 and their opposite-side copies.
// Clean asphalt crop: pixels [3084,4052) x [1312,1476), bottom-left UV origin.
// The original complete road island contains paint and must not be repeated.
internal static class Draw03RoadAppearance
{
    private static float FixedUnitEnd => Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[0];
    private static float OuterEnd => Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[1];

    internal static Patch[] Patches(MovableSelectedRoadLayout layout, Pose roadToObject, Vector2? surfaceSpan = null)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[0]];
        for (var zone = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[1]; zone < Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[2]; zone++)
            for (var half = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[3]; half < Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[4]; half++)
                for (var walk = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[5]; walk < Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[6]; walk++)
                    for (var side = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[7]; side < Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[8]; side++)
                    {
                        var direction = half == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[9] ? -Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[2] : Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[3];
                        var sideSign = side == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[10] ? -Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[4] : Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[5];
                        var length = zone == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[11] ? OuterEnd - FixedUnitEnd : Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[6];
                        var origin = zone == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[12] ? OuterEnd : zone == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[13] ? FixedUnitEnd : walk == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[14] ? Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[7] : Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[8];
                        var u = frame * new Vector4(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[9], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[10], -direction / length, origin / length);
                        Vector2 min, max;
                        Vector4 v;
                        if (walk != Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[15])
                        {
                            min = new Vector2(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[11], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[12]);
                            max = new Vector2(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[13], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[14]);
                            var span = side == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[16] ? layout.LeftSidewalk : layout.RightSidewalk;
                            var outer = side == Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[17] ? span.x : span.y;
                            // Move the sampling origin with each selected sidewalk's outer edge;
                            // retain the measured 3.10009765625 m native period independently.
                            var gradient = -sideSign / Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[15];
                            v = frame * new Vector4(gradient, Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[16], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[17], -gradient * outer);
                        }
                        else
                        {
                            min = new Vector2(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[18], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[19]);
                            max = new Vector2(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[20], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[21]);
                            // Preserve the native atlas-V/metre gradient after selecting a smaller
                            // paint-free crop. The crop is not stretched across the road width.
                            var width = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[22] - sideSign * direction * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[23];
                            var gradient = -sideSign * (Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[24] - Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[25]) / width;
                            var crop = max.y - min.y;
                            v = frame * new Vector4(gradient / crop, Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[26], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[27], (Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[28] - gradient * direction * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[29] - min.y) / crop);
                        }

                        result[zone * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[18] + half * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[19] + walk * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[20] + side] = new Patch(min, max, u, v, repeat: true);
                    }

        return Draw03RoadWear.Apply(result, layout.CarLaneCenters, frame, surfaceSpan ?? layout.SurfaceSpan);
    }

    internal static Partition Regions(bool sidewalk, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }

        Partition Sides(int zone, int half)
        {
            var offset = zone * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[21] + half * Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[22] + (sidewalk ? Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[23] : Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[24]);
            var frame = Matrix4x4.identity;
            frame.m00 = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[30];
            frame.m02 = -Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[31];
            frame.m20 = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[32];
            frame.m22 = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[33];
            return new Partition(frame * pose, new Partition(offset), new Partition(offset + Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[25]));
        }

        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            return node.Bone switch
            {
                0 => Cut(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32[34], Cut(-FixedUnitEnd, Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[26], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[27]), Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[28], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[29])), Cut(FixedUnitEnd, Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[30], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[31]), Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[32], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[33]))),
                1 => Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[34], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[35]),
                2 => Sides(Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[36], Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[37]),
                _ => throw new InvalidOperationException("Draw03 road appearance requires its original three bones.")};
        }

        return Convert(sidewalk ? Draw03RoadPartitions.Sidewalk : Draw03RoadPartitions.Carriageway);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadAppearance_cs
internal static class Numeric_Types_Draw03_Draw03RoadAppearance_cs
{
    static Numeric_Types_Draw03_Draw03RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadAppearance_cs");
}
