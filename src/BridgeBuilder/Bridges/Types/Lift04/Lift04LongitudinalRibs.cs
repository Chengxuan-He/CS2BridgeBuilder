using UnityEngine;

namespace BridgeBuilder.Bridges;
// Complete offline-measured rib sections, including short segments and end caps.
internal static class Lift04LongitudinalRibs
{
    private static class Data
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_LongitudinalRibMeasurements");
    }

    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)
    {
        var data = Data.Values;
        int start = data.I32[lod * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[0]], count = data.I32[lod * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[1] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[2]];
        int coefficient = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[3];
        for (int level = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[4]; level < lod; level++)
            coefficient += data.I32[level * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[5] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[6]] * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[7];
        for (int n = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[8]; n < count; n++)
        {
            int i = data.I32[start + n];
            positions[i].x = AffineGeometry.AddScaled(source[i].x + data.F32[coefficient + n * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[9]], data.F32[coefficient + n * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[10] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[11]], delta);
        }
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04LongitudinalRibs_cs
internal static class Numeric_Types_Lift04_Lift04LongitudinalRibs_cs
{
    static Numeric_Types_Lift04_Lift04LongitudinalRibs_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04LongitudinalRibs_cs");
}
