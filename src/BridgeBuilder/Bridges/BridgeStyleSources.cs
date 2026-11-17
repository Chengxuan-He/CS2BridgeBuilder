using System;

namespace BridgeBuilder.Bridges;

/// <summary>Ordered required native identities. First entry owns the prototype; no recursive dependency inference.</summary>
internal static class BridgeStyleSources
{
    internal static string[] For(string id) => id switch
    {
        "TrussViaduct" or "SuspensionDouble" => new[] { "mod:92245" },
        "ArtDecoViaduct" or "GoldenGateDouble" => new[] { "mod:92245", "content:SanFranciscoSet" },
        "GoldenGate" => new[] { "content:SanFranciscoSet" },
        "GOViaduct" => new[] { "mod:166012" },
        "Nanpu" => new[] { "mod:162995" },
        "Lupu" => new[] { "mod:151335" },
        "PedestrianDraw01" or "PedestrianDraw02" or
        "Draw01" or "Draw02" or "Draw03" or
        "Lift01" or "Lift02" or "Lift03" or "Lift04" or "Lift05" or
        "CoveredWood" or "Suspension01" or "Suspension02" or "SuspensionGolden" or
        "Extradosed02" or "Extradosed03" or "TrussArch02" or "TrussArch03"
            => new[] { "content:BridgesAndPorts" },
        "ArcPedestrian" or "CoveredPedestrian" or "WoodenCovered" or "Suspension" or
        "Extradosed01" or "ExtradosedLarge" or "CableStayed" or "TrussArch01" or
        "TrussArch" or "TiedArch" or "Grand" => Array.Empty<string>(),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Style sources are not registered.")
    };
}
