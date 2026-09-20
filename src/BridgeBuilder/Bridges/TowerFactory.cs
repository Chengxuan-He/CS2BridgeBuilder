using Colossal.AssetPipeline;
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

/// <summary>
/// Builds a tower sized for one particular road, by widening the style's own.
///
/// Derived, not invented. The standard this has to meet is that a tower generated for a road the game
/// already has a bridge for must be that bridge's tower - so the geometry starts as a real tower of
/// that style and every vertex is carried outward by <see cref="TowerWidening"/>. At the width it was
/// authored for the shift is zero and the mesh comes out vertex for vertex identical; at any other
/// width the legs are the same legs, further apart.
///
/// A tower is usually modelled in pieces - a base, a shaft, a top - each sitting at its own offset.
/// They are measured together and moved together: measuring them separately spreads a narrow
/// crossbeam further than the legs under it, and zeroing their offsets collapses them onto each
/// other. Both were tried; both take the tower apart.
///
/// The surfaces come across too - the materials themselves, not the tower they were read off. A
/// SurfaceAsset is a shader and its textures; pointing at one is not pointing at another bridge, and
/// it is what lets a generated tower look like the tower it was derived from without this mod
/// shipping textures of its own.
/// </summary>
internal sealed class TowerFactory
{
    private readonly ExportReport _report;
    private readonly PrefabBase[] _prefabs;
    private readonly List<PrefabBase> _created = new();
    private readonly PreviewGeometry? _previewGeometry;

    /// <summary>
    /// The road width and footway either side of the road being fitted, where there is one.
    ///
    /// Two numbers and not one: a road may carry a footway on one side and a shoulder on the other,
    /// and the archetype's inner railing follows each side's own kerb.
    /// </summary>
    private (RoadEdge Left, RoadEdge Right)? _roadEdges;

    /// <summary>
    /// The target bridge's two semantic width envelopes. They are equal for ordinary styles; the
    /// white TrussArchBridge02 records its fitted visible-road envelope outside and the outermost
    /// footway boundaries inside.
    /// </summary>
    private readonly struct StructureWidths
    {
        internal StructureWidths(float outer, float innerLeft, float innerRight)
        {
            Outer = Math.Max(0f, outer);
            InnerLeft = Math.Max(0f, innerLeft);
            InnerRight = Math.Max(0f, innerRight);
        }

        internal float Outer { get; }
        internal float InnerLeft { get; }
        internal float InnerRight { get; }
        internal float Inner => InnerLeft + InnerRight;
    }

    private StructureWidths? _structureWidths;

    /// <summary>
    /// What is being done to the kerb railing of the piece in hand, while it is being derived.
    ///
    /// Held across the piece and its levels of detail rather than worked out afresh for each. A coarse
    /// mesh does not always draw the two railings apart, so asked for itself it finds one, does
    /// nothing, and keeps what the full detail mesh took away.
    /// </summary>
    private List<KerbPlan>? _kerbPlans;

    /// <summary>The bridge being built, for the things that must not be shared with another.</summary>
    private string _bridgeName = string.Empty;

    /// <summary>
    /// Towers built for the bridge currently being created. Different bridges never share this map;
    /// one bridge that places its own tower more than once still references one owned prefab.
    /// </summary>
    private readonly Dictionary<string, ObjectPrefab> _thisRun = new(StringComparer.Ordinal);

    /// <summary>The same, for the overhead sections that carry the cables.</summary>
    private readonly Dictionary<string, NetSectionPrefab> _sectionsThisRun = new(StringComparer.Ordinal);

    /// <summary>
    /// Everything this factory built, in the order it has to be saved: meshes before the tower that
    /// references them. A generated prefab that is never written is a reference to nothing, which is
    /// what made the first generated tower a broken asset - the geometry was saved, the prefabs
    /// wrapping it were not.
    /// </summary>
    internal IReadOnlyList<PrefabBase> Created => _created;

    /// <summary>The style currently being composed; exposed only to the composer decision pipeline.</summary>
    internal string? StyleId => _styleId;

    /// <summary>Records both outer section boundaries read from the target road prefab.</summary>
    internal void MeasureFootways(RoadEdge left, RoadEdge right) => _roadEdges = (left, right);

    /// <summary>Records the reviewed outer and inner targets for the bridge currently being built.</summary>
    internal void MeasureStructureWidths(float outer, float innerLeft, float innerRight) =>
        _structureWidths = new StructureWidths(outer, innerLeft, innerRight);


    /// <summary>The ground decal every tower's base part carries, looked up once per run.</summary>
    private RenderPrefab? _groundBase;

    private bool _groundBaseSearched;

    /// <summary>
    /// The game's own <c>Default_Base Mesh</c>, found by name.
    ///
    /// By name and not from the archetype: the archetype may not be installed, and this is base game
    /// content that is there whenever the game is. If it somehow is not, the base is left off - a
    /// tower without a ground decal is a cosmetic fault, a tower that failed to generate is not.
    /// </summary>
    private RenderPrefab? GroundBase()
    {
        if (_groundBaseSearched) return _groundBase;
        _groundBaseSearched = true;

        _groundBase = _prefabs
            .OfType<RenderPrefab>()
            .FirstOrDefault(prefab => string.Equals(
                prefab.name, BridgeTowerSpec.BaseMeshName, StringComparison.Ordinal));

        if (_groundBase == null)
        {
            _report.Warning(
                $"'{BridgeTowerSpec.BaseMeshName}' was not found, so generated towers carry no ground base.");
        }

        return _groundBase;
    }

    internal TowerFactory(PrefabSystem prefabSystem, ExportReport report,
        PreviewGeometry? previewGeometry = null)
    {
        _report = report;
        _previewGeometry = previewGeometry;
        _prefabs = PrefabCatalog.GetAll(prefabSystem)
            .Where(prefab => prefab != null)
            .ToArray();
    }

    /// <summary>
    /// A tower spanning <paramref name="deckWidth"/> metres, derived from the style's own.
    /// Null when one could not be built, which is never fatal: the caller keeps the tower it had.
    /// </summary>
    internal ObjectPrefab? Create(
        string styleId, string sourceTowerName, float sourceRoadWidth, float deckWidth,
        bool primary = true)
    {
        _towerKey = sourceTowerName;
        _styleId = styleId;

        if (styleId == "TrussArch01" && sourceTowerName == "TrussArchBridge01NetPillar"
            && !_trussArch01StructureExtra.HasValue)
        {
            _report.Failed(sourceTowerName, new InvalidOperationException(
                "The blue truss-arch pier requires its bridge's structural width plan."));
            return null;
        }

        // Every generated bridge owns its tower prefab. Sharing by style and width (for example
        // Suspension-40) made a later bridge depend on mutable prefab state created for an earlier
        // one, and prevents either bridge from evolving independently at runtime. The golden bridge
        // already carried the bridge name; that convention now applies to every style.
        //
        // A style can name more than one structure - a pylon at course ends and a pier at nodes. The
        // primary structure has exactly [bridge prefix]-[bridge name]; a secondary retains its source
        // name after that owner key so the two structures of the same bridge remain distinct.
        var wanted = TowerPrefabNaming.ForBridge(
            styleId, deckWidth, _bridgeName, sourceTowerName, primary);

        // Within one bridge the same tower can be asked for twice - a double deck wants it for both
        // decks - and building it twice would be two prefabs where one is meant.
        if (_thisRun.TryGetValue(wanted, out var already))
        {
            _report.Note($"{wanted}: the same tower as the one built for the other deck.");
            return already;
        }

        // Across runs it is rebuilt, under a name of its own.
        //
        // This used to find the tower a previous run left behind and hand that back. It reads as an
        // optimisation and behaves as a freeze: the old tower is returned whatever has changed since -
        // a different road, a corrected width, a fixed derivation - and the report says "reused" while
        // the bridge on screen is the one built by code that no longer exists. Several rounds of
        // generation changes had no effect at all for this reason, because none of the code past this
        // line ran.
        //
        // Naming around the old one rather than replacing it, because a prefab already registered in
        // the session is referenced by whatever was built from it, and the mod's own removal is what
        // clears those out.
        var name = wanted;
        for (var attempt = 2; Exists(name); attempt++)
        {
            name = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", wanted, attempt);
        }

        if (!string.Equals(name, wanted, StringComparison.Ordinal))
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: an earlier '{1}' is still registered, so this one is built as '{0}'. Remove the "
                + "generated bridges to reclaim the name.",
                name, wanted));
        }

        try
        {
            var authoredTower = Find(sourceTowerName);
            if (authoredTower == null)
            {
                _report.Warning(
                    $"'{name}' was not generated: the tower it derives from ('{sourceTowerName}') is not installed.");
                return null;
            }

            RecordPillars(authoredTower, authoredTower, name);

            // A placeholder stays a placeholder, and its replacement is generated alongside it.
            //
            // The net names a placeholder, and the game swaps that for a real object when the bridge is
            // built. Handing the net the replacement directly instead - which is what deriving from the
            // concrete object amounted to - takes the bridge off that path, and off it the tower is
            // placed as an ordinary sub object and hangs above the ground. The golden bridge was the one
            // type that kept working, and the reason is that its tower is named directly rather than
            // through a placeholder, so nothing about it was being rerouted.
            //
            // So both halves are built: a placeholder shaped like the original placeholder, and a
            // replacement that declares itself its stand-in. The net gets the placeholder, the same
            // arrangement the bridge had before, and the materials still come from the replacement.
            var built = authoredTower.Has<PlaceholderObject>()
                ? CreatePair(authoredTower, name, sourceRoadWidth, deckWidth)
                : Build(authoredTower, name, sourceRoadWidth, deckWidth, BridgeTowerTemplate.ApplyToWhole);

            if (built != null) _thisRun[wanted] = built;
            return built;
        }
        catch (Exception)
        {
            _report.Warning($"'{name}' could not be generated, so the style's own tower was kept.");
            return null;
        }
    }


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
    /// when it drifts. See <see cref="BridgeCables.TowerOutsideCables"/> for how it can drift without
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
                loaded = info.m_Piece.ObtainMeshes();
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
                    try { info.m_Piece.ReleaseMeshes(); }
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
                loaded = render.ObtainMeshes();
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
                    try { render.ReleaseMeshes(); }
                    catch (Exception) { /* a courtesy to the cache, not a correctness requirement */ }
                }
            }
        }

        return widest;
    }


    /// <summary>
    /// Points a derived prefab's levels of detail at derived meshes instead of the archetype's.
    ///
    /// <c>LodProperties.m_LodMeshes</c> names other render prefabs, and carrying the component across
    /// carries those names with it - so a widened piece kept the archetype's own coarse meshes. Close
    /// up it drew the widened one and looked right; far enough away the game swapped to a level of
    /// detail that was never widened, and the structure snapped back to the width it was authored at.
    /// That is a fault with a viewing distance attached to it, which is a hard thing to catch and an
    /// easy one to describe once seen.
    ///
    /// So each level is derived the same way the mesh above it was, by the same extra and against the
    /// same boundary, and the component is repointed. A level that cannot be derived is dropped rather
    /// than left: no level of detail is a mesh drawn at full detail from further away, while the wrong
    /// level is a mesh of the wrong size.
    /// </summary>
    private void DeriveLods(
        PrefabBase widened, string name, float extra, TowerWidening.Profile? profile,
        bool railings = false, bool preserveGeometry = false)
    {
        var lods = widened.GetComponent<LodProperties>();
        var meshes = lods?.m_LodMeshes;
        if (lods == null || meshes == null || meshes.Length == 0) return;

        var derived = new List<RenderPrefab>();
        for (var index = 0; index < meshes.Length; index++)
        {
            var source = meshes[index];
            if (source == null) continue;

            // The levels of detail belong to the piece, so they take its railing plan too. Left out,
            // a railing taken off the deck up close is still there at a distance.
            var copy = Widen(
                source,
                ScriptableObject.CreateInstance<RenderPrefab>(),
                string.Format(CultureInfo.InvariantCulture, "{0} LOD{1}", name, index + 1),
                extra,
                profile,
                railings,
                preserveGeometry);

            if (copy == null) continue;

            // A level of detail has no levels of its own. The copy carried the source's components
            // across and the source may name its own, which would derive levels of levels without
            // end - and the guard is here rather than at the recursion because the answer is not
            // "stop after N" but "there is nothing below this".
            copy.components.RemoveAll(component => component is LodProperties);
            derived.Add(copy);
        }

        var sources = new HashSet<RenderPrefab>(meshes.Where(mesh => mesh != null));
        lods.m_LodMeshes = derived.ToArray();

        // Nothing derived may still be one of the archetype's own. Checked rather than assumed,
        // because when it is wrong the piece draws correctly at the distance anyone works at and
        // wrongly at some other distance, and no screenshot of the thing being worked on shows it.
        var kept = derived.Where(sources.Contains).ToArray();
        if (kept.Length > 0)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' still points at the archetype's own level(s) of detail: {1}. It will draw at "
                + "the width it was widened to up close and at the width it was authored at from "
                + "further away.",
                name, string.Join(", ", kept.Select(mesh => $"'{mesh.name}'"))));
        }

        if (derived.Count != meshes.Length)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' kept {1} of {2} level(s) of detail. The rest could not be derived and were "
                + "dropped rather than left pointing at the archetype's own, which would have snapped "
                + "back to its width at a distance.",
                name, derived.Count, meshes.Length));
        }
    }


    /// <summary>
    /// A level of detail must be widened by what the mesh above it was widened by.
    ///
    /// Not "to the same width". A coarse mesh rounds off what a fine one draws, so the two are not the
    /// same width in the archetype either - the extradosed tower's top is 1.03 m wider than its second
    /// level - and demanding they match reported that difference as a fault every time. What has to
    /// hold is that both moved by the same amount, which is what an un-derived level fails: it does
    /// not move at all.
    /// </summary>

    /// <summary>
    /// What to do with the kerb railing on one side: the band it occupies, and where it goes.
    ///
    /// Worked out once, from the mesh that shows the most, and then applied to every mesh of the same
    /// piece. A level of detail draws the same railing with fewer triangles and does not always
    /// resolve the two of them as separate stands - so asked for itself it finds one railing, does
    /// nothing, and keeps a railing the full detail mesh has taken away. That is a railing which is
    /// there from a distance and gone up close, which is what the bridge showed.
    /// </summary>
    private readonly struct KerbPlan
    {
        internal KerbPlan(float side, float from, float to, float shift, bool remove, float3 onto)
        {
            Side = side;
            From = from;
            To = to;
            Shift = shift;
            Remove = remove;
            Onto = onto;
        }

        /// <summary>Which side of the centre this is, as a sign.</summary>
        internal float Side { get; }

        /// <summary>The band the kerb railing occupies, as distances from the centre.</summary>
        internal float From { get; }

        internal float To { get; }

        /// <summary>How far it is carried, when it is kept.</summary>
        internal float Shift { get; }

        /// <summary>Whether it is taken away instead.</summary>
        internal bool Remove { get; }

        /// <summary>The point it is drawn to when it is taken away.</summary>
        internal float3 Onto { get; }

        /// <summary>Whether a vertex belongs to the railing this plan is about.</summary>
        internal bool Covers(float3 vertex) =>
            Math.Sign(vertex.x) == Math.Sign(Side)
            && Math.Abs(vertex.x) >= From - TowerWidening.CentreEpsilon
            && Math.Abs(vertex.x) <= To + TowerWidening.CentreEpsilon;
    }

    private List<KerbPlan>? PlanKerbRailings(
        string name,
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? outline,
        float extra)
    {
        if (source.Length != moved.Length || source.Length == 0) return null;
        if (!BridgeTowers.BringsItsOwnRailings(_styleId)) return null;
        if (!_roadEdges.HasValue) return null;

        var bands = GoldenBridgeRailings.BandsOf(source, -RailingFoot, RailingHead);

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: material between {1:0.##} m and {2:0.##} m above the deck stands at {3}. "
            + "Road width {4:0.##} m; outermost sidewalk widths read from the road prefab: left "
            + "{5:0.##} m, right {6:0.##} m.",
            name,
            -RailingFoot,
            RailingHead,
            bands.Count == 0
                ? "nothing"
                : string.Join(", ", bands.Select(band => string.Format(
                    CultureInfo.InvariantCulture, "{0:0.##}..{1:0.##}", band.From, band.To)))
                + " m from the centre",
            _roadEdges.Value.Left.OuterBoundary + _roadEdges.Value.Right.OuterBoundary,
            _roadEdges.Value.Left.SidewalkWidth,
            _roadEdges.Value.Right.SidewalkWidth));

        if (bands.Count < 2)
        {
            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: nothing stands at a kerb here - the deck carries {1} band(s) of railing.",
                name, bands.Count));
            return null;
        }

        var plans = new List<KerbPlan>();
        var roadSurfaceGap = BridgeTowers.RailingRoadSurfaceGap(_styleId);
        foreach (var side in new[] { -1f, 1f })
        {
            var edge = side < 0f ? _roadEdges.Value.Left : _roadEdges.Value.Right;
            var footway = edge.SidewalkWidth;
            if (!GoldenBridgeRailings.TryPlan(
                    bands,
                    source,
                    moved,
                    -RailingFoot,
                    RailingHead,
                    edge,
                    side,
                    roadSurfaceGap,
                    out var railing))
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: the authored suspension railing layout could not be read; leaving its railings as authored.",
                    name));
                continue;
            }

            var kerb = railing.Layout.Inner;

            if (railing.Remove)
            {
                // No footway on this side, so no railing at its kerb. Every vertex of it is drawn to
                // one point, so every triangle of it has no area and none is rasterised - all three
                // coordinates, or each quad still stands in a plane and is drawn as a sheet.
                var onto = float3.zero;
                var found = false;
                for (var index = 0; index < source.Length; index++)
                {
                    var at = Math.Abs(source[index].x);
                    if (Math.Sign(source[index].x) != Math.Sign(side)) continue;
                    if (at < kerb.From || at > kerb.To) continue;

                    onto = new float3(side * railing.OuterEdgeAfter, moved[index].y, moved[index].z);
                    found = true;
                    break;
                }

                if (!found) continue;

                plans.Add(new KerbPlan(side, kerb.From, kerb.To, 0f, remove: true, onto));
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: no railing at the {1} kerb - that side of the road has no footway. Taking "
                    + "away what stands at {2:0.##}..{3:0.##} m.",
                    name, side < 0f ? "left" : "right", kerb.From, kerb.To));
                continue;
            }

            // Match the two boundary-facing railing edges: the outer railing's outer edge and the
            // inner railing's road-facing edge. SuspensionBridge01 uses the complete sidewalk width;
            // the golden family retains its measured one-metre road-surface strip.
            var shift = railing.Shift;
            plans.Add(new KerbPlan(side, kerb.From, kerb.To, shift, remove: false, float3.zero));

            _report.Note(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: the {1} boundary-facing railing edges are {2:0.###} m apart: the road prefab's "
                + "{3:0.###} m outermost sidewalk less the prototype's {4:0.###} m road-surface "
                + "gap. Road edge {5:0.###} m, outer railing edge {6:0.###} m; sidewalk inner edge "
                + "{7:0.###} m, inner railing edge {8:0.###} -> {9:0.###} m. What stands at "
                + "{10:0.##}..{11:0.##} m is carried {12:0.###} m, where the deck moved "
                + "{13:0.###} m.",
                name,
                side < 0f ? "left" : "right",
                railing.RailingGap,
                railing.SidewalkWidth,
                railing.RoadSurfaceGap,
                railing.RoadOuterBoundary,
                railing.OuterEdgeAfter,
                railing.SidewalkInnerBoundary,
                railing.InnerEdgeBefore,
                railing.InnerTarget,
                kerb.From,
                kerb.To,
                shift,
                extra * 0.5f));
        }

        return plans;
    }

    /// <summary>
    /// Carries out a plan on one mesh, by where its vertices are.
    ///
    /// By position and not by piece, so that a level of detail which does not resolve the two railings
    /// apart is still treated the same as the mesh it stands in for.
    /// </summary>
    private static bool[]? ApplyKerbPlans(
        IReadOnlyList<KerbPlan> plans, float3[] source, float3[] moved)
    {
        if (source.Length != moved.Length) return null;

        bool[]? dropped = null;
        for (var index = 0; index < source.Length; index++)
        {
            foreach (var plan in plans)
            {
                if (!plan.Covers(source[index])) continue;

                if (plan.Remove)
                {
                    // Marked, not moved. What is taken off the bridge is taken out of the index
                    // buffer when the mesh is written; moving it anywhere leaves it in the file.
                    dropped ??= new bool[source.Length];
                    dropped[index] = true;
                }
                else
                {
                    moved[index] = new float3(
                        source[index].x + plan.Shift, moved[index].y, moved[index].z);
                }

                break;
            }
        }

        return dropped;
    }

    /// <summary>How far below deck level a railing may start, and how high it may reach.</summary>
    private const float RailingFoot = 0.5f;

    private const float RailingHead = 3f;

    /// <summary>One widened object, derived from one authored object.</summary>
    private StaticObjectPrefab? Build(
        ObjectGeometryPrefab source,
        string name,
        float sourceRoadWidth,
        float deckWidth,
        Action<ObjectGeometryPrefab> role)
    {
        {
            var parts = (source.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
                .Where(info => info?.m_Mesh is RenderPrefab)
                .ToArray();
            if (parts.Length == 0)
            {
                _report.Warning($"'{name}' was not generated: '{source.name}' has no readable mesh.");
                return null;
            }

            // One shift for the whole tower, measured across all of its parts together.
            //
            // Measuring each part on its own was the first version, and it took a tower apart: the
            // crossbeam is narrower than the legs it sits on, so it was told to spread by more than
            // they were, and the pieces no longer met. A tower is one object that happens to be
            // modelled in pieces, so the pieces move as one.
            // The shift is what puts the tower the archetype's distance outside the cables, which is
            // what the distance is measured to. At the tower's own width it is zero and the result is
            // that tower unchanged, which is the property the self test checks. `authored` stays the
            // road, because half the road is where the legs begin and that is a different quantity.
            var authored = sourceRoadWidth > 0f ? sourceRoadWidth : WidthOf(parts);
            // From where the cables ended up, not from the road - see ExtraFor. The two agree
            // whenever the tower and the cables came from the same bridge, and only the cables
            // are right when they did not.
            var extra = ExtraFor(parts, authored, deckWidth, name);

            // Each part against its own opening, which is where its own legs begin.
            //
            // One boundary for the whole tower was tried and is worse. A tower's parts open by
            // different amounts - the golden pillar's four open 43.31, 24.98, 13 and 8 metres - and
            // the widest is inside the legs of every other part: taking 43.31 for all of them put the
            // boundary at 21.66, while the pier's legs begin at 12.49, so the leg was cut in two and
            // its inner nine metres scaled. A leg is never scaled; that is rule 5, and this scaled
            // most of one.
            //
            // What one boundary was meant to fix was shear: parts stretching their interiors by
            // different ratios. They should. A part's interior is the material spanning between that
            // part's own legs, and it stretches to meet them - by its own ratio, because they are its
            // own legs. Two parts that open differently are two spans of different lengths.
            //
            // The inversion that one boundary also fixed is fixed properly in TowerWidening: the ratio
            // is floored at zero and a translation stops at the centre, so a part brought in by more
            // than it opens closes rather than folding through itself.
            var opening = OpeningOf(parts, out var narrowest);

            // A part brought in by more than it stands out is carried through the centre and comes
            // out the other side. Reported, not prevented: holding the whole tower back to whatever
            // its narrowest part can take left every other part - the base most visibly - wider than
            // the road it was built for, which is a fault in the bridge rather than in one part of it.
            if (narrowest > TowerWidening.CentreEpsilon && narrowest + extra <= 0f)
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' has a part opening only {1:0.##} m and is being brought in by {2:0.##} m, "
                    + "so that part is carried through the centre. The road is narrower than this "
                    + "design was drawn for.",
                    name, narrowest, -extra));
            }

            if (opening > TowerWidening.CentreEpsilon
                && (opening * 0.5f) + (extra * 0.5f) <= 0f)
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' opens {1:0.##} m and is being brought in by {2:0.##} m, which closes the "
                    + "portal the road passes through. It was closed to nothing rather than folded "
                    + "through itself, and neither is a bridge the road fits under.",
                    name, opening, -extra));
            }

            var meshes = new List<ObjectMeshInfo>();
            var partExtras = new Dictionary<ObjectMeshInfo, float>();
            var layerWidths = new List<string>();
            foreach (var info in parts)
            {
                var sourceMesh = (RenderPrefab)info.m_Mesh!;
                var partExtra = ExtraForPart(info, extra, name);
                if (_structureWidths.HasValue)
                {
                    partExtra = BridgeTowers.WhiteTrussArchWidths.TowerPartExtra(
                        _styleId, sourceMesh.name,
                        _structureWidths.Value.Inner, partExtra);
                }
                partExtras[info] = partExtra;
                var widened = Widen(sourceMesh, name, meshes.Count, partExtra);
                if (widened == null) continue;

                // The part keeps where it sat, carried outward by the same shift as its vertices.
                // Zeroing these was what collapsed the base, the shaft and the top onto one another.
                var position = info.m_Position;
                if (Math.Abs(position.x) > 0.001f)
                {
                    if (TrussArch02Geometry.IsRecorded(_styleId, sourceMesh.name)
                        && _structureWidths.HasValue)
                    {
                        position.x += position.x > 0f
                            ? _structureWidths.Value.InnerRight
                                - TrussArch02Geometry.PrototypeSectionInnerRight
                            : -(_structureWidths.Value.InnerLeft
                                - TrussArch02Geometry.PrototypeSectionInnerLeft);
                    }
                    else
                    {
                        position.x += position.x > 0f
                            ? partExtra * 0.5f
                            : -partExtra * 0.5f;
                    }
                }

                meshes.Add(new ObjectMeshInfo
                {
                    m_Mesh = widened,
                    m_Position = position,
                    m_Rotation = info.m_Rotation,
                    m_RequireState = info.m_RequireState,
                });

                if (Math.Abs(partExtra - extra) > TowerWidening.CentreEpsilon)
                {
                    layerWidths.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' by {1:0.###} m", sourceMesh.name, partExtra));
                }
            }

            if (meshes.Count == 0)
            {
                _report.Warning($"'{name}' was not generated: none of '{source.name}' could be read.");
                return null;
            }

            var tower = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            _created.Add(tower);
            tower.name = name;
            tower.m_Meshes = meshes.ToArray();

            // Everything that is not geometry comes from the template.
            //
            // Not from the prefab this was derived from. The tower a bridge is built around may not be
            // installed - a road can be converted with nothing of the style present but the numbers -
            // and a generated tower still has to behave like a tower. BridgeTowerTemplate holds what
            // one is, read out of the game once and written down, so the result does not depend on the
            // archetype being there to copy from.
            role(tower);

            if (_styleId is "GoldenGate" or "GoldenGateDouble")
            {
                // The Golden Gate foundation is a Base candidate, not another standalone
                // shaft. Keep its native role after binding the generated placeholder.
                if (source.TryGet<PillarObject>(out var authoredPillar))
                {
                    var pillar = tower.AddOrGetComponent<PillarObject>();
                    pillar.m_Type = authoredPillar.m_Type;
                    pillar.m_AnchorOffset = authoredPillar.m_AnchorOffset;
                    pillar.m_VerticalPillarOffsetRange = authoredPillar.m_VerticalPillarOffsetRange;
                    pillar.active = authoredPillar.active;
                }

                // Native anchorages explicitly forbid terrain raising AND lowering. Without
                // this component their below-origin geometry excavates a hole at each end.
                if (source.TryGet<BuildingTerraformOverride>(out var terraform))
                    tower.AddComponentFrom(terraform);
                tower.m_Circular = source.m_Circular;
            }

            // Lights and other props mounted on the authored object are part of the tower, not part of
            // the selected road. Earlier generation rebuilt only the placeholder/pillar role and
            // silently discarded ObjectSubObjects, so lit archetypes produced unlit towers. Carry the
            // component whole, then translate each side-mounted child by the same delta as the mesh it
            // names through m_ParentMesh. The light prefab still owns its EffectSource, colour, range
            // and culling data; retaining that exact reference preserves the authored light effect.
            CarryAuthoredSubObjects(source, tower, partExtras, extra, name);

            // The stacking goes on the parts, and it is what lets the tower reach the ground: without
            // it the game builds no StackData, gives the placed tower no Stack, and draws it at the
            // height it was modelled at - hanging above the ground by however far it was raised.
            // The parts carry the archetype's own components now, stacking included - see Widen. What
            // is left here is the check: a multi-part tower with no stacking is the floating tower,
            // and it went unreported for five rounds because nothing looked.
            CheckStacking(name, tower.m_Meshes);

            // And held to its distance from the cables it will stand beside.
            CheckSpacing(name, tower.m_Meshes);

            if (layerWidths.Count > 0 && _structureWidths.HasValue)
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: white truss bridge-pier derived from '{1}': outer deck target {2:0.###} m, "
                    + "inner arch/pier target {3:0.###} m; {4}. The pier column and its footing use "
                    + "the same inner-layer displacement, preserving their prototype joint; the "
                    + "same assignment is reused by every LOD.",
                    name, source.name, _structureWidths.Value.Outer, _structureWidths.Value.Inner,
                    string.Join(", ", layerWidths)));
            }
            else
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: derived from '{1}' ({2:0.#} m authored, {3} part(s)) by moving everything "
                    + "{4:0.#} m apart{5}.",
                    name, source.name, authored, meshes.Count, extra,
                    Math.Abs(extra) < 0.001f ? " - geometry identical to the original" : string.Empty));
            }

            return tower;
        }
    }

    /// <summary>
    /// Carries tower-mounted props, including every authored light, without classifying them by name.
    /// A child object is a rigid part: a non-zero x position follows its parent mesh's half-width
    /// displacement, while a child on x = 0 remains on the centre line. All non-positional fields and
    /// all referenced effect prefabs remain those of the archetype.
    /// </summary>
    private void CarryAuthoredSubObjects(
        ObjectGeometryPrefab source,
        ObjectGeometryPrefab target,
        IReadOnlyDictionary<ObjectMeshInfo, float> partExtras,
        float objectExtra,
        string name)
    {
        if (!source.TryGet<ObjectSubObjects>(out var authored) || authored == null) return;

        target.AddComponentFrom(authored);
        if (!target.TryGet<ObjectSubObjects>(out var carried) || carried == null)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' did not retain the archetype's ObjectSubObjects component, so its mounted "
                + "lights and props were not published.",
                name));
            return;
        }

        var entries = carried.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>();
        var sourceParts = source.m_Meshes ?? Array.Empty<ObjectMeshInfo>();
        var shifted = 0;

        foreach (var entry in entries)
        {
            if (entry == null) continue;

            var childExtra = objectExtra;
            var parent = entry.m_ParentMesh;
            if (parent >= 0
                && parent < sourceParts.Length
                && sourceParts[parent] != null
                && partExtras.TryGetValue(sourceParts[parent], out var recordedExtra))
            {
                childExtra = recordedExtra;
            }

            var position = entry.m_Position;
            var shift = childExtra * 0.5f;
            var movedX = position.x == 0f
                ? position.x
                : position.x + (position.x > 0f ? shift : -shift);
            if (BitConverter.SingleToInt32Bits(movedX)
                != BitConverter.SingleToInt32Bits(position.x))
            {
                shifted++;
            }

            position.x = movedX;
            entry.m_Position = position;
        }

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: carried {1} authored tower-mounted object(s), including their exact light/effect "
            + "prefab references, rotations, parent meshes, groups and probabilities; {2} side "
            + "object(s) followed their parent mesh's width displacement.",
            name,
            entries.Length,
            shifted));
    }

    /// <summary>
    /// How wide the whole tower is: the outermost reach of any of its parts, counting where each part
    /// sits as well as how wide it is.
    /// </summary>
    private static float WidthOf(ObjectMeshInfo[] parts)
    {
        var width = 0f;
        foreach (var info in parts)
        {
            if (info.m_Mesh is not RenderPrefab mesh) continue;
            var bounds = mesh.bounds;
            var reach = math.max(
                Math.Abs(info.m_Position.x + bounds.max.x),
                Math.Abs(info.m_Position.x + bounds.min.x));
            width = math.max(width, reach * 2f);
        }

        return width;
    }

    /// <summary>
    /// A generated placeholder and the generated object it turns into, mirroring the pair the bridge
    /// already had.
    ///
    /// The placeholder is what the net references and what carries the placement behaviour - including
    /// how far down the tower reaches - so it is built from the authored placeholder and keeps its
    /// components. The replacement is built from the object the game would have substituted, which is
    /// where the materials live. Neither half is invented; each is the widened form of the half it
    /// stands in for.
    /// </summary>
    private ObjectPrefab? CreatePair(
        ObjectGeometryPrefab placeholder, string name, float sourceRoadWidth, float deckWidth)
    {
        var concretes = Concretes(placeholder, name);

        // The placeholder is built from the placeholder, exactly as the game builds its own.
        //
        // It was built from the replacement's geometry for a while, on the reasoning that a placeholder
        // holding only the shaft would hang in the air if the swap ever failed. Read out of the game,
        // the reference does not do that:
        //
        //     5LaneSuspensionBridgePillar Placeholder   1 part,  y 0..86.55
        //     5LaneSuspensionBridgePillar               3 parts, y -10..86.55
        //
        // The shaft alone is what a placeholder is meant to be. Padding it out is a second difference
        // from the reference laid over whatever the first one was, and differences from the reference
        // are what every fault here has turned out to be.
        var stand = Build(
            placeholder, name, sourceRoadWidth, deckWidth, BridgeTowerTemplate.ApplyToPlaceholder);
        if (stand == null) return null;

        if (concretes.Length == 0) return stand;

        // Every replacement, not the likeliest one.
        //
        // A standalone pillar is not stretched to reach the ground - SubObjectSystem compares each
        // candidate's own height against the gap between the deck and the terrain and takes one that
        // covers it. The placeholder stands for a set of them at different heights, and that set is how
        // one bridge serves a crossing of any depth. Deriving only the most probable member leaves a
        // single height: high enough for the bridge it was copied from, and short of the ground for a
        // taller one, which is a tower hanging in the air with nothing under it.
        var built = new List<string>();
        foreach (var concrete in concretes)
        {
            var replacement = Build(
                concrete,
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", name, concrete.name),
                sourceRoadWidth,
                deckWidth,
                tower => BridgeTowerTemplate.ApplyToReplacement(tower, stand));
            if (replacement == null) continue;

            built.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1:0.#} m tall, probability {2})",
                replacement.name, HeightOf(replacement), BridgeTowerSpec.SpawnProbability));
        }

        if (built.Count == 0)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': none of the {1} replacement(s) for '{2}' could be derived, so the tower has "
                + "only its placeholder and will not reach the ground.",
                name, concretes.Length, placeholder.name));
            return stand;
        }

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: built as a placeholder with {1} replacement(s) - {2}.",
            name, built.Count, string.Join(", ", built)));

        return stand;
    }

    /// <summary>
    /// Every object a placeholder can turn into, shortest first.
    ///
    /// All of them, not the likeliest. A standalone pillar is never stretched to reach the ground:
    /// <c>SubObjectSystem.CreateSubObject</c> reads each candidate's own <c>ObjectGeometryData.m_Size.y</c>,
    /// takes off its placement offset, and compares what is left against the gap between the deck and
    /// the terrain. The placeholder stands for a set of pillars at different heights, and that set is
    /// how one bridge serves a crossing of any depth.
    /// </summary>
    private ObjectGeometryPrefab[] Concretes(ObjectGeometryPrefab placeholder, string name)
    {
        if (!placeholder.Has<PlaceholderObject>()) return Array.Empty<ObjectGeometryPrefab>();

        var found = new List<ObjectGeometryPrefab>();
        foreach (var candidate in _prefabs.OfType<ObjectGeometryPrefab>())
        {
            if (!candidate.TryGet<SpawnableObject>(out var spawnable)) continue;
            if (spawnable?.m_Placeholders == null) continue;
            if (!spawnable.m_Placeholders.Any(entry => ReferenceEquals(entry, placeholder))) continue;

            found.Add(candidate);
        }

        if (found.Count == 0)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': nothing declares itself a replacement for the placeholder '{1}', so the tower "
                + "keeps the placeholder's stand-in surface and will render untextured.",
                name, placeholder.name));
            return Array.Empty<ObjectGeometryPrefab>();
        }

        var ordered = found.OrderBy(HeightOf).ToArray();

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: '{1}' stands for {2} object(s) - {3}. All are derived, because the game chooses "
            + "between them by height rather than stretching one.",
            name, placeholder.name, ordered.Length,
            string.Join(", ", ordered.Select(candidate =>
                string.Format(CultureInfo.InvariantCulture, "{0} ({1:0.#} m tall)",
                    candidate.name, HeightOf(candidate))))));

        return ordered;
    }

    /// <summary>
    /// How tall an object's meshes reach, counting where each part sits as well as how tall it is.
    ///
    /// This is the number the game selects pillars on, so it is worth reporting even though nothing
    /// here computes with it: a set of towers that all stop short of the deck leaves the bridge
    /// standing on nothing, and the heights are what say so.
    /// </summary>
    private static float HeightOf(ObjectGeometryPrefab prefab)
    {
        var top = float.MinValue;
        var bottom = float.MaxValue;

        foreach (var info in prefab.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
        {
            if (info?.m_Mesh is not RenderPrefab mesh) continue;
            top = Math.Max(top, info.m_Position.y + mesh.bounds.max.y);
            bottom = Math.Min(bottom, info.m_Position.y + mesh.bounds.min.y);
        }

        return top > bottom ? top - bottom : 0f;
    }

    /// <summary>
    /// What the pillar components say, on the placeholder and on the object it turns into.
    ///
    /// Kept because the tower failed to reach the ground several times for reasons that each looked
    /// settled, and the values themselves went unread through all of them. They turned out identical on
    /// both prefabs - type Standalone, range plus or minus a metre - which is what finally pointed at
    /// selection rather than stretching.
    /// </summary>
    private void RecordPillars(ObjectGeometryPrefab placeholder, ObjectGeometryPrefab source, string name)
    {
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: pillar data - placeholder '{1}' {2}; source '{3}' {4}.",
            name, placeholder.name, Describe(placeholder), source.name, Describe(source)));
    }

    private static string Describe(PrefabBase prefab)
    {
        if (!prefab.TryGet<PillarObject>(out var pillar) || pillar == null) return "has no PillarObject";

        return string.Format(
            CultureInfo.InvariantCulture,
            "type {0}, anchor {1}, vertical range {2}",
            pillar.m_Type, pillar.m_AnchorOffset, pillar.m_VerticalPillarOffsetRange);
    }


    /// <summary>Whether a prefab of this name is already registered, generated or shipped.</summary>
    private bool Exists(string name)
    {
        // _created matters when one long-lived factory creates several bridges at runtime: the first
        // bridge's dependencies may not have been published to PrefabSystem yet, but their names are
        // already reserved and the next bridge must not create a second prefab under the same key.
        return _created.Any(candidate => candidate != null
                && string.Equals(candidate.name, name, StringComparison.Ordinal))
            || _prefabs
            .Any(candidate => candidate != null
                && string.Equals(candidate.name, name, StringComparison.Ordinal));
    }

    private ObjectGeometryPrefab? Find(string towerName)
    {
        return _prefabs
            .OfType<ObjectGeometryPrefab>()
            .FirstOrDefault(candidate => string.Equals(candidate.name, towerName, StringComparison.Ordinal));
    }

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

    /// <summary>
    /// One widened copy of one of the source's meshes. Every level of detail is widened the same way,
    /// or the tower would change shape as the camera pulls back.
    /// </summary>
    private RenderPrefab? Widen(
        RenderPrefab original,
        string towerName,
        int index,
        float extra)
    {
        var name = index == 0 ? towerName + " Mesh" : towerName + " Mesh " + index;
        return Widen(
            original,
            ScriptableObject.CreateInstance<RenderPrefab>(),
            name,
            extra);
    }

    /// <summary>
    /// Widens <paramref name="original"/> into <paramref name="widened"/>, which the caller has already
    /// created as whatever kind of render prefab it needs.
    ///
    /// The caller chooses the type because a net piece is a <see cref="RenderPrefab"/> too - that is the
    /// whole reason the cables can be fixed at all. It no longer chooses the rule: which parts stretch
    /// and which move is decided by the geometry, by whether a part crosses the centre line.
    /// </summary>
    private T? Widen<T>(
        RenderPrefab original, T widened, string name, float extra,
        TowerWidening.Profile? profile = null, bool railings = false, bool preserveGeometry = false)
        where T : RenderPrefab
    {
        name = TowerPrefabNaming.Safe(name);
        Mesh[]? loaded = null;
        var models = new List<ModelImporter.Model>();
        // A rejected preview still owns the ScriptableObject allocated by the caller.
        // Track it before loading/transforming can fail.
        if (_previewGeometry != null) _created.Add(widened);
        try
        {
            var box = original.bounds;
            var shift = extra * 0.5f;
            var resultBounds = preserveGeometry ? box : new Bounds3(
                new float3(box.min.x - shift, box.min.y, box.min.z),
                new float3(box.max.x + shift, box.max.y, box.max.z));
            var span = resultBounds.max.x - resultBounds.min.x;
            // A source plane may legitimately have zero thickness. Reject inverted
            // bounds, or a positive source span collapsed by narrowing, not authored planes.
            if (!float.IsFinite(span) || span < 0f || (span == 0f && extra < 0f))
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' would have invalid width {1:R} m after a {2:R} m change. "
                    + "The bridge was stopped before geometry was written.", name, span, extra));
                if (_previewGeometry == null) UnityEngine.Object.Destroy(widened);
                return null;
            }
            loaded = original.ObtainMeshes();
            if (loaded == null || loaded.Length == 0) return null;

            // Every mesh the source holds, not the first of them. A render prefab can hold several -
            // the levels of detail - and it carries one surface for each; declaring one mesh while
            // handing over the whole set leaves the renderer pairing them off wrongly.
            var totalVertices = 0;
            var totalIndices = 0;
            var recordedTruss02 = TrussArch02Geometry.IsRecorded(_styleId, original.name);
            var recordedTruss03 = railings
                && TrussArch03Geometry.IsRecorded(_styleId, original.name);
            var recordedGeometry = recordedTruss02 || recordedTruss03;
            var suspensionSheetSpan = 0f;
            var continuousSuspensionSheet = railings
                && SuspensionGeometry.TryGetContinuousSpan(
                    _styleId, original.name, out suspensionSheetSpan);
            var rigidSuspensionSidePart = !railings
                && SuspensionGeometry.IsRigidSidePart(_styleId, original.name);
            var rigidGoldenGateCable = railings
                && GoldenGateGeometry.IsCable(_styleId, original.name);

            // One profile for everything widened here. A section hands one in, because its pieces
            // are one structure; a tower part measures its own, from its full detail mesh and the
            // prefabs its levels of detail live in, together. Those are one structure too - the same part drawn coarsely -
            // and letting each answer for itself is how a leg came to be carried at full detail and
            // scaled at distance, which read as the bridge changing width as the camera pulled back.
            //
            var shapes = new List<float3[]>();
            var outlines = new List<IReadOnlyList<int>?>();
            if (!recordedGeometry && !preserveGeometry)
            {
                foreach (var part in loaded)
                {
                    if (part == null) continue;

                    shapes.Add(ToPoints(part.vertices));
                    outlines.Add(part.triangles);
                }
            }

            // CONTRACT rule 8: an LOD cannot vote on what the part is. Keep the full-detail
            // archetype measurement before adding any coarse substitute. TrussArch01's portal uses
            // this exact side-body boundary at every viewing distance.
            var fullDetailScope = preserveGeometry ? null
                : profile ?? TowerWidening.Profile.Of(shapes, outlines);

            // The levels of detail are named by a component and live in prefabs of their own, so they
            // have to be fetched to be included. They were not, and the comment above said they were:
            // the scope was the full detail mesh alone, a coarse mesh's outermost material fell outside
            // the places that scope called carried, and it was scaled where the fine one was carried -
            // 7.899 m against 8, which is the bridge changing width as the camera pulls back.
            var lodMeshes = new List<RenderPrefab>();
            if (profile == null && !recordedGeometry && !preserveGeometry)
            {
                foreach (var lod in original.GetComponent<LodProperties>()?.m_LodMeshes
                    ?? Array.Empty<RenderPrefab>())
                {
                    if (lod == null) continue;

                    try
                    {
                        foreach (var mesh in lod.ObtainMeshes() ?? Array.Empty<Mesh>())
                        {
                            if (mesh == null) continue;

                            shapes.Add(ToPoints(mesh.vertices));
                            outlines.Add(mesh.triangles);
                        }

                        lodMeshes.Add(lod);
                    }
                    catch (Exception)
                    {
                        // Generation diagnostics are silent; retain the external API exception boundary.
                    }
                }
            }

            var scope = recordedGeometry || preserveGeometry
                ? null
                : profile
                    ?? (IsBluePrototypeMainPier(original)
                        ? fullDetailScope
                        : TowerWidening.Profile.Of(shapes, outlines));

            foreach (var lod in lodMeshes)
            {
                try { lod.ReleaseMeshes(); }
                catch (Exception) { /* a courtesy to the cache */ }
            }

            for (var index = 0; index < loaded.Length; index++)
            {
                var part = loaded[index];
                if (part == null) continue;

                // All three arch-above colours are open trusses. Their top beams cross x=0 and must
                // be stretched. Blue and white author one logical transverse assembly as several
                // render islands, which all share the complete full-detail reach. Green welds side
                // arches to transverse work, so it uses one continuous x-map measured from the
                // full-detail side boundary. The same decision is carried into every LOD.
                var source = ToPoints(part.vertices);
                var openTruss = IsThroughArchSection(railings);
                var preserveOpenTrussSides =
                    BridgeStyleDefinitions.PreservesOpenTrussSideAssembly(_styleId);
                var rigidBlueBase = IsBluePrototypeBase(original);
                var bluePortal = IsBluePrototypeMainPier(original);
                var blueSection = IsBluePrototypeSection(original);
                TowerWidening.TrussWideningFacts trussFacts = default;
                var usedRecordedTruss02 = false;
                var usedRecordedTruss03 = false;
                float3[] moved;
                bool[]? dropped = null;
                if (preserveGeometry)
                {
                    // Independent copy of the recorded central sheet, including every LOD.
                    // No spatial inference or road-width deformation belongs to this assembly.
                    moved = source;
                }
                else if (recordedTruss02)
                {
                    var applied = false;
                    if (railings && _structureWidths.HasValue)
                    {
                        applied = TrussArch02Geometry.TryWidenSection(
                            original.name,
                            source,
                            _structureWidths.Value.Outer,
                            _structureWidths.Value.InnerLeft,
                            _structureWidths.Value.InnerRight,
                            _roadEdges.HasValue && !_roadEdges.Value.Left.IsSidewalk,
                            _roadEdges.HasValue && !_roadEdges.Value.Right.IsSidewalk,
                            out moved,
                            out _,
                            out dropped);
                    }
                    else if (!railings)
                    {
                        var leftDelta = _structureWidths.HasValue
                            ? _structureWidths.Value.InnerLeft
                                - TrussArch02Geometry.PrototypeSectionInnerLeft
                            : extra * 0.5f;
                        var rightDelta = _structureWidths.HasValue
                            ? _structureWidths.Value.InnerRight
                                - TrussArch02Geometry.PrototypeSectionInnerRight
                            : extra * 0.5f;
                        applied = TrussArch02Geometry.TryWidenTowerPart(
                            original.name,
                            source,
                            leftDelta,
                            rightDelta,
                            out moved,
                            out _);
                    }
                    else
                    {
                        moved = source;
                    }

                    if (!applied)
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not match its immutable TrussArchBridge02 inner/outer "
                            + "vertex map. The derived prefab was stopped before geometry was written.",
                            name));
                        return null;
                    }

                    usedRecordedTruss02 = true;
                }
                else if (openTruss && recordedTruss03)
                {
                    if (!TrussArch03Geometry.TryWidenSection(
                            original.name,
                            source,
                            extra,
                            out moved,
                            out _,
                            out _))
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not match its immutable TrussArchBridge03 vertex map. "
                            + "The derived prefab was stopped before geometry was written.",
                            name));
                        return null;
                    }

                    usedRecordedTruss03 = true;
                }
                else
                {
                    moved = rigidGoldenGateCable
                        ? TowerWidening.Widen(source, extra)
                        : continuousSuspensionSheet
                        // The source-prefab metaprogram marks this entire net as one continuous
                        // transverse sheet. Full detail and every named LOD use the same recorded
                        // full-detail span, so distance cannot change its width decision.
                        ? TowerWidening.Stretch(source, suspensionSheetSpan, extra)
                        : rigidSuspensionSidePart
                            // These exact source identities contain side material only. Carrying all
                            // non-centre x coordinates preserves the columns and bases at every LOD.
                            ? TowerWidening.Widen(source, extra)
                    : rigidBlueBase
                        // AGENTS rule 8: the TrussArch01 deck base is authored as side material.
                        // Start from its prototype vertices and carry every non-zero x by the whole d.
                        // This is a translation, never a proportional widening of the base.
                        ? TowerWidening.WidenRigidBase(source, extra)
                        : bluePortal
                            // Generated metadata names every prototype vertex: the columns and side
                            // fittings translate rigidly, while only transverse beams crossing x=0
                            // stretch. LOD2 inherits the high-detail decision even though it is welded.
                            ? WidenBluePrototypePier(original, source, extra, name)
                        : blueSection
                            // Exact offline metadata keeps every side island rigid and stretches each
                            // centre-crossing logical top-truss assembly against its own archetype span.
                            // Using one global reach is what left shorter diagonal assemblies several
                            // metres short of the translated side arches.
                            ? WidenBluePrototypeSection(original, source, extra, name)
                        : openTruss
                        ? TowerWidening.WidenOpenTruss(
                            source, part.triangles, extra,
                            preserveOpenTrussSides,
                            scope!,
                            out trussFacts)
                        : TowerWidening.WidenParts(source, extra, scope!);
                }

                if (openTruss && !usedRecordedTruss02 && !usedRecordedTruss03 && !blueSection
                    && !trussFacts.ContractSatisfied)
                {
                    _report.Defect(string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' could not satisfy the recorded x=0 transform for every vertex. "
                        + "The current derived prefab was stopped before any geometry was written.",
                        name));
                    return null;
                }

                if (railings && _styleId == "CoveredWood"
                    && !CoveredWoodGeometry.TryApply(original.name, source, moved))
                {
                    _report.Defect($"'{name}' does not match the recorded CoveredWood upright map; geometry was not written.");
                    return null;
                }

                if (railings && TrussArch03BaseGeometry.IsRecorded(_styleId, original.name))
                {
                    if (!TrussArch03BaseGeometry.TryApply(
                            original.name,
                            source,
                            moved,
                            extra * 0.5f,
                            out var baseMoved,
                            out _,
                            out var baseError))
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not apply its recorded TrussArchBridge03 deck-base transform: {1}. "
                            + "The derived prefab was stopped before geometry was written.",
                            name,
                            baseError));
                        return null;
                    }

                    moved = baseMoved;
                }
                // Planned from the first mesh, which shows the most, and carried out on every one of
                // them. A level of detail asked for itself finds one railing where there are two and
                // keeps what the full detail mesh took away: a railing that is there from a distance
                // and gone up close.
                // Only where a railing is being fitted. The plan is held in a field so that a piece
                // and its levels of detail share one, and a field outlives the piece that set it: the
                // towers are derived after the sections, so a plan left standing was applied to them
                // too and drew whatever stood in its band - part of a leg - to a single point.
                if (IsGoldenTopOrnament(name, railings))
                {
                    TowerWidening.RectangularizeCentralSpoke(
                        source, moved, part.triangles, out _, out _);
                }

                if (railings && !preserveGeometry && !rigidGoldenGateCable)
                {
                    _kerbPlans ??= PlanKerbRailings(name, source, moved, part.triangles, extra);
                    if (_kerbPlans != null) dropped = ApplyKerbPlans(_kerbPlans, source, moved);
                }

                var partVertices = ToVectors(moved);

                var model = BuildModel(
                    models.Count == 0 ? name : name + " LOD" + models.Count,
                    part,
                    partVertices,
                    out var modelError,
                    dropped);
                if (model == null)
                {
                    _report.Defect(modelError ?? string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' could not be converted to generated geometry; the current derived "
                        + "prefab was stopped instead of publishing a partial mesh.",
                        name));
                    return null;
                }
                models.Add(model);
                totalVertices += partVertices.Length;
                totalIndices += (int)CountIndices(part);
            }

            if (models.Count == 0) return null;

            GeometryAsset? asset = null;
            if (_previewGeometry != null)
            {
                // Exactly the same generated vertex channels and submeshes as a
                // permanent bridge, but no asset database entry and no disk writes.
                if (!_previewGeometry.Capture(widened, models)) return null;
                widened.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
            var geometry = new Geometry(models.ToArray());
            // Defence at the write boundary as well as at each generated-prefab naming boundary:
            // AssetDataPath reads the last period as an extension, and rejects any extension other
            // than the one belonging to GeometryAsset.
            var geometryAssetName = TowerPrefabNaming.Safe(name);
            asset = AssetDatabase.user.AddAsset(
                AssetDataPath.Create("BridgeBuilder", geometryAssetName, EscapeStrategy.None),
                geometry);

            // Registering an asset is not writing it. Without this the geometry existed in the database
            // and nowhere else, so the renderer asked for its mesh a frame later and got nothing.
            asset.Save();

            // And then let go of it. An asset built in memory ends up in a state the loader treats as
            // impossible: its Data already holds the meshes it was constructed from while its Loading
            // still records that nothing has been read, and the header read asserts on the difference.
            // Save put the meshes on disk a line ago, so the load that follows reads them back the
            // ordinary way.
            asset.Unload();
            }

            if (_previewGeometry == null) _created.Add(widened);
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
            foreach (var component in original.components)
            {
                if (component != null) widened.AddComponentFrom(component);
            }

            // The levels of detail the component just named are the archetype's. Derive them.
            DeriveLods(
                widened, name, extra, scope, railings, preserveGeometry);

            widened.geometryAsset = asset;

            // Derived from the source's bounds, not recomputed from the vertices. Bounds are authored,
            // and for a pillar they reach below what the geometry draws; measuring the vertices returns
            // a box that stops where the drawing stops.
            //
            // One expression for every mesh now, because both branches of the widening rule move the
            // outermost vertex by the same half of the same number: a part that crosses the centre is
            // scaled by exactly what puts its outer edge there, and a part that does not is carried
            // there. There is no longer a stretched case and a translated case to tell apart.
            widened.bounds = resultBounds;


            widened.vertexCount = totalVertices;
            widened.indexCount = totalIndices;
            widened.meshCount = models.Count;
            widened.surfaceArea = original.surfaceArea;

            // The surfaces themselves, not the tower they came off. A SurfaceAsset is a shader and its
            // textures; pointing at one is not pointing at another bridge.
            var surfaces = original.surfaceAssets?.Where(surface => surface != null).ToArray()
                ?? Array.Empty<SurfaceAsset>();
            widened.surfaceAssets = surfaces;

            if (surfaces.Length != models.Count)
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' has {1} mesh(es) but {2} material(s), copied from '{3}'. The renderer pairs "
                    + "them off one to one, so at least one will draw with the wrong material.",
                    name, models.Count, surfaces.Length, original.name));
            }

            return widened;
        }
        finally
        {
            // Permanent Geometry owns these buffers; in-memory previews own only
            // the converted Unity meshes and must release the importer buffers now.
            if (_previewGeometry != null)
                foreach (var model in models) model.Dispose();
            if (loaded != null)
            {
                try
                {
                    original.ReleaseMeshes();
                }
                catch (Exception)
                {
                    // Releasing is a courtesy to the asset cache, not a correctness requirement.
                }
            }
        }
    }

    private bool IsThroughArchSection(bool section) => section
        && BridgeStyleDefinitions.UsesOpenTrussTopology(_styleId);

    /// <summary>
    /// Identifies the base directly from the TrussArch01 archetype, including its separately authored
    /// LOD prefabs. Its vertices take the contract's rigid side mapping instead of generic component
    /// classification; no generated width or road boundary is used to infer the base.
    /// </summary>
    private bool IsBluePrototypeBase(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && _towerKey == "TrussArchBridge01NetPillar"
        && (string.Equals(
                original.name, "TrussArchBridge01NetPillarBase Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillarBase_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillarBase_LOD2 Mesh", StringComparison.Ordinal));

    /// <summary>
    /// Identifies the TrussArch01 portal body and its LODs. This must not include the separately
    /// authored base: the base uses the contract's exact sign translation and a delta which preserves
    /// the TrussArchBridge01 prototype's base-minus-arch width difference.
    /// </summary>
    private bool IsBluePrototypeMainPier(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && _towerKey == "TrussArchBridge01NetPillar"
        && (string.Equals(
                original.name, "TrussArchBridge01NetPillar Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillar_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01NetPillar_LOD2 Mesh", StringComparison.Ordinal));

    /// <summary>
    /// Identifies the three shipped TrussArchBridge01 section meshes. The names select immutable
    /// metaprogram output only; no runtime coordinate threshold or topology guess is involved.
    /// </summary>
    private bool IsBluePrototypeSection(RenderPrefab original) =>
        _styleId == "TrussArch01"
        && (string.Equals(
                original.name, "TrussArchBridge01Net Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01Net_LOD1 Mesh", StringComparison.Ordinal)
            || string.Equals(
                original.name, "TrussArchBridge01Net_LOD2 Mesh", StringComparison.Ordinal));

    private float3[] WidenBluePrototypeSection(
        RenderPrefab original, float3[] source, float extra, string sectionName)
    {
        if (TrussArch01Geometry.TryWidenSection(original.name, source, extra, out var moved))
            return moved;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: unsupported TrussArch01 section mesh '{1}' with {2} vertices; no geometry "
            + "fallback was used, so the prototype coordinates were kept unchanged.",
            sectionName, original.name, source.Length));
        return moved;
    }

    private float3[] WidenBluePrototypePier(
        RenderPrefab original, float3[] source, float extra, string towerName)
    {
        if (TrussArch01Geometry.TryWidenPier(original.name, source, extra, out var moved)) return moved;

        _report.Defect(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: unsupported TrussArch01 pier mesh '{1}' with {2} vertices; no geometry fallback "
            + "was used, so the prototype coordinates were kept unchanged.",
            towerName, original.name, source.Length));
        return moved;
    }

    private bool IsGoldenTopOrnament(string name, bool railings)
    {
        if (railings || !string.Equals(_styleId, "SuspensionGolden", StringComparison.Ordinal))
            return false;

        // A tower consists of top, shaft and base meshes. Only the first ("Mesh") contains the fan;
        // "Mesh 1" and "Mesh 2" must remain untouched. Its LOD names append " LODn" to that same
        // first-mesh name, so they deliberately take the correction too.
        var marker = name.LastIndexOf(" Mesh", StringComparison.Ordinal);
        if (marker < 0) return false;
        var tail = name.Substring(marker + " Mesh".Length);
        return tail.Length == 0 || tail.StartsWith(" LOD", StringComparison.Ordinal);
    }

    /// <summary>
    /// One mesh turned into one model: the widened positions, and every other channel carried across
    /// byte for byte in the format the source declared it in.
    ///
    /// The formats are the point, and getting them wrong is what drew the cables as shards lying over
    /// the deck. A net piece declares:
    ///
    ///     Position:Float32x3@0  Normal:SNorm16x2@1  Tangent:Float32x1@1  TexCoord0:Float16x2@1
    ///
    /// Two components of signed normalised sixteen-bit for a normal, because it is octahedrally packed;
    /// one float for a tangent, because it is an angle about that normal. Reading them back through
    /// Unity's convenience accessors gives unpacked Vector3 and Vector4, and writing those out declares
    /// three and four floats where the shader expects two shorts and one float. The renderer walks each
    /// vertex at a stride it computes from the declared layout, so from the first vertex onward every
    /// channel is read from the wrong place. Positions survive - they are Float32x3 either way and come
    /// first - which is why the geometry was the right size and everything about it was wrong.
    ///
    /// So nothing is re-encoded. The raw vertex buffer is read, each attribute is lifted out of its
    /// stream at the offset and width the mesh says it occupies, and handed on unchanged. Only the
    /// positions are rewritten, and only because they are the one thing this is meant to change.
    /// </summary>
    private static ModelImporter.Model? BuildModel(
        string name, Mesh mesh, Vector3[] vertices, out string? error,
        bool[]? dropped = null)
    {
        error = null;
        var attributes = new List<ModelImporter.Model.VertexData>();
        var declared = mesh.GetVertexAttributes();

        // Refuse an unsupported layout before allocating any persistent buffers. This used to abort
        // from the game export path; now the caller records the failure and omits the incomplete
        // derived prefab without unwinding through the simulation update.
        foreach (var attribute in declared)
        {
            if (FormatSize(attribute.format) == 0)
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' uses unsupported vertex format {1}; the current derived prefab was "
                    + "stopped without publishing a partial mesh.",
                    name, attribute.format);
                return null;
            }

            if (attribute.attribute == VertexAttribute.Position
                && (attribute.format != VertexAttributeFormat.Float32 || attribute.dimension != 3))
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' stores positions as {1}x{2}, which this exporter cannot rewrite; the "
                    + "current derived prefab was stopped without publishing a partial mesh.",
                    name, attribute.format, attribute.dimension);
                return null;
            }
        }

        Mesh.MeshDataArray read = default;
        try
        {
            read = Mesh.AcquireReadOnlyMeshData(mesh);
            var data = read[0];

            // The raw bytes of each stream, kept as they are. Attributes within a stream are
            // interleaved, so each one is picked out by its own offset and width.
            var streams = new Dictionary<int, byte[]>();
            var strides = new Dictionary<int, int>();
            foreach (var attribute in declared)
            {
                if (streams.ContainsKey(attribute.stream)) continue;

                var raw = data.GetVertexData<byte>(attribute.stream);
                var copy = new byte[raw.Length];
                raw.CopyTo(copy);
                streams[attribute.stream] = copy;
                strides[attribute.stream] = mesh.GetVertexBufferStride(attribute.stream);
            }

            foreach (var attribute in declared)
            {
                var size = FormatSize(attribute.format) * attribute.dimension;
                var offset = mesh.GetVertexAttributeOffset(attribute.attribute);
                var stride = strides[attribute.stream];
                var stream = streams[attribute.stream];

                var bytes = new byte[size * vertices.Length];
                if (attribute.attribute == VertexAttribute.Position)
                {
                    // The one channel that changes. Written in the format the mesh declares for it -
                    // three floats on every mesh seen so far, preflighted above rather than assumed.
                    Buffer.BlockCopy(Flatten(vertices), 0, bytes, 0, bytes.Length);
                }
                else
                {
                    for (var index = 0; index < vertices.Length; index++)
                    {
                        var from = (index * stride) + offset;
                        if (from + size > stream.Length) break;
                        Buffer.BlockCopy(stream, from, bytes, index * size, size);
                    }
                }

                attributes.Add(new ModelImporter.Model.VertexData(
                    attribute.attribute,
                    attribute.format,
                    attribute.dimension,
                    new NativeArray<byte>(bytes, Allocator.Persistent),
                    false));

            }
        }
        finally
        {
            if (read.Length > 0) read.Dispose();
        }

        // The bounds every generated mesh was missing.
        //
        // ModelImporter.Model.ToUnityMesh calls Mesh.SetSubMesh with flags 15, which includes
        // DontRecalculateBounds, and then sets mesh.bounds to the union of the descriptors' own
        // bounds. Nothing else ever computes them. A descriptor built from the three argument
        // constructor leaves that field at its default, so the mesh declared itself a zero-size box
        // at the origin - which is what every mesh this mod has written did, tower and cable alike,
        // while its vertices, indices and vertex layout were all correct. The game's own importers
        // set this field; this did not until it was read out of the dump.
        var points = ToPoints(vertices);
        var indices = new List<int>();
        var subMeshes = new List<SubMeshDescriptor>();
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var start = indices.Count;

            // A triangle whose three corners all belong to something being taken off the bridge is not
            // written. That is what taking it off means: the geometry is gone from the index buffer
            // rather than present with no area, which is a thing the renderer still has to carry and
            // the file still has to hold.
            //
            // All three corners, so that a triangle bridging the railing and what it stands on is
            // kept and the surface it belongs to is not left with a hole in it.
            var corners = mesh.GetTriangles(sub);
            for (var corner = 0; corner + 2 < corners.Length; corner += 3)
            {
                if (dropped != null
                    && corners[corner] < dropped.Length
                    && corners[corner + 1] < dropped.Length
                    && corners[corner + 2] < dropped.Length
                    && dropped[corners[corner]]
                    && dropped[corners[corner + 1]]
                    && dropped[corners[corner + 2]])
                {
                    continue;
                }

                indices.Add(corners[corner]);
                indices.Add(corners[corner + 1]);
                indices.Add(corners[corner + 2]);
            }

            var count = indices.Count - start;

            TowerWidening.ExtentOf(points, indices, start, count, out var low, out var high);
            TowerWidening.IndexRangeOf(indices, start, count, out var first, out var used);

            var centre = (low + high) * 0.5f;
            var size = high - low;
            subMeshes.Add(new SubMeshDescriptor(start, count, MeshTopology.Triangles)
            {
                bounds = new Bounds(
                    new Vector3(centre.x, centre.y, centre.z),
                    new Vector3(size.x, size.y, size.z)),
                baseVertex = 0,
                firstVertex = first,
                vertexCount = used,
            });
        }

        return new ModelImporter.Model(
            name,
            Matrix4x4.identity,
            vertices.Length,
            new NativeArray<int>(indices.ToArray(), Allocator.Persistent),
            attributes.ToArray(),
            subMeshes.ToArray(),
            -1,
            Array.Empty<ModelImporter.Model.BoneInfo>());
    }

    /// <summary>How many bytes one component of a vertex channel takes.</summary>
    private static int FormatSize(VertexAttributeFormat format)
    {
        switch (format)
        {
            case VertexAttributeFormat.Float32:
            case VertexAttributeFormat.UInt32:
            case VertexAttributeFormat.SInt32:
                return 4;
            case VertexAttributeFormat.Float16:
            case VertexAttributeFormat.UNorm16:
            case VertexAttributeFormat.SNorm16:
            case VertexAttributeFormat.UInt16:
            case VertexAttributeFormat.SInt16:
                return 2;
            case VertexAttributeFormat.UNorm8:
            case VertexAttributeFormat.SNorm8:
            case VertexAttributeFormat.UInt8:
            case VertexAttributeFormat.SInt8:
                return 1;
            default:
                return 0;
        }
    }




    private static float[] Flatten(Vector3[] values)
    {
        var result = new float[values.Length * 3];
        for (var index = 0; index < values.Length; index++)
        {
            result[index * 3] = values[index].x;
            result[(index * 3) + 1] = values[index].y;
            result[(index * 3) + 2] = values[index].z;
        }

        return result;
    }


    private static float3[] ToPoints(Vector3[] vertices)
    {
        var points = new float3[vertices.Length];
        for (var index = 0; index < vertices.Length; index++)
        {
            points[index] = new float3(vertices[index].x, vertices[index].y, vertices[index].z);
        }

        return points;
    }

    private static Vector3[] ToVectors(float3[] points)
    {
        var vertices = new Vector3[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            vertices[index] = new Vector3(points[index].x, points[index].y, points[index].z);
        }

        return vertices;
    }

    private static uint CountIndices(Mesh mesh)
    {
        var total = 0u;
        for (var sub = 0; sub < mesh.subMeshCount; sub++) total += mesh.GetIndexCount(sub);
        return total;
    }
}
