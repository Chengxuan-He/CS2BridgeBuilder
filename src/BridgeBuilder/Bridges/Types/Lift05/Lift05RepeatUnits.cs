namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{
    // The four fixed-road regions share one measured source strip. Their original
    // UV phase/orientation remain on the receivers; road template 14 preserves
    // double-sided alpha-test and Snow=0 semantics without rebaking the texels.
    static partial void Lift05(string samplerKey, ref string? unitKey)
    {
        if (samplerKey == "3b59afd3aa0ad2d963d6c6c7ba95cb1d0e33ccd1eec3f4b19e1a324a77551222")
            unitKey = "e56ec4c072e5cd227a3d38319d3733d6337a0e25c4825b4d476e233dbf65cc41";
    }
}
