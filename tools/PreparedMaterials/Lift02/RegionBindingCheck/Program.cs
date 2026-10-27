using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;
var root=Path.GetFullPath(args[0]);
var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"source-rebuild/native-source-inventory.json"))).RootElement;
var byCid=source.GetProperty("surfaces").EnumerateArray().ToDictionary(s=>s.GetProperty("cid").GetString()!,s=>s.GetProperty("sha256").GetString()!);
var byRender=source.GetProperty("renders").EnumerateArray().ToDictionary(s=>s.GetProperty("render").GetString()!,s=>s.GetProperty("surfaceCids").EnumerateArray().Select(c=>byCid[c.GetString()!]).ToArray());
var faceDomains=new Dictionary<(string,int),Dictionary<int,int>>();
var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"source-sharing/native-chart-integration.json")));
foreach(var route in metadata.RootElement.GetProperty("routes").EnumerateArray())
{
    string name=route.GetProperty("render").GetString()!;int mesh=route.GetProperty("mesh").GetInt32();
    var domains=MovableUvDomains.For(name,mesh)!.Items;
    var faces=new Dictionary<int,int>();
    for(int domain=0;domain<domains.Length;domain++)
        foreach(int face in domains[domain].Faces)faces.Add(face,domain);
    faceDomains.Add((name,mesh),faces);
}
var cases=new Dictionary<(string,int,float),(int Original,int Repeat,int Pending)>();
var units=new Dictionary<string,int>();int total=0,originals=0,regions=0,pending=0,gradientRejected=0;
foreach(var line in File.ReadLines(Path.Combine(root,"source-sharing/actual-receivers.json.receivers.ndjson")))
{
    using var doc=JsonDocument.Parse(line);var r=doc.RootElement;
    string name=r.GetProperty("name").GetString()!;int mesh=r.GetProperty("mesh").GetInt32(),face=r.GetProperty("face").GetInt32();float delta=r.GetProperty("delta").GetSingle();
    Vector2 Point(string n)=>new(r.GetProperty(n)[0].GetSingle(),r.GetProperty(n)[1].GetSingle());
    var b=r.GetProperty("bounds");var unit=new MovableStructurePacking.Unit{Triangle=r.GetProperty("triangle").GetBoolean(),A=Point("a"),B=Point("b"),C=Point("c"),Bounds=new(b[0].GetSingle(),b[1].GetSingle(),b[2].GetSingle(),b[3].GetSingle())};
    foreach(var q in r.GetProperty("q").EnumerateArray())unit.Points.Add(new(q[0].GetSingle(),q[1].GetSingle()));
    var candidates=MovableRepeatUnits.Regions("Lift02",byRender[name][mesh]);
    var mapping=candidates.Select(region=>MovableRepeatUnits.RegionMapping.Create(region,unit)).FirstOrDefault(m=>m!=null);
    var mapped=unit.Points.Select(p=>unit.Triangle
        ?new Vector2((float)(unit.A.x+((double)unit.B.x-unit.A.x)*p.x+((double)unit.C.x-unit.A.x)*p.y),(float)(unit.A.y+((double)unit.B.y-unit.A.y)*p.x+((double)unit.C.y-unit.A.y)*p.y))
        :new Vector2((float)(unit.Bounds.x+((double)unit.Bounds.z-unit.Bounds.x)*p.x),(float)(unit.Bounds.y+((double)unit.Bounds.w-unit.Bounds.y)*p.y))).ToArray();
    bool original=Lift02UvCharts.For(name,mesh,faceDomains[(name,mesh)][face]).Any(c=>c.ContainsTriangle(mapped[0],mapped[1],mapped[2]));
    var key=(name,mesh,delta);cases.TryGetValue(key,out var state);total++;
    if(original){originals++;state.Original++;}
    else if(mapping!=null)
    {
        regions++;state.Repeat++;units.TryGetValue(mapping.Region.BindingKey,out var count);units[mapping.Region.BindingKey]=count+1;
        foreach(var point in unit.Points)if(!float.IsFinite(mapping.Map(point).x)||!float.IsFinite(mapping.Map(point).y))throw new Exception("Nonfinite mapped UV");
    }
    else {pending++;state.Pending++;if(candidates.Any(c=>c.ContainsDomain(unit)))gradientRejected++;}
    cases[key]=state;
}
var result=new{style="Lift02",total,originals,regions,pending,gradientRejected,units,cases=cases.Select(c=>new{render=c.Key.Item1,mesh=c.Key.Item2,halfDelta=c.Key.Item3,original=c.Value.Original,repeat=c.Value.Repeat,pending=c.Value.Pending}),
    limitations="Source-linked public RegionMapping and original chart containment run on preserved finite halfDelta0/2/8 receiver corpus. Original count is containment only; legacy corpus has no unconditioned ExactCoordinates. This is finite coverage evidence, not full current runtime Unit construction or future width certification."};
File.WriteAllText(Path.Combine(root,"structure-units/region-check.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS Lift02 public regions: receivers={total}; original={originals}; repeat={regions}; pending={pending}; gradient-rejected={gradientRejected}");
