using BridgeBuilder.Runtime;

internal static class FileChecks
{
    internal static void Existing(string root)
    {
        var builtins = new HashSet<string> { "b00176216b0ce36579bce6fc675cfb77", "9f446790360787756b0e5909db12e200", "375b871b66aaf675dacef141eb2a9810" };
        int variants = 0;
        foreach (var style in Directory.GetDirectories(root))
        foreach (var variant in new[] { ("Paint", "SkinnedPaint.Surface", 3), ("Cable", "SkinnedCable.Surface", 2), ("Road", "NativeAsphalt.Surface", 2) })
        {
            var files = BridgePreparedMaterialFiles.Read(style, variant.Item1, variant.Item2,
                bytes => BridgeSurfaceReferences.Read(bytes, (_, _) => throw new Exception("Unexpected VT")),
                cid => { if (!builtins.Contains(cid)) throw new Exception("Unexpected built-in dependency: " + cid); });
            if (files.Count != variant.Item3) throw new Exception("Existing variant graph changed");
            variants++;
        }
        if (variants != 30) throw new Exception("All ten styles must be checked");
        Console.WriteLine($"PASS existing packaged materials: {variants} real paint/cable/road graphs through production preflight.");
    }

    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "PreparedMaterialFiles-" + Guid.NewGuid().ToString("N"));
        var style = Path.Combine(root, "Draw01");
        const string material = "11111111111111111111111111111111", texture = "22222222222222222222222222222222";
        void Write(string path, string cid, byte value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, new[] { value }); File.WriteAllText(path + ".cid", cid);
        }
        string surface = Path.Combine(style, "Variant", "Test.Surface");
        string shared = Path.Combine(style, "Shared", texture + ".Texture");
        void Missing(string cid) => throw new InvalidDataException("Missing: " + cid);
        List<BridgePreparedMaterialFiles.Input> Read() => BridgePreparedMaterialFiles.Read(style, "Variant", "Test.Surface", _ => new[] { texture }, Missing);
        int checks = 0;
        void Require(bool value) { if (!value) throw new Exception("Prepared source preflight mismatch"); checks++; }
        void Reject(Action action)
        {
            try { action(); }
            catch (IOException) { checks++; return; }
            catch (InvalidDataException) { checks++; return; }
            throw new Exception("Invalid prepared source accepted");
        }
        try
        {
            Write(surface, material, 1);
            Write(Path.Combine(root, "Draw02", "Shared", texture + ".Texture"), texture, 2);
            Reject(() => Read()); // Another style cannot satisfy this style's dependency.
            Write(shared, texture, 2);
            Require(Read().Count == 2);
            var time = File.GetLastWriteTimeUtc(shared);
            Require(Read().Count == 2 && File.GetLastWriteTimeUtc(shared) == time);
            File.WriteAllText(shared + ".cid", material);
            Reject(() => Read());
            File.WriteAllText(shared + ".cid", texture);
            File.Delete(shared + ".cid"); Reject(() => Read());
            File.WriteAllText(shared + ".cid", texture);
            string local = Path.Combine(style, "Variant", "Local.Texture");
            Write(local, texture, 2);
            Require(Read().Count == 2 && Read().Any(i => i.Path == local));
            Write(Path.Combine(style, "Variant", "Conflict.Texture"), texture, 3);
            Reject(() => Read());
            File.Delete(Path.Combine(style, "Variant", "Conflict.Texture"));
            Reject(() => BridgePreparedMaterialFiles.Read(style, "Variant", "Test.Surface", _ => new[] { "../Draw02" }, Missing));
            Reject(() => BridgePreparedMaterialFiles.Read(style, "Variant", "Absent.Surface", _ => Array.Empty<string>(), Missing));
            Require(BridgePreparedMaterialFiles.Read(style, "Variant", "Test.Surface", _ => new[] { "33333333333333333333333333333333" }, _ => { }).Count == 2);
            Console.WriteLine($"PASS prepared source preflight: {checks} shared/local/built-in, isolation, conflict, missing-sidecar, identity and immutable-read checks.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
