using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

if (args.Length != 2 && !(args.Length == 3 && args[2] == "--check"))
    throw new ArgumentException("Usage: PrepareRoad <Cities2_Data> <assets/Materials> [--check]");
const string sourceCid = "1d7dad9f64c9fe55a809f1080c39fb92";
const string recipe = "BridgeBuilder/SkinnedWorldspaceAsphalt/1";
using var archive = ZipFile.OpenRead(Path.Combine(args[0], "Content/Game/Blob.cok"));
var sources = new Dictionary<string, ZipArchiveEntry>();
foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".cid", StringComparison.Ordinal)))
{
    using var text = new StreamReader(entry.Open());
    var payload = archive.GetEntry(entry.FullName[..^4]);
    if (payload != null) sources[text.ReadToEnd().Trim()] = payload;
}
using var input = sources[sourceCid].Open(); using var bytes = new MemoryStream(); input.CopyTo(bytes);
var source = bytes.ToArray();
using var r = new BinaryReader(new MemoryStream(source));
Require(r.ReadUInt16() == 1 && r.ReadInt32() == 17, "Expected native worldspace road source.");
if (r.ReadBoolean()) { r.BaseStream.Position += 276; if (Present()) r.BaseStream.Position += 16; }
var floats = new Dictionary<string, float>();
for (int n = Count(); n > 0; n--) floats.Add(r.ReadString(), r.ReadSingle());
foreach (int size in new[] { 4, 16, 16 })
    for (int n = Count(); n > 0; n--) { r.ReadString(); r.BaseStream.Position += size; }
var textures = new Dictionary<string, byte[]>();
for (int n = Count(); n > 0; n--) { var key = r.ReadString(); if (Present()) textures.Add(key, r.ReadBytes(16)); }
for (int n = Count(); n > 0; n--) r.ReadString();
Require(r.BaseStream.Position == source.Length, "Unconsumed native descriptor.");
var bindings = new Dictionary<string, byte[]>();
foreach (var pair in new[] { ("_WorldspaceAlbedo", "_BaseColorMap"), ("_WorldspaceNormalMap", "_NormalMap"), ("_WorldspaceMaskMap", "_MaskMap") })
{
    Require(textures.ContainsKey(pair.Item1), "Missing native worldspace texture.");
    var id = textures[pair.Item1];
    Require(sources.ContainsKey(Hash(id)), "Worldspace texture is not built-in.");
    bindings.Add(pair.Item2, id);
}
var files = new Dictionary<string, (string Cid, byte[] Bytes)>();
string Add(string name, byte[] data)
{
    var cid = Digest(Encoding.UTF8.GetBytes(recipe + "/" + sourceCid + "/" + Digest(source) + "/" + Digest(data)))[..32];
    files.Add(name, (cid, data)); return cid;
}
var expectedControl = Bytes(w => {
    w.Write((ushort)5); w.Write((ushort)1); w.Write((ushort)1); w.Write((ushort)1);
    w.Write((byte)1); w.Write((byte)8); w.Write((byte)2);
    w.Write((byte)1); w.Write((byte)0); w.Write((byte)1); w.Write(-1); w.Write(new byte[4]);
});
// Reuse the already prepared constant control identity, not another material's
// generated snapshot. Each Road variant still carries a complete local copy.
var controlPath = Path.Combine(Path.GetFullPath(args[1]), "Draw01/Cable/ZeroControl.Texture");
var controlBytes = File.ReadAllBytes(controlPath);
var controlCid = File.ReadAllText(controlPath + ".cid").Trim();
Require(controlBytes.SequenceEqual(expectedControl), "Prepared control sampling/encoding differs.");
Require(System.Text.RegularExpressions.Regex.IsMatch(controlCid, "^[a-f0-9]{32}$"), "Invalid prepared control identity.");
files.Add("ZeroControl.Texture", (controlCid, controlBytes));
bindings.Add("_ControlMask", Enumerable.Range(0, 16).Select(i => (byte)(Convert.ToInt32(controlCid[i * 2].ToString(), 16)
    | Convert.ToInt32(controlCid[i * 2 + 1].ToString(), 16) << 4)).ToArray());
Add("NativeAsphalt.Surface", Bytes(w => {
    void Map(int count) { w.Write((byte)255); w.Write(count); }
    w.Write((ushort)1); w.Write(0); w.Write(false);
    Map(1); w.Write("_Snow"); w.Write(0f); Map(0); Map(0); Map(0);
    Map(bindings.Count); foreach (var binding in bindings) { w.Write(binding.Key); w.Write((byte)255); w.Write(binding.Value); }
    Map(1); w.Write("_TANGENTSPACE_OCTO");
}));
foreach (var style in new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" })
foreach (var file in files)
{
    var path = Path.Combine(Path.GetFullPath(args[1]), style, "Road", file.Key);
    if (args.Length == 3)
    {
        Require(File.ReadAllBytes(path).SequenceEqual(file.Value.Bytes), "Prepared road payload differs: " + path);
        Require(File.ReadAllText(path + ".cid") == file.Value.Cid, "Prepared road identity differs: " + path);
    }
    else
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, file.Value.Bytes); File.WriteAllText(path + ".cid", file.Value.Cid, new UTF8Encoding(false));
    }
}
Console.WriteLine($"PASS prepared asphalt: sourceSHA256={Digest(source)}; surface={files["NativeAsphalt.Surface"].Cid}; bytes/style={files.Values.Sum(f => f.Bytes.Length + 32)}; three unchanged built-in texture references; no texture baking.");
foreach (var binding in bindings) Console.WriteLine(binding.Key + "=" + Hash(binding.Value));
bool Present() => r.ReadByte() switch { 0 => false, 255 => true, _ => throw new InvalidDataException("Native marker") };
int Count() => Present() ? r.ReadInt32() : 0;
static string Hash(byte[] bytes) => string.Concat(bytes.Select(b => (b & 15).ToString("x") + (b >> 4).ToString("x")));
static byte[] Bytes(Action<BinaryWriter> write) { using var b = new MemoryStream(); using var w = new BinaryWriter(b); write(w); return b.ToArray(); }
static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
