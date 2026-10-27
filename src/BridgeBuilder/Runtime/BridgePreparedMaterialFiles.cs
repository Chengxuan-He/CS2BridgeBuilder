using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BridgeBuilder.Runtime;

// Preflight one immutable variant and its style-local shared textures before registration.
internal static class BridgePreparedMaterialFiles
{
    internal readonly struct Input
    {
        internal readonly string Path, Cid, Digest;
        internal Input(string path, string cid, string digest) { Path = path; Cid = cid; Digest = digest; }
    }

    internal static List<Input> Read(string styleFolder, string variant, string name,
        Func<byte[], string[]> references, Action<string> requireBuiltin)
    {
        string folder = System.IO.Path.Combine(styleFolder, variant);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Prepared material payloads are missing: " + folder);
        var inputs = new List<Input>();
        var identities = new Dictionary<string, Input>(StringComparer.OrdinalIgnoreCase);
        void Add(string path, string? expected = null)
        {
            string cid = File.ReadAllText(path + ".cid").Trim().ToLowerInvariant();
            if (cid.Length != 32 || cid.All(c => c == '0') || cid.Any(c => !Uri.IsHexDigit(c)) ||
                (expected != null && !StringComparer.OrdinalIgnoreCase.Equals(cid, expected)))
                throw new InvalidDataException("Invalid prepared material identity: " + path);
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            string digest = BitConverter.ToString(sha.ComputeHash(stream));
            var input = new Input(path, cid, digest);
            if (identities.TryGetValue(cid, out var existing) && existing.Digest != digest)
                throw new InvalidDataException("Prepared variant contains conflicting payloads: " + cid);
            identities[cid] = input;
            inputs.Add(input);
        }
        foreach (string path in Directory.EnumerateFiles(folder).OrderBy(p => p, StringComparer.Ordinal))
            if (System.IO.Path.GetExtension(path) is ".Surface" or ".Texture") Add(path);
        if (!inputs.Any(i => System.IO.Path.GetFileName(i.Path) == name))
            throw new FileNotFoundException("Prepared material descriptor is missing.", System.IO.Path.Combine(folder, name));
        foreach (var surface in inputs.Where(i => System.IO.Path.GetExtension(i.Path) == ".Surface").ToArray())
        foreach (string cid in references(File.ReadAllBytes(surface.Path)))
        {
            if (identities.ContainsKey(cid)) continue;
            // Only a serialized CID may select a shared filename. No global or cross-owner lookup.
            if (cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("Invalid prepared dependency identity: " + cid);
            var shared = System.IO.Path.Combine(styleFolder, "Shared", cid.ToLowerInvariant() + ".Texture");
            if (File.Exists(shared)) Add(shared, cid);
            else requireBuiltin(cid);
        }
        return inputs;
    }
}
