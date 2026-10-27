using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;

var root = Path.GetFullPath(args[0]);
foreach (var style in new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" })
{
    var prepared = Read(Path.Combine(root, style, "Cable", "SkinnedCable.Surface"));
    BridgePreparedMaterials.Style = style; BridgePreparedMaterials.Prepared = prepared;
    var color = prepared.colors["_BaseColor"];
    AssetDatabase.global.resources.materialLibrary.Template.Color = color;
    AssetDatabase.global.resources.materialLibrary.Template.Floats = prepared.floats;
    foreach (bool preview in new[] { false, true })
    foreach (bool explicitOverrides in new[] { false, true })
    {
        var source = new SurfaceAsset { id = Guid.NewGuid().ToString(), name = "Same name, independent source" };
        if (explicitOverrides) { source.colors.Add("_BaseColor", color); foreach (var p in prepared.floats) source.floats.Add(p.Key, p.Value); }
        var owned = new List<AssetData>(); var capture = preview ? new PreviewGeometry() : null;
        var before = Database.Created;
        var result = MovableCableSurface.Create(source, "tmp-owner", style, capture, owned);
        Require(ReferenceEquals(prepared, result) && owned.Count == 0 && Database.Created == before, "Equal appearance must bind prepared resource with no allocation.");
        Require(source.Leases == 0 && prepared.Leases == 0, "Source leases balance; catalogue lease is not released by the factory.");
        foreach (var property in new[] { "_BaseColor", "_Metallic", "_Smoothness", "_Snow" })
        {
            var changed = new SurfaceAsset { id = source.id, name = source.name };
            if (property == "_BaseColor") changed.colors.Add(property, new Color(MathF.BitIncrement(color.r), color.g, color.b, color.a));
            else changed.floats.Add(property, MathF.BitIncrement(prepared.floats[property]));
            var changedOwned = new List<AssetData>();
            var different = MovableCableSurface.Create(changed, "tmp-owner", style, capture, changedOwned);
            Require(!ReferenceEquals(different, prepared) && changedOwned.Count == 1 && ReferenceEquals(changedOwned[0], different), "Different source override keeps its own descriptor.");
            Require(ReferenceEquals(different.textures["_ControlMask"], prepared.textures["_ControlMask"]), "Constant control is referenced, never allocated or owned.");
            Require(different.SaveCount == 1 && changed.Leases == 0, "Changed descriptor is persisted and source lease released.");
            if (property == "_BaseColor") Require(different.colors[property].Equals(changed.colors[property]), "Exact changed color retained.");
            else Require(different.floats[property].Equals(changed.floats[property]), "Exact changed scalar retained.");
        }
    }
    foreach (bool vt in new[] { false, true })
    {
        var invalid = new SurfaceAsset { isVTMaterial = vt };
        if (!vt) invalid.textures.Add("_BaseColorMap", new TextureAsset());
        var before = Database.Created;
        try { MovableCableSurface.Create(invalid, "tmp-owner", style, null, new()); throw new Exception("Textured cable accepted."); }
        catch (NotSupportedException) { }
        Require(Database.Created == before && invalid.Leases == 0, "Unsupported cable rejects before allocation and balances source lease.");
    }
}
Console.WriteLine("PASS production cable binding: ten styles, preview/formal, source defaults/overrides, exact equality, independent changed descriptors, shared unowned control, rejection cleanup. Native rendering is checked separately.");

static SurfaceAsset Read(string file)
{
    var surface = new SurfaceAsset(); using var r = new BinaryReader(File.OpenRead(file));
    Require(r.ReadUInt16() == 1 && r.ReadInt32() == 0 && !r.ReadBoolean(), "Native object cable header.");
    int Count() { Require(r.ReadByte() == 255, "Property marker."); return r.ReadInt32(); }
    for (int i = Count(); i > 0; i--) surface.floats.Add(r.ReadString(), r.ReadSingle());
    Require(Count() == 0 && Count() == 0, "No integer/vector overrides.");
    for (int i = Count(); i > 0; i--) surface.colors.Add(r.ReadString(), new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()));
    Require(Count() == 1 && r.ReadString() == "_ControlMask" && r.ReadByte() == 255, "Zero control reference.");
    var cid = string.Concat(r.ReadBytes(16).Select(b => (b & 15).ToString("x") + (b >> 4).ToString("x")));
    Require(cid == File.ReadAllText(Path.Combine(Path.GetDirectoryName(file)!, "ZeroControl.Texture.cid")).Trim(), "Actual packaged control identity.");
    surface.textures.Add("_ControlMask", new TextureAsset()); return surface;
}
static void Require(bool ok, string why) { if (!ok) throw new Exception(why); }
