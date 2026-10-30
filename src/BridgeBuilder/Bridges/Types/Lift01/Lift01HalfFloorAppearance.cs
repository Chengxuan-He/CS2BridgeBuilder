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
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Lift01_HalfFloorDomains");
    private static readonly HardcodedNumbers UnitData = HardcodedNumbers.Load("Types_Lift01_ContinuousUnit");
    internal static MovableRoadAppearance.Level Create(int lod, SurfaceAsset native, Pose roadToObject, float halfDelta)
        => Prepared(lod, native, BridgePreparedMaterials.RepeatUnit("Lift01", "7f727eeef2ad7c28df48e6200815357a780ac5636f83f82cd8d0ba46180e5f43"), roadToObject, halfDelta);

    internal static MovableRoadAppearance.Level Prepared(int lod, SurfaceAsset native, SurfaceAsset unit, Pose roadToObject, float halfDelta)
    {
        if (lod != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[0] && lod != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[1])
            throw new ArgumentOutOfRangeException(nameof(lod));
        var f = Data.F32;
        var halfWidth = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[2]] + halfDelta;
        if (!(halfWidth > Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[3]))
            throw new InvalidOperationException("Lift01 finite floor width is invalid.");
        var panels = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[4] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[5] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[6];
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var sourceScale = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[7]] / halfWidth;
        var patches = new Patch[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[10] * panels];
        var types = new MovableRoadAppearance.Type[patches.Length];
        for (var group = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[11]; group < Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[12]; group++)
        {
            var moving = group / Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[13];
            var half = group / Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[14] % Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[15];
            var side = group % Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[16];
            var segmentCount = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[23] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[24] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[25];
            var firstSegment = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[26] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[27] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[28];
            var firstPatch = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[29] ? group * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[30] * panels : (Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[31] + (group - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[32]) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[33]) * panels;
            for (var segment = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[34]; segment < segmentCount; segment++)
                for (var panel = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[35]; panel < panels; panel++)
                {
                    var segmentIndex = firstSegment + segment;
                    var finite = segmentIndex != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[36] && segmentIndex != Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[37];
                    var slope = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[38] ? (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[39]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[40]]) / f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[41]] : panel == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[42] ? (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[43]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[44]]) / f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[45]] : (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[46]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[47]]) / (f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[48]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[49]]);
                    var intercept = lod == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[50] && panel == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[51] ? f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[52]] - slope * f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[53]] : f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[54]];
                    var u = pose.transpose * new Vector4((side == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[57] ? -slope : slope) * sourceScale, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[58], Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[59], intercept);
                    var segmentOffset = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[60] + segmentIndex * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[61];
                    var vScale = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[62]] - f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[63]];
                    var v = pose.transpose * new Vector4(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[65], Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[66], (half == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[67] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[68] : -Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[69]) * f[segmentOffset + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[70]] * vScale, f[segmentOffset + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[71]] * vScale + f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[72]]);
                    var index = firstPatch + segment * panels + panel;
                    var sourceMin = new Vector2(lod == 1 || panel == 0 ? f[0] : f[1], f[3]);
                    var sourceMax = new Vector2(lod == 1 || panel == 1 ? f[2] : f[1], f[4]);
                    if (finite)
                        patches[index] = new Patch(sourceMin, sourceMax, u, v, native,
                            sourceUv: true, projectTangent: true);
                    else
                    {
                        var map = UnitData.F32;
                        u = u * map[0] + new Vector4(0, 0, 0, map[1]);
                        v = v * map[2] + new Vector4(0, 0, 0, map[3]);
                        patches[index] = new Patch(new Vector2(map[4], map[5]), new Vector2(map[6], map[7]),
                            u, v, unit, preparedUnit: true, repeatU: false, repeatV: true, projectTangent: true)
                            .WithFiniteSampling(Vector2.zero, Vector2.one);
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
            seam.m20 = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[82];
            seam.m22 = Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[83];
            seam.m23 = -station;
            return new Partition(seam * pose, negative, positive);
        }

        Partition SidePanels(int first, int side)
        {
            if (panels == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[84])
                return new Partition(first);
            var boundary = f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[85]] / sourceScale;
            return side == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[86] ? Lateral(-boundary, new Partition(first + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[87]), new Partition(first)) : Lateral(boundary, new Partition(first), new Partition(first + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[88]));
        }

        Partition Half(int moving, int half)
        {
            Partition Side(int side)
            {
                var group = moving * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[89] + half * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[90] + side;
                var count = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[91] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[92] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[93];
                var firstSegment = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[94] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[95] : Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[96];
                var firstPatch = moving == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[97] ? group * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[98] * panels : (Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[99] + (group - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[100]) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[101]) * panels;
                var result = SidePanels(firstPatch + (count - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[102]) * panels, side);
                for (var segment = count - Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[103]; segment >= Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[104]; segment--)
                {
                    var seam = Matrix4x4.identity;
                    seam.m22 = half == Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[105] ? Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[106] : -Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[107];
                    seam.m23 = -f[Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[108] + (firstSegment + segment) * Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[109] + Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[110]];
                    result = new Partition(seam * pose, SidePanels(firstPatch + segment * panels, side), result);
                }

                return result;
            }

            return Lateral(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[111], Side(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[112]), Side(Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[113]));
        }

        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            var moving = node.Bone switch
            {
                0 => Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[114],
                4 => Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[115],
                _ => throw new InvalidOperationException("Lift01 appearance requires fixed or road bone ownership.")};
            return new Partition(pose, Half(moving, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[116]), Half(moving, Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs.Values.I32[117]));
        }

        var regions = Convert(Lift01RoadPartitions.Carriageway);
        return new MovableRoadAppearance.Level(native, patches, types, regions, regions);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01HalfFloorAppearance_cs
internal static class Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs
{
    static Numeric_Types_Lift01_Lift01HalfFloorAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01HalfFloorAppearance_cs");
}
