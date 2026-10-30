using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Bridges;

if (args.Length != 2) throw new ArgumentException("Expected repository and report path.");
var root = Path.GetFullPath(args[0]);
string PathIn(string relative) => Path.Combine(root, relative);
string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
string Str(JsonElement value, string key) => value.GetProperty(key).GetString()!;
void Require(bool valid, string reason) { if (!valid) throw new InvalidDataException(reason); }
var graphPath = PathIn("artifacts/prepared-materials/Lift01/rebuild-native-v1/config/source-graph.json");
using var graphDoc = JsonDocument.Parse(File.ReadAllBytes(graphPath));
var graph = graphDoc.RootElement;
var sources = graph.GetProperty("sources").EnumerateArray().ToDictionary(s => Str(s, "cid"));
var uses = graph.GetProperty("usages").EnumerateArray().ToArray();
var native = uses.Where(u => sources[Str(u, "sourceCid")].GetProperty("template").GetInt32() == 33).ToArray();
Require(uses.Length == 23 && native.Length == 3, "Native source inventory changed; review every stream.");
var records = new List<object>();
int predicates = 0;
foreach (var source in sources.Values)
{
    using var archive = ZipFile.OpenRead(Str(source, "archive"));
    var entry = archive.GetEntry(Str(source, "entry")) ?? throw new InvalidDataException("Missing original Surface.");
    using var stream = entry.Open();
    using var memory = new MemoryStream(); stream.CopyTo(memory);
    var bytes = memory.ToArray();
    Require(Sha(bytes) == Str(source, "sha256"), "Original Surface digest changed.");
    Require(BitConverter.ToInt32(bytes, 2) == source.GetProperty("template").GetInt32(), "Template evidence changed.");
    using var sidecar = new StreamReader(archive.GetEntry(Str(source, "entry") + ".cid")!.Open());
    Require(sidecar.ReadToEnd().Trim() == Str(source, "cid"), "Original Surface identity changed.");
    var original = new Colossal.IO.AssetDatabase.SurfaceAsset
    {
        materialTemplateHash = source.GetProperty("template").GetInt32(),
        isBuiltin = true,
        isVTMaterial = bytes[6] != 0
    };
    original.id.guid = Str(source, "cid");
    bool rejected = false;
    try
    {
        var result = Lift01NativeMaterials.Resolve(original,
            _ => throw new InvalidDataException("Non-road reuse invoked a replacement descriptor loader."));
        Require(ReferenceEquals(result, original), "Non-road material identity was replaced.");
    }
    catch (InvalidOperationException error) when (error.Message.Contains("native NetPiece render context"))
    { rejected = true; }
    Require(rejected == (original.materialTemplateHash is 17 or 33),
        "Network materials must be rejected from the object-only resolver.");
}
foreach (var use in uses)
{
    string render = Str(use, "render"), cid = Str(use, "sourceCid");
    int mesh = use.GetProperty("slot").GetInt32();
    bool expected = sources[cid].GetProperty("template").GetInt32() == 33;
    Require(Lift01NativeMaterials.IsNativeNetwork(render, mesh, cid) == expected, "Stream classification differs from original template evidence.");
    predicates++;
    foreach (string otherCid in sources.Keys.Append(new string('0', 32)))
    {
        Require(Lift01NativeMaterials.IsNativeNetwork(render, mesh, otherCid) == (expected && otherCid == cid), "Cross-surface identity admitted.");
        predicates++;
    }
    Require(!Lift01NativeMaterials.IsNativeNetwork(render, mesh + 1, cid), "Unmeasured slot admitted.");
    Require(!Lift01NativeMaterials.IsNativeNetwork(render + " Copy", mesh, cid), "Name resemblance admitted.");
    Require(!Lift01NativeMaterials.IsNativeNetwork("LiftBridge01 Mesh", mesh, cid), "Mechanism object admitted.");
    predicates += 3;
    var code = File.ReadAllText(PathIn("src/BridgeBuilder/Bridges/Types/Lift01/" + Str(use, "code")));
    Require(Sha(File.ReadAllBytes(PathIn("src/BridgeBuilder/Bridges/Types/Lift01/" + Str(use, "code")))) == Str(use, "codeSha256"), "Reconstructed source definition changed.");
    if (expected)
    {
        Require(code.Contains('"' + render + '"') && code.Contains('"' + cid + '"'), "Source construction lacks the recorded identity.");
        records.Add(new { render, mesh, sourceCid = cid, sourceSha256 = Str(sources[cid], "sha256"), template = 33 });
    }
}
var captures = PathIn("artifacts/prepared-materials/shared-nontiling/actual-final2/styles/Lift01.regions.json.classifications.ndjson.gz");
int remaining = 0, totalNative = 0;
var perRender = new Dictionary<string, int>();
using (var gzip = new GZipStream(File.OpenRead(captures), CompressionMode.Decompress))
using (var reader = new StreamReader(gzip))
{
    while (reader.ReadLine() is { } line)
    {
        using var doc = JsonDocument.Parse(line); var unit = doc.RootElement;
        string render = Str(unit, "render"); int mesh = unit.GetProperty("mesh").GetInt32();
        var use = uses.Single(u => Str(u, "render") == render && u.GetProperty("slot").GetInt32() == mesh);
        string cid = Str(use, "sourceCid");
        Require(Str(unit, "sourceDigest") == Str(sources[cid], "sha256"), "Captured receiver source lineage changed.");
        bool admitted = Lift01NativeMaterials.IsNativeNetwork(render, mesh, cid);
        if (admitted) totalNative++;
        if (Str(unit, "status") != "remaining") continue;
        Require(admitted, "An unresolved receiver is outside the verified native graph.");
        remaining++; perRender[render] = perRender.GetValueOrDefault(render) + 1;
    }
}
Require(remaining == 16184, "Changed baseline count; review capture coverage.");
var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new
{
    scope = "Production identity dispatch and object-context rejection, original COK Surface provenance, and prior actual-plan receiver attribution; not native renderer acceptance",
    sources = records, sourceGraphSha256 = Sha(File.ReadAllBytes(graphPath)),
    predicateSourceSha256 = Sha(File.ReadAllBytes(PathIn("src/BridgeBuilder/Bridges/Types/Lift01/Lift01NativeMaterials.cs"))),
    capturedClassificationSha256 = Sha(File.ReadAllBytes(captures)),
    checkedPredicates = predicates, checkedOriginalResolvers = sources.Count,
    attributedPriorRemaining = remaining, nativeContextPriorUnits = totalNative, perRender,
    newTextures = 0, changedGeometry = false, humanAcceptance = "pending"
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS Lift01 native identity: {predicates} checks, {remaining} prior remaining receivers, {totalNative} prior density units in the native graph.");
