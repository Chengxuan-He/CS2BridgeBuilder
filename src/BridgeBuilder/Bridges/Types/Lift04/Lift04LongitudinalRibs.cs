using UnityEngine;

namespace BridgeBuilder.Bridges;
// Complete offline-measured rib sections, including short segments and end caps.
internal static class Lift04LongitudinalRibs
{
    private static class Data
    {
        internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift04LongitudinalRibs.Data.Values", () => HardcodedNumbers.Load("Types_Lift04_LongitudinalRibMeasurements"));
    }

    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)
    {
        var data = Data.Values;
        int start = data.I32[lod * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[0]], count = data.I32[lod * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[1] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[2]];
        int coefficient = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[3];
        for (int level = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[4]; level < lod; level++)
            coefficient += data.I32[level * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[5] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[6]] * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[7];
        var origins = new float[count];
        var weights = new float[count];
        for (int n = Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[8]; n < count; n++)
        {
            origins[n] = data.F32[coefficient + n * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[9]];
            weights[n] = data.F32[coefficient + n * Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[10] + Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values.I32[11]];
        }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.RowOriginDelta,
            new[] { source, positions }, new[] { origins, weights }, new[] { delta },
            count: count, indices: new[] { StructureRows.Slice(data.I32, start, count) });
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04LongitudinalRibs_cs
internal static class Numeric_Types_Lift04_Lift04LongitudinalRibs_cs
{
    static Numeric_Types_Lift04_Lift04LongitudinalRibs_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift04_Lift04LongitudinalRibs_cs.Values", () => HardcodedNumbers.Load("Types_Lift04_Lift04LongitudinalRibs_cs"));
}
