using Game.Prefabs;
using BridgeBuilder.Settings;
using System;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.SceneFlow;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The content that owns a bridge archetype and the game's own answer to whether that content is
/// currently available. This is read from prefab metadata; names, bridge families and filesystem
/// locations are never used to guess whether a DLC or mod is installed.
/// </summary>
internal sealed class BridgePrototypeSource
{
    private const string BxpBaseId = "92245";
    private const string BxpPortsId = "124160";
    private readonly PrefabBase? _prefab;
    private readonly ContentPrefab? _prerequisite;
    private readonly AssetPackPrefab[] _packs;
    private readonly bool _metadataReadable;
    private readonly string? _modId;

    private BridgePrototypeSource(
        string label, ContentPrefab? prerequisite, AssetPackPrefab[] packs, bool metadataReadable = true, string? modId = null,
        PrefabBase? prefab = null)
    {
        Label = label;
        _prerequisite = prerequisite;
        _packs = packs;
        _metadataReadable = metadataReadable;
        _modId = modId;
        _prefab = prefab;
    }

    internal string Label { get; }

    internal bool IsBaseGame => _metadataReadable && _prerequisite == null && _packs.Length == 0
        && Label == "Base game";

    // Stable ownership identity; localized display names must not decide which donor is selected.
    internal string Key => _modId != null ? "mod:" + _modId : _prerequisite != null ? "content:" + _prerequisite.name
        : _packs.Length > 0 ? "pack:" + string.Join("|", _packs.Select(pack => pack.name).Distinct().OrderBy(name => name, StringComparer.Ordinal))
        : IsBaseGame ? "base" : Label;

    internal bool HasSingleSource => _metadataReadable
        && (_modId != null || _prerequisite != null || _packs.Select(pack => pack.name).Distinct().Count() <= 1);

    internal int Priority => _modId != null ? 2 : IsBaseGame ? 0
        : _prerequisite?.GetComponent<DlcRequirement>() != null || (_packs.Length > 0 && _packs.All(pack => pack.isBuiltin)) ? 1 : 2;

    // Ownership/availability stays metadata-driven. This property only translates
    // the UI presentation, and re-reads content names after a language change.
    internal string LocalizedLabel
    {
        get
        {
            if (!_metadataReadable) return RuntimeUiText.Get("SourceUnreadable");
            if (_modId != null)
            {
                var label = RuntimeUiText.Get("SourceMod", ModDisplayName(_modId));
                return _prerequisite?.GetComponent<DlcRequirement>() != null
                    ? label + " + " + RuntimeUiText.Get("SourceDlc", DeckCatalog.DisplayNameOf(_prerequisite))
                    : label;
            }
            if (_prerequisite != null)
            {
                if (_prerequisite.GetComponent<DlcRequirement>() != null)
                    return RuntimeUiText.Get("SourceDlc", DeckCatalog.DisplayNameOf(_prerequisite));
                var mod = _prerequisite.GetComponent<ModRequirement>();
                if (mod != null && !string.IsNullOrWhiteSpace(mod.m_ModId))
                    return RuntimeUiText.Get("SourceMod", mod.m_ModId);
                return RuntimeUiText.Get("SourceContent", DeckCatalog.DisplayNameOf(_prerequisite));
            }
            if (_packs.Length > 0)
                return string.Join(", ", _packs.Select(pack => RuntimeUiText.Get(
                    pack.isBuiltin ? "SourcePack" : "SourceMod", PackDisplayName(pack))).Distinct());
            // Label is constructed internally by Inspect, never supplied by the player.
            return Label == "Base game" ? RuntimeUiText.Get("BaseGame") :
                RuntimeUiText.Get("SourceMod", Label.StartsWith("Mod: ", StringComparison.Ordinal) ? Label.Substring(5) : Label);
        }
    }

    /// <summary>
    /// Uses the same prerequisite object as the game UI. A registered prefab is not sufficient:
    /// prefabs belonging to disabled DLC and unsubscribed asset packs may remain in the database.
    /// </summary>
    internal bool IsAvailable
    {
        get
        {
            try
            {
                if (!_metadataReadable || _prefab == null || !_prefab.active) return false;
                if (_prerequisite != null && !_prerequisite.IsAvailable()) return false;
                // DLC ownership and mod ownership are independent gates, not alternatives.
                if (_packs.Length > 0 && !_packs.All(pack => PackAvailable(pack,
                    IsBxp(_modId) && pack.name == "Bridge Asset Pack Filter"))) return false;
                if (_prefab.isSubscribedMod) return OwnerAvailable(_prefab);
                if (_prefab.isBuiltin) return true;
                return _packs.Length > 0 || OwnerAvailable(_prefab);
            }
            catch (Exception)
            {
                // An unreadable prerequisite is not permission to publish a dangling dependency.
                return false;
            }
        }
    }

    private static bool PackAvailable(AssetPackPrefab pack, bool sharedBxpCategory)
    {
        if (pack == null || !pack.active) return false;
        var requirement = pack.GetComponent<ContentPrerequisite>();
        if (requirement != null && (requirement.m_ContentPrerequisite == null
            || !requirement.m_ContentPrerequisite.IsAvailable())) return false;
        // Both BXP downloads ship this same category asset. The registered copy can belong to
        // either package; it is not an extra mod dependency. The donor's own platform ID is gated.
        return sharedBxpCategory || pack.isBuiltin || OwnerAvailable(pack);
    }

    private static bool OwnerAvailable(PrefabBase prefab)
    {
        if (prefab.isSubscribedMod)
        {
            var id = prefab.asset?.GetMeta().platformID;
            return !string.IsNullOrWhiteSpace(id)
                && AssetDatabase<ParadoxMods>.instance.dataSource is ParadoxModsDataSource source
                && source.ContainsActiveMod(id!);
        }
        // Runtime prefabs can be supplied by a loaded code mod without a PDX asset.
        var assembly = prefab.GetType().Assembly;
        return assembly != typeof(PrefabBase).Assembly && GameManager.instance?.modManager != null
            && GameManager.instance.modManager.Any(mod => mod.state == ModManager.ModInfo.State.Loaded
                && mod.asset?.assembly == assembly);
    }

    internal static BridgePrototypeSource Inspect(PrefabBase prefab, bool bxpGoldenGate = false)
    {
        try
        {
            var requirement = prefab.GetComponent<ContentPrerequisite>();
            var prerequisite = requirement?.m_ContentPrerequisite;
            var modId = prefab.isSubscribedMod ? prefab.asset?.GetMeta().platformID : null;
            var packs = prefab.GetComponent<AssetPackItem>()?.m_Packs;
            if ((requirement != null && prerequisite == null)
                || (packs != null && packs.Any(pack => pack == null))
                || (prefab.isSubscribedMod && string.IsNullOrWhiteSpace(modId))
                || (bxpGoldenGate && !IsBxp(modId)))
            {
                return new BridgePrototypeSource("Unreadable content prerequisite", prerequisite,
                    Array.Empty<AssetPackPrefab>(), metadataReadable: false, prefab: prefab);
            }
            packs ??= Array.Empty<AssetPackPrefab>();
            // Ownership comes from the actual donor, never from the shared category name or DLC.
            // In particular, a B&P bridge must not be admitted just because base BXP is enabled.
            if (!string.IsNullOrWhiteSpace(modId))
            {
                var label = "Mod: " + ModDisplayName(modId!);
                if (prerequisite != null) label += " + " + RequirementLabel(prerequisite);
                return new BridgePrototypeSource(label, prerequisite, packs, modId: modId, prefab: prefab);
            }
            if (prerequisite != null)
                return new BridgePrototypeSource(RequirementLabel(prerequisite), prerequisite, packs, prefab: prefab);
            if (packs.Length > 0)
            {
                var labels = packs
                    .Select(PackLabel)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase);
                return new BridgePrototypeSource(string.Join(", ", labels), null, packs, prefab: prefab);
            }

            if (prefab.isBuiltin)
                return new BridgePrototypeSource("Base game", null, Array.Empty<AssetPackPrefab>(), prefab: prefab);

            return new BridgePrototypeSource("Mod: " + AssetSource(prefab), null, Array.Empty<AssetPackPrefab>(), prefab: prefab);
        }
        catch (Exception)
        {
            // Unknown ownership cannot prove that a prerequisite is installed. Fail closed so a
            // malformed ContentPrerequisite never becomes a generated dangling reference.
            return new BridgePrototypeSource(
                "Unreadable content prerequisite", null, Array.Empty<AssetPackPrefab>(), metadataReadable: false);
        }
    }

    private static string RequirementLabel(ContentPrefab prerequisite)
    {
        var dlc = prerequisite.GetComponent<DlcRequirement>();
        if (dlc != null) return "DLC: " + DeckCatalog.DisplayNameOf(prerequisite);

        var mod = prerequisite.GetComponent<ModRequirement>();
        if (mod != null && !string.IsNullOrWhiteSpace(mod.m_ModId)) return "Mod: " + mod.m_ModId;

        return "Content: " + DeckCatalog.DisplayNameOf(prerequisite);
    }

    private static string PackLabel(AssetPackPrefab pack)
    {
        var name = PackDisplayName(pack);
        return pack.isBuiltin ? "DLC/asset pack: " + name : "Mod: " + name;
    }

    private static bool IsBxp(string? id) => id == BxpBaseId || id == BxpPortsId;

    private static string ModDisplayName(string id) => id switch
    {
        BxpBaseId => "Bridge Expansion Pack",
        BxpPortsId => "Bridge Expansion Pack: B&P",
        _ => id,
    };

    private static string PackDisplayName(AssetPackPrefab pack) =>
        pack.isSubscribedMod && IsBxp(pack.asset?.GetMeta().platformID)
            ? ModDisplayName(pack.asset!.GetMeta().platformID) : DeckCatalog.DisplayNameOf(pack);

    private static string AssetSource(PrefabBase prefab)
    {
        var path = prefab.asset?.path;
        if (string.IsNullOrWhiteSpace(path)) return prefab.name ?? "Unknown";

        var parts = path!.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? parts[parts.Length - 2] : path;
    }
}
