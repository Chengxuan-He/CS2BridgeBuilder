using System;
using System.Collections.Generic;
using System.IO;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

// Shared by automatic startup recovery and the optional offline recovery tool. Odin text is NOT
// JSON ($fstrref/$iref tokens). Only the exact historical Bridge Builder policy is rewritten.
internal static class BridgeUnlockMigration
{
    private static readonly Regex GateType = new("\"\\$type\":\\s*\"(\\d+)\\|Game\\.Prefabs\\.Unlockable, Game\"");
    private static readonly Regex Reference = new("\\$fstrref:\"((?:CID:|UnityGUID:)[^\"]+)\"");

    internal static bool Rewrite(string text, out string rewritten, out string error)
    {
        rewritten = text;
        error = string.Empty;
        var matches = GateType.Matches(text);
        if (matches.Count == 0) return true;
        if (matches.Count != 1) { error = "Unexpected multiple unlock components; file retained"; return false; }
        var match = matches[0];
        var start = text.LastIndexOf('{', match.Index);
        var end = ObjectEnd(text, start);
        if (start < 0 || end < 0) { error = "Incomplete Odin unlock component"; return false; }
        var block = text.Substring(start, end - start);
        var all = Regex.Match(block, "\"m_RequireAll\":\\s*\\{");
        var any = Regex.Match(block, "\"m_RequireAny\":\\s*\\{");
        if (!all.Success || !any.Success) { error = "Unsupported unlock policy"; return false; }
        var allStart = block.IndexOf('{', all.Index);
        var anyStart = block.IndexOf('{', any.Index);
        var allEnd = ObjectEnd(block, allStart);
        var anyEnd = ObjectEnd(block, anyStart);
        if (allEnd < 0 || anyEnd < 0) { error = "Incomplete unlock arrays"; return false; }
        var allText = block.Substring(allStart, allEnd - allStart);
        var anyText = block.Substring(anyStart, anyEnd - anyStart);
        var refs = Reference.Matches(allText);
        if (!Regex.IsMatch(block, "\"active\":\\s*true")
            || !Regex.IsMatch(block, "\"m_IgnoreDependencies\":\\s*true")
            || !Regex.IsMatch(allText, "\"\\$rlength\":\\s*1\\s*,")
            || !Regex.IsMatch(anyText, "\"\\$rlength\":\\s*0\\s*,")
            || refs.Count != 1 || Reference.Matches(block).Count != 1)
        { error = "Not the historical one-prototype rule; file retained"; return false; }
        var identity = refs[0].Groups[1].Value;
        if (!Regex.IsMatch(identity, "^(CID:[a-fA-F0-9]{32}|UnityGUID:[a-fA-F0-9-]{32,36})$"))
        { error = "Unsupported prototype identity"; return false; }
        var id = Regex.Match(block, "\"\\$id\":\\s*(\\d+)");
        if (!id.Success) { error = "Unlock component has no Odin identity"; return false; }
        var outside = text.Substring(0, start) + text.Substring(end);
        foreach (Match arrayId in Regex.Matches(allText + anyText, "\"\\$id\":\\s*(\\d+)"))
            if (Regex.IsMatch(outside, "\\$iref:\\s*" + arrayId.Groups[1].Value + "(?![0-9])"))
            { error = "Unlock array shared outside its component; file retained"; return false; }
        var typeId = match.Groups[1].Value;
        if (Regex.IsMatch(outside, "\"\\$type\":\\s*" + typeId + "(?![0-9])"))
        { error = "Unlock type reused outside component; file retained"; return false; }
        var encoded = new BridgeUnlockExpression { Kind = 2, Identity = identity }.Encode();
        var replacement = "{\n                \"$id\": " + id.Groups[1].Value
            + ",\n                \"$type\": \"" + typeId + "|Game.Prefabs.ManualUnlockable, Game\","
            + "\n                \"name\": \"" + encoded + "\",\n                \"active\": true\n            }";
        var tail = text.Substring(end);
        // Removing an array can remove a type declaration later reused by Odin's numeric type IDs.
        // Promote its FIRST later use, leaving every object ID and non-unlock reference unchanged.
        foreach (Match declaration in Regex.Matches(allText + anyText, "\"\\$type\":\\s*\"(\\d+)\\|([^\"]+)\""))
        {
            var token = new Regex("\"\\$type\":\\s*" + declaration.Groups[1].Value + "(?![0-9])");
            tail = token.Replace(tail, _ => "\"$type\": \"" + declaration.Groups[1].Value + "|" + declaration.Groups[2].Value + "\"", 1);
        }
        rewritten = text.Substring(0, start) + replacement + tail;
        return true;
    }

    private static int ObjectEnd(string text, int start)
    {
        if (start < 0) return -1;
        var depth = 0;
        var quoted = false;
        var escaped = false;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') quoted = false;
                continue;
            }
            if (c == '"') quoted = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i + 1;
        }
        return -1;
    }

    internal static bool Run(string gameRoot, bool apply, out int changed, out string error)
    {
        changed = 0;
        error = string.Empty;
        try
        {
            var imported = Path.Combine(Path.GetFullPath(gameRoot), "ImportedData");
            if (!BridgeFileAccess.Exists(imported)) return true;
            if ((BridgeFileAccess.Attributes(imported) & FileAttributes.ReparsePoint) != 0)
            { error = "Reparse-point imported root retained"; return false; }
            var planned = new List<(string Path, string Before, string After)>();
            foreach (var directory in Directory.GetDirectories(BridgeFileAccess.Native(imported)))
            {
                if ((BridgeFileAccess.Attributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                var stem = Path.GetFileName(directory);
                if (!BridgeAssetInfo.TryFileOwner(stem, out _)) continue;
                var path = Path.Combine(directory, stem + ".Prefab");
                if (!BridgeFileAccess.Exists(path)) continue;
                if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0)
                { error = "Reparse point retained: " + path; return false; }
                var before = BridgeFileAccess.ReadText(path);
                if (!Rewrite(before, out var after, out error)) { error = path + ": " + error; return false; }
                if (after != before) planned.Add((path, before, after));
            }
            // Preflight ALL files before committing any. Each replace is atomic and has a backup.
            foreach (var item in planned)
            {
                if (apply && !Commit(item.Path, item.Before, item.After, out error)) return false;
                changed++;
            }
            return true;
        }
        catch (Exception exception) { error = "Access/migration incomplete (assets retained): " + exception.Message; return false; }
    }

    internal static bool ReplaceGate(string path, string previous, string next, out string error)
    {
        error = string.Empty;
        try
        {
            if (!BridgeAssetInfo.TryFileOwner(path, out _)) return false;
            if (!BridgeUnlockExpression.TryDecode(previous, out _) || !BridgeUnlockExpression.TryDecode(next, out _)) return false;
            var before = BridgeFileAccess.ReadText(path);
            var marker = "\"" + previous + "\"";
            if (before.IndexOf(marker, StringComparison.Ordinal) < 0
                || before.IndexOf(marker, StringComparison.Ordinal) != before.LastIndexOf(marker, StringComparison.Ordinal))
            { error = "Gate changed or ambiguous; file retained"; return false; }
            return Commit(path, before, before.Replace(marker, "\"" + next + "\""), out error);
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    private static bool Commit(string path, string before, string after, out string error)
    {
        error = string.Empty;
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".bbpending";
        try
        {
            if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0
                || BridgeFileAccess.ReadText(path) != before)
            { error = "File changed since preflight; retained: " + path; return false; }
            // Backups do not carry a live .Prefab/.cid extension and are never reimported.
            using var sha = SHA256.Create();
            var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(before))).Replace("-", "");
            var backup = path + "." + hash.Substring(0, 16) + ".bbunlockbackup";
            File.WriteAllText(BridgeFileAccess.Native(temp), after, new UTF8Encoding(false));
            if (!BridgeFileAccess.Exists(backup)) File.Copy(BridgeFileAccess.Native(path), BridgeFileAccess.Native(backup));
            File.Replace(BridgeFileAccess.Native(temp), BridgeFileAccess.Native(path), null);
            return BridgeFileAccess.ReadText(path) == after;
        }
        catch (Exception exception) { error = "Migration not committed: " + exception.Message; return false; }
        finally
        {
            try { if (BridgeFileAccess.Exists(temp)) File.Delete(BridgeFileAccess.Native(temp)); }
            catch (Exception) { /* An inert pending file is safer than deleting an unverified target. */ }
        }
    }
}
