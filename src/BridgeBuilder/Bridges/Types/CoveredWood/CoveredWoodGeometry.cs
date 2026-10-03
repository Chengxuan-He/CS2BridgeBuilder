using System;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>Above-deck centre uprights measured from PedestrianBridgeCoveredWood01Cover.</summary>
internal static class CoveredWoodGeometry
{
    internal static bool TryApply(string meshName, float3[] source, float3[] moved)
    {
        int lod;
        int vertices;
        switch (meshName)
        {
            case "PedestrianBridgeCoveredWood01Cover Mesh": lod = 0; vertices = 10564; break;
            case "PedestrianBridgeCoveredWood01Cover_LOD1 Mesh": lod = 1; vertices = 6563; break;
            case "PedestrianBridgeCoveredWood01Cover_LOD2 Mesh": lod = 2; vertices = 444; break;
            default: return false;
        }
        if (source.Length != vertices || moved.Length != vertices) return false;
        var ranges = CoveredWoodColumnData.Ranges[lod];
        // Validate before modifying the output. Exact authored membership replaces the height-slice
        // heuristic which gave the top and bottom of one upright different widths.
        for (var r = 0; r < ranges.Length; r += 2)
            for (var i = ranges[r]; i < ranges[r + 1]; i++)
                if (Math.Abs(source[i].x) != CoveredWoodColumnData.HalfSpan) return false;

        for (var r = 0; r < ranges.Length; r += 2)
            for (var i = ranges[r]; i < ranges[r + 1]; i++)
            {
                // User explicitly requires the pre-fix TOP width throughout the upright.
                // The metaprogram has resolved its rectangular faces; road widening must not
                // enlarge them again. Preserve authored y/z and all other mesh data and parts.
                moved[i].x = source[i].x < 0f ? CoveredWoodColumnData.Left : CoveredWoodColumnData.Right;
            }
        return true;
    }
}
