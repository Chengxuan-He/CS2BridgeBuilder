using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

// Two measured fixed-road units; road and structure descriptors share pixel payloads.
// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs (shared existing resource)
internal static partial class MovableRepeatUnits
{
    static partial void PedestrianDraw01(string samplerKey, ref string? unitKey) { }

    static partial void PedestrianDraw01Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "9339aa46ba254430ed01aa6c7f858bfc612dabb3bea70bdc01b31bfb7922ec3c") return;
        var v = Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32;
        regions.Add(new Region("5fcf2f7131ca9fe3a757f2773c43ff7b94a501d652a4f67d5a151d1884f79ff3",
            v[3], v[27], v[4], v[5]));
        regions.Add(new Region("49a56708ed0dc7e82ccb0cc21b36288e503b912ebe590593658103bf096654d1",
            v[3], v[25], v[4], v[5]));
    }
}
