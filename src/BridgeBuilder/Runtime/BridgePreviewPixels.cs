using System;
using BridgeBuilder.Bridges;
using UnityEngine;

namespace BridgeBuilder.Runtime;

// Detached CPU arrays only; native color conversion and texture access stay on the caller.
internal static class BridgePreviewPixels
{
    internal static void Accumulate(Color[] linear, Color32[] mask, Color[] accumulated,
        int width, int height, int scale)
    {
        int sourceWidth = width * scale;
        AffineGeometry.Groups(height, y =>
        {
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
                    sum.r += Math.Max(0f, p.r) * alpha;
                    sum.g += Math.Max(0f, p.g) * alpha;
                    sum.b += Math.Max(0f, p.b) * alpha;
                    sum.a += alpha;
                }
                int output = y * width + x;
                var prior = accumulated[output];
                accumulated[output] = new Color(prior.r + sum.r, prior.g + sum.g,
                    prior.b + sum.b, prior.a + sum.a);
            }
        });
    }

}
