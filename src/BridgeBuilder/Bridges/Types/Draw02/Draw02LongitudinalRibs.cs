using UnityEngine;

namespace BridgeBuilder.Bridges;
// Original thin longitudinal ribs retain their sections. Their centres share
// the added width equally, while the existing panels follow adjoining members.
internal static class Draw02LongitudinalRibs
{
    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)
    {
        if (lod == Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[0])
            return; // Native far shell has no independent thin ribs.
        var data = Draw02LongitudinalRibMeasurements.Values;
        int row = lod * Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[1];
        if (source.Length != data.I32[row] || positions.Length != data.I32[row])
            throw new System.InvalidOperationException("Draw02 longitudinal ribs require the measured main mesh.");
        AffineGeometry.For(Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[2], data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[3]], i =>
        {
            int vertex = data.I32[data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[4]] + i];
            positions[vertex].x = AffineGeometry.AddScaled(source[vertex].x, data.F32[data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[5]] + i], delta);
        });
    }
}

internal static class Draw02LongitudinalRibMeasurements
{
    static Draw02LongitudinalRibMeasurements()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_LongitudinalRibMeasurements");
}

// Embedded numerical arrays: Types_Draw02_Draw02LongitudinalRibs_cs
internal static class Numeric_Types_Draw02_Draw02LongitudinalRibs_cs
{
    static Numeric_Types_Draw02_Draw02LongitudinalRibs_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02LongitudinalRibs_cs");
}
