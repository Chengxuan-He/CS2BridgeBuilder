using System.Text.Json;
using BridgeBuilder.Bridges;

using var document = JsonDocument.Parse(File.ReadAllBytes(args[0]));
var root = document.RootElement;
var identities = new Dictionary<int, MovableUvChart>();
int domains = 0, references = 0;
if (Draw01UvCharts.For("Unknown", 0, 0).Length != 0) throw new Exception("Unknown mesh must have no Draw01 chart.");
foreach (var mesh in root.GetProperty("meshes").EnumerateArray())
{
    string name = mesh.GetProperty("render").GetString()!;
    int index = mesh.GetProperty("mesh").GetInt32();
    foreach (var domain in mesh.GetProperty("domains").EnumerateArray())
    {
        int number = domain.GetProperty("index").GetInt32();
        var expected = domain.GetProperty("charts").EnumerateArray().Select(v => v.GetInt32()).ToArray();
        var actual = Draw01UvCharts.For(name, index, number);
        if (!ReferenceEquals(actual, Draw01UvCharts.For(name, index, number))) throw new Exception("Repeated domain access allocated an array.");
        if (actual.Length != expected.Length) throw new Exception("Domain binding length mismatch.");
        for (int c = 0; c < expected.Length; c++)
        {
            int id = expected[c];
            if (identities.TryGetValue(id, out var previous) && !ReferenceEquals(previous, actual[c])) throw new Exception("Duplicate chart instance.");
            identities[id] = actual[c];
            var loops = root.GetProperty("charts")[id];
            if (loops.GetArrayLength() != actual[c].Loops.Length) throw new Exception("Loop count mismatch.");
            for (int l = 0; l < actual[c].Loops.Length; l++)
            {
                var points = loops[l].EnumerateArray().SelectMany(p => p.EnumerateArray().Select(v => v.GetDouble())).ToArray();
                if (!points.Select(BitConverter.DoubleToInt64Bits).SequenceEqual(actual[c].Loops[l].Select(BitConverter.DoubleToInt64Bits))) throw new Exception("Boundary precision changed.");
            }
            references++;
        }
        domains++;
    }
}
if (identities.Count != MovableUvChart.Reads) throw new Exception("Unreferenced chart was loaded.");
var report = new { status="PASS", domains, references, uniqueChartInstances=identities.Count,
    repeatedLookupsReuseInstances=true, exactFloat64Boundaries=true,
    scope="Production Draw01 loader and BBN reader with isolated shared-interface stand-in; not native rendering acceptance." };
File.WriteAllText(args[1], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented=true }));
Console.WriteLine(JsonSerializer.Serialize(report));

// Only the parent-owned Read ABI is represented here. This test cannot certify
// the parent's polygon containment, density, filtering or native material path.
namespace BridgeBuilder.Bridges
{
    internal sealed class MovableUvChart
    {
        internal static int Reads;
        internal double[][] Loops = Array.Empty<double[]>();
        internal static MovableUvChart Read(int[] integers, double[] coordinates, int offset)
        {
            Reads++;
            int count = integers[offset++];
            var result = new MovableUvChart { Loops = new double[count][] };
            for (int i=0; i<count; i++)
            {
                int start=integers[offset++], points=integers[offset++];
                if (points < 3) throw new Exception("A chart ring needs three points.");
                result.Loops[i] = coordinates.AsSpan(start, points*2).ToArray();
                if (!result.Loops[i].All(double.IsFinite)) throw new Exception("Nonfinite chart boundary.");
                if (result.Loops[i][0]==result.Loops[i][^2] && result.Loops[i][1]==result.Loops[i][^1]) throw new Exception("Implicitly closed loop duplicates its last point.");
            }
            return result;
        }
    }
}
