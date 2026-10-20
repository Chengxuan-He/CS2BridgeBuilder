using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
const string owner = "b11111111-2222-3333-4444-555555555555";
void Check(bool ok,string message) { if(!ok) throw new Exception(message); Console.WriteLine("PASS "+message); }
var sandbox=Path.Combine(Path.GetTempPath(),"BBPersistence-"+Guid.NewGuid());
Directory.CreateDirectory(sandbox);
try {
    var root=BridgePrefabPersistence.RelativePathFor(owner).Replace('\\','/');
    Check(root==$"ImportedData/{owner}/Prefabs/Bridge.Prefab","new root filename");
    Check(BridgePrefabPersistence.RelativePathFor("RBBridgeDep_NetSectionPrefab_RB_Empty_Section_2.5_499b37c1e0-"+owner).Replace('\\','/')
        ==$"ImportedData/{owner}/Prefabs/RBBridgeDep_NetSectionPrefab_RB_Empty_Section_2_5_499b37c1e0.Prefab",
        "Road Builder decimal stem retains real Prefab extension");
    Check(BridgeAssetInfo.FileStem("A.<>:\"/\\|?*\u0001-"+owner,owner)=="A___________",
        "period and Windows-invalid characters replaced individually");
    Check(BridgeAssetInfo.FileStem("-Part 2.5","")=="-Part 2_5","unowned resource keeps prefix");
    Check(BridgePrefabPersistence.ResourcePath(owner+" Road 2.5","Geometry").ToPath(new()).Replace('\\','/')
        ==$"ImportedData/{owner}/Geometry/Road 2_5","generated geometry uses same safe stem");
    Check(BridgePrefabPersistence.RelativePathFor(owner+" Fixed Base 0 LOD2").Replace('\\','/')==$"ImportedData/{owner}/Prefabs/Fixed Base 0 LOD2.Prefab","flat LOD path");
    Check(BridgeAssetInfo.ShortName("Part-"+owner+" Mesh",owner)=="PartMesh","embedded owner UUID removed");
    Check(BridgePrefabPersistence.RelativePathFor("GoldenGateDouble-38_5-"+owner).Replace('\\','/')==$"ImportedData/{owner}/Prefabs/GoldenGateDouble-38_5.Prefab","requested GoldenGate filename");
    Check(BridgeAssetInfo.ShortName("Part"+owner+" LOD1-"+owner,owner)=="PartLOD1-"+owner,"only the first occurrence removed");
    Check(BridgeAssetInfo.ShortName(owner.ToUpperInvariant()+"-en-US",owner)=="en-US","following hyphen removed and case-insensitive identity");
    Check(BridgeAssetInfo.ShortName("GoldenGateDouble-38_5",owner)=="GoldenGateDouble-38_5","already short filename unchanged");
    Check(BridgeAssetInfo.ShortName("Name-"+owner+"_LOD1",owner)=="NameLOD1","greedy surrounding hyphen and underscore");
    Check(BridgeAssetInfo.ShortName("Name"+owner+"_LOD1",owner)=="NameLOD1","bare UUID with following underscore");
    Check(BridgeAssetInfo.ShortName("  Name-"+owner+"__LOD1- ",owner)=="  Name_LOD1- ","every character outside the match remains unchanged");
    Check(BridgeAssetInfo.ShortName("Name-"+owner+"-LOD1",owner)=="NameLOD1","greedy hyphens on both sides");
    Check(BridgeAssetInfo.ShortName("Name-"+owner+"--LOD1",owner)=="Name-LOD1","only one following separator consumed");
    Check(BridgeAssetInfo.ShortName(owner+" Fixed Base 0 LOD1.Prefab",owner)=="Fixed Base 0 LOD1.Prefab","following space removed");
    Check(BridgeAssetInfo.ShortName(owner+"  Mesh.Geometry",owner)==" Mesh.Geometry","only one following space consumed");
    Check(BridgeAssetInfo.ShortName(owner+"\tMesh.Geometry",owner)=="\tMesh.Geometry","tabs remain unchanged");
    Check(BridgeAssetInfo.ShortName(" Mesh.Geometry",owner)==" Mesh.Geometry","no retroactive trimming without owner match");
    var db=new FakeDatabase(sandbox);
    AssetData Make(string name) {
        var source=Path.Combine(sandbox,"BridgeBuilder",name+".Surface");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllBytes(source,new byte[]{2,4,6}); File.WriteAllText(source+".cid",name);
        var a=new AssetData { database=db,id=Guid.NewGuid().ToString("N") };
        db.dataSource.Entries[a.id]=source; return a;
    }
    var asset=Make(owner+" Road Surface"); var id=asset.id; var old=asset.GetMeta().path;
    BridgePrefabPersistence.RelocateGenerated(asset,owner,"Appearance");
    var final=Path.Combine(sandbox,"ImportedData",owner,"Appearance","Road Surface.Surface");
    Check(asset.id==id && asset.GetMeta().path==final && !File.Exists(old)
        && File.ReadAllBytes(final).SequenceEqual(new byte[]{2,4,6})
        && File.ReadAllText(final+".cid")==owner+" Road Surface","relocation retains CID and payload; registration uses new path");
    BridgePrefabPersistence.RelocateGenerated(asset,owner,"Appearance");
    Check(File.Exists(final),"already relocated resource is a no-op");
    var failing=Make(owner+" Failing"); var failureOld=failing.GetMeta().path;
    db.dataSource.FailNextAdd=true;
    try { BridgePrefabPersistence.RelocateGenerated(failing,owner,"Appearance"); throw new Exception("failure expected"); }
    catch(IOException) { }
    Check(failing.GetMeta().path==failureOld && File.Exists(failureOld) && File.Exists(failureOld+".cid"),"failed re-registration restores original payload and CID");
    var collision=Make(owner+" Road Surface");
    try { BridgePrefabPersistence.RelocateGenerated(collision,owner,"Appearance"); throw new Exception("collision expected"); }
    catch(IOException) { }
    Check(File.Exists(collision.GetMeta().path)&&File.ReadAllBytes(final).SequenceEqual(new byte[]{2,4,6}),"generation collision never replaces existing file");
}
finally { Directory.Delete(sandbox,true); }
namespace Colossal.IO.AssetDatabase {
 enum EscapeStrategy { None }
 class AssetDataPath {
  readonly string path;
  AssetDataPath(string p){path=p;}
  public static AssetDataPath Create(string folder,string name,EscapeStrategy strategy=EscapeStrategy.None)=>new(Path.Combine(folder,name));
  public string ToPath(FileSystemDataSource.PathEscapePolicy p)=>path;
 }
 class FileSystemDataSource { public class PathEscapePolicy {} }
 class Meta {
  public string path=""; public string extension=>Path.GetExtension(path);
  public string subPath=>Path.GetDirectoryName(path)!;public string fileName=>Path.GetFileNameWithoutExtension(path);
  public DateTime creationTime,lastAccessTime,lastWriteTime;
 }
 class AssetData {
  public FakeDatabase database=null!;public string id="";
  public string name=>Path.GetFileNameWithoutExtension(GetMeta().path);
  public Meta GetMeta()=>new(){path=database.dataSource.Entries[id]};
 }
 class FakeDatabase {
  public string rootPath;public Source dataSource;
  public FakeDatabase(string root){rootPath=root;dataSource=new(root);}
 }
 class Source {
  readonly string root; public Dictionary<string,string> Entries=new();public bool FailNextAdd;
  public Source(string r){root=r;}
  public void DeleteEntryFromDatabase(string id){var p=Entries[id];Entries.Remove(id);File.Delete(p);File.Delete(p+".cid");}
  public bool Contains(string id)=>Entries.ContainsKey(id);
  public string AddEntryFromDatabase(AssetDataPath p,Type t,string id) {
   if(FailNextAdd){FailNextAdd=false;throw new IOException("injected registration failure");}
   Entries[id]=Path.GetFullPath(Path.Combine(root,p.ToPath(new()))+".Surface"); return id;
  }
  public void SetTimestamp(string id,DateTime a,DateTime b,DateTime c){}
 }
 static class AssetDatabase { public static FakeDatabase user=new(Path.GetTempPath()); }
 static class PrefabAssetExtensions { public static void AddAsset(FakeDatabase d,AssetDataPath p,CS2Mods.Shared.Conversion.Target t){} }
}
namespace CS2Mods.Shared.Conversion {
 class Target { public string name="";public object? asset; }
 class PrefabCloneNode {public bool NeedsSave;public object? ReplacementAsset;public Target Target=new();}
}
namespace CS2Mods.Shared.Export {class PrefabAssetWriter {public int Save(IReadOnlyList<CS2Mods.Shared.Conversion.PrefabCloneNode> nodes)=>0;}}
namespace Game.AssetPipeline {
 enum DataType {Prefab}
 static class AssetImportPipeline {
 public static Colossal.IO.AssetDatabase.AssetDataPath GetPath(object? a,string b,string c,DataType d,Colossal.IO.AssetDatabase.FakeDatabase e,bool f)=>Colossal.IO.AssetDatabase.AssetDataPath.Create("ImportedData/"+b,c);
 }
}

