using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class GoldenOrnamentGeometry
{
    private static readonly Dictionary<string, byte[]> Masks = Decode();
    private static Dictionary<string, byte[]> Decode()
    {
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var pair in GoldenOrnamentData.Maps)
            result.Add(pair.Key, Convert.FromBase64String(pair.Value.Mask));
        return result;
    }

    internal static bool IsRecorded(string mesh) => Masks.ContainsKey(mesh);
    internal static float3 Position(float3 source, float extra)
    {
        if (extra == Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.F32[0])
            return source;
        var scale = (GoldenOrnamentData.Span + extra) / GoldenOrnamentData.Span;
        var matrix = new AffineGeometry.Matrix(
            AffineGeometry.Row.Linear(source.x),
            AffineGeometry.Row.Affine(GoldenOrnamentData.AnchorY, source.y - GoldenOrnamentData.AnchorY),
            AffineGeometry.Row.Constant(source.z));
        matrix.Evaluate(scale, 0f, 0f, out float x, out float y, out float z);
        return new float3(x, y, z);
    }

    internal static bool TryApply(string mesh, float3[] source, float3[] moved, float extra)
    {
        if (!GoldenOrnamentData.Maps.TryGetValue(mesh, out var data) || source.Length != data.Count || moved.Length != data.Count || !float.IsFinite(extra) || GoldenOrnamentData.Span + extra <= Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.F32[1])
            return false;
        var mask = Masks[mesh];
        // Explicit user-requested exception (2026-09-22): treat the complete fan and
        // curved chord as one ornament; uniform XY scaling about the central spoke's
        // authored top replaces the ordinary width-only mapping. Z is unchanged.
        // Always read native coordinates, never scale the already widened result.
        AffineGeometry.For(0, source.Length, i =>
        {
            if ((mask[i / Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.I32[1]] & (Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.I32[2] << (i % Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.I32[3]))) != Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.I32[4])
                moved[i] = Position(source[i], extra);
        });
        return true;
    }
}

// Embedded numerical arrays: Types_SuspensionGolden_GoldenOrnamentGeometry_cs
internal static class Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs
{
    static Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_SuspensionGolden_GoldenOrnamentGeometry_cs");
}
