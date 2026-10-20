using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

internal static class BridgeNativePresentation
{
    internal static bool Validate(string owner, IEnumerable<PrefabBase> roots, out string error)
    {
        error = "";
        try
        {
            var pending = new Stack<string>();
            ILookup<string, AssetData>? alternatives = null;
            foreach (var root in roots)
            {
                if (root.asset == null || !BridgeAssetInfo.MatchesOwner(root.asset.path, owner))
                { error = "Unowned persisted root"; return false; }
                pending.Push(root.asset.id.guid.ToString());
            }
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count != 0)
            {
                var id = pending.Pop();
                if (!visited.Add(id)) continue;
                var source = BridgeDependencyPersistence.Resolve(id, owner, ref alternatives);
                if (source == null) { error = "Saved dependency missing or disallowed: " + id; return false; }
                if (source.Builtin) continue;
                var extension = source.Extension;
                if (!extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".Material", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".Surface", StringComparison.OrdinalIgnoreCase)) continue;
                var contents = source.Bytes;
                var text = System.Text.Encoding.UTF8.GetString(contents);
                if (text.Contains(", BridgeBuilder\"") || text.IndexOf("coui://bridgebuilder", StringComparison.OrdinalIgnoreCase) >= 0)
                { error = "Saved prefab still requires Bridge Builder: " + id; return false; }
                foreach (var cid in BridgeDependencyCopies.References(source))
                {
                    pending.Push(cid);
                }
            }
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    internal static bool Save(BridgeAssetInfo entry, out string error)
        => Save(entry, false, out _, out error);

    internal static bool EnsureDescriptions(BridgeAssetInfo entry, out bool changed, out string error)
        => Save(entry, true, out changed, out error);

    private static bool Save(BridgeAssetInfo entry, bool missingOnly, out bool changed, out string error)
    {
        changed = false;
        error = "";
        try
        {
            if (!BridgeAssetInfo.IsPrefabName(entry.PrefabName)) { error = "Invalid bridge UUID"; return false; }
            // Native startup indexes SystemLanguage -> localeId before mods load.
            // Reusing Unknown for multiple locale IDs crashes that dictionary on a cold start.
            var localization = GameManager.instance?.localizationManager;
            if (localization == null) { error = "Native localization is not ready"; return false; }
            var locales = Settings.UiStringCatalog.LocaleIds.Select(locale => (
                Id: locale, Language: localization.LocaleIdToSystemLanguage(locale),
                Name: localization.GetLocalizedName(locale))).ToArray();
            if (locales.Any(locale => locale.Language == SystemLanguage.Unknown))
            { error = "Native language mapping unavailable for bridge locales"; return false; }
            var prefabs = AssetDatabase.user.GetAssets(SearchFilter<PrefabAsset>.ByCondition(a =>
                BridgeAssetInfo.MatchesOwner(a.path, entry.PrefabName)))
                .Select(a => a.GetInstance<PrefabBase>()).Where(p => p is NetGeometryPrefab).ToArray();
            foreach (var locale in locales)
            {
                var name = entry.PrefabName + "-" + locale.Id;
                var shortName = BridgeAssetInfo.FileStem(name, entry.PrefabName);
                var asset = AssetDatabase.user.GetAssets(SearchFilter<LocaleAsset>.ByCondition(a => (a.name == name || a.name == shortName || a.name == locale.Id || a.name == "-" + locale.Id)
                    && BridgeAssetInfo.MatchesOwner(a.path, entry.PrefabName))).FirstOrDefault();
                var existing = asset?.data;
                var entries = existing == null ? new Dictionary<string, string>() : new Dictionary<string, string>(existing.entries);
                var description = Settings.RuntimeUiText.Format(locale.Id, "BridgeAssetDescription");
                var tags = new[] { "", "_Lower", "_Upper" }.Select(s => entry.PrefabName + s)
                    .Concat(prefabs.Select(p => p!.uiTag)).Distinct();
                foreach (var tag in tags)
                {
                    var nameKey = "Assets.NAME[" + tag + "]";
                    var descriptionKey = "Assets.DESCRIPTION[" + tag + "]";
                    if (!missingOnly || (!entries.TryGetValue(nameKey, out var label) || string.IsNullOrWhiteSpace(label))) entries[nameKey] = entry.DisplayName;
                    if (!missingOnly || !entries.TryGetValue(descriptionKey, out var text) || string.IsNullOrWhiteSpace(text))
                        entries[descriptionKey] = description;
                }
                if (existing != null && entries.Count == existing.entries.Count
                    && entries.All(p => existing.entries.TryGetValue(p.Key, out var value) && value == p.Value)) continue;
                asset ??= AssetDatabase.user.AddAsset<LocaleAsset>(BridgePrefabPersistence.ResourcePath(name, "Locales"));
                asset.SetData(new LocaleData(locale.Id, entries, existing == null
                    ? new Dictionary<string, int>() : new Dictionary<string, int>(existing.indexCounts)), locale.Language, locale.Name);
                asset.Save(true);
                changed = true;
                // Native asset change notifications register this source; startup discovers it globally.
            }
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    internal static void Prepare(PrefabBase prefab)
    {
        // Migration clones use PrefabBase.Clone rather than PrefabGraphCloner.
        // Apply the same native-persistence policy: Road Builder editor groups can contain
        // session-only Prefabs and must never create unsaved CID dependencies in our assets.
        prefab.components.RemoveAll(component => component != null
            && CS2Mods.Shared.Conversion.PrefabGraphCloner.ShouldStripComponent(component, false));
        // Preserve the prototype's native icon URI. An empty URI does not request a thumbnail;
        // ImageSystem falls back to a generic icon instead. Validate rejects manager-host URIs.
    }
}
