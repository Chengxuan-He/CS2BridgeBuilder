using BridgeBuilder.Runtime;
using System.Text;
using System.Text.RegularExpressions;

// File migration only. No game process, live prefabs or mesh generation.
var sandbox = Path.Combine(Path.GetTempPath(), "BBLegacy-" + Guid.NewGuid().ToString("N"));
var game = Path.Combine(sandbox, "game");
var backup = Path.Combine(sandbox, "backup");
var ownerA = "b8a73af23-ea53-4b61-8e0b-12bae69355e2";
var ownerB = "b22222222-2222-2222-2222-222222222222";
var sectionCid = new string('a', 32);
var pieceCid = new string('b', 32);
var geometryCid = new string('c', 32);
var materialCid = new string('d', 32);
var section = args.Length == 0 ? "5-Lane Suspension Bridge 9" : args[0];
var pieceName = args.Length > 1 ? args[1] : section + " Piece";
string Expected(string name, string owner) => name.Insert(name.IndexOf(' ') is var index && index >= 0 ? index : name.Length, "-" + owner);
string Doc(string name, string type, params string[] refs) =>
    "{\"$type\":\"0|Game.Prefabs." + type + ", Game\",\"name\":\"" + name + "\",\"refs\":["
    + string.Join(",", refs.Select(c => "$fstrref:\"CID:" + c + "\"")) + "]}";
string Save(string name, string cid, string text)
{
    var path = Path.Combine(game, "ImportedData", name, name + ".Prefab");
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, text); File.WriteAllText(path + ".cid", cid); return path;
}
void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
// SDK path escaping is supplied by PrefabAssetWriter in the game.
bool Run(string root, IEnumerable<string> owners, string copies, out HashSet<string> changed, out string error) =>
    BridgeLegacyNames.Run(root, owners, copies, out changed, out error,
        name => Path.Combine("ImportedData", name, name + ".Prefab"));
try
{
    var a = Save(ownerA, new string('1', 32), Doc(ownerA, "RoadPrefab", sectionCid));
    var b = Save(ownerB, new string('2', 32), Doc(ownerB, "RoadPrefab", sectionCid));
    var old = Save(section, sectionCid, Doc(section, "NetSectionPrefab", pieceCid));
    var piece = Save(pieceName, pieceCid, Doc(pieceName, "NetPiecePrefab", geometryCid, materialCid));
    var material = Save("OtherModMaterial", materialCid, Doc("OtherModMaterial", "MaterialPrefab"));
    var g = Path.Combine(game, "BridgeBuilder", pieceName + ".Geometry");
    Directory.CreateDirectory(Path.GetDirectoryName(g)!);
    File.WriteAllBytes(g, new byte[] { 0, 255, 17, 42 }); File.WriteAllText(g + ".cid", geometryCid);
    var unrelated = Save("Unrelated Bridge 3", new string('e', 32), Doc("Unrelated Bridge 3", "NetSectionPrefab"));
    Check(Run(game, new[] { ownerA }, backup, out var changed, out var error), error);
    Check(changed.SetEquals(new[] { ownerA }) && File.Exists(old), "migrate A; preserve old components still referenced by B");
    var newA = BridgeDependencyCopies.References(File.ReadAllBytes(a)).Single();
    Check(newA != sectionCid && File.ReadAllText(b).Contains(sectionCid), "private CID and unchanged other bridge");
    var renamedPiece = Path.Combine(game, "ImportedData", Expected(pieceName, ownerA), Expected(pieceName, ownerA) + ".Prefab");
    Check(File.ReadAllText(renamedPiece).Contains(materialCid), "preserve external material reference");
    Check(File.ReadAllBytes(Path.Combine(game, "BridgeBuilder", Expected(pieceName, ownerA) + ".Geometry")).SequenceEqual(new byte[] {0,255,17,42}), "geometry byte preservation");
    Check(Run(game, new[] { ownerB }, Path.Combine(sandbox, "backupB"), out changed, out error), error);
    Check(!File.Exists(old) && !File.Exists(piece) && !File.Exists(g), "retire old files after last bridge migrates");
    Check(File.Exists(Path.Combine(sandbox, "backupB", "LegacyNames", "ImportedData", section, section + ".Prefab")), "old files backed up");
    Check(BridgeDependencyCopies.References(File.ReadAllBytes(b)).Single() != newA, "shared legacy source gets distinct per-bridge CID");
    Check(File.Exists(unrelated) && File.Exists(material), "unrelated assets preserved");
    var before = File.ReadAllBytes(a);
    Check(Run(game, new[] { ownerA, ownerB }, Path.Combine(sandbox, "again"), out changed, out error)
        && changed.Count == 0 && before.SequenceEqual(File.ReadAllBytes(a)), "idempotent second run");
    var ownerC = "b33333333-3333-3333-3333-333333333333";
    var c = Save(ownerC, new string('3', 32), Doc(ownerC, "RoadPrefab", sectionCid));
    var deps = Path.Combine(game, "ImportedData", ownerC + "_Dependencies");
    Directory.CreateDirectory(deps);
    File.WriteAllText(Path.Combine(deps, sectionCid + ".Prefab"), Doc(section, "NetSectionPrefab", pieceCid));
    File.WriteAllText(Path.Combine(deps, sectionCid + ".Prefab.cid"), sectionCid);
    File.WriteAllText(Path.Combine(deps, pieceCid + ".Prefab"), Doc(pieceName, "NetPiecePrefab", geometryCid));
    File.WriteAllText(Path.Combine(deps, pieceCid + ".Prefab.cid"), pieceCid);
    File.WriteAllBytes(Path.Combine(deps, geometryCid + ".Geometry"), new byte[] { 0, 255, 17, 42 });
    File.WriteAllText(Path.Combine(deps, geometryCid + ".Geometry.cid"), geometryCid);
    Check(Run(game, new[] { ownerC }, Path.Combine(sandbox, "backupC"), out changed, out error)
        && changed.Contains(ownerC) && !File.ReadAllText(c).Contains(sectionCid), "migrate CID-named snapshots without original files");
    Check(File.ReadAllBytes(Path.Combine(game, "BridgeBuilder", Expected(pieceName + " Geometry", ownerC) + ".Geometry"))
        .SequenceEqual(new byte[] { 0, 255, 17, 42 }), "snapshot-only geometry gets an owned name without changing bytes");
    var ownerD = "b44444444-4444-4444-4444-444444444444";
    var goodCid = new string('8', 32); var badCid = new string('9', 32);
    var leafCid = new string('f', 32);
    Save(ownerD, new string('4', 32), Doc(ownerD, "RoadPrefab", goodCid));
    var good = Save(ownerD + "-Correct", goodCid, Doc(ownerD + "-Correct", "NetSectionPrefab", badCid, leafCid));
    var bad = Save("Cable without UUID", badCid, Doc("Cable without UUID", "NetPiecePrefab"));
    var leaf = Save(ownerD + "-Leaf", leafCid, Doc(ownerD + "-Leaf", "NetPiecePrefab"));
    var leafBefore = File.ReadAllBytes(leaf);
    Check(Run(game, new[] { ownerD }, Path.Combine(sandbox, "backupD"), out changed, out error), error);
    Check(File.ReadAllText(good).Contains(ownerD + "-Correct") && File.ReadAllText(good + ".cid") == goodCid
        && !File.ReadAllText(good).Contains(badCid) && !File.Exists(bad), "correctly named parent keeps name/CID while unnamed child migrates");
    Check(leafBefore.SequenceEqual(File.ReadAllBytes(leaf)) && File.ReadAllText(leaf + ".cid") == leafCid,
        "correctly named leaf remains byte-identical");
    // Destination collision must fail before touching the bridge or legacy source.
    File.WriteAllText(a, Doc(ownerA, "RoadPrefab", sectionCid));
    old = Save(section, sectionCid, Doc(section, "NetSectionPrefab"));
    before = File.ReadAllBytes(a);
    Check(!Run(game, new[] { ownerA }, Path.Combine(sandbox, "collision"), out _, out error)
        && error.Contains("destination exists") && before.SequenceEqual(File.ReadAllBytes(a)) && File.Exists(old), "collision preserves original files");
}
finally { Directory.Delete(sandbox, true); }
