using BridgeBuilder.Bridges;
using System.Diagnostics;

var random = new Random(12031);
var rows = new (int Index, float Origin, float Coefficient)[120000];
for (int i = 0; i < rows.Length; i++)
    rows[i] = (i % 60000, (float)(random.NextDouble() * 200 - 100), (float)(random.NextDouble() * 4 - 2));
foreach (float width in new[] { -12f, -.125f, -0f, 0f, .125f, 3.5f, 16f })
{
    var expected = new float[60000];
    foreach (var row in rows) expected[row.Index] = row.Origin + row.Coefficient * width;
    for (int repeat = 0; repeat < 6; repeat++)
    {
        var actual = new float[expected.Length];
        AffineGeometry.Scatter(rows, row => row.Index,
            row => actual[row.Index] = AffineGeometry.AddScaled(row.Origin, row.Coefficient, width));
        for (int i = 0; i < actual.Length; i++) Equal(actual[i], expected[i]);
    }
}
foreach (float value in new[] { -0f, 0f, float.Epsilon, -float.Epsilon, -14.5f, 7.125f })
{
    Equal(AffineGeometry.Scale(value, 1f), value * 1f);
    Equal(AffineGeometry.Translate(value, -0f), value + -0f);
    Equal(AffineGeometry.AddScaled(value, 0f, -2f), value + 0f * -2f);
}
var indices = Enumerable.Range(0, 15000).Select(i => i % 5000).ToArray();
var counts = new int[5000];
AffineGeometry.Indices(indices, i => counts[i]++);
if (counts.Any(c => c != 3)) throw new Exception("Repeated index accumulation changed.");
var x = new AffineGeometry.Row(1f, 2f, -3f, 4f);
var y = new AffineGeometry.Row(5f, -.5f, .25f, 2f);
var z = new AffineGeometry.Row(-2f, 4f, 1f, -.75f);
Equal(x.Evaluate(2f, 3f, 4f), ((1f + 2f * 2f) + -3f * 3f) + 4f * 4f);
Equal(y.Evaluate(2f, 3f, 4f), ((5f + -.5f * 2f) + .25f * 3f) + 2f * 4f);
Equal(z.Evaluate(2f, 3f, 4f), ((-2f + 4f * 2f) + 1f * 3f) + -.75f * 4f);
var nested = new int[3][];
for (int i = 0; i < 10000; i++)
{
    float sx = (float)random.NextDouble(), sy = (float)random.NextDouble(), sz = -0f;
    float anchor = 15.25f, scale = (float)random.NextDouble() * 4;
    var matrix = new AffineGeometry.Matrix(AffineGeometry.Row.Linear(sx),
        AffineGeometry.Row.Affine(anchor, sy - anchor), AffineGeometry.Row.Constant(sz));
    matrix.Evaluate(scale, 0f, 0f, out float mx, out float my, out float mz);
    Equal(mx, sx * scale); Equal(my, anchor + (sy - anchor) * scale); Equal(mz, sz);
}
AffineGeometry.Groups(3, lod =>
{
    var data = new int[24000];
    AffineGeometry.For(0, data.Length, i => data[i] = i + lod);
    nested[lod] = data;
});
for (int lod = 0; lod < nested.Length; lod++)
    for (int i = 0; i < nested[lod].Length; i++)
        if (nested[lod][i] != i + lod) throw new Exception("Nested LOD output changed.");
Console.WriteLine("PASS sparse affine rows, XYZ width coefficients, signed zero, duplicate overwrite/accumulation, repeated parallel runs and nested LOD batches.");
static void Equal(float actual, float expected)
{
    if (BitConverter.SingleToInt32Bits(actual) != BitConverter.SingleToInt32Bits(expected))
        throw new Exception($"Float bits differ: {actual:R} / {expected:R}");
}
