using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

internal static class MidMipSnapshotCheck
{
    internal static void Run(string imported)
    {
        string surfaceCid = new string('5',32), cacheCid = new string('6',32);
        string owner = "b"+Guid.NewGuid(), otherOwner = "b"+Guid.NewGuid();
        var surfaceBytes = Surface(); var cacheBytes = Cache(surfaceCid);
        AssetData Asset(string cid,string path,string extension,byte[] bytes,bool packaged=true)
        {
            var asset=new AssetData(cid,()=>new MemoryStream(bytes));
            asset.Metadata.path=path; asset.Metadata.extension=extension; asset.Metadata.packaged=packaged;
            return asset;
        }
        var surface=Asset(surfaceCid,"Package/Surface.Surface",".Surface",surfaceBytes);
        var cache=Asset(cacheCid,"Package/Materials/Shared/MidMips/"+cacheCid+"/TS512_MB3.MidMips",".MidMips",cacheBytes);
        var forbidden=new AssetData(cacheCid,()=>throw new Exception("Foreign cache read"));
        forbidden.Metadata.extension=".MidMips";
        forbidden.Metadata.path=Path.Combine(imported,"b"+Guid.NewGuid(),"Dependencies",cacheCid,"TS512_MB3.MidMips");
        AssetDatabase.global.Assets.Clear(); AssetDatabase.global.Assets.AddRange(new[]{surface,forbidden,cache});
        Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},out var count,out var error,out var written)
            && count==2 && written==4,"implicit native cache copied: "+error);
        string folder=BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner);
        string path=Path.Combine(folder,cacheCid,"TS512_MB3.MidMips");
        Check(File.ReadAllBytes(path).SequenceEqual(cacheBytes)&&File.ReadAllText(path+".cid")==cacheCid,
            "native tag, bytes and CID preserved");
        Check(!File.Exists(Path.Combine(folder,cacheCid+".MidMips")),"CID does not replace native basename");
        string report=File.ReadAllText(Path.Combine(UnityEngine.Application.persistentDataPath,"ModsData","BridgeBuilder","material-footprints",owner+".tsv"));
        Check(report.Contains("MaterialFiles\t4") && report.Contains("MaterialBytes\t"+(surfaceBytes.Length+cacheBytes.Length+64))
            && report.Contains(path),"material footprint includes native cache bytes and sidecar");
        Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&written==0,
            "cache snapshot idempotent: "+error);
        BridgePreparedMaterials.Assets[cacheCid]=cache;
        BridgePreparedMaterials.MidMips[surfaceCid]=new[]{cacheCid};
        AssetDatabase.global.Assets.Remove(cache);
        Check(BridgeDependencyPersistence.Save(otherOwner,new[]{surfaceCid},out count,out error,out written)&&written==4,
            "common prepared source gives each owner independent native cache: "+error);
        BridgePreparedMaterials.Assets.Clear(); BridgePreparedMaterials.MidMips.Clear();
        string localSurface=Path.Combine(folder,surfaceCid+".Surface");
        AssetDatabase.global.Assets.Clear();
        AssetDatabase.global.Assets.Add(Asset(surfaceCid,localSurface,".Surface",surfaceBytes,false));
        AssetDatabase.global.Assets.Add(forbidden);
        Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&count==2&&written==0,
            "unregistered current-owner cache discovered without prepared source map: "+error);
        File.Delete(path+".cid");
        // A registered original can repair a missing sidecar; discovery must not hide the missing file.
        AssetDatabase.global.Assets.Add(cache);
        Check(BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&written==1,
            "missing cache sidecar repaired additively: "+error);
        var changed=cacheBytes.ToArray(); changed[^1]^=1; File.WriteAllBytes(path,changed);
        Check(!BridgeDependencyPersistence.Save(owner,new[]{surfaceCid},out count,out error)&&error.Contains("Conflicting"),
            "same-CID cache conflict rejected");
        Check(File.ReadAllBytes(path).SequenceEqual(changed),"conflict never overwrites owner cache");
        BridgePreparedMaterials.MidMips[surfaceCid]=new[]{new string('7',32)};
        AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(surface);
        Check(!BridgeDependencyPersistence.Save("b"+Guid.NewGuid(),new[]{surfaceCid},out count,out error)&&error.Contains("Dependency unavailable"),
            "missing declared native cache fails persistence");
        BridgePreparedMaterials.MidMips.Clear();
    }
    private static byte[] Surface()
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write((ushort)1);writer.Write(0);writer.Write(false);
        for(int i=0;i<6;i++)writer.Write((byte)0);
        return stream.ToArray();
    }
    private static byte[] Cache(string surface)
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        foreach(int n in new[]{512,3,64,1,1,1,1,512,512,1,0,3})writer.Write(n);
        writer.Write(false);writer.Write(0);
        for(int i=0;i<32;i+=2)writer.Write((byte)(Convert.ToInt32(surface[i].ToString(),16)|Convert.ToInt32(surface[i+1].ToString(),16)<<4));
        writer.Write(1);writer.Write((short)0);writer.Write(0);
        for(int level=0;level<3;level++){writer.Write(level);writer.Write(1);}
        writer.Write(3);writer.Write(stream.Position);writer.Write(new byte[528*528*5/4*3]);
        return stream.ToArray();
    }
    private static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
}
