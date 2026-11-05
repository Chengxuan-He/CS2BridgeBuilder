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
        AffineGeometry.Indices(left, index => positions[index].x = AffineGeometry.Translate(source[index].x, -delta));
        AffineGeometry.Indices(right, index => positions[index].x = AffineGeometry.Translate(source[index].x, delta));
    }

    private static readonly int[] NearFaces = ReadPackedNearFaces();
    private static readonly int[] NearLeft = ReadPackedNearLeft();
    private static readonly int[] NearRight = ReadPackedNearRight();
    private static readonly int[] MiddleFaces = ReadPackedMiddleFaces();
    private static readonly int[] MiddleLeft = ReadPackedMiddleLeft();
    private static readonly int[] MiddleRight = ReadPackedMiddleRight();
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
}

// Embedded numerical arrays: Types_Draw01_Draw01OuterFrameData_cs
internal static class Numeric_Types_Draw01_Draw01OuterFrameData_cs
{
    static Numeric_Types_Draw01_Draw01OuterFrameData_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01OuterFrameData_cs");
}
