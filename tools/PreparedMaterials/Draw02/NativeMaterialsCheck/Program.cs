using System.Text.Json;
using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;

if (args.Length == 2 && args[0] == "--network")
{
    using var network = JsonDocument.Parse(File.ReadAllBytes(args[1]));
    int count = 0;
    foreach (var source in network.RootElement.GetProperty("sources").EnumerateArray())
    {
        string render = source.GetProperty("render").GetString()!;
        int mesh = source.GetProperty("mesh").GetInt32();
        string cid = source.GetProperty("sourceCid").GetString()!;
        if (!Draw02NativeMaterials.IsNativeNetwork(render, mesh, cid) ||
            Draw02NativeMaterials.IsNativeNetwork(render, mesh + 1, cid) ||
            Draw02NativeMaterials.IsNativeNetwork(render, mesh, "incorrect") ||
            Draw02NativeMaterials.IsNativeNetwork(render + " unrelated", mesh, cid) ||
            Draw02NativeMaterials.IsNativeNetwork("DrawBridge02 Mesh", mesh, cid))
            throw new Exception("Native network identity gate differs from independently verified archive evidence.");
        count++;
    }
    if (count != 19) throw new Exception("Incomplete native network source coverage.");
    Console.WriteLine($"PASS Draw02 native network identity: {count} exact slots; wrong CID/slot/render and moving object rejected. Graph and shader execution are tested separately.");
    return;
}

using var report = JsonDocument.Parse(File.ReadAllBytes(args[0]));
int converted = 0, retained = 0;
foreach (var entry in report.RootElement.GetProperty("roots").EnumerateArray())
{
    string operation = entry.GetProperty("operation").GetString()!;
    string source = entry.GetProperty("sourceCid").GetString()!;
    if (operation == "native-vt-paint-descriptor")
    {
        if (Draw02NativeMaterials.ObjectDescriptor(source) != null) throw new Exception("Paint incorrectly routed as structure.");
        continue;
    }
    var original = new SurfaceAsset(source, entry.GetProperty("sourceTemplate").GetInt32(), true);
    int calls = 0;
    SurfaceAsset Load(string cid)
    {
        calls++;
        if (cid != entry.GetProperty("cid").GetString()) throw new Exception("CID differs from native producer.");
        return new SurfaceAsset(cid, entry.GetProperty("targetTemplate").GetInt32(), false);
    }
    var result = Draw02NativeMaterials.Resolve(original, Load);
    if (operation == "builtin-reference")
    {
        if (!ReferenceEquals(original, result) || calls != 0 || original.HeaderLoads != 0)
            throw new Exception("Compatible source was replaced or loaded unnecessarily.");
        retained++;
    }
    else
    {
        if (calls != 1 || ReferenceEquals(original, result) || original.HeaderLoads != 1 || result.HeaderLoads != 1)
            throw new Exception("Conversion did not validate both native headers.");
        var path = Path.Combine(args[1], entry.GetProperty("path").GetString()!);
        if (File.ReadAllText(path + ".cid").Trim() != result.id.guid) throw new Exception("Prepared sidecar differs.");
        foreach (var invalid in new[] { new SurfaceAsset(source, 0, true), new SurfaceAsset(source, 17, false) })
            Reject(() => Draw02NativeMaterials.Resolve(invalid, _ => throw new Exception("Invalid source reached loader.")));
        foreach (var invalid in new[] { new SurfaceAsset("incorrect", 0, false), new SurfaceAsset(result.id.guid, 17, false), new SurfaceAsset(result.id.guid, 0, false, true) })
            Reject(() => Draw02NativeMaterials.Resolve(original, _ => invalid));
        converted++;
    }
}
if (converted != 6 || retained != 8) throw new Exception("Incomplete Draw02 inventory coverage.");
Console.WriteLine($"PASS Draw02 native resolver: {converted} converted sources, {retained} exact retained sources; producer CIDs and descriptor sidecars match; invalid identity/template/VT rejected.");

static void Reject(Action operation)
{
    try { operation(); }
    catch (InvalidOperationException) { return; }
    throw new Exception("Invalid native descriptor was accepted.");
}

namespace Colossal.IO.AssetDatabase
{
    public sealed class Identifier(string identity)
    {
        public readonly string guid = identity;
        public override string ToString() => guid + " [assetdb://draw02-check/Surface]";
    }

    // Control-flow seam only; actual native serialization is verified by
    // Test-NativeRebuild.ps1 against the installed game readers.
    public sealed class SurfaceAsset(string identity, int template, bool builtin, bool vt = false)
    {
        public Identifier id = new(identity);
        public int materialTemplateHash = template;
        public bool isBuiltin = builtin, isVTMaterial = vt;
        public int HeaderLoads;
        public void LoadHeader() => HeaderLoads++;
    }
}
