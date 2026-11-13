using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Native FileAsset text envelope. The BBS1 payload already contains complete
// source and container bytes; no second compiled bundle is persisted.
internal static class BridgeCustomShaderPackage
{
    private const string Header = "BridgeBuilder.MaterialProgram/1";
    private const int MaximumPackageBytes = 80 * 1024 * 1024;

    internal static bool TryWrite(Stream output, int sourceTemplate, byte[] package, out string? error)
        => TryWriteConfigured(output, sourceTemplate, package, Array.Empty<int>(), out error);

    internal static bool TryWriteConfigured(Stream output, int sourceTemplate, byte[] package, int[] queues, out string? error)
    {
        error = null;
        try
        {
            using var writer = new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true);
            writer.WriteLine(queues.Length == 0 ? Header : "BridgeBuilder.MaterialProgram/2");
            writer.WriteLine(sourceTemplate.ToString(CultureInfo.InvariantCulture));
            if (queues.Length != 0) writer.WriteLine(string.Join(",", queues.Distinct().OrderBy(value => value).Select(value => value.ToString(CultureInfo.InvariantCulture))));
            writer.WriteLine(Convert.ToBase64String(package));
            writer.Flush();
            return true;
        }
        catch (Exception exception)
        { error = "Generated material package write failed: " + exception.Message; return false; }
    }

    internal static bool TryRead(Stream input, out int sourceTemplate, out byte[] package, out string? error)
        => TryReadConfigured(input, out sourceTemplate, out package, out _, out error);

    internal static bool TryReadConfigured(Stream input, out int sourceTemplate, out byte[] package, out int[] queues, out string? error)
    {
        sourceTemplate = -1; package = Array.Empty<byte>(); queues = Array.Empty<int>(); error = null;
        try
        {
            // The native file stream is bounded before decoding its text envelope.
            if (!input.CanSeek || input.Length - input.Position > (long)MaximumPackageBytes * 4 / 3 + 1024)
            { error = "Generated material package stream is unbounded or oversized."; return false; }
            using var reader = new StreamReader(input, Encoding.UTF8, true, 4096, leaveOpen: true);
            string? header = reader.ReadLine();
            if ((header != Header && header != "BridgeBuilder.MaterialProgram/2") || !int.TryParse(reader.ReadLine(), NumberStyles.None,
                CultureInfo.InvariantCulture, out var original) || original < 0)
            { error = "Generated material package text header is invalid."; return false; }
            var variants = Array.Empty<int>();
            if (header == "BridgeBuilder.MaterialProgram/2")
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrEmpty(line) || line!.Length > 1024)
                { error = "Generated material queue variants are missing or oversized."; return false; }
                var values = line.Split(','); variants = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                    if (!int.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out variants[i]))
                    { error = "Generated material queue variant is invalid."; return false; }
                if (variants.Distinct().Count() != variants.Length)
                { error = "Generated material queue variants are duplicated."; return false; }
            }
            string? encoded = reader.ReadLine();
            if (encoded == null || reader.ReadLine() != null)
            { error = "Generated material package text is incomplete or has trailing content."; return false; }
            var decoded = Convert.FromBase64String(encoded);
            if (decoded.Length < 8 || decoded.Length > MaximumPackageBytes ||
                BitConverter.ToUInt32(decoded, 0) != 0x31534242u || !Version(decoded))
            { error = "Generated material program header is invalid."; return false; }
            sourceTemplate = original; package = decoded; queues = variants; return true;
        }
        catch (Exception exception)
        { error = "Generated material package read failed: " + exception.Message; return false; }
    }
    private static bool Version(byte[] bytes) => BitConverter.ToInt32(bytes, 4) == 1 || BitConverter.ToInt32(bytes, 4) == 2;
}
