using BridgeBuilder.Runtime;
using System.Text;

// Actual file operations in an isolated test directory; never reads player data.
var root = Path.Combine(Path.GetTempPath(), "BridgeBuilder-Registry-" + Guid.NewGuid());
UnityEngine.Application.persistentDataPath = root;
var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
BridgeRegistration Entry(string id, bool pending = false) => new(id, "桥梁\tname", "upper", null, "Suspension", "2026-10-04T12:00:00Z", pending);
var a = BridgeRegistration.NewPrefabName();
var b = BridgeRegistration.NewPrefabName();
var file = BridgeRegistrationStore.FilePath;
try
{
    Check(BridgeRegistrationStore.TryLoad(out var empty) && empty.Count == 0, "missing registry is empty");
    Check(BridgeRegistrationStore.Record(Entry(a)), "first atomic save");
    var first = File.ReadAllBytes(file);
    Check(BridgeRegistrationStore.Rename(a, "new name"), "replace existing registry");
    Check(File.ReadAllBytes(file + ".bak").SequenceEqual(first), "previous complete version backed up");
    var healthy = File.ReadAllBytes(file);
    var backup = File.ReadAllBytes(file + ".bak");
    using (var held = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
    {
        Check(!BridgeRegistrationStore.Record(Entry(b)), "locked read blocks record");
        Check(!BridgeRegistrationStore.Rename(a, "lost") && !BridgeRegistrationStore.Remove(a), "locked read blocks rename/delete");
    }
    Check(File.ReadAllBytes(file).SequenceEqual(healthy), "read failure preserves original bytes");
    using (var held = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        Check(!BridgeRegistrationStore.Record(Entry(b)), "replacement failure returns false");
    Check(File.ReadAllBytes(file).SequenceEqual(healthy)
        && File.ReadAllBytes(file + ".bak").SequenceEqual(backup), "replacement failure preserves both copies");
    Check(!Directory.GetFiles(Path.GetDirectoryName(file)!, "*.tmp").Any(), "failed save releases temporary file");
    foreach (var invalid in new[] { "", "wrong header\n", Encoding.UTF8.GetString(healthy) + "broken\trow\n",
        Encoding.UTF8.GetString(healthy) + File.ReadAllLines(file)[1] + "\n",
        Encoding.UTF8.GetString(healthy).Replace("committed", "unknown") })
    {
        File.WriteAllText(file, invalid);
        var damaged = File.ReadAllBytes(file);
        Check(!BridgeRegistrationStore.TryLoad(out var partial) && partial.Count == 0, "malformed input returns no partial records");
        Check(!BridgeRegistrationStore.Record(Entry(b)) && !BridgeRegistrationStore.Rename(a, "lost")
            && !BridgeRegistrationStore.Remove(a), "malformed input blocks every mutation");
        Check(File.ReadAllBytes(file).SequenceEqual(damaged), "malformed original preserved");
    }
    File.WriteAllBytes(file, healthy);
    var legacy = File.ReadAllLines(file).Select(line => line.Substring(0, line.LastIndexOf('\t'))).ToArray();
    File.WriteAllLines(file, legacy);
    Check(BridgeRegistrationStore.Find(a)?.RegistrationName == "new name", "six-column legacy file readable");
    Check(BridgeRegistrationStore.Begin(Entry(b, true)), "creation reserves UUID durably");
    Check(!BridgeRegistrationStore.Begin(Entry(b, true)), "cannot reuse reserved UUID");
    Check(BridgeRegistrationStore.Find(b) == null && BridgeRegistrationStore.Load().Count == 1, "pending creation hidden from management");
    Check(BridgeRegistrationStore.CanRegister(b), "active creation may enter native initialization");
    BridgeRegistrationStore.ResetSession();
    Check(!BridgeRegistrationStore.CanRegister(b) && BridgeRegistrationStore.CanRegister(a), "crash leaves pending graph blocked at next boot");
    Check(BridgeRegistrationStore.TryLoad(out var all) && all.Single(e => e.PrefabName == b).Pending,
        "boot recovery receives pending owner");
    Check(BridgeRegistrationStore.Remove(b), "retired pending record can be removed");
    var c = BridgeRegistration.NewPrefabName();
    Check(BridgeRegistrationStore.Begin(Entry(c, true)), "reserve next creation");
    BridgeRegistrationStore.CanRegister(a);
    Check(BridgeRegistrationStore.Commit(c) && BridgeRegistrationStore.CanRegister(c), "successful commit clears session gate");
    Check(!BridgeRegistrationStore.Commit(c) && !BridgeRegistrationStore.Commit(b), "commit requires an active pending creation");
    BridgeRegistrationStore.ResetSession();
    Check(BridgeRegistrationStore.Find(c) != null && BridgeRegistrationStore.CanRegister(c), "committed bridge survives restart");
    File.Delete(file);
    Check(!BridgeRegistrationStore.Record(Entry(b)), "missing primary with surviving backup fails closed");
    Console.WriteLine($"PASS {count} registry checks");
}
finally
{
    // root is a fresh, fixed-prefix test directory under GetTempPath, never player data.
    if (Directory.Exists(root)) Directory.Delete(root, true);
}
