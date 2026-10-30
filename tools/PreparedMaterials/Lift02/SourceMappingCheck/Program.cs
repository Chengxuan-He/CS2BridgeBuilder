using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnityEngine;

// Run the actual candidate assembly's plans and retained-axis dispatch against
// original native coordinates. This is not a rendered or cold-load acceptance test.
if (args.Length != 4) throw new ArgumentException("Expected candidate DLL, managed directory, native fixture and report path.");
var managed = Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
{
    var path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
    return File.Exists(path) ? Assembly.LoadFrom(path) : null;
};
var mod = Assembly.LoadFrom(Path.GetFullPath(args[0]));
const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
Type TypeOf(string name) => mod.GetType("BridgeBuilder.Bridges." + name, true)!;
T Field<T>(object item, string name) => (T)item.GetType().GetField(name, Members)!.GetValue(item)!;
var axes = (Func<string, int, int, int>)TypeOf("Lift02UvSampling").GetMethod("Axes", Members)!.CreateDelegate(typeof(Func<string, int, int, int>));
var dispatch = (Func<string, string, int, int, int>)TypeOf("MovableUvSampling").GetMethod("Axes", Members)!.CreateDelegate(typeof(Func<string, string, int, int, int>));
using var input = new BinaryReader(File.OpenRead(args[2]));
if (input.ReadInt32() != 2) throw new InvalidOperationException("Native fixture version must be 2.");
int inputs = input.ReadInt32(), routes = 0;
var reports = new List<object>();
for (int item = 0; item < inputs; item++)
{
    string ReadText() => Encoding.UTF8.GetString(input.ReadBytes(input.ReadInt32()));
    string render = ReadText(), implementation = ReadText();
    int family = input.ReadInt32(), lod = input.ReadInt32(), mesh = input.ReadInt32();
    int count = input.ReadInt32(), indexCount = input.ReadInt32();
    var source = new Vector3[count]; var uv = new Vector2[count]; var indices = new int[indexCount];
    for (int i = 0; i < count; i++) source[i] = new(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
    for (int i = 0; i < count; i++) uv[i] = new(input.ReadSingle(), input.ReadSingle());
    for (int i = 0; i < indexCount; i++) indices[i] = input.ReadInt32();
    if (!render.StartsWith("LiftBridge02", StringComparison.Ordinal)) continue;
    routes++;
    var domains = TypeOf("MovableUvDomains").GetMethod("For", Members)!.Invoke(null, new object[] { render, mesh })!;
    var items = Field<Array>(domains, "Items");
    foreach (float delta in new[] { 0f, 2f, 8f })
    {
        var method = TypeOf(implementation).GetMethod("Create", Members)!;
        object Argument(ParameterInfo p) => p.Name switch
        {
            "lod" => lod, "family" => family, "part" => family, "source" => source,
            "indices" => indices, "delta" => delta,
            _ => throw new InvalidOperationException("Unexpected native plan argument: " + p.Name)
        };
        var plan = method.Invoke(null, method.GetParameters().Select(Argument).ToArray())!;
        var positions = Field<Vector3[]>(plan, "Positions");
        var planIndices = Field<int[][]>(plan, "SourceIndices");
        if (!indices.SequenceEqual(planIndices[0])) throw new Exception("Source topology changed.");
        int finite = 0, mixed = 0, mixedFaces = 0;
        for (int domain = 0; domain < items.Length; domain++)
        {
            int mask = axes(render, mesh, domain);
            if (dispatch("Lift02", render, mesh, domain) != mask) throw new Exception("Shared axis dispatch mismatch.");
            if (mask == 3) { finite++; continue; }
            if (mask != 2) throw new Exception("Unresolved Lift02 source mapping.");
            mixed++;
            var record = items.GetValue(domain)!;
            int[] data = Field<int[]>(record, "_data");
            int start = Field<int>(record, "_start"), length = Field<int>(record, "_count");
            for (int at = start; at < start + length; at++)
            {
                int face = data[at];
                for (int corner = 0; corner < 3; corner++)
                {
                    int vertex = indices[face * 3 + corner];
                    if (positions[vertex].z != source[vertex].z)
                        throw new Exception("Longitudinal mapping changed in a retained roadbed panel.");
                }
                // Exact source-U/Z proportionality; constant terminal U is valid.
                int a = indices[face * 3], b = indices[face * 3 + 1], c = indices[face * 3 + 2];
                double dz1 = (double)source[b].z - source[a].z, dz2 = (double)source[c].z - source[a].z;
                double du1 = (double)uv[b].x - uv[a].x, du2 = (double)uv[c].x - uv[a].x;
                if (dz1 * du2 != dz2 * du1 || (dz1 == 0 && du1 != 0) || (dz2 == 0 && du2 != 0))
                    throw new Exception("Source U is not longitudinal or constant.");
                mixedFaces++;
            }
        }
        reports.Add(new { render, lod, mesh, delta, finiteDomains = finite, mixedDomains = mixed,
            mixedFaces, longitudinalCoordinatesUnchanged = true });
    }
}
if (routes != 20) throw new Exception("The fixture must cover all 20 Lift02 material streams.");
var result = new { candidateSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),
    fixtureSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2]))), routes,
    cases = reports.Count, reports, limitations = "Source-domain and actual geometry-plan checks only; final uploaded UVs, native rendering and cold loading require separate verification." };
File.WriteAllText(args[3], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS Lift02 source mapping: {routes} streams, {reports.Count} actual geometry plans; source topology and longitudinal U mapping preserved.");
