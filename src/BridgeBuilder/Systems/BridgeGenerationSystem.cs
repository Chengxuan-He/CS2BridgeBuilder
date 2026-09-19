using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.UI;
using Colossal.Serialization.Entities;
using CS2Mods.Shared;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Discovery;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

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
    private int _previewRevision = -1;
    private int _previewFailedRevision = -1;
    private BridgePreviewSession? _previewSession;
    private BridgePreviewDrawList? _previewDraws;
    private BridgePreviewRenderer? _previewRenderer;
    private bool _previewReleasePending;
    private BridgeInstanceRemoval? _pendingRemoval;
    private BridgeRegistration? _removingRegistration;
    private bool _activationLocked;

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

        RoadSelectionModel.PublishMessage(this, RuntimeUiText.Get("Scanning"));
        _settling = true;
        Enabled = true;
        ModHost.Log.Info($"Waiting for prefabs to settle ({mode})");
    }

    protected override void OnDestroy()
    {
        ClearPreview();
        BridgePreviewState.Clear();
        RoadSelectionModel.ReleaseIfOwner(this, UiStringCatalog.Current.StateNoWorld);
        base.OnDestroy();
    }

    protected override void OnStopRunning()
    {
        _pendingRemoval = null;
        _removingRegistration = null;
        ClearPreview();
        BridgePreviewState.Clear();
        base.OnStopRunning();
    }

    protected override void OnUpdate()
    {
        // A queued update may need the next outer PrefabSystem pass. Do not process another
        // create/delete request until its scheduled publication callback has finished.
        if (World.GetOrCreateSystemManaged<BridgePublicationSystem>().IsPending) return;
        if (_pendingRemoval != null)
        {
            try
            {
                if (_pendingRemoval.IsComplete(EntityManager)) CompleteRuntimeDeletion();
            }
            catch (Exception exception)
            {
                _pendingRemoval = null;
                _removingRegistration = null;
                Mod.Log.Warn(exception, "Could not finish bridge deletion safely");
                BridgeRuntimeRequests.Complete("DeleteIncomplete");
            }
            return;
        }
        if (_previewReleasePending) ClearPreview();
        if (_previewRevision != BridgePreviewState.Revision)
        {
            ClearPreview();
            if (!_settling)
            {
                _previewRevision = BridgePreviewState.Revision;
                if (BridgePreviewState.Selection != null) BuildPreview(BridgePreviewState.Selection, _previewRevision);
            }
        }
        try
        {
            _previewRenderer?.Tick();
        }
        catch (Exception)
        {
            if (BridgePreviewState.Selection != null)
                FailPreview(_previewRevision, "RenderFailed");
        }
        if (_settling)
        {
            UpdateSettling();
            return;
        }

        if (_pageRefreshCooldown > 0) _pageRefreshCooldown--;

        if (BridgeRuntimeRequests.TryTake(out var runtimeRequest) && runtimeRequest != null)
        {
            HandleRuntimeRequest(runtimeRequest);
            return;
        }

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
            count = RoadBuilderCompatibility.IsAvailable ? RoadBuilderDiscovery.CountRoads(_prefabSystem) : 0;
        }
        catch (Exception exception)
        {
            count = 0;
            ModHost.Log.Warn(exception, "Optional Road Builder discovery failed; other networks remain available.");
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
            if (RoadBuilderCompatibility.IsAvailable)
                roads = RoadBuilderDiscovery.Find(_prefabSystem, generated).Roads;
        }
        catch (Exception exception)
        {
            // Road Builder being unreadable is not fatal here: every other road is still a valid deck.
            ModHost.Log.Warn(exception, "Could not read the Road Builder roads; other decks are unaffected.");
        }

        try
        {
            BridgeAssetPack.RefreshExisting(_prefabSystem, EntityManager,
                generated.Concat(BridgeRegistrationStore.Load().Select(entry => entry.PrefabName)));
            BridgeStyleCatalog.Rebuild(_prefabSystem, generated);
            DeckCatalog.Rebuild(_prefabSystem, roads);
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Could not read the available bridge styles and decks");
            RoadSelectionModel.PublishMessage(this, "Could not read the bridge styles: " + exception.Message);
            return;
        }

        RoadSelectionModel.PublishMessage(this, Describe(UiStringCatalog.Current));
        BridgeBuilderUISystem.RequestRefresh();
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
        var variant = style.Nearest(upper.Width, upper.IsRoad);
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
        var report = new ExportReport(logIssues: false);
        var setting = Mod.Setting;

        if ((_gameMode & GameMode.Editor) == 0
            && (_gameMode & GameMode.Game) != 0
            && !(setting?.AllowGameplayExport ?? false))
        {
            RoadSelectionModel.PublishMessage(this, UiStringCatalog.Current.StateGameplayBlocked);
            return;
        }

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
        if (!TryBuildBridge(upper, lower, style, exportName, options, overwrite, report,
            onPublished: ready =>
            {
                if (ready) state.Record(exportName, Fingerprint(upper));
                Finish(report, state, "Export bridge");
            }))
            Finish(report, state, "Export bridge");
    }

    /// <summary>
    /// The one bridge construction path used by both the legacy options page and the runtime UI.
    /// A true result means that publication has been queued (or a private preview was built).
    /// Permanent registration and activation happen in onPublished, after native initialization.
    /// </summary>
    private bool TryBuildBridge(
        Deck upper,
        Deck? chosen,
        BridgeStyle style,
        string exportName,
        BridgeOptions options,
        bool overwrite,
        ExportReport report,
        BridgePreviewSession? preview = null,
        Action<bool>? onPublished = null)
    {
        var unsupported = BridgeStyleDefinitions.GenerationUnsupportedReason(style.Id, options.DoubleDeck);
        if (unsupported != null)
        {
            report.Failed(exportName, new NotSupportedException(
                $"'{style.Id}' cannot be generated: {unsupported}."));
            return false;
        }
        var failuresBefore = report.FailedRoads;
        var cloner = new PrefabGraphCloner(_prefabSystem, _settings, report, overwrite);
        var towers = new TowerFactory(_prefabSystem, report, preview?.Geometry);
        var composer = new BridgeComposer(report, towers);
        var doubleDeck = new DoubleDeckComposer(report);
        var economy = new BridgeEconomy();

        try
        {
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
            // Selection is asked here as well as inside the composer. It is pure and both calls give
            // it the same width, measured from the same sections - the source road carries the
            // sections its clone will - and the same constraint, so both arrive at the same variant.
            // It has to be asked before either deck is cloned, because the answer decides which one is
            // cloned under the export name. Variants of one style disagree about this: the plain A
            // pylon hangs its second net above and its subway, train and tram variants hang theirs
            // below, so the question is about the variant and never about the style.
            var chosenWidth = BridgeComposer.WidthOf(upper.Prefab, upper.Width);
            var stated = options.DoubleDeck
                ? style.Select(chosenWidth, upper.IsRoad, doubleDeck: true).Variant?.LowerDeck
                : null;
            var arrangement = DeckArrangement.For(stated?.m_Position.y ?? 0f);
            var secondNetAbove = stated != null && arrangement.MainIsChosenDeck;

            if (options.DoubleDeck && chosen == null)
            {
                report.Failed(exportName, new InvalidOperationException(
                    $"The selected second deck '{options.LowerDeckId}' is not registered any more."));
                return false;
            }

            var main = upper;
            Deck? auxiliary = chosen;
            if (options.DoubleDeck && chosen != null)
            {
                var roles = arrangement.Arrange(upper, chosen);
                main = roles.Main;
                auxiliary = roles.Auxiliary;
            }

            var clone = CloneDeck(cloner, main, exportName, report);
            if (clone == null) return false;

            // Size from the archetype's root ownership role. For Suspension and ExtradosedBridge01
            // that is the converted upper road; for ExtradosedBridge02 the auxiliary is above, so
            // the chosen lower road/track is the root and supplies the width reference.
            var variant = composer.Apply(
                clone, style, main.Width, options, measure: main.Prefab);
            if (variant == null) return false;

            ApplyPrototypeIcon(clone, variant, report);
            AttachSecondDeck(
                clone, auxiliary, secondNetAbove, exportName, cloner, doubleDeck, options,
                style.Id, variant, report);
            // Extra auxiliary networks inherited from a road may still be shared native assets.
            // Never edit their prices or unlock components as if the bridge owned them.
            var ownedNets = new HashSet<PrefabBase>(cloner.Nodes.Where(node => node.NeedsSave)
                .Select(node => node.Target));
            if ((clone.GetComponent<AuxiliaryNets>()?.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
                .Any(entry => entry?.m_Prefab == null || !ownedNets.Contains(entry.m_Prefab)))
            {
                report.Failed(exportName, new InvalidOperationException(
                    "The selected network contains an auxiliary prefab not owned by this bridge."));
                return false;
            }
            if (!BridgeUnlockPolicy.Apply(clone, variant, report)) return false;
            // Pricing creates private native section/piece assets only for permanent exports.
            // It never changes rendered geometry, so previews need no pricing graph copies.
            if (preview == null && !economy.Apply(clone, variant, upper.Prefab,
                options.DoubleDeck ? chosen?.Prefab : null, report)) return false;
            DescribeResult(clone, exportName, report);

            var nodes = cloner.Nodes
                .Concat(economy.Nodes)
                .Concat(towers.Created.Select(prefab =>
                    new PrefabCloneNode(prefab, prefab, false, true, null)))
                .ToList();
            if (preview != null)
            {
                // Preview and permanent generation share composition, not identity
                // or publication. A later Create request always runs this method anew.
                if (report.FailedRoads != failuresBefore) return false;
                preview.SetResult(clone, variant);
                return true;
            }
            if (report.FailedRoads != failuresBefore) return false;
            var pack = BridgeAssetPack.Ensure(_prefabSystem);
            if (pack == null)
            {
                report.Failed(exportName, new InvalidOperationException("BridgeBuilder asset pack is unavailable."));
                return false;
            }
            foreach (var node in nodes.Where(node => node.NeedsSave))
                if (node.Target is NetGeometryPrefab network) BridgeAssetPack.Assign(network, pack);
            report.SavedDependencies = new PrefabAssetWriter().Save(nodes);
            return World.GetOrCreateSystemManaged<BridgePublicationSystem>().Publish(nodes, report, ready =>
            {
                if (ready) report.Exported(exportName);
                onPublished?.Invoke(ready);
            });
        }
        catch (Exception exception)
        {
            report.Failed(exportName, exception);
            return false;
        }
        finally
        {
            // Capture even a partial graph so failed previews can be completely
            // discarded without touching any shared archetype or source road.
            preview?.Adopt(cloner.Nodes, towers.Created);
        }
    }

    /// <summary>
    /// Clones the chosen deck into a standalone prefab. A Road Builder road still contributes its
    /// authored speed limit, but never its road thumbnail: once composition chooses an archetype, the
    /// generated asset receives that bridge prototype's icon.
    /// </summary>
    private NetGeometryPrefab? CloneDeck(
        PrefabGraphCloner cloner, Deck deck, string exportName, ExportReport report)
    {
        if (deck.Prefab is RoadPrefab roadSource)
        {
            var road = cloner.CloneRoad(roadSource, exportName, string.Empty);
            if (!IsPrivateDeck(roadSource, road, report)) return null;
            if (deck.Road != null) SpeedLimitFix.Apply(road, deck.Road, report);
            return road;
        }

        if (deck.Prefab is NetPrefab netSource)
        {
            var clone = (NetGeometryPrefab)cloner.CloneNet(netSource, exportName);
            return IsPrivateDeck(netSource, clone, report) ? clone : null;
        }

        report.Defect(
            $"'{deck.DisplayName}' is not a network prefab and cannot own a double-deck bridge. "
            + "The current bridge export was stopped without publishing a partial prefab.");
        return null;
    }

    private static bool IsPrivateDeck(NetPrefab source, NetPrefab clone, ExportReport report)
    {
        if (!ReferenceEquals(source, clone)) return true;
        report.Defect($"Network '{source.name}' could not be cloned into an owned native prefab. " +
            "Generation stopped before modifying the shared source network.");
        return false;
    }

    /// <summary>
    /// The generated bridge is presented as the selected bridge design, not as another copy of its
    /// carried road. The icon URI remains owned by the archetype's content; catalogue selection has
    /// already rejected that donor when its DLC or mod prerequisite is unavailable.
    /// </summary>
    private static void ApplyPrototypeIcon(
        NetGeometryPrefab target, BridgeStyleVariant variant, ExportReport report)
    {
        var targetUi = target.components.OfType<UIObject>().FirstOrDefault();
        if (targetUi == null)
        {
            report.Warning(
                $"'{target.name}' has no UIObject, so the icon from bridge prototype "
                + $"'{variant.Name}' could not be assigned.");
            return;
        }

        var prototypeUi = variant.Donor.components.OfType<UIObject>().FirstOrDefault();
        targetUi.m_Icon = prototypeUi?.m_Icon ?? string.Empty;
        if (string.IsNullOrWhiteSpace(targetUi.m_Icon))
            report.Warning($"Bridge prototype '{variant.Name}' does not expose a UI icon.");
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
        string styleId,
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
        if (deck.Prefab is not NetGeometryPrefab source)
        {
            report.Warning($"'{exportName}': the auxiliary deck could not be built from '{deck.DisplayName}'.");
            return;
        }

        var auxiliaryName = BridgeNaming.CarriedDeckName(exportName, above);
        var auxiliaryClone = CloneDeck(cloner, deck, auxiliaryName, report);
        if (auxiliaryClone == null) return;

        if (arrangement.m_Prefab is not NetGeometryPrefab prototypeAuxiliaryDeck)
        {
            report.Defect(
                $"'{exportName}' was exported without its second deck: prototype '{variant.Name}' "
                + "does not reference a usable auxiliary network whose node seam behavior can be "
                + "copied.");
            return;
        }

        var pillars = DoubleDeckComposer.PrepareDeck(
            auxiliaryClone, main, prototypeAuxiliaryDeck);
        if (pillars > 0)
        {
            report.Note(
                $"{auxiliaryName}: {pillars} pillar(s) removed. The main network owns the bridge "
                + "structure, so an independent second set would conflict with it.");
        }

        var seamSource = DoubleDeckComposer.CopyCompatibleSeamBehavior(
            auxiliaryClone, prototypeAuxiliaryDeck, copyAggregate: true)
            ? $"copied from transport-compatible prototype '{prototypeAuxiliaryDeck.name}'"
            : $"preserved from selected {deck.Kind} deck because prototype "
                + $"'{prototypeAuxiliaryDeck.name}' carries another transport type";

        // ExtradosedBridge01's lower network belongs to the same named bridge as its root deck. Its
        // prototype auxiliary is a train track, and blindly retaining that aggregate makes the lower
        // road receive an ordinary street/road/track name. The style table records the exception; the
        // generated deck takes the already-copied aggregate from the main bridge, without inferring
        // anything from a generated name or from geometry.
        if (BridgeStyleDefinitions.CarriedDeckUsesBridgeAggregate(styleId)
            && main.m_AggregateType != null)
        {
            auxiliaryClone.m_AggregateType = main.m_AggregateType;
            seamSource += $"; aggregate copied from main bridge '{main.m_AggregateType.name}'";
        }

        report.Note(
            $"{auxiliaryName}: auxiliary seam behavior {seamSource} - "
            + $"{auxiliaryClone.m_EdgeStates?.Length ?? 0} edge rule(s), "
            + $"{auxiliaryClone.m_NodeStates?.Length ?? 0} node rule(s), aggregate "
            + $"'{auxiliaryClone.m_AggregateType?.name ?? "none"}'.");

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
        catch (Exception)
        {
            // Generation diagnostics are silent; retain the external API exception boundary.
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

    private void HandleRuntimeRequest(BridgeRuntimeRequest request)
    {
        switch (request.Action)
        {
            case BridgeRuntimeAction.Refresh:
                Refresh();
                BridgeRuntimeRequests.Complete(string.Empty);
                return;
            case BridgeRuntimeAction.Create:
                CreateRuntimeBridge(request);
                return;
            case BridgeRuntimeAction.Activate:
                ActivateRuntimeBridge(request.PrefabName);
                return;
            case BridgeRuntimeAction.Rename:
                RenameRuntimeBridge(request.PrefabName, request.RegistrationName);
                return;
            case BridgeRuntimeAction.Delete:
                DeleteRuntimeBridge(request.PrefabName);
                return;
        }
    }

    private void ClearPreview()
    {
        _previewReleasePending = false;
        _previewRenderer?.Dispose();
        _previewRenderer = null;
        _previewDraws?.Dispose();
        _previewDraws = null;
        _previewSession?.Dispose();
        _previewSession = null;
    }

    private void BuildPreview(BridgeRuntimeRequest request, int revision)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.PrefabName))
            {
                var registration = BridgeRegistrationStore.Find(request.PrefabName);
                var existing = registration == null ? null : PrefabCatalog.GetAll(_prefabSystem)
                    .OfType<NetGeometryPrefab>()
                    .FirstOrDefault(prefab => string.Equals(prefab.name, request.PrefabName, StringComparison.Ordinal));
                if (existing == null)
                {
                    FailPreview(revision, "PreviewInvalid");
                    return;
                }
                // Borrow the existing prefab graph; do not rebuild its original recipe.
                _previewSession = new BridgePreviewSession(existing);
            }
            else
            {
                var upper = DeckCatalog.Find(request.UpperDeckId);
                var lower = string.IsNullOrEmpty(request.LowerDeckId) ? null : DeckCatalog.Find(request.LowerDeckId);
                var style = BridgeStyleCatalog.Find(request.StyleId);
                var doubleDeck = !string.IsNullOrEmpty(request.LowerDeckId);
                // DeckCatalog validates network types; both tracks and roads can be
                // primary decks. IsRoad only guides archetype selection in the composer.
                if (upper == null || style == null || !style.IsInstalled ||
                    (doubleDeck && lower == null) || !style.Variants.Any(v => v.IsDoubleDeck == doubleDeck))
                {
                    FailPreview(revision, "PreviewInvalid");
                    return;
                }
                _previewSession = new BridgePreviewSession();
                var options = new BridgeOptions
                {
                    DoubleDeck = doubleDeck, LowerDeckId = lower?.Id, LowerDeckOpposite = request.LowerDeckOpposite
                };
                if (!TryBuildBridge(upper, lower, style, _previewSession.Name, options, false,
                        new ExportReport(logIssues: false), _previewSession))
                {
                    FailPreview(revision, "PreviewBuildFailed");
                    ClearPreview();
                    return;
                }
            }
            _previewDraws = new BridgePreviewDrawList(_previewSession);
            if (!BridgePreviewScene.Build(_previewSession, _previewDraws))
            {
                FailPreview(revision, "PreviewAssemblyFailed");
                ClearPreview();
                return;
            }
            // Own the renderer before native allocation starts, so ClearPreview
            // also releases a partially initialized scene/camera after an error.
            _previewRenderer = new BridgePreviewRenderer();
            _previewRenderer.Initialize(_previewSession.Name, _previewDraws);
            _previewRenderer.Start((image, error) =>
            {
                if (revision != BridgePreviewState.Revision || BridgePreviewState.Selection == null) return;
                if (error.Length != 0 || string.IsNullOrEmpty(image))
                    FailPreview(revision, error.Length != 0 ? error : "RenderEmpty");
                else
                    BridgePreviewState.Publish(revision, image, "PreviewReady");
            });
        }
        catch (Exception)
        {
            FailPreview(revision, "PreviewFailed");
            ClearPreview();
        }
    }

    private void FailPreview(int revision, string stage)
    {
        // A cancelled/superseded selection is not a model generation failure.
        // Complete each failed request once without emitting a game error.
        if (revision != BridgePreviewState.Revision || BridgePreviewState.Selection == null ||
            _previewFailedRevision == revision) return;
        _previewFailedRevision = revision;
        // Never destroy a camera inside its rendering callback. Release the
        // isolated preview on the next update; no city/saved assets are touched.
        _previewReleasePending = true;
        BridgePreviewState.Publish(revision, string.Empty, stage);
    }

    private void CreateRuntimeBridge(BridgeRuntimeRequest request)
    {
        var state = ExportStateStore.Load();
        var report = new ExportReport(logIssues: false);
        var upper = DeckCatalog.Find(request.UpperDeckId);
        var lower = string.IsNullOrEmpty(request.LowerDeckId)
            ? null
            : DeckCatalog.Find(request.LowerDeckId);
        var style = BridgeStyleCatalog.Find(request.StyleId);

        if (upper == null)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected upper network is no longer registered."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("UpperUnavailable");
            return;
        }

        if (style == null || !style.IsInstalled)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected bridge prototype is no longer installed."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("StyleUnavailable");
            return;
        }

        var doubleDeck = !string.IsNullOrEmpty(request.LowerDeckId);
        if (doubleDeck && lower == null)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "The selected lower network is no longer registered."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("LowerUnavailable");
            return;
        }

        if (!style.Variants.Any(variant => variant.IsDoubleDeck == doubleDeck))
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                doubleDeck
                    ? "The selected bridge style has no double-deck prototype."
                    : "The selected bridge style has no single-deck prototype."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete(doubleDeck
                ? "NoDoublePrototype"
                : "NoSinglePrototype");
            return;
        }

        var loaded = LoadedExportNames();
        var prefabName = string.Empty;
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var candidate = BridgeRegistration.NewPrefabName();
            if (loaded.Contains(candidate) || state.Contains(candidate)
                || BridgeRegistrationStore.Find(candidate) != null)
                continue;
            prefabName = candidate;
            break;
        }

        if (prefabName.Length == 0)
        {
            report.Failed("(runtime bridge)", new InvalidOperationException(
                "A unique bridge UUID could not be allocated."));
            Finish(report, state, "Create runtime bridge", showMessage: false);
            BridgeRuntimeRequests.Complete("UuidFailed");
            return;
        }

        var registrationName = string.IsNullOrWhiteSpace(request.RegistrationName)
            ? BridgeNaming.BaseName(upper, lower, style)
            : request.RegistrationName.Trim();
        var options = new BridgeOptions
        {
            DoubleDeck = doubleDeck,
            LowerDeckId = lower?.Id,
            LowerDeckOpposite = request.LowerDeckOpposite,
        };

        if (!TryBuildBridge(
            upper, lower, style, prefabName, options, overwrite: false, report,
            onPublished: ready =>
            {
                if (ready)
                    CompleteRuntimeBridge(upper, lower, style, prefabName, registrationName, state, report,
                        request.BuildAfterCreate);
                else
                    BridgeRuntimeRequests.Complete("CreateFailed");
                Finish(report, state, "Create runtime bridge", showMessage: false);
                // Refresh only after publication and export-state persistence, so the
                // new bridge cannot re-enter the selectable source-road catalogue.
                if (ready) Refresh();
            }))
        {
            BridgeRuntimeRequests.Complete("CreateFailed");
            Finish(report, state, "Create runtime bridge", showMessage: false);
        }
    }

    private void CompleteRuntimeBridge(
        Deck upper, Deck? lower, BridgeStyle style, string prefabName, string registrationName,
        ExportStateStore state, ExportReport report, bool buildAfterCreate)
    {
        state.Record(prefabName, RuntimeFingerprint(upper, lower, style));
        var recorded = BridgeRegistrationStore.Record(new BridgeRegistration(
            prefabName,
            registrationName,
            upper.Id,
            lower?.Id,
            style.Id,
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));

        if (recorded)
        {
            Mod.ReloadActiveLocale();
            var activated = buildAfterCreate && ActivatePrefab(prefabName);
            BridgeRuntimeRequests.Complete(activated
                ? "CreatedActive"
                : !buildAfterCreate ? "CreatedManage"
                : _activationLocked ? "CreatedLocked" : "ActivateUnloaded",
                registrationName, prefabName);
        }
        else
        {
            report.Failed(prefabName, new IOException(
                "The prefab was created, but its runtime registration record could not be saved."));
            BridgeRuntimeRequests.Complete("RegistrationFailed", registrationName);
        }
    }

    private static string RuntimeFingerprint(Deck upper, Deck? lower, BridgeStyle style)
    {
        return string.Join("|", new[]
        {
            "runtime-v1",
            upper.Road?.Fingerprint ?? upper.Id,
            style.Id,
            lower?.Id ?? string.Empty,
            lower == null ? "single" : "opp",
        });
    }

    private void ActivateRuntimeBridge(string prefabName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName)
            || BridgeRegistrationStore.Find(prefabName) == null)
        {
            BridgeRuntimeRequests.Complete("ActivateInvalid");
            return;
        }

        BridgeRuntimeRequests.Complete(ActivatePrefab(prefabName)
            ? "Activated"
            : _activationLocked ? "ActivateLocked" : "ActivateUnloaded");
    }

    private bool ActivatePrefab(string prefabName)
    {
        _activationLocked = false;
        try
        {
            var prefab = PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.name, prefabName, StringComparison.Ordinal));
            if (prefab == null) return false;
            if ((_gameMode & GameMode.Game) != 0
                && World.GetOrCreateSystemManaged<UnlockSystem>().IsLocked(prefab))
            {
                _activationLocked = true;
                // Both Create-and-build and management Build enter here. Keep the panel
                // open, leave the active tool untouched, and report the lock once per click.
                Mod.ShowMessage(UiStringCatalog.Current.Title, RuntimeUiText.Get("ActivateLocked"));
                return false;
            }
            if (!World.GetOrCreateSystemManaged<ToolSystem>().ActivatePrefabTool(prefab)) return false;
            World.GetExistingSystemManaged<BridgeBuilderUISystem>()?.CloseForBuild();
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, $"Could not activate runtime bridge '{prefabName}'");
            return false;
        }
    }

    private void RenameRuntimeBridge(string prefabName, string registrationName)
    {
        if (!BridgeRegistration.IsPrefabName(prefabName))
        {
            BridgeRuntimeRequests.Complete("RenameInvalid");
            return;
        }
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            BridgeRuntimeRequests.Complete("NameRequired");
            return;
        }

        if (BridgeRegistrationStore.Find(prefabName)?.RegistrationName == registrationName.Trim())
        {
            BridgeRuntimeRequests.Complete("Renamed", registrationName.Trim());
            return;
        }
        if (!BridgeRegistrationStore.Rename(prefabName, registrationName))
        {
            BridgeRuntimeRequests.Complete("RenameFailed");
            return;
        }

        Mod.ReloadActiveLocale();
        Refresh();
        BridgeRuntimeRequests.Complete(
            "Renamed", registrationName.Trim(), prefabName);
    }

    private void DeleteRuntimeBridge(string prefabName)
    {
        var registration = BridgeRegistrationStore.Find(prefabName);
        if (!BridgeRegistration.IsPrefabName(prefabName) || registration == null)
        {
            BridgeRuntimeRequests.Complete("DeleteMissing");
            return;
        }

        try
        {
            var roots = RemovalRoots(prefabName, PrefabCatalog.GetAll(_prefabSystem)).ToArray();
            if (roots.Length == 0) { BridgeRuntimeRequests.Complete("DeleteMissing"); return; }
            var ids = new HashSet<Entity>();
            foreach (var root in roots)
                if (_prefabSystem.TryGetEntity(root, out var id)) ids.Add(id);
            var plan = BridgeInstanceRemoval.Collect(EntityManager, ids);
            // Stop placement before marking any entity. A temporary tool preview is not a
            // placed bridge, and must be released by its owning tool, not by our PrefabRef query.
            var tools = World.GetOrCreateSystemManaged<ToolSystem>();
            if (roots.Contains(tools.activePrefab)) tools.ActivatePrefabTool(null);
            if (plan.DeletedEntities.Contains(tools.selected)) tools.selected = Entity.Null;
            ClearPreview();
            BridgePreviewState.Clear();
            plan.Apply(EntityManager);
            _pendingRemoval = plan;
            _removingRegistration = registration;
            Mod.Log.Info($"Removing '{prefabName}': {plan.DeletedEntities.Count} network entities; "
                + "waiting for native cleanup before deleting assets. Composition caches are retained.");
            if (plan.IsComplete(EntityManager)) CompleteRuntimeDeletion();
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, $"Could not safely begin deletion of '{prefabName}'");
            BridgeRuntimeRequests.Complete("DeleteUnsafe");
        }
    }

    private void CompleteRuntimeDeletion()
    {
        var registration = _removingRegistration;
        var placed = _pendingRemoval?.DeletedEntities.Count ?? 0;
        _pendingRemoval = null;
        _removingRegistration = null;
        if (registration == null) return;
        var prefabName = registration.PrefabName;
        var state = ExportStateStore.Load();
        var report = new ExportReport();
        var removed = RemoveByName(prefabName, state, report);
        if (removed.Count > 0 && BridgeRegistrationStore.Remove(prefabName))
        {
            Mod.ReloadActiveLocale();
            BridgeRuntimeRequests.Complete(
                "Deleted", registration.RegistrationName, placed);
        }
        else
        {
            BridgeRuntimeRequests.Complete(
                "DeleteIncomplete");
        }

        Finish(report, state, "Delete runtime bridge", showMessage: false);
        // Native removal has completed and state is saved. Also reflect partial
        // deletion accurately instead of keeping a stale list until reopening.
        Refresh();
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
            report.Warning("The removed prefabs stay registered in the running session. Restart the game to get rid of them.");
        }

        Finish(report, state, "Remove bridge");
    }

    private IReadOnlyList<string> RemoveByName(string exportName, ExportStateStore state, ExportReport report)
    {
        var removed = new List<string>();
        var loaded = PrefabCatalog.GetAll(_prefabSystem).ToArray();
        var roots = RemovalRoots(exportName, loaded).ToArray();
        if (roots.Length == 0)
        {
            report.Skipped(exportName, "no exported asset with that name is loaded");
            return removed;
        }
        var rootEntities = new HashSet<Entity>();
        foreach (var root in roots)
            if (_prefabSystem.TryGetEntity(root, out var entity)) rootEntities.Add(entity);
        // Also protects the settings-page remover and a shared junction that has not yet been
        // reassigned by the native network update. Never delete its backing asset prematurely.
        if (BridgeInstanceRemoval.HasPlacedReferences(EntityManager, rootEntities))
        {
            report.Warning($"Kept '{exportName}': a placed network entity still references it.");
            return removed;
        }
        var tools = World.GetOrCreateSystemManaged<ToolSystem>();
        if (roots.Contains(tools.activePrefab)) tools.ActivatePrefabTool(null);
        // Registered prefab/composition entities remain valid until world teardown. Do not call
        // RemovePrefab (which invalidates PrefabData indices) or unload their meshes here.
        var uuidOwner = BridgeRegistration.IsPrefabName(exportName);
        var sharedPrefix = _settings.NamePrefix + "Dep_";
        var deleted = BridgePrefabRemoval.Remove(roots, loaded,
            candidate => (candidate.name ?? string.Empty).StartsWith(sharedPrefix, StringComparison.Ordinal)
                || (uuidOwner && (candidate.name ?? string.Empty).Contains(exportName)),
            Mod.Setting?.RemoveUnusedDependencies ?? true, report);
        foreach (var root in roots)
        {
            if (!deleted.Contains(root)) continue;
            HideRemovedBridge(root);
            state.Remove(root.name);
            RoadBuilderIconExporter.Discard(root.name);
            report.Removed(root.name);
            removed.Add(root.name);
        }
        if (removed.Count > 0)
            World.GetOrCreateSystemManaged<BridgePublicationSystem>().RefreshMenus(report);
        return removed;
    }

    private static IEnumerable<PrefabBase> RemovalRoots(string exportName, IEnumerable<PrefabBase> loaded)
    {
        // The lower deck, when there is one, is a second asset next to the bridge and has to go too.
        foreach (var name in new[]
                 {
                     exportName,
                     BridgeNaming.LowerDeckName(exportName),
                     BridgeNaming.CarriedDeckName(exportName, above: true),
                 })
        {
            var prefab = loaded
                .OfType<NetGeometryPrefab>()
                .FirstOrDefault(candidate => candidate.asset != null
                    && !candidate.isReadOnly
                    && string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (prefab == null)
            {
                continue;
            }

            yield return prefab;
        }

    }

    private void HideRemovedBridge(PrefabBase root)
    {
        if (root.TryGet<UIObject>(out var ui))
        {
            ui.m_IsDebugObject = true;
            ui.m_Group = null;
        }
        if (!_prefabSystem.TryGetEntity(root, out var entity)
            || !EntityManager.HasComponent<UIObjectData>(entity)) return;
        var data = EntityManager.GetComponentData<UIObjectData>(entity);
        if (EntityManager.Exists(data.m_Group) && EntityManager.HasBuffer<UIGroupElement>(data.m_Group))
        {
            var group = EntityManager.GetBuffer<UIGroupElement>(data.m_Group);
            for (var i = group.Length - 1; i >= 0; i--)
                if (group[i].m_Prefab == entity) group.RemoveAt(i);
        }
        data.m_Group = Entity.Null;
        EntityManager.SetComponentData(entity, data);
    }

    private void Finish(
        ExportReport report, ExportStateStore state, string operation, bool showMessage = true)
    {
        try
        {
            state.Save();
            report.Save(_gameMode.ToString(), operation);
        }
        catch (Exception)
        {
            // Generation diagnostics are silent; retain the external API exception boundary.
        }

        ModHost.Log.Info(
            $"{operation}: {report.ExportedRoads} exported, {report.RemovedRoads} removed, "
            + $"{report.SkippedRoads} skipped, {report.FailedRoads} failed");

        UiStrings text = UiStringCatalog.Current;
        var summary = string.Format(
            text.OperationSummary,
            report.ExportedRoads,
            report.RemovedRoads,
            report.SkippedRoads,
            report.FailedRoads);
        RoadSelectionModel.PublishOperationResult(summary);

        if (showMessage && report.FailedRoads == 0)
            Mod.ShowMessage(text.Title, summary + "\n" + text.StateReportHint);
        BridgeBuilderUISystem.RequestRefresh();
    }
}
