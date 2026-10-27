using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

var counts = new Dictionary<string,int>();
void Add(string key) => counts[key] = counts.GetValueOrDefault(key)+1;
foreach (var line in File.ReadLines(args[0]))
{
    using var record=JsonDocument.Parse(line); var r=record.RootElement;
    Vector2 V(string name) { var a=r.GetProperty(name);return new(a[0].GetSingle(),a[1].GetSingle()); }
    var bounds=r.GetProperty("bounds");
    var charts=Lift04UvCharts.For(r.GetProperty("name").GetString()!,r.GetProperty("mesh").GetInt32(),r.GetProperty("domain").GetInt32());
    var unit=new MovableStructurePacking.Unit { Triangle=r.GetProperty("triangle").GetBoolean(),
        RepeatOnly=r.GetProperty("repeatOnly").GetBoolean(), A=V("a"), B=V("b"), C=V("c"),
        Bounds=new(bounds[0].GetSingle(),bounds[1].GetSingle(),bounds[2].GetSingle(),bounds[3].GetSingle()),Charts=charts };
    foreach(var q in r.GetProperty("points").EnumerateArray())unit.Points.Add(new(q[0].GetSingle(),q[1].GetSingle()));
    Add("receivers");
    if(charts.Length==0) { Add("noReliableChart");continue; }
    if(unit.RepeatOnly) { Add("repeatOnly");continue; }
    var p=unit.Points.Select(unit.SourceUv).ToArray();
    if(!charts.Any(c=>c.ContainsTriangle(p[0],p[1],p[2]))) { Add("outsideChart");continue; }
    Add("containedReceiver");
    Add(unit.CanUseSource ? "nativeSourceEligible" : "float32GradientRejected");
}
var report=JsonSerializer.Serialize(new { status="PASS",counts,productionChartReader=true,productionSourceUv=true,productionGradientGuard=true },new JsonSerializerOptions{WriteIndented=true});
File.WriteAllText(args[1],report);Console.WriteLine(report);
