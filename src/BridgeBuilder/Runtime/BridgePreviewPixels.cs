using System;
using BridgeBuilder.Bridges;
using UnityEngine;

namespace BridgeBuilder.Runtime;

// Detached CPU arrays only; native color conversion and texture access stay on the caller.
internal static class BridgePreviewPixels
{
    internal static bool Accumulate(Color[] linear, Color32[] mask, Color[] accumulated,
        int width, int height, int scale)
    {
        int sourceWidth = checked(width * scale);
        if (linear.Length != checked(sourceWidth * height * scale) || mask.Length != linear.Length ||
            accumulated.Length != checked(width * height))
            throw new ArgumentException("Preview pixel dimensions differ.");
        var visible = new bool[height];
        AffineGeometry.Groups(height, y =>
        {
            bool rowVisible = false;
            for (int x = 0; x < width; x++)
            {
                var sum = new Color();
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    int index = (y * scale + sy) * sourceWidth + x * scale + sx;
                    var p = linear[index];
                    float alpha = mask[index].a / 255f;
                    if (alpha <= 0f) continue;
                    if ((p.r > 0f || p.g > 0f || p.b > 0f) &&
                        !float.IsNaN(p.r) && !float.IsNaN(p.g) && !float.IsNaN(p.b)) rowVisible = true;
                    sum.r += Finite(p.r) * alpha;
                    sum.g += Finite(p.g) * alpha;
                    sum.b += Finite(p.b) * alpha;
                    sum.a += alpha;
                }
                int output = y * width + x;
                var prior = accumulated[output];
                accumulated[output] = new Color(prior.r + sum.r, prior.g + sum.g,
                    prior.b + sum.b, prior.a + sum.a);
            }
            visible[y] = rowVisible;
        });
        foreach (bool row in visible) if (row) return true;
        return false;
    }

    private static float Finite(float value)
        => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value);
}
