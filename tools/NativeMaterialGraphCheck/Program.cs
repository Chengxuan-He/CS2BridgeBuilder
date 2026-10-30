using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Runtime;

if(args.Length!=4)throw new ArgumentException("NativeMaterialGraphCheck <Materials> <Cities2_Data> <report.json> <isolated-work-root>");
string root=Path.GetFullPath(args[0]);var builtins=new HashSet<string>();
var nativeLevels=new Dictionary<string,int>();
foreach(var family in new[]{"Game","BridgesAndPorts"})
foreach(var path in Directory.GetFiles(Path.Combine(args[1],"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT")))
{
    using var zip=ZipFile.OpenRead(path);
    foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal)))
    {
        using var reader=new StreamReader(entry.Open());var cid=reader.ReadToEnd().Trim();
        var payload=zip.GetEntry(entry.FullName[..^4]);
        if(payload!=null)
        {
            builtins.Add(cid);
            if(payload.FullName.EndsWith(".VTTexture")||payload.FullName.EndsWith(".VTSurface"))
            {
                using var stream=payload.Open();using var header=new BinaryReader(stream);
                using var bytes=new MemoryStream(header.ReadBytes(498));
                nativeLevels[cid]=BridgeMidMipReferences.RequiredLevels(bytes,payload.FullName.EndsWith(".VTSurface"));
            }
        }
    }
}
var identities=new Dictionary<string,string>();var rows=new List<object>();
string test=Path.Combine(Path.GetFullPath(args[3]),"BridgeBuilder-NativeGraph-"+Guid.NewGuid().ToString("N"));
var common=new Dictionary<string,(BridgeDependencyCopies.Source Source,string Path,string[] Members)>();
if(Directory.GetFiles(Path.Combine(root,"Shared"),"*",SearchOption.AllDirectories).Any(p=>!p.EndsWith(".cid")&&!p.EndsWith(".MidMips")))
    throw new InvalidDataException("The common material root is reserved for native MidMips.");
foreach(var file in Directory.GetFiles(Path.Combine(root,"Shared","MidMips"),"*.MidMips",SearchOption.AllDirectories))
{
    var bytes=File.ReadAllBytes(file);string cid=File.ReadAllText(file+".cid").Trim();
    if(!common.TryAdd(cid,(new(){Extension=".MidMips",NativeFileName=Path.GetFileName(file),Bytes=bytes},file,BridgeMidMipReferences.Read(bytes))))
        throw new InvalidDataException("Duplicated physical common MidMips payload: "+cid);
    identities[cid]=Convert.ToHexString(SHA256.HashData(bytes));
}
var usedCommon=new HashSet<string>();
try
{
    foreach(var folder in Directory.GetDirectories(root))
    {
        if(Path.GetFileName(folder)=="Shared")continue;
        string style=Path.GetFileName(folder);var graph=new Dictionary<string,BridgeDependencyCopies.Source>();
        var locations=new Dictionary<string,string>();
        foreach(var file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(f=>!f.EndsWith(".cid")))
        {
            string cid=File.ReadAllText(file+".cid").Trim();var bytes=File.ReadAllBytes(file);
            if(cid.Length!=32||cid.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("CID: "+file);
            string digest=Convert.ToHexString(SHA256.HashData(bytes));
            if(identities.TryGetValue(cid,out var previous)&&previous!=digest)throw new InvalidDataException("Conflicting CID bytes: "+cid);
            identities[cid]=digest;
            var references=file.EndsWith(".Surface")?BridgeSurfaceReferences.Read(bytes,(_,p)=>new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"}.Contains(p)):null;
            if(!graph.TryAdd(cid,new(){Extension=Path.GetExtension(file),Bytes=bytes,SerializedReferences=references}))throw new InvalidDataException("Duplicated physical payload within style: "+cid);
            locations.Add(cid,file);
        }
        var seeds=graph.Where(p=>p.Value.Extension==".Surface").Select(p=>p.Key).ToArray();
        foreach(var seed in seeds)
        {
            var caches=common.Where(p=>p.Value.Members.Contains(seed)).ToArray();
            int levels=(graph[seed].SerializedReferences??Array.Empty<string>()).Select(cid=>nativeLevels.GetValueOrDefault(cid)).DefaultIfEmpty().Max();
            if(levels>0&&caches.Length==0)throw new InvalidDataException("Missing native Surface-to-MidMips edge: "+style+"/"+seed);
            graph[seed].SerializedReferences=(graph[seed].SerializedReferences??Array.Empty<string>()).Concat(caches.Select(p=>p.Key)).ToArray();
            foreach(var cache in caches)
            {
                graph.TryAdd(cache.Key,cache.Value.Source);locations.TryAdd(cache.Key,cache.Value.Path);usedCommon.Add(cache.Key);
            }
        }
        var reached=new HashSet<string>(seeds);foreach(var p in graph.Values)foreach(var cid in p.SerializedReferences??Array.Empty<string>())
        {
            if(!graph.ContainsKey(cid)&&!builtins.Contains(cid))throw new InvalidDataException("Missing authoritative dependency: "+style+"/"+cid);
            if(graph.ContainsKey(cid))reached.Add(cid);
        }
        if(reached.Count!=graph.Count)throw new InvalidDataException("Unreferenced local payload: "+style);
        var first="b"+Guid.NewGuid();var second="b"+Guid.NewGuid();
        foreach(var owner in new[]{first,second})
        {
            if(!BridgeDependencyCopies.Save(test,owner,seeds,cid=>graph.GetValueOrDefault(cid)??(builtins.Contains(cid)?new(){Builtin=true}:null),out int count,out string error,out _))throw new InvalidDataException(error);
            if(count!=graph.Count)throw new InvalidDataException("Incomplete owner snapshot.");
            foreach(var p in graph)if(!File.ReadAllBytes(BridgeDependencyCopies.Destination(test,owner,p.Key,p.Value)).SequenceEqual(p.Value.Bytes))throw new InvalidDataException("Snapshot bytes changed.");
        }
        Directory.Delete(BridgeDependencyCopies.Folder(test,first),true);
        foreach(var p in graph)if(!File.ReadAllBytes(BridgeDependencyCopies.Destination(test,second,p.Key,p.Value)).SequenceEqual(p.Value.Bytes)||!File.ReadAllBytes(locations[p.Key]).SequenceEqual(p.Value.Bytes))throw new InvalidDataException("Deletion affected another owner/source.");
        rows.Add(new{style,descriptors=seeds.Length,payloads=graph.Count,bytes=graph.Values.Sum(p=>(long)p.Bytes.Length+32),missingDependencies=0,orphanPayloads=0,independentSnapshots=true});
    }
    if(usedCommon.Count!=common.Count)throw new InvalidDataException("Unreferenced common MidMips payload.");
    var report=new{schema="bridgebuilder.native-material-graph/2",completeStyle=false,styles=rows,distinctCids=identities.Count,commonMidMipPayloads=common.Count,
        scope="Native catalogue dependency closure and byte-identical independent snapshots including implicit mid-mip edges. All descriptor roots retained together as a test catalogue, not a generated-owner size or rendering acceptance."};
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine($"PASS native material graphs: {rows.Count} styles; {identities.Count} identities; source-linked independent snapshots; no missing dependencies or orphan textures.");
}
finally
{
    var temp=Path.GetFullPath(args[3]).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
    if(!Path.GetFullPath(test).StartsWith(temp,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(test).StartsWith("BridgeBuilder-NativeGraph-",StringComparison.Ordinal))throw new IOException("Unsafe test cleanup.");
    if(Directory.Exists(test))Directory.Delete(test,true);
}
namespace BridgeBuilder.Runtime
{
    // The source-linked copy engine only needs this owned-name predicate here.
    internal static class BridgeAssetInfo
    {
        internal static bool IsPrefabName(string name)=>System.Text.RegularExpressions.Regex.IsMatch(name,@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z");
    }
}
