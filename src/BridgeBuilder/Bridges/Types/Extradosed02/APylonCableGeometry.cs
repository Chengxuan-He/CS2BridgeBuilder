using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>The native A-pylon cable assemblies stretch about their fixed tower axis x=0.</summary>
internal static class APylonCableGeometry
{
    private static readonly Dictionary<string, byte[]> Decoded = new(StringComparer.Ordinal);

    internal static bool IsRecorded(string? style, string name) =>
        style == "Extradosed02" && APylonCableData.Maps.ContainsKey(name);

    internal static bool TryApply(string name, float3[] source, float extra, float3[] moved)
    {
        if (!APylonCableData.Maps.TryGetValue(name, out var map)
            || map.Count != source.Length || moved.Length != source.Length) return false;
        if (!Decoded.TryGetValue(name, out var data))
        {
            using var input = new MemoryStream(Convert.FromBase64String(map.Data));
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            data = output.ToArray();
            Decoded.Add(name, data);
        }
        if (data.Length % 8 != 0) return false;
        for (var offset = 0; offset < data.Length; offset += 8)
        {
            var vertex = BitConverter.ToInt32(data, offset);
            var coefficient = BitConverter.ToSingle(data, offset + 4);
            if (vertex < 0 || vertex >= source.Length || !float.IsFinite(coefficient)) return false;
            // Always derive from the native position, never the generic side translation.
            // Full-detail and LOD maps inherit the same native centre-crossing cable span.
            moved[vertex] = source[vertex];
            moved[vertex].x += coefficient * (extra * 0.5f);
        }
        return true;
    }
}
