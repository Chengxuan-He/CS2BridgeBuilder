using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Offline authoring only. No Unity scene or runtime-generated bridge is read.
if (args.Length != 3) throw new ArgumentException("Usage: PrepareCable <Cities2_Data> <native-template-30.json> <assets/Materials>");
const string sourceCid = "106cc56e9f15a135bb6ea7b99d675c2d";
const string recipe = "BridgeBuilder/SkinnedCable/1";
var source = ReadAsset(args[0], sourceCid);
Require(Digest(source) == "8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219",
    "Native cable descriptor changed; review the measured conversion recipe.");
var templateBytes = File.ReadAllBytes(args[1]);
Require(Digest(templateBytes) == "59148636eb4c18381319292be459c9b4077713741e3925cc89e22e80ac38b6c8",
    "Native template evidence changed; remeasure defaults before preparation.");
using var template = JsonDocument.Parse(templateBytes);
Require(template.RootElement.GetProperty("m_Name").GetString() == "CurvedTransparent", "Expected native template 30.");
var defaults = template.RootElement.GetProperty("m_SavedProperties");
var defaultFloats = defaults.GetProperty("m_Floats").EnumerateArray().ToDictionary(p => p[0].GetString()!, p => p[1].GetSingle());
var defaultColors = defaults.GetProperty("m_Colors").EnumerateArray().ToDictionary(p => p[0].GetString()!, p =>
    new[] { "r", "g", "b", "a" }.Select(c => p[1].GetProperty(c).GetSingle()).ToArray());
using var reader = new BinaryReader(new MemoryStream(source));
Require(reader.ReadUInt16() == 1 && reader.ReadInt32() == 30 && !reader.ReadBoolean(), "Expected textureless native cable.");
int Count() { Require(reader.ReadByte() == 255, "Native property-map marker."); return reader.ReadInt32(); }
var floats = new Dictionary<string, float>();
for (int i = Count(); i > 0; i--) floats.Add(reader.ReadString(), reader.ReadSingle());
Require(Count() == 0 && Count() == 0, "Review cable integer/vector overrides before conversion.");
var colors = new Dictionary<string, float[]>();
for (int i = Count(); i > 0; i--) colors.Add(reader.ReadString(), Enumerable.Range(0, 4).Select(_ => reader.ReadSingle()).ToArray());
Require(Count() == 0, "Texture-bearing cable needs a different recipe.");
for (int i = Count(); i > 0; i--) Require(reader.ReadString() == "_TANGENTSPACE_OCTO", "Review cable keyword semantics.");
Require(reader.BaseStream.Position == reader.BaseStream.Length, "Entire cable source must be consumed.");
var color = colors.GetValueOrDefault("_BaseColor") ?? defaultColors["_BaseColor"];
var scalars = new[] { "_Metallic", "_Smoothness", "_Snow" }.ToDictionary(p => p, p => floats.GetValueOrDefault(p, defaultFloats[p]));
var files = new Dictionary<string, (string Cid, byte[] Bytes)>();
string Add(string name, byte[] bytes)
{
    var cid = Digest(Encoding.UTF8.GetBytes(recipe + "/" + sourceCid + "/" + Digest(source) + "/" + Digest(templateBytes) + "/" + Digest(bytes)))[..32];
    files.Add(name, (cid, bytes)); return cid;
}
// Native Texture v5: 1x1 linear RGBA8, one raw zero mip. No encoding/decoding at runtime.
var control = Bytes(w => {
    w.Write((ushort)5); w.Write((ushort)1); w.Write((ushort)1); w.Write((ushort)1);
    w.Write((byte)1); w.Write((byte)8); w.Write((byte)2); // mips, GraphicsFormat, TextureDimension
    w.Write((byte)1); w.Write((byte)0); w.Write((byte)1); // bilinear, repeat, anisotropy
    w.Write(-1); w.Write(new byte[4]);
});
var controlCid = Add("ZeroControl.Texture", control);
Add("SkinnedCable.Surface", Bytes(w => {
    void Map(int count) { w.Write((byte)255); w.Write(count); }
    w.Write((ushort)1); w.Write(0); w.Write(false); // Default, non-VT
    Map(scalars.Count); foreach (var pair in scalars) { w.Write(pair.Key); w.Write(pair.Value); }
    Map(0); Map(0); // integer/vector overrides
    Map(1); w.Write("_BaseColor"); foreach (var channel in color) w.Write(channel);
    Map(1); w.Write("_ControlMask"); w.Write((byte)255);
    w.Write(Enumerable.Range(0, 16).Select(i => (byte)(Convert.ToInt32(controlCid[i * 2].ToString(), 16)
        | Convert.ToInt32(controlCid[i * 2 + 1].ToString(), 16) << 4)).ToArray());
    Map(1); w.Write("_TANGENTSPACE_OCTO");
}));
var styles = new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" };
foreach (var style in styles)
foreach (var file in files)
{
    var path = Path.Combine(Path.GetFullPath(args[2]), style, "Cable", file.Key);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, file.Value.Bytes);
    File.WriteAllText(path + ".cid", file.Value.Cid, new UTF8Encoding(false));
}
Console.WriteLine($"PASS prepared cable: {styles.Length} styles; surface={files["SkinnedCable.Surface"].Cid}; graphBytes={files.Values.Sum(f => f.Bytes.Length + 32)}; recipe={recipe}");

static byte[] ReadAsset(string game, string cid)
{
    foreach (var family in new[] { "Game", "BridgesAndPorts" })
    {
        using var archive = ZipFile.OpenRead(Path.Combine(game, "Content", family, "Blob.cok"));
        foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".cid", StringComparison.Ordinal)))
        {
            using var sidecar = new StreamReader(entry.Open());
            if (sidecar.ReadToEnd().Trim() != cid) continue;
            using var input = archive.GetEntry(entry.FullName[..^4])!.Open();
            using var result = new MemoryStream(); input.CopyTo(result); return result.ToArray();
        }
    }
    throw new FileNotFoundException("Native cable source missing: " + cid);
}
static byte[] Bytes(Action<BinaryWriter> write)
{ using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); write(writer); return stream.ToArray(); }
static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
