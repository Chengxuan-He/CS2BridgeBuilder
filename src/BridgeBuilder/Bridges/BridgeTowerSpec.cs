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
internal static class BridgeTowerSpec
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
    /// <summary>The rest of the enum, recorded so the value above can be checked rather than trusted.</summary>
    internal static int PillarTypeNone => -Numeric_BridgeTowerSpec_cs.Values.I32[1];
    internal static int PillarTypeVertical => Numeric_BridgeTowerSpec_cs.Values.I32[2];
    internal static int PillarTypeHorizontal => Numeric_BridgeTowerSpec_cs.Values.I32[3];
    internal static int PillarTypeBase => Numeric_BridgeTowerSpec_cs.Values.I32[4];
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
    /// <summary>Shown in the editor's object list, below anything the player builds with.</summary>
    internal static int UiPriority => Numeric_BridgeTowerSpec_cs.Values.I32[6];

    /// <summary>Meshes sit at the object's own origin; a tower's parts are modelled in place.</summary>
    internal const bool Circular = false;
    /// <summary>The placeholder holds the shaft alone - the part that shows before the swap.</summary>
    internal static int PlaceholderParts => Numeric_BridgeTowerSpec_cs.Values.I32[7];
    /// <summary>The replacement holds the whole tower: base, shaft and top.</summary>
    internal static int ReplacementParts => Numeric_BridgeTowerSpec_cs.Values.I32[8];
    /// <summary>
    /// <c>StackDirection.Up</c> - the axis a tower's parts are stacked along.
    ///
    /// The whole enum, so the value can be checked rather than trusted:
    ///
    ///     None = 0   Right = 1   Up = 2   Forward = 3
    /// </summary>
    internal static int StackDirectionUp => Numeric_BridgeTowerSpec_cs.Values.I32[9];
    internal static int StackDirectionNone => Numeric_BridgeTowerSpec_cs.Values.I32[10];
    internal static int StackDirectionRight => Numeric_BridgeTowerSpec_cs.Values.I32[11];
    internal static int StackDirectionForward => Numeric_BridgeTowerSpec_cs.Values.I32[12];
    /// <summary>
    /// <c>StackOrder</c>: which end of the stack a part is, or that it is the repeatable middle.
    ///
    ///     First = 0   Middle = 1   Last = 2
    /// </summary>
    internal static int StackOrderFirst => Numeric_BridgeTowerSpec_cs.Values.I32[13];
    internal static int StackOrderMiddle => Numeric_BridgeTowerSpec_cs.Values.I32[14];
    internal static int StackOrderLast => Numeric_BridgeTowerSpec_cs.Values.I32[15];
    /// <summary>
    /// How far consecutive parts are sunk into one another. The archetype butts them together.
    /// </summary>
    internal static float StackStartOverlap => Numeric_BridgeTowerSpec_cs.Values.F32[3];
    internal static float StackEndOverlap => Numeric_BridgeTowerSpec_cs.Values.F32[4];

    /// <summary>The repeatable part is repeated, never stretched, so a tall tower is not a smeared one.</summary>
    internal const bool StackForbidScaling = false;
    /// <summary>
    /// Which part of a stack of <paramref name = "count"/> the part at <paramref name = "index"/> is.
    ///
    /// First at the bottom, last at the top, everything between repeatable - which is the pattern the
    /// archetype's three parts hold, and the only one the enum can express. A single part is not a
    /// stack at all: it would have to be first and last at once, and the archetype's placeholder,
    /// which has exactly one, carries no stacking either. <see cref = "Stacks"/> is that case.
    /// </summary>
    internal static int StackOrderOf(int index, int count) => index <= Numeric_BridgeTowerSpec_cs.Values.I32[16] ? StackOrderFirst : index >= count - Numeric_BridgeTowerSpec_cs.Values.I32[17] ? StackOrderLast : StackOrderMiddle;
    /// <summary>Whether a prefab with this many parts is stacked. One part is nothing to stack.</summary>
    internal static bool Stacks(int count) => count > Numeric_BridgeTowerSpec_cs.Values.I32[18];
    /// <summary>
    /// The mesh the game draws where a tower meets the ground, named rather than referenced.
    ///
    /// Base game content, present whenever the game is, which is what makes naming it safe where
    /// naming the archetype's own meshes would not be. Resolved by name at generation time; if it is
    /// somehow absent the base is left off, because a tower without a ground decal is a cosmetic
    /// fault and a tower that failed to generate is not.
    /// </summary>
    internal const string BaseMeshName = "Default_Base Mesh";
    /// <summary>The base is sized from the part's minimum bounds, not its drawn extent.</summary>
    internal const bool BaseUseMinBounds = true;
    /// <summary>
    /// Every part of a tower carries a UIObject, at the same priority as the tower.
    ///
    /// On the parts, not on the prefab - which is where an earlier reading of the archetype put it,
    /// because a dump that did not distinguish a mesh from the object owning it made three components
    /// on three meshes look like one component on their owner.
    /// </summary>
    internal static int MeshUiPriority => Numeric_BridgeTowerSpec_cs.Values.I32[19];
}

// Embedded numerical arrays: BridgeTowerSpec_cs
internal static class Numeric_BridgeTowerSpec_cs
{
    static Numeric_BridgeTowerSpec_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("BridgeTowerSpec_cs");
}
