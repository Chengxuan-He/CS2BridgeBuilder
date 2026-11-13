using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace BridgeBuilder.UI;

// Reads the source canvas, including transparent margins. No browser image-load dependency.
internal static class BridgeIconDimensions
{
    internal static bool TryRead(Stream input, out float ratio)
    {
        using var copy = new MemoryStream();
        input.CopyTo(copy);
        var bytes = copy.ToArray();
        ratio = 0;
        bool Size(double width, double height)
        {
            var value = width / height;
            if (width <= 0 || height <= 0 || double.IsInfinity(value) || double.IsNaN(value)
                || value > float.MaxValue || value < float.Epsilon) return false;
            return true;
        }
        double w = 0, h = 0;
        uint Big(int i) => ((uint)bytes[i] << 24) | ((uint)bytes[i+1] << 16) | ((uint)bytes[i+2] << 8) | bytes[i+3];
        if (bytes.Length >= 24 && bytes[0] == 137 && Encoding.ASCII.GetString(bytes, 1, 7) == "PNG\r\n\x1a\n")
        { w = Big(16); h = Big(20); }
        else if (bytes.Length >= 10 && Encoding.ASCII.GetString(bytes, 0, 3) == "GIF")
        { w = bytes[6] | bytes[7] << 8; h = bytes[8] | bytes[9] << 8; }
        else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216)
        {
            var i = 2;
            while (i + 3 < bytes.Length)
            {
                if (bytes[i++] != 255) return false;
                while (i < bytes.Length && bytes[i] == 255) i++;
                if (i >= bytes.Length) return false;
                var marker = bytes[i++];
                if (marker == 217 || marker == 218) break;
                if (marker == 1 || marker >= 208 && marker <= 215) continue;
                if (i + 1 >= bytes.Length) return false;
                var length = bytes[i] << 8 | bytes[i+1];
                if (length < 2 || i + length > bytes.Length) return false;
                if (marker >= 192 && marker <= 207 && marker != 196 && marker != 200 && marker != 204)
                {
                    if (length < 7) return false;
                    h = bytes[i+3] << 8 | bytes[i+4]; w = bytes[i+5] << 8 | bytes[i+6]; break;
                }
                i += length;
            }
        }
        else
        {
            copy.Position = 0;
            // Road Builder SVGs declare the SVG 1.1 DTD. Canvas attributes do not require it.
            using var xml = XmlReader.Create(copy, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            xml.MoveToContent();
            if (xml.LocalName != "svg") return false;
            w = Length(xml.GetAttribute("width")); h = Length(xml.GetAttribute("height"));
            if (!Size(w, h))
            {
                var view = (xml.GetAttribute("viewBox") ?? "").Split(new[] {' ', ',', '\t', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                if (view.Length != 4 || !double.TryParse(view[2], NumberStyles.Float, CultureInfo.InvariantCulture, out w)
                    || !double.TryParse(view[3], NumberStyles.Float, CultureInfo.InvariantCulture, out h)) return false;
            }
        }
        if (!Size(w, h)) return false;
        ratio = (float)(w / h); return true;
    }

    private static double Length(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text!.Trim();
        var units = new[] { "px", "in", "cm", "mm", "pt", "pc" };
        var factors = new[] { 1d, 96d, 96d/2.54, 96d/25.4, 96d/72, 16d };
        for (var i = 0; i < units.Length; i++)
            if (text.EndsWith(units[i], StringComparison.OrdinalIgnoreCase))
                return double.TryParse(text.Substring(0, text.Length-2), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n*factors[i] : 0;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
