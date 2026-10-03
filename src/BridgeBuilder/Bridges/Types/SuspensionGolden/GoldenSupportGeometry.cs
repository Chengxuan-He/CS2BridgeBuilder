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

    internal static bool IsRecorded(string? style, string name) =>
        style == "SuspensionGolden" && GoldenSupportData.Maps.ContainsKey(name);

    internal static bool TryWiden(string name, float3[] source, float extra, out float3[] moved,
        out bool[] protectedSupport)
    {
        moved = Array.Empty<float3>();
        protectedSupport = Array.Empty<bool>();
        if (!GoldenSupportData.Maps.TryGetValue(name, out var encoded)) return false;
        if (!Decoded.TryGetValue(name, out var data))
        {
            using var input = new MemoryStream(Convert.FromBase64String(encoded));
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            data = output.ToArray();
            Decoded.Add(name, data);
        }
        const int stride = sizeof(float) + 1;
        if (data.Length != source.Length * stride) return false;
        moved = (float3[])source.Clone();
        protectedSupport = new bool[source.Length];
        for (var i = 0; i < moved.Length; i++)
        {
            var coefficient = BitConverter.ToSingle(data, i * stride);
            protectedSupport[i] = data[i * stride + sizeof(float)] != 0;
            if (!float.IsFinite(coefficient)) return false;
            // Preserve the centre truss coordinates exactly, including signed zero.
            if (coefficient == 0f) continue;
            moved[i].x = source[i].x + coefficient * (extra * 0.5f);
        }
        return true;
    }
}
