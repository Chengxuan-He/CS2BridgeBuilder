using BridgeBuilder.Runtime;
using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Four original finite charts. Only the negative moving chart needs one
// continuous gradient fold: no common affine fits its original Float16 bounds.
// The measured fold is a projection boundary, not a prototype-face Boolean.
internal static class Lift01FarAppearance
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Lift01_FarAppearance");
    internal static MovableRoadAppearance.Level Create(SurfaceAsset native, Pose roadToObject, float halfDelta)
        => Prepared(BridgePreparedMaterials.RepeatUnit("Lift01", "aa572971f7048c8097d4756077cccd5bce76d15388d43755a5d8e80bf76fa8bf"), roadToObject, halfDelta);

    internal static MovableRoadAppearance.Level Prepared(SurfaceAsset native, Pose roadToObject, float halfDelta)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var f = Data.F32;
        var patches = new Patch[Data.I32[Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[0]]];
        var supported = new MovableRoadAppearance.Type[patches.Length];
        for (var i = Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[1]; i < patches.Length; i++)
        {
            var offset = i * Data.I32[Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[2]];
            float F(int field) => f[offset + field];
            var width = F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[3]) + halfDelta;
            if (!(width > Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[4]))
                throw new InvalidOperationException("Lift01 finite far width is invalid.");
            var scale = F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[5]) / width;
            var u = pose.transpose * new Vector4(F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[6]) * scale, Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[7], F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[8]), F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[9]));
            var v = pose.transpose * new Vector4(F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[10]) * scale, Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[11], F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[12]), F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[13]));
            patches[i] = new Patch(new Vector2(F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[14]), F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[15])), new Vector2(F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[16]), F(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[17])), u, v, native, sourceUv: true, projectTangent: true);
            supported[i] = MovableRoadAppearance.Type.Ballast;
        }

        var seam = Matrix4x4.identity;
        var fold = Data.I32[Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[18]] * Data.I32[Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[19]];
        var halfWidth = f[Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[20]];
        seam.m20 = f[fold] * halfWidth / (halfWidth + halfDelta);
        seam.m22 = f[fold + Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[21]];
        seam.m23 = f[fold + Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[22]];
        var negativeMoving = new Partition(seam * pose, new Partition(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[23]), new Partition(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[24]));
        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            return node.Bone switch
            {
                0 => new Partition(pose, new Partition(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[25]), new Partition(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[26])),
                4 => new Partition(pose, negativeMoving, new Partition(Numeric_Types_Lift01_Lift01FarAppearance_cs.Values.I32[27])),
                _ => throw new InvalidOperationException("Lift01 far appearance requires fixed or road bone ownership.")};
        }

        var domains = Convert(Lift01RoadPartitions.Carriageway);
        return new MovableRoadAppearance.Level(native, patches, supported, domains, domains);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01FarAppearance_cs
internal static class Numeric_Types_Lift01_Lift01FarAppearance_cs
{
    static Numeric_Types_Lift01_Lift01FarAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01FarAppearance_cs");
}
