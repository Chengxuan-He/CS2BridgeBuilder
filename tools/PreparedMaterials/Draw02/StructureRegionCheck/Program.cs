using System.Text.Json;
using System.Security.Cryptography;
using BridgeBuilder.Bridges;
using UnityEngine;


var evidence=Path.Combine(args[0],"artifacts/prepared-materials/Draw02");
using var members=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(evidence,"structure-units/membership.json")));
var digests=members.RootElement.GetProperty("sources").EnumerateArray().ToDictionary(p=>p.GetProperty("cid").GetString()!,p=>p.GetProperty("sha256").GetString()!);
var lookup=members.RootElement.GetProperty("domains").EnumerateArray().ToDictionary(p=>p.GetProperty("id").GetString()!,p=>digests[p.GetProperty("sourceCid").GetString()!]);
var plansPath=args.Length>1?Path.GetFullPath(args[1]):Path.Combine(evidence,"shared-source-migration/chart-plans.json.seams.json");
using var plans=JsonDocument.Parse(File.ReadAllBytes(plansPath));
int phaseRows=0,completeUnits=0;
int original=0,repeat=0,remaining=0;var checks=new List<object>();var unresolved=new List<object>();
foreach(var row in plans.RootElement.EnumerateArray())
{
 string render=row.GetProperty("render").GetString()!;int mesh=row.GetProperty("mesh").GetInt32(),domain=row.GetProperty("domain").GetInt32();
 Vector2 V(string key){var v=row.GetProperty(key);return new(v[0].GetSingle(),v[1].GetSingle());}
 var bd=row.GetProperty("bounds");
 var u=new MovableStructurePacking.Unit{Triangle=row.GetProperty("triangle").GetBoolean(),A=V("a"),B=V("b"),C=V("c"),Bounds=new(bd[0].GetSingle(),bd[1].GetSingle(),bd[2].GetSingle(),bd[3].GetSingle()),Charts=Draw02UvCharts.For(render,mesh,domain)};
 foreach(var p in row.GetProperty("q").EnumerateArray())u.Points.Add(new(p[0].GetSingle(),p[1].GetSingle()));
 if(row.TryGetProperty("phaseSchema",out var schema))
 {
  if(schema.GetInt32()!=1)throw new Exception("Unsupported parent phase schema");
  phaseRows++;if(row.TryGetProperty("completeUnit",out var full) && full.GetBoolean())completeUnits++;u.RepeatOnly=row.GetProperty("repeatOnly").GetBoolean();
  double[][]? Exact(string key)=>row.GetProperty(key).ValueKind==JsonValueKind.Null?null:row.GetProperty(key).EnumerateArray().Select(p=>new[]{p[0].GetDouble(),p[1].GetDouble()}).ToArray();
  u.ExactPoints=Exact("exactPoints");
  var coords=Exact("exactCoordinates");var uv=Exact("exactSourceUvs");
  if(coords!=null)u.ExactCoordinates=new();if(uv!=null)u.ExactSourceUvs=new();
  if(new[]{u.ExactPoints,coords,uv}.Any(a=>a!=null && a.Length!=u.Points.Count))throw new Exception("Parent phase point count differs");
  for(int i=0;i<u.Points.Count;i++)
  {
   if(coords!=null)
   {
    if(u.ExactCoordinates!.TryGetValue(u.Points[i],out var previous) && !previous.SequenceEqual(coords[i]))throw new Exception("Conflicting exact coordinates");
    u.ExactCoordinates[u.Points[i]]=coords[i];
   }
   if(uv!=null)
   {
    var value=new Vector2((float)uv[i][0],(float)uv[i][1]);
    if(u.ExactSourceUvs!.TryGetValue(u.Points[i],out var previous) && !previous.Equals(value))throw new Exception("Conflicting exact source UVs");
    u.ExactSourceUvs[u.Points[i]]=value;
   }
  }
 }
 if(u.CanUseSource){original++;continue;}
 var regions=MovableRepeatUnits.Regions("Draw02",lookup[$"{render}|{mesh}|{domain}"]);
 var mapping=regions.Select(r=>MovableRepeatUnits.RegionMapping.Create(r,u)).FirstOrDefault(m=>m!=null);
 if(mapping==null){remaining++;unresolved.Add(new{render,mesh,domain,sourceDigest=lookup[$"{render}|{mesh}|{domain}"],repeatOnly=u.RepeatOnly,pointCount=u.Points.Count});continue;}
 foreach(var p in u.Points){var mapped=mapping.Map(p);if(!float.IsFinite(mapped.x)||!float.IsFinite(mapped.y))throw new Exception("Non-finite mapped UV");}
 repeat++;checks.Add(new{render,mesh,domain,bindingKey=mapping.Region.BindingKey});
 string descriptor=Path.Combine(args[0],"assets/Materials/Draw02/RepeatUnits",mapping.Region.BindingKey,"Repeat.Surface");
 using var reader=new BinaryReader(File.OpenRead(descriptor));if(reader.ReadUInt16()!=1||reader.ReadInt32()!=1)throw new Exception("Original structural template1 not preserved");
}
if(MovableRepeatUnits.Regions("Draw02","unknown").Count!=0)throw new Exception("Source escape");
var result=new{receiverCount=original+repeat+remaining,original,repeat,remaining,checks,unresolved,phaseRows,completeUnits,inputSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(plansPath))).ToLowerInvariant(),scope=completeUnits==original+repeat+remaining && completeUnits>0?"Current parent phase metadata replay; complete generated-owner validation remains pending":"Legacy or mixed Float32 receiver plans; not current production phase acceptance",completeGeneration=false};
File.WriteAllText(args.Length>2?args[2]:Path.Combine(evidence,"structure-units/production-region-check.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(new{original,repeat,remaining}));

