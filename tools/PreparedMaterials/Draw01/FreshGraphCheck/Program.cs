using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Runtime;

string style=Path.GetFullPath(args[0]);
var builtins=new HashSet<string>();
foreach(string family in new[]{"Game","BridgesAndPorts"})
foreach(string file in Directory.GetFiles(Path.Combine(args[1],"Content",family),"*.cok"))
{
    using var zip=ZipFile.OpenRead(file);
    foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".cid")))
    {
        if(zip.GetEntry(entry.FullName[..^4])==null)continue;
        using var reader=new StreamReader(entry.Open());builtins.Add(reader.ReadToEnd().Trim());
    }
}
var references=new HashSet<string>();var roots=new List<object>();
foreach(string file in Directory.GetFiles(style,"*.Surface",SearchOption.AllDirectories))
{
    string relative=Path.GetRelativePath(style,file);
    string[] References(byte[] bytes)=>BridgeSurfaceReferences.Read(bytes,(_,p)=>new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"}.Contains(p));
    var inputs=BridgePreparedMaterialFiles.Read(style,Path.GetDirectoryName(relative)!,Path.GetFileName(file),References,
        cid=>{if(!builtins.Contains(cid))throw new Exception("Missing built-in dependency: "+cid);});
    foreach(var input in inputs){references.Add(input.Path);references.Add(input.Path+".cid");}
    roots.Add(new{relativePath=relative.Replace('\\','/'),reason="Fresh descriptor produced directly from verified native source"});
}
var files=Directory.GetFiles(style,"*",SearchOption.AllDirectories);
if(files.Any(f=>!references.Contains(f)))throw new Exception("Unreachable fresh file.");
if(Directory.GetDirectories(style,"*",SearchOption.AllDirectories).Any(d=>!Directory.EnumerateFileSystemEntries(d).Any()))throw new Exception("Empty material directory.");
var textures=files.Where(f=>f.EndsWith(".Texture")).ToArray();
var payloads=files.Where(f=>!f.EndsWith(".cid")).ToArray();
if(payloads.Select(f=>File.ReadAllText(f+".cid").Trim()).Distinct().Count()!=payloads.Length)throw new Exception("Duplicate physical native payload.");
var report=new{status="PASS",style="Draw01",root=style,roots,files=files.Length,totalBytes=files.Sum(f=>new FileInfo(f).Length),
    surfaces=roots.Count,texturePayloads=textures.Length,productionDependencyReader=true,
    entries=files.Order().Select(f=>new{path=Path.GetRelativePath(style,f).Replace('\\','/'),bytes=new FileInfo(f).Length,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))).ToLowerInvariant()}),
    scope="Dependency closure of every candidate Surface; not native rendering or a mod package-size measurement."};
File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS fresh Draw01 graph: {roots.Count} surfaces, {textures.Length} texture payloads, {files.Length} files, {files.Sum(f=>new FileInfo(f).Length)} bytes.");
