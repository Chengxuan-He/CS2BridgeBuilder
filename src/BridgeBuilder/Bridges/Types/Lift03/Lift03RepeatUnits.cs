using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    static partial void Lift03(string samplerKey, ref string? unitKey)
    {
        // Road appearances bind explicit prepared units or finite source UVs.
        // Historical RoadRepeat aliases have no current caller. StructuralRepeat
        // keys belong to a different recipe and must not inherit those aliases.
    }

    static partial void Lift03Regions(string sourceDigest, List<Region> regions)
    {
        // Both historical atlas regions contain only replaced road-top faces.
        // RemoveDetailed removes all 640/554 overlapping LOD0/1 faces; none
        // belongs to any structural fragment family. Non-road source materials
        // therefore need no derivative registered for these regions.
    }
}
