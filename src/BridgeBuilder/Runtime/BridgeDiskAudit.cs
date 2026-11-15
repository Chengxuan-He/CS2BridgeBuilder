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
    private readonly string _imported;
    private readonly string _geometry;
    private readonly string _virtual;
    internal bool Changed { get; private set; }
    internal bool Complete { get; private set; }
    internal string Error { get; private set; } = string.Empty;

    private BridgeDiskAudit(string imported)
    {
        _imported = Path.GetFullPath(imported);
        _geometry = Path.Combine(Path.GetDirectoryName(_imported)!, "BridgeBuilder");
        _virtual = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_imported)!, "StreamingData~/VT"));
    }

    internal static BridgeDiskAudit ForMemoryFailures(string gameRoot, IDictionary<string, string> failures,
        IEnumerable<string>? knownPaths = null)
    {
        if (knownPaths == null) return SelectPaths(gameRoot, failures);
        var audit = new BridgeDiskAudit(Path.Combine(gameRoot, "ImportedData"));
        foreach (var failure in failures)
        {
            if (!BridgeAssetInfo.IsPrefabName(failure.Key)) { audit.Error = "Invalid bridge owner"; return audit; }
            audit.Failures.Add(failure.Key, failure.Value);
        }
        foreach (string path in knownPaths)
            foreach (string owner in failures.Keys)
                if (BridgeAssetInfo.MatchesOwner(path, owner) && BridgeFileAccess.Exists(path))
                {
                    string source = Path.GetFullPath(BridgeFileAccess.Logical(path));
                    string ownerRoot = Path.Combine(audit._imported, owner);
                    // Move the owner tree once, retaining relative names and sidecar pairs.
                    // Metadata and file enumeration can spell the same file with different separators.
                    if (Below(source, ownerRoot)) source = ownerRoot;
                    audit.FileOwners[source] = owner;
                }
        foreach (string owner in failures.Keys)
            if (!audit.FileOwners.ContainsValue(owner)) { audit.Error = "No matching local paths for bridge: " + owner; return audit; }
        audit.Complete = true;
        return audit;
    }

    private static BridgeDiskAudit SelectPaths(string gameRoot, IDictionary<string, string>? failures)
    {
        var audit = new BridgeDiskAudit(Path.Combine(gameRoot, "ImportedData"));
        foreach (var failure in failures ?? new Dictionary<string, string>())
        {
            if (!BridgeAssetInfo.IsPrefabName(failure.Key))
            { audit.Error = "Invalid bridge owner: " + failure.Key; return audit; }
            audit.Failures.Add(failure.Key, failure.Value);
        }
        var ancestry = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in new[] { audit._imported, audit._geometry, audit._virtual })
        {
            if (!Directory.Exists(BridgeFileAccess.Native(root))) continue;
            Select(root);
        }
        void Select(string directory)
        {
            string physical = BridgeFileAccess.Physical(directory);
            if (!ancestry.Add(physical)) throw new IOException("Directory mapping cycle: " + directory);
            try
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
                    if (Directory.Exists(native))
                        Select(path);
                    continue;
                }
                audit.FileOwners[path] = owner;
                if (failures == null) audit.Failures[owner] = "User requested removal of all bridges";
            }
            } finally { ancestry.Remove(physical); }
        }
        foreach (var owner in audit.Failures.Keys)
            if (!audit.FileOwners.ContainsValue(owner))
            { audit.Error = "No matching local paths for bridge: " + owner; return audit; }
        audit.Complete = true;
        return audit;
    }

    private static bool Below(string path, string root) => path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static string UniqueTarget(string path)
    {
        var candidate = path;
        while (BridgeFileAccess.Exists(candidate)) candidate = path + "__" + Guid.NewGuid().ToString("N");
        return candidate;
    }

    // Shared by explicit deletion and the self-check fallback after backup/move failure.
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
                if (!Below(path, _imported) && !Below(path, _geometry) && !Below(path, _virtual))
                { errors.Add("Unsafe deletion path: " + path); continue; }
                BridgeRemovalFiles.RequireExclusive(Path.GetDirectoryName(_imported)!, path, pair.Value);
                var existed = BridgeFileAccess.Exists(path);
                BridgeFileAccess.DeletePayload(path, () => Changed = true);
                if (existed && !BridgeFileAccess.Exists(path)) Changed = true;
                if (BridgeFileAccess.Exists(path)) errors.Add("File remains: " + path);
            }
            catch (UnauthorizedAccessException exception) { throw new BridgeRequiredFilePermissionException("delete", path, exception); }
            catch (Exception exception) { errors.Add(path + ": " + exception.Message); }
        }
        error = string.Join("; ", errors);
        return errors.Count == 0;
    }

    // True means every selected source was removed. On true, a nonempty error means
    // the backup is incomplete; false means removal itself did not complete.
    internal bool RetireFiles(ISet<string> owners, string backup, out string error)
    {
        error = "";
        if (!Complete || owners.Any(o => !Failures.ContainsKey(o)))
        { error = "No matching paths for requested bridge group"; return false; }
        try
        {
            backup = Path.GetFullPath(backup);
            var gameRoot = Path.GetDirectoryName(_imported)!;
            if (BridgeFileAccess.WithinAssetStorage(backup, gameRoot))
            { error = "Unsafe backup destination"; return false; }
            foreach (var selected in FileOwners.Where(p => owners.Contains(p.Value)))
                if (BridgeFileAccess.WithinTree(backup, selected.Key)) throw new IOException("Backup aliases selected payload: " + backup);
            PrepareBackupDirectory(backup);
        }
        catch (UnauthorizedAccessException e) { error = "Backup destination permission denied: " + e.Message; return DeleteAfterBackupFailure(owners, ref error); }
        catch (Exception e) { error = "Backup unavailable; sources retained: " + e.Message; return false; }
        var errors = new List<string>();
        foreach (var pair in FileOwners.Where(p => owners.Contains(p.Value)))
        {
            var source = Path.GetFullPath(pair.Key);
            try
            {
                // Containment protects the move destination and source roots, not asset validity.
                if (!Below(source, _imported) && !Below(source, _geometry) && !Below(source, _virtual))
                { errors.Add("Source outside asset roots: " + source); continue; }
                BridgeRemovalFiles.RequireExclusive(Path.GetDirectoryName(_imported)!, source, pair.Value);
                var destinationRoot = Below(source, _geometry)
                    ? Path.Combine(backup, "BridgeBuilder")
                    : Below(source, _virtual) ? Path.Combine(backup, "VT") : backup;
                if (BridgeFileAccess.WithinAssetStorage(destinationRoot, Path.GetDirectoryName(_imported)!)) throw new IOException("Backup aliases asset storage: " + destinationRoot);
                try { PrepareBackupDirectory(destinationRoot); }
                catch (UnauthorizedAccessException e)
                {
                    errors.Add("Backup destination permission denied: " + e.Message);
                    error = string.Join("; ", errors);
                    return DeleteAfterBackupFailure(owners, ref error);
                }
                var destination = UniqueTarget(Path.Combine(destinationRoot, Path.GetFileName(source)));
                if (BridgeFileAccess.WithinAssetStorage(destination, Path.GetDirectoryName(_imported)!)) throw new IOException("Backup aliases asset storage: " + destination);
                BridgeFileAccess.MovePayload(source, destination, () => Changed = true);
                if (BridgeFileAccess.Exists(source)) errors.Add("Source remains after move: " + source);
            }
            catch (UnauthorizedAccessException e) { throw new BridgeRequiredFilePermissionException("move", source, e); }
            catch (Exception e) { errors.Add("Move failed: " + source + ": " + e.Message); }
        }
        error = string.Join("; ", errors);
        return errors.Count == 0;
    }

    private static void PrepareBackupDirectory(string directory)
    {
        Directory.CreateDirectory(BridgeFileAccess.Native(directory));
        var probe = BridgeFileAccess.Native(Path.Combine(directory, ".bb-write-" + Guid.NewGuid().ToString("N")));
        // Existing directory creation is not evidence of destination write access.
        var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        Exception? closeError = null;
        try { stream.Dispose(); } catch (Exception error) { closeError = error; }
        try { File.Delete(probe); }
        catch (Exception error) { throw new IOException("Backup probe cleanup failed: " + probe, error); }
        if (closeError != null) throw new IOException("Backup probe close failed: " + probe, closeError);
    }

    private bool DeleteAfterBackupFailure(ISet<string> owners, ref string error)
    {
        var removed = DeleteFiles(owners, out var deletionError);
        error += removed ? "; remaining source assets deleted without complete backup"
            : "; deletion failed: " + deletionError;
        return removed;
    }
}
