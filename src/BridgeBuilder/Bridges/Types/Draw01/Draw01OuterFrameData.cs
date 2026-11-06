using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Original opaque outer walkway frame, previously removed with the road top.
// Immutable measured face/vertex membership; retain the entire rigid profile and source attributes.
internal static class Draw01OuterFrameData
{
    internal static void Apply(int lod, float delta, Vector3[] source, Vector3[] positions, bool[] removed)
    {
        if (lod == Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values.I32[0])
            return; // The far level carries the middle-source supplement.
        var faces = lod == Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values.I32[1] ? NearFaces : MiddleFaces;
        foreach (var face in faces)
            removed[face] = false;
        var left = lod == Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values.I32[2] ? NearLeft : MiddleLeft;
        var right = lod == Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values.I32[3] ? NearRight : MiddleRight;
        foreach (var coordinateRows in StructureRows.Stages(left))
            AffineGeometry.Execute(RangePrograms.Data, 0, new[] { source, positions }, Array.Empty<float[]>(), new float[] { delta }, count: coordinateRows.Length, indices: new[] { coordinateRows });
        foreach (var coordinateRows in StructureRows.Stages(right))
            AffineGeometry.Execute(RangePrograms.Data, 1, new[] { source, positions }, Array.Empty<float[]>(), new float[] { delta }, count: coordinateRows.Length, indices: new[] { coordinateRows });
    }

    private static int[] NearFaces => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.NearFaces", () => ReadPackedNearFaces());
    private static int[] NearLeft => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.NearLeft", () => ReadPackedNearLeft());
    private static int[] NearRight => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.NearRight", () => ReadPackedNearRight());
    private static int[] MiddleFaces => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.MiddleFaces", () => ReadPackedMiddleFaces());
    private static int[] MiddleLeft => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.MiddleLeft", () => ReadPackedMiddleLeft());
    private static int[] MiddleRight => GenerationParameters.Get<int[]>("BridgeBuilder.Bridges.Draw01OuterFrameData.MiddleRight", () => ReadPackedMiddleRight());
    private static int[] ReadPackedNearFaces()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[504];
        for (int i = 0; i < 504; i++)
            values[0 + i] = d.I32[4 + i * 1];
        return values;
    }

    private static int[] ReadPackedNearLeft()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[504];
        for (int i = 0; i < 504; i++)
            values[0 + i] = d.I32[508 + i * 1];
        return values;
    }

    private static int[] ReadPackedNearRight()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[504];
        for (int i = 0; i < 504; i++)
            values[0 + i] = d.I32[1012 + i * 1];
        return values;
    }

    private static int[] ReadPackedMiddleFaces()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[328];
        for (int i = 0; i < 328; i++)
            values[0 + i] = d.I32[1516 + i * 1];
        return values;
    }

    private static int[] ReadPackedMiddleLeft()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[328];
        for (int i = 0; i < 328; i++)
            values[0 + i] = d.I32[1844 + i * 1];
        return values;
    }

    private static int[] ReadPackedMiddleRight()
    {
        var d = Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values;
        var values = new int[328];
        for (int i = 0; i < 328; i++)
            values[0 + i] = d.I32[2172 + i * 1];
        return values;
    }

    private static class RangePrograms
    {
        static RangePrograms()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw01OuterFrameData.RangePrograms.Data", () => HardcodedNumbers.Load("Types_Draw01_Draw01OuterFrameData_RangePrograms"));
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01OuterFrameData_cs
internal static class Numeric_Types_Draw01_Draw01OuterFrameData_cs
{
    static Numeric_Types_Draw01_Draw01OuterFrameData_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01OuterFrameData_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01OuterFrameData_cs"));
}
