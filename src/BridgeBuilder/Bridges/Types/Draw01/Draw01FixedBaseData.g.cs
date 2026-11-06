// Native 1.6.2f1 Bottom-layer geometry, decoded from BridgesAndPorts/Blob.cok.
// Seam memberships are measured offline, never classified from runtime geometry.
using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw01FixedBaseData
{
    internal static readonly string[] Names =
    {
        "DrawBridge01Bottom300cm Mesh",
        "DrawBridge01BottomNode300cm Mesh",
        "DrawBridge01BottomEdge350cm Mesh",
        "DrawBridge01BottomEdgeNode350cm Mesh",
        "DrawBridge01BottomIntersectionMiddle200cm Mesh"
    };
    private static int[][] Counts => GenerationParameters.Get<int[][]>("BridgeBuilder.Bridges.Draw01FixedBaseData.Counts", () => ReadPackedCounts());
    private static int[][] Triangles => GenerationParameters.Get<int[][]>("BridgeBuilder.Bridges.Draw01FixedBaseData.Triangles", () => ReadPackedTriangles());
    private static int[][][] Seam => GenerationParameters.Get<int[][][]>("BridgeBuilder.Bridges.Draw01FixedBaseData.Seam", () => ReadPackedSeam());
    internal static MovableStructureModel.Plan Plan(int family, int lod, Vector3[] source, int[] indices, float halfDelta)
    {
        if (lod >= Counts[family].Length || source.Length != Counts[family][lod] || indices.Length != Triangles[family][lod] * Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[774])
            throw new InvalidOperationException($"Draw01 fixed base {family} LOD {lod} differs from its measured geometry.");
        var positions = (Vector3[])source.Clone();
        if (family < Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[775])
        {
            // Right strip centre moves by delta/2 in the independently composed base.
            // In bridge coordinates its non-seam vertices translate by delta; seam stays x=0.
            AffineGeometry.Execute(RangePrograms.Data, 0, new[] { positions }, Array.Empty<float[]>(), new float[] { halfDelta, Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.F32[0] }, Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[776], checked((positions.Length) - (Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[776])));
            foreach (var coordinateRows in StructureRows.Stages(Seam[family][lod]))
                AffineGeometry.Execute(RangePrograms1.Data, 0, new[] { positions }, Array.Empty<float[]>(), new float[] { halfDelta }, count: coordinateRows.Length, indices: new[] { coordinateRows });
        }
        else if (family == Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[777])
        {
            var sides = new float[source.Length];
            for (var i = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[778]; i < positions.Length; i++)
                sides[i] = Math.Sign(source[i].x);
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.DenseDelta,
                new[] { positions, positions }, new[] { sides }, new[] { halfDelta },
                Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[778],
                positions.Length - Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[778]);
        }

        // Outer strips have no local deformation. Their network placement supplies
        // the rigid half-width translation. No Y/Z or original UV channels change.
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[indices.Length / Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values.I32[779]] });
    }

    private static int[] ReadPackedCounts_0()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[0 + i * 1];
        return values;
    }

    private static int[] ReadPackedCounts_1()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[3 + i * 1];
        return values;
    }

    private static int[] ReadPackedCounts_2()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[6 + i * 1];
        return values;
    }

    private static int[] ReadPackedCounts_3()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[9 + i * 1];
        return values;
    }

    private static int[] ReadPackedCounts_4()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[1];
        for (int i = 0; i < 1; i++)
            values[0 + i] = d.I32[12 + i * 1];
        return values;
    }

    private static int[][] ReadPackedCounts()
    {
        var values = new int[5][];
        values[0] = ReadPackedCounts_0();
        values[1] = ReadPackedCounts_1();
        values[2] = ReadPackedCounts_2();
        values[3] = ReadPackedCounts_3();
        values[4] = ReadPackedCounts_4();
        return values;
    }

    private static int[] ReadPackedTriangles_0()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[13 + i * 1];
        return values;
    }

    private static int[] ReadPackedTriangles_1()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[16 + i * 1];
        return values;
    }

    private static int[] ReadPackedTriangles_2()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[19 + i * 1];
        return values;
    }

    private static int[] ReadPackedTriangles_3()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
            values[0 + i] = d.I32[22 + i * 1];
        return values;
    }

    private static int[] ReadPackedTriangles_4()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[1];
        for (int i = 0; i < 1; i++)
            values[0 + i] = d.I32[25 + i * 1];
        return values;
    }

    private static int[][] ReadPackedTriangles()
    {
        var values = new int[5][];
        values[0] = ReadPackedTriangles_0();
        values[1] = ReadPackedTriangles_1();
        values[2] = ReadPackedTriangles_2();
        values[3] = ReadPackedTriangles_3();
        values[4] = ReadPackedTriangles_4();
        return values;
    }

    private static int[] ReadPackedSeam_0_0()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[330];
        for (int i = 0; i < 330; i++)
            values[0 + i] = d.I32[26 + i * 1];
        return values;
    }

    private static int[] ReadPackedSeam_0_1()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[266];
        for (int i = 0; i < 266; i++)
            values[0 + i] = d.I32[356 + i * 1];
        return values;
    }

    private static int[] ReadPackedSeam_0_2()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[94];
        for (int i = 0; i < 94; i++)
            values[0 + i] = d.I32[622 + i * 1];
        return values;
    }

    private static int[][] ReadPackedSeam_0()
    {
        var values = new int[3][];
        values[0] = ReadPackedSeam_0_0();
        values[1] = ReadPackedSeam_0_1();
        values[2] = ReadPackedSeam_0_2();
        return values;
    }

    private static int[] ReadPackedSeam_1_0()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[20];
        for (int i = 0; i < 20; i++)
            values[0 + i] = d.I32[716 + i * 1];
        return values;
    }

    private static int[] ReadPackedSeam_1_1()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[20];
        for (int i = 0; i < 20; i++)
            values[0 + i] = d.I32[736 + i * 1];
        return values;
    }

    private static int[] ReadPackedSeam_1_2()
    {
        var d = Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values;
        var values = new int[18];
        for (int i = 0; i < 18; i++)
            values[0 + i] = d.I32[756 + i * 1];
        return values;
    }

    private static int[][] ReadPackedSeam_1()
    {
        var values = new int[3][];
        values[0] = ReadPackedSeam_1_0();
        values[1] = ReadPackedSeam_1_1();
        values[2] = ReadPackedSeam_1_2();
        return values;
    }

    private static int[][][] ReadPackedSeam()
    {
        var values = new int[2][][];
        values[0] = ReadPackedSeam_0();
        values[1] = ReadPackedSeam_1();
        return values;
    }

    private static class RangePrograms
    {
        static RangePrograms()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw01FixedBaseData.RangePrograms.Data", () => HardcodedNumbers.Load("Types_Draw01_Draw01FixedBaseData_RangePrograms"));
    }

    private static class RangePrograms1
    {
        static RangePrograms1()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw01FixedBaseData.RangePrograms1.Data", () => HardcodedNumbers.Load("Types_Draw01_Draw01FixedBaseData_RangePrograms1"));
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01FixedBaseData_g_cs
internal static class Numeric_Types_Draw01_Draw01FixedBaseData_g_cs
{
    static Numeric_Types_Draw01_Draw01FixedBaseData_g_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01FixedBaseData_g_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01FixedBaseData_g_cs"));
}
