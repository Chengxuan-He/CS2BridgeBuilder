using Colossal.AssetPipeline;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    /// <summary>
    /// A copy of an overhead section - the cables - sized for this deck. Null when it could not be
    /// built, which leaves the caller with the donor's own.
    ///
    /// Every attempt to fix the cables by shifting their lateral offset did nothing, and the
    /// measurements say why:
    ///
    ///     road 12 -> "2-Lane Suspension Bridge" 15 m @ 0
    ///     road 16 -> "3-Lane Suspension Bridge" 19 m @ 0
    ///     road 20 -> "4-Lane Suspension Bridge" 23 m @ 0
    ///     road 24 -> "5-Lane Suspension Bridge" 27 m @ 0
    ///
    /// The offset is zero every time, and shifting zero moves nothing. What changes with the road is the
    /// section's width - road plus three - and that width lives in a single full-width net piece with
    /// the cables modelled into it. The game does not place cables; it swaps in a wider piece. So this
    /// does the same, which it can because a net piece is a render prefab like any other.
    /// </summary>
    internal NetSectionPrefab? WidenSection(
        NetSectionPrefab source, string bridgeName, float extra, bool preserveGeometry = false)
    {
        if (preserveGeometry) extra = 0f;
        // Named for the bridge, not for the widening, where the style fits railings to the road.
        //
        // A section keyed by how much it was widened is shared by every bridge widened by that much -
        // which is right while a section is a function of the widening alone, and wrong the moment it
        // is not. The kerb railings are placed against the footways of one particular road: two roads
        // of the same width with different footways would be handed the same section, and the second
        // would wear the first one's railings.
        // A fresh plan for each section, and the same one for every piece of it and every level of
        // detail of those. The pieces are one structure seen at different points along the span - the
        // end piece carries an anchorage the middle one does not - so planned separately they read
        // different bands and treat the same railing differently, which is a railing that changes as
        // the eye moves along the bridge.
        _kerbPlans = null;

        var wanted = TowerPrefabNaming.Safe(preserveGeometry
            ? string.Format(CultureInfo.InvariantCulture, "{0}-{1}", bridgeName, source.name)
            : BridgeTowers.BringsItsOwnRailings(_styleId)
                ? string.Format(CultureInfo.InvariantCulture, "{0}-{1}", source.name, bridgeName)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1:0.#}", source.name, extra));

        if (_sectionsThisRun.TryGetValue(wanted, out var already))
        {
            // The factory can reuse a section generated earlier in the same run, but BeginBridge has
            // reset the width measurements. Restore both sides of the archetype relationship before
            // the tower and its base are derived; otherwise ExtraForPart falls back to the generic
            // tower delta and the base silently loses its prototype width rule.
            _cablePrototypeOuter = OuterOf(source.m_Pieces ?? Array.Empty<NetPieceInfo>());
            _cableOuter = OuterOf(already.m_Pieces ?? Array.Empty<NetPieceInfo>());
            _cableName = already.name;
            return already;
        }

        // Rebuilt across runs, under a name of its own, for the same reason the towers are: handing
        // back what a previous run left behind means every change to how cables are widened stops
        // taking effect the moment one has been built once.
        var name = wanted;
        for (var attempt = 2; Exists(name); attempt++)
        {
            name = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", wanted, attempt);
        }

        try
        {
            if (source.m_Pieces == null || source.m_Pieces.Length == 0)
            {
                _report.Warning(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' has no pieces of its own. Cable generation was stopped; "
                    + "the donor is not used as a fallback.", source.name));
                return null;
            }

            // Keep the archetype's actual outer width before any piece is widened. TrussArch01's
            // separately authored base preserves its prototype width difference from this arch.
            _cablePrototypeOuter = OuterOf(source.m_Pieces);

            // TrussArch03's full-detail prototype has already made the x=0 decision offline and its
            // LODs inherit that exact decision. Re-measuring it here would replace the committed
            // metaprogram result with a runtime geometry guess. Other styles still use their shared
            // section profile.
            TowerWidening.Profile? profile =
                preserveGeometry || _styleId is "TrussArch02" or "TrussArch03"
                    ? null
                    : ProfileOfPieces(source.m_Pieces);

            var pieces = new List<NetPieceInfo>();
            foreach (var info in source.m_Pieces)
            {
                if (info?.m_Piece == null) continue;

                var piece = WidenPiece(info.m_Piece, name, pieces.Count, extra, profile, preserveGeometry);
                if (piece == null) return null;

                pieces.Add(new NetPieceInfo
                {
                    m_Piece = piece,
                    m_RequireAll = info.m_RequireAll?.ToArray(),
                    m_RequireAny = info.m_RequireAny?.ToArray(),
                    m_RequireNone = info.m_RequireNone?.ToArray(),
                    m_Offset = new float3(
                        TowerWidening.Spread(info.m_Offset.x, extra), info.m_Offset.y, info.m_Offset.z),
                });
            }

            if (pieces.Count == 0)
            {
                _report.Warning($"'{name}' was not generated: none of '{source.name}' could be read.");
                return null;
            }

            var widened = ScriptableObject.CreateInstance<NetSectionPrefab>();
            _created.Add(widened);
            widened.name = name;

            // The archetype's own components, carried across.
            //
            // Not applied from a recorded template. The template held what was measured on the
            // suspension bridge's cable piece and on its tower's parts, and applying it to every
            // family's geometry is the fault rule 9 names: an arch section is not a cable sheet and a
            // truss is not a pylon. Here the archetype is in hand - it is the thing being widened - so
            // there is nothing to recall and nothing to get wrong.
            //
            // AddComponentFrom copies field by field through a JSON round trip, which is also what
            // gives the component its back reference to the prefab that owns it. Adding to
            // `components` directly leaves that null and the component's own Initialize throws.
            foreach (var component in source.components)
            {
                if (component != null) widened.AddComponentFrom(component);
            }

            // The levels of detail the component just named are the archetype's. Derive them.
            DeriveLods(widened, name, extra, null, preserveGeometry: preserveGeometry);

            widened.m_Pieces = pieces.ToArray();
            widened.m_SubSections = source.m_SubSections?.ToArray();

            _sectionsThisRun[wanted] = widened;

            // Remembered so the tower built next can be held to its distance from these cables. The
            // composer widens the cables first and fits the tower second, per bridge.
            _cableOuter = OuterOf(pieces);
            _cableName = name;

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: cables derived from '{1}' ({2:0.#} m) by widening {3} piece(s) {4:0.#} m{5}.",
                name, source.name, NetWidth.Of(source), pieces.Count, extra,
                Math.Abs(extra) < 0.001f ? " - identical to the original" : string.Empty));

            return widened;
        }
        catch (Exception)
        {
            _report.Warning($"'{name}' could not be generated, so the donor's own cables were kept.");
            return null;
        }
    }

    /// <summary>
    /// One widened net piece. The width has to move with the mesh: it is what the composition lays the
    /// piece out by, so a widened mesh under an unchanged width would be drawn clipped to the old one.
    /// </summary>

    /// <summary>
    /// A copy of a section with the pieces that stand on the deck taken out, or null if it has none.
    ///
    /// A copy, and not the section itself. <c>Highway Side 0</c> belongs to every highway in the game:
    /// taking the railing off it in place takes the railing off all of them, which is the same fault
    /// as stripping a track's pillars in place and is worse than the one being fixed.
    /// </summary>

    /// <summary>
    /// Whether a piece is drawn along the elevated deck itself, rather than at the point where the
    /// deck meets the ground.
    ///
    /// The game gates these by state. A piece that requires <c>Elevated</c> and nothing else is the
    /// straight run of the bridge; one that also requires <c>HighTransition</c> or <c>LowTransition</c>
    /// is at the end, where the deck comes down to meet the road - the turnaround. The bridge brings
    /// its own railing along the run and none at the end, so that is where the road's own is wanted
    /// and where it is not.
    ///
    /// Asked of the requirements rather than of the shape, because the shape cannot say it. Reading
    /// "a side piece standing above the deck" took the road's tunnel, lowered, raised and sound
    /// barrier pieces off as well: they stand above their own deck, on roads that are not this bridge.
    /// </summary>

    /// <summary>One more requirement on a piece, without disturbing the ones it had.</summary>
    private static NetPieceRequirements[] With(
        NetPieceRequirements[]? requirements, NetPieceRequirements added)
    {
        var existing = requirements ?? Array.Empty<NetPieceRequirements>();
        foreach (var requirement in existing)
        {
            if (requirement == added) return existing.ToArray();
        }

        var all = new NetPieceRequirements[existing.Length + 1];
        Array.Copy(existing, all, existing.Length);
        all[existing.Length] = added;
        return all;
    }

    private static bool OnTheElevatedRun(NetPieceInfo piece)
    {
        var all = piece.m_RequireAll;
        if (all == null) return false;

        var elevated = false;
        foreach (var requirement in all)
        {
            if (requirement == NetPieceRequirements.HighTransition) return false;
            if (requirement == NetPieceRequirements.LowTransition) return false;
            if (requirement == NetPieceRequirements.Elevated) elevated = true;
        }

        return elevated;
    }

    internal NetSectionPrefab? WithoutDeckPieces(
        NetSectionPrefab source, string name, float above, out IReadOnlyList<string> removed)
    {
        var taken = new List<string>();
        removed = taken;

        var pieces = source.m_Pieces;
        if (pieces == null || pieces.Length == 0) return null;

        var kept = new List<NetPieceInfo>();
        foreach (var piece in pieces)
        {
            if (piece?.m_Piece != null
                && piece.m_Piece.m_Layer == NetPieceLayer.Side
                && piece.m_Piece.m_HeightRange.max > above
                && OnTheElevatedRun(piece))
            {
                // Kept, and asked for one thing more: that this is somewhere the road is joined or
                // ends. The piece then draws at a seam with another net and at a turnaround, where the
                // bridge has no railing of its own, and nowhere along the run, where it has.
                //
                // Asked as a choice and not as a second requirement. Requiring the end outright drew
                // the railing at a dead end only, and a bridge that meets another bridge is neither a
                // dead end nor a transition to the ground - so the seam had no railing from either
                // side of it. The game never asks for Elevated and Node together in one set, which is
                // why this is a choice between them rather than an addition to them.
                taken.Add(piece.m_Piece.name);
                kept.Add(new NetPieceInfo
                {
                    m_Piece = piece.m_Piece,
                    m_RequireAll = piece.m_RequireAll?.ToArray(),
                    m_RequireAny = With(
                        With(piece.m_RequireAny, NetPieceRequirements.DeadEnd),
                        NetPieceRequirements.Node),
                    m_RequireNone = piece.m_RequireNone?.ToArray(),
                    m_Offset = piece.m_Offset,
                });
                continue;
            }

            kept.Add(piece!);
        }

        if (taken.Count == 0) return null;


        var copy = ScriptableObject.CreateInstance<NetSectionPrefab>();
        _created.Add(copy);
        copy.name = TowerPrefabNaming.Safe(name);

        foreach (var component in source.components)
        {
            if (component != null) copy.AddComponentFrom(component);
        }

        copy.m_Pieces = kept.ToArray();
        copy.m_SubSections = source.m_SubSections?.ToArray();
        return copy;
    }

    private NetPiecePrefab? WidenPiece(
        NetPiecePrefab original, string sectionName, int index, float extra,
        TowerWidening.Profile? profile, bool preserveGeometry)
    {
        var name = index == 0 ? sectionName + " Piece" : $"{sectionName} Piece {index}";

        var widened = ScriptableObject.CreateInstance<NetPiecePrefab>();

        widened.m_Layer = original.m_Layer;
        widened.m_Width = original.m_Width + extra;
        widened.m_Length = original.m_Length;
        widened.m_HeightRange = original.m_HeightRange;
        widened.m_WidthOffset = original.m_WidthOffset;
        widened.m_NodeOffset = original.m_NodeOffset;
        widened.m_SideConnectionOffset = original.m_SideConnectionOffset;
        widened.m_SurfaceHeights = original.m_SurfaceHeights;


        // No width to pass: which parts stretch and which move is decided by the geometry itself, by
        // whether a part crosses the centre line. The cable sheet does, so it is scaled about the
        // centre and its outer edge lands half the extra width further out - the same distance the
        // tower's legs travel, which is what keeps the two at the archetype's spacing.
        return Widen(original, widened, name, extra, profile,
            railings: true, preserveGeometry: preserveGeometry);
    }

}
