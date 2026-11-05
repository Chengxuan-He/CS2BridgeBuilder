using UnityEngine;

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
        if (source.Length != data[row] || positions.Length != data[row])
            throw new System.InvalidOperationException("Draw02 moving base requires its measured main mesh.");
        AffineGeometry.For(Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[2], data[row + Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[3]], i =>
        {
            int vertex = data[data[row + Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[4]] + i];
            float x = source[vertex].x;
            positions[vertex].x = AffineGeometry.Translate(x, x < Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[5] ? -delta : x > Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[6] ? delta : Numeric_Types_Draw02_Draw02MovingBase_cs.Values.I32[7]);
        });
    }
}

internal static class Draw02MovingBaseMeasurements
{
    static Draw02MovingBaseMeasurements()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_MovingBaseMeasurements");
}

// Embedded numerical arrays: Types_Draw02_Draw02MovingBase_cs
internal static class Numeric_Types_Draw02_Draw02MovingBase_cs
{
    static Numeric_Types_Draw02_Draw02MovingBase_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02MovingBase_cs");
}
