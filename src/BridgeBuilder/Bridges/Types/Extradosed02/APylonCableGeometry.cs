using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
/// <summary>The native A-pylon cable assemblies stretch about their fixed tower axis x=0.</summary>
internal static class APylonCableGeometry
{
    private static Dictionary<string, (int[] Vertices, float[] Coefficients)> Decoded => GenerationParameters.Get<Dictionary<string, (int[] Vertices, float[] Coefficients)>>("BridgeBuilder.Bridges.APylonCableGeometry.Decoded", () => new(StringComparer.Ordinal));
    internal static bool IsRecorded(string? style, string name) => style == "Extradosed02" && APylonCableData.Maps.ContainsKey(name);
    internal static bool TryApply(string name, float3[] source, float extra, float3[] moved)
    {
        if (!APylonCableData.Maps.TryGetValue(name, out var map))
            return false;
        (int[] Vertices, float[] Coefficients) rows;
        lock (Decoded)
        {
            if (!Decoded.TryGetValue(name, out rows))
            {
                using var input = new MemoryStream(Convert.FromBase64String(map.Data));
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                var data = output.ToArray();
                var last = new Dictionary<int, int>();
                var coefficients = new float[source.Length];
                for (int row = 0; row < data.Length / 8; row++)
                {
                    int vertex = BitConverter.ToInt32(data, row * 8);
                    float coefficient = BitConverter.ToSingle(data, row * 8 + 4);
                    last[vertex] = row;
                    coefficients[vertex] = coefficient;
                }

                rows = (last.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray(), coefficients);
                Decoded.Add(name, rows);
            }
        }

        foreach (var coordinateRows in StructureRows.Stages(rows.Vertices))
            AffineGeometry.Execute(RangePrograms.Data, 0, new[] { source, moved }, Array.Empty<float[]>(), Array.Empty<float>(), count: coordinateRows.Length, indices: new[] { coordinateRows });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.HalfWidthTable, new[] { source, moved }, new[] { rows.Coefficients }, new[] { extra }, count: rows.Vertices.Length, indices: new[] { rows.Vertices });
        return true;
    }

    private static class RangePrograms
    {
        static RangePrograms()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.APylonCableGeometry.RangePrograms.Data", () => HardcodedNumbers.Load("Types_Extradosed02_APylonCableGeometry_RangePrograms"));
    }
}

// Embedded numerical arrays: Types_Extradosed02_APylonCableGeometry_cs
internal static class Numeric_Types_Extradosed02_APylonCableGeometry_cs
{
    static Numeric_Types_Extradosed02_APylonCableGeometry_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Extradosed02_APylonCableGeometry_cs.Values", () => HardcodedNumbers.Load("Types_Extradosed02_APylonCableGeometry_cs"));
}
