using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

/// <summary>
/// One-sided x corrections produced by the retained-prefab width audit.
///
/// Each bridge-specific branch owns its own entry. Values are the round-trip binary32 results from
/// <c>tools/AuditBridgeWidths.ps1</c>; no displayed or rounded measurement is used here. The geometry
/// widening APIs take a full span, so <see cref="FullSpanFor"/> applies the same audited coordinate
/// correction to both sides.
/// </summary>
internal static class BridgeWidthAuditCorrections
{
    private static readonly Dictionary<string, float> OneSided =
        new(StringComparer.Ordinal)
        {
            ["CoveredWood"] = -1f, // 0xBF800000
            ["Extradosed03"] = 0.4999981f, // 0x3EFFFFC0
        };

    internal static float OneSidedFor(string? styleId)
    {
        if (styleId == null) return 0f;
        return OneSided.TryGetValue(styleId, out var correction) ? correction : 0f;
    }

    internal static float FullSpanFor(string? styleId) => OneSidedFor(styleId) * 2f;
}
