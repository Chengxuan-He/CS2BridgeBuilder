using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Where each bridge type's towers get their look from.
///
/// A generated tower brings its own geometry - built in code, at whatever width the road needs - but
/// not its own materials. Painting it would mean shipping textures, and a concrete-grey pylon under a
/// painted deck reads as a mistake rather than as a plain choice. So the surfaces are taken from the
/// bridge the style already uses, which is a reference to installed content and not a copy of it.
///
/// What is hardcoded here is the *source*: the prefab whose surfaces to read. The surfaces themselves
/// are read at generation time, so a pack that recolours a bridge recolours the generated towers with
/// it, and nothing here has to be updated when a texture changes.
///
/// The names come from the Bridges &amp; Ports content. Each has a matching set of surface assets -
/// SuspensionBridge01NetPylon, SuspensionBridge01NetPylon_LOD2, ...Top, ...Base - and taking them from
/// the pylon means the generated tower is painted like the towers of the bridge it stands in for.
/// </summary>
internal static class BridgeTowerMaterials
{
    /// <summary>The tower prefab whose surfaces a generated tower borrows, keyed by style id.</summary>
    private static readonly Dictionary<string, string[]> Sources =
        new(StringComparer.Ordinal)
        {
            // The pale steel pair. 01 is the two-lane design and the one whose pylon is a plain tower
            // rather than a portal, which makes it the cleaner source of a flat colour.
            ["Suspension"] = new[] { "SuspensionBridge01NetPylon", "SuspensionBridge01NetPillar" },

            // The golden pair. 03 and 04 share one set of surfaces, named after 03.
            ["SuspensionGolden"] = new[] { "SuspensionBridge03Pillar", "SuspensionBridge03PylonTop" },

            ["Extradosed"] = new[] { "ExtradosedBridge01NetPillar", "ExtradosedBridge02NetPillar" },
            // The two arch-above colours are independent prototypes. Their generated structures must
            // follow their own source surfaces even when another truss-arch prefab is also installed.
            ["TrussArch01"] = new[] { "TrussArchBridge01NetPillar" },
            ["TrussArch02"] = new[] { "TrussArchBridge02NetPillar" },
            ["TrussArch03"] = new[] { "TrussArchBridge03NetPillar" },
            // The general arch-below family keeps the established pale material source. This is only
            // a surface reference; the generated object prefabs remain owned by their individual bridge.
            ["TrussArch"] = new[] { "TrussArchBridge02NetPillar" },
            ["TiedArch"] = new[] { "TiedArchBridge01NetPillar" },
            ["CableStayed"] = new[] { "8LaneCableStayedBridgePillar Placeholder" },
            ["Grand"] = new[] { "GrandBridgePillar Placeholder", "GrandBridgePylon Placeholder" },
            ["Draw"] = new[] { "DrawBridge03NetPillar", "DrawBridge02NetPillar" },
            ["Lift"] = new[] { "LiftBridge03NetPillar" },
        };

    /// <summary>
    /// The prefabs to read surfaces from, best first. Empty when the style has no recorded source, in
    /// which case the caller falls back to the surfaces of whichever tower it was going to use.
    /// </summary>
    internal static IReadOnlyList<string> SourcesFor(string styleId)
    {
        return Sources.TryGetValue(styleId, out var names) ? names : Array.Empty<string>();
    }
}
