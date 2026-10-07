using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

/// <summary>Selects bridge cleanup paths by UUID text only; moves whole directories without inspecting contents.</summary>
internal sealed class BridgeDiskAudit
{
    internal readonly Dictionary<string, string> Failures = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, string> FileOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _imported;
    private readonly string _geometry;
    internal bool Complete { get; private set; }
    internal string Error { get; private set; } = string.Empty;

    private BridgeDiskAudit(string imported)
    {
        _imported = Path.GetFullPath(imported);
        _geometry = Path.Combine(Path.GetDirectoryName(_imported)!, "BridgeBuilder");
    }

    internal static BridgeDiskAudit ForMemoryFailures(string gameRoot, IDictionary<string, string> failures)
        => SelectPaths(gameRoot, failures);

    internal static BridgeDiskAudit ForAllBridges(string gameRoot) => SelectPaths(gameRoot, null);

    private static BridgeDiskAudit SelectPaths(string gameRoot, IDictionary<string, string>? failures)
    {
        var audit = new BridgeDiskAudit(Path.Combine(gameRoot, "ImportedData"));
        foreach (var failure in failures ?? new Dictionary<string, string>())
        {
            if (!BridgeAssetInfo.IsPrefabName(failure.Key))
            { audit.Error = "Invalid bridge owner: " + failure.Key; return audit; }
            audit.Failures.Add(failure.Key, failure.Value);
        }
        foreach (var root in new[] { audit._imported, audit._geometry })
        {
            if (!SafeParents(root)) { audit.Error = "Unsafe asset root: " + root; return audit; }
            if (!Directory.Exists(BridgeFileAccess.Native(root))) continue;
            Select(root);
        }
        void Select(string directory)
        {
            foreach (var native in Directory.GetFileSystemEntries(BridgeFileAccess.Native(directory)))
            {
                var path = BridgeFileAccess.Logical(native);
                var name = Path.GetFileName(path);
                var owner = failures != null
                    ? failures.Keys.FirstOrDefault(id => BridgeAssetInfo.MatchesOwner(name, id))
                    : BridgeAssetInfo.TryFileOwner(name, out var matched) ? matched : null;
                if (owner == null)
                {
                    if (Directory.Exists(native) && (File.GetAttributes(native) & FileAttributes.ReparsePoint) == 0)
                        Select(path);
                    continue;
                }
                audit.FileOwners[path] = owner;
                if (failures == null) audit.Failures[owner] = "User requested removal of all bridges";
                if (Directory.Exists(native)) audit._directories.Add(path);
            }
        }
        foreach (var owner in audit.Failures.Keys)
            if (!audit.FileOwners.ContainsValue(owner))
            { audit.Error = "No matching local paths for bridge: " + owner; return audit; }
        audit.Complete = true;
        return audit;
    }

    private static bool Below(string path, string root) => path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static bool SafeParents(string path)
    {
        for (var directory = new DirectoryInfo(path); directory != null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) return false;
        return true;
    }
    private static string UniqueTarget(string path)
    {
        var candidate = path;
        while (BridgeFileAccess.Exists(candidate)) candidate = path + "__" + Guid.NewGuid().ToString("N");
        return candidate;
    }

    // Explicit user deletion only. Self-check continues using RetireFiles and its backups.
    internal bool DeleteFiles(ISet<string> owners, out string error)
    {
        error = "";
        if (!Complete) { error = Error; return false; }
        var errors = new List<string>();
        foreach (var pair in FileOwners.Where(p => owners.Contains(p.Value)))
        {
            var path = Path.GetFullPath(pair.Key);
            try
            {
                if ((!Below(path, _imported) && !Below(path, _geometry)) || !SafeParents(path))
                { errors.Add("Unsafe deletion path: " + path); continue; }
                if (Directory.Exists(BridgeFileAccess.Native(path))) Directory.Delete(BridgeFileAccess.Native(path), true);
                else File.Delete(BridgeFileAccess.Native(path));
                if (BridgeFileAccess.Exists(path)) errors.Add("File remains: " + path);
            }
            catch (Exception exception) { errors.Add(path + ": " + exception.Message); }
        }
        error = string.Join("; ", errors);
        return errors.Count == 0;
    }

    internal bool RetireFiles(ISet<string> owners, string backup, out string error)
    {
        error = "";
        if (!Complete || owners.Any(o => !Failures.ContainsKey(o)))
        { error = "No matching paths for requested bridge group"; return false; }
        try
        {
            backup = Path.GetFullPath(backup);
            var gameRoot = Path.GetDirectoryName(_imported)!;
            if (string.Equals(backup, gameRoot, StringComparison.OrdinalIgnoreCase) || Below(backup, gameRoot) || !SafeParents(backup))
            { error = "Unsafe backup destination"; return false; }
            Directory.CreateDirectory(BridgeFileAccess.Native(backup));
        }
        catch (Exception e) { error = "Backup unavailable: " + e.Message; return false; }
        var errors = new List<string>();
        foreach (var pair in FileOwners.Where(p => owners.Contains(p.Value)))
        {
            var source = Path.GetFullPath(pair.Key);
            try
            {
                var parent = Path.GetDirectoryName(source)!;
                // Containment protects the move destination and source roots, not asset validity.
                if ((!Below(source, _imported) && !Below(source, _geometry)) || !SafeParents(parent))
                { errors.Add("Source outside asset roots: " + source); continue; }
                var destinationRoot = Below(source, _geometry)
                    ? Path.Combine(backup, "BridgeBuilder") : backup;
                if (!SafeParents(destinationRoot)) { errors.Add("Unsafe backup path: " + destinationRoot); continue; }
                Directory.CreateDirectory(BridgeFileAccess.Native(destinationRoot));
                var destination = UniqueTarget(Path.Combine(destinationRoot, Path.GetFileName(source)));
                // Directory.Move/File.Move are the native equivalent of mv; no parsing, hashes,
                // tree snapshots, copying or deletion fallback. Failed moves remain explicit failures.
                if (_directories.Contains(pair.Key))
                    Directory.Move(BridgeFileAccess.Native(source), BridgeFileAccess.Native(destination));
                else File.Move(BridgeFileAccess.Native(source), BridgeFileAccess.Native(destination));
            }
            catch (Exception e) { errors.Add("Move failed: " + source + ": " + e.Message); }
        }
        error = string.Join("; ", errors);
        return errors.Count == 0;
    }
}
