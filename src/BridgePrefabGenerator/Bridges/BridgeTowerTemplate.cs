using System;
using Colossal.Mathematics;
using Game.Prefabs;
using UnityEngine;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// What a bridge tower prefab is, apart from its geometry - written down rather than copied.
///
/// Read out of the game's own suspension bridge and frozen here. A generated tower used to be
/// assembled by hand and then corrected each time a field turned out to differ from the reference: the
/// placeholder had three mesh parts where the reference has one, the sub object entry carried
/// <c>m_RequireElevated</c> set where the reference leaves it clear, a component was added to the list
/// directly and so never got the back reference the game sets. Each was the same fault - a parameter
/// decided here rather than taken from the archetype - and each cost a round to find.
///
/// Copying the archetype's components at run time would fix that, and cannot be relied on: the tower a
/// bridge is derived from may not be installed, and the generator still has to produce a prefab that
/// behaves like one. So the archetype is recorded instead. The values below are the reference, and
/// applying them is what makes a generated tower a tower.
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
///
/// The four other tower families were measured the same way and hold the same component set, so one
/// template covers them; where a family turns out to differ, the difference belongs here as data.
/// </summary>
internal static class BridgeTowerTemplate
{
    /// <summary>
    /// How a tower stands relative to the deck.
    ///
    /// Standalone rather than Vertical: the game does not stretch a standalone pillar to reach the
    /// ground, it compares the object's own height against the gap and places it. The range is the
    /// small adjustment it is allowed either way, not a stretching distance - which is why a tower
    /// cannot be made to reach further by widening this.
    /// </summary>
    internal const PillarType Pillar = (PillarType)BridgeTowerSpec.PillarTypeStandalone;

    internal static Bounds1 VerticalRange =>
        new(BridgeTowerSpec.VerticalRangeMin, BridgeTowerSpec.VerticalRangeMax);

    /// <summary>
    /// The half of a tower the net names, before the game swaps it for the real one.
    ///
    /// It carries the shaft alone - one mesh part, nothing below the deck - and the parts that reach
    /// the ground live on the replacement. Filling it out was tried, on the reasoning that a failed
    /// swap would otherwise leave a tower hanging; it is not what the archetype does, and the game
    /// reads this prefab's height when it decides where the tower goes, so a padded one is measured
    /// wrongly whether or not the swap works.
    /// </summary>
    internal static void ApplyToPlaceholder(ObjectGeometryPrefab tower)
    {
        tower.m_Circular = BridgeTowerSpec.Circular;

        var placeholder = tower.AddComponent<PlaceholderObject>();
        placeholder.m_RandomizeGroupIndex = false;
        placeholder.active = true;

        ApplyPillar(tower);
    }

    /// <summary>
    /// The half the game swaps in: the whole tower, base and shaft and top, and the materials.
    /// </summary>
    internal static void ApplyToReplacement(ObjectGeometryPrefab tower, ObjectPrefab standsFor)
    {
        tower.m_Circular = BridgeTowerSpec.Circular;

        var spawnable = tower.AddComponent<SpawnableObject>();
        spawnable.m_Placeholders = new[] { standsFor };
        spawnable.m_Probability = BridgeTowerSpec.SpawnProbability;
        spawnable.m_RandomizationGroup = null;
        spawnable.active = true;

        // No UIObject here. The archetype carries one on each of the tower's parts and none on
        // the tower, which a dump that did not distinguish a mesh from its owner read the other
        // way round. ApplyToParts puts them where they belong.
        ApplyPillar(tower);
    }

    /// <summary>
    /// A tower that is not reached through a placeholder - the golden bridge names its pylon directly -
    /// and so is both halves at once.
    /// </summary>
    internal static void ApplyToWhole(ObjectGeometryPrefab tower)
    {
        tower.m_Circular = BridgeTowerSpec.Circular;
        ApplyPillar(tower);
    }

    /// <summary>
    /// The stacking, applied to the tower's parts - which is what makes a tower reach the ground.
    ///
    /// A tower is modelled as a base, a repeatable shaft and a top, and the game only knows that
    /// because each part says so. <c>ObjectInitializeSystem.UpdateStackBounds</c> reads these and
    /// collapses each part's contribution to the object's own size down to the end it belongs to -
    /// the first part counts only below the origin, the last only above it, the middle not at all -
    /// and moves the rest into <c>StackData</c>. <c>SubObjectSystem</c> then gives the placed tower a
    /// <c>Game.Objects.Stack</c> whose range runs from <c>m_FirstBounds.min</c> minus the object's
    /// elevation up to <c>m_LastBounds.max</c>: the stack grows downward by exactly however far the
    /// tower has been raised, and the shaft is repeated to fill it.
    ///
    /// Leave these off and there is no <c>StackData</c>, so no <c>Stack</c>, so nothing grows: the
    /// tower is drawn at the height it was modelled at and hangs above the ground by the elevation.
    /// That is the whole of the floating tower - the geometry, the pillar type, the placement and the
    /// bounds were all correct, and the parts simply never said they were a stack. It was invisible
    /// on a bridge low enough that the shaft happened to reach.
    ///
    /// One part is not a stack. It would have to be the first and the last at once, which the enum
    /// cannot say, and the archetype's placeholder - which has exactly one part - carries no stacking
    /// either. So this does nothing to a single-part tower, and the placeholder needs no special case.
    /// </summary>
    internal static void ApplyToParts(ObjectGeometryPrefab tower, RenderPrefab? groundBase)
    {
        var parts = tower.m_Meshes ?? Array.Empty<ObjectMeshInfo>();
        if (!BridgeTowerSpec.Stacks(parts.Length)) return;

        for (var index = 0; index < parts.Length; index++)
        {
            if (parts[index]?.m_Mesh is not RenderPrefab mesh) continue;

            var stack = mesh.AddComponent<StackProperties>();
            stack.m_Direction = (StackDirection)BridgeTowerSpec.StackDirectionUp;
            stack.m_Order = (StackOrder)BridgeTowerSpec.StackOrderOf(index, parts.Length);
            stack.m_StartOverlap = BridgeTowerSpec.StackStartOverlap;
            stack.m_EndOverlap = BridgeTowerSpec.StackEndOverlap;
            stack.m_ForbidScaling = BridgeTowerSpec.StackForbidScaling;
            stack.active = true;

            // The ground decal, on the part that meets the ground. Named rather than taken from the
            // archetype: it is base game content and so is there whenever the game is, which is what
            // the archetype's own meshes are not.
            if (index == 0 && groundBase != null)
            {
                var ground = mesh.AddComponent<BaseProperties>();
                ground.m_BaseType = groundBase;
                ground.m_UseMinBounds = BridgeTowerSpec.BaseUseMinBounds;
                ground.active = true;
            }

            var ui = mesh.AddComponent<UIObject>();
            ui.m_Group = null;
            ui.m_Priority = BridgeTowerSpec.MeshUiPriority;
            ui.m_IsDebugObject = false;
            ui.active = true;
        }
    }

    private static void ApplyPillar(ObjectGeometryPrefab tower)
    {
        var pillar = tower.AddComponent<PillarObject>();
        pillar.m_Type = Pillar;
        pillar.m_AnchorOffset = BridgeTowerSpec.AnchorOffset;
        pillar.m_VerticalPillarOffsetRange = VerticalRange;
        pillar.active = true;
    }
}
