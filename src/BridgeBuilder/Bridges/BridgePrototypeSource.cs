using Game.Prefabs;
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
    private readonly ContentPrefab? _prerequisite;
    private readonly AssetPackPrefab[] _packs;
    private readonly bool _metadataReadable;

    private BridgePrototypeSource(
        string label, ContentPrefab? prerequisite, AssetPackPrefab[] packs, bool metadataReadable = true)
    {
        Label = label;
        _prerequisite = prerequisite;
        _packs = packs;
        _metadataReadable = metadataReadable;
    }

    internal string Label { get; }

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
                if (_prerequisite != null) return _prerequisite.IsAvailable();
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

    internal static BridgePrototypeSource Inspect(PrefabBase prefab)
    {
        try
        {
            var prerequisite = prefab.GetComponent<ContentPrerequisite>()?.m_ContentPrerequisite;
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
        var name = DeckCatalog.DisplayNameOf(pack);
        return pack.isBuiltin ? "DLC/asset pack: " + name : "Mod: " + name;
    }

    private static string AssetSource(PrefabBase prefab)
    {
        var path = prefab.asset?.path;
        if (string.IsNullOrWhiteSpace(path)) return prefab.name ?? "Unknown";

        var parts = path!.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? parts[parts.Length - 2] : path;
    }
}
