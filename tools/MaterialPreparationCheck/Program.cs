using BridgeBuilder.Runtime;
using BridgeBuilder.Systems;
using System.Diagnostics;
using System.Security.Cryptography;

var root = Path.Combine(Path.GetTempPath(), "BridgeMaterialPreparation-" + Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(root, "Draw01", "Road"));
var texture = Path.Combine(root, "Draw01", "Road", "Payload.Texture");
var surface = Path.Combine(root, "Draw01", "Road", "NativeAsphalt.Surface");
File.WriteAllBytes(texture, Enumerable.Range(0, 2_000_000).Select(i => (byte)i).ToArray());
File.WriteAllBytes(surface, new byte[] { 1, 2, 3 });
File.WriteAllText(texture + ".cid", new string('1', 32));
File.WriteAllText(surface + ".cid", new string('2', 32));
void Check(bool value, string message) { if (!value) throw new Exception(message); }
List<BridgePreparedMaterialFiles.Input> Graph(BridgeMaterialFileSnapshot? snapshot) =>
    BridgePreparedMaterialFiles.Read(Path.Combine(root, "Draw01"), "Road", "NativeAsphalt.Surface",
        _ => Array.Empty<string>(), _ => throw new Exception("Unexpected built-in"), snapshot: snapshot);
try
{
    int caller = Environment.CurrentManagedThreadId;
    var sync = Graph(null);
    using (var snapshot = await Task.Run(() => BridgeMaterialFileSnapshot.Read(root, "Draw01", CancellationToken.None)))
    {
        Check(snapshot.WorkerThreadId != caller, "Work ran on caller thread");
        var asyncGraph = Graph(snapshot);
        Check(sync.Select(x => (x.Path, x.Cid, x.Digest)).SequenceEqual(asyncGraph.Select(x => (x.Path, x.Cid, x.Digest))), "Graph differs");
        Check(snapshot.Get(texture).Bytes == null, "Texture unnecessarily resident");
        using var nativeRead = File.OpenRead(texture);
        Check(BitConverter.ToString(SHA256.HashData(nativeRead)) == snapshot.Get(texture).Digest, "Native read differs");
        bool writeBlocked = false;
        try { using var write = new FileStream(texture, FileMode.Open, FileAccess.Write, FileShare.Read); }
        catch (IOException) { writeBlocked = true; }
        Check(writeBlocked, "Lease did not prevent write");
    }
    using (var writable = new FileStream(texture, FileMode.Open, FileAccess.Write, FileShare.None)) { }
    using (var cts = new CancellationTokenSource())
    {
        cts.Cancel();
        try { BridgeMaterialFileSnapshot.Read(root, "Draw01", cts.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
    }
    using (var empty = BridgeMaterialFileSnapshot.Read(root, "Suspension", CancellationToken.None))
        Check(empty.FileCount == 0, "Fixed style read shared files");
    var large = Path.Combine(root, "Draw01", "0000.Texture");
    using (var allocate = File.Create(large)) allocate.SetLength(256 * 1024 * 1024);
    using (var cancelRunning = new CancellationTokenSource())
    {
        var running = Task.Run(() => BridgeMaterialFileSnapshot.Read(root, "Draw01", cancelRunning.Token));
        var limit = Stopwatch.StartNew();
        bool leased = false;
        while (!leased && limit.Elapsed < TimeSpan.FromSeconds(10))
        {
            try { using var probe = new FileStream(large, FileMode.Open, FileAccess.Write, FileShare.ReadWrite); }
            catch (IOException) { leased = true; }
            if (!leased) await Task.Delay(1);
        }
        Check(leased, "Could not observe running worker lease");
        cancelRunning.Cancel();
        try { using var unexpected = await running; throw new Exception("Running work ignored cancellation"); }
        catch (OperationCanceledException) { }
        using var released = new FileStream(large, FileMode.Open, FileAccess.Write, FileShare.None);
    }
    File.Delete(large);
    BridgePreparedMaterials.SourceRoot = root;
    var system = new BridgeGenerationSystem();
    int applied = 0, failed = 0;
    system.Begin("Draw01", () => { Check(BridgePreparedMaterials.RequestFiles != null, "Missing active scope"); applied++; }, _ => failed++, 1);
    BridgePreviewState.Revision = 2;
    Check(!system.Tick(), "Stale preview still pending");
    Check(applied == 0, "Stale request applied");
    // A create job has no preview revision and must survive selection changes.
    system.Begin("Draw01", () => applied++, _ => failed++);
    BridgePreviewState.Revision = 3;
    var timeout = Stopwatch.StartNew();
    while (system.Pending && timeout.Elapsed < TimeSpan.FromSeconds(10)) { system.Tick(); Thread.Sleep(1); }
    Check(!system.Pending && applied == 1 && failed == 0, "Create was dropped or invalidated");
    Check(BridgePreparedMaterials.RequestFiles == null, "Snapshot leaked outside apply");
    system.Begin("../invalid", () => applied++, _ => failed++);
    timeout.Restart();
    while (system.Pending && timeout.Elapsed < TimeSpan.FromSeconds(10)) { system.Tick(); Thread.Sleep(1); }
    Check(failed == 1 && applied == 1, "Worker failure not reported once");
    system.Tick(); Check(failed == 1, "Failure repeatedly reported");
    // An incomplete worker must return immediately instead of waiting.
    var incomplete = new TaskCompletionSource<BridgeMaterialFileSnapshot>();
    system.Inject(incomplete.Task);
    timeout.Restart(); Check(system.Tick() && timeout.ElapsedMilliseconds < 100, "Main thread waited");
    system.Stop();
    var abandoned = BridgeMaterialFileSnapshot.Read(root, "Draw01", CancellationToken.None);
    incomplete.SetResult(abandoned);
    timeout.Restart();
    while (abandoned.FileCount != 0 && timeout.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(1);
    Check(abandoned.FileCount == 0, "Completed canceled result leaked leases");
    using (var writable = new FileStream(texture, FileMode.Open, FileAccess.Write, FileShare.None)) { }
    Console.WriteLine("PASS worker thread, byte-identical graph, native-style read sharing, write lock, cancellation, stale preview, create isolation, faults, nonblocking poll and abandoned lease cleanup.");
    if (args.Length != 0)
    {
        using var actual = await Task.Run(() => BridgeMaterialFileSnapshot.Read(args[0], "Draw01", CancellationToken.None));
        Console.WriteLine($"Actual Draw01 snapshot: files={actual.FileCount}, hashedBytes={actual.BytesHashed}, workerMs={actual.ElapsedMilliseconds}, textureBytesRetained=0");
        int graphs = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(args[0], "Draw01"), "*.Surface", SearchOption.AllDirectories))
        {
            string style = Path.Combine(args[0], "Draw01");
            string variant = Path.GetRelativePath(style, Path.GetDirectoryName(path)!);
            var expected = BridgePreparedMaterialFiles.Read(style, variant, Path.GetFileName(path),
                bytes => BridgeSurfaceReferences.Read(bytes, (_, _) => true), _ => { });
            var observed = BridgePreparedMaterialFiles.Read(style, variant, Path.GetFileName(path),
                bytes => BridgeSurfaceReferences.Read(bytes, (_, _) => true), _ => { }, snapshot: actual);
            Check(expected.Select(x => (x.Path,x.Cid,x.Digest)).SequenceEqual(observed.Select(x => (x.Path,x.Cid,x.Digest))), "Actual prepared graph differs: " + path);
            graphs++;
        }
        Console.WriteLine($"PASS actual Draw01 {graphs} descriptor dependency graphs, exact CID/SHA/path equivalence (built-in validation remains on main thread).");
    }
}
finally { Directory.Delete(root, true); }
