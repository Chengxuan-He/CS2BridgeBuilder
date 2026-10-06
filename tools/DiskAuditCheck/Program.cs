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
BridgeDiskAudit Inventory(string gameRoot, string owner) => BridgeDiskAudit.ForMemoryFailures(gameRoot,
    new Dictionary<string,string> { [owner] = "confirmed invalid required reference" });
var audit = Inventory(root, a);
Check(audit.Complete && audit.FileOwners.Count == 2 && !audit.FileOwners.ContainsKey(Prefab(healthy)),
    "inventory is limited to the invalid owner");
var recovery = root + "-Recovery";
Check(!audit.RetireFiles(new HashSet<string> { healthy }, recovery, out _), "healthy files cannot be retired");
File.AppendAllText(Prefab(a), "\nchanged");
Check(!audit.RetireFiles(new HashSet<string> { a }, recovery, out _), "changed files cannot be retired");
audit = Inventory(root, a);
var original = File.ReadAllBytes(Prefab(a));
var originalCid = File.ReadAllBytes(Prefab(a) + ".cid");
void RestoreFixture()
{
    Directory.CreateDirectory(Path.GetDirectoryName(Prefab(a))!);
    File.WriteAllBytes(Prefab(a), original);
    File.WriteAllBytes(Prefab(a) + ".cid", originalCid);
    audit = Inventory(root, a);
}
Check(audit.RetireFiles(new HashSet<string> { a }, Path.Combine(root, "ImportedData", "Recovery"), out var backupWarning)
    && backupWarning.Length > 0, "unsafe backup rejected, confirmed source still cleared");
Check(!File.Exists(Prefab(a)) && !Directory.Exists(Path.Combine(root, "ImportedData", "Recovery")),
    "damaged assets are not moved back into discovery roots");
RestoreFixture();
Check(audit.RetireFiles(new HashSet<string> { a }, Path.Combine(root, "ModsData", "Recovery"), out backupWarning)
    && backupWarning.Length > 0 && !File.Exists(Prefab(a)), "ModsData backup rejected with source cleanup");
RestoreFixture();
var blockedRecovery = root + "-BlockedRecovery";
Directory.CreateDirectory(Path.Combine(blockedRecovery, a));
File.WriteAllText(Path.Combine(blockedRecovery, a, a + ".Prefab.cid"), "existing backup");
Check(audit.RetireFiles(new HashSet<string> { a }, blockedRecovery, out backupWarning) && backupWarning.Length == 0,
    "directory collision selects independent backup without dropping files");
Check(!File.Exists(Prefab(a)) && !File.Exists(Prefab(a) + ".cid"), "partial backup never rolls damaged files back");
Check(File.ReadAllText(Path.Combine(blockedRecovery, a, a + ".Prefab.cid")) == "existing backup", "existing backup never overwritten");
Check(File.ReadAllBytes(Path.Combine(Directory.GetDirectories(blockedRecovery, a + "__*").Single(), a + ".Prefab")).SequenceEqual(original), "complete colliding directory preserved under unique outer name");
RestoreFixture();
var unavailableBackup = root + "-BackupIsFile";
File.WriteAllText(unavailableBackup, "not a directory");
Check(audit.RetireFiles(new HashSet<string> { a }, unavailableBackup, out backupWarning)
    && backupWarning.Length > 0 && !File.Exists(Prefab(a)) && !File.Exists(Prefab(a) + ".cid"),
    "backup directory creation failure still clears confirmed damaged files");
RestoreFixture();
Check(audit.RetireFiles(new HashSet<string> { a }, recovery, out var error), "confirmed files retired: " + error);
Check(!File.Exists(Prefab(a)) && File.Exists(Prefab(b)) && File.Exists(Prefab(healthy)), "retirement leaves healthy files untouched");
Check(File.ReadAllBytes(Path.Combine(recovery, a, a + ".Prefab")).SequenceEqual(original)
    && File.ReadAllBytes(Path.Combine(recovery, a, a + ".Prefab.cid")).SequenceEqual(originalCid),
    "recovery preserves original prefab and CID names and bytes");

// Crash between geometry save, prefab save and final asset metadata commit.
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
audit = Inventory(root, pendingOwner);
Check(audit.Complete && audit.Failures.ContainsKey(pendingOwner), "pending journal is explicit interruption evidence");
Check(audit.FileOwners.ContainsKey(orphanGeometry) && audit.FileOwners.ContainsKey(partialCid),
    "recovery includes geometry without prefab and partial CID files");
var interruptedRecovery = root + "-InterruptedRecovery";
Check(audit.RetireFiles(pendingSet, interruptedRecovery, out error), "interrupted graph retired: " + error);
Check(!File.Exists(orphanGeometry) && !File.Exists(partialCid) && File.Exists(sharedGeometry)
    && File.Exists(Prefab(healthy)), "only interrupted owner removed");
Check(File.ReadAllText(Path.Combine(interruptedRecovery, "BridgeBuilder", orphanName + ".Geometry"))
    == "private geometry bytes", "interrupted geometry backed up byte-for-byte");
audit = Inventory(root, pendingOwner);
Check(!audit.Complete, "absent files cannot report another successful retirement");

var longRoot = Path.Combine(root, new string('a', 110), new string('b', 110));
var longDirectory = Path.Combine(longRoot, "ImportedData", healthy);
Directory.CreateDirectory(longDirectory);
var longFile = Path.Combine(longDirectory, healthy + ".Prefab");
File.WriteAllText(longFile, "{\n    \"name\": \"" + healthy + "\"\n}");
audit = Inventory(longRoot, healthy);
Check(longFile.Length > 300 && audit.Complete && audit.Failures.ContainsKey(healthy) && audit.FileOwners.ContainsKey(longFile), "long path audited, not skipped");
// Memory verdict inventories only its owner, even if healthy files cannot be read.
using (var locked = new FileStream(longFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    var invalid = "b" + Guid.NewGuid();
    var dir = Path.Combine(longRoot, "ImportedData", invalid);
    Directory.CreateDirectory(dir);
    var brokenPath = Path.Combine(dir, invalid + ".Prefab");
    File.WriteAllText(brokenPath, "opaque bytes: no file integrity parsing");
    var copies = Path.Combine(longRoot, "ImportedData", invalid + "_Dependencies");
    Directory.CreateDirectory(copies);
    File.WriteAllText(Path.Combine(copies, "copy.Prefab"), "shared dependency");
    var memoryAudit = BridgeDiskAudit.ForMemoryFailures(longRoot, new Dictionary<string,string> { [invalid] = "null required reference" });
    Check(memoryAudit.Complete && memoryAudit.FileOwners.Count == 2 && !memoryAudit.FileOwners.ContainsKey(longFile),
        "memory verdict inventories only invalid owner; locked healthy prefab is never read");
    Check(memoryAudit.RetireFiles(new HashSet<string> { invalid }, root + "-MemoryRecovery", out _)
        && !File.Exists(brokenPath) && File.Exists(longFile), "memory retirement preserves healthy owner");
    Check(!BridgeDiskAudit.ForMemoryFailures(longRoot, new Dictionary<string,string> { [invalid] = "null" }).Complete,
        "missing local files cannot report successful retirement");
}
Console.WriteLine($"{checks} total checks including memory retirement passed.");

var treeOwner = "b" + Guid.NewGuid();
var tree = Path.Combine(root,"ImportedData",treeOwner);
Directory.CreateDirectory(Path.Combine(tree,"nested","empty"));
File.WriteAllText(Path.Combine(tree,treeOwner+".Prefab"),"broken");
File.WriteAllText(Path.Combine(tree,"nested","extra.bin"),"additional contents");
var verdict = new Dictionary<string,string> { [treeOwner] = "required null" };
var treeAudit = BridgeDiskAudit.ForMemoryFailures(root, verdict);
var treeBackup = root + "-TreeBackup"; Directory.CreateDirectory(treeBackup);
File.WriteAllText(Path.Combine(treeBackup,treeOwner),"existing outer file");
Check(treeAudit.RetireFiles(verdict.Keys.ToHashSet(),treeBackup,out error),"recursive directory retirement");
var movedTree = Directory.GetDirectories(treeBackup,treeOwner+"__*").Single();
Check(!Directory.Exists(tree) && Directory.Exists(Path.Combine(movedTree,"nested","empty"))
    && File.ReadAllText(Path.Combine(movedTree,"nested","extra.bin")) == "additional contents", "whole tree and empty children preserved, source directory removed");
Check(File.ReadAllText(Path.Combine(treeBackup,treeOwner)) == "existing outer file", "outer file collision does not overwrite prior backup");
Directory.CreateDirectory(tree); File.WriteAllText(Path.Combine(tree,treeOwner+".Prefab"),"broken");
treeAudit = BridgeDiskAudit.ForMemoryFailures(root,verdict);
File.WriteAllText(Path.Combine(tree,"new.bin"),"arrived after validation");
Check(!treeAudit.RetireFiles(verdict.Keys.ToHashSet(),treeBackup,out error) && File.Exists(Path.Combine(tree,"new.bin")), "new file after snapshot prevents recursive deletion");
Console.WriteLine($"{checks} checks including recursive directories passed.");
