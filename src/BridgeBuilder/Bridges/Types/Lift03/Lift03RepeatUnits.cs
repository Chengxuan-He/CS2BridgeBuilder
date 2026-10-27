using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Embedded numerical arrays: Types_Lift03_Lift03RoadAppearance_cs
    // Reuse its demand-loaded holder; the numeric literals below are array indices.
    // Road templates retain the producer cutout/double-sided semantics.
    // Structural regions instead retain the original single-sided template 1.
    static partial void Lift03(string samplerKey, ref string? unitKey)
    {
        switch (samplerKey)
        {
            case "cdd91a7309593e637bf3246b614254e8e05fbf238762d4efa0cf5e06a74963e8":
                unitKey = "13f5d80eece0285680559a6785cdf60d5103b762f440ca9f91d2bf21b86b8e6e";
                break;
            case "e9f03cf15c697ebf84d8c905ac66090ad22c49f7f001d8c3c65bd5197b9ea15e":
                unitKey = "994279acd825b00ec92435884ef6067e635852718100bbbd6f152511634e9f7f";
                break;
            case "90a022d243814d9f5689e8e163780be66a0fa5a14bd8d58a28af3bf05f34872b":
                unitKey = "9227a336e07c0153fe784787e4d85ade399e21c243603715d7b1ec3af10ef12d";
                break;
        }
    }

    static partial void Lift03Regions(string sourceDigest, List<Region> regions)
    {
        if (sourceDigest != "b416f8ead14d3208611b9a86d4b57688ddd1bb87acd0cfce5b541f6340f202f0") return;
        var values = Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32;
        regions.Add(new Region("8d07fa2c5f44a58e4f1024b25b5ac673e44747b6d27fb5a6256c58fc19089813",
            values[0] / values[1], values[2] / values[3],
            values[4] / values[5], values[6] / values[7]));
        regions.Add(new Region("906eeb04251378f9e20b74cc9dc1a3d0f689e383892d58517a3853e6b803adff",
            values[10] / values[11], values[12] / values[13],
            values[14] / values[15], values[16] / values[17]));
    }
}
