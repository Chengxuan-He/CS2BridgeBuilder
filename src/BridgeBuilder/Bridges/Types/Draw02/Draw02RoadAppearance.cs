using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using RepeatBand = BridgeBuilder.Bridges.MovableExistingRoadSurface.RepeatBand;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// DrawBridge02 mesh 0, game 1.6.2f1. Geometry SHA-256:
// 1f0ff1a2c0a8318e6537bd347ae064d1bb55bfa48f3cf3c2375601adcd05dd18.
// Measured top faces: 3680/3756 (grating), 3684/3760/3784 (solid strips),
// 3634/3710 (walkway), 2492/2512 (fixed road), 10701/10703 (fixed walkway).
internal static class Draw02RoadAppearance
{
    // One complete transverse period, including both halves of the solid strip.
    // Sharing this immutable recipe also shares the generated repeating material.
    private static readonly RepeatBand[] CarriagewayBands = ReadPackedCarriagewayBands();
    internal static Patch[] Patches(MovableSelectedRoadLayout layout, Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var result = new Patch[Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[0]];
        for (var half = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[1]; half < Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[2]; half++)
            for (var side = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[3]; side < Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[4]; side++)
            {
                var offset = half * Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[5];
                var movingV = new Vector4(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[6], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[7], half == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[8] ? -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[22] : Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[23], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[24]);
                // Keep the measured UV gradient (density), but anchor each sidewalk at
                // its selected-road outer edge. Asymmetric layouts need separate origins.
                var span = side == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[9] ? layout.LeftSidewalk : layout.RightSidewalk;
                var gradient = side == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[10] ? -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[25] : Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[26];
                var outer = side == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[11] ? span.x : span.y;
                var walkU = new Vector4(gradient, Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[12], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[13], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[27] - gradient * outer);
                result[offset + side] = new Patch(new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[28], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[29]), new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[30], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[31]), frame * new Vector4(-Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[32] / Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[33], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[14], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[15], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[16]), frame * movingV, repeat: true, bands: CarriagewayBands);
                result[offset + Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[17] + side] = new Patch(new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[34], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[35]), new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[36], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[37]), frame * walkU, frame * movingV, repeat: true);
                // Negative fixed road faces 24658..24661 retain negative V slope;
                // unlike the moving leaf, they are not a simple mirrored projection.
                result[offset + Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[18] + side] = new Patch(new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[38], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[39]), new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[40], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[41]), frame * new Vector4(-Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[42] / Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[43], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[19], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[20], side), frame * new Vector4(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[21], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[22], -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[44], half == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[23] ? Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[45] : -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[46]));
                result[offset + Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[24] + side] = new Patch(new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[47], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[48]), new Vector2(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[49], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[50]), frame * walkU, frame * new Vector4(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[25], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[26], half == Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[27] ? -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[51] : Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[52], Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[53]));
            }

        return result;
    }

    internal static Partition Regions(bool sidewalk, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Z(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -z;
            return new Partition(frame * pose, negative, positive);
        }

        Partition Half(int offset)
        {
            var start = offset + (sidewalk ? Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[28] : Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[29]);
            Partition Side(int side) => Z(-Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[54], new Partition(start + side + Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[30]), Z(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.F32[55], new Partition(start + side), new Partition(start + side + Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[31])));
            var frame = Matrix4x4.identity;
            frame.m00 = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[32];
            frame.m02 = -Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[33];
            frame.m20 = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[34];
            frame.m22 = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[35];
            return new Partition(frame * pose, Side(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[36]), Side(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[37]));
        }

        return Z(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[38], Half(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[39]), Half(Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values.I32[40]));
    }

    private static RepeatBand[] ReadPackedCarriagewayBands()
    {
        var d = Numeric_Types_Draw02_Draw02RoadAppearance_cs.Values;
        var values = new RepeatBand[3];
        for (int i = 0; i < 1; i++)
            values[0 + i] = new(d.F32[0 + i * 7], d.F32[1 + i * 7] / d.F32[2 + i * 7], new Vector2(d.F32[3 + i * 7], d.F32[4 + i * 7]), new Vector2(d.F32[5 + i * 7], d.F32[6 + i * 7]));
        for (int i = 0; i < 1; i++)
            values[1 + i] = new(d.F32[7 + i * 8] / d.F32[8 + i * 8], d.F32[9 + i * 8] / d.F32[10 + i * 8], new Vector2(d.F32[11 + i * 8], d.F32[12 + i * 8]), new Vector2(d.F32[13 + i * 8], d.F32[14 + i * 8]));
        for (int i = 0; i < 1; i++)
            values[2 + i] = new(d.F32[15 + i * 7] / d.F32[16 + i * 7], d.F32[17 + i * 7], new Vector2(d.F32[18 + i * 7], d.F32[19 + i * 7]), new Vector2(d.F32[20 + i * 7], d.F32[21 + i * 7]));
        return values;
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02RoadAppearance_cs
internal static class Numeric_Types_Draw02_Draw02RoadAppearance_cs
{
    static Numeric_Types_Draw02_Draw02RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02RoadAppearance_cs");
}
