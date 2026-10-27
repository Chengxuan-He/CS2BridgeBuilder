using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured UV projection inputs. Repetition is evaluated in GPU pixels, never geometry.
internal static class MovableExistingRoadSurface
{
    // Generation-only texture strips inside one repeating unit. Destination is a
    // normalized U interval (V when BandRows is set); source rectangles retain native pixels.
    internal readonly struct RepeatBand
    {
        internal readonly float Start, End, RepeatY;
        internal readonly Vector2 Min, Max;
        internal RepeatBand(float start, float end, Vector2 min, Vector2 max, float repeatY = 1f)
        { RepeatY = repeatY; Start = start; End = end; Min = min; Max = max; }
    }

    internal readonly struct Patch
    {
        internal readonly Vector2 Min, Max;
        internal readonly Vector4 U, V;
        internal readonly bool Project, Repeat, BandRows;
        internal readonly SurfaceAsset? Surface;
        internal readonly RepeatBand[]? Bands;
        internal Patch(Vector2 min, Vector2 max) { Min = min; Max = max; U = V = default; Project = false; Repeat = false; BandRows = false; Surface = null; Bands = null; }
        internal Patch(Vector2 min, Vector2 max, Vector4 u, Vector4 v, SurfaceAsset? surface = null, bool repeat = false,
            RepeatBand[]? bands = null, bool bandRows = false)
        { Min = min; Max = max; U = u; V = v; Project = true; Surface = surface; Repeat = repeat; Bands = bands; BandRows = bandRows; }
    }

}
