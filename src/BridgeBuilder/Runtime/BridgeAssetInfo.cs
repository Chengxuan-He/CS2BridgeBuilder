using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Asset-local identity and construction metadata for one bridge created by the in-game UI.
/// <para>
/// <see cref="PrefabName"/> is the stable identity used by prefab references and every destructive
/// operation. <see cref="DisplayName"/> is only the player-facing label and may be changed
/// without renaming the prefab or invalidating a saved game.
/// </para>
/// </summary>
internal sealed class BridgeAssetInfo
{
    internal static bool IsDependencyPath(string path, string owner)
    {
        var parts = path.Split('\\', '/');
        for (var i = 0; i < parts.Length; i++)
            if (parts[i].Equals(owner + "_Dependencies", StringComparison.OrdinalIgnoreCase)
                || (parts[i].Equals(owner, StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length
                    && parts[i + 1].Equals("Dependencies", StringComparison.OrdinalIgnoreCase))) return true;
        return false;
    }

    internal static string ShortName(string name, string owner)
    {
        return new Regex("-?" + Regex.Escape(owner) + "[-_ ]?", RegexOptions.IgnoreCase).Replace(name, "", 1);
    }

    // Input is the asset name/stem, never a payload filename with its real extension.
    internal static string FileStem(string name, string owner)
    {
        var chars = (owner.Length == 0 ? name : ShortName(name, owner)).ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (chars[i] < 32 || ".<>:\"/\\|?*".IndexOf(chars[i]) >= 0)
                chars[i] = '_';
        return new string(chars);
    }

    internal static string OwnedDirectory(string owner, string category) => "ImportedData/" + owner + "/" + category;

    internal static string? AssetCategory(string extension) => extension.ToLowerInvariant() switch
    {
        ".prefab" => "Prefabs", ".geometry" => "Geometry",
        ".surface" or ".material" or ".texture" => "Appearance", ".locale" or ".loc" => "Locales",
        ".vtsurface" or ".vttexture" => "Appearance/VT", ".midmips" => "Appearance/MidMips", _ => null
    };

    private static readonly Regex AssetOwner = new(@"b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}");
    internal static bool TryFileOwner(string? path, out string owner)
    {
        var match = AssetOwner.Match(path ?? "");
        owner = match.Value;
        return match.Success;
    }
    internal static bool MatchesOwner(string? path, string owner) => IsPrefabName(owner)
        && path != null && path.IndexOf(owner, StringComparison.OrdinalIgnoreCase) >= 0;

    internal static bool IsOtherBridgePrefab(string? path, string owner, string importedRoot)
        => path != null && path.EndsWith(".Prefab", StringComparison.OrdinalIgnoreCase)
            && IsOtherBridgeAsset(path, owner, importedRoot);

    internal static bool IsOtherBridgeAsset(string? path, string owner, string importedRoot)
    {
        if (string.IsNullOrEmpty(path)) return false;
        var full = Path.GetFullPath(BridgeFileAccess.Logical(path));
        var root = Path.GetFullPath(importedRoot).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
        foreach (Match match in AssetOwner.Matches(full.Substring(root.Length)))
            if (!match.Value.Equals(owner, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal BridgeAssetInfo(
        string prefabName,
        string displayName,
        string upperDeckId,
        string? lowerDeckId,
        string styleId,
        string createdUtc,
        bool pending = false)
    {
        PrefabName = prefabName;
        DisplayName = displayName;
        UpperDeckId = upperDeckId;
        LowerDeckId = lowerDeckId;
        StyleId = styleId;
        CreatedUtc = createdUtc;
        Pending = pending;
    }

    internal string PrefabName { get; }

    internal string DisplayName { get; set; }

    internal string UpperDeckId { get; }

    internal string? LowerDeckId { get; }

    internal string StyleId { get; }

    internal string CreatedUtc { get; }

    // Persisted on the root prefab; cleared only after native publication succeeds.
    internal bool Pending { get; }

    internal bool IsDoubleDeck => !string.IsNullOrEmpty(LowerDeckId);

    internal static string NewPrefabName() => "b" + Guid.NewGuid().ToString("D");

    internal static bool IsPrefabName(string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value!.Length > 1
            // Permanent bridge identities use b{uuid} exclusively. Older assets
            // and their save references must be migrated offline, not aliased here.
            // tmp previews and Road Builder road IDs are not bridge identities.
            && value[0] == 'b'
            && Guid.TryParseExact(value.Substring(1), "D", out _);
    }
}
