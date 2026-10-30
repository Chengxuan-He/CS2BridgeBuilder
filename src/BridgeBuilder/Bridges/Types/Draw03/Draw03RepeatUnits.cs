namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Current road domains bind completed materials directly. Structural region
    // aliases are owned by Draw03RepeatRegions; no legacy road aliases remain.
    static partial void Draw03(string samplerKey, ref string? unitKey)
    {
    }
}
