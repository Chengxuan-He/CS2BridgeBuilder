using BridgeBuilder.Runtime;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length > 0 && args[0] == "--test")
{
    FileChecks.Run();
    FileChecks.Existing(args.Length > 1 ? args[1] : "assets/Materials");
    return;
}
var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
// Optional current inventory plus separately validated new units. Old candidates
// outside that inventory are not published merely because their files exist.
var selectedSurfaces = new List<(string Style, string Surface)>();
if(args.Length > 4)
{
    using var inventory = JsonDocument.Parse(File.ReadAllBytes(args[3]));
    var supplementRoot = Path.GetFullPath(args[4]);
    var supplements = Directory.GetFiles(supplementRoot, "Structure.Surface", SearchOption.AllDirectories)
        .GroupBy(p => (Style: Path.GetRelativePath(supplementRoot, p).Split(Path.DirectorySeparatorChar)[0],
                       Key: Path.GetFileName(Path.GetDirectoryName(p)!)))
        .ToDictionary(g => g.Key, g => g.ToArray());
    foreach(var unit in inventory.RootElement.GetProperty("units").EnumerateArray())
    {
        var style = unit.GetProperty("style").GetString()!;
        var key = unit.GetProperty("key").GetString()!;
        var original = Path.Combine(source, style, "Structure", key, "Structure.Surface");
        if(File.Exists(original)) { selectedSurfaces.Add((style, original)); continue; }
        if(!supplements.TryGetValue((style,key), out var candidates))
            throw new FileNotFoundException("Current structural recipe is not prepared: " + style + "/" + key);
        if(candidates.Skip(1).Any(p => !File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(candidates[0]))))
            throw new InvalidDataException("Conflicting supplementary descriptor: " + style + "/" + key);
        selectedSurfaces.Add((style, candidates[0]));
    }
}
else selectedSurfaces.AddRange(Directory.GetFiles(source, "Structure.Surface", SearchOption.AllDirectories)
    .Select(p => (Directory.GetParent(Path.GetDirectoryName(p)!)!.Parent!.Name, p)));
if (selectedSurfaces.GroupBy(s => (s.Style, Key: Path.GetFileName(Path.GetDirectoryName(s.Surface)!))).Any(g => g.Count() != 1))
    throw new InvalidDataException("Duplicate structural recipe in selected inventory.");
if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Use a new destination; existing sources/outputs are never replaced.");
if (output.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new IOException("The destination cannot be inside the source.");
string Digest(string path) { using var s = File.OpenRead(path); return BitConverter.ToString(SHA256.HashData(s)); }
string[] References(byte[] bytes) => BridgeSurfaceReferences.Read(bytes, (_, _) => throw new InvalidDataException("Structural repeats must not use VT."));
void Missing(string cid) => throw new InvalidDataException("Incomplete structural source graph: " + cid);
var copies = new Dictionary<string, (string Source, string Digest)>(StringComparer.OrdinalIgnoreCase);
var variants = new List<(string Style, string Variant, string Name)>();
long inputBytes = 0;
int inputs = 0;
foreach (var styleGroup in selectedSurfaces.GroupBy(p => (p.Style, Folder: Directory.GetParent(Path.GetDirectoryName(p.Surface)!)!.Parent!.FullName))
    .OrderBy(g => g.Key.Style, StringComparer.Ordinal).ThenBy(g => g.Key.Folder, StringComparer.Ordinal))
{
    var styleFolder = styleGroup.Key.Folder;
    var style = styleGroup.Key.Style;
    foreach (var selected in styleGroup)
    {
        var surface = selected.Surface;
        var sourceVariant = Path.GetRelativePath(styleFolder, Path.GetDirectoryName(surface)!);
        var variant = Path.Combine("Structure", Path.GetFileName(Path.GetDirectoryName(surface)!));
        variants.Add((style, variant, "Structure.Surface"));
        foreach (var input in BridgePreparedMaterialFiles.Read(styleFolder, sourceVariant, "Structure.Surface", References, Missing))
        {
            inputs++; inputBytes += new FileInfo(input.Path).Length + new FileInfo(input.Path + ".cid").Length;
            var destination = Path.GetExtension(input.Path) == ".Texture"
                ? Path.Combine(style, "Shared", input.Cid + ".Texture")
                : Path.Combine(style, variant, Path.GetFileName(input.Path));
            if (copies.TryGetValue(destination, out var previous))
            {
                if (previous.Digest != input.Digest) throw new InvalidDataException("Same-CID source conflict: " + destination);
            }
            else copies.Add(destination, (input.Path, input.Digest));
        }
    }
    Console.WriteLine($"Preflighted {style}; variants={variants.Count}; unique payloads={copies.Count}.");
}
if (variants.Count == 0) throw new InvalidDataException("No structural variants found.");
long outputBytes = 0;
int written = 0;
foreach (var copy in copies)
{
    var target = Path.Combine(output, copy.Key);
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    File.Copy(copy.Value.Source, target);
    File.Copy(copy.Value.Source + ".cid", target + ".cid");
    if (Digest(target) != copy.Value.Digest || !File.ReadAllBytes(target + ".cid").SequenceEqual(File.ReadAllBytes(copy.Value.Source + ".cid")))
        throw new IOException("Copy differs: " + target);
    outputBytes += new FileInfo(target).Length + new FileInfo(target + ".cid").Length;
    if (++written % 10000 == 0) Console.WriteLine($"Copied and verified {written}/{copies.Count} payloads.");
}
foreach (var variant in variants)
    BridgePreparedMaterialFiles.Read(Path.Combine(output, variant.Style), variant.Variant, variant.Name, References, Missing);
var report = new { variants = variants.Count, sourcePayloadVisits = inputs, outputPayloads = copies.Count,
    authoringInputBytes = inputBytes, authoringOutputBytes = outputBytes, bytesAndCidsUnchanged = true,
    independentStyles = variants.Select(v => v.Style).Distinct().Count(), runtimePreflightPassed = true };
File.WriteAllText(args[2], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS compact structural sources: {variants.Count} variants; {copies.Count} unique style-local payloads; exact bytes/CIDs and runtime dependency preflight.");
