using System.IO;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Immutable pixel recipe shared by offline preparation and runtime lookup.
// Receiver projections and bridge dimensions do not change these source pixels.
internal static class MovableRepeatKey
{
    internal const string Recipe = "BridgeBuilder/RoadRepeat/1";

    internal static string For(string sourceDigest, Vector2 min, Vector2 max, bool cutout,
        MovableExistingRoadSurface.RepeatBand[]? bands, bool rows)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        writer.Write(Recipe);
        writer.Write(sourceDigest);
        writer.Write(min.x); writer.Write(min.y);
        writer.Write(max.x); writer.Write(max.y);
        writer.Write(cutout); writer.Write(rows);
        writer.Write(bands?.Length ?? -1);
        if (bands != null)
            foreach (var band in bands)
            {
                writer.Write(band.Start); writer.Write(band.End); writer.Write(band.RepeatY);
                writer.Write(band.Min.x); writer.Write(band.Min.y);
                writer.Write(band.Max.x); writer.Write(band.Max.y);
            }
        writer.Flush();
        return MovableStructureKey.Digest(bytes.ToArray());
    }
}
