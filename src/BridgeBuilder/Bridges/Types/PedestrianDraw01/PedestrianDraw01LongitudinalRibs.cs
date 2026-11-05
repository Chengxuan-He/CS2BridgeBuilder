using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Exact original component ranges measured by PedestrianDraw01LongitudinalRibs.py.
// Internal centres retain equal intervals; native outer-edge offsets are separate.
internal static class PedestrianDraw01LongitudinalRibs
{
    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)
    {
        var data = Numeric_Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs.Values;
        var cursor = 0;
        for (var level = 0; level < lod; level++)
            cursor += 2 + data.I32[cursor + 1] * 3;
        var count = data.I32[cursor++];
        var parts = data.I32[cursor++];
        if (source.Length != count || positions.Length != count || !float.IsFinite(delta))
            throw new InvalidOperationException("PedestrianDraw01 longitudinal rib source layout changed.");
        for (var part = 0; part < parts; part++)
        {
            var first = data.I32[cursor++];
            var last = data.I32[cursor++];
            var coefficient = data.F32[data.I32[cursor++]];
            AffineGeometry.For(first, checked(last + 1), index =>
                positions[index].x = AffineGeometry.AddScaled(source[index].x, coefficient, delta));
        }
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs");
}
