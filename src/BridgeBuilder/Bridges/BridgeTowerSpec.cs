namespace BridgeBuilder.Bridges;
/// <summary>
/// What a bridge tower prefab is, apart from its geometry - as numbers, read out of the game once.
///
/// Separate from <see cref = "BridgeTowerTemplate"/>, which applies these to a prefab and therefore needs
/// the game. Nothing here does, so the archetype can be held to what was measured by a test that runs
/// without one - which matters, because these values are the only description of a tower the generator
/// has when the tower it imitates is not installed.
///
/// Taken from <c>Suspension Bridge - Highway Oneway - 5 Lanes</c> and its tower:
///
///     [subobject] 5LaneSuspensionBridgePillar Placeholder at (0, 77.9, 0)
///         placement=EdgeMiddle fixedIndex=0 spacing=0 anchorTop=False anchorCenter=False
///         requireElevated=False
///
///     5LaneSuspensionBridgePillar Placeholder (StaticObjectPrefab) m_Circular=False
///       1 mesh part at (0,0,0), requires None
///       PlaceholderObject  m_RandomizeGroupIndex=False
///       PillarObject       m_Type=Standalone m_AnchorOffset=0 m_VerticalPillarOffsetRange=(-1,1)
///
///     5LaneSuspensionBridgePillar (StaticObjectPrefab) m_Circular=False
///       SpawnableObject    m_Placeholders=[the placeholder] m_Probability=100 m_RandomizationGroup=null
///       PillarObject       m_Type=Standalone m_AnchorOffset=0 m_VerticalPillarOffsetRange=(-1,1)
///       3 mesh parts at (0,0,0), stacked - and the stacking is what reaches the ground:
///         Base Mesh  StackProperties m_Direction=Up m_Order=First  m_StartOverlap=0 m_EndOverlap=0
///                    BaseProperties  m_BaseType=Default_Base Mesh m_UseMinBounds=True
///                    UIObject        m_Group=null m_Priority=1 m_IsDebugObject=False
///         Mesh       StackProperties m_Direction=Up m_Order=Middle m_StartOverlap=0 m_EndOverlap=0
///                    UIObject        m_Group=null m_Priority=1 m_IsDebugObject=False
///         Top Mesh   StackProperties m_Direction=Up m_Order=Last   m_StartOverlap=0 m_EndOverlap=0
///                    UIObject        m_Group=null m_Priority=1 m_IsDebugObject=False
/// </summary>
internal static partial class BridgeTowerSpec
{
    /// <summary>
    /// <c>PillarType.Standalone</c>, held as its number so this file needs nothing from the game.
    ///
    /// The enum does not start at zero, and assuming it did put every generated tower on the wrong
    /// placement branch:
    ///
    ///     None = -1   Vertical = 0   Horizontal = 1   Standalone = 2   Base = 3
    ///
    /// Three was read off the order the fields appear in metadata, which is not the same thing as
    /// their values. Three is Base - a pillar under the deck - and the game places one by a different
    /// branch of SubObjectSystem than it places a standalone tower by, so the tower came out at a
    /// height nothing in the prefab explained. Every field matched the archetype except this one, and
    /// this one was a number the archetype was never asked for.
    /// </summary>
    internal static int PillarTypeStandalone => Numeric_BridgeTowerSpec_cs.Values.I32[0];
    /// <summary>Where the deck meets the tower, measured from the tower's own origin.</summary>
    internal static float AnchorOffset => Numeric_BridgeTowerSpec_cs.Values.F32[0];
    /// <summary>A metre either way: the adjustment a pillar is allowed, not a distance it can stretch.</summary>
    internal static float VerticalRangeMin => -Numeric_BridgeTowerSpec_cs.Values.F32[1];
    internal static float VerticalRangeMax => Numeric_BridgeTowerSpec_cs.Values.F32[2];
    /// <summary>
    /// The replacement is the only object standing in for its placeholder, so it always wins.
    ///
    /// Zero - which is what a freshly created component takes - means it never wins, the placeholder is
    /// left standing, and since a placeholder carries only the shaft the bridge ends up on a tower with
    /// no base and nothing below the deck.
    /// </summary>
    internal static int SpawnProbability => Numeric_BridgeTowerSpec_cs.Values.I32[5];

    /// <summary>Meshes sit at the object's own origin; a tower's parts are modelled in place.</summary>
    internal const bool Circular = false;

    /// <summary>
    /// Which part of a stack of <paramref name = "count"/> the part at <paramref name = "index"/> is.
    ///
    /// First at the bottom, last at the top, everything between repeatable - which is the pattern the
    /// archetype's three parts hold, and the only one the enum can express. A single part is not a
    /// stack at all: it would have to be first and last at once, and the archetype's placeholder,
    /// which has exactly one, carries no stacking either. <see cref = "Stacks"/> is that case.
    /// </summary>

    /// <summary>Whether a prefab with this many parts is stacked. One part is nothing to stack.</summary>
    internal static bool Stacks(int count) => count > Numeric_BridgeTowerSpec_cs.Values.I32[18];
}

// Embedded numerical arrays: BridgeTowerSpec_cs
internal static class Numeric_BridgeTowerSpec_cs
{
    static Numeric_BridgeTowerSpec_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_BridgeTowerSpec_cs.Values", () => HardcodedNumbers.Load("BridgeTowerSpec_cs"));
}
