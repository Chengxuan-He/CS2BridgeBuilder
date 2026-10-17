using BridgeBuilder.Runtime;
using System.Text;

var sandbox = Path.Combine(Path.GetTempPath(), "BBLayout-" + Guid.NewGuid().ToString("N"));
var game = Path.Combine(sandbox, "game");
var backup = Path.Combine(sandbox, "backup");
const string owner = "b8a73af23-ea53-4b61-8e0b-12bae69355e2";
string PrefabPath(string name) => Path.Combine("ImportedData", name, name + ".Prefab");
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
    if (args.Length != 0)
    {
        var files = Directory.GetFiles(args[0], "*.Prefab", SearchOption.AllDirectories);
        Check(files.Length != 0, "real saved Prefab fixtures found");
        foreach (var file in files)
            Check(BridgeSerializedReferences.TryRead(File.ReadAllText(file), out var document)
                && !string.IsNullOrEmpty(document.Name)
                && (file.Contains("_Dependencies") || document.Name == Path.GetFileNameWithoutExtension(file)), "saved file parses: " + Path.GetFileName(file));
        var realGame = Path.Combine(sandbox, "real-game");
        foreach (var file in Directory.GetFiles(args[0], "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(args[0], file);
            var destination = Path.Combine(realGame, relative.StartsWith("BridgeBuilder" + Path.DirectorySeparatorChar)
                ? relative : Path.Combine("ImportedData", relative));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var realOwner = Path.GetFileNameWithoutExtension(files.First(f => Path.GetFileNameWithoutExtension(f).Length == 37));
        var beforeFiles = Directory.GetFiles(realGame,"*",SearchOption.AllDirectories)
            .ToDictionary(f => f, File.ReadAllBytes);
        Check(BridgeAssetLayout.Run(realGame, new[] { realOwner }, Path.Combine(sandbox,"real-backup"),
            PrefabPath, out var realChanges, out var realError) && realChanges.Count == 0,
            "real bridge layout is valid and needs no migration: " + realError);
        Check(beforeFiles.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value)), "real graph and CIDs remain byte-identical");
    }
    var source = Save(Path.Combine("ImportedData", "old-" + owner, "old-root.Prefab"), Doc(owner));
    var deckName = owner + " Lower Road";
    var deck = Save(Path.Combine("ImportedData", "old-" + owner, "nested", "old-deck.Prefab"), Doc(deckName));
    var geometry = Save(Path.Combine("ImportedData", "old-" + owner, owner + " Mesh.Geometry"), new byte[] { 0, 255, 17, 42 });
    var external = Save(Path.Combine("ImportedData", owner + "_Dependencies", new string('f', 32) + ".Prefab"), Doc("External original"));
    var other = Save(Path.Combine("ImportedData", "Other", "Other.Prefab"), Doc("Other"));
    var before = File.ReadAllBytes(source); var cid = File.ReadAllBytes(source + ".cid");
    var target = Path.Combine(game, PrefabPath(owner));
    Check(BridgeAssetLayout.Run(game, new[] { owner }, backup, PrefabPath, out var changed, out var error), error);
    Check(changed.SetEquals(new[] { owner }) && File.Exists(target) && !File.Exists(source), "root uses new-generation directory and filename");
    Check(File.ReadAllBytes(target).SequenceEqual(before) && File.ReadAllBytes(target + ".cid").SequenceEqual(cid), "root name/UUID, CID sidecar, version and payload byte-identical");
    Check(File.Exists(Path.Combine(game, PrefabPath(deckName))) && !File.Exists(deck), "nested deck uses the same layout");
    Check(File.ReadAllBytes(Path.Combine(game, "BridgeBuilder", owner + " Mesh.Geometry")).SequenceEqual(new byte[] {0,255,17,42}) && !File.Exists(geometry), "geometry moved without data changes");
    Check(File.Exists(external) && File.Exists(other), "CID snapshots and unrelated assets untouched");
    Check(File.ReadAllBytes(Path.Combine(backup, "Layout", "ImportedData", "old-" + owner, "old-root.Prefab")).SequenceEqual(before), "original bytes backed up outside active asset tree");
    Check(BridgeAssetLayout.Run(game, new[] { owner }, backup, PrefabPath, out changed, out error) && changed.Count == 0, "second pass is read-only and idempotent");
    source = Save(Path.Combine("ImportedData", "old-" + owner, "collision.Prefab"), Doc(owner));
    Check(!BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "collision"), PrefabPath, out _, out error)
        && error.Contains("already exists") && File.Exists(source) && File.ReadAllBytes(target).SequenceEqual(before), "collision never overwrites existing identity");
    File.Delete(source); File.Delete(source + ".cid");
    var missing = Save(Path.Combine("ImportedData", "old-" + owner, "missing.Prefab"), Doc(owner + " Missing"));
    File.Delete(missing + ".cid");
    Check(!BridgeAssetLayout.Run(game, new[] { owner }, Path.Combine(sandbox, "missing"), PrefabPath, out _, out error)
        && error.Contains("Missing CID") && File.Exists(missing), "missing sidecar cannot create a new identity");
    File.Delete(missing);
    // Lock a later file: an earlier move must be rolled back after the IO failure.
    var first = Save(Path.Combine("ImportedData", "old-" + owner, "first.Prefab"), Doc(owner + " First"));
    var locked = Save(Path.Combine("ImportedData", "old-" + owner, "second.Prefab"), Doc(owner + " Second"));
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
