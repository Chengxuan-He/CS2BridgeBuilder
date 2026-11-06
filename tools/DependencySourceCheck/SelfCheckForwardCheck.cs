using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

internal static class SelfCheckForwardCheck
{
    internal static void Run()
    {
        string root=UnityEngine.Application.persistentDataPath, owner="b"+Guid.NewGuid(), other="b"+Guid.NewGuid();
        string surfaceCid=new string('1',32), cacheCid=new string('2',32);
        var surface=new AssetData(surfaceCid,()=>new MemoryStream(MidMipSnapshotCheck.Surface()));
        surface.Metadata.extension=".Surface"; surface.Metadata.packaged=true;
        var cache=new MidMipCacheAsset(cacheCid,()=>new MemoryStream(MidMipSnapshotCheck.Cache(surfaceCid)));
        cache.Metadata.extension=".MidMips";cache.Metadata.fileName="TS512_MB3";cache.Metadata.packaged=true;
        AssetDatabase.global.Assets.Clear();BridgePreparedMaterials.Assets.Clear();BridgePreparedMaterials.MidMips.Clear();
        AssetDatabase.global.Assets.Add(surface); AssetDatabase.global.Assets.Add(cache);
        AssetDatabase.global.ForbidEnumeration=true;
        try
        {
            // Existing owners without a ledger use only the selected native material's provenance.
            int nativeQueries=0;
            var old=new BridgeCreationDependencies(a=>{if(!ReferenceEquals(a,surface))throw new Exception("Unexpected native material");nativeQueries++;return new[]{cache};});
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},old,out _,out var error,out _),"historical exact native provenance: "+error);
            Check(nativeQueries==1,"native provenance queried only for required Surface");
            string path=Path.Combine(BridgeDependencyCopies.Folder(root,owner),cacheCid,"TS512_MB3.MidMips");
            string ledger=Path.Combine(root,"ImportedData",owner,"BridgeCacheEdges.txt");
            string original=File.ReadAllText(ledger);
            AssetDatabase.global.Assets.Remove(cache);
            BridgeCreationDependencies Restart() { var x=new BridgeCreationDependencies(_=>throw new Exception("Unexpected discovery"));x.ReadRelations(owner);return x; }
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out int writes)&&writes==0,"unregistered owned cache exact path after restart: "+error);
            File.Delete(path+".cid");
            Check(!BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out _)&&error.Contains("Dependency unavailable"),"missing cache sidecar remains explicit failure");
            AssetDatabase.global.Assets.Add(cache);
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out writes)&&writes==1,"registered exact source repairs missing cache sidecar: "+error);
            AssetDatabase.global.Assets.Remove(cache);
            File.WriteAllText(path+".cid",new string('3',32));
            Check(!BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out _)&&error.Contains("sidecar mismatch"),"wrong local CID rejected");
            File.WriteAllText(path+".cid",cacheCid);
            File.WriteAllBytes(path,MidMipSnapshotCheck.Cache(new string('4',32)));
            Check(!BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out _)&&error.Contains("membership"),"cache without required Surface rejected");
            File.WriteAllBytes(path,MidMipSnapshotCheck.Cache(surfaceCid));
            foreach(string bad in new[]{original.Replace(owner,other),original.Replace("BridgeCacheEdges/1","BridgeCacheEdges/2"),original+"E\t"+surfaceCid+"\n"})
            {
                File.WriteAllText(ledger,bad);
                try { Restart();throw new Exception("Invalid ledger accepted"); } catch(InvalidDataException) {}
            }
            File.WriteAllText(ledger,original);
            string secondCid=new string('7',32);
            var second=new AssetData(secondCid,()=>new MemoryStream(MidMipSnapshotCheck.Surface()));
            second.Metadata.extension=".Surface";second.Metadata.packaged=true;AssetDatabase.global.Assets.Add(second);
            File.WriteAllText(ledger,original+"E\t"+secondCid+"\t"+cacheCid+"\n");
            Check(!BridgeDependencyPersistence.Save(owner,new[]{secondCid,surfaceCid},Restart(),out _,out error,out _)
                &&error.Contains("membership"),"shared cache checked for Surface reached after cache visitation");
            File.WriteAllText(ledger,original);AssetDatabase.global.Assets.Remove(second);
            string unrelated=Path.Combine(root,"ImportedData","Unrelated","Deep",other,"poison.Prefab");
            Directory.CreateDirectory(Path.GetDirectoryName(unrelated)!);File.WriteAllText(unrelated,"not a Prefab");
            var scope=new BridgeInspectionScope(root,new[]{owner});
            Check(scope.Paths(owner).Contains(path)&&!scope.Paths(owner).Contains(unrelated),"path discovery enters only named owner directories, no content/CID index");
            Check(scope.Find(surfaceCid,owner)==null,"packaged source is not a local legacy clone target");
            Check(BridgeBuilder.Mod.Log.LastSave.Contains("globalIndexBuilds=0, midMipIndexBuilds=0"),"self-check failure paths never build global indices");
            var relocated=Restart();
            string moved=Path.Combine(root,"ImportedData",owner,"Appearance","MidMips",cacheCid,"TS512_MB3.MidMips");
            Directory.CreateDirectory(Path.GetDirectoryName(moved)!);File.Move(path,moved);File.Move(path+".cid",moved+".cid");
            relocated.Relocate(BridgeFileAccess.Native(path.Replace('\\','/')),moved);
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},relocated,out _,out error,out writes)&&writes==0,
                "cache relocation matches native/logical slash aliases: "+error);
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},Restart(),out _,out error,out writes)&&writes==0,
                "relocated cache edge survives restart without old-path reads: "+error);
        }
        finally {AssetDatabase.global.ForbidEnumeration=false;}
        var testOwner="b"+Guid.NewGuid(); var id=new string('c',32);
        var path2=Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,testOwner),id+".Texture");
        Directory.CreateDirectory(Path.GetDirectoryName(path2)!);
        var foreignHandle=new AssetData(id,()=>throw new Exception("Foreign owner bytes opened"));
        foreignHandle.Metadata.path=Path.Combine(BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,"b"+Guid.NewGuid()),id+".Texture");foreignHandle.Metadata.packaged=false;
        AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(foreignHandle);
        var inspect=new BridgeCreationDependencies(_=>Array.Empty<AssetData>());inspect.ReadRelations(testOwner);
        var timing=new DependencySaveTimings();
        Check(inspect.Resolve(id,testOwner,timing)==null&&inspect.Unconfirmed,"Foreign-only unresolved identity remains unfinished");
        File.WriteAllBytes(path2,new byte[]{101});File.WriteAllText(path2+".cid",id);
        Check(inspect.Resolve(id,testOwner,timing)!.Bytes.SequenceEqual(new byte[]{101})&&!inspect.Unconfirmed,"Owner file wins over foreign duplicate");
        File.WriteAllText(path2+".cid",new string('f',32));
        try { inspect.Resolve(id,testOwner,timing);throw new Exception("Wrong sidecar accepted"); }
        catch(InvalidDataException){Check(!inspect.Unconfirmed,"Mismatched sidecar is a definite integrity failure");}
        File.Delete(path2);File.Delete(path2+".cid");AssetDatabase.global.Assets.Clear();
        Check(inspect.Resolve(id,testOwner,timing)==null&&!inspect.Unconfirmed,"Missing identity without stale handle retains definite missing result");
    }
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
}

namespace BridgeBuilder.Runtime
{
    internal static class BridgeInspectionAssets {internal sealed class Entry {internal AssetData Asset=null!;}}
    internal static class BridgePreparedMaterialFiles {internal static string? CacheFor(string cid)=>null;}
}
namespace BridgeBuilder.Bridges
{
    internal static class MovableLaneMidMips {internal static string? OwnedCachePath(AssetData a,string owner)=>null;internal static IEnumerable<AssetData> RequiredCaches(AssetData a,string owner)=>throw new Exception("Unexpected native adapter");}
}
