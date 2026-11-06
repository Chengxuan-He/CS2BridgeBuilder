using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using System.Diagnostics;

// Run only against an isolated, restored copy of the game directory.
internal static class OwnerSnapshotReplay
{
    internal static void Run(string root)
    {
        if (!File.Exists(Path.Combine(root, ".isolated-replay"))) throw new Exception("Isolated fixture required.");
        UnityEngine.Application.persistentDataPath = Path.GetFullPath(root);
        SavedMaterialReplay.Active = true;
        int owners = 0;
        var elapsed = Stopwatch.StartNew();
        foreach (var dir in Directory.GetDirectories(Path.Combine(root, "ImportedData")))
        {
            string owner = Path.GetFileName(dir);
            var report = File.ReadAllLines(Path.Combine(root,"ModsData","BridgeBuilder","material-footprints",owner+".tsv"));
            var roots = report.Single(x => x.StartsWith("MaterialRootCIDs\t")).Split('\t')[1].Split(',',StringSplitOptions.RemoveEmptyEntries);
            AssetDatabase.global.Assets.Clear();
            BridgePreparedMaterials.Assets.Clear();
            BridgePreparedMaterials.MidMips.Clear();
            foreach(string verified in File.ReadAllLines(Path.Combine(root,"native-builtin.tsv")))
            {
                var v=verified.Split('\t');
                var native=new AssetData(v[0],()=>throw new Exception("Builtin asset opened")) {isBuiltin=true};
                native.Metadata.extension=v[1]; AssetDatabase.global.Assets.Add(native);
            }
            foreach (var line in report.Where(x => x.StartsWith("Builtin\t")))
            {
                var v=line.Split('\t');
                var builtin=new AssetData(v[1],()=>throw new Exception("Builtin payload opened")) { isBuiltin=true };
                builtin.Metadata.extension=v[2];
                AssetDatabase.global.Assets.Add(builtin);
            }
            // Owner-authored Appearance resources are normally registered by their unique IDs.
            // Deliberately omit the CID-named Dependencies folders from all databases.
            var registeredPrefabSeeds = new List<string>();
            foreach (var sidecar in Directory.GetFiles(dir,"*.cid",SearchOption.AllDirectories))
            {
                string payload=sidecar.Substring(0,sidecar.Length-4);
                if (BridgeAssetInfo.IsDependencyPath(payload,owner)) continue;
                var native=new AssetData(File.ReadAllText(sidecar).Trim(),()=>File.OpenRead(payload));
                native.Metadata.path=payload;
                native.Metadata.packaged=false;
                native.Metadata.extension=Path.GetExtension(payload);
                AssetDatabase.global.Assets.Add(native);
                if(native.Metadata.extension==".Prefab") registeredPrefabSeeds.Add(native.id.guid.ToString());
            }
            var request=new BridgeCreationDependencies(_=>throw new Exception("Unexpected cache search."));
            request.ReadRelations(owner);
            AssetDatabase.global.ForbidEnumeration=true;
            if(!BridgeDependencyPersistence.Save(owner,roots,request,out _,out var error,out var writes)||writes!=0)
                throw new Exception(owner+" "+error+" writes="+writes);
            var request2=new BridgeCreationDependencies(_=>throw new Exception("Unexpected cache search."));
            request2.ReadRelations(owner);
            if(!BridgeDependencyPersistence.Save(owner,registeredPrefabSeeds,request2,out _,out error,out writes)||writes!=0)
                throw new Exception("Full registered prefab graph: "+owner+" "+error+" writes="+writes);
            Console.WriteLine("PASS material and registered Prefab closure: "+owner);
            owners++;
        }
        if(owners==0)throw new Exception("No owner graphs tested.");
        Console.WriteLine($"PASS {owners} actual material and registered Prefab graphs; no installed mod providers, no database index, no new dependency files; {elapsed.ElapsedMilliseconds}ms");
    }
}
