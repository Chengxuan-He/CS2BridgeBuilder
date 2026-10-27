using BridgeBuilder.Runtime;
using System.Security.Cryptography;
var root=Path.GetFullPath(args[0]);
var styles=new[]{"Draw01","Draw02","Draw03","Lift01","Lift02","Lift03","Lift04","Lift05","PedestrianDraw01","PedestrianDraw02"};
var digest=new Dictionary<string,string>();
int files=0;
foreach(var style in styles) {
 foreach(var variant in new[]{"Paint","Cable","Road"}) {
 var folder=Path.Combine(root,style,variant);
 var graph=new Dictionary<string,BridgeDependencyCopies.Source>();
 foreach(var file in Directory.GetFiles(folder).Where(f=>!f.EndsWith(".cid"))) {
  var bytes=File.ReadAllBytes(file);var cid=File.ReadAllText(file+".cid").Trim();
  Require(System.Text.RegularExpressions.Regex.IsMatch(cid,"^[a-f0-9]{32}$"),"native CID");
  var hash=Convert.ToHexString(SHA256.HashData(bytes));
  if(digest.TryGetValue(cid,out var previous))Require(previous==hash,"same CID has identical bytes across styles");
  digest[cid]=hash;
  var surface=Path.GetExtension(file)==".Surface";
  if(!surface)Texture(file,bytes);
  graph.Add(cid,new(){Extension=Path.GetExtension(file),Bytes=bytes,
   SerializedReferences=surface?BridgeSurfaceReferences.Read(bytes,(_,_)=>throw new Exception("Paint unexpectedly uses VT")):null});
  files++;
 }
 var surfaceName=variant=="Paint"?"SkinnedPaint.Surface":variant=="Road"?"NativeAsphalt.Surface":"SkinnedCable.Surface";
 var seed=File.ReadAllText(Path.Combine(folder,surfaceName+".cid")).Trim();
 var builtins=variant=="Road"?new[]{"b00176216b0ce36579bce6fc675cfb77","9f446790360787756b0e5909db12e200","375b871b66aaf675dacef141eb2a9810"}:Array.Empty<string>();
 Require(graph[seed].SerializedReferences!.Length==(variant=="Paint"?2:variant=="Road"?4:1),"complete prepared texture dependencies");
 Require(graph[seed].SerializedReferences!.All(id=>graph.ContainsKey(id)||builtins.Contains(id)),"each style has complete non-builtin dependencies");
 var test=Path.Combine(Path.GetTempPath(),"PreparedPaintCheck-"+Guid.NewGuid());
 var first="b"+Guid.NewGuid();var second="b"+Guid.NewGuid();
 foreach(var owner in new[]{first,second}) {
  Require(BridgeDependencyCopies.Save(test,owner,new[]{seed},id=>graph.GetValueOrDefault(id)??(builtins.Contains(id)?new(){Builtin=true}:null),out var count,out var error,out _),error);
  Require(count==(variant=="Paint"?3:2),"complete prepared graph snapshotted per bridge");
  foreach(var item in graph)Require(File.ReadAllBytes(Path.Combine(BridgeDependencyCopies.Folder(test,owner),item.Key+item.Value.Extension)).SequenceEqual(item.Value.Bytes),"owner copy is byte-identical");
 }
 // This exact test-owned temporary path is the first owner's snapshot only.
 Directory.Delete(BridgeDependencyCopies.Folder(test,first),true);
 foreach(var item in graph)Require(File.ReadAllBytes(Path.Combine(BridgeDependencyCopies.Folder(test,second),item.Key+item.Value.Extension)).SequenceEqual(item.Value.Bytes),"deleting one bridge preserves the other");
 Require(File.ReadAllBytes(Path.Combine(folder,surfaceName)).SequenceEqual(graph[seed].Bytes),"deleting a snapshot preserves prepared source");
 }
}
Require(!Directory.EnumerateDirectories(root,"*",SearchOption.AllDirectories).Any(d=>!Directory.EnumerateFileSystemEntries(d).Any()),"no empty material directories");
Console.WriteLine($"PASS prepared paint/cable/road: {styles.Length} styles, {files} payloads, {digest.Count} distinct identities; full mips, self-contained graphs, exact snapshots, independent deletion.");
int lanes=0;
foreach(var style in styles)
foreach(var file in Directory.GetFiles(Path.Combine(root,style,"Lanes"),"*.Surface",SearchOption.AllDirectories)) {
 var bytes=File.ReadAllBytes(file);var cid=File.ReadAllText(file+".cid");
 // These five properties are the measured 0/1/35 native VT stack slots;
 // offline preparation separately verifies every dependency against native archives.
 var references=BridgeSurfaceReferences.Read(bytes,(_,p)=>new[]{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"}.Contains(p));
 var source=new BridgeDependencyCopies.Source{Extension=".Surface",Bytes=bytes,SerializedReferences=references};
 var hash=Convert.ToHexString(SHA256.HashData(bytes));
 if(digest.TryGetValue(cid,out var previous))Require(previous==hash,"lane same-CID bytes identical");
 digest[cid]=hash;
 var test=Path.Combine(Path.GetTempPath(),"PreparedLanePersistence-"+Guid.NewGuid());
 var first="b"+Guid.NewGuid();var second="b"+Guid.NewGuid();
 foreach(var owner in new[]{first,second}) {
  Require(BridgeDependencyCopies.Save(test,owner,new[]{cid},id=>id==cid?source:references.Contains(id)?new(){Builtin=true}:null,
   out var count,out var error,out _),error);
  Require(count==1,"only converted lane descriptor copied, builtin VT retained");
 }
 Directory.Delete(BridgeDependencyCopies.Folder(test,first),true);
 Require(File.ReadAllBytes(Path.Combine(BridgeDependencyCopies.Folder(test,second),cid+".Surface")).SequenceEqual(bytes),"independent lane owner snapshot");
 Require(File.ReadAllBytes(file).SequenceEqual(bytes),"lane prepared source retained");lanes++;
}
Console.WriteLine($"PASS prepared lanes: {lanes} style variants; exact descriptor-only snapshots; built-in dependency boundary and independent deletion.");
static void Texture(string path,byte[] bytes) {
 using var stream=new MemoryStream(bytes);using var r=new BinaryReader(stream);
 Require(r.ReadUInt16()==5,"native Texture v5");
 int w=r.ReadUInt16(),h=r.ReadUInt16(),depth=r.ReadUInt16(),mips=r.ReadByte(),format=r.ReadByte(),dimension=r.ReadByte();
 int filter=r.ReadByte(),wrap=r.ReadByte(),aniso=r.ReadByte();
 int compressed=r.ReadInt32();
 Require(depth==1&&dimension==2&&compressed>=-1,"ordinary native 2D texture");
 bool color=Path.GetFileName(path)=="SkinnedPaintColor.Texture";
 Require(w==(color?2048:1)&&h==w&&mips==(color?12:1),"source resolution and complete mip chain");
 Require(format==(color?4:8),"RGBA8 sRGB color / linear control");
 int size=0;for(int i=0;i<mips;i++)size+=Math.Max(1,w>>i)*Math.Max(1,h>>i)*4;
 Require(stream.Length-stream.Position==(compressed==-1?size:compressed),"all stored native mip bytes present");
 if(compressed!=-1) {
  // The Unity/native-reader test separately decompresses and uploads every mip.
  Require(r.ReadUInt32()==0xfd2fb528,"native ZSTD frame");
 }
 if(color)Require(filter==2&&wrap==1,"trilinear clamped atlas");
 else { Require(compressed==-1,"tiny control stays raw");Require(r.ReadBytes(size).All(b=>b==0),"zero control does not tint paint"); }
}
static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
namespace BridgeBuilder.Runtime { internal static class BridgeAssetInfo { internal static bool IsPrefabName(string n)=>System.Text.RegularExpressions.Regex.IsMatch(n,@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z"); } }
