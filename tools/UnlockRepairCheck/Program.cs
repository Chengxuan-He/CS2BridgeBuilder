using BridgeBuilder.Runtime;

var checks = 0;
void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); checks++; }
BridgeUnlockExpression Leaf(string name) => new() { Kind = 1, Identity = name };
var expression = new BridgeUnlockExpression { Flags = new byte[] { 1, 2, 2 },
    Children = new[] { Leaf("milestone"), Leaf("devA"), Leaf("devB") } };
Check(BridgeUnlockExpression.TryDecode(expression.Encode(), out var decoded), "round trip");
for (var mask = 0; mask < 8; mask++)
{
    bool? Lookup(string id) => (mask & (id == "milestone" ? 1 : id == "devA" ? 2 : 4)) != 0;
    Check(decoded.Evaluate(Lookup) == ((mask & 1) != 0 && (mask & 6) != 0), "AND / OR truth table");
}
Check(decoded.Evaluate(_ => null) == null, "missing dependency must defer");
Check(!BridgeUnlockExpression.TryDecode(expression.Encode() + "invalid", out _), "invalid encoded rule");
Check(new BridgeUnlockExpression().Evaluate(_ => null) == true, "empty native requirement group");
var uuid = "b7dff25d3-cf93-4466-9c26-d5aa3be39f3e";
// Recorded Odin layout and offending CID from the player's FileSystem.log. No player/private metadata.
var fixture = """
{
    "$id": 0,
    "$type": "0|Game.Prefabs.RoadPrefab, Game",
    "name": "ROOT",
    "components": {
        "$rcontent": [
            {
                "$id": 47,
                "$type": "26|Game.Prefabs.Unlockable, Game",
                "name": "Unlockable",
                "active": true,
                "m_RequireAll": {
                    "$id": 48,
                    "$type": "27|Game.Prefabs.PrefabBase[], Game",
                    "$rlength": 1,
                    "$rcontent": [
                        $fstrref:"CID:b98f0304f8b33d02f5b79adab26663a6"
                    ]
                },
                "m_RequireAny": {
                    "$id": 49,
                    "$type": 27,
                    "$rlength": 0,
                    "$rcontent": []
                },
                "m_IgnoreDependencies": true
            },
            { "$type": "28|BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder", "m_BaseConstructionCost": 184 },
            { "$type": 27, "$rcontent": [$fstrref:"UnityGUID:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"] }
        ]
    },
    "m_Sections": { "$rcontent": [$fstrref:"CID:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"] }
}
""".Replace("ROOT", uuid);
Check(BridgeUnlockMigration.Rewrite(fixture, out var after, out var error), error);
Check(!after.Contains("$fstrref:\"CID:b98f0304f8b33d02f5b79adab26663a6\""), "no unsafe unlock CID reference");
Check(after.Contains("Game.Prefabs.ManualUnlockable, Game"), "native self gate");
Check(after.Contains("\"$type\": \"27|Game.Prefabs.PrefabBase[], Game\""), "preserve later Odin type reuse");
Check(after.Substring(after.IndexOf("\"m_Sections\"")) == fixture.Substring(fixture.IndexOf("\"m_Sections\"")), "geometry references identical");
Check(BridgeUnlockMigration.Rewrite(after, out var again, out error) && again == after, "idempotent");
Check(!BridgeUnlockMigration.Rewrite(fixture.Replace("m_IgnoreDependencies\": true", "m_IgnoreDependencies\": false"), out _, out _), "unknown rule retained");
Check(!BridgeUnlockMigration.Rewrite(fixture.Replace("$fstrref:\"CID:b98f0304f8b33d02f5b79adab26663a6\"", "null"), out _, out _), "already missing identity retained");
Check(!BridgeUnlockMigration.Rewrite(fixture.Replace("\"m_Sections\":", "\"shared\": $iref:48,\n    \"m_Sections\":"), out _, out _), "shared array retained");
var root = Path.Combine(Path.GetTempPath(), "BBUnlockRepairCheck-" + Guid.NewGuid().ToString("N"));
// These are disposable test inputs only; leave their backups for inspection, never touch a game save.
var gameRoot = Path.Combine(root, new string('a', 110), new string('b', 110));
var directory = Path.Combine(gameRoot, "ImportedData", uuid);
Directory.CreateDirectory(directory);
var path = Path.Combine(directory, uuid + ".Prefab");
File.WriteAllText(path, fixture);
File.WriteAllText(path + ".cid", "cccccccccccccccccccccccccccccccc");
Check(path.Length > 300 && BridgeFileAccess.ReadText(path) == fixture, "extended path read");
Check(BridgeUnlockMigration.Run(gameRoot, false, out var count, out error) && count == 1 && File.ReadAllText(path) == fixture, "dry run");
using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    Check(!BridgeUnlockMigration.Run(gameRoot, true, out _, out _) && File.Exists(path), "access failure is not corruption");
Check(BridgeUnlockMigration.Run(gameRoot, true, out count, out error) && count == 1, error);
Check(File.ReadAllText(path) == after && Directory.GetFiles(directory, "*.bbunlockbackup").Length == 1, "atomic replace + backup");
Check(File.ReadAllText(path + ".cid") == "cccccccccccccccccccccccccccccccc", "CID unchanged");
Check(BridgeUnlockMigration.Run(gameRoot, true, out count, out error) && count == 0, "file migration idempotent");
if (args.Length > 0)
{
    var log = File.ReadAllText(args[0]);
    var marker = "Reading array went wrong. Data dump: Json: ";
    var offset = 0;
    var dumps = 0;
    while ((offset = log.IndexOf(marker, offset, StringComparison.Ordinal)) >= 0)
    {
        offset += marker.Length;
        var start = log.IndexOf('{', offset);
        var depth = 0; var quoted = false; var escape = false;
        var end = start;
        for (; end < log.Length; end++)
        {
            var c = log[end];
            if (quoted) { if (escape) escape = false; else if (c == '\\') escape = true; else if (c == '"') quoted = false; }
            else if (c == '"') quoted = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) { end++; break; }
        }
        var original = log.Substring(start, end - start);
        Check(BridgeUnlockMigration.Rewrite(original, out var repaired, out error) && original != repaired, "actual player Odin dump: " + error);
        Check(!repaired.Contains("$fstrref:\"CID:b98f0304f8b33d02f5b79adab26663a6\""), "actual offending unlock CID removed");
        Check(BridgeUnlockMigration.Rewrite(repaired, out var repeat, out error) && repeat == repaired, "actual dump idempotent");
        dumps++; offset = end;
    }
    Check(dumps >= 2, "both actual player decks examined");
}
Console.WriteLine($"Passed {checks} unlock/migration/access checks. Fixture backups: {root}");
