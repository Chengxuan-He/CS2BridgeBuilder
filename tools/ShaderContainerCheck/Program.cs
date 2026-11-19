using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using System.Text;
foreach (var name in new[] { "BoneDecal", "WorldLayeredPbr", "NetworkPalette" })
{
    var native = File.ReadAllBytes("assets/MaterialPrograms/" + name + ".bundle");
    if (native.Length < 8 || Encoding.ASCII.GetString(native, 0, 8) != "UnityFS\0") throw new Exception("Packaged program is not a native bundle");
    int sourceTemplate = name == "BoneDecal" ? 18 : name == "WorldLayeredPbr" ? 17 : BridgeNativeMaterialProgram.NetworkPalette;
    var allowed = new HashSet<int>();
    foreach (int offset in new[] { BridgeNativeMaterialProgram.HashOffset(sourceTemplate), BridgeNativeMaterialProgram.QueueOffset(sourceTemplate) })
        for (int i = 0; i < 4; i++) allowed.Add(offset + i);
    foreach (int offset in BridgeNativeMaterialProgram.IdentityOffsets(sourceTemplate))
        for (int i = 0; i < 32; i++) allowed.Add(offset + i);
    var identities = new HashSet<int>();
    foreach (int queue in new[] { 1999, 2000, 2007, 2473 })
    {
        var configured = BridgeNativeMaterialProgram.Configure(native, sourceTemplate, queue);
        if (!configured.SequenceEqual(BridgeNativeMaterialProgram.Configure(native, sourceTemplate, queue))) throw new Exception("Configuration is nondeterministic");
        if (configured.Length != native.Length || configured.Where((value, i) => value != native[i] && !allowed.Contains(i)).Any()) throw new Exception("Configuration changed native graph content outside its scalar identity/queue fields");
        if (BitConverter.ToInt32(configured, BridgeNativeMaterialProgram.QueueOffset(sourceTemplate)) != queue) throw new Exception("Queue mismatch");
        if (!identities.Add(BitConverter.ToInt32(configured, BridgeNativeMaterialProgram.HashOffset(sourceTemplate)))) throw new Exception("Queue variants share a template identity");
        if (args.Length > 0 && args[0] != "--verify-only") { Directory.CreateDirectory(args[0]); File.WriteAllBytes(Path.Combine(args[0], name + "-" + queue + ".bundle"), configured); }
    }
    Console.WriteLine($"PASS {name}: native graph scalar-only variants, exact other bytes, deterministic distinct identities");
}
foreach (int source in new[] {BridgeNativeMaterialProgram.NetworkPalette,17,18})
foreach (int queue in new[] {int.MinValue, -1, 0, 2000, 2007, int.MaxValue})
{
    int id=BridgeNativeMaterialProgram.TemplateId(source,queue);
    if (!BridgeNativeMaterialProgram.TryIdentify(id,queue,out var resolved) || resolved!=source) throw new Exception("Stable family/queue identity failed");
    var bytes=File.ReadAllBytes("assets/MaterialPrograms/"+BridgeNativeMaterialProgram.ProgramName(source)+".bundle");
    bytes[bytes.Length-1]^=1;
    var patched=BridgeNativeMaterialProgram.Configure(bytes,source,queue);
    if(BitConverter.ToInt32(patched,BridgeNativeMaterialProgram.HashOffset(source))!=id)throw new Exception("Identity changed with compiled bytes");
    using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
    w.Write((ushort)1);w.Write(id);w.Write(false);
    w.Write((byte)255);w.Write(1);w.Write("_DrawOrder");w.Write(7f);
    w.Write((byte)255);w.Write(1);w.Write(BridgeNativeMaterialProgram.QueueProperty);w.Write(queue);w.Flush();
    if(!BridgeCurrentProgramSurface.TryReadSurfaceIdentity(stream.ToArray(),out var template,out var actualQueue,out var order,out var error)||template!=id||actualQueue!=queue||order!=7f)throw new Exception("Native Surface identity: "+error);
    if(BridgeCurrentProgramSurface.TryReadSurfaceIdentity(stream.ToArray()[..^1],out _,out _,out _,out _))throw new Exception("Truncated Surface accepted");
}
Console.WriteLine("PASS content-independent signed-queue identities and native Surface metadata without Material realization");

foreach (bool vt in new[] {false,true})
{
    using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
    int id=BridgeNativeMaterialProgram.TemplateId(18,2007);
    w.Write((ushort)1);w.Write(id);w.Write(vt);
    if(vt){w.Write(1);w.Write(new byte[272]);w.Write((byte)255);w.Write(new byte[16]);}
    w.Write((byte)0);w.Write((byte)255);w.Write(1);w.Write(BridgeNativeMaterialProgram.QueueProperty);w.Write(2007);w.Flush();
    if(!BridgeCurrentProgramSurface.TryReadSurfaceIdentity(stream.ToArray(),out int template,out var queue,out var order,out var error)||template!=id||queue!=2007||order!=null)throw new Exception("VT/plain Surface queue: "+error);
}
foreach(var file in new[]{"src/BridgeBuilder/Bridges/Common/Movable/MovableCustomShaderAsset.cs", "src/BridgeBuilder/Runtime/BridgeCustomShaderRestoration.cs"})
{
    string source=File.ReadAllText(file);
    if(source.Contains("AddAsset<FileAsset>")||source.Contains("SearchFilter<FileAsset>")||source.Contains("TryPersist("))throw new Exception("Owner program IO returned: "+file);
}
if(File.ReadAllText("src/BridgeBuilder/Runtime/BridgeDependencyPersistence.cs").Contains("ProgramRoots"))throw new Exception("Owner program dependency roots returned");
Console.WriteLine("PASS VT/plain Surface prefix, no program writer/reader or dependency roots");

string mod = File.ReadAllText("src/BridgeBuilder/Mod.cs");
if (!mod.Contains("BridgeCustomShaderRestoration.TryRestore(") ||
    mod.Contains("MovableCustomShaderAsset.TryRegisterCurrent("))
    throw new Exception("OnLoad must run shared default and owned Surface restoration");
string defaults = File.ReadAllText("src/BridgeBuilder/Bridges/Common/Movable/MovableCustomShaderAsset.cs");
if (!defaults.Contains("BridgeNativeMaterialProgram.NetworkPalette, 17, 18") ||
    !defaults.Contains("BridgeNativeMaterialProgram.DefaultQueue(source)") || defaults.Contains("SearchFilter<SurfaceAsset>"))
    throw new Exception("Default registration must retain exactly three program families without Surface discovery");
foreach (int family in new[] { BridgeNativeMaterialProgram.NetworkPalette, 17, 18 })
    if (BridgeNativeMaterialProgram.DefaultQueue(family) != (family == 18 ? 2000 : 2225))
        throw new Exception("Default queue changed");
foreach (var file in new[] { "src/BridgeBuilder/Systems/BridgeStartupAssetSystem.cs" })
{
    string source = File.ReadAllText(file);
    if (!source.Contains("BridgeCustomShaderRestoration.TryRestore("))
        throw new Exception("Startup shader registration missing: " + file);
}
foreach (var file in new[] { "Geometry/TowerFactory.NetworkMaterials.cs", "Movable/MovableMaterialConversion.cs", "Movable/MovableRigidDecalMaterial.cs" })
{
    string source = File.ReadAllText("src/BridgeBuilder/Bridges/Common/" + file);
    if (!source.Contains("MovableCustomShaderAsset.TryPrepare(") || source.Contains("MovableCustomShaderAsset.TryResolve("))
        throw new Exception("On-demand registration caller missing: " + file);
}
string restoration = File.ReadAllText("src/BridgeBuilder/Runtime/BridgeCustomShaderRestoration.cs");
foreach (string required in new[] { "MovableCustomShaderAsset.TryRegisterCurrent(", "SearchFilter<SurfaceAsset>", "BridgeAssetInfo.TryFileOwner(", "BridgeCurrentProgramSurface.TryReadSurfaceIdentity(", "MovableCustomShaderAsset.TryPrepare(" })
    if (!restoration.Contains(required)) throw new Exception("Three-stage restoration missing: " + required);
Console.WriteLine("PASS three-stage registration: OnLoad and game-load owned Surface restoration plus on-demand registration");
