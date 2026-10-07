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
            var pending = new Stack<AssetData>();
            foreach (var root in roots)
            {
                if (root.asset == null || !BridgeAssetInfo.MatchesOwner(root.asset.path, owner))
                { error = "Unowned persisted root"; return false; }
                pending.Push(root.asset);
            }
            var visited = new HashSet<Colossal.Hash128>();
            while (pending.Count != 0)
            {
                var asset = pending.Pop();
                if (asset.isBuiltin || !visited.Add(asset.id.guid)) continue;
                var extension = asset.GetMeta().extension;
                if (!extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".Material", StringComparison.OrdinalIgnoreCase)) continue;
                using var stream = asset.GetReadStream();
                using var bytes = new MemoryStream(); stream.CopyTo(bytes);
                var contents = bytes.ToArray();
                var text = System.Text.Encoding.UTF8.GetString(contents);
                if (text.Contains(", BridgeBuilder\"") || text.IndexOf("coui://bridgebuilder", StringComparison.OrdinalIgnoreCase) >= 0)
                { error = "Saved prefab still requires Bridge Builder: " + asset.path; return false; }
                foreach (var cid in BridgeDependencyCopies.References(contents))
                {
                    if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out AssetData dependency))
                    { error = "Saved dependency missing: " + cid; return false; }
                    pending.Push(dependency);
                }
            }
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    internal static bool Save(BridgeAssetInfo entry, out string error)
    {
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
                var asset = AssetDatabase.user.GetAssets(SearchFilter<LocaleAsset>.ByCondition(a => a.name == name)).FirstOrDefault()
                    ?? AssetDatabase.user.AddAsset<LocaleAsset>(AssetDataPath.Create("ImportedData/" + entry.PrefabName, name));
                var entries = new Dictionary<string, string>();
                foreach (var suffix in new[] { "", "_Lower", "_Upper" })
                    entries["Assets.NAME[" + entry.PrefabName + suffix + "]"] = entry.DisplayName;
                foreach (var prefab in prefabs)
                    entries["Assets.NAME[" + prefab.uiTag + "]"] = entry.DisplayName;
                asset.SetData(new LocaleData(locale.Id, entries, new Dictionary<string, int>()), locale.Language, locale.Name);
                asset.Save(true);
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
