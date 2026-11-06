using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
/// <summary>Recorded authored transverse panels, including their connectors, at every LOD.</summary>
internal static class GoldenSupportGeometry
{
    private static Dictionary<string, byte[]> Decoded => GenerationParameters.Get<Dictionary<string, byte[]>>("BridgeBuilder.Bridges.GoldenSupportGeometry.Decoded", () => new(StringComparer.Ordinal));
    private static Dictionary<string, (float[] Coefficients, int[] Vertices, bool[] Protected)> Rows => GenerationParameters.Get<Dictionary<string, (float[] Coefficients, int[] Vertices, bool[] Protected)>>("BridgeBuilder.Bridges.GoldenSupportGeometry.Rows", () => new(StringComparer.Ordinal));
    internal static bool IsRecorded(string? style, string name) => style == "SuspensionGolden" && GoldenSupportData.Maps.ContainsKey(name);
    internal static bool TryWiden(string name, float3[] source, float extra, out float3[] moved, out bool[] protectedSupport)
    {
        moved = Array.Empty<float3>();
        protectedSupport = Array.Empty<bool>();
        if (!GoldenSupportData.Maps.TryGetValue(name, out var encoded))
            return false;
        if (!Decoded.TryGetValue(name, out var data))
        {
            using var input = new MemoryStream(Convert.FromBase64String(encoded));
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            data = output.ToArray();
            Decoded.Add(name, data);
        }

        int stride = sizeof(float) + Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs.Values.I32[0];
        if (data.Length != source.Length * stride)
            return false;
        moved = (float3[])source.Clone();
        protectedSupport = new bool[source.Length];
        if (!Rows.TryGetValue(name, out var rows))
        {
            var coefficients = new float[source.Length];
            var selected = new List<int>();
            var protection = new bool[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                coefficients[i] = BitConverter.ToSingle(data, i * stride);
                if (!float.IsFinite(coefficients[i])) return false;
                protection[i] = data[i * stride + sizeof(float)] != 0;
                // A zero row is an authored copy, including its signed zero bits.
                if (coefficients[i] != 0f) selected.Add(i);
            }
            rows = (coefficients, selected.ToArray(), protection);
            Rows.Add(name, rows);
        }
        Array.Copy(rows.Protected, protectedSupport, protectedSupport.Length);
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.HalfWidthTable,
            new[] { source, moved }, new[] { rows.Coefficients }, new[] { extra },
            count: rows.Vertices.Length, indices: new[] { rows.Vertices });
        return true;
    }
}

// Embedded numerical arrays: Types_SuspensionGolden_GoldenSupportGeometry_cs
internal static class Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs
{
    static Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs.Values", () => HardcodedNumbers.Load("Types_SuspensionGolden_GoldenSupportGeometry_cs"));
}
