using System.Collections;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using UnityEngine;

if(args.Length<5)throw new ArgumentException("Usage: RetileRegionCheck <DLL> <Managed> <units.ndjson.gz> <source-map.json> <report.json> [--current-charts] [--chart-overrides <json>]");
bool current=false;string? overridePath=null;
for(int at=5;at<args.Length;at++)
{
    if(args[at]=="--current-charts")current=true;
    else if(args[at]=="--chart-overrides"&&at+1<args.Length)overridePath=args[++at];
    else throw new ArgumentException("Unknown option: "+args[at]);
}
const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
var managed=Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{
    var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
Type Type(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true)!;
var unitType=Type("MovableStructurePacking+Unit");
var fields=unitType.GetFields(flags).ToDictionary(f=>f.Name);
var chartType=Type("MovableUvChart");var readChart=chartType.GetMethod("Read",flags)!;
var currentCharts=Type("MovableUvCharts").GetMethod("For",flags)!;
var eligible=unitType.GetProperty("CanUseSource",flags)!;
var gradient=unitType.GetMethod("PreservesGradient",flags)!;
var sourceUv=unitType.GetMethod("SourceUv",flags,null,new[]{typeof(Vector2)},null)!;
var exactSourceUv=unitType.GetMethod("SourceUv",flags,null,new[]{typeof(double),typeof(double)},null)!;
var containsTriangle=chartType.GetMethod("ContainsTriangle",flags)!;
var getRegions=Type("MovableRepeatUnits").GetMethod("Regions",flags)!;
var create=Type("MovableRepeatUnits+RegionMapping").GetMethod("Create",flags)!;
var regionType=Type("MovableRepeatUnits+Region");var contains=regionType.GetMethod("ContainsDomain",flags)!;
var binding=regionType.GetField("BindingKey",flags)!;
using var sourceMap=JsonDocument.Parse(File.ReadAllText(args[3]));
var sources=sourceMap.RootElement.EnumerateArray().ToDictionary(
    r=>(r.GetProperty("style").GetString()!,r.GetProperty("render").GetString()!,r.GetProperty("mesh").GetInt32()),
    r=>r.GetProperty("sourceDigest").GetString()!);
if(sources.Values.Any(s=>s.Length!=64||s.Any(c=>!Uri.IsHexDigit(c))))throw new Exception("Expected exact source SHA256 digests.");
using var overrideDocument=overridePath==null?null:JsonDocument.Parse(File.ReadAllText(overridePath));
var overrides=overrideDocument==null?new Dictionary<(string,string,int,int),JsonElement>():overrideDocument.RootElement.EnumerateArray().ToDictionary(
    r=>(r.GetProperty("style").GetString()!,r.GetProperty("render").GetString()!,r.GetProperty("mesh").GetInt32(),r.GetProperty("originalDomain").GetInt32()),r=>r.GetProperty("charts"));
var overridesUsed=new HashSet<(string,string,int,int)>();
var regionCache=new Dictionary<(string,string),object[]>();
var counts=new Dictionary<string,long>();var directReasons=new Dictionary<string,long>();var repeatOnlyReasons=new Dictionary<string,long>();var uses=new Dictionary<string,long>();var scenarios=new Dictionary<string,Dictionary<string,long>>();
var representatives=new Dictionary<string,List<object>>();
void Count(Dictionary<string,long> table,string key){table.TryGetValue(key,out long n);table[key]=n+1;}
Vector2 V(JsonElement e)=>new(e[0].GetSingle(),e[1].GetSingle());
Array Charts(JsonElement record,string style,string render,int mesh)
{
    var key=(style,render,mesh,record.GetProperty("originalDomain").GetInt32());
    bool replaced=overrides.TryGetValue(key,out var charts);
    if(replaced)overridesUsed.Add(key);
    else if(current)return (Array)currentCharts.Invoke(null,new object[]{style,render,mesh,key.Item4})!;
    else charts=record.GetProperty("charts");
    var output=Array.CreateInstance(chartType,charts.GetArrayLength());int index=0;
    foreach(var chart in charts.EnumerateArray())
    {
        var integers=new List<int>{chart.GetArrayLength()};var doubles=new List<double>();
        foreach(var loop in chart.EnumerateArray())
        {integers.Add(doubles.Count);integers.Add(loop.GetArrayLength());foreach(var p in loop.EnumerateArray()){doubles.Add(p[0].GetDouble());doubles.Add(p[1].GetDouble());}}
        output.SetValue(readChart.Invoke(null,new object[]{integers.ToArray(),doubles.ToArray(),0}),index++);
    }
    return output;
}
object Restore(JsonElement r,string style,string render,int mesh)
{
    var u=Activator.CreateInstance(unitType,true)!;
    void Set(string name,object? value)=>fields[name].SetValue(u,value);
    Set("Id",r.GetProperty("id").GetInt32());Set("Triangle",r.GetProperty("triangle").GetBoolean());Set("RepeatOnly",r.GetProperty("repeatOnly").GetBoolean());
    if(fields.ContainsKey("RetainedAxes"))Set("RetainedAxes",r.TryGetProperty("retainedAxes",out var axes)?axes.GetInt32():0);
    Set("A",V(r.GetProperty("a")));Set("B",V(r.GetProperty("b")));Set("C",V(r.GetProperty("c")));
    var b=r.GetProperty("bounds");Set("Bounds",new Vector4(b[0].GetSingle(),b[1].GetSingle(),b[2].GetSingle(),b[3].GetSingle()));
    var points=(List<Vector2>)fields["Points"].GetValue(u)!;foreach(var p in r.GetProperty("points").EnumerateArray())points.Add(V(p));
    Set("Min",new Vector2(points.Min(p=>p.x),points.Min(p=>p.y)));Set("Max",new Vector2(points.Max(p=>p.x),points.Max(p=>p.y)));
    Set("Charts",Charts(r,style,render,mesh));
    var exact=r.GetProperty("exactPoints");Set("ExactPoints",exact.ValueKind==JsonValueKind.Null?null:exact.EnumerateArray().Select(a=>a.EnumerateArray().Select(x=>x.GetDouble()).ToArray()).ToArray());
    foreach(var pair in new[]{("ExactCoordinates","exactCoordinates"),("ExactSourceUvs","exactSourceUvs")})
    {
        var entries=r.GetProperty(pair.Item2);if(entries.ValueKind==JsonValueKind.Null){Set(pair.Item1,null);continue;}
        var dictionary=(IDictionary)Activator.CreateInstance(fields[pair.Item1].FieldType)!;
        foreach(var e in entries.EnumerateArray())dictionary.Add(V(e.GetProperty("key")),pair.Item1=="ExactSourceUvs"?(object)V(e.GetProperty("value")):e.GetProperty("value").EnumerateArray().Select(x=>x.GetDouble()).ToArray());
        Set(pair.Item1,dictionary);
    }
    return u;
}
string DirectReason(object u)
{
    if((bool)fields["RepeatOnly"].GetValue(u)!)return "RepeatOnly";
    var points=(List<Vector2>)fields["Points"].GetValue(u)!;
    if(points.Count==0||points.Count%3!=0||points.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)))return "invalid-points";
    var charts=(Array)fields["Charts"].GetValue(u)!;bool triangle=(bool)fields["Triangle"].GetValue(u)!;
    if(charts.Length==0&&points.Any(p=>p.x<0||p.y<0||(triangle?(double)p.x+p.y>1:p.x>1||p.y>1)))return "absentCharts-outside-parent";
    for(int at=0;at<points.Count;at+=3)
    {
        var p=points.Skip(at).Take(3).ToArray();var mapped=p.Select(v=>sourceUv.Invoke(u,new object[]{v})).ToArray();
        if(charts.Length!=0&&!charts.Cast<object>().Any(c=>(bool)containsTriangle.Invoke(c,mapped)!))return "ContainsTriangle";
        if(!(bool)gradient.Invoke(u,new object[]{p[0],p[1],p[2],at})!)return "PreservesGradient";
    }
    throw new Exception("Direct failure did not reproduce production predicate.");
}
string RepeatOnlyReason(object u)
{
    if(!(bool)fields["RepeatOnly"].GetValue(u)!)return "";
    if(fields["ExactCoordinates"].GetValue(u) is not Dictionary<Vector2,double[]> coordinates)return "missing-exact-coordinates";
    var oldPoints=(List<Vector2>)fields["Points"].GetValue(u)!;
    if(oldPoints.Any(p=>!coordinates.ContainsKey(p)))return "missing-exact-coordinate-key";
    var candidate=Activator.CreateInstance(unitType,true)!;
    foreach(var field in fields.Values)if(field.Name!="Points")field.SetValue(candidate,field.GetValue(u));
    fields["RepeatOnly"].SetValue(candidate,false);
    var exact=oldPoints.Select(p=>coordinates[p]).ToArray();
    fields["ExactPoints"].SetValue(candidate,exact);
    var points=(List<Vector2>)fields["Points"].GetValue(candidate)!;
    var native=new Dictionary<Vector2,Vector2>();
    foreach(var q in exact)
    {
        var p=new Vector2((float)q[0],(float)q[1]);points.Add(p);
        var uv=(Vector2)exactSourceUv.Invoke(candidate,new object[]{q[0],q[1]})!;
        if(native.TryGetValue(p,out var previous)&&!previous.Equals(uv))return "source-UV-key-collision";
        native[p]=uv;
    }
    fields["ExactSourceUvs"].SetValue(candidate,native);
    return (bool)eligible.GetValue(candidate)!?"reconstructed-original-eligible":DirectReason(candidate);
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[4]))!);
using var compressed=File.OpenRead(args[2]);using var unpack=new GZipStream(compressed,CompressionMode.Decompress);using var input=new StreamReader(unpack);
using var recordsFile=File.Create(args[4]+".classifications.ndjson.gz");using var recordZip=new GZipStream(recordsFile,System.IO.Compression.CompressionLevel.Fastest);using var records=new StreamWriter(recordZip);
long lines=0,directParityChanges=0,overriddenUnits=0;
while(input.ReadLine() is { } line)
{
    lines++;using var document=JsonDocument.Parse(line);var r=document.RootElement;
    string style=r.GetProperty("style").GetString()!,render=r.GetProperty("name").GetString()!;int mesh=r.GetProperty("mesh").GetInt32();
    if(!sources.TryGetValue((style,render,mesh),out string? digest))throw new Exception($"Missing exact source-map entry: {style}/{render}/{mesh}; line {lines}");
    bool overridden=overrides.ContainsKey((style,render,mesh,r.GetProperty("originalDomain").GetInt32()));
    if(overridden)overriddenUnits++;
    var u=Restore(r,style,render,mesh);bool direct=(bool)eligible.GetValue(u)!;
    if(direct!=r.GetProperty("canUseSource").GetBoolean())
    {directParityChanges++;if(!current&&!overridden)throw new Exception($"Captured CanUseSource parity failed at line {lines}.");}
    string status="original",reason="",key="",directReason=direct?"":DirectReason(u);
    if(!direct)Count(directReasons,directReason);
    string repeatOnlyReason=RepeatOnlyReason(u);if(repeatOnlyReason.Length>0)Count(repeatOnlyReasons,repeatOnlyReason);
    if(!direct)
    {
        if(!regionCache.TryGetValue((style,digest),out var regions))
            regionCache.Add((style,digest),regions=((IEnumerable)getRegions.Invoke(null,new object[]{style,digest})!).Cast<object>().ToArray());
        bool contained=false;object? mapped=null;
        foreach(var region in regions)
        {
            contained|=(bool)contains.Invoke(region,new[]{u})!;
            mapped=create.Invoke(null,new[]{region,u});
            if(mapped!=null){key=(string)binding.GetValue(region)!;Count(uses,style+"/"+key);break;}
        }
        status=mapped==null?"remaining":"repeat";
        if(mapped==null)reason=regions.Length==0?"no-source-region":!contained?"outside-recorded-coverage":"contained-but-mapping-rejected";
    }
    Count(counts,status);if(reason.Length>0)Count(counts,reason);
    if(status=="remaining")
    {
        string representativeKey=reason+"/"+directReason;
        if(!representatives.TryGetValue(representativeKey,out var examples))representatives.Add(representativeKey,examples=new());
        if(examples.Count<12)examples.Add(new{line=lines,style,render,mesh,id=r.GetProperty("id").GetInt32(),originalDomain=r.GetProperty("originalDomain").GetInt32(),delta=r.GetProperty("delta").GetDouble()});
    }
    string scenario=JsonSerializer.Serialize(new{style,render,mesh,delta=r.GetProperty("delta"),scenario=r.GetProperty("scenario"),upper=r.GetProperty("upper"),lower=r.GetProperty("lower"),left=r.GetProperty("left"),right=r.GetProperty("right")});
    if(!scenarios.TryGetValue(scenario,out var bucket))scenarios.Add(scenario,bucket=new());Count(bucket,status);
    records.WriteLine(JsonSerializer.Serialize(new{line=lines,style,render,mesh,id=r.GetProperty("id"),originalDomain=r.GetProperty("originalDomain"),sourceFaces=r.GetProperty("sourceFaces"),delta=r.GetProperty("delta"),scenario=r.GetProperty("scenario"),upper=r.GetProperty("upper"),lower=r.GetProperty("lower"),left=r.GetProperty("left"),right=r.GetProperty("right"),sourceDigest=digest,status,reason,directReason,repeatOnlyReason,bindingKey=key}));
}
string Hash(string path){using var s=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(s));}
File.WriteAllText(args[4],JsonSerializer.Serialize(new{lines,counts,directReasons,repeatOnlyReasons,representatives,uses,currentCharts=current,directParityChanges,
    chartOverrides=overridePath,chartOverridesSha256=overridePath==null?null:Hash(overridePath),overriddenUnits,overridesUsed=overridesUsed.Count,unusedOverrides=overrides.Count-overridesUsed.Count,
    assembly=Path.GetFullPath(args[0]),assemblySha256=Hash(args[0]),input=Path.GetFullPath(args[2]),inputSha256=Hash(args[2]),sourceMapSha256=Hash(args[3]),
    scope="Exact restored emitted Units evaluated by production CanUseSource and RegionMapping; source-map provenance is supplied by the style owner. No claim of native pixel/mip, owner-footprint or visual acceptance.",
    scenarios=scenarios.Select(p=>new{input=JsonSerializer.Deserialize<JsonElement>(p.Key),counts=p.Value})},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(new{lines,counts,directParityChanges}));
