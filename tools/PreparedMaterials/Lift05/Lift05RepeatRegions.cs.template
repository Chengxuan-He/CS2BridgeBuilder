using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
internal static partial class MovableRepeatUnits
{
    // The retained deck-soffit region has 128 active density-mapped faces per near/middle LOD.
    // Other atlas islands retain their own source mapping; neither region grows
    // to a part bounding box or substitutes road cutout semantics.
    static partial void Lift05Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "40c466ebabd798b8bca9674788deefa14c10490f9f011f5377c8e48535bb984e")
            return;
        regions.Add(new Region("d1b116c5e0d5370d5a20b11fd85e5de3b71558b9db2cefa47425b6ba80788d31", Numeric_Types_Lift05_Lift05RepeatRegions_cs.Values.F64[4], Numeric_Types_Lift05_Lift05RepeatRegions_cs.Values.F64[5], Numeric_Types_Lift05_Lift05RepeatRegions_cs.Values.F64[6], Numeric_Types_Lift05_Lift05RepeatRegions_cs.Values.F64[7]));
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05RepeatRegions_cs
internal static class Numeric_Types_Lift05_Lift05RepeatRegions_cs
{
    static Numeric_Types_Lift05_Lift05RepeatRegions_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05RepeatRegions_cs");
}
