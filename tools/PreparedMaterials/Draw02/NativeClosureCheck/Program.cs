using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Runtime;
var folder=Path.GetFullPath(args[0]);var game=args[1];using var library=JsonDocument.Parse(File.ReadAllBytes(args[2]));
var templates=library.RootElement.EnumerateArray().ToDictionary(e=>e.GetProperty("hash").GetInt32());
var builtins=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach(var family in new[]{"Game","BridgesAndPorts"})foreach(var path in Directory.EnumerateFiles(Path.Combine(game,"Content",family),"*.cok"))
{
    if(!Path.GetFileName(path).StartsWith("Blob")&&!Path.GetFileName(path).StartsWith("VT"))continue;
    using var zip=ZipFile.OpenRead(path);
    foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal)))
    {
        using var reader=new StreamReader(entry.Open());var cid=reader.ReadToEnd().Trim();
        if(zip.GetEntry(entry.FullName[..^4])!=null)builtins.Add(cid);
    }
}
var payloads=new Dictionary<string,string>();var files=Directory.GetFiles(folder,"*",SearchOption.AllDirectories);
foreach(var sidecar in files.Where(p=>p.EndsWith(".cid",StringComparison.Ordinal)))
{
    string cid=File.ReadAllText(sidecar).Trim(),path=sidecar[..^4];if(!File.Exists(path))throw new Exception("Orphan CID sidecar.");
    if(!payloads.TryAdd(cid,path))throw new Exception("More than one payload for one CID.");
}
var roots=new List<object>();var dependencies=new Dictionary<string,string[]>();var reached=new HashSet<string>();
foreach(var pair in payloads.Where(p=>p.Value.EndsWith(".Surface",StringComparison.Ordinal)))
{
    var refs=BridgeSurfaceReferences.Read(File.ReadAllBytes(pair.Value),(template,property)=>templates[template].GetProperty("stacks").EnumerateArray().Any(s=>s.EnumerateArray().Any(p=>p.GetString()==property)));
    foreach(var cid in refs)if(!payloads.ContainsKey(cid)&&!builtins.Contains(cid))throw new Exception("Unresolved original/native dependency "+cid);
    dependencies[pair.Key]=refs;
    roots.Add(new {relativePath=Path.GetRelativePath(folder,pair.Value).Replace('\\','/'),reason=pair.Value.Contains(Path.DirectorySeparatorChar+"Lanes"+Path.DirectorySeparatorChar)?"Exact native curve-to-object descriptor conversion reachable from selected track sources":"Native object descriptor required by common bridge generation",sourceKey=pair.Value.Contains(Path.DirectorySeparatorChar+"Lanes"+Path.DirectorySeparatorChar)?Path.GetFileName(Path.GetDirectoryName(pair.Value)):null});
    reached.Add(pair.Key);foreach(var cid in refs)if(payloads.ContainsKey(cid))reached.Add(cid);
}
var unreachable=payloads.Keys.Where(cid=>!reached.Contains(cid)).ToArray();if(unreachable.Length!=0)throw new Exception("Orphan new payload.");
var manifest=new {style="Draw02",completeStyle=false,categories=payloads.Values.Where(p=>p.EndsWith(".Surface",StringComparison.Ordinal)).Select(p=>Path.GetRelativePath(folder,p).Split(Path.DirectorySeparatorChar)[0]).Distinct().OrderBy(x=>x),roots,
    files=files.OrderBy(p=>p).Select(p=>new{relativePath=Path.GetRelativePath(folder,p).Replace('\\','/'),bytes=new FileInfo(p).Length,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant()}),
    reachable=reached.OrderBy(cid=>cid),unreachable,dependencies,
    builtinReferences=dependencies.Values.SelectMany(x=>x).Where(builtins.Contains).Distinct().OrderBy(cid=>cid),
    fileCount=files.Length,bytes=files.Sum(p=>new FileInfo(p).Length),texturePayloads=payloads.Values.Count(p=>p.EndsWith(".Texture")),surfaces=roots.Count,
    rule="Native descriptor graph; each local CID has one payload. Built-in dependency boundary verified in installed archives. Pixel semantics are verified separately."};
File.WriteAllText(args[3],JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(new{manifest.fileCount,manifest.bytes,manifest.texturePayloads,manifest.surfaces,orphanPayloads=unreachable.Length}));
