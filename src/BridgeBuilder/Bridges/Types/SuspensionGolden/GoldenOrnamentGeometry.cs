using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
internal static class GoldenOrnamentGeometry
{
    private static Dictionary<string, int[]> Masks => GenerationParameters.Get<Dictionary<string, int[]>>("BridgeBuilder.Bridges.GoldenOrnamentGeometry.Masks", () => Decode());
    private static Dictionary<string, int[]> Decode()
    {
        var result = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var pair in GoldenOrnamentData.Maps)
        {
            var bytes = Convert.FromBase64String(pair.Value.Mask);
            var rows = new List<int>();
            for (int i = 0; i < pair.Value.Count; i++)
                if ((bytes[i / 8] & (1 << (i % 8))) != 0)
                    rows.Add(i);
            result.Add(pair.Key, rows.ToArray());
        }

        return result;
    }

    internal static bool IsRecorded(string mesh) => Masks.ContainsKey(mesh);
    internal static float3 Position(float3 source, float extra)
    {
        if (extra == Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.F32[0])
            return source;
        var input = new[]
        {
            source
        };
        var output = new float3[1];
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.Ornament, new[] { input, output }, Array.Empty<float[]>(), new[] { GoldenOrnamentData.Span, extra, GoldenOrnamentData.AnchorY }, count: 1, indices: new[] { new[] { 0 } });
        return output[0];
    }

    internal static bool TryApply(string mesh, float3[] source, float3[] moved, float extra)
    {
        if (!GoldenOrnamentData.Maps.ContainsKey(mesh))
            return false;
        var mask = Masks[mesh];
        // Explicit user-requested exception (2026-09-22): treat the complete fan and
        // curved chord as one ornament; uniform XY scaling about the central spoke's
        // authored top replaces the ordinary width-only mapping. Z is unchanged.
        // Always read native coordinates, never scale the already widened result.
        if (extra == Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values.F32[0])
        {
            foreach (var coordinateRows in StructureRows.Stages(mask))
                AffineGeometry.Execute(RangePrograms.Data, 0, new[] { source, moved }, Array.Empty<float[]>(), Array.Empty<float>(), count: coordinateRows.Length, indices: new[] { coordinateRows });
        }
        else
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.Ornament, new[] { source, moved }, Array.Empty<float[]>(), new[] { GoldenOrnamentData.Span, extra, GoldenOrnamentData.AnchorY }, count: mask.Length, indices: new[] { mask });
        return true;
    }

    private static class RangePrograms
    {
        static RangePrograms()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.GoldenOrnamentGeometry.RangePrograms.Data", () => HardcodedNumbers.Load("Types_SuspensionGolden_GoldenOrnamentGeometry_RangePrograms"));
    }
}

// Embedded numerical arrays: Types_SuspensionGolden_GoldenOrnamentGeometry_cs
internal static class Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs
{
    static Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_SuspensionGolden_GoldenOrnamentGeometry_cs.Values", () => HardcodedNumbers.Load("Types_SuspensionGolden_GoldenOrnamentGeometry_cs"));
}
