using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

string root=Path.GetFullPath(args[0]);
string corpus=args.Length>2?Path.GetFullPath(args[2]):Path.Combine(root,"artifacts/prepared-materials/shared-current/receivers/Lift05.units.ndjson.gz");
using var inventory=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"artifacts/prepared-materials/Lift05/rebuild-native-v1/inputs/native-inputs.json")));
var digests=inventory.RootElement.GetProperty("sources").EnumerateArray().ToDictionary(s=>s.GetProperty("cid").GetString()!,s=>s.GetProperty("sha256").GetString()!);
var bindings=inventory.RootElement.GetProperty("bindings").EnumerateArray().ToDictionary(b=>(b.GetProperty("render").GetString()!,b.GetProperty("materialSlot").GetInt32()),b=>digests[b.GetProperty("surfaceCid").GetString()!]);
using var oldDocument=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"artifacts/backups/lift05-topology-recovery-20261026/charts.json")));
MovableUvChart Chart(JsonElement loops)
{
    var integers=new List<int>{loops.GetArrayLength()};var values=new List<double>();
    foreach(var loop in loops.EnumerateArray()){integers.Add(values.Count);integers.Add(loop.GetArrayLength());foreach(var p in loop.EnumerateArray())values.AddRange(p.EnumerateArray().Select(x=>x.GetDouble()));}
    return MovableUvChart.Read(integers.ToArray(),values.ToArray(),0);
}
var oldCharts=oldDocument.RootElement.GetProperty("charts").EnumerateArray().Select(c=>Chart(c.GetProperty("loops"))).ToArray();
var oldStreams=oldDocument.RootElement.GetProperty("streams").EnumerateArray().ToDictionary(s=>(s.GetProperty("name").GetString()!,s.GetProperty("mesh").GetInt32()),s=>s.GetProperty("domains").EnumerateArray().Select(d=>d.EnumerateArray().Select(i=>oldCharts[i.GetInt32()]).ToArray()).ToArray());
Vector2 V(JsonElement p)=>new(p[0].GetSingle(),p[1].GetSingle());
int total=0,original=0,prepared=0,unresolved=0,baseline=0,recoveryGain=0,recoveryRegression=0,regionRejected=0;
int degenerateLineContained=0,degenerateLineGradientValid=0;
var gradientMethod=typeof(MovableStructurePacking.Unit).GetMethod("PreservesGradient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
var groups=new Dictionary<string,int>();var examples=new Dictionary<string,object>();var units=new Dictionary<string,int>();
var lostOriginalCases=new List<JsonElement>();
var conditionedReasons=new Dictionary<string,int>();var conditionedExamples=new Dictionary<string,JsonElement>();
using var input=new StreamReader(new GZipStream(File.OpenRead(corpus),CompressionMode.Decompress));
string? line;
while((line=input.ReadLine())!=null)
{
    using var doc=JsonDocument.Parse(line);var r=doc.RootElement;
    string name=r.GetProperty("name").GetString()!;int mesh=r.GetProperty("mesh").GetInt32();int domain=r.GetProperty("originalDomain").GetInt32();
    var b=r.GetProperty("bounds");
    var unit=new MovableStructurePacking.Unit{Id=r.GetProperty("id").GetInt32(),Triangle=r.GetProperty("triangle").GetBoolean(),RepeatOnly=r.GetProperty("repeatOnly").GetBoolean(),A=V(r.GetProperty("a")),B=V(r.GetProperty("b")),C=V(r.GetProperty("c")),Bounds=new(b[0].GetSingle(),b[1].GetSingle(),b[2].GetSingle(),b[3].GetSingle()),Charts=r.GetProperty("charts").EnumerateArray().Select(Chart).ToArray()};
    unit.Points.AddRange(r.GetProperty("points").EnumerateArray().Select(V));
    if(r.GetProperty("exactPoints").ValueKind!=JsonValueKind.Null)unit.ExactPoints=r.GetProperty("exactPoints").EnumerateArray().Select(p=>p.EnumerateArray().Select(v=>v.GetDouble()).ToArray()).ToArray();
    if(r.GetProperty("exactCoordinates").ValueKind!=JsonValueKind.Null)unit.ExactCoordinates=r.GetProperty("exactCoordinates").EnumerateArray().ToDictionary(p=>V(p.GetProperty("key")),p=>p.GetProperty("value").EnumerateArray().Select(v=>v.GetDouble()).ToArray());
    if(r.GetProperty("exactSourceUvs").ValueKind!=JsonValueKind.Null)unit.ExactSourceUvs=r.GetProperty("exactSourceUvs").EnumerateArray().ToDictionary(p=>V(p.GetProperty("key")),p=>V(p.GetProperty("value")));
    bool direct=unit.CanUseSource;
    if(direct!=r.GetProperty("canUseSource").GetBoolean())throw new Exception("Actual Unit reconstruction disagrees with observer.");
    var current=unit.Charts;unit.Charts=oldStreams[(name,mesh)][domain];bool before=unit.CanUseSource;unit.Charts=current;
    baseline+=before?1:0;recoveryGain+=direct&&!before?1:0;recoveryRegression+=before&&!direct?1:0;
    if(before&&!direct)lostOriginalCases.Add(r.Clone());
    total++;
    var regions=MovableRepeatUnits.Regions("Lift05",bindings[(name,mesh)]);
    var mapping=direct?null:regions.Select(region=>MovableRepeatUnits.RegionMapping.Create(region,unit)).FirstOrDefault(m=>m!=null);
    string outcome;
    if(direct){original++;outcome="original";}
    else if(mapping!=null){prepared++;outcome="prepared";units.TryGetValue(mapping.Region.BindingKey,out int n);units[mapping.Region.BindingKey]=n+1;}
    else
    {
        unresolved++;
        bool contained=unit.Charts.Length!=0&&Enumerable.Range(0,unit.Points.Count/3).All(i=>unit.Charts.Any(c=>c.ContainsTriangle(unit.SourceUv(unit.Points[i*3]),unit.SourceUv(unit.Points[i*3+1]),unit.SourceUv(unit.Points[i*3+2]))));
        outcome=unit.RepeatOnly?"conditioned-phase":unit.Charts.Length==0?"no-chart":contained?"source-gradient-rejected":"outside-authored-chart";
        if(outcome=="no-chart"&&unit.Triangle)
        {
            var anchors=new[]{unit.A,unit.B,unit.C};
            var ends=(from a in anchors from c in anchors orderby (c-a).sqrMagnitude descending select (a,c)).First();
            double dx=(double)ends.c.x-ends.a.x,dy=(double)ends.c.y-ends.a.y,length=dx*dx+dy*dy;
            bool OnLine(Vector2 p){double x=(double)p.x-ends.a.x,y=(double)p.y-ends.a.y;return x*dy-y*dx==0&&x*dx+y*dy>=0&&x*dx+y*dy<=length;}
            if(length>0&&anchors.All(OnLine)&&unit.Points.Select(unit.SourceUv).All(OnLine))
            {
                degenerateLineContained++;
                if(Enumerable.Range(0,unit.Points.Count/3).All(i=>(bool)gradientMethod.Invoke(unit,new object[]{unit.Points[i*3],unit.Points[i*3+1],unit.Points[i*3+2],i*3})!))degenerateLineGradientValid++;
            }
        }
        if(regions.Any(c=>c.ContainsDomain(unit)))regionRejected++;
    }
    string key=name+"|"+mesh+"|"+outcome;groups.TryGetValue(key,out int count);groups[key]=count+1;
    if(!examples.ContainsKey(key))examples[key]=new{render=name,mesh,domain,unit.Id,delta=r.GetProperty("delta").GetDouble(),scenario=r.GetProperty("scenario").GetString(),outcome};
    if(unit.RepeatOnly&&mapping==null)
    {
        string reason="missing-exact-coordinates";
        if(unit.ExactCoordinates!=null)
        {
            var exact=unit.Points.Select(p=>unit.ExactCoordinates[p]).ToArray();
            unit.Points.Clear();unit.ExactPoints=exact;unit.ExactSourceUvs=new();unit.RepeatOnly=false;
            bool consistent=true;
            foreach(var q in exact)
            {
                var p=new Vector2((float)q[0],(float)q[1]);var uv=unit.SourceUv(q[0],q[1]);
                if(unit.ExactSourceUvs.TryGetValue(p,out var prior)&&!prior.Equals(uv))consistent=false;
                unit.ExactSourceUvs[p]=uv;unit.Points.Add(p);
            }
            bool covered=unit.Charts.Length!=0&&Enumerable.Range(0,unit.Points.Count/3).All(i=>unit.Charts.Any(c=>c.ContainsTriangle(unit.SourceUv(unit.Points[i*3]),unit.SourceUv(unit.Points[i*3+1]),unit.SourceUv(unit.Points[i*3+2]))));
            bool gradientOk=Enumerable.Range(0,unit.Points.Count/3).All(i=>(bool)gradientMethod.Invoke(unit,new object[]{unit.Points[i*3],unit.Points[i*3+1],unit.Points[i*3+2],i*3})!);
            reason=!consistent?"float-key-conflict":!covered?(gradientOk?"outside-chart-gradient-valid":"outside-chart-and-gradient-invalid"):gradientOk?"covered-gradient-valid":"covered-gradient-invalid";
        }
        conditionedReasons.TryGetValue(reason,out int n);conditionedReasons[reason]=n+1;
        if(!conditionedExamples.ContainsKey(reason))conditionedExamples[reason]=r.Clone();
    }
}
var result=new{style="Lift05",total,original,prepared,unresolved,baselineOriginal=baseline,recoveryGain,recoveryRegression,regionRejected,degenerateLineContained,degenerateLineGradientValid,units,groups,examples=examples.Values,
    conditionedReasons,conditionedExamples,lostOriginalCases,corpusSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(corpus))).ToLowerInvariant(),
    metadataPreserved=true,observerDecisionsReproduced=true,completeOwnerMaterialBytes=(long?)null,
    limitation="Exact production Unit and RegionMapping applied to preserved finite observer records. Old-chart comparison changes only Charts and is not a re-execution of old Retile construction. No tiles.Build, native material save or complete owner was executed."};
Directory.CreateDirectory(Path.GetDirectoryName(args[1])!);File.WriteAllText(args[1],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(new{total,original,prepared,unresolved,baseline,recoveryGain,recoveryRegression,regionRejected}));
