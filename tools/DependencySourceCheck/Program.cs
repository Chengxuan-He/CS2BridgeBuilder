using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using System.Text;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
var owner="b"+Guid.NewGuid();
UnityEngine.Application.persistentDataPath=Path.Combine(Path.GetTempPath(),"BridgeSourceCheck-"+Guid.NewGuid());
var a=new string('a',32); var b=new string('b',32);
var stale=new AssetData(a,()=>throw new DirectoryNotFoundException("deleted bridge directory"));
var missing=new AssetData(a,()=>throw new FileNotFoundException("missing copy"));
var original=new AssetData(a,()=>new MemoryStream(Encoding.UTF8.GetBytes("CID:"+b)));
var second=new AssetData(b,()=>new MemoryStream(Encoding.UTF8.GetBytes("original bytes")));
AssetDatabase.global.Assets.AddRange(new[]{stale,missing,original,new AssetData(b,()=>throw new FileNotFoundException()),second});
Check(BridgeDependencyPersistence.Save(owner,new[]{a},out var count,out var error,out var written)&&count==2&&written==4,error);
Check(File.ReadAllText(Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner),a+".Prefab"))=="CID:"+b,"alternate original bytes persisted");
Check(AssetDatabase.global.Scans==1,"one alternate index for multiple missing CIDs");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(stale);
Check(!BridgeDependencyPersistence.Save("b"+Guid.NewGuid(),new[]{a},out _,out error)&&error.Contains(a),"all sources missing remains failure with CID");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(new AssetData(a,()=>throw new UnauthorizedAccessException("denied")));AssetDatabase.global.Assets.Add(original);
Check(!BridgeDependencyPersistence.Save(owner,new[]{a},out _,out error)&&error.Contains("denied"),"permission errors are not treated as missing files");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(original);AssetDatabase.global.Assets.Add(second);AssetDatabase.global.Scans=0;
Check(BridgeDependencyPersistence.Save(owner,new[]{a},out _,out error)&&AssetDatabase.global.Scans==0,"readable first match avoids scanning");
var foreign=new AssetData(a,()=>throw new Exception("Foreign source must never be opened"));
foreign.Metadata.path=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData","b"+Guid.NewGuid()+"_Dependencies",a+".Prefab");
AssetDatabase.global.Assets.Insert(0,foreign);
Check(BridgeDependencyPersistence.Save(owner,new[]{a},out _,out error),"readable foreign first match excluded before stream access");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(stale);AssetDatabase.global.Assets.Add(foreign);
Check(!BridgeDependencyPersistence.Save(owner,new[]{a},out _,out error)&&error.Contains(a),"foreign fallback is never selected");
var imported=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData");
Check(!BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,owner+"_Dependencies",a+".Prefab"),owner,imported),"current bridge remains permitted");
Check(!BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported+"Other","b"+Guid.NewGuid()+".Prefab"),owner,imported),"root containment has directory boundary");
Check(BridgeAssetInfo.IsOtherBridgePrefab(foreign.Metadata.path.Replace("ImportedData", "IMPORTEDDATA").Replace(".Prefab", ".PREFAB"),owner,imported),"case-insensitive foreign ownership");
Check(BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,"b"+Guid.NewGuid(),"Prefabs","Bridge.Prefab"),owner,imported),"short foreign prefab remains excluded by directory UUID");
Check(BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,"b"+Guid.NewGuid(),"Dependencies",a+".Prefab"),owner,imported),"new foreign dependency directory excluded");
Check(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner)==Path.Combine(imported,owner,"Dependencies"),"dependency copies belong inside owner folder");
foreach(var fixture in args) {
 var payload=File.ReadAllBytes(fixture);
 var refs=BridgeSurfaceReferences.Read(payload,(_,_)=>false);
 foreach(var extension in new[]{".Surface",".Material"}) {
  var materialOwner="b"+Guid.NewGuid();
  AssetDatabase.global.Assets.Clear();
  var material=new AssetData(a,()=>new MemoryStream(payload));
  material.Metadata.extension=extension;material.Metadata.packaged=false;
  material.Metadata.path=Path.Combine(imported,materialOwner+extension);
  AssetDatabase.global.Assets.Add(material);
  foreach(var cid in refs) {
   var texture=new AssetData(cid,()=>new MemoryStream(new byte[]{4,2}));
   texture.Metadata.extension=".Texture";AssetDatabase.global.Assets.Add(texture);
  }
  Check(BridgeDependencyPersistence.Save(materialOwner,new[]{a},out count,out error,out written)&&count==refs.Length&&written==refs.Length*2,
   "owned native material traverses original texture CIDs: "+extension+" "+Path.GetFileName(fixture)+" "+error);
 }
}
namespace UnityEngine {static class Application {public static string persistentDataPath="";}}
namespace Colossal {record Hash128(string Value){public static Hash128 Parse(string s)=>new(s);public override string ToString()=>Value;}}
namespace Colossal.IO.AssetDatabase {
 class AssetData {
  public sealed class Identifier{public Colossal.Hash128 guid=null!;}
  public sealed class Meta{public bool packaged=true;public string path="",subPath="",extension=".Prefab";}
  public Identifier id=new();public bool isBuiltin=>false;readonly Func<Stream> open;
  public AssetData(string cid,Func<Stream> read){id.guid=Colossal.Hash128.Parse(cid);open=read;}
  public Meta Metadata=new(); public Meta GetMeta()=>Metadata;public Stream GetReadStream()=>open();
 }
 class AssetDatabase {
  public Resources resources=new();
  public class Resources { public Library materialLibrary=new(); }
  public class Library { public Description GetMaterialDescription(int template)=>new(); }
  public class Description { public int GetStackConfigIndex(string property)=>property=="_BaseColorMap"?0:-1; }
  public static AssetDatabase global=new(); public List<AssetData> Assets=new();public int Scans;
  public bool TryGetAsset(Colossal.Hash128 cid,out AssetData asset){asset=Assets.FirstOrDefault(a=>a.id.guid==cid)!;return asset!=null;}
  public IEnumerable<T> GetAssets<T>(){Scans++;return Assets.Cast<T>();}
 }
}
namespace BridgeBuilder {static class Mod{public static Logger Log=new();} class Logger{public void Info(string s){} public void Warn(string s){}}}
