using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Runtime;
var materials=Path.GetFullPath(args[0]);var archives=new List<ZipArchive>();
var builtin=new Dictionary<string,(string Archive,ZipArchiveEntry Entry)>();
var local=new Dictionary<string,string>();
using var library=JsonDocument.Parse(File.ReadAllText(args[3]));
var stacks=library.RootElement.EnumerateArray().ToDictionary(x=>x.GetProperty("hash").GetInt32(),x=>x.GetProperty("stacks").EnumerateArray().SelectMany(s=>s.EnumerateArray().Select(p=>p.GetString()!)).ToHashSet());
using var manifest=JsonDocument.Parse(File.ReadAllText(args[1]));
try{
 foreach(var family in new[]{"Game","BridgesAndPorts"})foreach(var path in Directory.GetFiles(Path.Combine(args[2],"Content",family),"*.cok").Where(p=>Path.GetFileName(p).StartsWith("Blob")||Path.GetFileName(p).StartsWith("VT"))){
 var z=ZipFile.OpenRead(path);archives.Add(z);foreach(var e in z.Entries.Where(e=>e.FullName.EndsWith(".cid"))){using var r=new StreamReader(e.Open());var cid=r.ReadToEnd().Trim();if(z.GetEntry(e.FullName[..^4]) is {} payload)builtin.TryAdd(cid,(path,payload));}}
 foreach(var p in Directory.GetFiles(materials,"*.cid",SearchOption.AllDirectories)){var cid=File.ReadAllText(p).Trim();if(!local.TryAdd(cid,p[..^4]))throw new Exception("Duplicate physical CID payload: "+cid);}
 var hashes=new Dictionary<string,object>();var descriptors=new List<object>();
 byte[] Read(string cid){if(local.TryGetValue(cid,out var p))return File.ReadAllBytes(p);if(!builtin.TryGetValue(cid,out var entry))throw new Exception("Unresolved dependency: "+cid);using var s=entry.Entry.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
 string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
 void Visit(string cid){if(hashes.ContainsKey(cid))return;var bytes=Read(cid);bool owned=local.ContainsKey(cid);hashes.Add(cid,new{cid,scope=owned?"owner-local":"builtin",bytes=bytes.Length,sha256=Hash(bytes),path=owned?Path.GetRelativePath(materials,local[cid]):builtin[cid].Entry.FullName,archive=owned?null:builtin[cid].Archive});}
 foreach(var source in manifest.RootElement.GetProperty("sources").EnumerateArray()){
 var cid=source.GetProperty("cid").GetString()!;var bytes=Read(cid);if(Hash(bytes)!=source.GetProperty("sha256").GetString())throw new Exception("Native source changed.");Inspect(cid,bytes,"builtin-source");}
 foreach(var pair in local.Where(p=>p.Value.EndsWith(".Surface")))Inspect(pair.Key,File.ReadAllBytes(pair.Value),"prepared-descriptor");
 void Inspect(string cid,byte[] bytes,string scope){Visit(cid);var deps=BridgeSurfaceReferences.Read(bytes,(t,p)=>stacks.TryGetValue(t,out var s)&&s.Contains(p));foreach(var dep in deps)Visit(dep);descriptors.Add(new{cid,scope,template=BitConverter.ToInt32(bytes,2),nativeVT=bytes[6]!=0,references=deps});}
 var files=Directory.GetFiles(materials,"*",SearchOption.AllDirectories).Select(p=>new{path=Path.GetRelativePath(materials,p),bytes=new FileInfo(p).Length,sha256=Hash(File.ReadAllBytes(p))}).ToArray();
 var result=new{status="PASS",style="Lift03",fileCount=files.Length,bytes=files.Sum(f=>f.bytes),localPayloads=local.Count,builtinReferences=hashes.Keys.Count(k=>!local.ContainsKey(k)),descriptors,nativeGraph=hashes.Values,files,mipBehavior="Original builtin VT payload identities retained; no bytes reencoded. Binary reader validation is separate; game rendering and cold-load acceptance pending."};
 File.WriteAllText(args[4],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS Lift03 source graph: {local.Count} owner-local payloads, {hashes.Count-local.Count} builtin references, {files.Length} files, {files.Sum(f=>f.bytes)} bytes");
}finally{foreach(var z in archives)z.Dispose();}
