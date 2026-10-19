using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Final file-only migration step. Cached asset streams must not be reused until restart.
internal static class BridgeAssetLayout
{
    internal static bool Run(string gameRoot, IEnumerable<string> owners, string backup,
        Func<string, string> prefabPath, out HashSet<string> changed, out string error)
    {
        changed = new(StringComparer.Ordinal); error = "";
        var saved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var removed = new List<string>();
        try
        {
            gameRoot = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar);
            var backupRoot = Path.GetFullPath(Path.Combine(backup, "Layout"));
            if (backupRoot.Equals(gameRoot, StringComparison.OrdinalIgnoreCase)
                || backupRoot.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Layout backup must be outside game data");
            var selected = new HashSet<string>(owners, StringComparer.OrdinalIgnoreCase);
            var plan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var files = BridgeFileAccess.EnumerateFiles(Path.Combine(gameRoot, "ImportedData"))
                .Concat(BridgeFileAccess.EnumerateFiles(Path.Combine(gameRoot, "BridgeBuilder")))
                .Concat(BridgeFileAccess.EnumerateFiles(Path.Combine(gameRoot, "StreamingData~", "VT"))).ToArray();
            foreach (var source in files)
            {
                var relative = source.Substring(gameRoot.Length + 1);
                if (source.EndsWith(".cid", StringComparison.OrdinalIgnoreCase)
                    || !BridgeAssetInfo.TryFileOwner(relative, out var owner) || !selected.Contains(owner)) continue;
                string destination;
                // External snapshots are opaque: retain their entire relative path and CID filenames.
                if (BridgeAssetInfo.IsDependencyPath(relative, owner))
                {
                    var parts = relative.Split('\\', '/');
                    var oldIndex = Array.FindIndex(parts, p => p.Equals(owner + "_Dependencies", StringComparison.OrdinalIgnoreCase));
                    if (oldIndex < 0) continue;
                    destination = Path.Combine(gameRoot, "ImportedData", owner, "Dependencies",
                        Path.Combine(parts.Skip(oldIndex + 1).ToArray()));
                }
                else
                {
                    var extension = Path.GetExtension(source);
                    var category = BridgeAssetInfo.AssetCategory(extension);
                    // Mid-mip filenames remain unchanged: the native loader uses their name as a tag.
                    if (category == null) continue;
                    var stem = Path.GetFileNameWithoutExtension(source);
                    var shortName = category == "Prefabs" && stem.Equals(owner, StringComparison.OrdinalIgnoreCase)
                        ? "Bridge" : BridgeAssetInfo.ShortName(stem, owner);
                    if (shortName.Length == 0) throw new IOException("Empty filename after UUID removal: " + source);
                    destination = Path.Combine(gameRoot, BridgeAssetInfo.OwnedDirectory(owner, category), shortName + extension);
                }
                destination = Path.GetFullPath(destination);
                if (!destination.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !BridgeAssetInfo.MatchesOwner(destination.Substring(gameRoot.Length), owner))
                    throw new IOException("Unowned layout destination: " + destination);
                if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) continue;
                if (!BridgeFileAccess.Exists(source + ".cid")) throw new IOException("Missing CID sidecar: " + source);
                // Compare the complete pair before accepting any duplicate destination.
                if (BridgeFileAccess.Exists(destination) || BridgeFileAccess.Exists(destination + ".cid"))
                    RequireSamePair(source, destination);
                if (targets.TryGetValue(destination, out var previous)) RequireSamePair(source, previous);
                else targets.Add(destination, source);
                plan.Add(source, destination);
                plan.Add(source + ".cid", destination + ".cid");
                changed.Add(owner);
            }
            foreach (var source in plan.Keys)
            {
                var copy = Path.Combine(backupRoot, source.Substring(gameRoot.Length + 1));
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(copy)!));
                if (BridgeFileAccess.Exists(copy))
                {
                    if (!SameFile(source, copy)) throw new IOException("Backup conflict: " + copy);
                }
                else File.Copy(BridgeFileAccess.Native(source), BridgeFileAccess.Native(copy), false);
                saved.Add(source, copy);
            }
            foreach (var pair in plan)
            {
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(pair.Value)!));
                if (!BridgeFileAccess.Exists(pair.Value))
                {
                    File.Move(BridgeFileAccess.Native(pair.Key), BridgeFileAccess.Native(pair.Value));
                    created.Add(pair.Value);
                }
                else
                {
                    if (!SameFile(pair.Key, pair.Value)) throw new IOException("Layout destination changed: " + pair.Value);
                    File.Delete(BridgeFileAccess.Native(pair.Key));
                }
                removed.Add(pair.Key);
            }
            foreach (var pair in plan)
                if (!SameFile(pair.Value, saved[pair.Key]))
                    throw new IOException("Migrated payload/CID verification failed: " + pair.Value);
            foreach (var source in plan.Keys)
            {
                var directory = Path.GetDirectoryName(source);
                while (directory != null && !directory.Equals(gameRoot, StringComparison.OrdinalIgnoreCase)
                    && Directory.Exists(BridgeFileAccess.Native(directory))
                    && !Directory.EnumerateFileSystemEntries(BridgeFileAccess.Native(directory)).Any())
                {
                    Directory.Delete(BridgeFileAccess.Native(directory));
                    directory = Path.GetDirectoryName(directory);
                }
            }
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            var restored = true;
            foreach (var source in removed)
                try
                {
                    Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(source)!));
                    File.Copy(BridgeFileAccess.Native(saved[source]), BridgeFileAccess.Native(source), true);
                }
                catch (Exception rollback) { restored = false; error += "; restore failed: " + rollback.Message; }
            // If restoration fails, retain all destination copies as recovery evidence.
            if (restored) foreach (var target in created)
                try { File.Delete(BridgeFileAccess.Native(target)); }
                catch (Exception rollback) { error += "; cleanup failed: " + rollback.Message; }
            changed.Clear(); return false;
        }
    }

    private static void RequireSamePair(string source, string target)
    {
        if (!SameFile(source, target) || !SameFile(source + ".cid", target + ".cid"))
            throw new IOException("Layout destination already exists with different payload or CID: " + target);
    }

    private static bool SameFile(string a, string b)
    {
        if (!BridgeFileAccess.Exists(a) || !BridgeFileAccess.Exists(b)) return false;
        using var left = BridgeFileAccess.OpenRead(a);
        using var right = BridgeFileAccess.OpenRead(b);
        if (left.Length != right.Length) return false;
        var x = new byte[65536]; var y = new byte[65536];
        int count;
        while ((count = left.Read(x, 0, x.Length)) != 0)
        {
            var read = 0;
            while (read < count)
            {
                var n = right.Read(y, read, count - read);
                if (n == 0) return false;
                read += n;
            }
            for (var i = 0; i < count; i++) if (x[i] != y[i]) return false;
        }
        return true;
    }
}
