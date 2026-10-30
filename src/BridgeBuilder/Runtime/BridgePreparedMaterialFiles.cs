using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BridgeBuilder.Runtime;

// Preflight one immutable variant and its style-local shared textures before registration.
internal static class BridgePreparedMaterialFiles
{
    private static readonly Dictionary<string, Dictionary<string, Input[]>> MidMipIndices = new(StringComparer.OrdinalIgnoreCase);
    internal static void Clear() => MidMipIndices.Clear();

    internal readonly struct Input
    {
        internal readonly string Path, Cid, Digest;
        internal Input(string path, string cid, string digest) { Path = path; Cid = cid; Digest = digest; }
    }

    internal static List<Input> Read(string styleFolder, string variant, string name,
        Func<byte[], string[]> references, Action<string> requireBuiltin, Func<string, int>? requiredMidMipLevels = null)
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
        {
        int levels = 0;
        foreach (string cid in references(File.ReadAllBytes(surface.Path)))
        {
            if (identities.ContainsKey(cid)) continue;
            // Only a serialized CID may select a shared filename. No global or cross-owner lookup.
            if (cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("Invalid prepared dependency identity: " + cid);
            var shared = System.IO.Path.Combine(styleFolder, "Shared", cid.ToLowerInvariant() + ".Texture");
            if (File.Exists(shared)) Add(shared, cid);
            else
            {
                requireBuiltin(cid);
                int required = requiredMidMipLevels?.Invoke(cid) ?? 0;
                if (required != 0 && levels != 0 && required != levels)
                    throw new InvalidDataException("Prepared VT dependencies disagree on mid-mip levels.");
                levels = Math.Max(levels, required);
            }
        }
        if (levels == 0) continue;
        // Only native MidMips may use the approved common immutable source root.
        // Ordinary textures remain strictly style-local; snapshots remain owner-local.
        string cacheFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(styleFolder))!, "Shared", "MidMips");
        if (!MidMipIndices.TryGetValue(cacheFolder, out var index))
        {
            var rows = new List<(Input Input, string[] Members)>();
            if (Directory.Exists(cacheFolder))
            foreach (string path in Directory.EnumerateFiles(cacheFolder, "*.MidMips", SearchOption.AllDirectories))
            {
                var bytes = File.ReadAllBytes(path);
                var members = BridgeMidMipReferences.Read(bytes);
                string cid = File.ReadAllText(path + ".cid").Trim().ToLowerInvariant();
                if (cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c)) ||
                    System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) != cid)
                    throw new InvalidDataException("Invalid common MidMips identity/path: " + path);
                using var sha = SHA256.Create();
                rows.Add((new Input(path, cid, BitConverter.ToString(sha.ComputeHash(bytes))), members));
            }
            index = rows.SelectMany(r => r.Members.Select(member => (member, r.Input)))
                .GroupBy(r => r.member, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Input).ToArray(), StringComparer.OrdinalIgnoreCase);
            MidMipIndices.Add(cacheFolder, index);
        }
        var matching = index.TryGetValue(surface.Cid, out var matched) ? matched : Array.Empty<Input>();
        if (matching.Length == 0) throw new InvalidDataException("Prepared Surface has no native mid-mip membership: " + surface.Cid);
        foreach (var cache in matching)
        {
            using var reader = new BinaryReader(File.OpenRead(cache.Path));
            int tile = reader.ReadInt32(), count = reader.ReadInt32();
            if (count != levels || System.IO.Path.GetFileNameWithoutExtension(cache.Path) != $"TS{tile}_MB{count}")
                throw new InvalidDataException("Prepared MidMips discovery tag or level count differs.");
            if (!identities.ContainsKey(cache.Cid)) { identities.Add(cache.Cid, cache); inputs.Add(cache); }
        }
        }
        return inputs;
    }
}
