using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BridgeBuilder.Runtime;

if (args.Length != 3 && !(args.Length == 4 && args[3] == "--check")) throw new ArgumentException("Usage: PrepareLanes <Cities2_Data> <NativeTemplates.json> <assets/Materials> [--check]");
bool check = args.Length == 4;
const string recipe = "BridgeBuilder/SkinnedLane/1";
var templateBytes = File.ReadAllBytes(args[1]);
using var document = JsonDocument.Parse(templateBytes);
var templates = document.RootElement.EnumerateArray().ToDictionary(t => t.GetProperty("hash").GetInt32());
var styles = new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" };
var archives = new List<ZipArchive>();
try
{
    foreach (var family in new[] { "Game", "BridgesAndPorts" })
    foreach (var file in Directory.GetFiles(Path.Combine(args[0], "Content", family), "*.cok")
        .Where(p => Path.GetFileName(p).StartsWith("Blob") || Path.GetFileName(p).StartsWith("VT")))
        archives.Add(ZipFile.OpenRead(file));
    var builtins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var archive in archives)
    foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".cid", StringComparison.Ordinal)))
    {
        using var text = new StreamReader(entry.Open());
        if (archive.GetEntry(entry.FullName[..^4]) != null) builtins.Add(text.ReadToEnd().Trim());
    }
    var prepared = new Dictionary<string, byte[]>();
    int sources = 0;
    foreach (var archive in archives)
    foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".Surface") || e.FullName.EndsWith(".Material")))
    {
        using var input = entry.Open(); using var buffer = new MemoryStream(); input.CopyTo(buffer);
        var source = buffer.ToArray(); using var r = new BinaryReader(new MemoryStream(source));
        var version = r.ReadUInt16(); int from = r.ReadInt32();
        int target = from switch { 16 => 0, 31 => 1, 34 => 35, _ => -1 };
        if (target < 0) continue;
        Require(version == 1, "Unsupported native descriptor version: " + entry.FullName);
        Require(Stack(from).SequenceEqual(Stack(target)), "Source/object texture stacks differ.");
        if (r.ReadBoolean())
        {
            r.BaseStream.Position += 276; // Native SurfaceAssetVTHeader, preserved exactly.
            if (Present()) r.BaseStream.Position += 16;
        }
        int floatStart = (int)r.BaseStream.Position;
        var floats = new Dictionary<string, float>();
        for (int n = Count(); n > 0; n--) floats.Add(r.ReadString(), r.ReadSingle());
        int middleStart = (int)r.BaseStream.Position;
        foreach (int size in new[] { 4, 16, 16 })
            for (int n = Count(); n > 0; n--) { r.ReadString(); r.BaseStream.Position += size; }
        for (int n = Count(); n > 0; n--) { r.ReadString(); if (Present()) r.BaseStream.Position += 16; }
        int keywordStart = (int)r.BaseStream.Position;
        var keywords = new HashSet<string>();
        for (int n = Count(); n > 0; n--) keywords.Add(r.ReadString());
        Require(r.BaseStream.Position == source.Length, "Unconsumed native descriptor data.");
        var defaults = templates[from].GetProperty("material").GetProperty("m_SavedProperties").GetProperty("m_Floats")
            .EnumerateArray().ToDictionary(p => p[0].GetString()!, p => p[1].GetSingle());
        foreach (string property in new[] { "_Snow", "_EmissiveIntensity", "_Metallic", "_Smoothness", "_NormalScale" })
            if (!floats.ContainsKey(property) && defaults.TryGetValue(property, out var value)) floats.Add(property, value);
        keywords.Add("_TANGENTSPACE_OCTO");
        using var output = new MemoryStream(); using var w = new BinaryWriter(output);
        w.Write(version); w.Write(target); w.Write(source, 6, floatStart - 6);
        w.Write((byte)255); w.Write(floats.Count);
        foreach (var pair in floats) { w.Write(pair.Key); w.Write(pair.Value); }
        w.Write(source, middleStart, keywordStart - middleStart);
        w.Write((byte)255); w.Write(keywords.Count); foreach (var keyword in keywords) w.Write(keyword);
        w.Flush(); var bytes = output.ToArray();
        string[] References(byte[] b) => BridgeSurfaceReferences.Read(b, (t, p) => Stack(t).Contains(p));
        Require(References(bytes).Order().SequenceEqual(References(source).Order()), "Conversion changed native dependencies.");
        Require(References(bytes).All(builtins.Contains), "Missing built-in dependency: " + entry.FullName + ": " + string.Join(",", References(bytes).Where(cid => !builtins.Contains(cid))));
        string digest = Digest(source);
        if (prepared.TryGetValue(digest, out var prior)) Require(prior.SequenceEqual(bytes), "Source has conflicting conversion.");
        prepared[digest] = bytes; sources++;

        bool Present() => r.ReadByte() switch { 0 => false, 255 => true, _ => throw new InvalidDataException("Native map marker") };
        int Count() => Present() ? r.ReadInt32() : 0;
    }
    // Validate everything before writing any prepared asset.
    foreach (var item in prepared)
    {
        var cid = Digest(Encoding.UTF8.GetBytes(recipe + "/" + Digest(templateBytes) + "/" + item.Key + "/" + Digest(item.Value)))[..32];
        foreach (var style in styles)
        {
            var file = Path.Combine(Path.GetFullPath(args[2]), style, "Lanes", item.Key, "SkinnedLane.Surface");
            if (check)
            {
                Require(File.ReadAllBytes(file).SequenceEqual(item.Value), "Prepared descriptor differs: " + file);
                Require(File.ReadAllText(file + ".cid") == cid, "Prepared identity differs: " + file);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllBytes(file, item.Value); File.WriteAllText(file + ".cid", cid, new UTF8Encoding(false));
            }
        }
    }
    Console.WriteLine($"PASS prepared lanes: {sources} sources, {prepared.Count} variants, {styles.Length} styles; {prepared.Values.Sum(b => b.Length + 32)} bytes/style; built-in dependencies unchanged; no texture baking.");
}
finally { foreach (var archive in archives) archive.Dispose(); }
IEnumerable<string> Stack(int template) => templates[template].GetProperty("stacks").EnumerateArray()
    .SelectMany(s => s.EnumerateArray().Select(p => p.GetString()!));
static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
