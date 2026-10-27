using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

// Read saved runtime traversal reports. Never create, modify or deserialize native assets.
internal static class ExistingOwnerAudit
{
    internal static void Run(string gameRoot,string assembly,string output)
    {
        gameRoot=Path.GetFullPath(gameRoot);assembly=Path.GetFullPath(assembly);output=Path.GetFullPath(output);
        if(output.StartsWith(gameRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Audit output must be outside the game data directory.");
        using var stream=File.OpenRead(assembly);using var pe=new PEReader(stream);
        var metadata=pe.GetMetadataReader();string build=metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString();
        var imported=Path.Combine(gameRoot,"ImportedData");
        var allFiles=Directory.Exists(imported)?Directory.GetFiles(imported,"*",SearchOption.AllDirectories):Array.Empty<string>();
        var ownerPattern=new Regex(@"\Ab[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\z",RegexOptions.IgnoreCase);
        var owners=(Directory.Exists(imported)?Directory.GetDirectories(imported):Array.Empty<string>())
            .Where(p=>ownerPattern.IsMatch(Path.GetFileName(p))).Select(path=>
            {
                var files=Directory.GetFiles(path,"*",SearchOption.AllDirectories);
                return new{owner=Path.GetFileName(path),files=files.Length,
                    newestFileUtc=files.Length==0?(DateTime?)null:files.Max(File.GetLastWriteTimeUtc),
                    rootPrefabs=files.Where(f=>Path.GetFileName(f).Equals("Bridge.Prefab",StringComparison.OrdinalIgnoreCase)).ToArray()};
            }).ToArray();
        var reportFolder=Path.Combine(gameRoot,"ModsData","BridgeBuilder","material-footprints");
        var reports=new List<object>();int complete=0;
        foreach(var path in Directory.Exists(reportFolder)?Directory.GetFiles(reportFolder,"*.tsv"):Array.Empty<string>())
        {
            var issues=new List<string>();var rows=File.ReadAllLines(path).Select(line=>line.Split('\t')).ToArray();
            string Value(string key)=>rows.SingleOrDefault(r=>r.Length==2&&r[0]==key)?[1]??"";
            string owner=Value("Owner");string ownerRoot=Path.Combine(imported,owner);
            if(!ownerPattern.IsMatch(owner))issues.Add("Invalid owner identity");
            if(Value("Schema")!="BridgeBuilder/SavedOwnerMaterialFootprint/1")issues.Add("Unsupported runtime report schema");
            if(Value("GeneratorBuild")!=build)issues.Add("Generator MVID differs from supplied build");
            if(Value("CompleteMaterialGraph")!="True")issues.Add("Runtime traversal did not prove complete material closure");
            if(!DateTime.TryParse(Value("UTC"),out var savedAt))issues.Add("Invalid save timestamp");
            if(string.IsNullOrWhiteSpace(Value("MaterialRootCIDs")))issues.Add("No observed material roots");
            issues.AddRange(rows.Where(r=>r.Length>1&&r[0]=="Issue").Select(r=>string.Join(" ",r.Skip(1))));
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var verified=new List<object>();long bytes=0;
            foreach(var row in rows.Where(r=>r.Length>0&&r[0]=="File"))
            {
                if(row.Length!=4 || !long.TryParse(row[2],out long length)){issues.Add("Malformed physical file row");continue;}
                string file=Path.GetFullPath(row[3]);
                if(!ownerPattern.IsMatch(owner)||!file.StartsWith(ownerRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                {issues.Add("Physical file outside exact owner: "+file);continue;}
                if(!seen.Add(file)){issues.Add("Duplicate physical file row: "+file);continue;}
                if(!File.Exists(file)){issues.Add("Missing physical file: "+file);continue;}
                var info=new FileInfo(file);
                if(info.Length!=length)issues.Add("Saved size differs: "+file);
                if(info.LastWriteTimeUtc>savedAt.ToUniversalTime())issues.Add("File modified after runtime measurement: "+file);
                string cidPath=file.EndsWith(".cid",StringComparison.OrdinalIgnoreCase)?file:file+".cid";
                if(!File.Exists(cidPath)||!File.ReadAllText(cidPath).Trim().Equals(row[1],StringComparison.OrdinalIgnoreCase))issues.Add("CID sidecar mismatch: "+file);
                bytes=checked(bytes+info.Length);
                using var content=File.OpenRead(file);verified.Add(new{path=file,bytes=info.Length,sha256=Convert.ToHexString(SHA256.HashData(content))});
            }
            if(!long.TryParse(Value("MaterialBytes"),out long declared)||declared!=bytes)issues.Add("Material byte total differs");
            if(!int.TryParse(Value("MaterialFiles"),out int declaredFiles)||declaredFiles!=verified.Count)issues.Add("Material file total differs");
            bool valid=issues.Count==0;if(valid)complete++;
            reports.Add(new{path,owner,generatorBuild=Value("GeneratorBuild"),completeCurrentGraph=valid,
                materialBytes=valid?(long?)bytes:null,observedListedBytes=bytes,budget=valid?(bytes<=10000000?"PASS":"OVER_BUDGET"):"UNVERIFIED",issues,files=verified});
        }
        using var dll=File.OpenRead(assembly);
        var result=new{utc=DateTime.UtcNow,gameRoot,assembly,assemblySha256=Convert.ToHexString(SHA256.HashData(dll)),generatorMvid=build,
            assemblyWriteUtc=File.GetLastWriteTimeUtc(assembly),importedFileCount=allFiles.Length,
            importedNewestUtc=allFiles.Length==0?(DateTime?)null:allFiles.Max(File.GetLastWriteTimeUtc),owners,
            reportFolderExists=Directory.Exists(reportFolder),verifiedCurrentOwners=complete,reports,
            ownerFootprint=complete==0?"UNMEASURED: no complete current-build saved traversal report":"See per-owner runtime closure reports",
            scope="Read-only inventory and physical recheck of runtime serialized-save closure evidence. Does not reparse native graph or establish visual/gameplay acceptance."};
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Read-only audit: {owners.Length} owner directories; {reports.Count} saved reports; {complete} verified current-build closures.");
    }
}
