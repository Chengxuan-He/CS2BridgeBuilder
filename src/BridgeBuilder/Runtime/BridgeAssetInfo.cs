using System;

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
