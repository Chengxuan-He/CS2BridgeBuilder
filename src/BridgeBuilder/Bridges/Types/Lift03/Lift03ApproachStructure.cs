using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Authored left/right halves meet at native center joints. Only their transverse
// connections change length; side arcs, holes, braces and longitudinal members
// translate without changing their section. All source faces and channels remain.
internal static class Lift03ApproachStructure
{
    private static HardcodedNumbers Numbers => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift03ApproachStructure.Numbers", () => HardcodedNumbers.Load("Types_Lift03_ApproachCenterJoints"));
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var data = Numbers.I32;
        var at = data[Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[6] + lod];
        var count = data[at + Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[10]];
        at += Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[11];
        var positions = (Vector3[])source.Clone();
        for (var range = Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[14]; range < count; range++, at += Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[15])
        {
            int first = data[at], last = data[at + Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[16]], side = data[at + Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[17]];
            AffineGeometry.Execute(RangePrograms.Data, 0, new[] { positions }, Array.Empty<float[]>(), new float[] { side, delta }, first, Math.Max(0, checked((checked((last) + 1)) - (first))));
        }

        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[indices.Length / Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values.I32[21]] });
    }

    private static class RangePrograms
    {
        static RangePrograms()
        {
        }

        internal static HardcodedNumbers Data => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Lift03ApproachStructure.RangePrograms.Data", () => HardcodedNumbers.Load("Types_Lift03_Lift03ApproachStructure_RangePrograms"));
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03ApproachStructure_cs
internal static class Numeric_Types_Lift03_Lift03ApproachStructure_cs
{
    static Numeric_Types_Lift03_Lift03ApproachStructure_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift03_Lift03ApproachStructure_cs.Values", () => HardcodedNumbers.Load("Types_Lift03_Lift03ApproachStructure_cs"));
}
