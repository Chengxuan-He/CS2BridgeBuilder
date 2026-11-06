using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

// Read-only native payload export for the SIMD numerical differential check.
internal static class SimdFixtures
{
    internal static void Export(string archive, string output)
    {
        Directory.CreateDirectory(output);
        using var zip = ZipFile.OpenRead(archive);
        string[] prefixes = { "SuspensionBridge03PillarTop", "SuspensionBridge03SupportMiddle", "SuspensionBridge03SupportEnd",
            "TrussArchBridge01", "TrussArchBridge02", "TrussArchBridge03", "PedestrianBridgeCoveredWood01Cover", "ExtradosedBridge02PillarCablesNet" };
        var receipts = new List<object>();
        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".Geometry", StringComparison.Ordinal) &&
            prefixes.Any(p => e.Name.StartsWith(p, StringComparison.Ordinal))).OrderBy(e => e.FullName, StringComparer.Ordinal))
        {
            using var input = entry.Open(); using var bytes = new MemoryStream(); input.CopyTo(bytes);
            byte[] original = bytes.ToArray(); var geometry = GeometryFile.Read(original);
            for (int mesh = 0; mesh < geometry.Meshes.Count; mesh++)
            {
                var data = geometry.Meshes[mesh]; string name = Path.GetFileNameWithoutExtension(entry.Name) + "-" + mesh + ".bin";
                string path = Path.Combine(output, name);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(data.Positions.Length); writer.Write(data.Indices.Length);
                    foreach (var p in data.Positions) { writer.Write(p.X); writer.Write(p.Y); writer.Write(p.Z); }
                    foreach (var index in data.Indices) writer.Write(index);
                }
                receipts.Add(new { Entry = entry.FullName, Mesh = mesh, GeometrySha256 = Convert.ToHexString(SHA256.HashData(original)),
                    File = name, FixtureSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), Vertices = data.Positions.Length, Indices = data.Indices.Length });
            }
        }
        File.WriteAllText(Path.Combine(output, "fixtures.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"Exported {receipts.Count} fixed native meshes from {archive}.");
    }
}
