using System.Text.Json;
using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
var root=Path.GetFullPath(args.Length==0?".":args[0]);
using var graph=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"artifacts/native-bridge-lengths/graph.json")));
int checks=0;
void Need(bool x,string message){if(!x)throw new Exception(message);checks++;}
foreach(var style in new[]{"Draw01","Draw02","Draw03"}){
 Func<string,int,string,bool> gate=style switch{"Draw01"=>Draw01NativeMaterials.IsNativeNetwork,"Draw02"=>Draw02NativeMaterials.IsNativeNetwork,_=>Draw03NativeMaterials.IsNativeNetwork};
 Func<SurfaceAsset,MovableExistingRoadSurface.SurfaceResult> resolve=style switch{"Draw01"=>Draw01NativeMaterials.Resolve,"Draw02"=>Draw02NativeMaterials.Resolve,_=>Draw03NativeMaterials.Resolve};
 var prefix="DrawBridge"+style[^2..];int entries=0;
 foreach(var node in graph.RootElement.EnumerateObject()){
  var v=node.Value;if(!v.TryGetProperty("name",out var name)||!v.TryGetProperty("surfaceCids",out var materials))continue;
  var render=name.GetString()!;if(!render.StartsWith(prefix+"Bottom") && !(style=="Draw03"&&render.StartsWith(prefix+"Side")))continue;
  int slot=0;foreach(var m in materials.EnumerateArray()){
   var cid=m.GetString()!;Need(gate(render,slot,cid),style+" rejected native graph "+render);
   Need(!gate(render,slot+1,cid),"wrong slot accepted");Need(!gate(render,slot,new string('0',32)),"foreign CID accepted");Need(!gate(render+" Copy",slot,cid),"unmeasured render accepted");
   var source=new SurfaceAsset();Need(ReferenceEquals(resolve(source).Value,source),"resolver changed original Surface");entries++;slot++;
  }
 }
 Need(entries==(style=="Draw01"?13:style=="Draw02"?19:15),"native graph coverage changed "+style);
 var unknown=new SurfaceAsset();Need(ReferenceEquals(resolve(unknown).Value,unknown),"arbitrary source replaced");
 Console.WriteLine($"PASS {style}: {entries} measured native render/slot/CID bindings, source references retained");
}
Console.WriteLine($"PASS {checks} native binding/source-retention assertions (managed fixture; not game rendering)");
namespace Colossal.IO.AssetDatabase {internal sealed class SurfaceAsset{}}
namespace BridgeBuilder.Bridges {internal static class MovableExistingRoadSurface{internal readonly struct SurfaceResult{internal SurfaceAsset Value{get;}public SurfaceResult(SurfaceAsset s)=>Value=s;public static implicit operator SurfaceResult(SurfaceAsset s)=>new(s);}}}
