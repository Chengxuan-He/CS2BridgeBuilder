using BridgeBuilder.Runtime;
using System.Text;
var root=Path.Combine(Path.GetTempPath(), "BBDeps-"+Guid.NewGuid().ToString("N"));
var owner="b11111111-1111-1111-1111-111111111111"; var second="b22222222-2222-2222-2222-222222222222";
var a=new string('a',32);var b=new string('b',32);var c=new string('c',32);var d=new string('d',32);
var sources=new Dictionary<string,BridgeDependencyCopies.Source> {
 [a]=new(){Owned=true,Extension=".Prefab",Bytes=Encoding.UTF8.GetBytes("CID:"+b+" CID:"+b)},
 [b]=new(){Extension=".Prefab",Bytes=Encoding.Unicode.GetBytes("CID:"+c+" CID:"+d)},
 [c]=new(){Extension=".Prefab",Bytes=Encoding.UTF8.GetBytes("CID:"+b)},
 [d]=new(){Builtin=true}
};
BridgeDependencyCopies.Source? Resolve(string cid)=>sources.GetValueOrDefault(cid);
void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
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
Check(BridgeDependencyCopies.Archive(root,owner,root+"-backup",out err)&&!Directory.Exists(folder),"removed owner's copies archived");
Check(Directory.Exists(BridgeDependencyCopies.Folder(root,second)),"other bridge's same-CID copies retained");
namespace BridgeBuilder.Runtime { internal static class BridgeAssetInfo { internal static bool IsPrefabName(string n)=>System.Text.RegularExpressions.Regex.IsMatch(n,@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z"); } }
