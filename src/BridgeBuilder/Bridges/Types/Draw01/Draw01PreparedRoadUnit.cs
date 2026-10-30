using System;
using BridgeBuilder.Runtime;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Offline native source-unit candidates. Sampling acceptance is tracked in the
// retained Draw01 reports; loading this graph does not certify its quality.
internal static class Draw01PreparedRoadUnit
{
    private readonly struct Unit
    {
        internal readonly string Key;
        internal readonly Vector2 SourceMin, SourceMax, Min, Max;
        internal Unit(string key, float x0, float y0, float x1, float y1, float u0, float v0, float u1, float v1)
        {
            Key = key;
            SourceMin = new Vector2(x0, y0);
            SourceMax = new Vector2(x1, y1);
            Min = new Vector2(u0, v0);
            Max = new Vector2(u1, v1);
        }
    }

    internal static MovableExistingRoadSurface.Patch Create(int domain, Vector4 u, Vector4 v, Matrix4x4 frame, float sourceUmin, float sourceUmax, Vector4? tangent = null)
    {
        var unit = domain switch
        {
            0 => new Unit("dfaf17e54e1938f1501398483bdc610c9046a2fbd097701940766b595fa4aeac", Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[0], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[1], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[2], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[3], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[4], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[5], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[6], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[7]),
            1 => new Unit("075ad0b2176777963bb613aa7dc920d51d53a5cbe783beb1aa24bce49cbadc14", Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[8], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[9], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[10], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[11], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[12], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[13], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[14], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[15]),
            2 => new Unit("d915d82270c2d03d2c38209fa87e82f89fb972821099165b21f894d43d3e2b9b", Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[16], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[17], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[18], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[19], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[20], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[21], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[22], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[23]),
            3 => new Unit("5fa17514a35be02b7a4884878d847a17d31ba163bfec3c2ed4d8f05954c8fbe4", Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[24], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[25], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[26], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[27], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[28], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[29], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[30], Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs.Values.F32[31]),
            _ => throw new ArgumentOutOfRangeException(nameof(domain))};
        // Finite U remains inside the actual producer guard. Continuous V can
        // cross any number of source periods without vertex modulo or cuts.
        float us = (unit.Max.x - unit.Min.x) / (unit.SourceMax.x - unit.SourceMin.x);
        float vs = (unit.Max.y - unit.Min.y) / (unit.SourceMax.y - unit.SourceMin.y);
        u *= us;
        u.w += unit.Min.x - unit.SourceMin.x * us;
        v *= vs;
        v.w += unit.Min.y - unit.SourceMin.y * vs;
        var min = new Vector2(unit.Min.x + (sourceUmin - unit.SourceMin.x) * us, unit.Min.y);
        var max = new Vector2(unit.Min.x + (sourceUmax - unit.SourceMin.x) * us, unit.Max.y);
        return new MovableExistingRoadSurface.Patch(min, max, frame.transpose * u, frame.transpose * v, BridgePreparedMaterials.RepeatUnit("Draw01", unit.Key), preparedUnit: true, repeatV: true, tangent: tangent, projectTangent: !tangent.HasValue).WithFiniteSampling(Vector2.zero, Vector2.one);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01PreparedRoadUnit_cs
internal static class Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs
{
    static Numeric_Types_Draw01_Draw01PreparedRoadUnit_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01PreparedRoadUnit_cs");
}
