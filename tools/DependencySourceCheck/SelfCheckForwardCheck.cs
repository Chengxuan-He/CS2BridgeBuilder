using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

internal static class SelfCheckForwardCheck
{
    internal static void Run()
    {
        string root=UnityEngine.Application.persistentDataPath;
        string[] owners={"b"+Guid.NewGuid(),"b"+Guid.NewGuid()};
        string surfaceCid=new string('1',32),cacheCid=new string('2',32);
        var surface=new AssetData(surfaceCid,()=>new MemoryStream(MidMipSnapshotCheck.Surface()));
        surface.Metadata.extension=".Surface";surface.Metadata.packaged=true;
        var cache=new MidMipCacheAsset(cacheCid,()=>new MemoryStream(MidMipSnapshotCheck.Cache(surfaceCid)));
        cache.Metadata.extension=".MidMips";cache.Metadata.fileName="TS512_MB3";
        AssetDatabase.global.Assets.Clear();BridgePreparedMaterials.Assets.Clear();BridgePreparedMaterials.MidMips.Clear();
        AssetDatabase.global.Assets.AddRange(new AssetData[]{surface,cache});
        foreach(string owner in owners)
        {
            var creation=new BridgeCreationDependencies(_=>new[]{cache});
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},creation,out _,out var error,out _),"create original cache snapshot: "+error);
            Check(!File.Exists(Path.Combine(root,"ImportedData",owner,"BridgeCacheEdges.txt")),"creation writes no relation ledger");
        }
        AssetDatabase.global.Assets.Remove(cache);
        int before=AssetDatabase.global.Scans;
        var scope=new BridgeInspectionScope(root,owners);
        Check(scope.PrepareCaches(out var preparationError),"shared preparation precedes all owner checks: "+preparationError);
        foreach(string owner in owners)
        {
            var context=scope.Dependencies(owner,Array.Empty<BridgeInspectionAssets.Entry>());
            Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},context,out _,out var error,out var writes)&&writes==0,"unregistered owner caches recovered without ledger: "+error);
        }
        Check(AssetDatabase.global.Scans-before==1,"one typed cache traversal shared across owners");
        var failed=new BridgeInspectionCaches(()=>throw new IOException("not ready"),()=>Array.Empty<string>());
        Check(!failed.TryFind(surfaceCid,owners[0],out _,out _)&&failed.Traversals==0,"owner lookup never builds inventory late");
        Check(!failed.Prepare(out _),"unready native inventory aborts preparation");
        Check(!failed.TryFind(surfaceCid,owners[1],out _,out _)&&failed.Traversals==1,"failed traversal is not retried per owner");
        string obsolete=Path.Combine(root,"ImportedData",owners[0],"BridgeCacheEdges.txt");
        File.WriteAllText(obsolete,"invalid old ledger must never be parsed");
        var restarted=scope.Dependencies(owners[0],Array.Empty<BridgeInspectionAssets.Entry>());
        Check(BridgeDependencyPersistence.Save(owners[0],new[]{surfaceCid},restarted,out _,out var failure,out _),"obsolete ledger is ignored: "+failure);
        string backup=Path.Combine(root,"backup-original-only");
        Check(BridgeCreationDependencies.RemoveObsoleteRelations(owners[0],backup,out var changed,out failure)&&changed&&!File.Exists(obsolete),"successful upgrade removes obsolete metadata: "+failure);
        Check(File.ReadAllText(Directory.GetFiles(backup,"BridgeCacheEdges.txt",SearchOption.AllDirectories).Single())=="invalid old ledger must never be parsed","obsolete original preserved exactly");
        Check(BridgeCreationDependencies.RemoveObsoleteRelations(owners[0],backup,out changed,out failure)&&!changed,"metadata cleanup idempotent");
        SnapshotOnly();
    }
    private static void SnapshotOnly()
    {
        AssetDatabase.global.Assets.Clear();
        string owner="b"+Guid.NewGuid(),cid=new string('a',32),texture=new string('b',32),cache=new string('c',32);
        string folder=BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner);
        void Save(string id,string ext,byte[] bytes)
        {Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,id+ext),bytes);File.WriteAllText(Path.Combine(folder,id+ext+".cid"),id);}
        byte[] Surface()
        {
            using var output=new MemoryStream();using var w=new BinaryWriter(output);
            w.Write((ushort)1);w.Write(0);w.Write(true);w.Write(1);
            for(int stack=0;stack<2;stack++)
            {w.Write(512);w.Write(512);w.Write(new byte[64]);for(int layer=0;layer<4;layer++)w.Write(Enumerable.Repeat((byte)(stack==0&&layer==0?0xbb:0),16).ToArray());}
            for(int i=0;i<7;i++)w.Write((byte)0);return output.ToArray();
        }
        byte[] Header(int levels)
        {
            using var output=new MemoryStream();using var w=new BinaryWriter(output);
            w.Write((ushort)1);w.Write((byte)0);w.Write(new byte[8]);w.Write((ushort)levels);w.Write(new byte[5]);w.Write(1);return output.ToArray();
        }
        Save(cid,".Surface",Surface());Save(texture,".VTTexture",Header(0));
        BridgeInspectionScope Scope()=>new(UnityEngine.Application.persistentDataPath,new[]{owner});
        var zero=Scope();Check(zero.PrepareCaches(out _),"zero-mip preparation");
        var request=zero.Dependencies(owner,Array.Empty<BridgeInspectionAssets.Entry>());
        Check(BridgeDependencyPersistence.Save(owner,new[]{cid},request,out var count,out var error,out var writes)&&count==2&&writes==0,"owner-only VT zero-mip header needs no cache or ledger: "+error);
        Save(texture,".VTTexture",Header(3));
        var missing=Scope();Check(missing.PrepareCaches(out _),"missing-cache preparation completed");
        request=missing.Dependencies(owner,Array.Empty<BridgeInspectionAssets.Entry>());
        Check(!BridgeDependencyPersistence.Save(owner,new[]{cid},request,out _,out error,out _)&&!request.Unconfirmed&&error.Contains("Required native cache"),"required missing cache is explicit after completed preparation: "+error);
        string cachePath=Path.Combine(folder,cache,"TS512_MB3.MidMips");Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        string second=new string('d',32);Save(second,".Surface",Surface());
        File.WriteAllBytes(cachePath,MidMipSnapshotCheck.Cache(cid,second));File.WriteAllText(cachePath+".cid",cache);
        var ready=Scope();Check(ready.PrepareCaches(out _),"owner-only cache preparation");
        var before=Directory.GetFiles(Path.GetDirectoryName(folder)!,"*",SearchOption.AllDirectories).OrderBy(x=>x).ToArray();
        request=ready.Dependencies(owner,Array.Empty<BridgeInspectionAssets.Entry>());
        Check(BridgeDependencyPersistence.Save(owner,new[]{cid,second},request,out count,out error,out writes)&&count==4&&writes==0,"owner-only primary/duplicate Surface identities share one cache without native handles: "+error);
        Check(before.SequenceEqual(Directory.GetFiles(Path.GetDirectoryName(folder)!,"*",SearchOption.AllDirectories).OrderBy(x=>x)),"self-check adds no owner files or duplicate caches");
        File.WriteAllBytes(cachePath,new byte[8]);var corrupt=Scope();
        Check(!corrupt.PrepareCaches(out error)&&error!=null,"corrupt cache aborts shared preflight before owner mutation");
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
    internal static class MovableLaneMidMips {internal static bool TryCachesByIdentity(string cid,out IEnumerable<AssetData> caches,out string? error){caches=Array.Empty<AssetData>();error="Native index not ready";return false;}internal static bool TryOwnedCachePath(AssetData a,string owner,out string? path,out string? error){path=null;error=null;return true;}internal static bool TryRequiredCaches(AssetData a,string owner,out IEnumerable<AssetData> caches,out string? error){caches=Array.Empty<AssetData>();error="Unexpected native adapter";return false;}}
}
