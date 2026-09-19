using Game.Prefabs;
using BridgeBuilder.Settings;
using System;
using System.Linq;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The content that owns a bridge archetype and the game's own answer to whether that content is
/// currently available. This is read from prefab metadata; names, bridge families and filesystem
/// locations are never used to guess whether a DLC or mod is installed.
/// </summary>
internal sealed class BridgePrototypeSource
{
    private const string BxpDisplayName = "Bridge Extension Pack";
    private readonly ContentPrefab? _prerequisite;
    private readonly AssetPackPrefab[] _packs;
    private readonly bool _metadataReadable;
    private readonly bool _bxpOwner;

    private BridgePrototypeSource(
        string label, ContentPrefab? prerequisite, AssetPackPrefab[] packs, bool metadataReadable = true, bool bxpOwner = false)
    {
        Label = label;
        _prerequisite = prerequisite;
        _packs = packs;
        _metadataReadable = metadataReadable;
        _bxpOwner = bxpOwner;
    }

    internal string Label { get; }

    internal bool IsBaseGame => _metadataReadable && _prerequisite == null && _packs.Length == 0
        && Label == "Base game";

    // Stable ownership identity; localized display names must not decide which donor is selected.
    internal string Key => _bxpOwner ? "pack:Bridge Asset Pack Filter" : _prerequisite != null ? "content:" + _prerequisite.name
        : _packs.Length > 0 ? "pack:" + string.Join("|", _packs.Select(pack => pack.name).Distinct().OrderBy(name => name, StringComparer.Ordinal))
        : IsBaseGame ? "base" : Label;

    internal bool HasSingleSource => _metadataReadable
        && (_prerequisite != null || _packs.Select(pack => pack.name).Distinct().Count() <= 1);

    internal int Priority => _bxpOwner ? 2 : IsBaseGame ? 0
        : _prerequisite?.GetComponent<DlcRequirement>() != null || (_packs.Length > 0 && _packs.All(pack => pack.isBuiltin)) ? 1 : 2;

    // Ownership/availability stays metadata-driven. This property only translates
    // the UI presentation, and re-reads content names after a language change.
    internal string LocalizedLabel
    {
        get
        {
            if (!_metadataReadable) return RuntimeUiText.Get("SourceUnreadable");
            if (_bxpOwner) return RuntimeUiText.Get("SourceMod", BxpDisplayName);
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
                if (!_metadataReadable) return false;
                if (_prerequisite != null && !_prerequisite.IsAvailable()) return false;
                if (_packs.Length == 0) return true;
                return _packs.Any(pack => pack != null
                    && pack.active
                    && (pack.isBuiltin || pack.isSubscribedMod));
            }
            catch (Exception)
            {
                // An unreadable prerequisite is not permission to publish a dangling dependency.
                return false;
            }
        }
    }

    internal static BridgePrototypeSource Inspect(PrefabBase prefab, bool bxpGoldenGate = false)
    {
        try
        {
            var prerequisite = prefab.GetComponent<ContentPrerequisite>()?.m_ContentPrerequisite;
            if (bxpGoldenGate)
            {
                // Recorded owner of BXP's two double-deck Golden Gate archetypes. Ownership is
                // distinct from the San Francisco DLC dependency; both remain availability gates.
                var owner = prefab.GetComponent<AssetPackItem>()?.m_Packs?
                    .Where(pack => pack != null && pack.name == "Bridge Asset Pack Filter" && !pack.isBuiltin)
                    .ToArray() ?? Array.Empty<AssetPackPrefab>();
                return new BridgePrototypeSource("Mod: " + BxpDisplayName, prerequisite, owner,
                    metadataReadable: owner.Length > 0, bxpOwner: true);
            }
            if (prerequisite != null)
                return new BridgePrototypeSource(RequirementLabel(prerequisite), prerequisite, Array.Empty<AssetPackPrefab>());

            var packs = prefab.GetComponent<AssetPackItem>()?.m_Packs?
                .Where(pack => pack != null)
                .ToArray() ?? Array.Empty<AssetPackPrefab>();
            if (packs.Length > 0)
            {
                var labels = packs
                    .Select(PackLabel)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase);
                return new BridgePrototypeSource(string.Join(", ", labels), null, packs);
            }

            if (prefab.isBuiltin)
                return new BridgePrototypeSource("Base game", null, Array.Empty<AssetPackPrefab>());

            return new BridgePrototypeSource("Mod: " + AssetSource(prefab), null, Array.Empty<AssetPackPrefab>());
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

    // Presentation only: retain the exact pack identity and subscription checks.
    private static string PackDisplayName(AssetPackPrefab pack) =>
        !pack.isBuiltin && pack.name == "Bridge Asset Pack Filter"
            ? BxpDisplayName : DeckCatalog.DisplayNameOf(pack);

    private static string AssetSource(PrefabBase prefab)
    {
        var path = prefab.asset?.path;
        if (string.IsNullOrWhiteSpace(path)) return prefab.name ?? "Unknown";

        var parts = path!.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? parts[parts.Length - 2] : path;
    }
}
