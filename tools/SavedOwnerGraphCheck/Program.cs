using System.IO.Compression;
using BridgeBuilder.Runtime;
if(args.Length<4)throw new ArgumentException("SavedOwnerGraphCheck <backed-up owner> <Materials> <Cities2_Data> <isolated output> [permitted non-owner source directory...]");
var builtins=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach(var path in Directory.GetFiles(Path.Combine(args[2],"Content"),"*.cok",SearchOption.AllDirectories)){
 using var zip=ZipFile.OpenRead(path);foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var r=new StreamReader(entry.Open());builtins.Add(r.ReadToEnd().Trim());}}
foreach(var cidFile in Directory.GetFiles(Path.Combine(args[2],"Content"),"*.cid",SearchOption.AllDirectories))builtins.Add(File.ReadAllText(cidFile).Trim());
var graph=new Dictionary<string,BridgeDependencyCopies.Source>(StringComparer.OrdinalIgnoreCase);var caches=new Dictionary<string,string[]>();var seeds=new List<string>();
foreach(var item in new[]{(Root:args[1],Owned:false),(Root:args[0],Owned:true)}.Concat(args.Skip(4).Select(p=>(Root:p,Owned:false))))foreach(var sidecar in Directory.GetFiles(item.Root,"*.cid",SearchOption.AllDirectories)){
 string file=sidecar[..^4],cid=File.ReadAllText(sidecar).Trim(),ext=Path.GetExtension(file);var bytes=File.ReadAllBytes(file);
 if(graph.TryGetValue(cid,out var old)&&!old.Bytes.SequenceEqual(bytes))throw new Exception("CID conflict "+cid);
 var refs=ext==".Surface"?(BridgeSurfaceReferences.TryRead(bytes,(_,p)=>new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"}.Contains(p),out var surfaceRefs1,out var surfaceRefsError1)?surfaceRefs1:throw new System.IO.InvalidDataException(surfaceRefsError1)):null;
 graph[cid]=new(){Owned=item.Owned,Extension=ext,PhysicalPath=file,NativeFileName=Path.GetFileName(file),Bytes=bytes,SerializedReferences=refs};
 if(ext==".MidMips")caches[cid]=(BridgeMidMipReferences.TryRead(bytes,out var midResult1,out var midError1)?midResult1:throw new System.IO.InvalidDataException(midError1));if(item.Owned&&ext==".Prefab")seeds.Add(cid);
}
foreach(var item in graph.Where(p=>p.Value.Extension==".Surface"))item.Value.SerializedReferences=(item.Value.SerializedReferences??Array.Empty<string>()).Concat(caches.Where(c=>c.Value.Contains(item.Key)).Select(c=>c.Key)).ToArray();
var visited=new HashSet<string>();string owner="b"+Guid.NewGuid();
BridgeDependencyCopies.Source? Resolve(string cid){visited.Add(cid);return graph.GetValueOrDefault(cid)??(builtins.Contains(cid)?new(){Builtin=true}:null);}
if(!BridgeDependencyCopies.Save(args[3],owner,seeds,Resolve,out int count,out string error,out int written))throw new Exception(error);
foreach(var cid in visited){if(graph.TryGetValue(cid,out var source)&&!source.Owned&&!source.Builtin){var dest=(BridgeDependencyCopies.TryDestination(args[3],owner,cid,source,out var destination1,out var destinationError1)?destination1:throw new System.IO.InvalidDataException(destinationError1));if(!File.ReadAllBytes(dest).SequenceEqual(source.Bytes))throw new Exception("Copy mismatch "+cid);}}
Console.WriteLine($"PASS actual backed-up owner graph: {seeds.Count} prefab roots; {visited.Count} visited identities; {count} external copies; {written} files; exact bytes/CIDs, shared native MidMips; no game asset registration or original-owner writes. Private snapshot owner {owner}.");
namespace BridgeBuilder.Runtime {internal static class BridgeAssetInfo {internal static bool IsPrefabName(string n)=>System.Text.RegularExpressions.Regex.IsMatch(n,@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z");}}
