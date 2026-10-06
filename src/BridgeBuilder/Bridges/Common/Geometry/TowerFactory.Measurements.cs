
using BridgeBuilder.Runtime;





using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;


using Unity.Mathematics;
using UnityEngine;


namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{

    /// <summary>How far the generated overhead section reaches across.</summary>
    private float _cableOuter;

    /// <summary>
    /// How far the source archetype's overhead section reached before widening. Kept separately so
    /// TrussArch01 can preserve the prototype difference between its base width and arch width.
    /// </summary>
    private float _cablePrototypeOuter;

    private string? _cableName;

    /// <summary>The tower the bridge being built names, which is the key the measured tables use.</summary>
    private string? _towerKey;

    /// <summary>The style being built, for its immutable authored geometry metadata.</summary>
    private string? _styleId;

    /// <summary>
    /// Final archetype structure allowance emitted by offline metaprogramming. It is supplied as part
    /// of the original bridge-style definition, never calculated from generated geometry at runtime.
    /// </summary>
    private float _archetypeStructureAllowance;

    /// <summary>
    /// The exact full-width delta already applied to TrussArch03's overhead arch for this bridge.
    /// Its pier must take this same delta: adding one number to both prototype widths preserves the
    /// measured prototype invariant without identifying parts from their geometry at runtime.
    /// </summary>
    private float? _trussArch03StructureExtra;

    // The blue pier is a support, not a selectable portal. Carry the arch's plan so the generic
    // source-road fallback cannot cancel target width against itself when sizing this support.
    private float? _trussArch01StructureExtra;

    /// <summary>
    /// Records the composer's final structural delta. Blue and green truss arches consume it because
    /// their objects are classified as supports and therefore are not selected as portals;
    /// recomputing from the selected tower would use the target road as the prototype datum.
    /// </summary>
    internal void MeasureStructureExtra(float extra)
    {
        if (string.Equals(_styleId, "TrussArch01", StringComparison.Ordinal))
            _trussArch01StructureExtra = extra;
        if (string.Equals(_styleId, "TrussArch03", StringComparison.Ordinal))
        {
            _trussArch03StructureExtra = extra;
        }
    }

    /// <summary>The outermost edge any of a section's pieces reaches, counting where each piece sits.</summary>
    private static float OuterOf(IEnumerable<NetPieceInfo> pieces)
    {
        var outer = 0f;
        foreach (var info in pieces)
        {
            if (info?.m_Piece == null) continue;
            var bounds = info.m_Piece.bounds;
            outer = Math.Max(outer, Math.Max(Math.Abs(bounds.min.x), Math.Abs(bounds.max.x)));
        }

        return outer;
    }

    /// <summary>
    /// Holds a generated tower to its distance from the cables it stands beside.
    ///
    /// The requirement is that the distance is the archetype's, at every width, and it is met by both
    /// edges moving outward by half the same number. That is a property of two independent code paths
    /// agreeing rather than of either one enforcing it, so it is measured on the result and reported
    /// when it drifts. See <see cref="BridgeCables.TowerBaseOutsideCables"/> for how it can drift without
    /// anyone making an arithmetic mistake.
    /// </summary>
    private void CheckSpacing(string name, IReadOnlyList<ObjectMeshInfo> parts)
    {
        if (_cableOuter <= 0f || parts.Count == 0 || _towerKey == null) return;

        // Only where a distance was measured to a section that encloses the road. Everywhere else
        // there is no archetype distance to hold the result to, and reporting one would be reporting
        // another family's number.
        var spacing = BridgeCables.SizingSpacingFor(_towerKey);
        if (spacing == null) return;

        for (var index = 0; index < parts.Count; index++)
        {
            if (parts[index]?.m_Mesh is not RenderPrefab mesh) continue;

            var bounds = mesh.bounds;
            var outer = Math.Max(Math.Abs(bounds.min.x), Math.Abs(bounds.max.x))
                + Math.Abs(parts[index].m_Position.x);
            var measured = outer - _cableOuter;
            if (BridgeCables.SpacingHolds(spacing.Value, measured, index, parts.Count)) continue;

            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' part {1} of {2} stands {3:0.###} m outside the cables of '{4}', where the "
                + "archetype stands {5:0.###} m. The tower and the cables were derived from different "
                + "bridges, so the two were widened from different starting widths.",
                name, index + 1, parts.Count, measured, _cableName,
                spacing.Value.For(index, parts.Count)));
        }
    }


    /// <summary>
    /// Forgets the cables of the bridge just finished, so the next one is sized against its own.
    ///
    /// The factory outlives a single bridge - it caches towers so one asked for twice is built once -
    /// and the cable measurement must not. A bridge with no overhead section sized against the
    /// previous bridge's cables would be wrong in a way nothing reported.
    /// </summary>
    internal void BeginBridge(
        string? styleId = null,
        float archetypeStructureAllowance = 0f,
        string bridgeName = "")
    {
        // This is an ownership boundary, not merely a measurement reset. A factory may be retained by
        // the future runtime creator and asked to build many bridges in one game session; no tower
        // from the preceding bridge may satisfy a request made by the next one, even if its style,
        // width or user-facing name happens to match.
        _thisRun.Clear();
        _bridgeName = bridgeName;
        _cableOuter = 0f;
        _cablePrototypeOuter = 0f;
        _cableName = null;
        _towerKey = null;
        _structureWidths = null;
        _trussArch03StructureExtra = null;
        _trussArch01StructureExtra = null;
        // Set here and not only where a tower is created. The sections are widened first - the cables
        // and the railings that live beside them - so anything that asks which style is being built
        // while that happens was asking a null. The inner railing rule did, and did nothing, silently.
        _styleId = styleId;
        _archetypeStructureAllowance = archetypeStructureAllowance;
    }

    /// <summary>
    /// How much wider than its archetype this tower has to be - taken from where the cables ended up.
    ///
    /// The requirement is that the tower stands the archetype's distance outside the cables, so the
    /// cables are what the tower is measured against. Solving
    /// <c>towerOuter + extra/2 == cableOuter + distance</c> gives the extra directly, and it comes out
    /// the same whichever part is used, because the archetype satisfies all three distances at once:
    /// the placeholder's single part is its top, at 3.67887 outside cables that reach 13.47333, and the
    /// replacement's legs are at 3.53745 outside the same cables, and both reduce to twice however far
    /// the cables moved.
    ///
    /// The old rule - the deck's width minus the road the tower was authored for - gives the same
    /// answer whenever the tower and the cables came from the same bridge, which is the ordinary case
    /// and is why the two agreed to five decimals on every bridge measured. It stops giving the same
    /// answer when they do not, and they need not: the tower archetype is chosen by width from the
    /// recorded list, the cables come from whichever installed bridge carries that tower, and the same
    /// tower is carried by several. Then the road rule sizes the tower against a road the cables know
    /// nothing about, and the two are widened from different starting widths. Measuring against the
    /// cables cannot drift that way, because the cables are the thing the distance is to.
    ///
    /// With no cables to measure against - most bridge types have no overhead section at all - the road
    /// rule is what there is, and it is used.
    /// </summary>
    private float ExtraFor(ObjectMeshInfo[] parts, float authored, float deckWidth, string name)
    {
        // TrussArch03's object is a support rather than a portal, so it is intentionally absent from
        // tower selection. The generic fallback consequently has no selected prototype-road datum.
        // Use the exact delta already applied to its arch: prototype pier + delta minus prototype arch
        // + delta is always the prototype's measured 4.313902 m difference.
        if (string.Equals(_styleId, "TrussArch03", StringComparison.Ordinal)
            && _trussArch03StructureExtra.HasValue)
        {
            var extra = _trussArch03StructureExtra.Value;
            var archWidth = BridgeTowers.TrussArch03PrototypeArchWidth + extra;
            var pierWidth = BridgeTowers.TrussArch03PrototypePierWidth + extra;
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: TrussArch03 pier takes the arch's exact {1:0.###} m width delta: pier "
                + "{2:0.######} m minus arch {3:0.######} m = prototype {4:0.######} m.",
                name, extra, pierWidth, archWidth, BridgeTowers.TrussArch03PierMinusArch));
            return extra;
        }

        // Apply the original bridge-style equation using only its final immutable source parameters.
        var byRoad = deckWidth - authored + _archetypeStructureAllowance;

        // TrussArchBridge01's first pillar mesh is the pier visible directly beneath the side arch.
        // The immutable difference below was measured from the shipped archetype by the offline
        // metaprogram. Runtime must not infer this relationship from generated bounds: doing that made
        // a failed section silently redefine the pier and base too. The separately authored base
        // preserves its own archetype difference in ExtraForPart.
        if (_styleId == "TrussArch01"
            && _towerKey == "TrussArchBridge01NetPillar"
            && parts.Length > 0)
        {
            // Create rejects a missing plan before allocating geometry. This is the arch's ordinary
            // width input, not an adjustment inferred from the generated mesh. The existing immutable
            // vertex maps still translate columns and stretch centre-crossing beams at every LOD.
            var sectionExtra = _trussArch01StructureExtra!.Value;
            var byTruss = TrussArch01Geometry.PierExtraForSection(sectionExtra);
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: widened the blue pier {1:0.###} m from immutable TrussArchBridge01 "
                + "metadata: section delta {2:0.###} m plus the prototype section/pier edge "
                + "total-width difference {3:0.###} m.",
                name, byTruss, sectionExtra,
                TrussArch01Geometry.PrototypeSectionWidth
                    - TrussArch01Geometry.PrototypePierWidth));
            return byTruss;
        }

        if (_cableOuter <= 0f || parts.Length == 0 || _towerKey == null) return byRoad;

        // Only towers whose own distances have been measured, and only where the section they are
        // measured to is the envelope the road runs between. Held as three constants and applied to
        // anything with an overhead section, these sized an extradosed tower - whose 21 m section is
        // narrower than its 31 m road and encloses nothing - against a suspension bridge's numbers.
        var spacing = BridgeCables.SizingSpacingFor(_towerKey);
        if (spacing == null)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: sized by the road, because '{1}' has no measured distance to its overhead "
                + "section - either the section is not the envelope the road runs between, or the "
                + "distance has not been taken. Widened {2:0.###} m.",
                name, _towerKey, byRoad));
            return byRoad;
        }

        // The legs, which are what stand beside the cables. A tower of one part is a placeholder and
        // its part is the top; see BridgeCables.Spacing.For.
        var part = BridgeCables.LegIndexOf(parts.Length);
        if (parts[part]?.m_Mesh is not RenderPrefab mesh) return byRoad;

        var bounds = mesh.bounds;
        var outer = Math.Max(Math.Abs(bounds.min.x), Math.Abs(bounds.max.x))
            + Math.Abs(parts[part].m_Position.x);
        var wanted = _cableOuter + spacing.Value.For(part, parts.Length);
        var byCables = BridgeCables.ExtraForTower(
            spacing.Value, _cableOuter, outer, part, parts.Length);

        // A tower the road would not fit through is not an improvement on a tower at the wrong
        // spacing. Nothing measured comes close to this - the cables already stand outside the
        // carriageway and the tower outside them - so reaching it means the donor is not what it was
        // taken for, and the road rule is the safer of two wrong answers.
        if (wanted <= deckWidth * 0.5f)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' sized against the cables of '{1}' would stand {2:0.###} m from the centre, "
                + "inside the {3:0.#} m deck it has to straddle, so it was sized against the road "
                + "instead. The cables are not the ones this tower belongs to.",
                name, _cableName, wanted, deckWidth));
            return byRoad;
        }

        if (Math.Abs(byCables - byRoad) > BridgeCables.SpacingTolerance)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: widened {1:0.###} m to stand {2:0.###} m outside the cables of '{3}', where the "
                + "road alone would have given {4:0.###} m. The tower and the cables were derived from "
                + "different bridges; the cables are what the distance is measured to.",
                name, byCables, spacing.Value.For(part, parts.Length), _cableName, byRoad));
        }

        return byCables;
    }

    /// <summary>
    /// Width change for one TrussArch01 prototype part. The separately authored base preserves the
    /// prototype's measured base-minus-arch width difference:
    /// generatedBase = generatedArch + (prototypeBase - prototypeArch).
    /// Widen applies the solved number to the base's prototype coordinates with
    /// x -> x + sign(x) * (extra / 2), never with a proportional scale.
    /// </summary>
    private float ExtraForPart(ObjectMeshInfo info, float towerExtra, string towerName)
    {
        if (_styleId != "TrussArch01"
            || _towerKey != "TrussArchBridge01NetPillar"
            || info.m_Mesh is not RenderPrefab mesh
            || !string.Equals(
                mesh.name, "TrussArchBridge01NetPillarBase Mesh", StringComparison.Ordinal))
            return towerExtra;

        var prototypeBaseWidth = TrussArch01Geometry.PrototypeBaseWidth;
        var prototypeArchWidth = TrussArch01Geometry.PrototypeSectionWidth;
        var prototypeDifference = prototypeBaseWidth - prototypeArchWidth;
        // Use the same plan directly for this independently authored part and all its LODs.
        // Do not reconstruct it by subtracting rounded pier dimensions.
        var baseExtra = _trussArch01StructureExtra!.Value;
        var generatedArchWidth = prototypeArchWidth + baseExtra;
        var generatedBaseWidth = generatedArchWidth + prototypeDifference;
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: TrussArch01 prototype base uses rigid x -> x + sign(x) * delta with "
            + "delta {1:0.###} m. Prototype base {2:0.###} m minus prototype arch {3:0.###} m "
            + "is the preserved {4:0.###} m difference; generated arch {5:0.###} m therefore gives "
            + "base {6:0.###} m.",
            towerName, baseExtra * 0.5f, prototypeBaseWidth, prototypeArchWidth,
            prototypeDifference, generatedArchWidth, generatedBaseWidth));
        return baseExtra;
    }


    /// <summary>
    /// A tower of more than one part must say it is a stack, or it does not reach the ground.
    ///
    /// The parts carry the archetype's components across, so this holds by construction - which is
    /// exactly why it is checked rather than assumed. Stacking is the difference between a tower that
    /// stands on the ground and one drawn at the height it was modelled at, and the fault is invisible
    /// in every measurement: the geometry, the bounds, the placement and the widths are all correct
    /// without it.
    /// </summary>
    private void CheckStacking(string name, IReadOnlyList<ObjectMeshInfo>? parts)
    {
        if (parts == null || !BridgeTowerSpec.Stacks(parts.Count)) return;

        var stacked = 0;
        foreach (var info in parts)
        {
            if (info?.m_Mesh is RenderPrefab mesh && mesh.Has<StackProperties>()) stacked++;
        }

        if (stacked == parts.Count) return;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "'{0}' has {1} part(s) and {2} of them say they stack. A tower reaches the ground by "
            + "stacking its repeatable part; without that on every part it is drawn at the height it "
            + "was modelled at and hangs above the ground by however far it was raised.",
            name, parts.Count, stacked));
    }


    /// <summary>
    /// One profile for a whole section, measured from every mesh of every piece it holds.
    ///
    /// A section's pieces are one structure seen at different points along the span, so a feature that
    /// appears in more than one of them has to move the same way in each. Measured per piece it does
    /// not: the golden bridge's end piece opens wider than its middle piece - it carries the anchorage
    /// - and the same pair of cables was scaled in one and carried in the other, meeting at neither
    /// node.
    ///
    /// Asked per height as well as per section, which is what keeps the railings intact. The golden
    /// bridge's railings are golden and live in these same support meshes, alongside the cables; at
    /// their height nothing stands on the centre line, so they are carried out rigidly and their
    /// distance to the tower's outer edge is the archetype's, whatever the road's width.
    /// </summary>
    private TowerWidening.Profile ProfileOfPieces(NetPieceInfo[] pieces)
    {
        var shapes = new List<float3[]>();
        var outlines = new List<IReadOnlyList<int>?>();
        foreach (var info in pieces)
        {
            if (info?.m_Piece == null) continue;

            Mesh[]? loaded = null;
            try
            {
                loaded = PrivateGeometryReader.Read(info.m_Piece);
                foreach (var mesh in loaded ?? Array.Empty<Mesh>())
                {
                    if (mesh == null) continue;

                    shapes.Add(ToPoints(mesh.vertices));
                    outlines.Add(mesh.triangles);
                }
            }
            catch (Exception)
            {
                // Generation diagnostics are silent; retain the external API exception boundary.
            }
            finally
            {
                if (loaded != null)
                {
                    try { PrivateGeometryReader.Release(loaded); }
                    catch (Exception) { /* a courtesy to the cache */ }
                }
            }
        }

        return TowerWidening.Profile.Of(shapes, outlines);
    }

    /// <summary>
    /// The portal a tower opens, and the narrowest opening any one of its parts leaves.
    ///
    /// The narrowest is what a bridge narrower than the archetype runs into. A V pylon's legs converge
    /// downward - the V pylon's stand 5.79 m apart at the bottom - so bringing the tower in by more
    /// than half of that carries each leg past the centre, where it is stopped, and the two arrive as
    /// one column. The widest opening says nothing about it: that pylon opens 36 m at the top, which
    /// survives the same correction easily.
    /// </summary>
    private float OpeningOf(ObjectMeshInfo[] parts, out float narrowest)
    {
        var widest = 0f;
        narrowest = 0f;
        foreach (var info in parts)
        {
            if (info?.m_Mesh is not RenderPrefab render) continue;

            Mesh[]? loaded = null;
            try
            {
                loaded = PrivateGeometryReader.Read(render);
                foreach (var mesh in loaded ?? Array.Empty<Mesh>())
                {
                    if (mesh == null) continue;
                    var opening = TowerWidening.ClearSpanOf(
                        ToPoints(mesh.vertices), TowerWidening.SpanBands);
                    widest = Math.Max(widest, opening);
                    if (opening > TowerWidening.CentreEpsilon)
                        narrowest = narrowest <= 0f ? opening : Math.Min(narrowest, opening);
                }
            }
            catch (Exception)
            {
                // Generation diagnostics are silent; retain the external API exception boundary.
            }
            finally
            {
                if (loaded != null)
                {
                    try { PrivateGeometryReader.Release(loaded); }
                    catch (Exception) { /* a courtesy to the cache, not a correctness requirement */ }
                }
            }
        }

        return widest;
    }

}
