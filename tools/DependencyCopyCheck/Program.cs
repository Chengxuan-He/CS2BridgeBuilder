using BridgeBuilder.Runtime;
using System.Text;
var root=Path.Combine(Path.GetTempPath(), "BBDeps-"+Guid.NewGuid().ToString("N"));
var owner="b11111111-1111-1111-1111-111111111111"; var second="b22222222-2222-2222-2222-222222222222";
var a=new string('a',32);var b=new string('b',32);var c=new string('c',32);var d=new string('d',32);
var empty = new string('0',32);
var sources=new Dictionary<string,BridgeDependencyCopies.Source> {
 [a]=new(){Owned=true,Extension=".Prefab",Bytes=Encoding.UTF8.GetBytes("CID:"+b+" CID:"+b+" CID:"+empty)},
 [b]=new(){Extension=".Prefab",Bytes=Encoding.Unicode.GetBytes("CID:"+c+" CID:"+d)},
 [c]=new(){Extension=".Prefab",Bytes=Encoding.UTF8.GetBytes("CID:"+b)},
 [d]=new(){Builtin=true}
};
BridgeDependencyCopies.Source? Resolve(string cid) {
 if(cid==empty) throw new Exception("Empty AssetReference must never reach the asset resolver");
 return sources.GetValueOrDefault(cid);
}
void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
foreach (var bytes in new[] {
 Encoding.UTF8.GetBytes("\"m_GeometryAsset\": $fstrref:\"CID:"+empty+"\", CID:"+b),
 Encoding.Unicode.GetBytes("CID:"+empty+" CID:"+b),
 new byte[]{0x7f}.Concat(Encoding.Unicode.GetBytes("CID:"+empty+" CID:"+b)).ToArray()
}) Check(BridgeDependencyCopies.References(bytes).SequenceEqual(new[]{b}), "UTF-8/UTF-16/unaligned empty references excluded while real dependencies remain");
Check(BridgeDependencyCopies.Save(root,owner,new[]{a},Resolve,out var count,out var err,out var written)&&count==2&&written==4, "recursive binary references, cycle termination, native boundary, per-CID dedup: "+err);
var folder=BridgeDependencyCopies.Folder(root,owner);
Check(File.ReadAllBytes(Path.Combine(folder,b+".Prefab")).SequenceEqual(sources[b].Bytes),"byte-identical payload");
Check(File.ReadAllText(Path.Combine(folder,b+".Prefab.cid"))==b,"original CID");
Check(BridgeDependencyCopies.Save(root,owner,new[]{a},Resolve,out count,out err,out written)&&count==2&&written==0&&Directory.GetFiles(folder).Length==4,"idempotent same-bridge copies");
Check(BridgeDependencyCopies.Save(root,second,new[]{a},Resolve,out count,out err,out _)&&Directory.GetFiles(BridgeDependencyCopies.Folder(root,second)).Length==4,"separate copies for second UUID");
File.Delete(Path.Combine(folder,b+".Prefab.cid"));
Check(BridgeDependencyCopies.Save(root,owner,new[]{a},Resolve,out count,out err,out written)&&written==1&&count==2,"missing sidecar repaired and counted");
File.Delete(Path.Combine(folder,c+".Prefab"));
Check(BridgeDependencyCopies.Save(root,owner,new[]{a},Resolve,out count,out err,out written)&&written==1&&count==2,"missing payload repaired and counted");
sources[b].Bytes=Encoding.UTF8.GetBytes("changed");
Check(!BridgeDependencyCopies.Save(root,owner,new[]{a},Resolve,out count,out err,out _)&&err.Contains("Different contents"),"same CID with changed content rejected without overwriting");
Check(!BridgeDependencyCopies.Save(root,"../foreign",new[]{a},Resolve,out count,out err,out _),"invalid owner rejected");
Check(!BridgeDependencyCopies.Save(root,second,new[]{new string('e',32)},Resolve,out count,out err,out _),"unavailable dependency fails before publishing");
Check(!BridgeDependencyCopies.Save(root,owner,new[]{empty},Resolve,out count,out err,out _) && err=="Invalid dependency CID",
 "empty root seed is rejected, not silently accepted as a valid asset");
foreach(var file in args.SelectMany(path=>Directory.Exists(path)?Directory.GetFiles(path,"*.Surface"):new[]{path})) {
 var bytes=File.ReadAllBytes(file);
 if (Path.GetExtension(file)==".Surface") {
  var stackProperties=new HashSet<string>{"_BaseColorMap","_NormalMap","_MaskMap","_ControlMask","_EmissiveColorMap"};
  var refs=BridgeSurfaceReferences.Read(bytes,(template,property)=>new[]{0,1,16}.Contains(template)&&stackProperties.Contains(property));
  Check(refs.OrderBy(cid=>cid).SequenceEqual(File.ReadAllLines(file+".refs")),"real native surface/VT payload graph: "+Path.GetFileName(file));
  continue;
 }
 if (Path.GetExtension(file)==".bin") {
  var refs=BridgeSurfaceReferences.Read(bytes,(_,_)=>throw new Exception("Expected ordinary non-VT source fixture"));
  // Independent oracle for these extracted non-VT files: texture property names followed by
  // native present marker + 16 raw hash bytes. The structured parser must match every reference.
  var text=Encoding.Latin1.GetString(bytes);
  var expected=System.Text.RegularExpressions.Regex.Matches(text,@"(_[A-Za-z][A-Za-z0-9_]*)\xff")
   .Select(m=>string.Concat(bytes.Skip(m.Index+m.Length).Take(16).Select(v=>"0123456789abcdef"[v&15].ToString()+"0123456789abcdef"[v>>4])))
   .Where(cid=>cid!=empty).Distinct().OrderBy(cid=>cid);
  Check(refs.OrderBy(cid=>cid).SequenceEqual(expected),"actual native surface reference fields: "+Path.GetFileName(file));
  continue;
 }
 Check(!BridgeDependencyCopies.References(bytes).Contains(empty),"actual serialized fixture excludes empty geometry reference: "+Path.GetFileName(file));
}
var textureCid="0123456789abcdef0123456789abcdef";
var vtCid="123456789abcdef0123456789abcdef0";
var surfaceCid="23456789abcdef0123456789abcdef01";
var ordinaryCid="3456789abcdef0123456789abcdef012";
byte[] Surface(bool vt) {
 using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
 void Hash(string cid) { for(int i=0;i<32;i+=2) w.Write((byte)(Convert.ToInt32(cid[i].ToString(),16)|(Convert.ToInt32(cid[i+1].ToString(),16)<<4))); }
 w.Write((ushort)1);w.Write(16);w.Write(vt);
 if(vt) {
  w.Write(1);
  for(int stack=0;stack<2;stack++) {
   w.Write(1024);w.Write(1024);
   for(int layer=0;layer<4;layer++)Hash(stack==0&&layer==0?textureCid:empty);
   for(int layer=0;layer<4;layer++)Hash(stack==0&&layer==0?vtCid:empty);
  }
  w.Write((byte)255);Hash(surfaceCid);
 }
 for(int i=0;i<4;i++)w.Write((byte)0);
 w.Write((byte)255);w.Write(2);
 w.Write("_BaseColorMap");w.Write((byte)255);Hash(textureCid);
 w.Write("_NonStackMap");w.Write((byte)255);Hash(ordinaryCid);
 w.Write((byte)255);w.Write(1);w.Write("CID:"+a);
 return stream.ToArray();
}
var vtBytes=Surface(true);
var vtRefs=BridgeSurfaceReferences.Read(vtBytes,(template,property)=>template==16&&property=="_BaseColorMap");
Check(vtRefs.OrderBy(x=>x).SequenceEqual(new[]{vtCid,surfaceCid,ordinaryCid}.OrderBy(x=>x)),"VT payloads and non-stack textures traversed; original VT source identity and keyword text excluded");
Check(BridgeSurfaceReferences.Read(Surface(false),(_,_)=>false).OrderBy(x=>x).SequenceEqual(new[]{textureCid,ordinaryCid}.OrderBy(x=>x)),"ordinary binary texture identities use native Hash128 nibble order");
sources[a]=new(){Owned=true,Extension=".Surface",Bytes=vtBytes,SerializedReferences=vtRefs};
sources[vtCid]=new(){Extension=".VTTexture",Bytes=new byte[]{1,2,3}};
sources[surfaceCid]=new(){Extension=".VTSurface",Bytes=new byte[]{4,5,6}};
sources[ordinaryCid]=new(){Builtin=true};
var vtOwner="b33333333-3333-3333-3333-333333333333";
Check(BridgeDependencyCopies.Save(root,vtOwner,new[]{a},Resolve,out count,out err,out written)&&count==2&&written==4,"owned Surface follows binary references without copying the owned asset: "+err);
Check(File.ReadAllBytes(Path.Combine(BridgeDependencyCopies.Folder(root,vtOwner),vtCid+".VTTexture")).SequenceEqual(sources[vtCid].Bytes),"VT payload snapshot is byte-identical");
sources.Remove(vtCid);
Check(!BridgeDependencyCopies.Save(root,vtOwner,new[]{a},Resolve,out _,out err,out _)&&err.Contains(vtCid),"missing VT payload is an explicit persistence failure");
foreach(var invalid in new[]{vtBytes.Take(50).ToArray(),new byte[]{12,0,0,0,0,0,0},Surface(false).Concat(new byte[]{1}).ToArray()}) {
 bool rejected=false;try {BridgeSurfaceReferences.Read(invalid,(_,_)=>false);}catch(Exception e) when(e is IOException or InvalidDataException){rejected=true;}
 Check(rejected,"truncated, unsupported-version or trailing surface data rejected");
}
namespace BridgeBuilder.Runtime { internal static class BridgeAssetInfo { internal static bool IsPrefabName(string n)=>System.Text.RegularExpressions.Regex.IsMatch(n,@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z"); } }
