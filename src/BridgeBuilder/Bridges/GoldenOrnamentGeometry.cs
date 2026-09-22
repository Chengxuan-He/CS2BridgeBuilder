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
        if (extra == 0f) return source;
        var scale = (GoldenOrnamentData.Span + extra) / GoldenOrnamentData.Span;
        return new float3(source.x * scale,
            GoldenOrnamentData.AnchorY + (source.y - GoldenOrnamentData.AnchorY) * scale,
            source.z);
    }

    internal static bool TryApply(string mesh, float3[] source, float3[] moved, float extra)
    {
        if (!GoldenOrnamentData.Maps.TryGetValue(mesh, out var data)
            || source.Length != data.Count || moved.Length != data.Count
            || !float.IsFinite(extra) || GoldenOrnamentData.Span + extra <= 0f)
            return false;
        var mask = Masks[mesh];
        // Explicit user-requested exception (2026-09-22): treat the complete fan and
        // curved chord as one ornament; uniform XY scaling about the central spoke's
        // authored top replaces the ordinary width-only mapping. Z is unchanged.
        // Always read native coordinates, never scale the already widened result.
        for (var i = 0; i < source.Length; i++)
            if ((mask[i / 8] & (1 << (i % 8))) != 0)
                moved[i] = Position(source[i], extra);
        return true;
    }
}
