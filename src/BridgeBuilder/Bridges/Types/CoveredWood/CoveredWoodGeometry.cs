using System;
using System.Collections.Generic;
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
            case "PedestrianBridgeCoveredWood01Cover Mesh":
                lod = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[0];
                vertices = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[1];
                break;
            case "PedestrianBridgeCoveredWood01Cover_LOD1 Mesh":
                lod = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[2];
                vertices = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[3];
                break;
            case "PedestrianBridgeCoveredWood01Cover_LOD2 Mesh":
                lod = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[4];
                vertices = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[5];
                break;
            default:
                return false;
        }
        var ranges = CoveredWoodColumnData.Ranges[lod];
        var left = new List<int>();
        var right = new List<int>();
        for (var r = Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[9]; r < ranges.Length; r += Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[10])
            for (int i = ranges[r]; i < ranges[r + Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.I32[11]]; i++)
            {
                // User explicitly requires the pre-fix TOP width throughout the upright.
                // The metaprogram has resolved its rectangular faces; road widening must not
                // enlarge them again. Preserve authored y/z and all other mesh data and parts.
                if (source[i].x < Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values.F32[0]) left.Add(i);
                else right.Add(i);
            }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ConstantX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { CoveredWoodColumnData.Left }, count: left.Count, indices: new[] { left.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ConstantX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { CoveredWoodColumnData.Right }, count: right.Count, indices: new[] { right.ToArray() });
        return true;
    }
}

// Embedded numerical arrays: Types_CoveredWood_CoveredWoodGeometry_cs
internal static class Numeric_Types_CoveredWood_CoveredWoodGeometry_cs
{
    static Numeric_Types_CoveredWood_CoveredWoodGeometry_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_CoveredWood_CoveredWoodGeometry_cs.Values", () => HardcodedNumbers.Load("Types_CoveredWood_CoveredWoodGeometry_cs"));
}
