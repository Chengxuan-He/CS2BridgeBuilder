using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Runtime;

/// <summary>Reads and edits scalar metadata in the existing prefab, preserving its Odin graph verbatim.</summary>
internal static class BridgeAssetMetadata
{
    internal const string NativeType = "Game.Prefabs.UIObject, Game";
    internal const string Prefix = "BridgeBuilder.Data.v2:";
    internal static string Encode(BridgeAssetInfo e) => Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        string.Join("\n", new[] { e.PrefabName, e.DisplayName, e.UpperDeckId, e.LowerDeckId ?? "", e.StyleId, e.CreatedUtc,
            e.Pending ? "1" : "0" }.Select(v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v))))));
    internal static bool Decode(string value, out BridgeAssetInfo entry)
    {
        entry = null!;
        if (!value.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        try
        {
            var v = Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(Prefix.Length))).Split('\n')
                .Select(x => Encoding.UTF8.GetString(Convert.FromBase64String(x))).ToArray();
            if (v.Length < 1 || v.Length > 7 || !BridgeAssetInfo.IsPrefabName(v[0])) return false;
            string Get(int index) => index < v.Length ? v[index] : "";
            entry = new BridgeAssetInfo(v[0], string.IsNullOrWhiteSpace(Get(1)) ? v[0] : Get(1),
                Get(2), Get(3), Get(4), Get(5), Get(6) == "1"); return true;
        }
        catch (Exception) { return false; }
    }
    private const string TypeName = "BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder";
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static bool TryRead(string text, out BridgeAssetInfo entry)
    {
        entry = null!;
        if (!BridgeSerializedReferences.TryRead(text, out var document)
            || !BridgeAssetInfo.IsPrefabName(document.Name)) return false;
        var native = document.Component(NativeType);
        if (native != null && native.Fields.TryGetValue("name", out var encoded)
            && Decode(encoded.Text, out entry) && entry.PrefabName == document.Name) return true;
        var component = document.Component(TypeName);
        if (component == null) return false;
        string Get(string key) => component.Fields.TryGetValue(key, out var value) && value.Text != "null" ? value.Text : "";
        var label = Get("m_BridgeDisplayName");
        entry = new BridgeAssetInfo(document.Name, string.IsNullOrWhiteSpace(label) ? document.Name : label,
            Get("m_BridgeUpperDeckId"), Get("m_BridgeLowerDeckId"), Get("m_BridgeStyleId"),
            Get("m_BridgeCreatedUtc"), Get("m_BridgeCreationPending") == "true");
        return true;
    }

    internal static bool Rewrite(string text, BridgeAssetInfo entry, out string updated)
    {
        updated = text;
        if (!BridgeSerializedReferences.TryRead(text, out var document) || document.Name != entry.PrefabName) return false;
        var native = document.Component(NativeType);
        if (native != null && native.Fields.TryGetValue("name", out var encoded) && encoded.Text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            updated = text.Remove(encoded.Start, encoded.End - encoded.Start).Insert(encoded.Start, Quote(Encode(entry)));
            return true;
        }
        var component = document.Component(TypeName);
        if (component == null || component.End <= component.Start) return false;
        var fields = new Dictionary<string, string>
        {
            ["m_BridgeDisplayName"] = Quote(entry.DisplayName),
            ["m_BridgeUpperDeckId"] = Quote(entry.UpperDeckId),
            ["m_BridgeLowerDeckId"] = Quote(entry.LowerDeckId ?? ""),
            ["m_BridgeStyleId"] = Quote(entry.StyleId),
            ["m_BridgeCreatedUtc"] = Quote(entry.CreatedUtc),
            ["m_BridgeCreationPending"] = entry.Pending ? "true" : "false"
        };
        var edits = new List<(int Start, int Length, string Text)>();
        var missing = new StringBuilder();
        foreach (var pair in fields)
            if (component.Fields.TryGetValue(pair.Key, out var value))
                edits.Add((value.Start, value.End - value.Start, pair.Value));
            else missing.Append(",\n                ").Append(Quote(pair.Key)).Append(": ").Append(pair.Value);
        if (missing.Length != 0) edits.Add((component.End - 1, 0, missing.ToString() + "\n            "));
        foreach (var edit in edits.OrderByDescending(e => e.Start))
            updated = updated.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
        return TryRead(updated, out var check) && check.PrefabName == entry.PrefabName
            && check.DisplayName == entry.DisplayName && check.Pending == entry.Pending;
    }

    internal static bool Write(string path, BridgeAssetInfo entry)
    {
        string? temporary = null;
        try
        {
            if (!BridgeAssetInfo.MatchesOwner(path, entry.PrefabName)) return false;
            if ((BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0) return false;
            var original = BridgeFileAccess.ReadText(path);
            if (!Rewrite(original, entry, out var updated)) return false;
            if (updated == original) return true;
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(BridgeFileAccess.Native(temporary), updated, Utf8);
            if (BridgeFileAccess.ReadText(path) != original) return false;
            // Replace only this prefab. The CID sidecar, dependencies and native identity never change.
            File.Replace(BridgeFileAccess.Native(temporary), BridgeFileAccess.Native(path), null);
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not save bridge asset metadata: " + path);
            return false;
        }
        finally
        {
            if (temporary != null)
                try { File.Delete(BridgeFileAccess.Native(temporary)); }
                catch (Exception exception) { Mod.Log.Warn(exception, "Could not remove metadata temporary file"); }
        }
    }

    private static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        foreach (var c in value)
            if (c == '"' || c == '\\') result.Append('\\').Append(c);
            else if (c < ' ') result.Append("\\u").Append(((int)c).ToString("x4"));
            else result.Append(c);
        return result.Append('"').ToString();
    }
}
