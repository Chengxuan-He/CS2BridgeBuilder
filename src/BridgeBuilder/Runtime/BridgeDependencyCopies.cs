using System;
using System.Collections.Generic;
using System.IO;

using System.Text;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Byte-identical, per-bridge snapshots of serialized external asset dependencies.</summary>
internal static class BridgeDependencyCopies
{
    private const string EmptyCid = "00000000000000000000000000000000";
    internal sealed class Source
    {
        internal bool Builtin;
        internal bool Owned;
        internal string Extension = "";
        internal byte[] Bytes = Array.Empty<byte>();
    }

    internal static string Folder(string gameRoot, string owner) =>
        Path.Combine(gameRoot, "ImportedData", owner + "_Dependencies");

    // Inspect serialized identifiers only. Never load, validate or repair a prefab instance.
    internal static IEnumerable<string> References(byte[] bytes)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in new[] { Encoding.UTF8.GetString(bytes), Encoding.Unicode.GetString(bytes),
                     bytes.Length > 1 ? Encoding.Unicode.GetString(bytes, 1, bytes.Length - 1) : "" })
            foreach (Match match in Regex.Matches(text, @"CID:([a-fA-F0-9]{32})(?![a-fA-F0-9])"))
            {
                var cid = match.Groups[1].Value.ToLowerInvariant();
                // Native AssetReference serializes an absent optional asset as Hash128.Empty.
                // For example, lane-only NetPiecePrefabs legitimately have no geometry asset.
                // This is not a dependency edge; required prefab slots are checked separately.
                if (cid != EmptyCid) result.Add(cid);
            }
        // UnityGUID references resolve to the game's built-in resource map and are terminal.
        return result;
    }

    internal static bool Save(string gameRoot, string owner, IEnumerable<string> seeds,
        Func<string, Source?> resolve, out int count, out string error, out int writtenFiles)
    {
        writtenFiles = 0;
        count = 0;
        error = "";
        if (!BridgeAssetInfo.IsPrefabName(owner)) { error = "Invalid bridge owner"; return false; }
        try
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<string>(seeds);
            var copies = new Dictionary<string, Source>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count != 0)
            {
                var cid = pending.Pop().ToLowerInvariant();
                if (cid == EmptyCid || !Regex.IsMatch(cid, @"\A[a-f0-9]{32}\z"))
                { error = "Invalid dependency CID"; return false; }
                if (!visited.Add(cid)) continue;
                var source = resolve(cid);
                if (source == null) { error = "Dependency unavailable: " + cid; return false; }
                if (source.Builtin) continue;
                if (!Regex.IsMatch(source.Extension, @"\A\.[A-Za-z0-9]+\z"))
                { error = "Unsupported dependency extension: " + source.Extension; return false; }
                if (!source.Owned) copies.Add(cid, source);
                if (source.Extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
                    || source.Extension.Equals(".Material", StringComparison.OrdinalIgnoreCase))
                    foreach (var dependency in References(source.Bytes)) pending.Push(dependency);
            }
            var folder = Folder(gameRoot, owner);
            // Preflight every destination before publishing any file. A changed same-CID asset
            // must not silently replace a snapshot on which this bridge already depends.
            var verifiedExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var copy in copies)
            {
                var path = Path.Combine(folder, copy.Key + copy.Value.Extension);
                var payloadExists = BridgeFileAccess.Exists(path);
                var sidecarExists = BridgeFileAccess.Exists(path + ".cid");
                if (payloadExists && !Matches(path, copy.Value.Bytes))
                { error = "Different contents for dependency CID " + copy.Key; return false; }
                if (sidecarExists && BridgeFileAccess.ReadText(path + ".cid").Trim() != copy.Key)
                { error = "Dependency sidecar mismatch: " + copy.Key; return false; }
                if (payloadExists && sidecarExists) verifiedExisting.Add(copy.Key);
            }
            if (copies.Count == 0) return true;
            Directory.CreateDirectory(BridgeFileAccess.Native(folder));
            foreach (var copy in copies)
            {
                // Preflight already verified these immutable destinations; no writes or second read.
                if (verifiedExisting.Contains(copy.Key)) { count++; continue; }
                var path = Path.Combine(folder, copy.Key + copy.Value.Extension);
                if (!BridgeFileAccess.Exists(path))
                {
                    using var stream = new FileStream(BridgeFileAccess.Native(path), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    stream.Write(copy.Value.Bytes, 0, copy.Value.Bytes.Length);
                    writtenFiles++;
                }
                if (!BridgeFileAccess.Exists(path + ".cid"))
                {
                    File.WriteAllText(BridgeFileAccess.Native(path + ".cid"), copy.Key, new UTF8Encoding(false));
                    writtenFiles++;
                }
                if (!Matches(path, copy.Value.Bytes))
                { error = "Dependency copy verification failed: " + copy.Key; return false; }
                count++;
            }
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }
    private static bool Matches(string path, byte[] expected)
    {
        using var stream = BridgeFileAccess.OpenRead(path);
        if (stream.Length != expected.Length) return false;
        var buffer = new byte[32768];
        var offset = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            for (var i = 0; i < read; i++) if (buffer[i] != expected[offset + i]) return false;
            offset += read;
        }
        return offset == expected.Length;
    }

}
