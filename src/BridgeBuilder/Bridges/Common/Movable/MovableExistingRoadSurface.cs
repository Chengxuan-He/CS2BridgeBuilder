using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured UV projection inputs. Repetition is evaluated in GPU pixels, never geometry.
internal static class MovableExistingRoadSurface
{
    internal readonly struct Patch
    {
        internal readonly Vector2 Min, Max;
        internal readonly Vector4 U, V;
        internal readonly bool Project, Repeat;
        internal readonly SurfaceAsset? Surface;
        internal Patch(Vector2 min, Vector2 max) { Min = min; Max = max; U = V = default; Project = false; Repeat = false; Surface = null; }
        internal Patch(Vector2 min, Vector2 max, Vector4 u, Vector4 v, SurfaceAsset? surface = null, bool repeat = false)
        { Min = min; Max = max; U = u; V = v; Project = true; Surface = surface; Repeat = repeat; }
    }

}
