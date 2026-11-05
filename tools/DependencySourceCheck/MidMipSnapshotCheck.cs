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
            var asset=extension==".MidMips" ? new MidMipCacheAsset(cid,()=>new MemoryStream(bytes)) : new AssetData(cid,()=>new MemoryStream(bytes));
            asset.Metadata.path=path; asset.Metadata.extension=extension; asset.Metadata.packaged=packaged;
            return asset;
        }
        var surface=Asset(surfaceCid,"Package/Surface.Surface",".Surface",surfaceBytes);
        var cache=Asset(cacheCid,"Package/UK Pack.cok",".MidMips",cacheBytes);
        cache.Metadata.fileName="StreamingData~/TS512_MB3";
        Check(BridgeDependencyPersistence.CacheFileName(cache.Metadata.fileName,".MidMips",cache.Metadata.path)=="TS512_MB3.MidMips", "packaged cache uses native entry filename, not container path");
        Check(BridgeDependencyPersistence.CacheFileName("TS512_MB3",".MidMips","folder/TS512_MB3.MidMips")=="TS512_MB3.MidMips", "filesystem cache retains native filename");
        FixturePersistence.NativeEdges[surfaceCid] = new[] { cache };
        var unrelatedCache=Asset(new string('a',32),"Package/Other.cok",".MidMips",Cache(new string('b',32)));
        unrelatedCache.Metadata.fileName="unrelated-legacy-name";
        var forbidden=new MidMipCacheAsset(cacheCid,()=>throw new Exception("Foreign cache read"));
        forbidden.Metadata.extension=".MidMips";
        forbidden.Metadata.path=Path.Combine(imported,"b"+Guid.NewGuid(),"Dependencies",cacheCid,"TS512_MB3.MidMips");
        AssetDatabase.global.Assets.Clear(); AssetDatabase.global.Assets.AddRange(new AssetData[]{surface,forbidden,cache,unrelatedCache});
        var unrelated=new AssetData(new string('8',32),()=>throw new Exception("Unrelated transient opened"));
        unrelated.Metadata.extension=null!;
        AssetDatabase.global.Assets.Add(unrelated);
        Check(FixturePersistence.Save(owner,new[]{surfaceCid},out var count,out var error,out var written)
            && count==2 && written==4,"native forward cache copied: "+error);
        TimingCheck.CheckSummary(true, resolves:2, globalBuilds:0, mipBuilds:0, preflight:2, verify:2,
            payloadBytes:surfaceBytes.Length + cacheBytes.Length);
        string folder=BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,owner);
        string path=Path.Combine(folder,cacheCid,"TS512_MB3.MidMips");
        Check(File.ReadAllBytes(path).SequenceEqual(cacheBytes)&&File.ReadAllText(path+".cid")==cacheCid,
            "native tag, bytes and CID preserved");
        Check(!File.Exists(Path.Combine(folder,cacheCid+".MidMips")),"CID does not replace native basename");
        string newOwner = "b" + Guid.NewGuid();
        var creation = new BridgeCreationDependencies(_ => throw new Exception("Unexpected native cache lookup"));
        creation.Material(surface, new[] { cache });
        Check(FixturePersistence.Save(newOwner,new[]{surfaceCid},creation,out var newCount,out error,out var newWritten)
            && newCount==2 && newWritten==4,"creation uses generated cache edge: "+error);
        TimingCheck.CheckSummary(true, resolves:2, globalBuilds:0, mipBuilds:0, preflight:2, verify:2,
            payloadBytes:surfaceBytes.Length+cacheBytes.Length);
        var destination = BridgeDependencyCopies.Folder(UnityEngine.Application.persistentDataPath,newOwner);
        foreach(var saved in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories))
            Check(File.ReadAllBytes(saved).SequenceEqual(File.ReadAllBytes(Path.Combine(destination,Path.GetRelativePath(folder,saved)))),
                "generated/native persisted identical "+Path.GetRelativePath(folder,saved));
        try { creation.Material(surface, Array.Empty<AssetData>()); throw new Exception("Expected conflicting declared edges"); }
        catch(InvalidDataException e) when(e.Message.Contains("Conflicting")) {}
        var preparedGraph = new BridgeCreationDependencies(_ => throw new Exception("Unexpected prepared reverse lookup"));
        BridgePreparedMaterials.Assets[cacheCid] = cache;
        BridgePreparedMaterials.MidMips[surfaceCid] = new[]{cacheCid};
        Check(FixturePersistence.Save(newOwner,new[]{surfaceCid},preparedGraph,out newCount,out error,out newWritten)
            && newCount==2 && newWritten==0,"prepared cache edge is resolved directly: "+error);
        TimingCheck.CheckSummary(true, resolves:2, globalBuilds:0, mipBuilds:0, preflight:2, verify:0, payloadBytes:0);
        preparedGraph = new BridgeCreationDependencies(_ => throw new Exception("Unexpected prepared reverse lookup"));
        BridgePreparedMaterials.MidMips[surfaceCid] = new[]{new string('0',31)+"1"};
        Check(!FixturePersistence.Save(newOwner,new[]{surfaceCid},preparedGraph,out _,out error,out _)
            && error.Contains("Dependency unavailable"),"missing explicit prepared cache fails: "+error);
        BridgePreparedMaterials.MidMips.Clear();
        BridgePreparedMaterials.Assets.Clear();
        string report=File.ReadAllText(Path.Combine(UnityEngine.Application.persistentDataPath,"ModsData","BridgeBuilder","material-footprints",owner+".tsv"));
        Check(report.Contains("MaterialFiles\t4") && report.Contains("MaterialBytes\t"+(surfaceBytes.Length+cacheBytes.Length+64))
            && report.Contains(path),"material footprint includes native cache bytes and sidecar");
        Check(FixturePersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&written==0,
            "cache snapshot idempotent: "+error);
        TimingCheck.CheckSummary(true, resolves:2, globalBuilds:0, mipBuilds:0, preflight:2, verify:0, payloadBytes:0);
        BridgePreparedMaterials.Assets[cacheCid]=cache;
        BridgePreparedMaterials.MidMips[surfaceCid]=new[]{cacheCid};
        AssetDatabase.global.Assets.Remove(cache);
        Check(FixturePersistence.Save(otherOwner,new[]{surfaceCid},out count,out error,out written)&&written==4,
            "common prepared source gives each owner independent native cache: "+error);
        BridgePreparedMaterials.Assets.Clear(); BridgePreparedMaterials.MidMips.Clear();
        string localSurface=Path.Combine(folder,surfaceCid+".Surface");
        AssetDatabase.global.Assets.Clear();
        AssetDatabase.global.Assets.Add(Asset(surfaceCid,localSurface,".Surface",surfaceBytes,false));
        AssetDatabase.global.Assets.Add(forbidden);
        FixturePersistence.NativeEdges.Clear();
        FixturePersistence.ReadRelations = true;
        Check(FixturePersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&count==2&&written==0,
            "unregistered current-owner cache resolved from persisted forward relation: "+error);
        File.Delete(path+".cid");
        // A registered original can repair a missing sidecar; discovery must not hide the missing file.
        AssetDatabase.global.Assets.Add(cache);
        Check(FixturePersistence.Save(owner,new[]{surfaceCid},out count,out error,out written)&&written==1,
            "missing cache sidecar repaired additively: "+error);
        FixturePersistence.ReadRelations = false;
        FixturePersistence.NativeEdges[surfaceCid] = new[] { cache };
        var changed=cacheBytes.ToArray(); changed[^1]^=1; File.WriteAllBytes(path,changed);
        Check(!FixturePersistence.Save(owner,new[]{surfaceCid},out count,out error)&&error.Contains("Different contents for dependency CID"),
            "same-CID cache conflict rejected: "+error);
        Check(File.ReadAllBytes(path).SequenceEqual(changed),"conflict never overwrites owner cache");
        BridgePreparedMaterials.MidMips[surfaceCid]=new[]{new string('7',32)};
        AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(surface);
        Check(!FixturePersistence.Save("b"+Guid.NewGuid(),new[]{surfaceCid},out count,out error)&&error.Contains("Dependency unavailable"),
            "missing declared native cache fails persistence");
        BridgePreparedMaterials.MidMips.Clear();
        FixturePersistence.NativeEdges.Clear();
        var mismatched=Asset(new string('c',32),"Package/Any.cok",".MidMips",cacheBytes);
        mismatched.Metadata.fileName="StreamingData~/TS256_MB3";
        AssetDatabase.global.Assets.Clear();AssetDatabase.global.Assets.Add(mismatched);
        Check(!FixturePersistence.Save("b"+Guid.NewGuid(),new[]{new string('c',32)},out count,out error)
            &&error.Contains("filename/header mismatch"),"direct cache seed validates actual tag against header");
    }
    internal static byte[] Surface()
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write((ushort)1);writer.Write(0);writer.Write(false);
        for(int i=0;i<6;i++)writer.Write((byte)0);
        return stream.ToArray();
    }
    internal static byte[] Cache(string surface)
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
