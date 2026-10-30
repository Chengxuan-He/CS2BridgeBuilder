using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Native LOD2 finite atlas, geometry 6535b8c60ffb7e45d69153e99c482c603443ba7f761a39a399bbaf84111dbf1c.
// EmitFarMapping.py verifies float32 projections against each original Float16
// storage interval. Original stations are finite panel boundaries, never periods.
internal static class Draw02FarAppearance
{
    internal static MovableRoadAppearance.Level Create(SurfaceAsset native, float halfDelta, Pose pose)
    {
        if (native.id.guid.ToString() != "436d1828c36d1336cf115c85def2c62c")
            throw new InvalidOperationException("Draw02 far appearance requires its original LOD2 Surface.");
        var d = Numeric_Types_Draw02_Draw02FarAppearance_cs.Values.F32;
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var patches = new Patch[32];
        var support = new Type[32];
        for (var role = 0; role < 2; role++)
            for (var half = 0; half < 2; half++)
                for (var side = 0; side < 2; side++)
                    for (var part = 0; part < 4; part++)
                    {
                        var domain = half * 8 + side * 4 + part;
                        var start = domain * 7;
                        var extent = d[start + 6] + halfDelta;
                        if (!float.IsFinite(extent) || extent <= 0)
                            throw new InvalidOperationException("Draw02 far appearance width is invalid.");
                        var xScale = d[start + 6] / extent;
                        var sign = half == 0 ? -1f : 1f;
                        // Preserve the far finite interval at the shared physical
                        // hinge/cap endpoints, including its independently authored LOD.
                        var zScale = 1f;
                        var zOffset = 0f;
                        if (part == 0)
                        {
                            var centre = role == 0 ? d[121] : d[122];
                            zScale = (d[112] - d[120]) / (d[112] - centre);
                            zOffset = sign * (d[120] - zScale * centre);
                        }
                        else if (part == 2)
                        {
                            zScale = (d[117] - d[113]) / (d[114] - d[113]);
                            zOffset = sign * d[113] * (1f - zScale);
                        }
                        else if (part == 3)
                        {
                            zScale = (d[119] - d[118]) / (d[116] - d[115]);
                            zOffset = sign * (d[118] - zScale * d[115]);
                        }
                        Vector4 Axis(int offset) => projection * new Vector4(
                            d[start + offset] * xScale, 0f,
                            d[start + offset + 1] * zScale,
                            d[start + offset + 2] + d[start + offset + 1] * zOffset);
                        var index = role * 16 + domain;
                        // Native codec audit: moving panels use packed +Z/-1;
                        // fixed caps use their signed X/-1 frame. Do not inherit
                        // the selected road's tangent with these original UVs.
                        var sourceTangent = part == 3 ? Vector3.right * Mathf.Sign(d[start])
                            : Vector3.forward * Mathf.Sign(d[start + 1]);
                        var tangent = Quaternion.Inverse(pose.rotation) * sourceTangent;
                        patches[index] = new Patch(Vector2.zero, Vector2.one, Axis(0), Axis(3),
                            surface: native, sourceUv: true,
                            tangent: new Vector4(tangent.x, tangent.y, tangent.z, -1f));
                        support[index] = role == 0 ? Type.Asphalt : Type.Asphalt | Type.Sidewalk;
                    }
        return new MovableRoadAppearance.Level(native, patches, support,
            Regions(false, transform), Regions(true, transform));
    }

    private static Partition Regions(bool sidewalk, Matrix4x4 pose)
    {
        var d = Numeric_Types_Draw02_Draw02FarAppearance_cs.Values.F32;
        Partition Z(float sign, float station, Partition before, Partition after)
        {
            var frame = Matrix4x4.identity;
            frame.m22 = sign;
            frame.m23 = -station;
            return new Partition(frame * pose, before, after);
        }
        Partition Half(int half)
        {
            Partition Side(int side)
            {
                var start = (sidewalk ? 16 : 0) + half * 8 + side * 4;
                var sign = half == 0 ? -1f : 1f;
                return Z(sign, d[112], new Partition(start),
                    Z(sign, d[113], new Partition(start + 1),
                    Z(sign, (d[114] + d[115]) * .5f,
                        new Partition(start + 2), new Partition(start + 3))));
            }
            var frame = Matrix4x4.identity;
            frame.m00 = 0f; frame.m02 = -1f;
            frame.m20 = 1f; frame.m22 = 0f;
            return new Partition(frame * pose, Side(0), Side(1));
        }
        return Z(1f, 0f, Half(0), Half(1));
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02FarAppearance_cs
internal static class Numeric_Types_Draw02_Draw02FarAppearance_cs
{
    static Numeric_Types_Draw02_Draw02FarAppearance_cs() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02FarAppearance_cs");
}
