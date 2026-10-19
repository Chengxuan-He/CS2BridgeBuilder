using BridgeBuilder.Runtime;
using System.Text;

var sandbox = Path.Combine(Path.GetTempPath(), "BBLayout-" + Guid.NewGuid().ToString("N"));
var game = Path.Combine(sandbox, "game");
var backup = Path.Combine(sandbox, "backup");
const string owner = "b8a73af23-ea53-4b61-8e0b-12bae69355e2";
string PrefabPath(string name) => Path.Combine(BridgeAssetInfo.OwnedDirectory(owner, "Prefabs"), (name == owner ? "Bridge" : BridgeAssetInfo.ShortName(name, owner)) + ".Prefab");
string Save(string relative, byte[] bytes)
{
    var path = Path.Combine(game, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, bytes);
    File.WriteAllBytes(path + ".cid", Encoding.UTF8.GetBytes(new string('a', 32) + "\r\n"));
    return path;
}
byte[] Doc(string name) => Encoding.UTF8.GetBytes("{\"$type\":\"0|Game.Prefabs.RoadPrefab, Game\",\"name\":\"" + name + "\",\"version\":17}");
void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
try
{
    var odin = """
        {"$id":0,"$type":"0|Game.Prefabs.RenderPrefab, Game","name":"moving",
         "position":{"$type":"1|UnityEngine.Vector3, UnityEngine.CoreModule",0,6.62,-81.3},
         "rotation":{"$type":"2|UnityEngine.Quaternion, UnityEngine.CoreModule",0,0,0,1},
         "anonymous":{"$type":"3|Example", "literal", {"nested":true}},
         "reference":$fstrref:"CID:0123456789abcdef0123456789abcdef"}
        """;
    Check(BridgeSerializedReferences.TryRead(odin, out var indexed) && indexed.Name == "moving",
        "Odin typed anonymous values retain following fields and references");
    Check(!BridgeSerializedReferences.TryRead(odin[..^2], out _), "truncated Odin still fails");
    var source = Save(Path.Combine("ImportedData", "old-" + owner, owner + ".Prefab"), Doc(owner));
    var deckName = owner + " Lower Road";
    var deck = Save(Path.Combine("ImportedData", "old-" + owner, "nested", deckName + ".Prefab"), Doc(deckName));
    var geometry = Save(Path.Combine("ImportedData", "old-" + owner, owner + " Mesh.Geometry"), new byte[] { 0, 255, 17, 42 });
    var external = Save(Path.Combine("ImportedData", owner + "_Dependencies", new string('f', 32) + ".Prefab"), Doc("External original"));
    var other = Save(Path.Combine("ImportedData", "Other", "Other.Prefab"), Doc("Other"));
    var before = File.ReadAllBytes(source); var cid = File.ReadAllBytes(source + ".cid");
    var target = Path.Combine(game, PrefabPath(owner));
    Check(BridgeAssetLayout.Run(game, new[] { owner }, backup, PrefabPath, out var changed, out var error), error);
    Check(changed.SetEquals(new[] { owner }) && File.Exists(target) && !File.Exists(source), "root uses new-generation directory and filename");
    Check(File.ReadAllBytes(target).SequenceEqual(before) && File.ReadAllBytes(target + ".cid").SequenceEqual(cid), "root name/UUID, CID sidecar, version and payload byte-identical");
    Check(File.Exists(Path.Combine(game, PrefabPath(deckName))) && !File.Exists(deck), "nested deck uses the same layout");
    Check(File.ReadAllBytes(Path.Combine(game, "ImportedData", owner, "Geometry", "Mesh.Geometry")).SequenceEqual(new byte[] {0,255,17,42}) && !File.Exists(geometry), "geometry moved without data changes");
    Check(File.Exists(Path.Combine(game,"ImportedData",owner,"Dependencies",Path.GetFileName(external))) && !File.Exists(external) && File.Exists(other), "CID snapshots moved unchanged; unrelated assets untouched");
    Check(File.ReadAllBytes(Path.Combine(backup, "Layout", "ImportedData", "old-" + owner, owner + ".Prefab")).SequenceEqual(before), "original bytes backed up outside active asset tree");
    Check(BridgeAssetLayout.Run(game, new[] { owner }, backup, PrefabPath, out changed, out error) && changed.Count == 0, "second pass is read-only and idempotent");
    var lodName = owner + " Fixed Base 0 LOD1";
    var oldLod = Save(Path.Combine("ImportedData", owner + " Fixed Base 0", lodName + ".Prefab"), Doc(lodName));
    var lodBytes = File.ReadAllBytes(oldLod);
    Check(BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "lod-backup"), PrefabPath, out changed, out error), error);
    Check(File.ReadAllBytes(Path.Combine(game, PrefabPath(lodName))).SequenceEqual(lodBytes)
        && !Directory.Exists(Path.GetDirectoryName(oldLod)), "grouped LOD moves to flat Prefabs unchanged");
    var duplicate = Save(Path.Combine("ImportedData", "old-" + owner, owner + ".Prefab"), Doc(owner));
    Check(BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox,"duplicate"), PrefabPath, out changed,out error)
        && !File.Exists(duplicate) && File.ReadAllBytes(target).SequenceEqual(before), "identical payload and CID deduplicate after backup");
    source = Save(Path.Combine("ImportedData", "old-" + owner, owner + ".Prefab"), Doc(owner));
    File.WriteAllText(source + ".cid", new string('b',32));
    Check(!BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "collision"), PrefabPath, out changed, out error)
        && error.Contains("already exists") && changed.Count == 0 && File.Exists(source)
        && File.ReadAllBytes(target).SequenceEqual(before), "CID conflict preserves both assets");
    File.Delete(source); File.Delete(source + ".cid");
    var vt = Save(Path.Combine("StreamingData~","VT",owner + " Road Surface.VTSurface"), new byte[]{1,2,3});
    var mip = Save(Path.Combine("BridgeBuilder",owner + " Road Surface","MidMipMapCache_128_3.MidMips"),new byte[]{4,5,6});
    var surface = Save(Path.Combine("BridgeBuilder",owner + " Road Surface.Surface"),new byte[]{7,8,9});
    var texture = Save(Path.Combine("BridgeBuilder",owner + " Road Surface.Texture"),new byte[]{9,8,7});
    var locale = Save(Path.Combine("ImportedData",owner,owner + "-en-US.loc"),new byte[]{3,2,1});
    var unknown = Save(Path.Combine("BridgeBuilder",owner + " Unknown.data"),new byte[]{5});
    Check(BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"vt"),PrefabPath,out changed,out error),error);
    Check(File.Exists(Path.Combine(game,"ImportedData",owner,"Appearance","VT","Road Surface.VTSurface"))
        && File.Exists(Path.Combine(game,"ImportedData",owner,"Appearance","MidMips","MidMipMapCache_128_3.MidMips"))
        && File.Exists(Path.Combine(game,"ImportedData",owner,"Appearance","Road Surface.Surface"))
        && File.Exists(Path.Combine(game,"ImportedData",owner,"Appearance","Road Surface.Texture"))
        && File.Exists(Path.Combine(game,"ImportedData",owner,"Locales","en-US.loc"))
        && File.Exists(unknown), "VT, mid-mip tag filename, appearance and locales migrate; unknown files retained");
    Check(BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"vt"),PrefabPath,out changed,out error) && changed.Count==0,
        "all categories idempotent");
    Check(BridgeAssetInfo.IsDependencyPath(Path.Combine("ImportedData",owner,"Dependencies","x.Prefab"),owner)
        && BridgeAssetInfo.IsDependencyPath(Path.Combine("ImportedData",owner+"_Dependencies","x.Prefab"),owner),
        "old and new dependency provenance recognized");
    var embeddedName = "GoldenGateDouble-38_5-"+owner;
    var embedded = Save(Path.Combine("ImportedData",owner,"Prefabs",embeddedName+".Prefab"),Doc(embeddedName));
    var embeddedBytes=File.ReadAllBytes(embedded);var embeddedCid=File.ReadAllBytes(embedded+".cid");
    Check(BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"embedded"),PrefabPath,out changed,out error),error);
    var embeddedTarget=Path.Combine(game,"ImportedData",owner,"Prefabs","GoldenGateDouble-38_5.Prefab");
    Check(!File.Exists(embedded)&&File.ReadAllBytes(embeddedTarget).SequenceEqual(embeddedBytes)
        &&File.ReadAllBytes(embeddedTarget+".cid").SequenceEqual(embeddedCid),"UUID suffix removed in existing new-layout folder; serialized name and CID preserved");
    Check(BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"embedded"),PrefabPath,out changed,out error)&&changed.Count==0,"suffix migration is idempotent");
    var competing=Save(Path.Combine("ImportedData","old-"+owner,"GoldenGateDouble-38_5"+owner+".Prefab"),Doc("GoldenGateDouble-38_5"+owner));
    Check(!BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"embedded-conflict"),PrefabPath,out changed,out error)
        &&error.Contains("already exists")&&File.Exists(competing)&&File.ReadAllBytes(embeddedTarget).SequenceEqual(embeddedBytes),"UUID removal collision preserves both differing payloads");
    File.Delete(competing);File.Delete(competing+".cid");
    var diskName = "Disk-"+owner+"_LOD2";
    var diskSource=Save(Path.Combine("ImportedData","old-"+owner,diskName+".Prefab"),Doc(owner+" Different internal name"));
    Check(BridgeAssetLayout.Run(game,new[]{owner},Path.Combine(sandbox,"disk-name"),PrefabPath,out changed,out error)
        &&File.Exists(Path.Combine(game,"ImportedData",owner,"Prefabs","DiskLOD2.Prefab")),"migration transforms disk filename, not serialized object name");
    var missing = Save(Path.Combine("ImportedData", "old-" + owner, "missing.Prefab"), Doc(owner + " Missing"));
    File.Delete(missing + ".cid");
    Check(!BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "missing"), PrefabPath, out _, out error)
        && error.Contains("Missing CID") && File.Exists(missing), "missing sidecar cannot create a new identity");
    File.Delete(missing);
    // Lock a later file: an earlier move must be rolled back after the IO failure.
    var first = Save(Path.Combine("ImportedData", "old-" + owner, owner+" First.Prefab"), Doc(owner + " First"));
    var locked = Save(Path.Combine("ImportedData", "old-" + owner, owner+" Second.Prefab"), Doc(owner + " Second"));
    using (var stream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.Read))
        Check(!BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "locked"), PrefabPath, out changed, out error)
            && changed.Count == 0 && File.Exists(first) && File.Exists(first + ".cid") && File.Exists(locked)
            && !File.Exists(Path.Combine(game, PrefabPath(owner + " First"))), "failed move restores earlier moves without losing payload or CID");
}
finally
{
    var full = Path.GetFullPath(sandbox);
    if (full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(full).StartsWith("BBLayout-")) Directory.Delete(full, true);
}
