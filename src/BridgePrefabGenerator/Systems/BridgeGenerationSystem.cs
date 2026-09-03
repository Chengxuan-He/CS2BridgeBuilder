using BridgePrefabGenerator.Bridges;
using BridgePrefabGenerator.Settings;
using Colossal.Serialization.Entities;
using CS2Mods.Shared;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BridgePrefabGenerator.Systems;

/// <summary>
/// Owns everything that needs a loaded world: it discovers what can serve as a deck and which bridge
/// styles are available, publishes both to the options page, and builds the one bridge the page asks
/// for.
///
/// One bridge per run, deliberately. A bridge is a pairing - this deck on top, that one underneath,
/// in this style - and a pairing does not distribute over a list. The road exporter next door remains
/// the batch tool.
/// </summary>
public partial class BridgeGenerationSystem : GameSystemBase
{
    private const int PageRefreshCooldownFrames = 60;
    private const int PageOnScreenGraceFrames = 30;

    private PrefabSystem _prefabSystem = null!;
    private ExportSettings _settings = null!;
    private GameMode _gameMode;
    private int _waitFrames;
    private int _quietFrames;
    private int _lastCandidateCount = -1;
    private bool _settling;
    private int _pageRefreshCooldown;
    private int _framesSincePageView = int.MaxValue;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        Enabled = false;
    }

    protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
    {
        base.OnGameLoadingComplete(purpose, mode);
        _settings = ExportSettings.Load();
        _gameMode = mode;
        _waitFrames = 0;
        _quietFrames = 0;
        _lastCandidateCount = -1;
        _settling = false;
        _pageRefreshCooldown = 0;

        UiStrings text = UiStringCatalog.Current;
        var isEditor = (mode & GameMode.Editor) != 0;
        var isGame = (mode & GameMode.Game) != 0;
        if (!isEditor && !isGame)
        {
            RoadSelectionModel.PublishMessage(this, text.StateNoWorld);
            Enabled = false;
            return;
        }

        if (!isEditor && !(Mod.Setting?.AllowGameplayExport ?? false))
        {
            RoadSelectionModel.PublishMessage(this, text.StateGameplayBlocked);
            Enabled = false;
            return;
        }

        RoadSelectionModel.PublishMessage(this, text.StateScanning);
        _settling = true;
        Enabled = true;
        ModHost.Log.Info($"Waiting for prefabs to settle ({mode})");
    }

    protected override void OnDestroy()
    {
        RoadSelectionModel.ReleaseIfOwner(this, UiStringCatalog.Current.StateNoWorld);
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (_settling)
        {
            UpdateSettling();
            return;
        }

        if (_pageRefreshCooldown > 0) _pageRefreshCooldown--;

        switch (RoadSelectionModel.TakeRequest())
        {
            case ExporterRequest.Refresh:
                _pageRefreshCooldown = PageRefreshCooldownFrames;
                Refresh();
                return;
            case ExporterRequest.ExportSelected:
                _pageRefreshCooldown = PageRefreshCooldownFrames;
                ExportOne();
                return;
            case ExporterRequest.RemoveSelected:
                _pageRefreshCooldown = PageRefreshCooldownFrames;
                RemoveOne();
                return;
        }

        var viewed = RoadSelectionModel.TakePageViewed();
        if (viewed) _framesSincePageView = 0;
        else if (_framesSincePageView < int.MaxValue) _framesSincePageView++;
        if (_framesSincePageView > PageOnScreenGraceFrames) RoadSelectionModel.SetPageOnScreen(false);

        if (viewed && _pageRefreshCooldown == 0)
        {
            _pageRefreshCooldown = PageRefreshCooldownFrames;
            Refresh();
        }
    }

    private void UpdateSettling()
    {
        _waitFrames++;
        int count;
        try
        {
            count = RoadBuilderDiscovery.CountRoads(_prefabSystem);
        }
        catch (Exception exception)
        {
            _settling = false;
            Enabled = false;
            ModHost.Log.Error(exception, "Road discovery failed");
            RoadSelectionModel.PublishMessage(this, "Road discovery failed: " + exception.Message);
            return;
        }

        if (count != _lastCandidateCount)
        {
            _lastCandidateCount = count;
            _quietFrames = 0;
        }
        else
        {
            _quietFrames++;
        }

        // Unlike the road exporter this does not need Road Builder roads to exist at all - every
        // registered road and track is a usable deck - so a count of zero still settles.
        var settled = _quietFrames >= _settings.QuietFrames;
        if (!settled && _waitFrames < _settings.MaxWaitFrames) return;

        _settling = false;
        _pageRefreshCooldown = 0;
        Refresh();
    }

    /// <summary>Re-reads the decks and styles, then republishes the status text.</summary>
    private void Refresh()
    {
        IReadOnlyList<RoadBuilderRoad> roads = Array.Empty<RoadBuilderRoad>();
        var generated = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            generated = new HashSet<string>(ExportStateStore.Load().ExportNames(), StringComparer.Ordinal);
            roads = RoadBuilderDiscovery.Find(_prefabSystem, generated).Roads;
        }
        catch (Exception exception)
        {
            // Road Builder being unreadable is not fatal here: every other road is still a valid deck.
            ModHost.Log.Warn(exception, "Could not read the Road Builder roads; other decks are unaffected.");
        }

        try
        {
            BridgeStyleCatalog.Rebuild(_prefabSystem, generated);
            DeckCatalog.Rebuild(_prefabSystem, roads);
            TowerSelfTest.Run(_prefabSystem);
            AssetAnatomy.Run(_prefabSystem);
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Could not read the available bridge styles and decks");
            RoadSelectionModel.PublishMessage(this, "Could not read the bridge styles: " + exception.Message);
            return;
        }

        RoadSelectionModel.PublishMessage(this, Describe(UiStringCatalog.Current));
    }

    /// <summary>
    /// The whole status panel: what will be built, from what, in which style, and what the result will
    /// depend on. This page has no road list, so this text is the only place the player can check the
    /// pairing before pressing export.
    /// </summary>
    private static string Describe(UiStrings text)
    {
        var setting = Mod.Setting;
        var lines = new List<string>();

        var upper = DeckCatalog.Find(setting?.UpperDeckId);
        lines.Add(upper == null
            ? text.StateNoUpperDeck
            : string.Format(text.StateUpperDeck, upper.DisplayName, Metres(upper.Width)));

        var style = BridgeStyleCatalog.Resolve(setting?.BridgeStyleId);
        if (BridgeStyleCatalog.Styles.All(candidate => !candidate.IsInstalled) || style == null)
        {
            lines.Add(text.StateNoStyles);
        }
        else if (!style.IsInstalled)
        {
            lines.Add(string.Format(text.StateStyleNotInstalled, style.DisplayName));
        }
        else
        {
            lines.Add(string.Format(text.StateStyleSource, style.DisplayName, style.Source));
            if (upper != null) lines.Add(DescribeFit(text, style, upper));
        }

        var lower = DeckCatalog.Find(setting?.LowerDeckId);
        if (lower != null)
        {
            lines.Add(string.Format(
                text.StateLowerDeck,
                lower.DisplayName,
                Metres(setting?.DeckSpacing ?? 0f),
                (setting?.LowerDeckOpposite ?? false) ? text.StateDirectionOpposite : text.StateDirectionSame));
            lines.Add(text.StateDoubleDeckExperimental);
        }

        if (upper != null)
        {
            lines.Add(string.Format(
                text.StateExportName,
                BridgeNaming.BaseName(upper, lower, BridgeStyleCatalog.Resolve(setting?.BridgeStyleId))));
        }
        return string.Join("\n", lines.Where(line => line.Length > 0));
    }

    /// <summary>
    /// Which variant of the style will actually be used and how far it has to stretch. Said before the
    /// export rather than after, because this is the number that decides whether the towers will line
    /// up with the deck edges.
    /// </summary>
    private static string DescribeFit(UiStrings text, BridgeStyle style, Deck upper)
    {
        var variant = style.Nearest(upper.Width);
        if (variant == null) return string.Empty;
        return string.Format(
            text.StateStyleFit,
            variant.Name,
            Metres(variant.StructureWidth > 0f ? variant.StructureWidth : variant.Width),
            Metres(upper.Width));
    }

    private static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private void ExportOne()
    {
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var setting = Mod.Setting;

        var upper = DeckCatalog.Find(setting?.UpperDeckId);
        if (upper == null)
        {
            report.Failed("(no deck)", new InvalidOperationException(
                "No upper deck is selected. Pick the road the bridge should carry."));
            Finish(report, state, "Export bridge");
            return;
        }

        var style = BridgeStyleCatalog.Resolve(setting?.BridgeStyleId);
        if (style == null || !style.IsInstalled)
        {
            report.Failed(upper.DisplayName, new InvalidOperationException(
                style == null
                    ? "No bridge style is selected."
                    : $"The bridge style '{style.Id}' has no prefab behind it in this installation, so "
                      + "there is nothing to copy a look from. Pick another style."));
            Finish(report, state, "Export bridge");
            return;
        }

        var overwrite = setting?.OverwriteExisting ?? true;
        var lower = DeckCatalog.Find(setting?.LowerDeckId);
        var loaded = LoadedExportNames();
        // A collision only counts as one when the existing asset is not this pairing's own earlier
        // output: re-running the same pairing should replace it, not leave a second copy beside it.
        var exportName = BridgeNaming.UniqueName(
            upper, lower, style, loaded, name => overwrite && state.Contains(name),
            setting?.BridgeName);
        if (!overwrite && loaded.Contains(exportName))
        {
            report.Skipped(exportName, "the output asset already exists and overwrite is disabled");
            Finish(report, state, "Export bridge");
            return;
        }

        var options = setting?.ToBridgeOptions() ?? new BridgeOptions();
        var cloner = new PrefabGraphCloner(_prefabSystem, _settings, report, overwrite);
        var towers = new TowerFactory(_prefabSystem, report);
        var composer = new BridgeComposer(report, towers);
        var doubleDeck = new DoubleDeckComposer(report);

        try
        {
            if (upper.Prefab is not RoadPrefab upperSource)
            {
                report.Failed(exportName, new InvalidOperationException(
                    $"'{upper.DisplayName}' is a {upper.Kind} track and cannot carry a bridge."));
                Finish(report, state, "Export bridge");
                return;
            }

            // Which of the two decks the bridge is built on.
            //
            // An archetype states where its second net runs, and the two arrangements both exist in
            // the game: the V pylon hangs a train track ten metres below its road, and the plain A
            // pylon carries a second carriageway ten metres above it - "ExtradosedBridge02 Above
            // Road", which is the archetype saying so in its own name. Where the second net is above,
            // the archetype's own main net is the lower of the two decks, so the deck the player chose
            // goes in the main slot and the road they are converting is hung above it. That is the
            // archetype's arrangement rather than a correction to it, and the towers sit where they
            // were drawn without anything being moved.
            //
            // "Main" is an AuxiliaryNets ownership role, not a synonym for road. A track can be that
            // main network: it owns the bridge components and carries the road as its auxiliary. The
            // earlier RoadPrefab-only guard rejected the A pylon before its references could be
            // swapped, which is why a train lower deck could not be exported at all.
            var chosen = options.DoubleDeck ? DeckCatalog.Find(options.LowerDeckId) : null;

            // Selection is asked here as well as inside the composer. It is pure and both calls give
            // it the same width, measured from the same sections - the source road carries the
            // sections its clone will - and the same constraint, so both arrive at the same variant.
            // It has to be asked before either deck is cloned, because the answer decides which one is
            // cloned under the export name. Variants of one style disagree about this: the plain A
            // pylon hangs its second net above and its subway, train and tram variants hang theirs
            // below, so the question is about the variant and never about the style.
            var chosenWidth = BridgeComposer.WidthOf(upperSource, upper.Width);
            var stated = options.DoubleDeck
                ? style.Select(chosenWidth, forRoad: true, doubleDeck: true).Variant?.LowerDeck
                : null;
            var arrangement = DeckArrangement.For(stated?.m_Position.y ?? 0f);
            var secondNetAbove = stated != null && arrangement.MainIsChosenDeck;

            if (options.DoubleDeck && chosen == null)
            {
                report.Failed(exportName, new InvalidOperationException(
                    $"The selected second deck '{options.LowerDeckId}' is not registered any more."));
                Finish(report, state, "Export bridge");
                return;
            }

            // Start with the user's upper/lower pointers, then exchange the references as one operation
            // when the archetype places its auxiliary above. For the A pylon this makes the chosen
            // lower network the root/main prefab and the converted upper road its auxiliary prefab.
            var main = upper;
            Deck? auxiliary = chosen;
            if (options.DoubleDeck && chosen != null)
            {
                var roles = arrangement.Arrange(upper, chosen);
                main = roles.Main;
                auxiliary = roles.Auxiliary;
            }

            var clone = CloneDeck(cloner, main, exportName, report);
            if (clone == null)
            {
                Finish(report, state, "Export bridge");
                return;
            }
            var variant = composer.Apply(
                clone, style, upper.Width, options, measure: upperSource);
            if (variant != null)
            {
                AttachSecondDeck(
                    clone, auxiliary, secondNetAbove, exportName, cloner, doubleDeck, options, variant,
                    report);
                DescribeResult(clone, exportName, report);

                // Generated towers are nodes like any other dependency: written before the bridge
                // that references them, and registered with it. Leaving them out is what made the
                // first generated tower a dangling reference in a saved asset.
                var nodes = cloner.Nodes
                    .Concat(towers.Created.Select(prefab => new PrefabCloneNode(prefab, prefab, false, true, null)))
                    .ToList();
                report.SavedDependencies = new PrefabAssetWriter().Save(nodes);
                report.Exported(exportName);
                state.Record(exportName, Fingerprint(upper));
                WorldRegistration.Publish(World, _prefabSystem, nodes, report);
            }
        }
        catch (Exception exception)
        {
            report.Failed(exportName, exception);
        }

        Finish(report, state, "Export bridge");
    }

    /// <summary>
    /// Clones the chosen deck into a standalone prefab. A Road Builder road carries its configuration
    /// with it - the authored speed limit, its thumbnail - and an already registered road does not, so
    /// only the first needs those applied.
    /// </summary>
    private NetGeometryPrefab? CloneDeck(
        PrefabGraphCloner cloner, Deck deck, string exportName, ExportReport report)
    {
        if (deck.Prefab is RoadPrefab roadSource)
        {
            var icon = deck.Road != null
                ? RoadBuilderIconExporter.Preserve(deck.Road, report, Mod.Setting?.EmbedIcons ?? false)
                : string.Empty;

            var road = cloner.CloneRoad(roadSource, exportName, icon);
            if (deck.Road != null) SpeedLimitFix.Apply(road, deck.Road, report);
            return road;
        }

        if (deck.Prefab is NetPrefab netSource)
            return (NetGeometryPrefab)cloner.CloneNet(netSource, exportName);

        report.Defect(
            $"'{deck.DisplayName}' is not a network prefab and cannot own a double-deck bridge. "
            + "The current bridge export was stopped without publishing a partial prefab.");
        return null;
    }

    /// <summary>
    /// Resolves the auxiliary network and attaches it on the side declared by the archetype.
    ///
    /// An already registered net - a track, or another road the player picked - is referenced as it
    /// is. The one case that needs work is picking the same net for both decks: pointing the bridge at
    /// itself would make a prefab that is its own auxiliary, so it is cloned a second time under its
    /// own name and stripped of the things a carried deck must not have.
    /// </summary>
    private void AttachSecondDeck(
        NetGeometryPrefab main,
        Deck? auxiliary,
        bool above,
        string exportName,
        PrefabGraphCloner cloner,
        DoubleDeckComposer doubleDeck,
        BridgeOptions options,
        BridgeStyleVariant variant,
        ExportReport report)
    {
        if (!options.DoubleDeck || options.LowerDeckId == null) return;

        // The archetype the bridge was built from is the double deck version of its style, so it
        // already states where the second deck runs and which way. The composer refuses to build at
        // all when the style has no such version, so reaching here without one is a contradiction.
        var arrangement = variant.LowerDeck;
        if (arrangement == null)
        {
            report.Warning(
                $"'{exportName}' was exported without its second deck: '{variant.Name}' states no "
                + "arrangement for one.");
            return;
        }

        // A double-deck bridge's node-link rule belongs to the prototype component that owns the
        // auxiliary entry. ExtradosedBridge01 and the expansion pack's double-deck suspension bridge
        // both set this to true. Read it from the selected variant rather than recreating the same bit
        // from memory so the generated network follows the prototype at its nodes as well as in deck
        // position.
        var prototypeAuxiliary = variant.Donor.GetComponent<AuxiliaryNets>();
        var linkEndOffsets = prototypeAuxiliary?.m_LinkEndOffsets ?? false;
        if (prototypeAuxiliary == null)
        {
            report.Warning(
                $"'{exportName}' has a second-deck entry but its prototype has no AuxiliaryNets "
                + "component, so its node end offsets cannot be linked from the prototype.");
        }

        // The auxiliary is whichever pointer did not become the main network. Normally that is the
        // selected lower deck; for the A pylon the pointers were exchanged and it is the converted
        // upper road.
        var deck = auxiliary;
        if (deck == null)
        {
            report.Warning(
                $"'{exportName}' was exported without its second deck: the chosen net "
                + $"'{options.LowerDeckId}' is not registered any more.");
            return;
        }

        // Every auxiliary deck is cloned, whatever it is and whether or not it matches the main one.
        //
        // It has to be changed - an auxiliary carries no independent pillars, because the main
        // network owns the structure - and the thing it is changed from is a
        // registered prefab shared with everything else built from it. Taking the pillars off a track
        // in place would take them off every other track in the world, which is a worse fault than the
        // one being fixed, and that is why this used to be reported instead of fixed.
        //
        // The pack does the same: its double deck bridges name a separate auxiliary prefab carrying
        // no structure of its own, rather than pointing at the shared road or track.
        if (deck.Prefab is not NetPrefab source)
        {
            report.Warning($"'{exportName}': the auxiliary deck could not be built from '{deck.DisplayName}'.");
            return;
        }

        var auxiliaryName = BridgeNaming.CarriedDeckName(exportName, above);
        var auxiliaryClone = source is RoadPrefab road
            ? cloner.CloneRoad(road, auxiliaryName, string.Empty)
            : cloner.CloneNet(source, auxiliaryName);

        var pillars = DoubleDeckComposer.PrepareDeck(auxiliaryClone, main);
        if (pillars > 0)
        {
            report.Note(
                $"{auxiliaryName}: {pillars} pillar(s) removed. The main network owns the bridge "
                + "structure, so an independent second set would conflict with it.");
        }

        // Two post conditions, checked rather than assumed. Each is a fault nothing else reports: a
        // lower deck on pillars of its own runs them to the ground beside the structure already
        // holding it up, and a lower deck with no bridge behaviour is held to an ordinary road's edge
        // length and reports "distance too long" on every span of the bridge carrying it.
        if (auxiliaryClone.GetComponent<Bridge>() == null)
        {
            report.Defect(
                $"'{auxiliaryName}' carries no bridge behaviour, so its edges are held to an ordinary "
                + "network's length while its main bridge spans further.");
        }

        var left = DoubleDeckComposer.PillarsOn(auxiliaryClone);
        if (left > 0)
        {
            report.Defect(
                $"'{auxiliaryName}' still carries {left} pillar(s) after being prepared. The main "
                + "network already owns the structure for both decks.");
        }

        doubleDeck.Apply(
            main, auxiliaryClone, $"'{auxiliaryName}', from {deck.DisplayName}", arrangement,
            linkEndOffsets, options.LowerDeckOpposite);
    }


    /// <summary>
    /// Writes what the conversion produced into the report. For a bridge the counts that matter are
    /// the ones that separate "the deck came out wrong" from "the style did not attach": sections and
    /// components as before, plus how much structure ended up above and below the deck.
    /// </summary>
    private static void DescribeResult(NetGeometryPrefab clone, string name, ExportReport report)
    {
        try
        {
            var sections = clone.m_Sections?.Length ?? 0;
            var missingSections = clone.m_Sections?.Count(section => section.m_Section == null) ?? 0;
            var overhead = clone.GetComponent<OverheadNetSections>()?.m_Sections?.Length ?? 0;
            var subObjects = clone.GetComponent<NetSubObjects>()?.m_SubObjects?.Length ?? 0;
            var auxiliary = clone.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets?.Length ?? 0;

            report.Note(
                $"{name}: {clone.components.Count} components, {sections} sections "
                + $"({missingSections} missing), {overhead} overhead section(s), {subObjects} sub object(s), "
                + $"{auxiliary} auxiliary net(s), speed "
                + (clone is RoadPrefab road ? road.m_SpeedLimit.ToString(CultureInfo.InvariantCulture) : "n/a"));
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, $"Could not summarise the exported bridge '{name}'");
        }
    }

    /// <summary>
    /// What the export was made from and with. A deck whose road is untouched still has to be built
    /// again after the style or the deck spacing changes, so all of it counts.
    /// </summary>
    private static string Fingerprint(Deck upper)
    {
        var setting = Mod.Setting;
        return string.Join("|", new[]
        {
            upper.Road?.Fingerprint ?? upper.Id,
            setting?.BridgeStyleId ?? string.Empty,
            setting?.BuildStyleOverride ?? string.Empty,
            setting?.LowerDeckId ?? string.Empty,
            (setting?.LowerDeckOpposite ?? false) ? "opp" : "same",
            (setting?.DeckSpacing ?? 0f).ToString("0.##", CultureInfo.InvariantCulture),
        });
    }

    private HashSet<string> LoadedExportNames()
    {
        return new HashSet<string>(
            PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .Where(prefab => prefab.asset != null
                    && !prefab.isReadOnly)
                .Select(prefab => prefab.name),
            StringComparer.Ordinal);
    }

    private void RemoveOne()
    {
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var upper = DeckCatalog.Find(Mod.Setting?.UpperDeckId);
        if (upper == null)
        {
            report.Failed("(no deck)", new InvalidOperationException(
                "No upper deck is selected, so there is no exported bridge to remove."));
            Finish(report, state, "Remove bridge");
            return;
        }

        // The remover works from a Road Builder road, which most decks are not, so removal goes by the
        // name the export would have used. That is the same name in either case.
        var removed = RemoveByName(
            BridgeNaming.BaseName(upper, DeckCatalog.Find(Mod.Setting?.LowerDeckId), BridgeStyleCatalog.Resolve(Mod.Setting?.BridgeStyleId)),
            state,
            report);
        if (removed.Count > 0)
        {
            if (Mod.Setting?.RemoveUnusedDependencies ?? true)
                new PrefabAssetRemover(_prefabSystem, _settings, report)
                    .Remove(Array.Empty<RoadBuilderRoad>(), state, true);

            report.Warning("The removed prefabs stay registered in the running session. Restart the game to get rid of them.");
        }

        Finish(report, state, "Remove bridge");
    }

    private IReadOnlyList<string> RemoveByName(string exportName, ExportStateStore state, ExportReport report)
    {
        var removed = new List<string>();
        var removedRoots = new HashSet<PrefabBase>(ReferenceEqualityComparer<PrefabBase>.Instance);
        var dependencyCandidates = new HashSet<PrefabBase>(ReferenceEqualityComparer<PrefabBase>.Instance);
        // The lower deck, when there is one, is a second asset next to the bridge and has to go too.
        foreach (var name in new[]
                 {
                     exportName,
                     BridgeNaming.LowerDeckName(exportName),
                     BridgeNaming.CarriedDeckName(exportName, above: true),
                 })
        {
            var prefab = PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate => candidate.asset != null
                    && !candidate.isReadOnly
                    && string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (prefab == null)
            {
                if (name == exportName) report.Skipped(name, "no exported asset with that name is loaded");
                continue;
            }

            try
            {
                // Capture the dependency graph while the root still exists. Deleting only the road
                // left its generated tower, RenderPrefabs and LODs in ImportedData. A later offline
                // cleanup removed their Geometry files, so those orphan prefabs loaded next launch
                // with a valid material batch but a null mesh.
                PrefabReferenceWalker.CollectInto(prefab, dependencyCandidates);
                prefab.asset!.Delete();
            }
            catch (Exception exception)
            {
                report.Failed(name, exception);
                continue;
            }

            state.Remove(name);
            RoadBuilderIconExporter.Discard(name);
            report.Removed(name);
            removed.Add(name);
            removedRoots.Add(prefab);
        }

        if (removed.Count > 0 && (Mod.Setting?.RemoveUnusedDependencies ?? true))
            RemoveBridgeDependencies(dependencyCandidates, removedRoots, state, report);

        return removed;
    }

    /// <summary>
    /// Deletes generated prefab dependencies which belonged only to the removed bridge.
    /// Geometry assets are deliberately retained until restart/offline cleanup: the corresponding
    /// RenderPrefabs remain registered in the running world, and deleting their Geometry immediately
    /// would recreate the null-mesh renderer failure during the current session.
    /// </summary>
    private void RemoveBridgeDependencies(
        IReadOnlyCollection<PrefabBase> candidates,
        HashSet<PrefabBase> removedRoots,
        ExportStateStore state,
        ExportReport report)
    {
        if (candidates.Count == 0) return;

        var referenced = new HashSet<PrefabBase>(ReferenceEqualityComparer<PrefabBase>.Instance);
        foreach (var survivorName in state.ExportNames())
        {
            var survivor = PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate => candidate.asset != null
                    && !candidate.isReadOnly
                    && string.Equals(candidate.name, survivorName, StringComparison.Ordinal));
            if (survivor == null)
            {
                report.Warning(
                    $"Kept generated bridge dependencies: surviving export '{survivorName}' is "
                    + "not loaded, so shared dependencies cannot be identified safely.");
                return;
            }

            PrefabReferenceWalker.CollectInto(survivor, referenced);
        }

        foreach (var candidate in candidates)
        {
            if (removedRoots.Contains(candidate)
                || referenced.Contains(candidate)
                || candidate.asset == null
                || candidate.isReadOnly
                || candidate.isBuiltin)
                continue;

            try
            {
                candidate.asset.Delete();
                report.RemovedDependency(candidate.name);
            }
            catch (Exception exception)
            {
                report.Warning(
                    $"Could not delete unused bridge dependency '{candidate.name}': "
                    + exception.Message);
            }
        }
    }

    private void Finish(ExportReport report, ExportStateStore state, string operation)
    {
        try
        {
            state.Save();
            report.Save(_gameMode.ToString(), operation);
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Unable to write exporter state/report");
        }

        ModHost.Log.Info(
            $"{operation}: {report.ExportedRoads} exported, {report.RemovedRoads} removed, "
            + $"{report.SkippedRoads} skipped, {report.FailedRoads} failed");

        // Raised at error level on purpose: this logger shows errors in the game, so a failed export
        // is not something the player has to go looking for in a file.
        if (report.FailedRoads > 0)
            ModHost.Log.Error(
                $"{operation} failed. Details in ModsData\\BridgePrefabGenerator\\last-export-report.txt");

        UiStrings text = UiStringCatalog.Current;
        var summary = string.Format(
            text.OperationSummary,
            report.ExportedRoads,
            report.RemovedRoads,
            report.SkippedRoads,
            report.FailedRoads);
        RoadSelectionModel.PublishOperationResult(summary);

        Mod.ShowMessage(text.Title, summary + "\n" + text.StateReportHint);
        Refresh();
    }
}
