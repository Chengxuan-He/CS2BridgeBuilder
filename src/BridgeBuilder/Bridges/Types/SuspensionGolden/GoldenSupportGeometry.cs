using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
/// <summary>Recorded authored transverse panels, including their connectors, at every LOD.</summary>
internal static class GoldenSupportGeometry
{
    private static readonly Dictionary<string, byte[]> Decoded = new(StringComparer.Ordinal);
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
        var outputPositions = moved;
        var protectedVertices = protectedSupport;
        for (int i = 0; i < source.Length; i++)
            if (!float.IsFinite(BitConverter.ToSingle(data, i * stride))) return false;
        AffineGeometry.For(0, source.Length, i =>
        {
            float coefficient = BitConverter.ToSingle(data, i * stride);
            protectedVertices[i] = data[i * stride + sizeof(float)] != 0;
            // Preserve the authored zero coefficient path, including signed zero.
            if (coefficient != 0f)
                outputPositions[i].x = AffineGeometry.AddScaled(source[i].x, coefficient, extra * 0.5f);
        });

        return true;
    }
}

// Embedded numerical arrays: Types_SuspensionGolden_GoldenSupportGeometry_cs
internal static class Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs
{
    static Numeric_Types_SuspensionGolden_GoldenSupportGeometry_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_SuspensionGolden_GoldenSupportGeometry_cs");
}
