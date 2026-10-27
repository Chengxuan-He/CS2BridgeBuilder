namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // Original fixed-end road chart; eight transverse periods retain the exact
    // source density through the prepared Surface's UV0 scale of 1/8.
    static partial void Lift02(string samplerKey,ref string? unitKey)
    {
        if(samplerKey=="7dab0150034a33385f9ec002c6268714040ab451b3ee9ffa591979669ac8f8d5")
            unitKey="aaac1eb40578f14f0c1c3bc5ba9081abc64f2a699f589da5d7b29a85444727fb";
    }
}
