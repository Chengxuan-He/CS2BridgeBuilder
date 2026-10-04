using BridgeBuilder.Runtime;

var checks = 0;
void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    checks++;
    Console.WriteLine("PASS " + name);
}
var root = Path.Combine(Path.GetTempPath(), "BridgeBuilder-DiskAudit-" + Guid.NewGuid());
Directory.CreateDirectory(root);
var a = "b" + Guid.NewGuid();
var b = "b" + Guid.NewGuid();
var healthy = "b" + Guid.NewGuid();
var owners = new[] { a, b, healthy };
string Prefab(string id) => Path.Combine(root, "ImportedData", id, id + ".Prefab");
void Write(string id, string name, string cid)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Prefab(id))!);
    File.WriteAllText(Prefab(id), "{\n    \"name\": \"" + name + "\"\n}");
    File.WriteAllText(Prefab(id) + ".cid", cid);
}
Write(a, a, new string('1', 32));
Write(b, b, new string('2', 32));
Write(healthy, healthy, new string('3', 32));
Check(BridgeDiskAudit.Read(root, owners).Failures.Count == 0, "healthy graphs retained");
Write(b, a, new string('2', 32));
var audit = BridgeDiskAudit.Read(root, owners);
Check(audit.Failures.Keys.ToHashSet().SetEquals(new[] { a, b }), "name collision includes both disk owners");
Write(b, b, new string('1', 32));
audit = BridgeDiskAudit.Read(root, owners);
Check(audit.Failures.Keys.ToHashSet().SetEquals(new[] { a, b }), "CID collision includes both disk owners");
Write(b, b, new string('2', 32));
File.AppendAllText(Prefab(a), "\n\"m_GeometryAsset\": $fstrref:\"CID:" + new string('9', 32) + "\"");
audit = BridgeDiskAudit.Read(root, owners);
Check(audit.Failures.Keys.SequenceEqual(new[] { a }), "missing geometry invalidates only owning bridge");
Check(BridgeDiskAudit.Read(root, owners, _ => true).Failures.Count == 0, "available shared geometry is retained");
Check(!audit.OwnsName("r" + a.Substring(1), owners.ToHashSet()), "Road Builder identity excluded");
Check(!audit.OwnsName(a + "-extra" + Guid.NewGuid(), new HashSet<string> { b }), "unrelated UUID excluded");
var recovery = root + "-Recovery";
Check(!audit.RetireFiles(new HashSet<string> { healthy }, recovery, out _), "healthy files cannot be retired");
File.AppendAllText(Prefab(a), "\nchanged");
Check(!audit.RetireFiles(new HashSet<string> { a }, recovery, out _), "changed files cannot be retired");
audit = BridgeDiskAudit.Read(root, owners);
Check(!audit.RetireFiles(new HashSet<string> { a }, Path.Combine(root, "ImportedData", "Recovery"), out _),
    "recovery inside imported asset root rejected");
Check(File.Exists(Prefab(a)), "unsafe recovery location leaves source intact");
Check(!audit.RetireFiles(new HashSet<string> { a }, Path.Combine(root, "ModsData", "Recovery"), out _),
    "ModsData backup rejected because game scans the complete user root");
var original = File.ReadAllBytes(Prefab(a));
var originalCid = File.ReadAllBytes(Prefab(a) + ".cid");
var blockedRecovery = root + "-BlockedRecovery";
Directory.CreateDirectory(Path.Combine(blockedRecovery, a));
File.WriteAllText(Path.Combine(blockedRecovery, a, a + ".Prefab.cid"), "existing backup");
Check(!audit.RetireFiles(new HashSet<string> { a }, blockedRecovery, out _), "backup collision reports failure");
Check(File.ReadAllBytes(Prefab(a)).SequenceEqual(original)
    && File.ReadAllBytes(Prefab(a) + ".cid").SequenceEqual(originalCid), "partial move rolled back, originals intact");
Check(File.ReadAllText(Path.Combine(blockedRecovery, a, a + ".Prefab.cid")) == "existing backup", "existing backup never overwritten");
Check(audit.RetireFiles(new HashSet<string> { a }, recovery, out var error), "confirmed files retired: " + error);
Check(!File.Exists(Prefab(a)) && File.Exists(Prefab(b)) && File.Exists(Prefab(healthy)), "retirement leaves healthy files untouched");
Check(File.ReadAllBytes(Path.Combine(recovery, a, a + ".Prefab")).SequenceEqual(original)
    && File.ReadAllBytes(Path.Combine(recovery, a, a + ".Prefab.cid")).SequenceEqual(originalCid),
    "recovery preserves original prefab and CID names and bytes");

// Crash between geometry save, prefab save and final registry commit.
var pendingOwner = "b" + Guid.NewGuid();
var orphanName = "Suspension-40-" + pendingOwner + " Mesh";
var geometryDir = Path.Combine(root, "BridgeBuilder");
Directory.CreateDirectory(geometryDir);
var orphanGeometry = Path.Combine(geometryDir, orphanName + ".Geometry");
File.WriteAllText(orphanGeometry, "private geometry bytes");
File.WriteAllText(orphanGeometry + ".cid", new string('a', 32));
var sharedGeometry = Path.Combine(geometryDir, "shared.Geometry");
File.WriteAllText(sharedGeometry, "preserve");
var partialDir = Path.Combine(root, "ImportedData", pendingOwner);
Directory.CreateDirectory(partialDir);
var partialCid = Path.Combine(partialDir, pendingOwner + ".Prefab.cid");
File.WriteAllText(partialCid, new string('b', 32));
var pendingSet = new HashSet<string> { pendingOwner };
audit = BridgeDiskAudit.Read(root, owners.Append(pendingOwner), pending: pendingSet);
Check(audit.Complete && audit.Failures.ContainsKey(pendingOwner), "pending journal is explicit interruption evidence");
Check(audit.FileOwners.ContainsKey(orphanGeometry) && audit.FileOwners.ContainsKey(partialCid),
    "recovery includes geometry without prefab and partial CID files");
var interruptedRecovery = root + "-InterruptedRecovery";
Check(audit.RetireFiles(pendingSet, interruptedRecovery, out error), "interrupted graph retired: " + error);
Check(!File.Exists(orphanGeometry) && !File.Exists(partialCid) && File.Exists(sharedGeometry)
    && File.Exists(Prefab(healthy)), "only interrupted owner removed");
Check(File.ReadAllText(Path.Combine(interruptedRecovery, "BridgeBuilder", orphanName + ".Geometry"))
    == "private geometry bytes", "interrupted geometry backed up byte-for-byte");
audit = BridgeDiskAudit.Read(root, owners.Append(pendingOwner), pending: pendingSet);
Check(audit.Complete && audit.RetireFiles(pendingSet, root + "-Retry", out error),
    "restart after files retired but before registry removal is idempotent");

// Optional read-only regression against the player's preserved fixture bundle.
if (args.Length > 0)
{
    var cases = File.ReadAllLines(Path.Combine(args[0], "cases.tsv")).Skip(1)
        .Select(l => l.Split('\t')).ToArray();
    var ids = cases.Select(f => f[2]).ToArray();
    var baseline = BridgeDiskAudit.Read(Path.Combine(args[0], "Healthy"), ids);
    Check(baseline.Complete && baseline.Failures.Count == 0, "all real healthy fixture graphs pass");
    var broken = BridgeDiskAudit.Read(Path.Combine(args[0], "Corrupt"), ids);
    var expected = cases.Where(f => f[0] != "13" && !new[] { "9", "10", "11" }.Contains(f[0])).Select(f => f[2]).ToHashSet();
    foreach (var failure in broken.Failures) Console.WriteLine(failure);
    Check(broken.Complete && broken.Failures.Keys.ToHashSet().SetEquals(expected),
        "real explicit-null/empty/geometry/collision fixtures rejected, healthy control retained");
}
Console.WriteLine($"{checks} checks passed. Temporary test artifacts: {root}");
var longRoot = Path.Combine(root, new string('a', 110), new string('b', 110));
var longDirectory = Path.Combine(longRoot, "ImportedData", healthy);
Directory.CreateDirectory(longDirectory);
var longFile = Path.Combine(longDirectory, healthy + ".Prefab");
File.WriteAllText(longFile, "{\n    \"name\": \"" + healthy + "\"\n}");
audit = BridgeDiskAudit.Read(longRoot, new[] { healthy });
Check(longFile.Length > 300 && audit.Complete && audit.Failures.Count == 0 && audit.FileOwners.ContainsKey(longFile), "long path audited, not skipped");
using (var locked = new FileStream(longFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    audit = BridgeDiskAudit.Read(longRoot, new[] { healthy });
    Check(!audit.Complete && audit.Failures.Count == 0, "IO access failure incomplete, not corrupt");
    Check(!audit.RetireFiles(new HashSet<string> { healthy }, recovery, out _), "IO failure cannot retire files");
}
Console.WriteLine($"{checks} total disk checks passed.");
