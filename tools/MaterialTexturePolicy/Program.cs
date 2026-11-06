using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Runtime;

var root = Path.GetFullPath(args[0]);
var records = JsonSerializer.Deserialize<Consumer[]>(File.ReadAllText(args[1]))!;
var reviewed = records.ToDictionary(r => r.style + "/" + r.path);
var surfaces = Files("*.Surface");
var textures = Files("*.Texture").ToDictionary(Cid);
var used = new HashSet<string>();
var covered = new HashSet<string>();
foreach (var file in surfaces)
{
    var path = Relative(file);
    if (!reviewed.TryGetValue(path, out var row))
        throw new InvalidDataException("Unreviewed Surface: " + path);
    var bytes = File.ReadAllBytes(file);
    if (!Digest(bytes).Equals(row.surfaceSha256, StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("Changed Surface requires a fresh channel/consumer review: " + path);
    var links = BridgeSurfaceReferences.Read(bytes, (_, _) => false)
        .Where(textures.ContainsKey).ToHashSet();
    CheckRole(row.role, links, row.local.Values, path);
    foreach (var cid in links)
    {
        var target = Relative(textures[cid]);
        var shared = target == "Shared/Paint/" + cid + ".Texture";
        if (shared ? row.path != "Paint/SkinnedPaint.Surface" : !target.StartsWith(row.style + "/", StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected cross-style Texture: " + target + " from " + path);
    }
    used.UnionWith(links);
    covered.Add(path);
}
if (!covered.SetEquals(reviewed.Keys) || !used.SetEquals(textures.Keys))
    throw new InvalidDataException("Missing approved Surface or orphan packaged Texture.");
var payloads = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "RoadTexturePayloads.json")))!;
if (!used.SetEquals(payloads.Keys)) throw new InvalidDataException("Reviewed Texture list differs from packaged CIDs.");
foreach (var (cid,file) in textures)
    if (!Digest(File.ReadAllBytes(file)).Equals(payloads[cid],StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("Texture payload differs from approved authoring identity: " + file);
if (args.Contains("--self-test"))
{
    foreach (var (role, actual, expected) in new[] { ("NonRoad", new[]{"local"}, new[]{"local"}), ("Road", new[]{"stale"}, new[]{"fresh"}), ("Unknown", Array.Empty<string>(), Array.Empty<string>()) })
    {
        try { CheckRole(role, actual.ToHashSet(), expected, "negative control"); }
        catch (InvalidDataException) { continue; }
        throw new Exception("Negative material-policy control was accepted.");
    }
}
Console.WriteLine($"PASS {covered.Count} serialized Surfaces, {textures.Count} uniquely owned Texture files, {records.Count(r => r.role == "NonRoad")} non-road descriptors with zero packaged edges.");
string[] Files(string glob) => Directory.GetFiles(root, glob, SearchOption.AllDirectories);
string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
static string Cid(string p) => File.ReadAllText(p + ".cid").Trim();
static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
static void CheckRole(string role, HashSet<string> actual, IEnumerable<string> expected, string path)
{
    if (role is not ("Road" or "NonRoad") || role == "NonRoad" && actual.Count != 0 || !actual.SetEquals(expected))
        throw new InvalidDataException("Invalid road material channel graph: " + path);
}
record Consumer(string style, string path, string role, string consumer, string surfaceSha256, Dictionary<string,string> local);
