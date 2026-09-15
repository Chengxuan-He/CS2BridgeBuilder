using System;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Persistent identity and immutable construction recipe for one bridge created by the in-game UI.
/// <para>
/// <see cref="PrefabName"/> is the stable identity used by prefab references and every destructive
/// operation. <see cref="RegistrationName"/> is only the player-facing label and may be changed
/// without renaming the prefab or invalidating a saved game.
/// </para>
/// </summary>
internal sealed class BridgeRegistration
{
    internal BridgeRegistration(
        string prefabName,
        string registrationName,
        string upperDeckId,
        string? lowerDeckId,
        string styleId,
        string createdUtc)
    {
        PrefabName = prefabName;
        RegistrationName = registrationName;
        UpperDeckId = upperDeckId;
        LowerDeckId = lowerDeckId;
        StyleId = styleId;
        CreatedUtc = createdUtc;
    }

    internal string PrefabName { get; }

    internal string RegistrationName { get; set; }

    internal string UpperDeckId { get; }

    internal string? LowerDeckId { get; }

    internal string StyleId { get; }

    internal string CreatedUtc { get; }

    internal bool IsDoubleDeck => !string.IsNullOrEmpty(LowerDeckId);

    internal static string NewPrefabName() => "b" + Guid.NewGuid().ToString("D");

    internal static bool IsPrefabName(string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value!.Length > 1
            && value[0] == 'b'
            && Guid.TryParseExact(value.Substring(1), "D", out _);
    }
}
