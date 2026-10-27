using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
/// <summary>The native A-pylon cable assemblies stretch about their fixed tower axis x=0.</summary>
internal static class APylonCableGeometry
{
    private static readonly Dictionary<string, (int Index, float Weight)[]> Decoded = new(StringComparer.Ordinal);
    internal static bool IsRecorded(string? style, string name) => style == "Extradosed02" && APylonCableData.Maps.ContainsKey(name);
    internal static bool TryApply(string name, float3[] source, float extra, float3[] moved)
    {
        if (!APylonCableData.Maps.TryGetValue(name, out var map) || map.Count != source.Length || moved.Length != source.Length)
            return false;
        (int Index, float Weight)[] rows;
        lock (Decoded)
        {
            if (!Decoded.TryGetValue(name, out rows!))
            {
                using var input = new MemoryStream(Convert.FromBase64String(map.Data));
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                var data = output.ToArray();
                if (data.Length % Numeric_Types_Extradosed02_APylonCableGeometry_cs.Values.I32[0] != Numeric_Types_Extradosed02_APylonCableGeometry_cs.Values.I32[1])
                    return false;
                rows = new (int Index, float Weight)[data.Length / 8];
                for (int row = 0; row < rows.Length; row++)
                {
                    int vertex = BitConverter.ToInt32(data, row * 8);
                    float coefficient = BitConverter.ToSingle(data, row * 8 + 4);
                    if (vertex < 0 || vertex >= source.Length || !float.IsFinite(coefficient)) return false;
                    rows[row] = (vertex, coefficient);
                }
                Decoded.Add(name, rows);
            }
        }
        AffineGeometry.Scatter(rows, row => row.Index, row =>
        {
            moved[row.Index] = source[row.Index];
            moved[row.Index].x = AffineGeometry.AddScaled(source[row.Index].x, row.Weight, extra * 0.5f);
        });

        return true;
    }
}

// Embedded numerical arrays: Types_Extradosed02_APylonCableGeometry_cs
internal static class Numeric_Types_Extradosed02_APylonCableGeometry_cs
{
    static Numeric_Types_Extradosed02_APylonCableGeometry_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Extradosed02_APylonCableGeometry_cs");
}
