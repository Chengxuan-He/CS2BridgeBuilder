using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Embedded numerical arrays: Types_Lift01_Lift01RoadAppearance_cs
    // Reuse its existing demand-loaded holder. The literals below are array
    // indices, not a second copy of the measured floor bounds.
    // One measured 2 m by 4 m floor unit serves all eight original direction/phase
    // uses. The source UV recipe retains those transforms; the texture is shared.
    static partial void Lift01(string samplerKey, ref string? unitKey)
    {
        if (samplerKey == "f7fcada6949aa34822e7279596faf7e86b7d927afb4b60c5203810d673dcc0c3")
            unitKey = "e1b5c319c5fe1af13e49a8fc18bbec63ca7540402deb7d9c7c262620ba7bcd0a";
    }

    // Structural receivers retain the source single-sided template. Only the
    // measured floor rectangle is registered, never an arbitrary triangle/chart.
    static partial void Lift01Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "da154acf2452319660b3729ff4ebc7f181fbf6524fdf0c3b8411b47d4990bbf4")
            return;
        var values = Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32;
        regions.Add(new Region("c0fba02d50a074bb7d466ed3329236a74a67777913bedb37ce685f63bd582dbd",
            values[0], values[1], values[2], values[3]));
    }
}
