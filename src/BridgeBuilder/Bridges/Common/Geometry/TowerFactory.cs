





using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;





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
internal sealed partial class TowerFactory
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
        Mod.Log.Info(string.Format(CultureInfo.InvariantCulture,
            "BridgeWidth factory owner='{0}', style='{1}', tower='{2}', sourceRoad={3:R}, targetDeck={4:R}.",
            _bridgeName, styleId, sourceTowerName, sourceRoadWidth, deckWidth));

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

}
