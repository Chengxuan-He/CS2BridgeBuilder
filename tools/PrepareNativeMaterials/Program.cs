using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BridgeBuilder.Runtime;

if(args.Length<5)throw new ArgumentException("PrepareNativeMaterials <style> <manifest> <output root> <report> <GameData> [--defaults directory] [--common style directory]");
var style=args[0];if(!System.Text.RegularExpressions.Regex.IsMatch(style,"^(Draw0[1-3]|Lift0[1-5]|PedestrianDraw0[12])$"))throw new ArgumentException("Unknown style.");
string? Option(string name){int i=Array.IndexOf(args,name);return i<0?null:args[i+1];}
using var input=JsonDocument.Parse(File.ReadAllBytes(args[1]));
if(input.RootElement.GetProperty("style").GetString()!=style)throw new InvalidDataException("Manifest/style mismatch.");
var output=Path.Combine(Path.GetFullPath(args[2]),style);var files=new Dictionary<string,byte[]>();var roots=new List<object>();
var archives=new List<ZipArchive>();var index=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
using var templates=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"NativeTemplates.json")));
var library=templates.RootElement.EnumerateArray().ToDictionary(x=>x.GetProperty("hash").GetInt32());
try {
 foreach(var family in new[]{"Game","BridgesAndPorts"})foreach(var path in Directory.GetFiles(Path.Combine(args[4],"Content",family),"*.cok").Where(x=>Path.GetFileName(x).StartsWith("Blob")||Path.GetFileName(x).StartsWith("VT"))){
  var zip=ZipFile.OpenRead(path);archives.Add(zip);
  foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".cid",StringComparison.Ordinal))){using var r=new StreamReader(entry.Open());var cid=r.ReadToEnd().Trim();var payload=zip.GetEntry(entry.FullName[..^4]);if(payload!=null)index.TryAdd(cid,payload);}
 }
 byte[] Read(string cid){if(!index.TryGetValue(cid,out var entry))throw new InvalidDataException("Missing builtin payload: "+cid);using var s=entry.Open();using var b=new MemoryStream();s.CopyTo(b);return b.ToArray();}
 string[] References(byte[] bytes)=>BridgeSurfaceReferences.Read(bytes,(template,p)=>Stack(template).Contains(p));
 IEnumerable<string> Stack(int template)=>library.TryGetValue(template,out var value)?value.GetProperty("stacks").EnumerateArray().SelectMany(s=>s.EnumerateArray().Select(p=>p.GetString()!)):template==17?new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"}:Array.Empty<string>();
 void Add(string path,byte[] bytes){if(files.TryGetValue(path,out var prior)&&!prior.SequenceEqual(bytes))throw new InvalidDataException("Conflicting output: "+path);files[path]=bytes;}
 string Surface(string path,byte[] bytes){var cid=Digest(bytes)[..32];Add(path,bytes);Add(path+".cid",Encoding.UTF8.GetBytes(cid));return cid;}
 foreach(var item in input.RootElement.GetProperty("sources").EnumerateArray()){
  string cid=item.GetProperty("cid").GetString()!;var bytes=Read(cid);if(!Digest(bytes).Equals(item.GetProperty("sha256").GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Source hash changed: "+cid);
  int template=BitConverter.ToInt32(bytes,2);int target=template switch{16 or 17=>0,31=>1,34=>35,33=>0,_=>template};
  if(template!=item.GetProperty("template").GetInt32())throw new InvalidDataException("Source template changed: "+cid);
  string? path=null;string resultCid=cid;var operation="builtin-reference";
  if(target!=template){
   // Conversion retains the full native VT header and all payload identities.
   // Network defaults must be explicit, not silently replaced by object defaults.
   var source=Descriptor.Read(bytes);var defaults=Defaults(template);var targetDefaults=Defaults(target);
   foreach(var pair in Floats(defaults))if(Floats(targetDefaults).ContainsKey(pair.Key)&&!source.Floats.ContainsKey(pair.Key))source.Floats.Add(pair.Key,pair.Value);
   if(template==17||template==33){
    if(source.Textures.Keys.Any(p=>p.StartsWith("_Worldspace",StringComparison.Ordinal)))throw new InvalidDataException("Network worldspace conversion needs an explicit source-level recipe: "+cid);
    if(source.VtHeader.Length!=0&&!Stack(template).SequenceEqual(Stack(target)))throw new InvalidDataException("Network VT stack requires explicit layer mapping: "+cid);
   }else if(!Stack(template).SequenceEqual(Stack(target)))throw new InvalidDataException("Incompatible VT layer layouts.");
   source.Template=target;source.Keywords.Add("_TANGENTSPACE_OCTO");var converted=source.Write();
   if(!References(bytes).Order().SequenceEqual(References(converted).Order()))throw new InvalidDataException("Conversion changed native dependencies: "+cid);
   resultCid=Digest(converted)[..32];path="NativeSources/"+resultCid+"/Object.Surface";Surface(path,converted);operation="native-descriptor-conversion";
   bytes=converted;
  }
  var dependencies=References(bytes);foreach(var dep in dependencies)if(!index.ContainsKey(dep))throw new InvalidDataException("Missing complete builtin source: "+dep);
  roots.Add(new{sourceCid=cid,sourceTemplate=template,targetTemplate=target,operation,cid=resultCid,path,builtinDependencies=dependencies});
 }
 // Paint uses the original native VTTexture, including its authored mip pyramid.
 // Never bind its deleted original authoring Texture CID to a non-VT descriptor.
 const string paintCid="4b5500b1b5c270890d99149f7f62e51a";
 var paintBytes=Read(paintCid);if(Digest(paintBytes)!="6bb8e926e34c3b39f8ad0cff6d8a893b5c8e305c0ace1370438d2614f799895f")throw new InvalidDataException("Paint source changed.");
 var paintSource=Descriptor.Read(paintBytes);var vt=paintSource.VtHeader;
 if(vt.Length<277)throw new InvalidDataException("Paint VT header is absent.");
 string paintBacking=Hash(vt,76);if(paintBacking==new string('0',32)||!index.ContainsKey(paintBacking))throw new InvalidDataException("Paint needs complete combined-VT layer extraction; no standalone source VTTexture.");
 var header=new byte[277];BitConverter.GetBytes(1).CopyTo(header,0);Array.Copy(vt,4,header,4,8);Array.Copy(vt,12,header,12,16);Array.Copy(vt,76,header,76,16);
 var paint=new Descriptor{Template=0,VtHeader=header};
 foreach(var pair in new Dictionary<string,float>{{"_SupportDecals",0},{"_Snow",0},{"_Metallic",0},{"_Smoothness",.35f},{"_DoubleSidedEnable",0},{"_CullMode",2},{"_CullModeForward",2}})paint.Floats.Add(pair.Key,pair.Value);
 paint.Keywords.UnionWith(new[]{"_TANGENTSPACE_OCTO","_DISABLE_DECALS"});
 // Source original authoring IDs stay in the native header only. Active VT CID is the dependency.
 var paintOutput=paint.Write();var paintRefs=References(paintOutput);if(!paintRefs.SequenceEqual(new[]{paintBacking}))throw new InvalidDataException("Paint backing reference mismatch.");
 var preparedPaint=Surface("Paint/SkinnedPaint.Surface",paintOutput);roots.Add(new{sourceCid=paintCid,operation="native-vt-paint-descriptor",cid=preparedPaint,path="Paint/SkinnedPaint.Surface",builtinDependencies=paintRefs});
 if(Option("--common") is string common){
  foreach(var category in new[]{"Cable","Road","Lanes","Shared"}){
   var folder=Path.Combine(common,category);if(!Directory.Exists(folder))continue;
   foreach(var file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))Add(Path.GetRelativePath(common,file).Replace('\\','/'),File.ReadAllBytes(file));
  }
 }
 // Resolve every outgoing descriptor dependency before touching the output.
 var local=files.Where(x=>x.Key.EndsWith(".cid")).ToDictionary(x=>Encoding.UTF8.GetString(x.Value).Trim(),x=>x.Key[..^4]);
 foreach(var item in files.Where(x=>x.Key.EndsWith(".Surface")))foreach(var dep in References(item.Value))if(!local.ContainsKey(dep)&&!index.ContainsKey(dep))throw new InvalidDataException("Unresolved output dependency: "+dep);
 foreach(var pair in files){var path=Path.GetFullPath(Path.Combine(output,pair.Key));if(!path.StartsWith(output+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Escaped output.");if(File.Exists(path)&&!File.ReadAllBytes(path).SequenceEqual(pair.Value))throw new IOException("Output already contains different payload: "+path);}
 foreach(var pair in files){var path=Path.Combine(output,pair.Key);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,pair.Value);}
 var report=new{schema="bridgebuilder.native-material-output/1",style,completeStyle=false,texturePayloadsCreated=0,nativeMipBytesReencoded=0,roots,
  files=files.OrderBy(x=>x.Key).Select(x=>new{path=x.Key,bytes=x.Value.Length,sha256=Digest(x.Value)}),fileCount=files.Count,bytes=files.Values.Sum(x=>(long)x.Length),
  remaining=new[]{"Native reader and renderer validation of the new paint VT descriptor","Cross-chart structural repeat mapping and production integration"}};
 Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);File.WriteAllText(args[3],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
 Console.WriteLine($"PASS source-level preparation: {style}; {files.Count} files; {files.Values.Sum(x=>(long)x.Length)} bytes; no texture re-encoding. Full style acceptance pending.");
 JsonElement Defaults(int template){if(library.TryGetValue(template,out var value))return value.GetProperty("material");var dir=Option("--defaults")??throw new InvalidDataException("Template defaults required: "+template);using var doc=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir,"template-"+template+".json")));return doc.RootElement.Clone();}
}finally{foreach(var zip in archives)zip.Dispose();}
static Dictionary<string,float> Floats(JsonElement material)=>material.GetProperty("m_SavedProperties").GetProperty("m_Floats").EnumerateArray().ToDictionary(p=>p[0].GetString()!,p=>p[1].GetSingle());
static string Digest(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static string Hash(byte[] bytes,int offset)=>string.Concat(bytes.Skip(offset).Take(16).Select(b=>(b&15).ToString("x")+(b>>4).ToString("x")));
sealed class Descriptor {
 public int Template;public byte[] VtHeader=Array.Empty<byte>();public Dictionary<string,float> Floats=new();public byte[] Middle=Array.Empty<byte>();public Dictionary<string,byte[]?> Textures=new();public HashSet<string> Keywords=new();
 public static Descriptor Read(byte[] bytes){using var r=new BinaryReader(new MemoryStream(bytes));if(r.ReadUInt16()!=1)throw new InvalidDataException("Surface version.");var d=new Descriptor{Template=r.ReadInt32()};if(r.ReadBoolean()){int start=(int)r.BaseStream.Position;r.BaseStream.Position+=276;if(Present())r.BaseStream.Position+=16;d.VtHeader=bytes[start..(int)r.BaseStream.Position];}
  for(int n=Count();n>0;n--)d.Floats.Add(r.ReadString(),r.ReadSingle());int middle=(int)r.BaseStream.Position;foreach(int size in new[]{4,16,16})for(int n=Count();n>0;n--){r.ReadString();r.BaseStream.Position+=size;}d.Middle=bytes[middle..(int)r.BaseStream.Position];
  for(int n=Count();n>0;n--)d.Textures.Add(r.ReadString(),Present()?r.ReadBytes(16):null);for(int n=Count();n>0;n--)d.Keywords.Add(r.ReadString());if(r.BaseStream.Position!=bytes.Length)throw new InvalidDataException("Trailing descriptor bytes.");return d;
  bool Present()=>r.ReadByte()switch{0=>false,255=>true,_=>throw new InvalidDataException("Native marker.")};int Count()=>Present()?r.ReadInt32():0;
 }
 public byte[] Write(){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)1);w.Write(Template);w.Write(VtHeader.Length!=0);w.Write(VtHeader);w.Write((byte)255);w.Write(Floats.Count);foreach(var p in Floats){w.Write(p.Key);w.Write(p.Value);}if(Middle.Length!=0)w.Write(Middle);else for(int i=0;i<3;i++){w.Write((byte)255);w.Write(0);}w.Write((byte)255);w.Write(Textures.Count);foreach(var p in Textures){w.Write(p.Key);w.Write((byte)(p.Value==null?0:255));if(p.Value!=null)w.Write(p.Value);}w.Write((byte)255);w.Write(Keywords.Count);foreach(var k in Keywords)w.Write(k);return s.ToArray();}
}
