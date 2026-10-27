using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;
var root=Path.GetFullPath(args[0]);
var directory=Path.Combine(root,"tools/PreparedMaterials/Draw01/NativeIntegration");
using var sources=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"Sources.json")));
using var sampling=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"Sampling.json")));
var sourceMap=sources.RootElement.GetProperty("sources").EnumerateArray().ToDictionary(s=>s.GetProperty("cid").GetString()!,s=>s.GetProperty("sha256").GetString()!);
foreach(var source in sourceMap.Values)Require(Draw01NativeMaterialBinding.IsRecordedSource(source),"Missing recorded source");
int records=0;
foreach(var sampler in sampling.RootElement.GetProperty("samplers").EnumerateArray()) {
 Require(sourceMap[sampler.GetProperty("sourceMaterial").GetString()!]==sampler.GetProperty("sourceDigest").GetString(),"Source binding mismatch");records++;
}
using var observations=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"artifacts/prepared-materials/Draw01/receiver-plans.json.receivers.json")));
int accepted=0,rejected=0,points=0;
foreach(var o in observations.RootElement.EnumerateArray()) {
 Vector2 V(string name) {var a=o.GetProperty(name);return new(a[0].GetSingle(),a[1].GetSingle());}
 var bounds=o.GetProperty("bounds");
 var unit=new MovableStructurePacking.Unit{Triangle=o.GetProperty("Triangle").GetBoolean(),RepeatOnly=o.GetProperty("RepeatOnly").GetBoolean(),A=V("a"),B=V("b"),C=V("c"),Bounds=new(bounds[0].GetSingle(),bounds[1].GetSingle(),bounds[2].GetSingle(),bounds[3].GetSingle())};
 foreach(var p in o.GetProperty("points").EnumerateArray())unit.Points.Add(new(p[0].GetSingle(),p[1].GetSingle()));
 bool compatible=o.GetProperty("canUseSource").GetBoolean();
 Require(compatible==unit.CanUseSource,"Current domain decision differs from recorded production evidence");
 try {
  Draw01NativeMaterialBinding.RequireCompatible(sourceMap.Values.First(),unit);
  Require(compatible,"Unsupported receiver accepted");accepted++;
  foreach(var p in unit.Points) {
   var actual=Draw01NativeMaterialBinding.Map(unit,p);var expected=unit.SourceUv(p);
   Require(BitConverter.SingleToInt32Bits(actual.x)==BitConverter.SingleToInt32Bits(expected.x)&&BitConverter.SingleToInt32Bits(actual.y)==BitConverter.SingleToInt32Bits(expected.y),"Native UV bits differ");points++;
  }
 } catch(InvalidOperationException) {Require(!compatible,"Compatible receiver rejected");rejected++;}
}
var report=new{status="PASS",sourceIdentities=sourceMap.Count,samplers=records,acceptedReceiverObservations=accepted,unsupportedReceiverObservations=rejected,bitExactMappedPoints=points,textureWrites=0,originalSurfaceIdentityRetainedByPatch=true,limitations="Recorded production receiver fixtures; no new game/native all-mip rendering or persistence acceptance. Patch awaits parent application."};
File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(report));
static void Require(bool v,string m){if(!v)throw new Exception(m);}
