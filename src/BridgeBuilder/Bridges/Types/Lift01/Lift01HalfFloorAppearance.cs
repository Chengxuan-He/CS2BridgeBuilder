using BridgeBuilder.Runtime;
using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// The existing near 2 m / 1 m physical panel boundary is independent of the
// longitudinal repeat. Regular domains share one offline complete half-floor unit;
// continuous UV adds no period seams. Finite terminals retain original atlas UVs.
internal static class Lift01HalfFloorAppearance
{
    private static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift01HalfFloorAppearance.Data", () => HardcodedNumbers.Load("Types_Lift01_HalfFloorDomains"));
    private static HardcodedNumbers UnitData => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift01HalfFloorAppearance.UnitData", () => HardcodedNumbers.Load("Types_Lift01_ContinuousUnit"));
    internal static MovableRoadAppearance.Level Create(int lod, SurfaceAsset native, Pose roadToObject, float halfDelta) => Prepared(lod, native, BridgePreparedMaterials.RepeatUnit("Lift01", "7f727eeef2ad7c28df48e6200815357a780ac5636f83f82cd8d0ba46180e5f43"), roadToObject, halfDelta);
    internal static MovableRoadAppearance.Level Prepared(int lod, SurfaceAsset native, MovableExistingRoadSurface.SurfaceResult unit, Pose roadToObject, float halfDelta)
    {
        if (lod != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[0] && lod != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[1])
            throw new ArgumentOutOfRangeException(nameof(lod));
        var f = Data.F32;
        var halfWidth = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[2]] + halfDelta;
        if (!float.IsFinite(halfWidth))
            throw new ArgumentOutOfRangeException(nameof(halfDelta));
        // A collapsed projection has no inverse; retain the selected material.
        if (halfWidth == 0f)
            return new MovableRoadAppearance.Level(native, new[] { new Patch(Vector2.zero, Vector2.one) }, new[] { MovableRoadAppearance.Type.None }, new Partition(0), new Partition(0));
        var panels = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[3] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[4] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[5];
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var sourceScale = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[6]] / halfWidth;
        var patches = new Patch[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[7] * panels];
        var types = new MovableRoadAppearance.Type[patches.Length];
        for (var group = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[8]; group < Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[9]; group++)
        {
            var moving = group / Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[10];
            var half = group / Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[11] % Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[12];
            var side = group % Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[13];
            var segmentCount = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[14] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[15] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[16];
            var firstSegment = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[17] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[18] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[19];
            var firstPatch = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[20] ? group * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[21] * panels : (Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[22] + (group - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[23]) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[24]) * panels;
            for (var segment = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[25]; segment < segmentCount; segment++)
                for (var panel = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[26]; panel < panels; panel++)
                {
                    var segmentIndex = firstSegment + segment;
                    var finite = segmentIndex != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[27] && segmentIndex != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[28];
                    var slope = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[29] ? (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[30]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[31]]) / f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[32]] : panel == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[33] ? (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[34]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[35]]) / f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[36]] : (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[37]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[38]]) / (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[39]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[40]]);
                    var intercept = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[41] && panel == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[42] ? f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[43]] - slope * f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[44]] : f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[45]];
                    var u = pose.transpose * new Vector4((side == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[46] ? -slope : slope) * sourceScale, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[47], Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[48], intercept);
                    var segmentOffset = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[49] + segmentIndex * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[50];
                    var vScale = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[51]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[52]];
                    var v = pose.transpose * new Vector4(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[53], Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[54], (half == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[55] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[56] : -Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[57]) * f[segmentOffset + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[58]] * vScale, f[segmentOffset + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[59]] * vScale + f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[60]]);
                    var index = firstPatch + segment * panels + panel;
                    var sourceMin = new Vector2(lod == 1 || panel == 0 ? f[0] : f[1], f[3]);
                    var sourceMax = new Vector2(lod == 1 || panel == 1 ? f[2] : f[1], f[4]);
                    if (finite)
                        patches[index] = new Patch(sourceMin, sourceMax, u, v, native, sourceUv: true, projectTangent: true);
                    else
                    {
                        var map = UnitData.F32;
                        u = u * map[0] + new Vector4(0, 0, 0, map[1]);
                        v = v * map[2] + new Vector4(0, 0, 0, map[3]);
                        patches[index] = new Patch(new Vector2(map[4], map[5]), new Vector2(map[6], map[7]), u, v, unit, preparedUnit: true, repeatU: false, repeatV: true, projectTangent: true).WithFiniteSampling(Vector2.zero, Vector2.one);
                    }

                    types[index] = MovableRoadAppearance.Type.Ballast;
                }
        }

        Partition Lateral(float station, Partition negative, Partition positive)
        {
            var seam = Matrix4x4.identity;
            // Complete the lateral frame: changing only its third row leaves
            // identity's first row parallel to it and fails native partition validation.
            seam.SetRow(0, new Vector4(0, 0, -1, 0));
            seam.m20 = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[61];
            seam.m22 = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[62];
            seam.m23 = -station;
            return new Partition(seam * pose, negative, positive);
        }

        Partition SidePanels(int first, int side)
        {
            if (panels == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[63])
                return new Partition(first);
            var boundary = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[64]] / sourceScale;
            return side == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[65] ? Lateral(-boundary, new Partition(first + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[66]), new Partition(first)) : Lateral(boundary, new Partition(first), new Partition(first + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[67]));
        }

        Partition Half(int moving, int half)
        {
            Partition Side(int side)
            {
                var group = moving * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[68] + half * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[69] + side;
                var count = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[70] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[71] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[72];
                var firstSegment = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[73] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[74] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[75];
                var firstPatch = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[76] ? group * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[77] * panels : (Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[78] + (group - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[79]) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[80]) * panels;
                var result = SidePanels(firstPatch + (count - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[81]) * panels, side);
                for (var segment = count - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[82]; segment >= Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[83]; segment--)
                {
                    var seam = Matrix4x4.identity;
                    seam.m22 = half == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[84] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[85] : -Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[86];
                    seam.m23 = -f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[87] + (firstSegment + segment) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[88] + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[89]];
                    result = new Partition(seam * pose, SidePanels(firstPatch + segment * panels, side), result);
                }

                return result;
            }

            return Lateral(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[90], Side(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[91]), Side(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[92]));
        }

        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            var moving = node.Bone switch
            {
                0 => Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[93],
                4 => Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[94],
                _ => throw new InvalidOperationException("Lift01 appearance requires fixed or road bone ownership.")};
            return new Partition(pose, Half(moving, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[95]), Half(moving, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[96]));
        }

        // A source category alone does not extend the measured floor sideways.
        // Keep unmatched receivers in the physical mesh with their original route.
        var originalDomain = patches.Length;
        Array.Resize(ref patches, originalDomain + 1);
        Array.Resize(ref types, patches.Length);
        patches[originalDomain] = new Patch(Vector2.zero, Vector2.one);
        var original = new Partition(originalDomain);
        var regions = Lateral(-halfWidth, original, Lateral(halfWidth, Convert(Lift01RoadPartitions.Carriageway), original));
        return new MovableRoadAppearance.Level(native, patches, types, regions, regions);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01HalfFloorAppearance_cs
internal static class Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs
{
    static Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01HalfFloorAppearance_cs"));
}
