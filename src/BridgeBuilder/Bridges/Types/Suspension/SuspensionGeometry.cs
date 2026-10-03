using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Immutable metaprogram output for the two numbered suspension prototypes.
///
/// The source-prefab anatomy identifies two kinds of geometry which a height/profile heuristic cannot
/// safely rediscover from a coarse mesh. The net pieces are continuous transverse sheets and therefore
/// inherit one affine transform from the full-detail span. The named shaft/base meshes contain only
/// side material and therefore translate rigidly. Every LOD is listed explicitly so it inherits the
/// full-detail decision instead of voting again from simplified topology.
/// </summary>
internal static class SuspensionGeometry
{
    // Exact left-to-right bounds recorded from the full-detail prototype prefabs. The public piece
    // widths are rounded to 21.5/22.6 m; geometry uses these retained float coordinates instead.
    private const float Suspension01NetSpan = 21.49888f;
    private const float Suspension02NetSpan = 22.52912f;

    private static readonly IReadOnlyDictionary<string, float> ContinuousSheets =
        new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["SuspensionBridge01EndNet Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge01EndNet_LOD1 Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge01EndNet_LOD2 Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge01MiddleNet Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge01MiddleNet_LOD1 Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge01MiddleNet_LOD2 Mesh"] = Suspension01NetSpan,
            ["SuspensionBridge02Net Mesh"] = Suspension02NetSpan,
            ["SuspensionBridge02Net_LOD1 Mesh"] = Suspension02NetSpan,
            ["SuspensionBridge02Net_LOD2 Mesh"] = Suspension02NetSpan,
        };

    private static readonly HashSet<string> RigidSideParts = new(StringComparer.Ordinal)
    {
        "SuspensionBridge01NetPillar Mesh",
        "SuspensionBridge01NetPillar_LOD1 Mesh",
        "SuspensionBridge01NetPillar_LOD2 Mesh",
        "SuspensionBridge01NetPillarBase Mesh",
        "SuspensionBridge01NetPillarBase_LOD1 Mesh",
        "SuspensionBridge01NetPillarBase_LOD2 Mesh",
        "SuspensionBridge01NetPylon Mesh",
        "SuspensionBridge01NetPylon_LOD1 Mesh",
        "SuspensionBridge01NetPylon_LOD2 Mesh",
        "SuspensionBridge01NetPylonBase Mesh",
        "SuspensionBridge01NetPylonBase_LOD1 Mesh",
        "SuspensionBridge01NetPylonBase_LOD2 Mesh",
        "SuspensionBridge02NetPillar Mesh",
        "SuspensionBridge02NetPillar_LOD1 Mesh",
        "SuspensionBridge02NetPillar_LOD2 Mesh",
        "SuspensionBridge02NetPillarBase Mesh",
        "SuspensionBridge02NetPillarBase_LOD1 Mesh",
        "SuspensionBridge02NetPillarBase_LOD2 Mesh",
        "SuspensionBridge02NetPylon Mesh",
        "SuspensionBridge02NetPylon_LOD1 Mesh",
        "SuspensionBridge02NetPylon_LOD2 Mesh",
        "SuspensionBridge02NetPylonBase Mesh",
        "SuspensionBridge02NetPylonBase_LOD1 Mesh",
        "SuspensionBridge02NetPylonBase_LOD2 Mesh",
    };

    internal static bool TryGetContinuousSpan(string? styleId, string? meshName, out float span)
    {
        span = 0f;
        if (styleId is not ("Suspension01" or "Suspension02") || string.IsNullOrEmpty(meshName))
            return false;

        return ContinuousSheets.TryGetValue(meshName!, out span);
    }

    internal static bool IsRigidSidePart(string? styleId, string? meshName) =>
        styleId is "Suspension01" or "Suspension02"
        && !string.IsNullOrEmpty(meshName)
        && RigidSideParts.Contains(meshName!);
}
