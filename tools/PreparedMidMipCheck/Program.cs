using BridgeBuilder.Runtime;

if (args.Length != 3) throw new ArgumentException("Usage: PreparedMidMipCheck <candidate-style-root> <original-style-root> <VT-fixtures>");
int required = 0, zero = 0, negative = 0;
string[] Refs(byte[] bytes) => BridgeSurfaceReferences.Read(bytes, (_, _) => true);
int Levels(string cid)
{
    var path = Path.Combine(args[2], cid + ".VTTexture");
    if (!File.Exists(path)) return 0;
    using var stream = File.OpenRead(path);
    return BridgeMidMipReferences.RequiredLevels(stream, false);
}
void MustFail(Action action)
{
    try { action(); }
    catch (InvalidDataException) { negative++; return; }
    catch (EndOfStreamException) { negative++; return; }
    throw new Exception("Malformed/missing MidMips input was accepted.");
}
foreach (var file in Directory.GetFiles(args[0], "*.Surface", SearchOption.AllDirectories))
{
    string relative = Path.GetRelativePath(args[0], Path.GetDirectoryName(file)!);
    if (relative != "Paint" && !relative.StartsWith("Lanes" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
    int levels = Refs(File.ReadAllBytes(file)).Select(Levels).DefaultIfEmpty().Max();
    var inputs = BridgePreparedMaterialFiles.Read(args[0], relative, Path.GetFileName(file), Refs, _ => { }, Levels);
    var caches = inputs.Where(i => Path.GetExtension(i.Path) == ".MidMips").ToArray();
    if (levels == 0)
    {
        if (caches.Length != 0) throw new Exception("Zero-level source gained a cache dependency.");
        zero++;
    }
    else
    {
        if (caches.Length != 1) throw new Exception("Expected one unambiguous native cache membership.");
        required++;
        MustFail(() => BridgePreparedMaterialFiles.Read(args[1], relative, Path.GetFileName(file), Refs, _ => { }, Levels));
        var bytes = File.ReadAllBytes(caches[0].Path);
        var cid = File.ReadAllText(file + ".cid").Trim();
        if (!BridgeMidMipReferences.Read(bytes).Contains(cid)) throw new Exception("Cache omitted exact Surface identity.");
    }
}
var common = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "Shared", "MidMips");
var sample = Directory.GetFiles(common, "*.MidMips", SearchOption.AllDirectories).First();
var content = File.ReadAllBytes(sample);
MustFail(() => BridgeMidMipReferences.Read(content[..^1]));
var wrongLevels = (byte[])content.Clone(); BitConverter.GetBytes(0).CopyTo(wrongLevels, 4);
MustFail(() => BridgeMidMipReferences.Read(wrongLevels));
if (required != 22 || zero != 5 || negative != 24) throw new Exception($"Coverage differs: {required}/{zero}/{negative}");
Console.WriteLine($"PASS source-linked prepared MidMips preflight: required={required}; zero-level/empty={zero}; missing/truncated/header negatives={negative}");
