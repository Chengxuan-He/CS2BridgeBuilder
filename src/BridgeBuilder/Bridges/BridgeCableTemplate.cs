using Game.Prefabs;

namespace BridgeBuilder.Bridges;

/// <summary>
/// What a cable piece is, apart from its geometry - written down rather than copied, for the same
/// reason <see cref="BridgeTowerTemplate"/> is: the bridge a generated one is derived from may not be
/// installed, and the result still has to behave like cables.
///
/// Taken from <c>Suspension Bridge - Highway Oneway - 5 Lanes</c> and the section it carries overhead:
///
///     [overhead] 5-Lane Suspension Bridge (NetSectionPrefab)
///         m_Median=True m_Invert=False m_Flip=False m_HalfLength=False m_Offset=(0,0,0)
///         m_RequireAll=[] m_RequireAny=[] m_RequireNone=[] m_HiddenLayers=0
///
///     5-Lane Suspension Bridge Piece (NetPiecePrefab)
///         m_Layer=Top m_Width=27 m_Length=256 m_HeightRange=(-0.3, 75.3)
///         m_WidthOffset=0 m_NodeOffset=0 m_SideConnectionOffset=0 m_SurfaceHeights=(0,0,0,0)
///         NetPieceTiling  m_DisableTextureTiling=True
///
/// Dumped side by side against a generated section, the two differ in the width, the bounds and this
/// one component - and the component is the whole of the cable fault. See
/// <see cref="BridgeCables.PieceDisablesTextureTiling"/> for what it does.
/// </summary>
internal static class BridgeCableTemplate
{
    /// <summary>
    /// The components a cable piece carries. The fields are the caller's; this is everything else.
    /// </summary>
    internal static void ApplyToPiece(NetPiecePrefab piece)
    {
        var tiling = piece.AddComponent<NetPieceTiling>();
        tiling.m_DisableTextureTiling = BridgeCables.PieceDisablesTextureTiling;
        tiling.active = true;
    }
}
