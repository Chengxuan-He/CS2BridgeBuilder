using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BridgeBuilder.Runtime;

// Preflight one immutable variant and its style-local shared textures before registration.
internal static class BridgePreparedMaterialFiles
{
    // Native primary/duplicate membership measured from the shipped cache payloads.
    // Package verification checks the exact CID and membership again before registration.
    internal static string? CacheFor(string surfaceCid) => surfaceCid.ToLowerInvariant() switch
    {
        "039ce6916b9e9b8bdfb9ed5d4d5cbe5c" => "590c2b892c085c362f90755731d335ad",
        "05c3fd924390ddae7736ad66be6933a5" => "eff35749207c2302687c20496f5d14d3",
        "0af13ac649e98277d8f97741d37fe84a" => "8eaf55988863bdd485e6077db7f7a8ad",
        "1ee4efbb3192b9ad5f385220374d86f2" => "dad4ab52cf3793950705f82b50d91c06",
        "40145174f666d20dae781e52746e58d9" => "74cb7448889402585579c7fc829293a3",
        "4210e269a573d3f104ce0084f5a8e7ed" => "19faa232f5ac1670257e4749528c94d2",
        "44b49057f6790331754d3493091cb6c4" => "28ac88b8a773989416ed18e83265c87a",
        "59ddcee6a5590e84df457c2dfa821909" => "b415e7dbdbc2b50f6994be36dff7ba0a",
        "71ff326645b2ef03a72db28a2a7ee97c" => "d313baac99368449f3058fd93113cb74",
        "9495831fec32b0e982bf1496735582e6" => "21bbb95f4454ba9c9862adb30acb6714",
        "9efcfe455fd5620755086008eaf3fd47" => "8cc900e79f99a3b99ad6481843b95d05",
        "a1d40af8d8c682682a51a03ce51b6575" => "95cbaae387ce9d41a3fa6e62b17b8db3",
        "ae154e542d65c0e10433d57379de06e8" => "859f55f59b610459373598c2635ae40c",
        "b3570d9965a7e1eae1a358313dbc6618" => "56c0be82b8d2e4ffe11136b7b2c997b6",
        "b92ae25c4e800acc8d27bf5895dd55ff" => "cd368184e835a9b58d0df696bc7c973f",
        "ba777b704ad7d546fd03ee79959278dc" => "30b3fe03f5f0baf7a7c258faae53c1df",
        "c763146534f1432a7080fc027ad1fa0a" => "d359e3b6bd5c36db42c7915ca85df5ab",
        "d6aa8585f66ccf35e14cb3d5b91f6339" => "eb7c9d91c3f789285524ec3e8b439b58",
        "d765f1348645cc68318b5ac29c61abde" => "cb58e3b6afef582151d671bffdd41677",
        "e051606c4255367725cfaa66c7931846" => "92a72fe03771ba1b64b8367508c9f55f",
        "ef31c0e29999c354b9e07c18ae0a81d7" => "e8dee46779b0eb4bd886c949a91ef74d",
        "fb89dbe7d2de48b8570e75276a5ec11e" => "b5b2120bcff0ecc72304f918ded8f618",
        _ => null
    };

    internal static string CachePath(string root, string cid) =>
        System.IO.Path.Combine(root, "Shared", "MidMips", cid, "TS512_MB3.MidMips");

    internal static bool IsSharedPaint(string cid) => cid is
        "9a7a7da8813bd92cf48a0f9a4052e651" or
        "bc2e09a359ba8e1f4b41c417b5f631af" or
        "d9d33ecb2b319859d2d356059bc00b22";

    internal static string PaintPath(string root, string cid) =>
        System.IO.Path.Combine(root, "Shared", "Paint", cid + ".Texture");

    internal readonly struct Input
    {
        internal readonly string Path, Cid, Digest;
        internal Input(string path, string cid, string digest) { Path = path; Cid = cid; Digest = digest; }
    }

    internal static List<Input> Read(string styleFolder, string variant, string name,
        Func<byte[], string[]> references, Action<string> requireBuiltin, Func<string, int>? requiredMidMipLevels = null,
        BridgeMaterialFileSnapshot? snapshot = null)
    {
        byte[] Bytes(string path) => snapshot == null ? File.ReadAllBytes(path)
            : snapshot.Get(path).Bytes ?? throw new InvalidDataException("Snapshot descriptor bytes are missing: " + path);
        string Identity(string path)
        {
            using var reader = new StreamReader(new MemoryStream(Bytes(path + ".cid"), false));
            return reader.ReadToEnd().Trim().ToLowerInvariant();
        }
        string Digest(string path)
        {
            if (snapshot != null) return snapshot.Get(path).Digest;
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream));
        }
        string folder = System.IO.Path.Combine(styleFolder, variant);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Prepared material payloads are missing: " + folder);
        var inputs = new List<Input>();
        var identities = new Dictionary<string, Input>(StringComparer.OrdinalIgnoreCase);
        void Add(string path, string? expected = null)
        {
            string cid = Identity(path);
            if (cid.Length != 32 || cid.All(c => c == '0') || cid.Any(c => !Uri.IsHexDigit(c)) ||
                (expected != null && !StringComparer.OrdinalIgnoreCase.Equals(cid, expected)))
                throw new InvalidDataException("Invalid prepared material identity: " + path);
            string digest = Digest(path);
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
        foreach (string cid in references(Bytes(surface.Path)))
        {
            if (identities.ContainsKey(cid)) continue;
            // Only a serialized CID may select a shared filename. No global or cross-owner lookup.
            if (cid.Length != 32 || cid.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("Invalid prepared dependency identity: " + cid);
            var shared = System.IO.Path.Combine(styleFolder, "Shared", cid.ToLowerInvariant() + ".Texture");
            if (variant == "Paint" && name == "SkinnedPaint.Surface" && IsSharedPaint(cid))
                Add(PaintPath(Directory.GetParent(styleFolder)!.FullName, cid), cid);
            else if (File.Exists(shared)) Add(shared, cid);
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
        string cacheCid = CacheFor(surface.Cid)
            ?? throw new InvalidDataException("Prepared Surface has no native mid-mip membership: " + surface.Cid);
        string cachePath = CachePath(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(styleFolder))!, cacheCid);
        if (!File.Exists(cachePath)) throw new InvalidDataException("Required prepared MidMips file is missing: " + cachePath);
        byte[] cacheBytes = Bytes(cachePath);
        if (!BridgeMidMipReferences.Read(cacheBytes).Contains(surface.Cid, StringComparer.OrdinalIgnoreCase)
            || levels != BitConverter.ToInt32(cacheBytes, 4)
            || System.IO.Path.GetFileNameWithoutExtension(cachePath) != $"TS{BitConverter.ToInt32(cacheBytes, 0)}_MB{levels}")
            throw new InvalidDataException("Prepared MidMips membership, discovery tag or level count differs: " + cacheCid);
        if (!identities.ContainsKey(cacheCid)) Add(cachePath, cacheCid);
        }
        return inputs;
    }
}
