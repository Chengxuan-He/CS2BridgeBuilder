using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native LiftBridge05 mesh 0 UV0: fixed unit faces 557/558, moving 1366/1367.
// Clean U strips exclude the baked rail tracks in the complete native islands.
internal static class Lift05RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[0]];
        for (var moving = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[1]; moving < Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[2]; moving++)
            for (var half = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[3]; half < Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[4]; half++)
                for (var side = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[5]; side < Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[6]; side++)
                {
                    var sideSign = side == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[7] ? -Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[0] : Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[1];
                    var halfSign = half == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[8] ? -Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[2] : Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[3];
                    var min = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[9] ? new Vector2(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[4], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[5]) : new Vector2(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[6], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[7]);
                    var max = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[10] ? new Vector2(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[8], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[9]) : new Vector2(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[10], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[11]);
                    var nativeMin = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[11] ? Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[12] : Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[13];
                    var nativeMax = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[12] ? Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[14] : Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[15];
                    var direction = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[13] ? -sideSign * halfSign : sideSign;
                    var gradient = direction * (nativeMax - nativeMin) / Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[16];
                    var origin = moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[14] && sideSign * halfSign > Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[17] ? nativeMax : nativeMin;
                    var u = frame * new Vector4(gradient / (max.x - min.x), Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[18], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[19], (origin - min.x) / (max.x - min.x));
                    var v = frame * (moving == Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[15] ? new Vector4(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[20], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[21], -halfSign / Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[22], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[23]) : new Vector4(Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[24], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[25], -Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[26], Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[27]));
                    result[moving * Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[16] + half * Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[17] + side] = new Patch(min, max, u, v, repeat: true);
                }

        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Sides(int offset)
        {
            var seam = Matrix4x4.identity;
            seam.m00 = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[28];
            seam.m02 = -Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[29];
            seam.m20 = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[30];
            seam.m22 = Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32[31];
            return new Partition(seam * pose, new Partition(offset), new Partition(offset + Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[18]));
        }

        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            var offset = node.Bone switch
            {
                0 => Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[19],
                2 => Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[20],
                _ => throw new InvalidOperationException("Lift05 road appearance requires fixed or road bone ownership.")};
            return new Partition(pose, Sides(offset), Sides(offset + Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[21]));
        }

        return Convert(Lift05RoadPartitions.Carriageway);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05RoadAppearance_cs
internal static class Numeric_Types_Lift05_Lift05RoadAppearance_cs
{
    static Numeric_Types_Lift05_Lift05RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05RoadAppearance_cs");
}
