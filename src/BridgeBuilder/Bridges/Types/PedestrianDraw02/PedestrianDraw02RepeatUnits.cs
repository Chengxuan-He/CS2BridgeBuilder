using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs
    // Reuse the existing demand-loaded holder; literals below are array indices.
    // The measured fixed-road rectangle retains its original density and phase.
    // Both mirrored regions share one full-mip unit; no structural alias is inferred.
    static partial void PedestrianDraw02(string samplerKey, ref string? unitKey)
    {
        if (samplerKey == "6b68a0d1dcd3be3978c4452f10b446aa9a71047b508956d5097175b8f88a07b2")
            unitKey = "32f9c9b0969c3cfc217e6155a7a39c8bc40cba840c70c495f8f811a68dbd91c3";
    }

    // Structural sampling keeps source template 0. Road cutout template 14 shares
    // these pixels but is not a substitute for the original structural descriptor.
    static partial void PedestrianDraw02Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219")
            return;
        var values = Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32;
        regions.Add(new Region("a0322215b6a9765e1697060da0b9a465ac9207840c4fdcec5fb54fb48bd6cc89",
            values[6], values[7], values[8], values[9]));
    }
}
