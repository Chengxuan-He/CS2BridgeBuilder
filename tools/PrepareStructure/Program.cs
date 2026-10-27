using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BridgeBuilder.Bridges;
using UnityEngine;

if (args.Length != 7) throw new ArgumentException("PrepareStructure <repository> <Cities2_Data> <native-audit.json> <layout-plans.bin> <verified-plans.samplers.json> <material-library.json> <output.json>");
using var reachedDocument = JsonDocument.Parse(File.ReadAllBytes(args[4]));
if (reachedDocument.RootElement.GetProperty("geometryDigest").GetString() != MovableStructureKey.Digest(File.ReadAllBytes(args[3])))
    throw new InvalidDataException("Sampler evidence belongs to another geometry fixture.");
var reached = new HashSet<(string Render, int Mesh, string Key)>();
int variableSamples = 0;
foreach (var u in reachedDocument.RootElement.GetProperty("units").EnumerateArray())
{
    if (u.GetProperty("fragment").GetBoolean()) { variableSamples++; continue; }
    float[] Array(string name) => u.GetProperty(name).EnumerateArray().Select(v => v.GetSingle()).ToArray();
    var b = Array("bounds"); var a = Array("a"); var v = Array("b"); var c = Array("c");
    reached.Add((u.GetProperty("render").GetString()!, u.GetProperty("mesh").GetInt32(),
        MovableStructureKey.For("", u.GetProperty("triangle").GetBoolean(), new(b[0],b[1],b[2],b[3]), new(a[0],a[1]), new(v[0],v[1]), new(c[0],c[1]))));
}
var unresolved = new HashSet<(string Render, int Mesh, string Key)>(reached);
var renders = new Dictionary<string, string[]>();
using var audit = JsonDocument.Parse(File.ReadAllBytes(args[2]));
foreach (var item in audit.RootElement.GetProperty("selected").EnumerateObject())
{
    var data = item.Value.GetProperty("data");
    if (!data.TryGetProperty("m_SurfaceAssets", out var surfaces)) continue;
    string Cid(JsonElement v)
    {
        var bytes = Enumerable.Range(1, 4).SelectMany(i => BitConverter.GetBytes(v.GetProperty("m_GuidPart" + i).GetInt32()));
        return string.Concat(bytes.Select(b => (b & 15).ToString("x") + (b >> 4).ToString("x")));
    }
    Add(data.GetProperty("m_Name").GetString()!, surfaces.EnumerateArray().Select(Cid).ToArray());
}
// Approach sources are measured immutable descriptors, alongside the native audit.
foreach (var path in Directory.GetFiles(Path.Combine(args[0], "src/BridgeBuilder/Bridges/Types"), "*.cs", SearchOption.AllDirectories))
foreach (Match match in Regex.Matches(File.ReadAllText(path), "Create(?:<[^>]+>)?\\(\"([^\"]+)\",\\s*\"[a-f0-9]{32}\",\\s*new\\[\\]\\s*\\{([^}]+)\\}"))
    Add(match.Groups[1].Value, Regex.Matches(match.Groups[2].Value, "[a-f0-9]{32}").Select(m => m.Value).ToArray());
void Add(string name, string[] cids)
{
    if (renders.TryGetValue(name, out var prior) && !prior.SequenceEqual(cids)) throw new InvalidDataException("Conflicting measured surface slots: " + name);
    renders[name] = cids;
}

var sources = new Dictionary<string, byte[]>();
foreach (var family in new[] { "Game", "BridgesAndPorts" })
foreach (var archivePath in Directory.GetFiles(Path.Combine(args[1], "Content", family), "Blob*.cok"))
{
    using var archive = ZipFile.OpenRead(archivePath);
    foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".Surface") || e.FullName.EndsWith(".Material")))
    {
        var sidecar = archive.GetEntry(entry.FullName + ".cid"); if (sidecar == null) continue;
        using var text = new StreamReader(sidecar.Open()); string cid = text.ReadToEnd().Trim();
        using var stream = entry.Open(); using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        if (sources.TryGetValue(cid, out var prior) && !prior.SequenceEqual(bytes.ToArray())) throw new InvalidDataException("Conflicting native source " + cid);
        sources[cid] = bytes.ToArray();
    }
}
using var reader = new BinaryReader(File.OpenRead(args[3]));
if (reader.ReadInt32() != 2) throw new InvalidDataException("Unsupported geometry evidence version.");
int inputs = reader.ReadInt32(), domains = 0;
var units = new Dictionary<(string, string), object>();
for (int i = 0; i < inputs; i++)
{
    string Text() => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    string render = Text(), implementation = Text();
    int family = reader.ReadInt32(), lod = reader.ReadInt32(), mesh = reader.ReadInt32(), vertices = reader.ReadInt32(), indices = reader.ReadInt32();
    reader.BaseStream.Position += vertices * 12;
    var uv = Enumerable.Range(0, vertices).Select(_ => new Vector2(reader.ReadSingle(), reader.ReadSingle())).ToArray();
    reader.BaseStream.Position += indices * 4;
    if (render is "DrawBridge01 Mesh" or "DrawBridge01_LOD1 Mesh" && mesh == 0)
        foreach (var fix in Draw01UndersideUv.ForLod(lod)) uv[fix.Key] = fix.Value;
    var recipe = MovableUvDomains.For(render, mesh) ?? throw new InvalidDataException("No measured domains: " + render);
    string style = Regex.Match(implementation, "^(PedestrianDraw|Draw|Lift)[0-9]{2}").Value;
    if (style.Length == 0)
        style = Regex.Match(render.Replace("Bridge", ""), "^(PedestrianDraw|Draw|Lift)[0-9]{2}").Value;
    if (!Regex.IsMatch(style, "^(PedestrianDraw|Draw|Lift)[0-9]{2}$")) throw new InvalidDataException("Unresolved style: " + render);
    if (!renders.TryGetValue(render, out var slots) || mesh >= slots.Length) throw new InvalidDataException("No measured surface slot: " + render + "/" + mesh);
    string cid = slots[mesh], digest = MovableStructureKey.Digest(sources[cid]);
    foreach (var domain in recipe.Items)
    {
        var a = uv[domain.A]; var b = uv[domain.B]; var c = uv[domain.C];
        var measured = (render, mesh, MovableStructureKey.For("", domain.Triangle, domain.Bounds, a, b, c));
        if (!reached.Contains(measured)) continue;
        unresolved.Remove(measured);
        string key = MovableStructureKey.For(digest, domain.Triangle, domain.Bounds, a, b, c);
        units[(style, key)] = new { style, sourceCid = cid, sourceDigest = digest, key, triangle = domain.Triangle,
            bounds = new[] { domain.Bounds.x, domain.Bounds.y, domain.Bounds.z, domain.Bounds.w },
            a = new[] { a.x, a.y }, b = new[] { b.x, b.y }, c = new[] { c.x, c.y } };
        domains++;
    }
}
if (unresolved.Count != 0) throw new InvalidDataException($"{unresolved.Count} used samplers are absent from immutable source domains: {unresolved.First()}");
using var library = JsonDocument.Parse(File.ReadAllBytes(args[5]));
var templates = library.RootElement.EnumerateArray().ToDictionary(t => t.GetProperty("hash").GetInt32());
var sourceCids = units.Values.Select(u => JsonSerializer.SerializeToElement(u).GetProperty("sourceCid").GetString()!).Distinct().Order();
var sourceDescriptions = sourceCids.Select(cid => new { cid, template = BitConverter.ToInt32(sources[cid], 2),
    channels = templates[BitConverter.ToInt32(sources[cid], 2)].GetProperty("stacks").EnumerateArray()
        .SelectMany((s, stack) => s.EnumerateArray().Select((p, layer) => new { stack, layer, name = p.GetString()! })).ToArray() }).ToArray();
File.WriteAllText(args[6], JsonSerializer.Serialize(new { recipe = MovableStructureKey.Recipe,
    auditDigest = MovableStructureKey.Digest(File.ReadAllBytes(args[2])),
    geometryDigest = MovableStructureKey.Digest(File.ReadAllBytes(args[3])),
    samplerDigest = MovableStructureKey.Digest(File.ReadAllBytes(args[4])),
    libraryDigest = MovableStructureKey.Digest(File.ReadAllBytes(args[5])),
    sources = sourceDescriptions,
    variableSamples,
    units = units.OrderBy(p => p.Key.Item1).ThenBy(p => p.Key.Item2).Select(p => p.Value) }));
Console.WriteLine($"PASS structural source inventory: {inputs} meshes; {domains} reached immutable domains; {units.Count} distinct style/sampler units; {variableSamples} receiver-dependent fragment samples excluded.");
