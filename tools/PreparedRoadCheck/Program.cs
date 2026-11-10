using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

int cases = 0;
foreach (var folder in Directory.GetDirectories(Path.GetFullPath(args[0])))
{
    string style = Path.GetFileName(folder);
    var surfacePath = Path.Combine(folder, "Road/NativeAsphalt.Surface");
    if (!File.Exists(surfacePath)) continue;
    var prepared = Read(surfacePath);
    BridgePreparedMaterials.Style = style; BridgePreparedMaterials.Prepared = prepared;
    foreach (bool preview in new[] { false, true })
    foreach (float? scale in new float?[] { null, 0f, -1f, .03125f, .125f, .5f })
    {
        SurfaceAsset Source()
        {
            var source = new SurfaceAsset();
            foreach (var p in new[] { ("_WorldspaceAlbedo", "_BaseColorMap"), ("_WorldspaceNormalMap", "_NormalMap"), ("_WorldspaceMaskMap", "_MaskMap") })
                source.textures[p.Item1] = new TextureAsset { id = prepared.textures[p.Item2].id };
            if (scale.HasValue) source.floats["_WorldspaceUVScale"] = scale.Value;
            return source;
        }
        var capture = preview ? new PreviewGeometry() : null;
        var source = Source(); var owned = new List<AssetData>(); int before = Database.Created;
        Require(MovableRoadSurface.TryCreate("test-owner", style, source, capture, owned, out var result, out var uv, out var asphalt, out var error), "Source road result: " + error);
        Require(ReferenceEquals(result, prepared) && owned.Count == 0 && Database.Created == before, "Prepared road creates no material or texture");
        Require(uv.Equals(scale ?? .125f) && ReferenceEquals(asphalt, source.textures["_WorldspaceAlbedo"]), "Original density and source texture preserved");
        Require(source.Leases == 0 && prepared.Leases == 0, "Balanced source lease; catalogue lease untouched");
        foreach (var property in new[] { "_WorldspaceAlbedo", "_WorldspaceNormalMap", "_WorldspaceMaskMap" })
        {
            var changed = Source(); changed.textures[property] = new TextureAsset { id = Guid.NewGuid().ToString("N") };
            var privateAssets = new List<AssetData>();
            Require(MovableRoadSurface.TryCreate("test-owner", style, changed, capture, privateAssets, out var different, out _, out _, out error), "Changed road result: " + error);
            Require(privateAssets.Count == 1 && ReferenceEquals(privateAssets[0], different) && different.Saves == 1, "Different source texture retains private descriptor");
            Require(ReferenceEquals(different.textures["_ControlMask"], prepared.textures["_ControlMask"]), "Prepared control is shared, never owned");
            Require(different.textures.Values.Any(t => t.id == changed.textures[property].id) && changed.Leases == 0, "Changed texture reference retained");
        }
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity })
        {
            var invalid = Source(); invalid.floats["_WorldspaceUVScale"] = bad; int reads = BridgePreparedMaterials.Reads;
            Require(!MovableRoadSurface.TryCreate("test-owner", style, invalid, capture, new(), out var rejected, out _, out _, out error) && rejected == null && error != null && error.Contains("owner=test-owner") && error.Contains("style="+style), "Diagnosed result: Invalid density accepted");
            Require(invalid.Leases == 0 && BridgePreparedMaterials.Reads == reads, "Reject invalid density before catalogue access, release source lease");
        }
        foreach (bool missing in new[] { false, true })
        {
            var invalid = Source(); int reads = BridgePreparedMaterials.Reads;
            if (missing) invalid.textures.Remove("_WorldspaceNormalMap");
            else invalid.textures["_WorldspaceNormalMap"].isDummy = true;
            Require(!MovableRoadSurface.TryCreate("test-owner", style, invalid, capture, new(), out var rejected, out _, out _, out error) && rejected == null && error != null && error.Contains("owner=test-owner") && error.Contains("style="+style), "Diagnosed result: Missing texture accepted");
            Require(invalid.Leases == 0 && BridgePreparedMaterials.Reads == reads, "Missing/dummy texture fails before binding and releases lease");
        }
        foreach(var fault in new[]{"load","save-false","save-throw","unload"}) {
            var invalid=Source(); invalid.textures["_WorldspaceAlbedo"]=new TextureAsset {id=Guid.NewGuid().ToString("N")};
            var partial=new List<AssetData>(); SurfaceAsset.Fault=fault;
            try {
                Require(!MovableRoadSurface.TryCreate("test-owner",style,invalid,capture,partial,out var failed,out var failedScale,out var failedTexture,out error)
                    && failed==null && failedTexture==null && failedScale==0 && error!=null,"Native fault must not expose partial success: "+fault);
                Require(invalid.Leases==0 && partial.Cast<SurfaceAsset>().All(x=>x.Leases==0),"Native fault must balance source/result leases: "+fault);
            } finally {SurfaceAsset.Fault="";}
        }
        cases++;
    }
}
Console.WriteLine($"PASS production road binding: {cases} style/preview/density cases; zero matching allocation; original three texture identities and density; changed textures, immutable control, invalid-density cleanup.");
static SurfaceAsset Read(string path)
{
    using var r = new BinaryReader(File.OpenRead(path));
    Require(r.ReadUInt16() == 1 && r.ReadInt32() == 0 && !r.ReadBoolean(), "native non-VT road descriptor");
    var result = new SurfaceAsset { materialTemplateHash = 0 };
    int Count() { Require(r.ReadByte() == 255, "native property map"); return r.ReadInt32(); }
    for (int n = Count(); n > 0; n--) result.floats.Add(r.ReadString(), r.ReadSingle());
    Require(Count() == 0 && Count() == 0 && Count() == 0, "native empty maps");
    for (int n = Count(); n > 0; n--)
    {
        var property = r.ReadString(); Require(r.ReadByte() == 255, "native reference");
        var id = string.Concat(r.ReadBytes(16).Select(b => (b & 15).ToString("x") + (b >> 4).ToString("x")));
        result.textures.Add(property, new TextureAsset { id = id });
    }
    for (int n = Count(); n > 0; n--) result.keywords.Add(r.ReadString());
    Require(r.BaseStream.Position == r.BaseStream.Length, "native file consumed"); return result;
}
static void Require(bool ok, string reason) { if (!ok) throw new Exception(reason); }
