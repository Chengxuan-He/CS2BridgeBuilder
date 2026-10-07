using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Prefabs;
using UnityEngine;
using BridgeBuilder.Settings;

namespace BridgeBuilder.Runtime;

internal static class BridgePackIcon
{
    internal static bool Persist(AssetPackPrefab pack, string icon, out string error)
    {
        error = string.Empty;
        if (pack.asset == null || pack.isReadOnly)
        { error = "Pack has no writable asset: " + pack.asset?.path; return false; }
        var root = Path.GetFullPath(BridgeFileAccess.Logical(Path.Combine(Application.persistentDataPath, "ImportedData")));
        var cid = pack.asset.id.guid.ToString();
        // Identical-CID snapshots must all receive the same icon, otherwise load order wins.
        var copies = Directory.EnumerateFiles(BridgeFileAccess.Native(root), cid + ".Prefab", SearchOption.AllDirectories)
            .Select(BridgeFileAccess.Logical).Where(path => BridgeAssetInfo.TryFileOwner(path, out _));
        var canonical = Path.Combine(root, BridgeAssetPack.PrefabName, BridgeAssetPack.PrefabName + ".Prefab");
        var paths = copies.Concat(new[] { pack.asset.path, canonical }.Where(BridgeFileAccess.Exists))
            .Select(path => Path.GetFullPath(BridgeFileAccess.Logical(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length == 0) { error = "No local pack files found: " + pack.asset.path; return false; }
        var backup = Path.Combine(BridgeRecoveryLocation.Path, "PackIcon-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
        foreach (var path in paths)
        {
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            { error = "Pack path is outside ImportedData: " + full; return false; }
            var text = BridgeFileAccess.ReadText(full);
            if (!TryReplace(text, icon, out var updated))
            { error = "Pack icon field or serialized pack identity is invalid: " + full; return false; }
            if (updated == text) continue;
            var saved = Path.Combine(backup, full.Substring(root.Length + 1));
            Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(saved)!));
            File.Copy(BridgeFileAccess.Native(full), BridgeFileAccess.Native(saved));
            var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(BridgeFileAccess.Native(temporary), updated, new UTF8Encoding(false));
                File.Replace(BridgeFileAccess.Native(temporary), BridgeFileAccess.Native(full), null);
            }
            finally { if (File.Exists(BridgeFileAccess.Native(temporary))) File.Delete(BridgeFileAccess.Native(temporary)); }
        }
        return true;
    }

    internal static bool TryReplace(string text, string icon, out string updated)
    {
        updated = text;
        if (!icon.StartsWith("data:image/svg+xml;base64,", StringComparison.Ordinal)
            || !BridgeSerializedReferences.TryRead(text, out var document)
            || document.Name != BridgeAssetPack.PrefabName
            || document.Component("Game.Prefabs.UIObject, Game") is not { } ui
            || !ui.Fields.TryGetValue("m_Icon", out var field)) return false;
        if (field.Text != icon)
            updated = text.Remove(field.Start, field.End - field.Start).Insert(field.Start, "\"" + icon + "\"");
        return true;
    }
}
