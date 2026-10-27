using BridgeBuilder.Runtime;
using UnityEngine;
using System.Diagnostics;

const int width = 1536, height = 768, scale = 2;
var random = new Random(21);
var pixels = new Color[width * height * scale * scale];
var mask = new Color32[pixels.Length];
for (int i = 0; i < pixels.Length; i++)
{
    pixels[i] = new Color((float)random.NextDouble() * 100, (float)random.NextDouble(), -1f, 1f);
    mask[i].a = (byte)random.Next(256);
}
pixels[0].r = float.NaN; pixels[1].g = float.PositiveInfinity; pixels[2].b = float.NegativeInfinity;
var expected = new Color[width * height];
var actual = new Color[expected.Length];
var clock = Stopwatch.StartNew();
bool serialVisible = false;
for (int sample = 0; sample < 4; sample++) serialVisible = Serial();
var serialMs = clock.Elapsed.TotalMilliseconds;
clock.Restart();
bool visible = false;
for (int sample = 0; sample < 4; sample++) visible = BridgePreviewPixels.Accumulate(pixels, mask, actual, width, height, scale);
var parallelMs = clock.Elapsed.TotalMilliseconds;
if (visible != serialVisible) throw new Exception("Visibility changed.");
for (int i = 0; i < actual.Length; i++)
{
    Equal(expected[i].r, actual[i].r); Equal(expected[i].g, actual[i].g);
    Equal(expected[i].b, actual[i].b); Equal(expected[i].a, actual[i].a);
}
Array.Clear(mask);
if (BridgePreviewPixels.Accumulate(pixels, mask, actual, width, height, scale)) throw new Exception("Transparent image visible.");
Console.WriteLine($"PASS four samples, exact float output, HDR/nonfinite and transparency. CPU kernel only: serial={serialMs:F1}ms parallel={parallelMs:F1}ms.");
bool Serial()
{
    bool found = false;
    for (int y = 0; y < height; y++)
    for (int x = 0; x < width; x++)
    {
        var sum = new Color();
        for (int sy = 0; sy < scale; sy++)
        for (int sx = 0; sx < scale; sx++)
        {
            int index = (y * scale + sy) * width * scale + x * scale + sx;
            var p = pixels[index]; var a = mask[index].a / 255f;
            if (a <= 0) continue;
            if ((p.r > 0 || p.g > 0 || p.b > 0) && !float.IsNaN(p.r) && !float.IsNaN(p.g) && !float.IsNaN(p.b)) found = true;
            sum.r += Finite(p.r) * a; sum.g += Finite(p.g) * a; sum.b += Finite(p.b) * a; sum.a += a;
        }
        int i = y * width + x;
        expected[i] = new(expected[i].r + sum.r, expected[i].g + sum.g, expected[i].b + sum.b, expected[i].a + sum.a);
    }
    return found;
}
static float Finite(float x) => float.IsNaN(x) || float.IsInfinity(x) ? 0 : (0 > x ? 0 : x);
static void Equal(float a, float b) { if (BitConverter.SingleToInt32Bits(a) != BitConverter.SingleToInt32Bits(b)) throw new Exception("Pixel bits changed."); }
namespace UnityEngine
{
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public struct Color32 { public byte r,g,b,a; }
}
