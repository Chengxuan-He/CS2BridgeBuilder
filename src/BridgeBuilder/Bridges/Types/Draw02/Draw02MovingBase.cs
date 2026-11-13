using UnityEngine;
using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
// Measured transverse supports immediately below the moving road, not hinge
// axles or the central longitudinal girder and its adjoining closure panels.
internal static class Draw02MovingBase
{
    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)
    {
        if (lod == Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[0])
            return; // The native merged far shell already uses this map.
        var data = Draw02MovingBaseMeasurements.Values.I32;
        int row = lod * Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[1];
        var left = new List<int>();
        var right = new List<int>();
        var centre = new List<int>();
        for (int i = Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[2]; i < data[row + Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[3]]; i++)
        {
            int vertex = data[data[row + Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[4]] + i];
            float x = source[vertex].x;
            if (x < Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[5]) left.Add(vertex);
            else if (x > Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[6]) right.Add(vertex);
            else centre.Add(vertex);
        }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.NegativeTranslateX, new[] { source, positions },
            Array.Empty<float[]>(), new[] { delta }, count: left.Count, indices: new[] { left.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.TranslateX, new[] { source, positions },
            Array.Empty<float[]>(), new[] { delta }, count: right.Count, indices: new[] { right.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.TranslateX, new[] { source, positions },
            Array.Empty<float[]>(), new float[] { Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[7] },
            count: centre.Count, indices: new[] { centre.ToArray() });
    }
}

internal static class Draw02MovingBaseMeasurements
{
    static Draw02MovingBaseMeasurements()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw02MovingBaseMeasurements.Values", () => HardcodedNumbers.Load("Types_Draw02_MovingBaseMeasurements"));
}

// Embedded numerical arrays: Types_Draw02_Draw02MovingBase_cs
internal static class Numeric_Types_Draw02_Draw02MovingBase_cs
{
    static Numeric_Types_Draw02_Draw02MovingBase_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02MovingBase_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02MovingBase_cs"));
}
