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
        int count = data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[2]];
        var vertices = StructureRows.Slice(data.I32, data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[3]], count);
        var weights = StructureRows.Slice(data.F32, data.I32[row + Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values.I32[4]], count);
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.RowDelta, new[] { source, positions }, new[] { weights }, new[] { delta }, count: count, indices: new[] { vertices });
    }
}

internal static class Draw02LongitudinalRibMeasurements
{
    static Draw02LongitudinalRibMeasurements()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw02LongitudinalRibMeasurements.Values", () => HardcodedNumbers.Load("Types_Draw02_LongitudinalRibMeasurements"));
}

// Embedded numerical arrays: Types_Draw02_Draw02LongitudinalRibs_cs
internal static class Numeric_Types_Draw02_Draw02LongitudinalRibs_cs
{
    static Numeric_Types_Draw02_Draw02LongitudinalRibs_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02LongitudinalRibs_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02LongitudinalRibs_cs"));
}
