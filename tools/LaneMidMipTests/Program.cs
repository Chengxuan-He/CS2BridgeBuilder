using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;

if (args.Length == 3 && args[0] == "--original")
{
    var manifest = JsonNode.Parse(File.ReadAllText(args[1]))!.AsArray();
    Directory.CreateDirectory(args[2]);
    foreach (var test in manifest)
    {
        using var stream = new BoundedInput(test!["archive"]!.GetValue<string>(), test["offset"]!.GetValue<long>(), test["length"]!.GetValue<long>());
        var bytes = MovableMidMipSubset.Copy(stream, test["sourceCid"]!.GetValue<string>(), test["targetCid"]!.GetValue<string>(), 512, 3)!;
        if (!bytes.SequenceEqual(File.ReadAllBytes(test["expected"]!.GetValue<string>()))) throw new Exception("Original native source block mismatch.");
        if (stream.ReadBytes > 2_000_000) throw new Exception("Native source payload read was not bounded.");
        File.WriteAllBytes(Path.Combine(args[2], test["targetCid"]!.GetValue<string>() + ".MidMips"), bytes);
        Console.WriteLine($"PASS original native source={test["sourceCid"]}; bytesRead={stream.ReadBytes}; outputBytes={bytes.Length}; ordinals={test["ordinals"]}");
    }
    Console.WriteLine($"PASS {manifest.Count} original packaged native sources, exact independent Python payload equivalence.");
    return;
}

// Input is the independent offline producer's combined native fixture/report.
// Its recorded source-block SHA256s are checked by VerifyMidMips.py afterwards.
if (args.Length != 2) throw new ArgumentException("Expected combined producer report and output directory.");
Directory.CreateDirectory(args[1]);
var original = JsonNode.Parse(File.ReadAllText(args[0]))!["styles"]![0]!;
string path = original["path"]!.GetValue<string>();
var originalBytes = File.ReadAllBytes(path);
var ids = BridgeMidMipReferences.Read(originalBytes);
RuntimeBoundaryTests.Run(originalBytes, ids[0]);
var rows = new JsonArray();
int rejected = 0;
foreach (string cid in ids)
{
    string target = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("runtime-alias:" + cid)))[..32].ToLowerInvariant();
    using var input = new MemoryStream(originalBytes, false);
    var bytes = MovableMidMipSubset.Copy(input, cid, target, 512, 3) ?? throw new Exception("Missing source membership.");
    if (!BridgeMidMipReferences.Read(bytes).SequenceEqual(new[] { target })) throw new Exception("Alias leaked another Surface CID.");
    using var again = new MemoryStream(bytes, false);
    var restored = MovableMidMipSubset.Copy(again, target, cid, 512, 3)!;
    using var baseline = new MemoryStream(originalBytes, false);
    if (!restored.SequenceEqual(MovableMidMipSubset.Copy(baseline, cid, cid, 512, 3)!)) throw new Exception("CID roundtrip changed blocks.");
    string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    string output = Path.GetFullPath(Path.Combine(args[1], target, "TS512_MB3.MidMips"));
    Directory.CreateDirectory(Path.GetDirectoryName(output)!); File.WriteAllBytes(output, bytes);
    File.WriteAllText(output + ".cid", hash[..32]);
    var checks = new JsonArray();
    foreach (var check in original["blockChecks"]!.AsArray().Where(c => c!["cid"]!.GetValue<string>() == cid))
    { var copy = check!.DeepClone(); copy["cid"] = target; checks.Add(copy); }
    rows.Add(new JsonObject { ["style"] = "RuntimeAlias", ["sourceCid"] = cid, ["targetCid"] = target,
        ["path"] = output, ["bytes"] = bytes.Length, ["sha256"] = hash, ["cid"] = hash[..32], ["blockChecks"] = checks });
}
using (var missing = new MemoryStream(originalBytes, false))
    if (MovableMidMipSubset.Copy(missing, new string('0', 32), new string('1', 32), 512, 3) != null) throw new Exception("Missing membership accepted.");
foreach (var broken in new[] { originalBytes[..^1], originalBytes.Select((b, i) => i == 4 ? (byte)0 : b).ToArray() })
{
    try { using var input = new MemoryStream(broken, false); MovableMidMipSubset.Copy(input, ids[0], ids[0], 512, 3); }
    catch (InvalidDataException) { rejected++; }
}
if (rejected != 2) throw new Exception("Malformed native inputs accepted.");
// Convert the first native member to a duplicate identity without changing any
// tile data; the subset must be byte-identical to extraction through its primary.
using (var r = new BinaryReader(new MemoryStream(originalBytes, false)))
{
    r.BaseStream.Position = 12; int stacks = r.ReadInt32(); r.ReadInt32(); r.ReadInt32();
    var counts = new List<int>();
    for (int stack = 0; stack < stacks; stack++)
    {
        int count = r.ReadInt32();
        for (int c = 0; c < count; c++)
        {
            r.ReadInt32(); r.ReadInt32(); int n = r.ReadInt32(); r.ReadInt32(); r.ReadInt32();
            bool disabled = r.ReadBoolean(); r.ReadInt32(); if (!disabled) counts.Add(n);
        }
    }
    int first = (int)r.BaseStream.Position;
    var firstHash = r.ReadBytes(16);
    string firstCid = string.Concat(firstHash.Select(b => $"{b & 15:x}{b >> 4:x}"));
    r.BaseStream.Position = first;
    foreach (int count in counts)
    for (int i = 0; i < count; i++)
    {
        r.BaseStream.Position += 22; int duplicates = r.ReadInt32();
        r.BaseStream.Position += duplicates * 16 + 3 * 8;
    }
    r.ReadInt32(); int oldAlignment = (int)r.BaseStream.Position;
    if (BitConverter.ToInt32(originalBytes, first + 22) != 0) throw new Exception("Expected primary fixture.");
    byte[] duplicate = originalBytes[..(first + 26)].Concat(firstHash).Concat(originalBytes[(first + 26)..]).ToArray();
    Array.Fill(duplicate, (byte)0x11, first, 16);
    BitConverter.GetBytes(1).CopyTo(duplicate, first + 22);
    BitConverter.GetBytes((long)oldAlignment + 16).CopyTo(duplicate, oldAlignment + 16);
    using var primaryInput = new MemoryStream(originalBytes, false);
    using var duplicateInput = new MemoryStream(duplicate, false);
    if (!MovableMidMipSubset.Copy(primaryInput, firstCid, firstCid, 512, 3)!
        .SequenceEqual(MovableMidMipSubset.Copy(duplicateInput, firstCid, firstCid, 512, 3)!))
        throw new Exception("Duplicate identity changed native blocks.");
}
// Native VTTexture: zero levels, empty tail, and required level 3 are distinct cases.
foreach (var test in new[] { (0, 7, 0), (3, 0, 0), (3, 7, 3) })
{
    using var stream = new MemoryStream(); using var w = new BinaryWriter(stream, Encoding.UTF8, true);
    w.Write((ushort)1); w.Write((byte)0); w.Write(new byte[8]); w.Write((ushort)test.Item1);
    w.Write(new byte[5]); w.Write(test.Item2); stream.Position = 0;
    if (BridgeMidMipReferences.RequiredLevels(stream, false) != test.Item3) throw new Exception("Native mip requirement mismatch.");
}
File.WriteAllText(Path.Combine(args[1], "report.json"), new JsonObject { ["styles"] = rows }.ToJsonString(new() { WriteIndented = true }));
Console.WriteLine($"PASS runtime lane aliases: {ids.Length} independent Surface subsets, duplicate identity, identity roundtrip, missing membership, malformed cache and zero/empty controls.");

sealed class BoundedInput : Stream
{
    readonly FileStream file; readonly long offset, length;
    internal long ReadBytes;
    internal BoundedInput(string path, long offset, long length) { file = File.OpenRead(path); this.offset = offset; this.length = length; file.Position = offset; }
    public override bool CanRead => true; public override bool CanSeek => true; public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => file.Position - offset; set { if (value < 0 || value > length) throw new IOException(); file.Position = offset + value; } }
    public override int Read(byte[] buffer, int start, int count) { int n = file.Read(buffer, start, (int)Math.Min(count, length - Position)); ReadBytes += n; return n; }
    public override long Seek(long at, SeekOrigin origin) { Position = origin switch { SeekOrigin.Begin => at, SeekOrigin.Current => Position + at, _ => length + at }; return Position; }
    public override void Flush() => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) file.Dispose(); base.Dispose(disposing); }
}
