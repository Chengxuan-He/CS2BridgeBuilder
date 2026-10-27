using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

var root = Path.GetFullPath(args[0]);
using var templates = JsonDocument.Parse(File.ReadAllBytes(args[1]));
var library = AssetDatabase.global.resources.materialLibrary;
foreach (var t in templates.RootElement.EnumerateArray())
{
    int id = t.GetProperty("hash").GetInt32();
    library.Descriptions[id] = new Description { m_Stacks = t.GetProperty("stacks").EnumerateArray()
        .Select(s => new Stack { m_PropertyNames = s.EnumerateArray().Select(p => p.GetString()!).ToArray() }).ToArray() };
    library.Templates[id] = new Template { Floats = t.GetProperty("material").GetProperty("m_SavedProperties").GetProperty("m_Floats")
        .EnumerateArray().ToDictionary(p => p[0].GetString()!, p => p[1].GetSingle()) };
}
int cases = 0;
foreach (var style in Directory.GetDirectories(root))
{
    string styleId = Path.GetFileName(style);
    BridgePreparedMaterials.Lookup = (requested, bytes) => {
        Require(requested == styleId, "Correct style");
        var path = Path.Combine(style, "Lanes", Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), "SkinnedLane.Surface");
        return File.Exists(path) ? SurfaceAsset.Read(File.ReadAllBytes(path)) : null;
    };
    foreach (var file in Directory.GetFiles(Path.Combine(style, "Lanes"), "*.Surface", SearchOption.AllDirectories))
    {
        var originalObject = SurfaceAsset.Read(File.ReadAllBytes(file));
        var owned = new List<AssetData>();
        Require(ReferenceEquals(MovableLaneSurface.Create(originalObject, "owner", "unused", null, owned), originalObject)
            && originalObject.Reads == 0 && owned.Count == 0, "Compatible object directly reused before catalogue access");
    }
    foreach (var archivePath in new[] { "Game", "BridgesAndPorts" }
        .SelectMany(family => Directory.GetFiles(Path.Combine(args[2], "Content", family), "Blob*.cok")))
    {
    using var archive = ZipFile.OpenRead(archivePath);
    foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".Surface") || e.FullName.EndsWith(".Material")))
    {
        using var input = entry.Open(); using var bytes = new MemoryStream(); input.CopyTo(bytes);
        var data = bytes.ToArray(); if (BitConverter.ToInt32(data, 2) is not (16 or 31 or 34)) continue;
        foreach (bool preview in new[] { false, true })
        {
            var source = SurfaceAsset.Read(data); var owned = new List<AssetData>(); int before = Database.Created;
            var result = MovableLaneSurface.Create(source, "owner", styleId, preview ? new PreviewGeometry() : null, owned);
            Require(Database.Created == before && owned.Count == 0 && result.Saves == 0, "Native lane binds prepared descriptor without allocating/saving");
            Require(source.Leases == 0 && source.Reads == 1, "Exactly one descriptor read and balanced lease");
            // Changes to template defaults must not silently bind an outdated conversion.
            var defaults = library.Templates[source.materialTemplateHash].Floats;
            if (!source.floats.ContainsKey("_Snow"))
            {
                var snow = defaults["_Snow"]; defaults["_Snow"] = MathF.BitIncrement(snow);
                try
                {
                    var changedOwned = new List<AssetData>();
                    var changed = MovableLaneSurface.Create(source, "owner", styleId, null, changedOwned);
                    Require(changedOwned.Count == 1 && changed.Saves == 1 && changed.floats["_Snow"].Equals(defaults["_Snow"]), "One-ULP native default change keeps distinct appearance");
                    Require(source.Leases == 0, "Changed-default source lease balanced");
                }
                finally { defaults["_Snow"] = snow; }
            }
            cases++;
        }
    }
    }
}
Console.WriteLine($"PASS production lane routing: {cases} preview/formal source cases; direct original reuse, zero matching allocations, exact default matching and balanced source leases.");
static void Require(bool value, string message) { if (!value) throw new Exception(message); }
