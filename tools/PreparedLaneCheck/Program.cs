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
foreach (var style in Directory.GetDirectories(root).Where(path => Directory.Exists(Path.Combine(path,"Lanes"))))
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
        Require(ReferenceEquals(Required(originalObject, "owner", "unused", null, owned), originalObject)
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
            var source = SurfaceAsset.Read(data); source.id = Guid.NewGuid().ToString(); source.name="Unlisted source identity"; var owned = new List<AssetData>(); int before = Database.Created;
            var result = Required(source, "owner", styleId, preview ? new PreviewGeometry() : null, owned);
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
                    var changed = Required(source, "owner", styleId, null, changedOwned);
                    Require(changedOwned.Count == 1 && changed.Saves == 1 && changed.floats["_Snow"].Equals(defaults["_Snow"]), "One-ULP native default change keeps distinct appearance");
                    Require(source.Leases == 0, "Changed-default source lease balanced");
                    Require(changed.Bytes.Skip(6).SequenceEqual(data.Skip(6)) && source.Bytes.SequenceEqual(data), "Full native header/VT/body copied and source bytes preserved");
                    Require(source.ints.All(p=>changed.ints[p.Key]==p.Value) && source.vectors.All(p=>changed.vectors[p.Key]==p.Value)
                        && source.colors.All(p=>changed.colors[p.Key]==p.Value) && source.textures.All(p=>changed.textures[p.Key]?.id==p.Value?.id)
                        && source.floats.All(p=>changed.floats[p.Key].Equals(p.Value)) && source.keywords.All(changed.keywords.Contains),
                        "All serialized source fields preserved in changed-default descriptor");
                }
                finally { defaults["_Snow"] = snow; }
            }
            cases++;
        }
    }
    }
}
var unknown = new SurfaceAsset { id = Guid.NewGuid().ToString(), Bytes = new byte[]{1,0,17,0,0,0,0} };
int allocated = Database.Created;
Require(!MovableLaneSurface.TryCreate(unknown,"owner","Draw02",null,new(),out var unresolved,out var missing)
    && unresolved == null && missing!.Contains("representation incomplete") && Database.Created == allocated,
    "Unknown valid shader must return a missing-adapter result without claiming corruption/success");
var broken = new SurfaceAsset { id = Guid.NewGuid().ToString(), Bytes = Array.Empty<byte>() };
Require(!MovableLaneSurface.TryCreate(broken,"owner","Draw02",null,new(),out unresolved,out missing)
    && unresolved == null && missing!.Contains("source-header"),"Broken native header must not escape");
unknown.Bytes[2]=16; unknown.FailRead=true;
Require(!MovableLaneSurface.TryCreate(unknown,"owner","Draw02",null,new(),out unresolved,out missing)
    && unresolved == null && missing!.Contains("source-descriptor-read") && missing.Contains("owner=owner") && missing.Contains("style=Draw02") && missing.Contains(unknown.id) && Database.Created == allocated,"IO failure result before registration");

Console.WriteLine($"PASS production lane routing: {cases} preview/formal source cases; direct original reuse, zero matching allocations, exact default matching and balanced source leases.");
static void Require(bool value, string message) { if (!value) throw new Exception(message); }

static SurfaceAsset Required(SurfaceAsset source, string owner, string style, PreviewGeometry? preview, List<AssetData> assets)
{
    Require(MovableLaneSurface.TryCreate(source, owner, style, preview, assets, out var result, out var error), error ?? "missing failure details");
    Require(result != null && error == null, "success result must be complete");
    return result!;
}
