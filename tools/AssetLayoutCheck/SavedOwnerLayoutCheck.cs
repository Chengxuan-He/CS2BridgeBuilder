using BridgeBuilder.Runtime;
using System.Security.Cryptography;
using System.Text.Json;

internal static class SavedOwnerLayoutCheck
{
    internal static void Run(string game, string output)
    {
        // Discovery/indexing here is fixture preparation, outside the production migration route.
        // Never mutate the live game tree: each UUID is copied into a unique isolated directory.
        string original=Path.GetFullPath(game), target=Path.GetFullPath(output);
        if(target.StartsWith(original,StringComparison.OrdinalIgnoreCase)||Directory.Exists(target))throw new Exception("Fresh isolated output required");
        Directory.CreateDirectory(target);
        var records=new List<object>(); int total=0,failed=0;
        var owners=Directory.GetDirectories(Path.Combine(original,"ImportedData")).Select(Path.GetFileName)
            .Where(o=>o!=null&&BridgeAssetInfo.IsPrefabName(o)&&File.Exists(Path.Combine(original,"ImportedData",o,"Prefabs","Bridge.Prefab"))).ToArray();
        foreach(string owner in owners!)
        {
            string from=Path.Combine(original,"ImportedData",owner), into=Path.Combine(target,"ImportedData",owner);
            foreach(string path in BridgeFileAccess.EnumerateFiles(from))
            {
                string dest=Path.Combine(into,Path.GetRelativePath(from,path));Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(dest)!));
                File.Copy(BridgeFileAccess.Native(path),BridgeFileAccess.Native(dest));
            }
            string Digest(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(BridgeFileAccess.Native(path))));
            var before=BridgeFileAccess.EnumerateFiles(into).ToDictionary(p=>p,Digest,StringComparer.OrdinalIgnoreCase);
            var index=before.Keys.Where(p=>p.EndsWith(".cid",StringComparison.OrdinalIgnoreCase))
                .GroupBy(p=>BridgeFileAccess.ReadText(p).Trim(),StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g=>g.Key,g=>g.First()[..^4],StringComparer.OrdinalIgnoreCase);
            string root=Path.Combine(into,"Prefabs","Bridge.Prefab");
            string PrefabPath(string name)=>Path.Combine(BridgeAssetInfo.OwnedDirectory(owner,"Prefabs"),
                (name==owner?"Bridge":BridgeAssetInfo.FileStem(name,owner))+".Prefab");
            var paths=new HashSet<string>(before.Keys,StringComparer.OrdinalIgnoreCase);
            // The runtime combines filesystem backslashes with native asset/helper forward slashes.
            paths.UnionWith(before.Keys.Select(p=>p.Replace('\\','/')));
            int lookups=0;
            string backup=Path.Combine(Path.GetDirectoryName(target)!,Path.GetFileName(target)+"-backups",owner);
            bool names=BridgeLegacyNames.Run(target,new[]{owner},backup,out var renamed,out string error,
                PrefabPath,new[]{root},cid=>{lookups++;return index.GetValueOrDefault(cid);},paths);
            if(!names)throw new Exception("Name migration: "+owner+": "+error);
            bool layout=BridgeAssetLayout.Run(target,new[]{owner},backup,PrefabPath,out var changed,out error,paths);
            bool identical=before.All(p=>File.Exists(BridgeFileAccess.Native(p.Key))&&Digest(p.Key)==p.Value);
            if(!layout)failed++;
            else
            {
                if(renamed.Count!=0||changed.Count!=0||!identical)throw new Exception("Healthy actual owner changed: "+owner);
                if(!BridgeAssetLayout.Run(target,new[]{owner},backup,PrefabPath,out changed,out error,paths)||changed.Count!=0)
                    throw new Exception("Second pass not idempotent: "+owner+": "+error);
            }
            records.Add(new{owner,files=before.Count,names,layout,identical,lookups,error});total+=before.Count;
            Console.WriteLine($"OWNER {owner} files={before.Count} nameLookups={lookups} layout={layout} exactOriginals={identical} error={error}");
        }
        File.WriteAllText(Path.Combine(target,"results.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"SUMMARY owners={owners.Length} nativeFilesAndMetadata={total} failed={failed}");
        if(failed!=0)throw new Exception("Actual owner name-to-layout pipeline failed: "+failed);
    }
}
