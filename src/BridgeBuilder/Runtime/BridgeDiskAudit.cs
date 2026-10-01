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
    private static readonly Regex RootName = new("(?m)^    \"name\": \"([^\"]+)\"");
    private static readonly Regex Geometry = new("\"m_GeometryAsset\":\\s*\\$fstrref:\"CID:([a-fA-F0-9]{32})\"");
    internal readonly Dictionary<string, string> Failures = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, string> FileOwners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _imported;
    internal bool Complete { get; private set; }
    internal string Error { get; private set; } = string.Empty;

    private BridgeDiskAudit(string imported) => _imported = Path.GetFullPath(imported);

    internal static BridgeDiskAudit Read(string gameRoot, IEnumerable<string> registered,
        Func<string, bool>? externalGeometryExists = null)
    {
        var audit = new BridgeDiskAudit(Path.Combine(gameRoot, "ImportedData"));
        try
        {
            var owners = new HashSet<string>(registered.Where(BridgeRegistration.IsPrefabName), StringComparer.Ordinal);
            var names = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var cids = new Dictionary<string, List<(string Owner, string File)>>(StringComparer.OrdinalIgnoreCase);
            var geometryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var geometryRoot = Path.Combine(gameRoot, "BridgeBuilder");
            if (BridgeFileAccess.Exists(geometryRoot))
                foreach (var sidecar in Directory.GetFiles(BridgeFileAccess.Native(geometryRoot), "*.Geometry.cid").Select(BridgeFileAccess.Logical))
                    if (BridgeFileAccess.Exists(sidecar.Substring(0, sidecar.Length - 4)))
                        geometryIds.Add(BridgeFileAccess.ReadText(sidecar).Trim());
            if (BridgeFileAccess.Exists(audit._imported))
                foreach (var directory in Directory.GetDirectories(BridgeFileAccess.Native(audit._imported)).Select(BridgeFileAccess.Logical))
                {
                    // Do not follow symlinks/junctions or infer ownership from arbitrary metadata.
                    if ((BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                    var stem = Path.GetFileName(directory);
                    var match = Owner.Match(stem);
                    if (!match.Success || !owners.Contains(match.Groups[1].Value)) continue;
                    var owner = match.Groups[1].Value;
                    var path = Path.Combine(directory, stem + ".Prefab");
                    if (!BridgeFileAccess.Exists(path)) continue;
                    if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0)
                    { audit.Error = "Reparse-point prefab: " + path; return audit; }
                    var text = BridgeFileAccess.ReadText(path);
                    audit.Remember(path, owner);
                    var cidPath = path + ".cid";
                    if (BridgeFileAccess.Exists(cidPath))
                    {
                        if ((BridgeFileAccess.Attributes(cidPath) & FileAttributes.ReparsePoint) != 0)
                        { audit.Error = "Reparse-point sidecar: " + cidPath; return audit; }
                        audit.Remember(cidPath, owner);
                        var cid = BridgeFileAccess.ReadText(cidPath).Trim();
                        if (Regex.IsMatch(cid, "^[a-fA-F0-9]{32}$"))
                        {
                            if (!cids.TryGetValue(cid, out var entries)) cids[cid] = entries = new();
                            entries.Add((owner, path));
                        }
                    }
                    // Registration names are user-editable. Only the serialized prefab identity
                    // is compared to the immutable UUID folder, never the UI label.
                    if (stem == owner || stem == owner + "_Upper" || stem == owner + "_Lower")
                    {
                        var nameMatch = RootName.Match(text);
                        if (nameMatch.Success)
                        {
                            var name = nameMatch.Groups[1].Value;
                            if (name != stem) audit.Add(owner, $"prefab identity '{name}' differs from file '{stem}'");
                            if (!names.TryGetValue(name, out var group)) names[name] = group = new(StringComparer.Ordinal);
                            group.Add(owner);
                        }
                    }
                    // Only private render assets bearing this registered UUID are checked.
                    // Never interpret missing optional/shared material references as corruption.
                    foreach (Match reference in Geometry.Matches(text))
                        if (!geometryIds.Contains(reference.Groups[1].Value)
                            && !(externalGeometryExists?.Invoke(reference.Groups[1].Value) ?? false))
                            audit.Add(owner, $"missing private geometry {reference.Groups[1].Value} in {stem}");
                }
            foreach (var group in names.Where(p => p.Value.Count > 1))
                foreach (var owner in group.Value) audit.Add(owner, "duplicate prefab name: " + group.Key);
            foreach (var group in cids.Where(p => p.Value.Count > 1))
                foreach (var entry in group.Value) audit.Add(entry.Owner, "duplicate prefab CID: " + group.Key);
            audit.Complete = true;
        }
        catch (Exception exception) { audit.Error = exception.Message; }
        return audit;
    }

    private void Remember(string path, string owner)
    {
        if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0) return;
        FileOwners[path] = owner;
        _hashes[path] = Hash(path);
    }

    private void Add(string owner, string reason)
    {
        Failures[owner] = Failures.TryGetValue(owner, out var previous) ? previous + "; " + reason : reason;
    }

    internal bool OwnsPath(string path, ISet<string> owners)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path)) return false;
        return FileOwners.TryGetValue(Path.GetFullPath(path), out var owner) && owners.Contains(owner);
    }

    internal bool OwnsName(string name, ISet<string> owners)
    {
        var match = Owner.Match(name ?? string.Empty);
        return match.Success && owners.Contains(match.Groups[1].Value);
    }

    internal bool RetireFiles(ISet<string> owners, string backup, out string error)
    {
        error = string.Empty;
        var moved = new List<(string Source, string Backup)>();
        try
        {
            if (!Complete || owners.Any(o => !Failures.ContainsKey(o)))
            { error = "No complete disk evidence for requested bridge group"; return false; }
            var files = FileOwners.Where(p => owners.Contains(p.Value)).Select(p => p.Key).ToArray();
            foreach (var path in files)
                if (!path.StartsWith(_imported + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !BridgeFileAccess.Exists(path) || (BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0
                    || Hash(path) != _hashes[path]
                    || (BridgeFileAccess.Attributes(Path.GetDirectoryName(path)!) & FileAttributes.ReparsePoint) != 0)
                { error = "Files changed after validation: " + path; return false; }
            foreach (var path in files)
            {
                // No live .Prefab/.cid extensions in the recovery directory: these copies
                // must never be re-imported by the game's asset discovery.
                var destination = Path.Combine(backup, path.Substring(_imported.Length + 1) + ".bbremoved");
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(destination)!));
                File.Move(BridgeFileAccess.Native(path), BridgeFileAccess.Native(destination));
                moved.Add((path, destination));
            }
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            foreach (var item in moved.AsEnumerable().Reverse())
                try { if (!BridgeFileAccess.Exists(item.Source)) File.Move(BridgeFileAccess.Native(item.Backup), BridgeFileAccess.Native(item.Source)); }
                catch (Exception rollback) { error += "; rollback: " + rollback.Message; }
            return false;
        }
    }

    private static string Hash(string path)
    {
        using var stream = BridgeFileAccess.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }
}
