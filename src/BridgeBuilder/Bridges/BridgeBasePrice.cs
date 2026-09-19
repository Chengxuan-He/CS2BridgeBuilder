using System;

namespace BridgeBuilder.Bridges;

internal static class BridgeBasePrice
{
    // All inputs use the same native currency / 8 m unit. Offsets are signed;
    // only the final total is constrained by the game's non-negative int cost.
    internal static bool TryCalculate(long prototypeBridge, long prototypeRoads,
        long upper, long lower, out long offset, out long total)
    {
        offset = total = 0;
        if (prototypeBridge < 0 || prototypeRoads < 0 || upper < 0 || lower < 0) return false;
        try
        {
            offset = checked(prototypeBridge - 3L * prototypeRoads);
            total = checked(offset + 3L * checked(upper + lower));
            return total >= 0 && total <= int.MaxValue;
        }
        catch (OverflowException) { return false; }
    }
}
