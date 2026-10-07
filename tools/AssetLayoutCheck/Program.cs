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
