using BridgeBuilder.Runtime;
using System.Security.Cryptography;
using System.Text.Json;

// Copy already verified Draw01 products without changing payloads or identities.
var root = Path.GetFullPath(args[0]);
var reportPath = Path.GetFullPath(args[1]);
var source = Path.Combine(root, "artifacts/prepared-current-structures-canonical/Draw01");
var target = Path.Combine(root, "assets/Materials/Draw01");
var inventoryPath = Path.Combine(root, "artifacts/uv-density/structure-tessellated-sources.json");
using var inventory = JsonDocument.Parse(File.ReadAllBytes(inventoryPath));
var keys = inventory.RootElement.GetProperty("units").EnumerateArray()
    .Where(u => u.GetProperty("style").GetString() == "Draw01")
    .Select(u => u.GetProperty("key").GetString()!).Order().ToArray();
Require(keys.Length == 1029 && keys.Distinct().Count() == keys.Length, "Unexpected Draw01 inventory");
Require(keys.SequenceEqual(Directory.GetDirectories(Path.Combine(source, "Structure")).Select(Path.GetFileName).Order()), "Staging inventory differs");
string Hash(string p) { using var stream = File.OpenRead(p); return Convert.ToHexString(SHA256.HashData(stream)); }
string[] References(byte[] bytes) => BridgeSurfaceReferences.Read(bytes, (_, _) => throw new InvalidDataException("Unexpected structural VT"));
void Missing(string cid) => throw new InvalidDataException("Missing style-local structural dependency: " + cid);
var identities = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var sidecar in Directory.GetFiles(target, "*.cid", SearchOption.AllDirectories))
{
    var cid = File.ReadAllText(sidecar).Trim();
    var digest = Hash(sidecar[..^4]);
    Require(!identities.TryGetValue(cid, out var old) || old == digest, "Existing CID conflict: " + cid);
    identities[cid] = digest;
}
var copies = new SortedDictionary<string, string>(StringComparer.Ordinal);
foreach (var key in keys)
foreach (var input in BridgePreparedMaterialFiles.Read(source, "Structure/" + key, "Structure.Surface", References, Missing))
{
    Require(!identities.TryGetValue(input.Cid, out var old) || old == Hash(input.Path), "CID conflict: " + input.Cid);
    identities[input.Cid] = Hash(input.Path);
    var relative = Path.GetExtension(input.Path) == ".Texture" ? "Shared/" + input.Cid + ".Texture" : "Structure/" + key + "/Structure.Surface";
    foreach (var suffix in new[] { "", ".cid" })
    {
        var src = input.Path + suffix;
        var rel = relative + suffix;
        if (copies.TryGetValue(rel, out var previous)) Require(Hash(previous) == Hash(src), "Staging path conflict: " + rel);
        else copies.Add(rel, src);
        var dst = Path.Combine(target, rel);
        if (File.Exists(dst)) Require(Hash(dst) == Hash(src), "Destination path conflict: " + rel);
    }
}
// All conflicts are checked before the first destination mutation. Never overwrite.
int added = 0, reused = 0;
long addedBytes = 0;
var manifest = new List<object>();
foreach (var (relative, src) in copies)
{
    var dst = Path.Combine(target, relative);
    if (File.Exists(dst)) reused++;
    else
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.Copy(src, dst, overwrite: false);
        added++; addedBytes += new FileInfo(dst).Length;
    }
    var hash = Hash(src);
    Require(hash == Hash(dst), "Copied bytes differ: " + relative);
    manifest.Add(new { path = relative, bytes = new FileInfo(dst).Length, sha256 = hash });
}
// Run the production dependency reader once for every integrated variant.
foreach (var key in keys)
    BridgePreparedMaterialFiles.Read(target, "Structure/" + key, "Structure.Surface", References, Missing);
Require(!Directory.GetDirectories(target, "*", SearchOption.AllDirectories).Any(p => !Directory.EnumerateFileSystemEntries(p).Any()), "Empty material directory");
var report = new { status = "PASS", style = "Draw01", inventorySha256 = Hash(inventoryPath), units = keys.Length,
    source, target, requiredFiles = copies.Count, addedFiles = added, reusedFiles = reused, addedBytes,
    requiredBytes = copies.Sum(p => new FileInfo(p.Value).Length), conflicts = 0, unchangedPayloadsAndCids = true,
    productionDependencyPreflight = true, bakedTextures = 0, localization = "No user-visible text or localization behavior changed.", files = manifest };
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS Draw01: {keys.Length} units; {copies.Count} files; {added} added / {reused} reused; {addedBytes} added bytes; production dependency preflight passed.");
static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
