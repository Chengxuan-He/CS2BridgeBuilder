using Colossal.Mathematics;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal abstract partial class BridgeGeneratorBase
{
    /// <summary>
    /// The bridge behaviour: span length, sag, how it meets water, whether it may curve.
    ///
    /// From the recorded archetype where there is one, and from the donor where there is not.
    ///
    /// The distinction matters when the donor is missing. A road can be converted with none of the
    /// style's content installed - the widths are recorded, so the tower can still be built - and a
    /// bridge that then takes its span length from a prefab that is not there gets whatever a default
    /// constructed component holds, which for m_SegmentLength is zero. Recorded values do not have that
    /// failure mode, which is the whole reason they are recorded.
    ///
    /// Only the suspension family has been measured. Falling back is reported rather than silent,
    /// because a bridge built from unmeasured numbers is as wrong as one built from invented numbers
    /// and the only difference is whether anyone knows which.
    /// </summary>
    protected virtual void CopyBridge(
        NetGeometryPrefab target, BridgeStyle style, BridgeStyleVariant variant, BridgeOptions options)
    {
        var source = variant.Bridge;
        var archetype = BridgeSpec.For(style.Id);
        var bridge = target.AddOrGetComponent<Bridge>();

        // The donor's own behaviour, whenever there is a donor.
        //
        // The recorded archetype exists for when there is not - rule 2's case, a style whose prefab is
        // not installed - and reaching for it while the prefab is in hand is the mistake rule 2's own
        // note warns about: what is present is carried, not recalled. One style's record is not one
        // style's variants. The suspension archetype records a 256 m span, measured on the single deck
        // bridge; the double deck bridge of the same style spans 320. Forcing 256 onto it put the
        // towers 256 m apart while the cables were drawn for 320, and the two disagreed at every node.
        var recorded = archetype;
        if (source != null)
        {
            bridge.m_SegmentLength = source.m_SegmentLength;
            bridge.m_Hanging = source.m_Hanging;
            bridge.m_ElevationOnWater = source.m_ElevationOnWater;
            bridge.m_CanCurve = source.m_CanCurve;
            bridge.m_AllowMinimalLength = source.m_AllowMinimalLength;

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: bridge behaviour carried from '{1}' - spans {2:0.#} m, sag {3:0.##}, {4:0.#} m "
                + "over water.",
                target.name, variant.Name, source.m_SegmentLength, source.m_Hanging,
                source.m_ElevationOnWater));

            // The record is still worth checking against, because a variant that disagrees with its
            // own style's archetype is either a different design or a mismeasurement, and either way
            // it is worth saying which bridge the numbers came from.
            if (recorded.HasValue
                && Math.Abs(recorded.Value.SegmentLength - source.m_SegmentLength) > 1f)
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: '{1}' spans {2:0.#} m where the recorded archetype '{3}' spans {4:0.#}. The "
                    + "donor's own is used, because it is the bridge this one is derived from.",
                    target.name, variant.Name, source.m_SegmentLength,
                    recorded.Value.MeasuredFrom, recorded.Value.SegmentLength));
            }
        }
        else if (recorded.HasValue)
        {
            bridge.m_SegmentLength = recorded.Value.SegmentLength;
            bridge.m_Hanging = recorded.Value.Hanging;
            bridge.m_ElevationOnWater = recorded.Value.ElevationOnWater;
            bridge.m_CanCurve = recorded.Value.CanCurve;
            bridge.m_AllowMinimalLength = recorded.Value.AllowMinimalLength;

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: bridge behaviour from the recorded archetype - spans {1:0.#} m, measured from "
                + "'{2}'. The donor carries none of its own.",
                target.name, recorded.Value.SegmentLength, recorded.Value.MeasuredFrom));
        }
        else
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': neither '{1}' nor any recorded archetype says how this style behaves as a "
                + "bridge, so it keeps whatever the road had.",
                target.name, variant.Name));
        }

        // Still from the donor: these say which content this bridge is made of rather than how it
        // behaves, and there is nothing to record them as. Guarded, because the branch above admits a
        // donor that carries no Bridge of its own.
        if (source != null)
        {
            bridge.m_WaterFlow = source.m_WaterFlow;
            bridge.m_FixedSegments = CopyFixedSegments(source);

            // Double-deck node seams are part of the prototype's two-network arrangement, but a
            // prototype road state must never be installed on a track selected as the main/lower
            // network: its Elevated node rule renders as a railway switch. Preserve the selected
            // network's own state tables when the transport roles differ. This fixes the seam without
            // changing bridge-variant selection or any measured width.
            if (options.DoubleDeck)
            {
                var copied = DoubleDeckComposer.CopyCompatibleSeamBehavior(
                    target, variant.Donor, copyAggregate: false);
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: double-deck main-network seam states {1} for '{2}' - {3} edge rule(s), "
                    + "{4} node rule(s).",
                    target.name,
                    copied ? "copied from the transport-compatible prototype" : "preserved from the selected deck",
                    variant.Name,
                    target.m_EdgeStates?.Length ?? 0,
                    target.m_NodeStates?.Length ?? 0));
            }
        }

        bridge.m_BuildStyle = options.BuildStyle ?? source?.m_BuildStyle ?? bridge.m_BuildStyle;
        bridge.active = true;
        CopyAggregate(target, variant);
    }

    /// <summary>
    /// Takes the donor's aggregate type, so a placed bridge is named as a bridge.
    ///
    /// The aggregate is what joins consecutive segments into one named road, and it carries the pool
    /// the game draws that name from. A road cloned from a street keeps the street's aggregate, so
    /// the finished bridge was christened "...街" - correct for what it was cloned from, wrong for
    /// what it became. The donor is a real bridge, so its aggregate names bridges.
    ///
    /// A donor without an aggregate leaves the deck's own alone: an aggregate of null would stop the
    /// segments joining up at all, which is a worse result than an unfitting name.
    /// </summary>
    private void CopyAggregate(NetGeometryPrefab target, BridgeStyleVariant variant)
    {
        var aggregate = variant.Donor.m_AggregateType;
        if (aggregate == null)
        {
            _report.Note(
                $"{target.name}: the style has no aggregate type, so the deck keeps its own and the "
                + "placed bridge will be named after the road it came from.");
            return;
        }

        if (ReferenceEquals(target.m_AggregateType, aggregate)) return;

        var previous = target.m_AggregateType?.name ?? "none";
        target.m_AggregateType = aggregate;
        _report.Note($"{target.name}: named as '{aggregate.name}' rather than '{previous}'.");
    }

    /// <summary>
    /// Copies the style's fixed spans, whole.
    ///
    /// An earlier version kept only the spans whose set states the deck's own sections respond to,
    /// reasoning that a span nothing draws is a span laid out around nothing. On a Road Builder road
    /// that condition is met by none of them, so the array came back empty - and the towers went with
    /// it, because a suspension bridge anchors its pylons to the ends of its main span through
    /// <see cref="NetSubObjectInfo.m_FixedIndex"/>. Every prop then indexed past the end of an empty
    /// array, every prop was dropped, and the bridge came out bare.
    ///
    /// So the array is copied intact. That keeps every index the props use valid, which is what makes
    /// them safe - the danger was never the spans themselves but an index reaching past them, and a
    /// compiled job reads that index without a bounds check. The deck still builds as ordinary spans
    /// where the style would have drawn a main one, because the sections that draw it stay with the
    /// donor; the structure at least stands in the right places.
    /// </summary>
    private FixedNetSegmentInfo[] CopyFixedSegments(Bridge source)
    {
        var segments = source.m_FixedSegments;

        // Empty, not null. The archetype carries an empty array and a generated bridge carried null,
        // which is a different thing to anything that reads it without checking first - and that kind
        // of difference surfaces as a crash somewhere else rather than as a bridge that looks wrong.
        if (segments == null || segments.Length == 0) return Array.Empty<FixedNetSegmentInfo>();

        return segments.Where(segment => segment != null).Select(Copy).ToArray();
    }

    private static FixedNetSegmentInfo Copy(FixedNetSegmentInfo segment) => new()
    {
        m_CountRange = segment.m_CountRange,
        m_Length = segment.m_Length,
        m_CanCurve = segment.m_CanCurve,
        m_SetState = Copy(segment.m_SetState),
        m_UnsetState = Copy(segment.m_UnsetState),
    };

    /// <summary>
    /// Carries over the opening mechanism of a draw or lift bridge. Without this, converting one of
    /// those would produce a bridge that looks like it should open and never does - the lift geometry
    /// is in the donor's sections either way, so the component is what makes it move rather than what
    /// makes it visible.
    /// </summary>
    private static void CopyMoveable(NetGeometryPrefab target, BridgeStyleVariant variant)
    {
        var source = variant.Donor.GetComponent<MoveableBridge>();
        if (source == null) return;

        var moveable = target.AddOrGetComponent<MoveableBridge>();
        moveable.m_LiftOffsets = source.m_LiftOffsets;
        moveable.m_MovingTime = source.m_MovingTime;
        moveable.active = true;
    }





    /// <summary>
    /// Whether a donor brings structure of its own: something over the deck, or something under it
    /// that the road runs between.
    ///
    /// A lower deck brings neither - the towers belong to the deck above it - and building from one
    /// produces a road on stilts that reports nothing worse than a warning.
    /// </summary>
    private static bool HasStructure(BridgeStyleVariant variant)
    {
        if (variant.Overhead?.m_Sections?.Length > 0) return true;

        var subObjects = variant.SubObjects?.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();
        foreach (var info in subObjects)
        {
            if (info?.m_Object != null && info.m_Object.Has<PillarObject>()) return true;
        }

        return false;
    }

}
