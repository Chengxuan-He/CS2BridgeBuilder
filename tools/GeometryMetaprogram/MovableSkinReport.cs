using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Offline inspection of actual native rigid skinning. No runtime classifications are emitted:
/// bone membership is evidence of motion, not evidence of road/structure membership or widening.
/// </summary>
internal static class MovableSkinReport
{
    internal static void Write(string sourcePath, string reportPath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var geometry = GeometryFile.Read(bytes, readSkin: true);
        var report = new StringBuilder("# Native geometry skin inspection\n\n");
        report.AppendLine($"Source: `{Path.GetFileName(sourcePath)}`.");
        report.AppendLine($"\nSHA-256: `{Convert.ToHexString(SHA256.HashData(bytes))}`.\n");
        report.AppendLine("Decoded from the actual geometry, not synthetic test vertices. Bone indices are geometry array indices; they are not connection IDs.");
        report.AppendLine("This report does not classify road triangles, identify widening parts, or establish visual acceptance.\n");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        for (var meshIndex = 0; meshIndex < geometry.Meshes.Count; meshIndex++)
        {
            var mesh = geometry.Meshes[meshIndex];
            if (mesh.Indices.Length % 3 != 0 || mesh.Indices.Any(i => i < 0 || i >= mesh.Positions.Length))
                throw new InvalidDataException($"Mesh {meshIndex} has invalid triangle indices.");
            if (mesh.Positions.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)))
                throw new InvalidDataException($"Mesh {meshIndex} has non-finite positions.");
            report.AppendLine($"## Mesh {meshIndex}\n");
            report.AppendLine($"Vertices: {mesh.Positions.Length}. Triangles: {mesh.Indices.Length / 3}.\n");
            if (mesh.BoneIndices.Length == 0)
            {
                report.AppendLine("No bone-index channel exists.\n");
                continue;
            }
            if (mesh.BoneIndices.Length != mesh.Positions.Length)
                throw new InvalidDataException($"Mesh {meshIndex} has incomplete skin data.");
            var groups = Enumerable.Range(0, mesh.Positions.Length)
                .GroupBy(i => mesh.BoneIndices[i]).OrderBy(g => g.Key).ToArray();
            var triangleCounts = new Dictionary<uint, int>();
            var mixed = 0;
            for (var i = 0; i < mesh.Indices.Length; i += 3)
            {
                var a = mesh.BoneIndices[mesh.Indices[i]];
                var b = mesh.BoneIndices[mesh.Indices[i + 1]];
                var c = mesh.BoneIndices[mesh.Indices[i + 2]];
                if (a != b || a != c) mixed++;
                else triangleCounts[a] = triangleCounts.GetValueOrDefault(a) + 1;
            }
            report.AppendLine($"Triangles referencing more than one bone: {mixed}.\n");
            report.AppendLine("| Bone index | Vertices | Single-bone triangles | Min X | Max X | Min Y | Max Y | Min Z | Max Z |");
            report.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var group in groups)
            {
                var points = group.Select(i => mesh.Positions[i]).ToArray();
                report.AppendLine(FormattableString.Invariant(
                    $"| {group.Key} | {points.Length} | {triangleCounts.GetValueOrDefault(group.Key)} | {points.Min(p => p.X):R} | {points.Max(p => p.X):R} | {points.Min(p => p.Y):R} | {points.Max(p => p.Y):R} | {points.Min(p => p.Z):R} | {points.Max(p => p.Z):R} |"));
            }
            report.AppendLine();
            // OBJ is a scratch inspection asset. It groups faces by native bone, but assigns no
            // semantic part names and changes no native asset. Cross-bone triangles remain explicit.
            var objPath = Path.ChangeExtension(reportPath, $"mesh-{meshIndex}.obj");
            using var obj = new StreamWriter(objPath, false, new UTF8Encoding(false));
            obj.WriteLine("# Native closed-pose geometry; groups are raw bone indices, not authored parts.");
            foreach (var p in mesh.Positions)
                obj.WriteLine(string.Create(CultureInfo.InvariantCulture, $"v {p.X:R} {p.Y:R} {p.Z:R}"));
            string? current = null;
            for (var i = 0; i < mesh.Indices.Length; i += 3)
            {
                var a = mesh.Indices[i]; var b = mesh.Indices[i + 1]; var c = mesh.Indices[i + 2];
                var same = mesh.BoneIndices[a] == mesh.BoneIndices[b] && mesh.BoneIndices[a] == mesh.BoneIndices[c];
                var group = same ? "bone_" + mesh.BoneIndices[a] : "mixed_bones";
                if (group != current) { obj.WriteLine("g " + group); current = group; }
                obj.WriteLine($"f {a + 1} {b + 1} {c + 1}");
            }
        }
        File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
    }
}
