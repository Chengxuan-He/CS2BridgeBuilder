using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Evidence from owned files, including assets hidden by a name/CID collision.</summary>
internal sealed class BridgeDiskAudit
{
    private static readonly Regex Owner = new(@"(?:^|[ _-])(b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})(?=$|[ _-])");
    internal readonly Dictionary<string, string> Failures = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, string> FileOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _trees = new(StringComparer.OrdinalIgnoreCase);
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

    // Only inventory files for owners already proven invalid in memory. No serialized integrity scan.
    internal static BridgeDiskAudit ForMemoryFailures(string gameRoot, IDictionary<string, string> failures)
    {
        var audit = new BridgeDiskAudit(Path.Combine(gameRoot, "ImportedData"));
        foreach (var failure in failures)
        {
            if (!BridgeAssetInfo.IsPrefabName(failure.Key))
            { audit.Error = "Invalid bridge owner: " + failure.Key; return audit; }
            audit.Failures.Add(failure.Key, failure.Value);
        }
        foreach (var root in new[] { audit._imported, audit._geometry })
            if (BridgeFileAccess.Exists(root)
                && (BridgeFileAccess.Attributes(root) & FileAttributes.ReparsePoint) != 0)
            { audit.Error = "Reparse-point asset root: " + root; return audit; }
        if (BridgeFileAccess.Exists(audit._imported))
            foreach (var directory in Directory.GetDirectories(BridgeFileAccess.Native(audit._imported)).Select(BridgeFileAccess.Logical))
            {
                var stem = Path.GetFileName(directory);
                var match = Owner.Match(stem);
                if (!match.Success || !failures.ContainsKey(match.Groups[1].Value)) continue;
                var owner = match.Groups[1].Value;
                if ((BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0)
                { audit.Error = "Reparse-point owned directory: " + directory; return audit; }
                if (!audit.RememberTree(directory, owner)) return audit;
            }
        if (BridgeFileAccess.Exists(audit._geometry))
            foreach (var path in Directory.GetFiles(BridgeFileAccess.Native(audit._geometry)).Select(BridgeFileAccess.Logical))
            {
                var name = Path.GetFileName(path);
                if (!name.EndsWith(".Geometry", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".Geometry.cid", StringComparison.OrdinalIgnoreCase)) continue;
                var match = Owner.Match(name.Substring(0, name.IndexOf(".Geometry", StringComparison.OrdinalIgnoreCase)));
                if (!match.Success || !failures.ContainsKey(match.Groups[1].Value)) continue;
                if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0)
                { audit.Error = "Reparse-point owned geometry: " + path; return audit; }
                audit.Remember(path, match.Groups[1].Value);
            }
        // Do not announce retirement for a memory-only/packaged object with no owned local files.
        foreach (var owner in failures.Keys)
            if (!audit.FileOwners.ContainsValue(owner))
            { audit.Error = "No owned local files for invalid bridge: " + owner; return audit; }
        audit.Complete = true;
        return audit;
    }

    private void Remember(string path, string owner)
    {
        if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0) return;
        FileOwners[path] = owner;
        _hashes[path] = Hash(path);
    }

    private bool RememberTree(string root, string owner)
    {
        _trees[root] = owner;
        return Walk(root);
        bool Walk(string directory)
        {
            if ((BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0)
            { Error = "Reparse-point owned directory: " + directory; return false; }
            _directories.Add(directory);
            foreach (var path in Directory.GetFiles(BridgeFileAccess.Native(directory)).Select(BridgeFileAccess.Logical))
            {
                if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0)
                { Error = "Reparse-point owned file: " + path; return false; }
                Remember(path, owner);
            }
            foreach (var child in Directory.GetDirectories(BridgeFileAccess.Native(directory)).Select(BridgeFileAccess.Logical))
                if (!Walk(child)) return false;
            return true;
        }
    }

    private bool ValidateTree(string root)
    {
        // Only exact UUID-owned children of ImportedData can be recursively moved or removed.
        if (!_trees.ContainsKey(root) || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(root)), _imported,
            StringComparison.OrdinalIgnoreCase) || !SafeParents(root)) return false;
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Walk(root)) return false;
        return files.SetEquals(_hashes.Keys.Where(p => Below(p, root)))
            && directories.SetEquals(_directories.Where(p => p == root || Below(p, root)));
        bool Walk(string directory)
        {
            if (!BridgeFileAccess.Exists(directory) || (BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0) return false;
            directories.Add(directory);
            foreach (var path in Directory.GetFiles(BridgeFileAccess.Native(directory)).Select(BridgeFileAccess.Logical))
            {
                if (!_hashes.TryGetValue(path, out var hash) || (BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0
                    || Hash(path) != hash) return false;
                files.Add(path);
            }
            foreach (var child in Directory.GetDirectories(BridgeFileAccess.Native(directory)).Select(BridgeFileAccess.Logical))
                if (!Walk(child)) return false;
            return true;
        }
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

    internal bool RetireFiles(ISet<string> owners, string backup, out string error)
    {
        error = "";
        var trees = _trees.Where(p => owners.Contains(p.Value)).Select(p => p.Key).ToArray();
        var loose = FileOwners.Where(p => owners.Contains(p.Value) && Below(p.Key, _geometry)).Select(p => p.Key).ToArray();
        try
        {
            if (!Complete || owners.Any(o => !Failures.ContainsKey(o)))
            { error = "No complete evidence for requested bridge group"; return false; }
            if (trees.Any(p => !ValidateTree(p)) || loose.Any(p => !ValidLoose(p)))
            { error = "Files or directory contents changed after validation"; return false; }
        }
        catch (Exception e) { error = "Source validation failed: " + e.Message; return false; }
        var warnings = new List<string>();
        var allowed = false;
        try
        {
            backup = Path.GetFullPath(backup);
            var gameRoot = Path.GetDirectoryName(_imported)!;
            allowed = !string.Equals(backup, gameRoot, StringComparison.OrdinalIgnoreCase)
                && !Below(backup, gameRoot) && SafeParents(backup);
            if (allowed) Directory.CreateDirectory(BridgeFileAccess.Native(backup));
            else warnings.Add("Backup rejected inside game discovery roots or through a junction");
        }
        catch (Exception e) { allowed = false; warnings.Add("Backup unavailable: " + e.Message); }
        var cleared = true;
        foreach (var source in trees)
        {
            try
            {
                if (allowed)
                {
                    var destination = UniqueTarget(Path.Combine(backup, Path.GetFileName(source)));
                    if (!ValidateTree(source)) { cleared = false; warnings.Add("Changed directory retained: " + source); continue; }
                    // Whole-directory rename on the same volume. For another volume, copy the
                    // complete snapshot (including empty children), verify, then remove the source.
                    if (string.Equals(Path.GetPathRoot(source), Path.GetPathRoot(destination), StringComparison.OrdinalIgnoreCase))
                        Directory.Move(BridgeFileAccess.Native(source), BridgeFileAccess.Native(destination));
                    else
                    {
                        foreach (var directory in _directories.Where(p => p == source || Below(p, source)))
                            Directory.CreateDirectory(BridgeFileAccess.Native(destination + directory.Substring(source.Length)));
                        foreach (var path in _hashes.Keys.Where(p => Below(p, source)))
                            File.Copy(BridgeFileAccess.Native(path), BridgeFileAccess.Native(destination + path.Substring(source.Length)), false);
                    }
                    if (_hashes.Where(p => Below(p.Key, source)).Any(p => Hash(destination + p.Key.Substring(source.Length)) != p.Value))
                        warnings.Add("Directory backup verification failed: " + source);
                }
                ClearTree(source);
            }
            catch (Exception e)
            {
                warnings.Add("Directory backup failed: " + source + ": " + e.Message);
                try { ClearTree(source); }
                catch (Exception cleanup) { cleared = false; warnings.Add("Directory cleanup failed: " + cleanup.Message); }
            }
        }
        // Geometry lives in a shared directory: only exact UUID-owned loose files may move.
        var geometryTarget = allowed ? UniqueTarget(Path.Combine(backup, "BridgeBuilder")) : "";
        foreach (var source in loose)
        {
            try
            {
                if (allowed)
                {
                    Directory.CreateDirectory(BridgeFileAccess.Native(geometryTarget));
                    var destination = Path.Combine(geometryTarget, Path.GetFileName(source));
                    File.Copy(BridgeFileAccess.Native(source), BridgeFileAccess.Native(destination), false);
                    if (Hash(destination) != _hashes[source]) warnings.Add("Geometry backup verification failed: " + source);
                }
                ClearLoose(source);
            }
            catch (Exception e)
            {
                warnings.Add("Geometry backup failed: " + e.Message);
                try { ClearLoose(source); }
                catch (Exception cleanup) { cleared = false; warnings.Add("Geometry cleanup failed: " + cleanup.Message); }
            }
        }
        error = string.Join("; ", warnings);
        return cleared;
        void ClearTree(string source)
        {
            if (!BridgeFileAccess.Exists(source)) return;
            if (!ValidateTree(source)) { cleared = false; warnings.Add("Changed directory retained: " + source); return; }
            Directory.Delete(BridgeFileAccess.Native(source), true);
        }
        bool ValidLoose(string source) => string.Equals(Path.GetDirectoryName(source), _geometry, StringComparison.OrdinalIgnoreCase)
            && SafeParents(_geometry) && BridgeFileAccess.Exists(source)
            && (BridgeFileAccess.Attributes(source) & FileAttributes.ReparsePoint) == 0 && Hash(source) == _hashes[source];
        void ClearLoose(string source)
        {
            if (!BridgeFileAccess.Exists(source)) return;
            if (!ValidLoose(source)) { cleared = false; warnings.Add("Changed geometry retained: " + source); return; }
            File.Delete(BridgeFileAccess.Native(source));
        }
    }

    private static string Hash(string path)
    {
        using var stream = BridgeFileAccess.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }
}
