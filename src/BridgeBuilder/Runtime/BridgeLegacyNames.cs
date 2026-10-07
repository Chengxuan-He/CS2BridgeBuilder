using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

// Start from UUID-owned bridges and follow serialized CID edges to local legacy sections.
// Never infer legacy ownership from a section name or mutate live prefab instances.
internal static class BridgeLegacyNames
{
    private sealed class Asset
    {
        internal string Path = "", Cid = "", Name = "", Text = "", Type = "";
        internal byte[] Bytes = Array.Empty<byte>();
    }

    internal static bool Run(string gameRoot, IEnumerable<string> owners, string backup,
        out HashSet<string> changed, out string error)
    {
        changed = new(StringComparer.Ordinal);
        error = "";
        var originals = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var created = new List<string>();
        try
        {
            var imported = Path.Combine(gameRoot, "ImportedData");
            var geometry = Path.Combine(gameRoot, "BridgeBuilder");
            var files = Files(imported).Concat(Files(geometry))
                .Where(p => p.EndsWith(".Prefab", StringComparison.OrdinalIgnoreCase)
                    || p.EndsWith(".Geometry", StringComparison.OrdinalIgnoreCase)).ToArray();
            var assets = new List<Asset>();
            foreach (var path in files)
            {
                if (!BridgeFileAccess.Exists(path + ".cid")) continue;
                var cid = BridgeFileAccess.ReadText(path + ".cid").Trim();
                if (!Regex.IsMatch(cid, @"\A[a-fA-F0-9]{32}\z")) continue;
                var asset = new Asset { Path = path, Cid = cid };
                if (path.EndsWith(".Prefab", StringComparison.OrdinalIgnoreCase))
                {
                    asset.Text = BridgeFileAccess.ReadText(path);
                    asset.Bytes = Encoding.UTF8.GetBytes(asset.Text);
                    if (BridgeSerializedReferences.TryRead(asset.Text, out var doc))
                    { asset.Name = doc.Name; asset.Type = doc.TypeName; }
                }
                assets.Add(asset);
            }
            var byCid = assets.GroupBy(a => a.Cid, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var writes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var retire = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var owner in owners.Distinct())
            {
                if (!BridgeAssetInfo.IsPrefabName(owner)) continue;
                var owned = assets.Where(a => BridgeAssetInfo.MatchesOwner(a.Path, owner)).ToArray();
                var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var pending = new Stack<string>(owned.Where(a => a.Name == owner
                    && !a.Path.Contains(owner + "_Dependencies")).Select(a => a.Cid));
                while (pending.Count != 0)
                {
                    var cid = pending.Pop();
                    if (!reachable.Add(cid) || !byCid.TryGetValue(cid, out var asset)) continue;
                    if (asset.Text.Length != 0)
                        foreach (var reference in BridgeDependencyCopies.References(asset.Bytes)) pending.Push(reference);
                }
                // Section -> subsection/piece -> render/LOD -> geometry. Material and road
                // dependencies are boundaries, regardless of their names.
                var components = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);
                var geometryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                pending = new Stack<string>(reachable);
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (pending.Count != 0)
                {
                    var cid = pending.Pop();
                    if (!visited.Add(cid) || !byCid.TryGetValue(cid, out var part)) continue;
                    if (part.Type != "Game.Prefabs.NetSectionPrefab, Game"
                        && part.Type != "Game.Prefabs.NetPiecePrefab, Game"
                        && part.Type != "Game.Prefabs.RenderPrefab, Game"
                        && !part.Path.EndsWith(".Geometry", StringComparison.OrdinalIgnoreCase)) continue;
                    var partName = part.Name.Length == 0 ? Path.GetFileNameWithoutExtension(part.Path) : part.Name;
                    if (!BridgeAssetInfo.MatchesOwner(partName, owner)) components.Add(cid, part);
                    foreach (var child in BridgeDependencyCopies.References(part.Bytes))
                    {
                        pending.Push(child);
                        if (byCid.TryGetValue(child, out var target) && target.Path.EndsWith(".Geometry", StringComparison.OrdinalIgnoreCase))
                            geometryNames[child] = part.Name + " Geometry";
                    }
                }
                var legacy = components.Values.ToArray();
                if (legacy.Length == 0) continue;
                var mapping = legacy.ToDictionary(a => a.Cid, a => NewCid(owner, a.Cid), StringComparer.OrdinalIgnoreCase);
                foreach (var asset in legacy)
                {
                    var oldName = asset.Name.Length == 0 ? Path.GetFileNameWithoutExtension(asset.Path) : asset.Name;
                    if (asset.Name.Length == 0 && oldName.Equals(asset.Cid, StringComparison.OrdinalIgnoreCase))
                        oldName = geometryNames[asset.Cid];
                    var nameParts = oldName.Split(' ');
                    nameParts[0] += "-" + owner;
                    var name = string.Join(" ", nameParts);
                    var extension = Path.GetExtension(asset.Path);
                    var path = extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
                        ? Path.Combine(imported, name, name + extension)
                        : Path.Combine(geometry, name + extension);
                    var bytes = asset.Text.Length == 0 ? File.ReadAllBytes(BridgeFileAccess.Native(asset.Path)) : asset.Bytes;
                    if (asset.Text.Length != 0)
                    {
                        if (!BridgeSerializedReferences.TryRead(asset.Text, out var doc))
                        { error = "Cannot read legacy prefab: " + asset.Path; return false; }
                        bytes = Encoding.UTF8.GetBytes(Repoint(doc.WithName(name), mapping));
                    }
                    writes.Add(path, bytes);
                    writes.Add(path + ".cid", Encoding.UTF8.GetBytes(mapping[asset.Cid]));
                    destinations.Add(path);
                    destinations.Add(path + ".cid");
                    foreach (var duplicate in assets.Where(a => a.Cid.Equals(asset.Cid, StringComparison.OrdinalIgnoreCase)))
                        retire.Add(duplicate.Path);
                }
                foreach (var asset in owned.Where(a => a.Text.Length != 0 && !mapping.ContainsKey(a.Cid)))
                {
                    var text = Repoint(asset.Text, mapping);
                    if (text != asset.Text) writes[asset.Path] = Encoding.UTF8.GetBytes(text);
                }
                changed.Add(owner);
            }
            if (changed.Count == 0) return true;
            // Keep shared old components until ALL local prefab consumers have stopped referring to them.
            var oldCids = assets.Where(a => retire.Contains(a.Path)).Select(a => a.Cid).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in assets.Where(a => a.Text.Length != 0 && !retire.Contains(a.Path)))
                foreach (var cid in BridgeDependencyCopies.References(writes.TryGetValue(asset.Path, out var bytes) ? bytes : asset.Bytes))
                    if (oldCids.Contains(cid)) keep.Add(cid);
            // Propagate retained references through the old graph as well.
            var retained = new Stack<string>(keep);
            while (retained.Count != 0)
            {
                if (!byCid.TryGetValue(retained.Pop(), out var asset)) continue;
                foreach (var cid in BridgeDependencyCopies.References(asset.Bytes))
                    if (oldCids.Contains(cid) && keep.Add(cid)) retained.Push(cid);
            }
            retire.RemoveWhere(path => assets.Any(a => a.Path == path && keep.Contains(a.Cid)));
            var retiredFiles = retire.SelectMany(p => new[] { p, p + ".cid" }).ToArray();
            var backupRoot = Path.GetFullPath(Path.Combine(backup, "LegacyNames"));
            if (backupRoot.StartsWith(Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            { error = "Legacy migration backup must be outside game data"; return false; }
            // Save all originals before any replacement. New copies may not overwrite unrelated assets.
            foreach (var path in writes.Keys.Concat(retiredFiles).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!BridgeFileAccess.Exists(path)) continue;
                var bytes = File.ReadAllBytes(BridgeFileAccess.Native(path));
                if (destinations.Contains(path))
                { error = "Legacy migration destination exists: " + path; return false; }
                originals[path] = bytes;
                var relative = path.Substring(Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                var saved = Path.Combine(backupRoot, relative);
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(saved)!));
                using var stream = new FileStream(BridgeFileAccess.Native(saved), FileMode.CreateNew, FileAccess.Write);
                stream.Write(bytes, 0, bytes.Length);
            }
            foreach (var pair in writes)
            {
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(pair.Key)!));
                if (!originals.ContainsKey(pair.Key)) created.Add(pair.Key);
                File.WriteAllBytes(BridgeFileAccess.Native(pair.Key), pair.Value);
            }
            foreach (var path in retiredFiles)
                if (BridgeFileAccess.Exists(path)) File.Delete(BridgeFileAccess.Native(path)); // Original is backed up above.
            foreach (var directory in retire.Select(Path.GetDirectoryName).Distinct())
                if (directory != null && directory != imported && directory != geometry
                    && !Directory.EnumerateFileSystemEntries(BridgeFileAccess.Native(directory)).Any())
                    Directory.Delete(BridgeFileAccess.Native(directory));
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            foreach (var pair in originals)
                try
                {
                    Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(pair.Key)!));
                    File.WriteAllBytes(BridgeFileAccess.Native(pair.Key), pair.Value);
                }
                catch (Exception rollback) { error += "; rollback failed: " + rollback.Message; }
            foreach (var path in created)
                try { File.Delete(BridgeFileAccess.Native(path)); }
                catch (Exception rollback) { error += "; copy cleanup failed: " + rollback.Message; }
            changed.Clear();
            return false;
        }
    }

    private static string Repoint(string text, Dictionary<string, string> mapping) =>
        Regex.Replace(text, @"CID:([a-fA-F0-9]{32})(?![a-fA-F0-9])", match =>
            mapping.TryGetValue(match.Groups[1].Value, out var cid) ? "CID:" + cid : match.Value);
    private static string NewCid(string owner, string oldCid)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(owner + ":legacy:" + oldCid.ToLowerInvariant())))
            .Replace("-", "").Substring(0, 32).ToLowerInvariant();
    }
    private static IEnumerable<string> Files(string directory)
    {
        if (!Directory.Exists(BridgeFileAccess.Native(directory))) yield break;
        if ((BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0) yield break;
        foreach (var file in Directory.GetFiles(BridgeFileAccess.Native(directory)))
            if ((BridgeFileAccess.Attributes(file) & FileAttributes.ReparsePoint) == 0) yield return BridgeFileAccess.Logical(file);
        foreach (var child in Directory.GetDirectories(BridgeFileAccess.Native(directory)))
            foreach (var file in Files(BridgeFileAccess.Logical(child))) yield return file;
    }
}
