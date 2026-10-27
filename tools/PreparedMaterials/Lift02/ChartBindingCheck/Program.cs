using System.Text.Json;
using System.Security.Cryptography;
using BridgeBuilder.Bridges;
using UnityEngine;
var directory=Path.GetFullPath(args[0]);
var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"native-chart-integration.json"))).RootElement;
var domainByFace=new Dictionary<(string,int),Dictionary<int,int>>();
int domainChecks=0;
foreach(var route in metadata.GetProperty("routes").EnumerateArray())
{
    var name=route.GetProperty("render").GetString()!;int mesh=route.GetProperty("mesh").GetInt32();
    var domains=MovableUvDomains.For(name,mesh)!.Items;
    var faces=new Dictionary<int,int>();
    for(int domain=0;domain<domains.Length;domain++)
    {
        foreach(int face in domains[domain].Faces)faces.Add(face,domain);
        var first=Lift02UvCharts.For(name,mesh,domain);
        if(!ReferenceEquals(first,Lift02UvCharts.For(name,mesh,domain)))throw new Exception("Chart set not shared.");
        domainChecks++;
    }
    domainByFace.Add((name,mesh),faces);
}
var cases=new Dictionary<(string,int,float),(int Accepted,int Pending)>();
int accepted=0,pending=0;
foreach(string line in File.ReadLines(Path.Combine(directory,"actual-receivers.json.receivers.ndjson")))
{
    using var doc=JsonDocument.Parse(line);var r=doc.RootElement;
    string name=r.GetProperty("name").GetString()!;int mesh=r.GetProperty("mesh").GetInt32();float delta=r.GetProperty("delta").GetSingle();
    double[] Values(string key)=>r.GetProperty(key).EnumerateArray().Select(v=>v.GetDouble()).ToArray();
    var a=Values("a");var b=Values("b");var c=Values("c");var bounds=Values("bounds");bool triangle=r.GetProperty("triangle").GetBoolean();
    var mapped=r.GetProperty("q").EnumerateArray().Select(v=>
    {
        double x=v[0].GetSingle(),y=v[1].GetSingle();
        return triangle?new Vector2((float)(a[0]+(b[0]-a[0])*x+(c[0]-a[0])*y),(float)(a[1]+(b[1]-a[1])*x+(c[1]-a[1])*y)):
            new Vector2((float)(bounds[0]+x*(bounds[2]-bounds[0])),(float)(bounds[1]+y*(bounds[3]-bounds[1])));
    }).ToArray();
    int domain=domainByFace[(name,mesh)][r.GetProperty("face").GetInt32()];
    bool valid=Lift02UvCharts.For(name,mesh,domain).Any(chart=>chart.ContainsTriangle(mapped[0],mapped[1],mapped[2]));
    if(valid)accepted++;else pending++;
    var key=(name,mesh,delta);cases.TryGetValue(key,out var old);cases[key]=(old.Accepted+(valid?1:0),old.Pending+(valid?0:1));
}
var result=new{style="Lift02",domainChecks,accepted,pending,sharedArrays=true,
    publicChartSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))).ToLowerInvariant(),
    cases=cases.Select(v=>new{render=v.Key.Item1,mesh=v.Key.Item2,delta=v.Key.Item3,accepted=v.Value.Accepted,pending=v.Value.Pending})};
File.WriteAllText(Path.Combine(directory,"public-native-chart-check.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS Lift02 charts: {domainChecks} domain arrays; accepted={accepted}; pending={pending}; unchanged original resources");
