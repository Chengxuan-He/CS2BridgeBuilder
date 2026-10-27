using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
internal static partial class MovableRepeatUnits
{
    static partial void Draw03Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "fb159828e8d73c22dc96cdf7e82c74b36a53483d0126124a6eacbd0fda4a7e46")
            return;
        var measured = Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.F32;
        // The original structural descriptor is template 1. Its Grating unit
        // shares the road variant's complete texture payloads, not its cutout template.
        // Restrict coverage to the real Grating island; no other atlas part is inferred.
        regions.Add(new Region("b2b2715a4fc310e146c16d3b2b2be0d687601e64a3d176cf8254024c1a2ade47", measured[Numeric_Types_Draw03_Draw03RepeatRegions_cs.Values.I32[0]], measured[Numeric_Types_Draw03_Draw03RepeatRegions_cs.Values.I32[1]], measured[Numeric_Types_Draw03_Draw03RepeatRegions_cs.Values.I32[2]], measured[Numeric_Types_Draw03_Draw03RepeatRegions_cs.Values.I32[3]]));
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RepeatRegions_cs
internal static class Numeric_Types_Draw03_Draw03RepeatRegions_cs
{
    static Numeric_Types_Draw03_Draw03RepeatRegions_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RepeatRegions_cs");
}
