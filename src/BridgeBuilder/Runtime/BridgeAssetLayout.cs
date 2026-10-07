using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

// Final file-only migration step. No further cached asset streams may be used until restart.
internal static class BridgeAssetLayout
{
    internal static bool Run(string gameRoot, IEnumerable<string> owners, string backup,
        Func<string, string> prefabPath, out HashSet<string> changed, out string error)
    {
        changed = new(StringComparer.Ordinal); error = "";
        var saved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var created = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var moved = new List<string>();
        try
        {
            gameRoot = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar);
            var backupRoot = Path.GetFullPath(Path.Combine(backup, "Layout"));
            if (backupRoot.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            { error = "Layout backup must be outside game data"; return false; }
            var selected = new HashSet<string>(owners, StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0) return true;
            var plan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = BridgeFileAccess.EnumerateFiles(Path.Combine(gameRoot, "ImportedData"))
                .Concat(BridgeFileAccess.EnumerateFiles(Path.Combine(gameRoot, "BridgeBuilder"))).ToArray();
            foreach (var source in files)
            {
                var relative = source.Substring(gameRoot.Length + 1);
                if (!BridgeAssetInfo.TryFileOwner(relative, out var owner) || !selected.Contains(owner)) continue;
                // External snapshots retain their own names/CIDs; they already use the generation layout.
                if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains(owner + "_Dependencies")) continue;
                string destination;
                if (source.EndsWith(".Prefab", StringComparison.OrdinalIgnoreCase))
                {
                    if (!BridgeSerializedReferences.TryRead(BridgeFileAccess.ReadText(source), out var document))
                    { error = "Cannot read prefab name for layout: " + source; return false; }
                    destination = Path.GetFullPath(Path.Combine(gameRoot, prefabPath(document.Name)));
                }
                else if (source.EndsWith(".Geometry", StringComparison.OrdinalIgnoreCase))
                    destination = Path.Combine(gameRoot, "BridgeBuilder", Path.GetFileName(source));
                else continue;
                if (!destination.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !BridgeAssetInfo.MatchesOwner(destination.Substring(gameRoot.Length), owner))
                { error = "Unowned layout destination: " + destination; return false; }
                if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) continue;
                if (!BridgeFileAccess.Exists(source + ".cid"))
                { error = "Missing CID sidecar: " + source; return false; }
                foreach (var suffix in new[] { "", ".cid" })
                {
                    var target = destination + suffix;
                    if (!targets.Add(target) || BridgeFileAccess.Exists(target))
                    { error = "Layout destination already exists: " + target; return false; }
                    plan.Add(source + suffix, target);
                }
                changed.Add(owner);
            }
            // Preflight and back up the complete plan before moving any payload or identity sidecar.
            foreach (var source in plan.Keys)
            {
                var copy = Path.Combine(backupRoot, source.Substring(gameRoot.Length + 1));
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(copy)!));
                File.Copy(BridgeFileAccess.Native(source), BridgeFileAccess.Native(copy), false);
                saved.Add(source, copy);
            }
            foreach (var pair in plan)
            {
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(pair.Value)!));
                File.Move(BridgeFileAccess.Native(pair.Key), BridgeFileAccess.Native(pair.Value));
                created.Add(pair.Key, pair.Value); moved.Add(pair.Key);
            }
            foreach (var directory in plan.Keys.Select(Path.GetDirectoryName).Distinct())
                if (directory != null && !Directory.EnumerateFileSystemEntries(BridgeFileAccess.Native(directory)).Any())
                    Directory.Delete(BridgeFileAccess.Native(directory));
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            foreach (var source in moved)
                try
                {
                    Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(source)!));
                    File.Copy(BridgeFileAccess.Native(saved[source]), BridgeFileAccess.Native(source), true);
                    File.Delete(BridgeFileAccess.Native(created[source]));
                }
                catch (Exception rollback) { error += "; restore failed: " + rollback.Message; }
            changed.Clear(); return false;
        }
    }

}
