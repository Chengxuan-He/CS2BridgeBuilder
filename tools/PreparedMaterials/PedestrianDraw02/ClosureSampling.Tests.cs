using System;
using System.IO;
using System.Text.Json;
using BridgeBuilder.Bridges;

if (args.Length != 1) throw new ArgumentException("Pass the verified classification.json.");
using var report = JsonDocument.Parse(File.ReadAllText(args[0]));
int checks = 0, retained = 0, closures = 0, density = 0, unknown = 0;
foreach (var record in report.RootElement.GetProperty("meshes").EnumerateArray())
{
    string render = record.GetProperty("render").GetString()!;
    int mesh = record.GetProperty("mesh").GetInt32();
    foreach (var group in record.GetProperty("classifications").EnumerateObject())
    {
        bool expected = group.Name.StartsWith("native-", StringComparison.Ordinal);
        foreach (var domain in group.Value.EnumerateArray())
        {
            int index = domain.GetInt32();
            if (PedestrianDraw02UvSampling.For(render, mesh, index) != expected)
                throw new Exception($"Incorrect source sampling for {render}, {mesh}, {index}.");
            checks++; if (expected) retained++;
            if (group.Name == "native-uv0-finite-authored-closure") closures++;
            if (group.Name == "density-existing-road-repeat-overlap") density++;
            if (group.Name == "unknown-template-2-interior-sampling") unknown++;
        }
    }
    ExpectError(() => PedestrianDraw02UvSampling.For(render, mesh, -1));
    ExpectError(() => PedestrianDraw02UvSampling.For(render, mesh, record.GetProperty("domainCount").GetInt32()));
    ExpectError(() => PedestrianDraw02UvSampling.For(render, -1, 0));
}
if (PedestrianDraw02UvSampling.For("unregistered render", 0, 0)) throw new Exception("Unknown render admitted.");
if (checks != 47983 || retained != 47761 || closures != 78 || density != 216 || unknown != 6)
    throw new Exception("Closure or protected repeat membership changed.");
Console.WriteLine($"PASS source-linked runtime adapter: {checks} domains; {closures} closure domains retained; {density} repeat and {unknown} unresolved Windows rejected; all bounds checked.");
static void ExpectError(Func<bool> action)
{
    try { action(); }
    catch (InvalidOperationException) { return; }
    throw new Exception("Known render mismatch must fail explicitly.");
}
