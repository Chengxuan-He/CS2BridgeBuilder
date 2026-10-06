using BridgeBuilder.Bridges;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Runtime;

/// <summary>Asset-derived catalogue. Reads metadata directly from each prefab.</summary>
internal static class BridgeAssetCatalog
{
    private static readonly Dictionary<string, BridgeAssetInfo> Creating = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PrefabBase> CreatingPrefabs = new(StringComparer.Ordinal);
    internal static string Root => Path.Combine(UnityEngine.Application.persistentDataPath, "ImportedData");
    private static string PathFor(string owner) => Path.Combine(Root, owner, owner + ".Prefab");

    internal static IReadOnlyList<BridgeAssetInfo> Load() => ReadAll().Where(e => !e.Pending)
        .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();

    internal static List<BridgeAssetInfo> ReadAll()
    {
        var result = new List<BridgeAssetInfo>();
        try
        {
            if (!BridgeFileAccess.Exists(Root) || (BridgeFileAccess.Attributes(Root) & FileAttributes.ReparsePoint) != 0)
                return result;
            foreach (var directory in Directory.GetDirectories(BridgeFileAccess.Native(Root)))
            {
                var owner = Path.GetFileName(directory);
                if (!BridgeAssetInfo.IsPrefabName(owner) || BridgeStartupRecovery.Retired.Contains(owner)) continue;
                var entry = Read(owner);
                if (entry != null) result.Add(entry);
            }
        }
        catch (Exception exception) { Mod.Log.Warn(exception, "Could not enumerate bridge assets"); }
        return result;
    }

    private static BridgeAssetInfo? Read(string owner)
    {
        if (!BridgeAssetInfo.IsPrefabName(owner)) return null;
        try
        {
            var path = PathFor(owner);
            if (!BridgeFileAccess.Exists(path) || (BridgeFileAccess.Attributes(Path.GetDirectoryName(path)!) & FileAttributes.ReparsePoint) != 0
                || (BridgeFileAccess.Attributes(path) & FileAttributes.ReparsePoint) != 0) return null;
            return BridgeAssetMetadata.TryRead(BridgeFileAccess.ReadText(path), out var entry)
                && entry.PrefabName == owner ? entry : null;
        }
        catch (Exception exception) { Mod.Log.Warn(exception, "Could not read bridge metadata: " + owner); return null; }
    }

    internal static BridgeAssetInfo? Find(string? owner) => owner == null || BridgeStartupRecovery.Retired.Contains(owner)
        ? null : Read(owner) is { Pending: false } entry ? entry : null;

    internal static bool Begin(BridgeAssetInfo entry)
    {
        if (!BridgeAssetInfo.IsPrefabName(entry.PrefabName) || Creating.ContainsKey(entry.PrefabName)
            || BridgeFileAccess.Exists(PathFor(entry.PrefabName))) return false;
        Creating.Add(entry.PrefabName, entry);
        return true;
    }

    internal static void Attach(PrefabBase prefab, BridgeAssetInfo fallback)
    {
        var entry = Creating.TryGetValue(prefab.name, out var pending) ? pending : fallback;
        var data = prefab.GetComponent<BridgeConstructionCost>() ?? prefab.AddComponent<BridgeConstructionCost>();
        Apply(data, entry);
        if (Creating.ContainsKey(prefab.name)) CreatingPrefabs[prefab.name] = prefab;
    }

    private static void Apply(BridgeConstructionCost data, BridgeAssetInfo entry)
    {
        data.m_BridgeDisplayName = entry.DisplayName;
        data.m_BridgeUpperDeckId = entry.UpperDeckId;
        data.m_BridgeLowerDeckId = entry.LowerDeckId ?? "";
        data.m_BridgeStyleId = entry.StyleId;
        data.m_BridgeCreatedUtc = entry.CreatedUtc;
        data.m_BridgeCreationPending = entry.Pending;
    }

    internal static bool Commit(string owner)
    {
        if (!Creating.TryGetValue(owner, out var pending)) return false;
        var entry = new BridgeAssetInfo(owner, pending.DisplayName, pending.UpperDeckId,
            pending.LowerDeckId, pending.StyleId, pending.CreatedUtc);
        if (!BridgeAssetMetadata.Write(PathFor(owner), entry)) return false;
        if (CreatingPrefabs.TryGetValue(owner, out var prefab)
            && prefab.GetComponent<BridgeConstructionCost>() is { } data) Apply(data, entry);
        EndCreation(owner);
        return true;
    }

    internal static bool Rename(string owner, string label)
    {
        var entry = Find(owner);
        if (entry == null || string.IsNullOrWhiteSpace(label)) return false;
        entry.DisplayName = label.Trim();
        return BridgeAssetMetadata.Write(PathFor(owner), entry);
    }
    internal static void EndCreation(string owner) { Creating.Remove(owner); CreatingPrefabs.Remove(owner); }
    internal static void ResetSession() { Creating.Clear(); CreatingPrefabs.Clear(); }
}
