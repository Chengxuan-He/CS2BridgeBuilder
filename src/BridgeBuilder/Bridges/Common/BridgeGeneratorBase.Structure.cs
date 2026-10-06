

using Game.Prefabs;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal abstract partial class BridgeGeneratorBase
{
    protected virtual bool CopyOverhead(
        NetGeometryPrefab target, BridgeStyleVariant variant, string styleId, float extra)
    {
        // The caller has already folded the prototype's structural allowance into this number. The
        // same effective extra also goes to node-bound props, while TowerFactory independently derives
        // the identical number from target and prototype widths. Applying a bonus here again would
        // put cables one half-bonus outside the nodes and tower they are meant to meet.

        var source = variant.Overhead;
        if (source?.m_Sections == null || source.m_Sections.Length == 0) return true;

        var sections = new List<NetSectionInfo>();
        foreach (var section in source.m_Sections)
        {
            if (section?.m_Section == null) continue;
            var preserveGeometry = BridgeStyleDefinitions.PreservesOverheadGeometry(
                styleId, section.m_Section.name);
            var derived = Widened(section, target.name, extra, preserveGeometry);
            // Do not replace a failed derivation with the donor or publish a partial cable frame.
            if (derived == null) return false;
            sections.Add(derived);
        }

        var overhead = target.AddOrGetComponent<OverheadNetSections>();
        overhead.m_Sections = sections.ToArray();
        overhead.active = true;
        return true;
    }

    /// <summary>
    /// One overhead section fitted to this deck: the cables.
    ///
    /// Paired cable assemblies change their mesh width, not merely their section offset.
    /// The recorded single-column archetype instead owns one central cable sheet: its
    /// geometry and composition width stay unchanged, in a separately owned copy.
    /// </summary>
    private NetSectionInfo? Widened(
        NetSectionInfo source, string bridgeName, float extra, bool preserveGeometry)
    {
        if (preserveGeometry) extra = 0f;
        var spread = Spread(source, extra);
        // A zero width delta is still not a no-op for a bridge whose inner railing follows the selected
        // road's sidewalks. A road can match the prototype width while having different, asymmetric or
        // absent sidewalks, so derive its owned section and apply the per-side railing plan.
        if (_towers == null
            || (!preserveGeometry && Math.Abs(extra) < 0.001f
                && !BridgeTowers.BringsItsOwnRailings(_towers.StyleId)))
            return spread;

        var widened = _towers.WidenSection(source.m_Section, bridgeName, extra, preserveGeometry);
        if (widened == null) return null;
        spread.m_Section = widened;
        return spread;
    }

    /// <summary>
    /// Adds the donor's deck props - pylons, portals - on top of whatever the road already has.
    ///
    /// Entries are dropped rather than translated when their placement depends on fixed segments the
    /// copied bridge does not have. A placement like EdgeMiddleFixedSegment indexes the bridge's fixed
    /// segment array through m_FixedIndex, and an index into an array that is shorter than the donor's
    /// is read by a compiled job with no bounds check to save us.
    /// </summary>
    protected virtual void CopySubObjects(NetGeometryPrefab target, BridgeStyleVariant variant, float extra)
    {
        var source = variant.SubObjects;

        // A style that brings no props of its own leaves the road's default pillars in place. They
        // are the wrong pillars for the style, but a bridge with no supports at all would be worse.
        if (source?.m_SubObjects == null || source.m_SubObjects.Length == 0) return;

        var fixedSegments = target.GetComponent<Bridge>()?.m_FixedSegments?.Length ?? 0;
        var usable = new List<NetSubObjectInfo>();
        var dropped = 0;
        var markers = 0;
        foreach (var info in source.m_SubObjects)
        {
            if (info?.m_Object == null) continue;

            // Markers belong to the road, not to the bridge style. The donor carries an outside
            // connection of its own, and copying it leaves the generated bridge with two - its road's
            // and the donor's - where the archetype has one.
            if (info.m_Object is MarkerObjectPrefab)
            {
                markers++;
                continue;
            }

            if (NeedsFixedSegment(info.m_Placement) && info.m_FixedIndex >= fixedSegments)
            {
                dropped++;
                continue;
            }

            usable.Add(Spread(info, extra));
        }

        if (dropped > 0)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': {1} of the style's {2} deck props were left out because they are anchored to "
                + "fixed spans this bridge does not have.",
                target.name, dropped, source.m_SubObjects.Length));
        }

        if (markers > 0)
        {
            // Not a shortfall, so not a warning. The road brought its own, and two would be one too
            // many - which is what the generated bridge had until these were left behind.
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: left the donor's {1} marker(s) behind; the road has its own.",
                target.name, markers));
        }

        var subObjects = target.AddOrGetComponent<NetSubObjects>();
        var existing = subObjects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();

        // The road's own elevated props are its default pillars - the plain columns any road grows
        // when it is raised. They are replaced, not joined: appending left the bridge standing on its
        // style's pylons and on a row of ordinary concrete pillars at the same time. Props that are
        // not gated on being elevated belong to the road at ground level and are left alone.
        var kept = existing.Where(info => info == null || !info.m_RequireElevated).ToArray();
        var replaced = existing.Length - kept.Length;
        if (replaced > 0)
        {
            _report.Note(
                $"{target.name}: replaced {replaced} default elevated prop(s) with the style's own.");
        }

        if (usable.Count == 0)
        {
            _report.Warning(
                $"'{target.name}': none of the style's {source.m_SubObjects.Length} deck prop(s) could be "
                + "used, so this bridge has no towers or pylons of its own.");
        }

        if (usable.Count == 0 && replaced == 0) return;

        subObjects.m_SubObjects = kept.Concat(usable).ToArray();
        subObjects.active = true;
    }

    /// <summary>Whether a placement reads the bridge's fixed segment array.</summary>
    private static bool NeedsFixedSegment(NetObjectPlacement placement) => placement
        is NetObjectPlacement.NodeBeforeFixedSegment
        or NetObjectPlacement.NodeBetweenFixedSegment
        or NetObjectPlacement.NodeAfterFixedSegment
        or NetObjectPlacement.EdgeMiddleFixedSegment
        or NetObjectPlacement.EdgeEndsFixedSegment
        or NetObjectPlacement.EdgeStartFixedSegment
        or NetObjectPlacement.EdgeEndFixedSegment
        or NetObjectPlacement.EdgeEndsOrNodeFixedSegment
        or NetObjectPlacement.EdgeStartOrNodeFixedSegment
        or NetObjectPlacement.EdgeEndOrNodeFixedSegment;


    private static NetSectionInfo Spread(NetSectionInfo source, float extra) => new()
    {
        m_Section = source.m_Section,
        m_RequireAll = Copy(source.m_RequireAll),
        m_RequireAny = Copy(source.m_RequireAny),
        m_RequireNone = Copy(source.m_RequireNone),
        m_HiddenLayers = source.m_HiddenLayers,
        m_Invert = source.m_Invert,
        m_Flip = source.m_Flip,
        // Marked as a median section, as the archetype marks its cables. Read from the donor because a
        // section that is not the cables - a pack with something else overhead - should keep its own.
        m_Median = source.m_Median,
        m_HalfLength = source.m_HalfLength,

        // The cables sit on the centre line and stay there. The archetype records this offset as zero
        // at every width the game ships, so what puts a wider bridge's cables further out is the
        // section being wider, not the section being moved - and moving a zero moves nothing, which is
        // why every attempt to fix the cables by shifting this did exactly nothing.
        //
        // Still passed through Spread rather than written as zero: a donor whose overhead section is
        // genuinely offset is not the archetype's arrangement, and it keeps its own.
        m_Offset = new float3(
            TowerWidening.Spread(source.m_Offset.x, extra), source.m_Offset.y, source.m_Offset.z),
    };

    private static NetSubObjectInfo Spread(NetSubObjectInfo source, float extra) => new()
    {
        m_Object = source.m_Object,
        m_Position = new float3(
            TowerWidening.Spread(source.m_Position.x, extra), source.m_Position.y, source.m_Position.z),
        m_Rotation = source.m_Rotation,
        m_Placement = source.m_Placement,
        m_FixedIndex = source.m_FixedIndex,
        m_Spacing = source.m_Spacing,
        m_AnchorTop = source.m_AnchorTop,
        m_AnchorCenter = source.m_AnchorCenter,

        // Copied, not forced. This was set to true on the reasoning that a pylon standing on a ground
        // level road would be the most obvious way for a converted bridge to look wrong - but the
        // bridge being imitated does not do that. Read out of the game, the five-lane suspension bridge
        // anchors its tower with:
        //
        //     [subobject] 5LaneSuspensionBridgePillar Placeholder at (0, 77.9, 0)
        //         placement=EdgeMiddle spacing=0 anchorTop=False anchorCenter=False requireElevated=False
        //
        // False, because a bridge prefab is only ever built elevated and the flag has nothing to add.
        // Setting it changes which props the game considers this to be, and the road's own props are
        // sorted on the same flag a few lines down - so forcing it here quietly reclassified every prop
        // the style brought.
        m_RequireElevated = source.m_RequireElevated,
        m_RequireOutsideConnection = source.m_RequireOutsideConnection,
        m_RequireDeadEnd = source.m_RequireDeadEnd,
        m_RequireOrphan = source.m_RequireOrphan,
    };

    private static NetPieceRequirements[]? Copy(NetPieceRequirements[]? source) => source?.ToArray();

}
