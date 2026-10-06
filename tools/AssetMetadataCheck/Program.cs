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
Check(BridgeAssetCatalog.Find(a) != null && BridgeAssetCatalog.Find(c) == null, "malformed asset does not block healthy bridge");
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
var legacyPath = Install(legacy, Fixture(legacy));
var legacyOriginal = File.ReadAllBytes(legacyPath);
var migrationBackup = root + "-migration";
Check(BridgeAssetMigration.Run(legacy, new[] { "seed" }, migrationBackup, out var migrated, out var error) && migrated, "healthy migration commits version");
Check(File.ReadAllBytes(Path.Combine(migrationBackup,"Migration",legacy,legacy+".Prefab")).SequenceEqual(legacyOriginal), "migration preserves original root backup");
Check(File.ReadAllText(legacyPath+".cid") == "unchanged" && File.ReadAllText(legacyPath).Contains("$fstrref:\"CID:12345678901234567890123456789012\""), "migration leaves CID and graph references unchanged");
var afterMigration = File.ReadAllBytes(legacyPath); var copyCalls = BridgeDependencyPersistence.Calls;
Check(BridgeAssetMigration.Run(legacy, new[] { "seed" }, migrationBackup, out migrated, out error) && !migrated
    && BridgeDependencyPersistence.Calls == copyCalls + 1 && File.ReadAllBytes(legacyPath).SequenceEqual(afterMigration), "migration repeat is byte-idempotent but rechecks dependencies");
BridgeDependencyPersistence.WrittenFiles = 2;
Check(BridgeAssetMigration.Run(legacy, new[] { "seed" }, migrationBackup, out migrated, out error) && migrated
    && File.ReadAllBytes(legacyPath).SequenceEqual(afterMigration) && File.ReadAllText(legacyPath+".cid") == "unchanged",
    "dependency-only repair reports change without rewriting bridge or CID");
BridgeDependencyPersistence.WrittenFiles = 0;
var failedId = BridgeAssetInfo.NewPrefabName(); var failedPath = Install(failedId, Fixture(failedId)); var failedOriginal = File.ReadAllBytes(failedPath);
BridgeDependencyPersistence.Fail = true;
Check(!BridgeAssetMigration.Run(failedId, new[] { "seed" }, migrationBackup, out migrated, out error)
    && !migrated && File.ReadAllBytes(failedPath).SequenceEqual(failedOriginal), "copy failure preserves original without version commit");
Console.WriteLine($"PASS {count} metadata and migration checks");

Check(!BridgeAssetMigration.Run(legacy, new[] { "missing geometry CID" }, migrationBackup, out migrated, out error),
    "already migrated bridge still reports subsequently missing dependencies");
