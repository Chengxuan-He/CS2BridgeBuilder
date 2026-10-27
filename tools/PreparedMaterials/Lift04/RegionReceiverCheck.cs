using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

using var bindings=JsonDocument.Parse(File.ReadAllText(args[1]));
var hashes=bindings.RootElement.GetProperty("surfaces").EnumerateArray().ToDictionary(s=>s.GetProperty("cid").GetString()!,s=>s.GetProperty("sourceSHA256").GetString()!);
var sources=bindings.RootElement.GetProperty("renderBindings").EnumerateArray().ToDictionary(s=>s.GetProperty("render").GetString()!,s=>s.GetProperty("originalSurfaceCids").EnumerateArray().Select(c=>c.GetString()!).ToArray());
var groups=new Dictionary<string,(string Render,string Digest,MovableStructurePacking.Unit Unit)>();
int ordinal=0;
foreach(var line in File.ReadLines(args[0]))
{
    using var record=JsonDocument.Parse(line);var r=record.RootElement;
    string name=r.GetProperty("name").GetString()!;int mesh=r.GetProperty("mesh").GetInt32(),domain=r.GetProperty("domain").GetInt32();
    bool fragment=r.GetProperty("fragment").GetBoolean();
    string key=name+"/"+mesh+"/"+r.GetProperty("delta").GetRawText()+"/"+domain+(fragment?"/fragment/"+ordinal:"");ordinal++;
    Vector2 V(string n){var a=r.GetProperty(n);return new(a[0].GetSingle(),a[1].GetSingle());}
    double[][]? Exact(string n)=>r.GetProperty(n).ValueKind==JsonValueKind.Null?null:r.GetProperty(n).EnumerateArray().Select(q=>new[]{q[0].GetDouble(),q[1].GetDouble()}).ToArray();
    if(!groups.TryGetValue(key,out var entry))
    {
        var b=r.GetProperty("bounds");
        var created=new MovableStructurePacking.Unit{Triangle=r.GetProperty("triangle").GetBoolean(),
            Bounds=new(b[0].GetSingle(),b[1].GetSingle(),b[2].GetSingle(),b[3].GetSingle()),A=V("a"),B=V("b"),C=V("c"),
            RepeatOnly=r.GetProperty("repeatOnly").GetBoolean(),Charts=Lift04UvCharts.For(name,mesh,domain),ExactPoints=Exact("exactPoints")};
        entry=(name,hashes[sources[name][mesh]],created);groups.Add(key,entry);
    }
    var unit=entry.Unit;var points=r.GetProperty("points").EnumerateArray().Select(q=>new Vector2(q[0].GetSingle(),q[1].GetSingle())).ToArray();
    unit.Points.AddRange(points);var exact=Exact("exact");
    if(r.GetProperty("hasExactCoordinates").GetBoolean())
    {unit.ExactCoordinates=new();for(int i=0;i<points.Length;i++)unit.ExactCoordinates[points[i]]=exact![i];}
    if(r.GetProperty("hasExactSourceUvs").GetBoolean())
    {unit.ExactSourceUvs=new();for(int i=0;i<points.Length;i++)unit.ExactSourceUvs[points[i]]=unit.SourceUv(exact![i][0],exact[i][1]);}
}
var counts=new Dictionary<string,int>();var perRender=new Dictionary<string,Dictionary<string,int>>();
var domainOutcomes=new List<object>();
void Add(string name,string key,int n){counts[key]=counts.GetValueOrDefault(key)+n;if(!perRender.TryGetValue(name,out var row))perRender.Add(name,row=new());row[key]=row.GetValueOrDefault(key)+n;}
foreach(var (identity,entry) in groups)
{
    var unit=entry.Unit;int faces=unit.Points.Count/3;Add(entry.Render,"receivers",faces);Add(entry.Render,"units",1);
    void Outcome(string kind)=>domainOutcomes.Add(new{identity,sourceDigest=entry.Digest,receivers=faces,kind});
    if(unit.CanUseSource){Add(entry.Render,"nativeSource",faces);Outcome("nativeSource");continue;}
    var regions=MovableRepeatUnits.Regions("Lift04",entry.Digest);
    var mapping=regions.Select(region=>MovableRepeatUnits.RegionMapping.Create(region,unit)).FirstOrDefault(m=>m!=null);
    if(mapping!=null)
    {
        string kind=mapping.Region.BindingKey switch
        {
            "92669b0bf58299589e4be13863ef777c4d818f28436c9abf4f70a2489da35eb9"=>"floorRegion",
            "649d6588b132d3b40bd9e329c2071e6df523b547530119704f5c3699143144a7"=>"beamRegion",
            "f443914a8812fa50dda65930e5046391736fc0c5a1bca024561c108f99fcd96f"=>"wideBeamProfile",
            "0cfb7f02cf0395308cd5f8034908a3263eb8415c39866bd76321f969df01e06e"=>"outerFlangeProfile",
            _=>throw new Exception("Structural mapping must use Default template binding.")
        };
        foreach(var p in unit.Points)
        {
            var q=mapping.Map(p);
            if(!float.IsFinite(q.x)||!float.IsFinite(q.y))throw new Exception("Nonfinite region output");
            if((!mapping.Region.RepeatU&&(q.x<.25f||q.x>.75f))||(!mapping.Region.RepeatV&&(q.y<.25f||q.y>.75f)))
                throw new Exception("Nonperiodic profile escaped its guarded central interval.");
        }
        Add(entry.Render,kind,faces);Add(entry.Render,"genuineRegion",faces);Add(entry.Render,"genuineRegionUnits",1);Outcome(kind);continue;
    }
    Add(entry.Render,"remaining",faces);
    Outcome("remaining");
    var eligible=regions.Where(r=>r.ContainsDomain(unit)).ToArray();
    bool OutsideProfile(MovableRepeatUnits.Region region)=>unit.Points.Select(p=>region.MapExact(unit,p))
        .Any(q=>(!region.RepeatU&&(q[0]<0||q[0]>1))||(!region.RepeatV&&(q[1]<0||q[1]>1)));
    bool rejectedProfile=eligible.Length>0&&eligible.All(OutsideProfile);
    if(rejectedProfile&&eligible.Any(r=>MovableRepeatUnits.RegionMapping.Create(r,unit)!=null))throw new Exception("U/V extrapolation was accepted.");
    Add(entry.Render,regions.Count==0?"differentSource":eligible.Length==0?"outsideMeasuredRegion":unit.RepeatOnly&&unit.ExactCoordinates==null?"missingExactPhase":rejectedProfile?"nonperiodicAxisExtrapolation":"float32OrDegenerateGuard",faces);
}
if(counts["receivers"]!=counts.GetValueOrDefault("nativeSource")+counts.GetValueOrDefault("genuineRegion")+counts.GetValueOrDefault("remaining"))throw new Exception("Coverage accounting mismatch.");
if(MovableRepeatUnits.Regions("Lift04","not-the-source-digest").Count!=0)throw new Exception("Unverified source accepted.");
var counterexample=groups["LiftBridge04 Mesh/0/2/2"];
var counterexampleMapping=MovableRepeatUnits.Regions("Lift04",counterexample.Digest)
    .Select(region=>MovableRepeatUnits.RegionMapping.Create(region,counterexample.Unit)).FirstOrDefault(m=>m!=null);
if(counterexampleMapping?.Region.BindingKey!="649d6588b132d3b40bd9e329c2071e6df523b547530119704f5c3699143144a7")
    throw new Exception("Original beam face 4/domain 2 at delta 2 did not select its verified repeating strip.");
var output=JsonSerializer.Serialize(new{status="PASS",counts,perRender,beamCounterexamplePassed=true,productionRegionPredicate=true,productionWholeDomainGrouping=true,exactParentPhaseRecorded=true,textureBakes=0,gameVisualAcceptance=false},new JsonSerializerOptions{WriteIndented=true});
File.WriteAllText(args[2],output);Console.WriteLine(output);
File.WriteAllText(args[2]+".domains.json",JsonSerializer.Serialize(domainOutcomes));
