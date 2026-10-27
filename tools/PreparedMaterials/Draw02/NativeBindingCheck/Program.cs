using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using System.Text.Json;

void Need(bool test,string error){if(!test)throw new Exception(error);}
void Throws<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
var fixtures=Path.GetFullPath(args[0]);var results=new List<object>();
foreach(var row in Draw02NativeSourceCatalog.Sources)
{
    byte[] data=File.ReadAllBytes(Path.Combine(fixtures,row.Key+".Surface"));
    var source=new SurfaceAsset(row.Key,data);var binding=new Draw02NativeSourceBindings();
    var original=new MovableStructurePacking.Unit{CanUseSource=true};
    Need(binding.TryBind(source,original,out var slot,out var requirement),"Valid original binding rejected");
    Need(ReferenceEquals(slot!.Surface,source)&&ReferenceEquals(slot.Original,original)&&requirement=="","Material/UV identity changed");
    Need(binding.TryBind(source,original,out _,out _)&&source.Reads==1,"Source descriptor reread unnecessarily");
    var unresolved=new MovableStructurePacking.Unit{CanUseSource=false,Triangle=true};
    Need(!binding.TryBind(source,unresolved,out var unsupported,out var reason)&&unsupported==null&&reason.Length>0,"Unsupported mapping not surfaced");
    var damaged=(byte[])data.Clone();damaged[^1]^=1;
    Throws<InvalidDataException>(()=>new Draw02NativeSourceBindings().TryBind(new SurfaceAsset(row.Key,damaged),original,out _,out _));
    Throws<InvalidOperationException>(()=>new Draw02NativeSourceBindings().TryBind(new SurfaceAsset(row.Key,data,false),original,out _,out _));
    results.Add(new{sourceCid=row.Key,sha256=row.Value,retainedSurfaceIdentity=true,retainedSamplingIdentity=true,readCount=source.Reads,unsupportedReported=true,rejectedChangedContent=true,rejectedNonBuiltin=true});
}
var unknown=new SurfaceAsset(new string('0',32),Array.Empty<byte>());
Need(!new Draw02NativeSourceBindings().TryBind(unknown,new MovableStructurePacking.Unit{CanUseSource=true},out _,out _)&&unknown.Reads==0,"Unknown source read");
var output=new{status="PASS",sourceBindings=results.Count,boundaryDoubleScope=true,nativeRenderingVerified=false,unknownSourceReadCount=unknown.Reads,results};
File.WriteAllText(args[1],JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS Draw02 native binding boundary checks: "+results.Count+" original descriptors; identity, ownership, digest and unsupported-mapping handling.");
