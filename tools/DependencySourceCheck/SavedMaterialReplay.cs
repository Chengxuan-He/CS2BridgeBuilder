using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

// Replay the material portion of each recorded, complete game footprint. Fixture
// enumeration happens here, before entering the real creation resolver. The
// original owner files are opened read-only. All save outputs use the test root.
internal static class SavedMaterialReplay
{
    internal static bool Active;
    internal static void Run(string gameRoot, string catalogPath)
    {
        Active = true;
        var catalog = System.Text.Json.JsonDocument.Parse(File.ReadAllText(catalogPath)).RootElement
            .EnumerateArray().ToDictionary(x=>x.GetProperty("s").GetString()!,x=>x.GetProperty("c").GetString()!);
        var imported = Path.Combine(gameRoot,"ImportedData");
        var reports = Path.Combine(gameRoot,"ModsData","BridgeBuilder","material-footprints");
        int owners=0, files=0, missingReports=0;
        long bytes=0;
        foreach (string folder in Directory.GetDirectories(imported).Order())
        {
            string owner=Path.GetFileName(folder);
            if (!BridgeAssetInfo.IsPrefabName(owner)) continue;
            string report=Path.Combine(reports, owner+".tsv");
            if (!File.Exists(report)) { missingReports++; continue; }
            var rows=File.ReadAllLines(report).Select(line=>line.Split('\t')).ToArray();
            if (!rows.Any(r=>r.Length>1 && r[0]=="CompleteMaterialGraph" && r[1]=="True")) { missingReports++;continue; }
            var roots=rows.Single(r=>r[0]=="MaterialRootCIDs")[1].Split(',',StringSplitOptions.RemoveEmptyEntries);
            var input=rows.Where(r=>r[0]=="File"&&!r[3].EndsWith(".cid")).ToArray();
            AssetDatabase.global.Assets.Clear(); BridgePreparedMaterials.Assets.Clear(); BridgePreparedMaterials.MidMips.Clear();
            var recorded=new Dictionary<string,AssetData>();
            var expected=new Dictionary<string,string>();
            var ownedEdges=new Dictionary<string,List<AssetData>>();
            foreach(var row in input)
            {
                string cid=row[1],file=row[3]; string relative=Path.GetRelativePath(folder,file);
                if(relative.StartsWith(".."))throw new Exception("Foreign footprint input: "+file);
                bool dependency=relative.StartsWith("Dependencies"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
                string path=dependency?"ReplayPackage/"+cid:Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",owner,relative);
                if(!dependency)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.Copy(file,path,true);File.Copy(file+".cid",path+".cid",true);
                }
                if(File.ReadAllText(file+".cid").Trim()!=cid)throw new Exception("Saved input CID differs: "+file);
                var a=new AssetData(cid,()=>File.OpenRead(file));
                a.Metadata.path=path; a.Metadata.packaged=dependency;a.Metadata.extension=Path.GetExtension(file);
                a.Metadata.fileName=Path.GetFileNameWithoutExtension(file);
                recorded.Add(cid,a);AssetDatabase.global.Assets.Add(a);expected.Add(relative,file);
                if(a.Metadata.extension.Equals(".MidMips",StringComparison.OrdinalIgnoreCase))
                {
                    foreach(string surface in BridgeMidMipReferences.Read(File.ReadAllBytes(file)))
                    {
                        if(!ownedEdges.TryGetValue(surface,out var list))ownedEdges.Add(surface,list=new());
                        list.Add(a);
                    }
                }
            }
            foreach(var row in rows.Where(r=>r[0]=="Builtin"))
            {
                var a=new AssetData(row[1],()=>throw new Exception("Builtin opened")){isBuiltin=true};a.Metadata.extension=row[2];
                AssetDatabase.global.Assets.Add(a);
            }
            foreach(var pair in catalog)
                if(recorded.ContainsKey(pair.Key)&&recorded.TryGetValue(pair.Value,out var cache))
                {
                    BridgePreparedMaterials.MidMips[pair.Key]=new[]{pair.Value};BridgePreparedMaterials.Assets[pair.Value]=cache;
                }
            var handoff=new BridgeCreationDependencies(asset=>ownedEdges.TryGetValue(asset.id.guid.ToString(),out var list)?list:Array.Empty<AssetData>());
            foreach(var pair in ownedEdges.Where(p=>recorded.ContainsKey(p.Key) && !BridgePreparedMaterials.MidMips.ContainsKey(p.Key)))
                handoff.Material(recorded[pair.Key],pair.Value);
            AssetDatabase.global.ForbidEnumeration = true;
            int before=AssetDatabase.global.Scans;
            if(!BridgeDependencyPersistence.Save(owner,roots,handoff,out _,out string error,out _))throw new Exception(owner+": "+error);
            if(before!=AssetDatabase.global.Scans || !BridgeBuilder.Mod.Log.LastSave.Contains("globalIndexBuilds=0, midMipIndexBuilds=0") ||
                !BridgeBuilder.Mod.Log.LastSave.Contains("midMipCacheReads=0, midMipBytesRead=0")) throw new Exception("Creation enumerated unrelated assets");
            foreach(var item in expected)
            {
                string dest=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",owner,item.Key);
                foreach(string ext in new[]{"",".cid"})
                {
                    byte[] actual=File.ReadAllBytes(dest+ext);
                    if(!actual.SequenceEqual(File.ReadAllBytes(item.Value+ext)))throw new Exception("Saved bytes differ: "+dest+ext);
                    bytes+=actual.Length;files++;
                }
            }
            var output=Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",owner);
            int outputFiles=Directory.Exists(output)?Directory.GetFiles(output,"*",SearchOption.AllDirectories).Length:0;
            if(outputFiles!=expected.Count*2+1)throw new Exception("Unexpected saved output "+owner);
            // A fresh self-check after restart has no generation handles or prepared in-memory map.
            // Native MidMips registrations are deliberately absent: only owner-local forward edges may resolve them.
            BridgePreparedMaterials.Assets.Clear(); BridgePreparedMaterials.MidMips.Clear();
            AssetDatabase.global.Assets.RemoveAll(a => a.Metadata.extension == ".MidMips");
            var restarted = new BridgeCreationDependencies(_ => throw new Exception("Restart tried reverse/native discovery despite recorded edges"));
            restarted.ReadRelations(owner);
            if(!BridgeDependencyPersistence.Save(owner,roots,restarted,out _,out error,out int restartWrites) || restartWrites!=0)
                throw new Exception("Self-check restart: "+owner+": "+error);
            if(before!=AssetDatabase.global.Scans || !BridgeBuilder.Mod.Log.LastSave.Contains("globalIndexBuilds=0, midMipIndexBuilds=0"))
                throw new Exception("Self-check constructed a full index");
            foreach(var item in expected)
                foreach(string ext in new[]{"", ".cid"})
                    if(!File.ReadAllBytes(Path.Combine(output,item.Key)+ext).SequenceEqual(File.ReadAllBytes(item.Value+ext)))
                        throw new Exception("Restart changed saved bytes");
            owners++;
            Console.WriteLine("PASS saved material replay "+owner+" files="+outputFiles+" "+BridgeBuilder.Mod.Log.LastSave);
        }
        AssetDatabase.global.ForbidEnumeration = false;
        Console.WriteLine($"PASS {owners} recorded complete material graphs, {files} exact payloads/sidecars, {bytes} bytes, {missingReports} owners without complete reports.");
    }
}
