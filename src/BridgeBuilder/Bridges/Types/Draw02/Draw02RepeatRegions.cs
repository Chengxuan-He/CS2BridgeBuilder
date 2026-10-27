using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
// Measured real road units reused by matching original structural domains.
internal static partial class MovableRepeatUnits
{
    private static class Draw02RegionsData
    {
        internal static readonly double[] Bounds = HardcodedNumbers.Load("Draw02_StructureRepeatUnits").F64;
    }

    static partial void Draw02Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597")
            return;
        var b = Draw02RegionsData.Bounds;
        regions.Add(new Region("ec7aba3a4552ce9cd0ac0f63fb9dd6715bb3ecec97fc038c6c9de625e528b2cd", b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[0]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[1]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[2]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[3]]));
        regions.Add(new Region("1f7e74d5483eab77b53257e4a5c73bc86efa2e23efb3cae6474206fbd234dd86", b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[4]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[5]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[6]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[7]]));
        regions.Add(new Region("ac901cefe3531d3480478aa588fac413ce742728904414c0b28dd3bb82a2edce", b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[8]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[9]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[10]], b[Numeric_Types_Draw02_Draw02RepeatRegions_cs.Values.I32[11]]));
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02RepeatRegions_cs
internal static class Numeric_Types_Draw02_Draw02RepeatRegions_cs
{
    static Numeric_Types_Draw02_Draw02RepeatRegions_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02RepeatRegions_cs");
}
