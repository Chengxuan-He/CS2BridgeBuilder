using BridgeBuilder.Runtime;
using System.Text;

var root = Path.Combine(Path.GetTempPath(), "BridgeBuilder-Metadata-" + Guid.NewGuid());
UnityEngine.Application.persistentDataPath = root;
Directory.CreateDirectory(Path.Combine(root, "ImportedData"));
var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
string Fixture(string id) => "{\"name\":\"" + id + "\",\"components\":{\"$rcontent\":[{\"$type\":\"1|BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder\",\"m_BaseConstructionCost\":368}]},\"dependency\":$fstrref:\"CID:12345678901234567890123456789012\",\"m_Sections\":null}";
string Install(string id, string text)
{
    var dir = Path.Combine(root, "ImportedData", id); Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, id + ".Prefab"); File.WriteAllText(path, text); File.WriteAllText(path + ".cid", "unchanged"); return path;
}
var a = BridgeAssetInfo.NewPrefabName();
var native = new BridgeAssetInfo(a, "原名 🌉", "Road", "Track", "Style", "", false);
var encoded = BridgeAssetMetadata.Encode(native);
var nativeText = "{\"name\":\"" + a + "\",\"components\":{\"$rcontent\":[{\"$type\":\"1|Game.Prefabs.UIObject, Game\",\"name\":\"" + encoded + "\",\"m_Icon\":\"\"}]},\"dependency\":$fstrref:\"CID:12345678901234567890123456789012\"}";
Check(BridgeAssetMetadata.TryRead(nativeText, out var nativeRead) && nativeRead.DisplayName == native.DisplayName,
    "native UI component carries legacy metadata without a mod type");
native.DisplayName = "新名";
Check(BridgeAssetMetadata.Rewrite(nativeText, native, out var rewrittenNative)
    && BridgeAssetMetadata.TryRead(rewrittenNative, out nativeRead) && nativeRead.DisplayName == "新名"
    && nativeRead.PrefabName == a && rewrittenNative.Contains("CID:12345678901234567890123456789012"),
    "native rename preserves UUID and referenced CID");
var b = BridgeAssetInfo.NewPrefabName();
var path = Install(a, Fixture(a));
Check(BridgeAssetCatalog.Find(a)?.DisplayName == a, "legacy prefab without metadata remains discoverable");
var label = "桥梁\t\"名称\"\\轨道\n🌉";
Check(BridgeAssetCatalog.Rename(a, label), "unicode, quotes, slash and control characters persist");
Check(BridgeAssetCatalog.Find(a)?.DisplayName == label, "name survives disk reread");
Check(File.ReadAllText(path + ".cid") == "unchanged", "CID unchanged by rename");
Check(File.ReadAllText(path).Contains("\"m_Sections\":null") && File.ReadAllText(path).Contains("$fstrref:\"CID:12345678901234567890123456789012\""), "rename preserves damaged and external references verbatim");
var pending = new BridgeAssetInfo(b, "new bridge", "upper", "lower", "style", "2026-10-05T00:00:00Z", true);
Check(BridgeAssetCatalog.Begin(pending), "creation reserves only the in-memory identity");
Check(BridgeAssetMetadata.Rewrite(Fixture(b), pending, out var text), "metadata embeds before publication");
Install(b, text);
Check(BridgeAssetCatalog.Find(b) == null, "pending asset not shown as finished bridge");
Check(BridgeAssetCatalog.Commit(b), "completion saved into prefab");
BridgeAssetCatalog.ResetSession();
Check(BridgeAssetCatalog.Find(b)?.StyleId == "style", "recipe survives session reset");
Check(BridgeAssetMetadata.Write(Path.Combine(root,"ImportedData",b,b+".Prefab"), pending), "simulate incomplete creation");
BridgeAssetCatalog.ResetSession();
Check(BridgeAssetCatalog.ReadAll().Any(e => e.PrefabName == b && e.Pending), "interrupted creation detected from asset alone");
var c = BridgeAssetInfo.NewPrefabName(); Install(c, "broken Odin");
Check(BridgeAssetCatalog.Find(a) != null && BridgeAssetCatalog.Find(c) != null, "ownership does not depend on parsable metadata");
var original = File.ReadAllBytes(path);
using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
    Check(!BridgeAssetCatalog.Rename(a, "lost"), "locked asset fails safely");
Check(File.ReadAllBytes(path).SequenceEqual(original), "failed rename preserves original bytes");
foreach (var file in args)
{
    var source = File.ReadAllText(file);
    Check(BridgeAssetMetadata.TryRead(source, out var entry), "actual fixture read " + Path.GetFileName(file));
    entry.DisplayName = label;
    Check(BridgeAssetMetadata.Rewrite(source, entry, out var changed) && BridgeAssetMetadata.TryRead(changed, out var result) && result.DisplayName == label, "actual Odin fixture name round trip");
}
Console.WriteLine($"PASS {count} asset metadata checks. Scratch: {root}");

var legacy = BridgeAssetInfo.NewPrefabName();
var legacyPath = Install(legacy, "{\"name\":\"" + legacy + "\",\"components\":{\"$rcontent\":[]}}");
var legacyOriginal = File.ReadAllBytes(legacyPath);
var copyCalls = BridgeDependencyPersistence.Calls;
BridgeDependencyPersistence.WrittenFiles = 2;
Check(BridgeAssetMigration.Run(legacy, new[] { "seed" }, out var migrated, out var error) && migrated
    && BridgeDependencyPersistence.Calls == copyCalls + 1, "bridge without metadata reaches dependency migration");
Check(File.ReadAllBytes(legacyPath).SequenceEqual(legacyOriginal) && File.ReadAllText(legacyPath + ".cid") == "unchanged",
    "migration never rewrites root or CID and never invents metadata");
BridgeDependencyPersistence.WrittenFiles = 0;
Check(BridgeAssetMigration.Run(legacy, new[] { "seed" }, out migrated, out error) && !migrated,
    "repeat migration depends on missing copies, not version metadata");
BridgeDependencyPersistence.Fail = true;
Check(!BridgeAssetMigration.Run(legacy, new[] { "missing dependency" }, out migrated, out error)
    && error == "copy failed" && File.ReadAllBytes(legacyPath).SequenceEqual(legacyOriginal),
    "real dependency failures still propagate without changing bridge bytes");
Console.WriteLine($"PASS {count} metadata and migration checks");

for (var fields = 1; fields <= 7; fields++)
{
    var values = new[] { a, "Existing name", "Road", "Track", "Style", "", "0" }.Take(fields);
    var partial = BridgeAssetMetadata.Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        string.Join("\n", values.Select(v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v))))));
    Check(BridgeAssetMetadata.Decode(partial, out var recovered) && recovered.PrefabName == a
        && recovered.DisplayName == (fields > 1 ? "Existing name" : a), "partial metadata fields=" + fields);
    Check(BridgeAssetMetadata.Decode(BridgeAssetMetadata.Encode(recovered), out var completed)
        && completed.PrefabName == a && completed.CreatedUtc == "", "canonical metadata preserves unknown history=" + fields);
}
