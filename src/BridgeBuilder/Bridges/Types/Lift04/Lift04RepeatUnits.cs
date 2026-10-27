namespace BridgeBuilder.Bridges;

// Measured floor rectangle and beam repeat strip with separate coverage bounds. Native descriptors preserve road
// double-sided cutout versus structural Default semantics; texture CIDs are shared.
internal static partial class MovableRepeatUnits
{
    // Embedded numerical arrays: Types_Lift04_Lift04RoadAppearance_cs
    // Reuse the existing demand-loaded holder; the literals below are array indices.
    static partial void Lift04(string samplerKey, ref string? unitKey)
    {
        unitKey = samplerKey switch
        {
            "28dc86a8c640b80ac3c0ad59d935ab79c4fdf2ba1919b36f1bd6df07da896505" => "905e8eb0e232862c6b74b7243082981adabb2a9fc3119825516d319c9705d820",
            "19e8a1b18bf2b7dd55f708ce62d6362207568cecf5055f7ad4771dd1856b28fc" => "92669b0bf58299589e4be13863ef777c4d818f28436c9abf4f70a2489da35eb9",
            _ => unitKey
        };
    }

    static partial void Lift04Regions(string sourceDigest, System.Collections.Generic.List<Region> regions)
    {
        if (sourceDigest != "ab2b09795bfa036e68938504786a9b2f3e62ae9b000fc60cbcb6e0f3b1a45a39") return;
        var values = Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32;
        regions.Add(new Region("92669b0bf58299589e4be13863ef777c4d818f28436c9abf4f70a2489da35eb9",
            values[0], values[1], values[2], values[3]));
        var beam = Lift04BeamRepeatData.Values.F64;
        regions.Add(new Region("649d6588b132d3b40bd9e329c2071e6df523b547530119704f5c3699143144a7",
            beam[0], beam[1], beam[2], beam[3], beam[4], beam[5], beam[6], beam[7]));
        // Single-axis profiles: measured coverage remains independent of unit bounds.
        var profiles = Lift04StructuralProfileData.Values.F64;
        regions.Add(new Region("f443914a8812fa50dda65930e5046391736fc0c5a1bca024561c108f99fcd96f",
            profiles[0], profiles[1], profiles[2], profiles[3], profiles[4], profiles[5], profiles[6], profiles[7], repeatU: false, repeatV: true));
        regions.Add(new Region("f443914a8812fa50dda65930e5046391736fc0c5a1bca024561c108f99fcd96f",
            profiles[8], profiles[9], profiles[10], profiles[11], profiles[12], profiles[13], profiles[14], profiles[15], repeatU: false, repeatV: true));
        regions.Add(new Region("0cfb7f02cf0395308cd5f8034908a3263eb8415c39866bd76321f969df01e06e",
            profiles[16], profiles[17], profiles[18], profiles[19], profiles[20], profiles[21], profiles[22], profiles[23], repeatU: false, repeatV: true));
        // End single-axis profiles.
    }

    // Embedded numerical arrays: Types_Lift04_BeamRepeatRegions
    private static class Lift04BeamRepeatData
    {
        static Lift04BeamRepeatData() { }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_BeamRepeatRegions");
    }
    // Embedded numerical arrays: Types_Lift04_StructuralProfileRegions
    private static class Lift04StructuralProfileData
    {
        static Lift04StructuralProfileData() { }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_StructuralProfileRegions");
    }
}
