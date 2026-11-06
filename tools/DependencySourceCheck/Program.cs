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
Check(FixturePersistence.Save(owner,new[]{a},out var count,out var error,out var written)&&count==2&&written==4,error);
TimingCheck.CheckSummary(true, resolves:2, globalBuilds:0, mipBuilds:0, preflight:2, verify:2, payloadBytes:50);
Check(File.ReadAllText(Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner),a+".Prefab"))=="CID:"+b,"alternate original bytes persisted");
Check(AssetDatabase.global.Scans==0,"exact database lookup for multiple missing CIDs");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(stale);
Check(!FixturePersistence.Save("b"+Guid.NewGuid(),new[]{a},out _,out error)&&error.Contains(a),"all sources missing remains failure with CID");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(new AssetData(a,()=>throw new UnauthorizedAccessException("denied")));AssetDatabase.global.Assets.Add(original);
Check(!FixturePersistence.Save(owner,new[]{a},out _,out error)&&error.Contains("denied"),"permission errors are not treated as missing files");
TimingCheck.CheckSummary(false, resolves:1, globalBuilds:0, mipBuilds:0, preflight:0, verify:0, payloadBytes:0);
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(original);AssetDatabase.global.Assets.Add(second);AssetDatabase.global.Scans=0;
Check(FixturePersistence.Save(owner,new[]{a},out _,out error)&&AssetDatabase.global.Scans==0,"readable first match avoids scanning");
var foreign=new AssetData(a,()=>throw new Exception("Foreign source must never be opened"));
foreign.Metadata.path=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData","b"+Guid.NewGuid()+"_Dependencies",a+".Prefab");
AssetDatabase.global.Assets.Insert(0,foreign);
Check(FixturePersistence.Save(owner,new[]{a},out _,out error),"readable foreign first match excluded before stream access");
AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(stale);AssetDatabase.global.Assets.Add(foreign);
Check(!FixturePersistence.Save(owner,new[]{a},out _,out error)&&error.Contains(a),"foreign fallback is never selected");
var imported=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData");
Check(!BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,owner+"_Dependencies",a+".Prefab"),owner,imported),"current bridge remains permitted");
Check(!BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported+"Other","b"+Guid.NewGuid()+".Prefab"),owner,imported),"root containment has directory boundary");
Check(BridgeAssetInfo.IsOtherBridgePrefab(foreign.Metadata.path.Replace("ImportedData", "IMPORTEDDATA").Replace(".Prefab", ".PREFAB"),owner,imported),"case-insensitive foreign ownership");
Check(BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,"b"+Guid.NewGuid(),"Prefabs","Bridge.Prefab"),owner,imported),"short foreign prefab remains excluded by directory UUID");
Check(BridgeAssetInfo.IsOtherBridgePrefab(Path.Combine(imported,"b"+Guid.NewGuid(),"Dependencies",a+".Prefab"),owner,imported),"new foreign dependency directory excluded");
Check(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner)==Path.Combine(imported,owner,"Dependencies"),"dependency copies belong inside owner folder");
AssetDatabase.global.Assets.Clear();
AssetDatabase.global.Assets.Add(new AssetData(a,()=>throw new Exception("Deleted snapshot was read before packaged source")));
BridgePreparedMaterials.Assets.Add(a,original); BridgePreparedMaterials.Assets.Add(b,second);
Check(FixturePersistence.Save(owner,new[]{a},out _,out error),"prepared source precedes deleted same-CID registration");
BridgePreparedMaterials.Assets.Clear();
foreach (var extension in new[]{".Prefab", ".Surface", ".Material", ".Texture", ".VTSurface", ".VTTexture", ".Geometry", ".MidMips"}) {
 var isolatedOwner="b"+Guid.NewGuid();
 var other=new AssetData(a,()=>throw new Exception("Foreign "+extension+" stream opened"));
 other.Metadata.path=Path.Combine(imported,"b"+Guid.NewGuid(),"Dependencies",a+extension);
 other.Metadata.extension=extension;
 AssetDatabase.global.Assets.Clear();
 AssetDatabase.global.Assets.AddRange(new[]{other,original,second});
 Check(FixturePersistence.Save(isolatedOwner,new[]{a},out _,out error),"exclude foreign first-match "+extension+": "+error);
 AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.AddRange(new[]{stale,other});
 Check(!FixturePersistence.Save(isolatedOwner,new[]{a},out _,out error)&&error.Contains("Dependency unavailable"),
  "exclude foreign fallback "+extension+" before reading: "+error);
}
var missingMetadata = new AssetData(new string('9',32),()=>throw new Exception("Missing metadata source must not be read"));
missingMetadata.Metadata.extension=null!;AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(missingMetadata);
Check(!FixturePersistence.Save("b"+Guid.NewGuid(),new[]{new string('9',32)},out _,out error)
 && error.Contains("no native file metadata") && error.Contains(new string('9',32)),"required metadata absence remains a contextual failure");
Exception? diagnostic=null;
Check(!BridgeDependencyCopies.Save(UnityEngine.Application.persistentDataPath,"b"+Guid.NewGuid(),new[]{a},
 _=>throw new NullReferenceException("original inner failure"),out _,out error,out _,null,e=>diagnostic=e)
 && error.Contains(a) && error.Contains("resolve source") && diagnostic?.InnerException is NullReferenceException
 && diagnostic.ToString().Contains("original inner failure"),"failure callback retains inner type/stack and current CID/phase");
if(args.Length==2 && args[0]=="--retired-owner-replay"){OwnerSnapshotReplay.Run(args[1]);return;}
if(args.Length==3 && args[0]=="--saved-materials"){SavedMaterialReplay.Run(args[1],args[2]);return;}
MaterialSnapshotCheck.Run(imported);
MidMipSnapshotCheck.Run(imported);
SelfCheckForwardCheck.Run();
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
  Check(FixturePersistence.Save(materialOwner,new[]{a},out count,out error,out written)&&count==refs.Length&&written==refs.Length*2,
   "owned native material traverses original texture CIDs: "+extension+" "+Path.GetFileName(fixture)+" "+error);
 }
}
namespace UnityEngine {static class Application {public static string persistentDataPath="";}}
namespace Colossal {record Hash128(string Value){public static Hash128 Parse(string s)=>new(s);public override string ToString()=>Value;}}
namespace Colossal.IO.AssetDatabase {
 class AssetData {
  public sealed class Identifier{public Colossal.Hash128 guid=null!;}
  public sealed class Meta{public bool packaged=true;public string path="",subPath="",fileName="",extension=".Prefab";}
  public Identifier id=new();public bool isBuiltin;readonly Func<Stream> open;
  public AssetData(string cid,Func<Stream> read){id.guid=Colossal.Hash128.Parse(cid);open=read;}
  public Meta Metadata=new(); public Meta GetMeta()=>Metadata;public Stream GetReadStream()=>open();
 }
 class MidMipCacheAsset : AssetData {public MidMipCacheAsset(string cid,Func<Stream> read):base(cid,read){} }
 class AssetDatabase {
  public Resources resources=new();
  public class Resources { public Library materialLibrary=new(); }
  public class Library { public Description GetMaterialDescription(int template)=>new(); }
  public class Description { public int GetStackConfigIndex(string property)=>property=="_BaseColorMap" || (SavedMaterialReplay.Active && property is ("_NormalMap" or "_MaskMap" or "_ControlMask" or "_EmissiveColorMap")) ? 0 : -1; }
  public static AssetDatabase global=new(); public List<AssetData> Assets=new();public int Scans;public IEnumerable<AssetDatabase> databases => Assets.Select(asset => new AssetDatabase { Assets = new List<AssetData>{asset} });
  public bool TryGetAsset(Colossal.Hash128 cid,out AssetData asset){asset=Assets.FirstOrDefault(a=>a.id.guid==cid)!;return asset!=null;}
  public bool ForbidEnumeration = true; public IEnumerable<T> GetAssets<T>(){if(ForbidEnumeration)throw new Exception("Global asset enumeration forbidden");Scans++;return Assets.Cast<T>();}
 }
}
namespace BridgeBuilder {static class Mod{public static Logger Log=new();} class Logger{public string LastSave=""; public void Info(string s){if(s.StartsWith("Bridge dependency persistence UUID=")) LastSave=s;} public void Warn(string s){} public void Error(Exception e){}}}
namespace BridgeBuilder.Runtime {
 static class BridgePreparedMaterials {
  internal static readonly Dictionary<string,AssetData> Assets=new();
  internal static readonly Dictionary<string,string[]> MidMips=new();
  internal static bool TryMidMipDependencies(string cid,out string[] caches)=>MidMips.TryGetValue(cid,out caches!);
  internal static string[] MidMipDependencies(string cid)=>MidMips.TryGetValue(cid,out var found)?found:Array.Empty<string>();
  internal static bool TryGet(string cid,out AssetData asset)=>Assets.TryGetValue(cid,out asset!);
 }
}

internal static class TimingCheck
{
    internal static void CheckSummary(bool success, int resolves, int globalBuilds, int mipBuilds, int preflight, int verify, long payloadBytes)
    {
        string line = BridgeBuilder.Mod.Log.LastSave;
        var fields = line.Substring(line.IndexOf(": ") + 2).Split(", ").Select(p => p.Split('='))
            .ToDictionary(pair => pair[0], pair => pair[1]);
        double Value(string key) => double.Parse(fields[key], System.Globalization.CultureInfo.InvariantCulture);
        var stages = new[] { "traversalMs", "resolveMs", "sourceReadMs", "globalIndexMs", "midMipIndexMs", "preflightMs", "writeMs", "verifyMs", "footprintMs", "otherMs" };
        var sum = stages.Sum(Value);
        double elapsed = Value("elapsedMs");
        if (sum < elapsed - 0.006 || sum > elapsed + 1.006 || stages.Any(stage => Value(stage) < 0)
            || bool.Parse(fields["success"]) != success || Value("resolveCalls") != resolves
            || Value("globalIndexBuilds") != globalBuilds || Value("midMipIndexBuilds") != mipBuilds
            || Value("preflightCopies") != preflight || Value("postWriteChecks") != verify
            || Value("payloadBytesWritten") != payloadBytes || (!success && Value("footprintMs") != 0))
            throw new Exception("Unexpected dependency timing summary: " + line);
        Console.WriteLine("PASS exclusive stages and counters: " + line);
    }
}
