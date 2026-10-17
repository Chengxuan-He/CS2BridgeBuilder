using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Offline measurements of the native LaneLine atlas. These are geometry masks,
// not generated textures. Paint is sampled from native bridge object atlases by MovableRoadPaint.
internal static class MovableExistingMarkings
{
    internal readonly struct Stroke
    {
        internal readonly Vector4 A, D;
        internal readonly float Width, Offset;
        internal readonly bool Yellow;
        internal Stroke(Vector4 a, Vector4 d, float width, float offset, bool yellow)
        { A = a; D = d; Width = width; Offset = offset; Yellow = yellow; }
    }
    internal static IEnumerable<Stroke> Strokes(SurfaceAsset source, Vector4 a, Vector4 d,
        Colossal.Mathematics.Bounds2 area)
    {
        // Other native curved decals use the existing atlas's stripe presentation.
        // No original surface is cloned, re-templated, tinted or saved.
        if (!source.name.StartsWith("LaneLine", StringComparison.Ordinal))
        { yield return new Stroke(a, d, 1f, 0f, false); yield break; }
        var uvWidth = area.max.x - area.min.x;
        var uvHeight = area.max.y - area.min.y;
        if (!(uvWidth > 0f) || !(uvHeight > 0f) || !(d.w > a.w))
            throw new InvalidOperationException("Invalid native marking UV interval.");
        var v0 = Mathf.LerpUnclamped(area.min.y, area.max.y, a.w);
        var v1 = Mathf.LerpUnclamped(area.min.y, area.max.y, d.w);
        for (var repeat = Mathf.FloorToInt(v0); repeat < Mathf.CeilToInt(v1); repeat++)
            foreach (var p in Patterns)
            {
                var x0 = Mathf.Max(area.min.x, p.X0); var x1 = Mathf.Min(area.max.x, p.X1);
                var y0 = Mathf.Max(v0, repeat + p.Y0); var y1 = Mathf.Min(v1, repeat + p.Y1);
                if (x1 <= x0 || y1 <= y0) continue;
                yield return new Stroke(Vector4.LerpUnclamped(a, d, (y0-v0)/(v1-v0)),
                    Vector4.LerpUnclamped(a, d, (y1-v0)/(v1-v0)),
                    (x1-x0)/uvWidth, ((x0+x1)*.5f-area.min.x)/uvWidth-.5f, p.Yellow);
            }
    }
    private static readonly (float X0, float Y0, float X1, float Y1, bool Yellow)[] Patterns =
    {
        (0.21484375f, 0f, 0.22265625f, 1f, false),
        (0.341796875f, 0f, 0.349609375f, 1f, false),
        (0.357421875f, 0f, 0.365234375f, 1f, false),
        (0.404296875f, 0f, 0.412109375f, 1f, false),
        (0.466796875f, 0f, 0.490234375f, 1f, false),
        (0.529296875f, 0f, 0.537109375f, 1f, true),
        (0.61328125f, 0f, 0.62109375f, 1f, true),
        (0.626953125f, 0f, 0.63671875f, 1f, true),
        (0.67578125f, 0f, 0.68359375f, 1f, true),
        (0.021484375f, 0.015625f, 0.103515625f, 0.046875f, false),
        (0.740234375f, 0.03125f, 0.76171875f, 0.078125f, false),
        (0.171875f, 0.0390625f, 0.181640625f, 0.203125f, false),
        (0.021484375f, 0.078125f, 0.103515625f, 0.109375f, false),
        (0.296875f, 0.09375f, 0.306640625f, 0.4140625f, false),
        (0.021484375f, 0.140625f, 0.103515625f, 0.171875f, false),
        (0.73828125f, 0.140625f, 0.76171875f, 0.1953125f, false),
        (0.130859375f, 0.1640625f, 0.140625f, 0.328125f, false),
        (0.021484375f, 0.203125f, 0.103515625f, 0.234375f, false),
        (0.73828125f, 0.25f, 0.76171875f, 0.3046875f, false),
        (0.021484375f, 0.265625f, 0.103515625f, 0.296875f, false),
        (0.171875f, 0.2890625f, 0.181640625f, 0.453125f, false),
        (0.021484375f, 0.328125f, 0.103515625f, 0.359375f, false),
        (0.255859375f, 0.3359375f, 0.265625f, 0.65625f, false),
        (0.419921875f, 0.3359375f, 0.427734375f, 0.65625f, false),
        (0.5703125f, 0.3359375f, 0.580078125f, 0.65625f, true),
        (0.689453125f, 0.3359375f, 0.69921875f, 0.65625f, true),
        (0.740234375f, 0.3671875f, 0.76171875f, 0.4140625f, false),
        (0.021484375f, 0.390625f, 0.103515625f, 0.421875f, false),
        (0.021484375f, 0.453125f, 0.103515625f, 0.484375f, false),
        (0.740234375f, 0.4765625f, 0.76171875f, 0.5234375f, false),
        (0.021484375f, 0.515625f, 0.103515625f, 0.546875f, false),
        (0.171875f, 0.5390625f, 0.181640625f, 0.7109375f, false),
        (0.021484375f, 0.578125f, 0.103515625f, 0.609375f, false),
        (0.740234375f, 0.5859375f, 0.76171875f, 0.640625f, false),
        (0.296875f, 0.6015625f, 0.306640625f, 0.921875f, false),
        (0.021484375f, 0.640625f, 0.103515625f, 0.671875f, false),
        (0.130859375f, 0.6640625f, 0.140625f, 0.8359375f, false),
        (0.73828125f, 0.6953125f, 0.76171875f, 0.75f, false),
        (0.021484375f, 0.703125f, 0.103515625f, 0.734375f, false),
        (0.021484375f, 0.765625f, 0.103515625f, 0.796875f, false),
        (0.171875f, 0.7890625f, 0.181640625f, 0.953125f, false),
        (0.73828125f, 0.8046875f, 0.76171875f, 0.859375f, false),
        (0.021484375f, 0.828125f, 0.103515625f, 0.859375f, false),
        (0.021484375f, 0.890625f, 0.103515625f, 0.921875f, false),
        (0.740234375f, 0.921875f, 0.76171875f, 0.96875f, false),
        (0.021484375f, 0.953125f, 0.103515625f, 0.984375f, false),
    };
}
