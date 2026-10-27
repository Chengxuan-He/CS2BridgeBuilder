using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native LiftBridge01 mesh 0, game 1.6.2f1, SHA-256
// cc34bfcb478f5621513c458120bacfb395e21548e3eee2e753dca97e138b5714.
// Central floor unit: X=-2..0, Z=87.779296875..91.779296875,
// faces 11144/11145. Moving units use the same atlas rectangle, with a
// separate longitudinal origin. Rails and sleepers are separate geometry.
internal static class Lift01RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[0]];
        for (var moving = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[1]; moving < Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[2]; moving++)
            for (var half = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[3]; half < Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[4]; half++)
                for (var side = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[5]; side < Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[6]; side++)
                {
                    var index = moving * Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[7] + half * Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[8] + side;
                    result[index] = new Patch(new Vector2(Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[0], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[1]), new Vector2(Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[2], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[3]), frame * new Vector4(side == Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[9] ? -Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[4] : Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[5], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[6], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[7], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[8]), frame * new Vector4(Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[9], Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[10], half == Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[10] ? -Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[11] : Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[12], moving == Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[11] ? Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[13] / Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[14] : Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[15]), repeat: true);
                }

        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Sides(int offset)
        {
            var seam = Matrix4x4.identity;
            seam.m00 = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[16];
            seam.m02 = -Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[17];
            seam.m20 = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[18];
            seam.m22 = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32[19];
            return new Partition(seam * pose, new Partition(offset), new Partition(offset + Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[12]));
        }

        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            var offset = node.Bone switch
            {
                0 => Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[13],
                4 => Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[14],
                _ => throw new InvalidOperationException("Lift01 floor appearance requires fixed or road bone ownership.")};
            return new Partition(pose, Sides(offset + Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.I32[15]), Sides(offset));
        }

        return Convert(Lift01RoadPartitions.Carriageway);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01RoadAppearance_cs
internal static class Numeric_Types_Lift01_Lift01RoadAppearance_cs
{
    static Numeric_Types_Lift01_Lift01RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01RoadAppearance_cs");
}
