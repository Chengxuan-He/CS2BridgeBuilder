// Extracted from original DrawBridge01 mesh 0 at each native LOD (game 1.6.2f1).
// X is the authored surface fraction. Z parameterizes the common native motion domain;
// these triangles clip selected receivers and never move their Y/Z coordinates.
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw01RoadTemplates
{
    internal static MovableRoadTemplate.Face[] Get(int lod) => Levels[lod];
    private static readonly MovableRoadTemplate.Face[][] Levels = ReadPackedLevels();
    private static MovableRoadTemplate.Face[] ReadPackedLevels_0()
    {
        var d = Numeric_Types_Draw01_Draw01RoadTemplates_g_cs.Values;
        var values = new MovableRoadTemplate.Face[336];
        for (int i = 0; i < 168; i++)
            values[0 + i] = new(d.I32[0 + i * 3], (uint)(d.I32[1 + i * 3]), new Vector2(d.F32[0 + i * 6], d.F32[1 + i * 6]), new Vector2(d.F32[2 + i * 6], d.F32[3 + i * 6]), new Vector2(d.F32[4 + i * 6], d.F32[5 + i * 6]), d.I32[2 + i * 3]);
        for (int i = 0; i < 168; i++)
            values[168 + i] = new(d.I32[504 + i * 3], (uint)(d.I32[505 + i * 3]), new Vector2(d.F32[1008 + i * 6], -d.F32[1009 + i * 6]), new Vector2(d.F32[1010 + i * 6], -d.F32[1011 + i * 6]), new Vector2(d.F32[1012 + i * 6], -d.F32[1013 + i * 6]), d.I32[506 + i * 3]);
        return values;
    }

    private static MovableRoadTemplate.Face[] ReadPackedLevels_1()
    {
        var d = Numeric_Types_Draw01_Draw01RoadTemplates_g_cs.Values;
        var values = new MovableRoadTemplate.Face[296];
        for (int i = 0; i < 148; i++)
            values[0 + i] = new(d.I32[1008 + i * 3], (uint)(d.I32[1009 + i * 3]), new Vector2(d.F32[2016 + i * 6], d.F32[2017 + i * 6]), new Vector2(d.F32[2018 + i * 6], d.F32[2019 + i * 6]), new Vector2(d.F32[2020 + i * 6], d.F32[2021 + i * 6]), d.I32[1010 + i * 3]);
        for (int i = 0; i < 148; i++)
            values[148 + i] = new(d.I32[1452 + i * 3], (uint)(d.I32[1453 + i * 3]), new Vector2(d.F32[2904 + i * 6], -d.F32[2905 + i * 6]), new Vector2(d.F32[2906 + i * 6], -d.F32[2907 + i * 6]), new Vector2(d.F32[2908 + i * 6], -d.F32[2909 + i * 6]), d.I32[1454 + i * 3]);
        return values;
    }

    private static MovableRoadTemplate.Face[] ReadPackedLevels_2()
    {
        var d = Numeric_Types_Draw01_Draw01RoadTemplates_g_cs.Values;
        var values = new MovableRoadTemplate.Face[56];
        for (int i = 0; i < 28; i++)
            values[0 + i] = new(d.I32[1896 + i * 3], (uint)(d.I32[1897 + i * 3]), new Vector2(d.F32[3792 + i * 6], d.F32[3793 + i * 6]), new Vector2(d.F32[3794 + i * 6], d.F32[3795 + i * 6]), new Vector2(d.F32[3796 + i * 6], d.F32[3797 + i * 6]), d.I32[1898 + i * 3]);
        for (int i = 0; i < 28; i++)
            values[28 + i] = new(d.I32[1980 + i * 3], (uint)(d.I32[1981 + i * 3]), new Vector2(d.F32[3960 + i * 6], -d.F32[3961 + i * 6]), new Vector2(d.F32[3962 + i * 6], -d.F32[3963 + i * 6]), new Vector2(d.F32[3964 + i * 6], -d.F32[3965 + i * 6]), d.I32[1982 + i * 3]);
        return values;
    }

    private static MovableRoadTemplate.Face[][] ReadPackedLevels()
    {
        var values = new MovableRoadTemplate.Face[3][];
        values[0] = ReadPackedLevels_0();
        values[1] = ReadPackedLevels_1();
        values[2] = ReadPackedLevels_2();
        return values;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01RoadTemplates_g_cs
internal static class Numeric_Types_Draw01_Draw01RoadTemplates_g_cs
{
    static Numeric_Types_Draw01_Draw01RoadTemplates_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01RoadTemplates_g_cs");
}
