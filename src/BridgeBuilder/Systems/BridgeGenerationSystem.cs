using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using BridgeBuilder.UI;
using Colossal.Serialization.Entities;
using CS2Mods.Shared;

using CS2Mods.Shared.Discovery;

using CS2Mods.Shared.Infrastructure;
using Game;



using Game.Prefabs;

using System;
using System.Collections.Generic;
using System.Globalization;

using System.Linq;

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
    private readonly BridgePrototypeMaterialAudit _prototypeMaterialAudit = new();
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
    private BridgeAssetInfo? _removingAsset;
    private DateTime _removalStartedUtc;
    private bool _activationLocked;
    private string _activationFailure = "ActivateFailed";

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
        BridgeRuntimeRequests.BeginCatalogLoad();
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
        BridgeRuntimeRequests.BeginCatalogLoad();
        _pendingRemoval = null;
        _removingAsset = null;
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
                else if (DateTime.UtcNow - _removalStartedUtc > TimeSpan.FromSeconds(90))
                    FailDeletion(_removingAsset?.PrefabName ?? "", "DeleteIncomplete",
                        "Native network cleanup did not complete within 90 seconds; assets retained.");
            }
            catch (Exception exception)
            {
                FailDeletion(_removingAsset?.PrefabName ?? "", "DeleteIncomplete",
                    "Could not finish native network cleanup safely.", exception);
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
        BridgeRuntimeRequests.BeginCatalogLoad();
        try
        {
            RefreshCatalogs();
        }
        finally
        {
            BridgeRuntimeRequests.EndCatalogLoad();
        }
    }

    private void RefreshCatalogs()
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
                generated.Concat(BridgeAssetCatalog.Load().Select(entry => entry.PrefabName)));
            BridgeStyleCatalog.Rebuild(_prefabSystem, generated);
            _prototypeMaterialAudit.Inspect(_prefabSystem);
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
                RenameRuntimeBridge(request.PrefabName, request.DisplayName);
                return;
            case BridgeRuntimeAction.Delete:
                DeleteRuntimeBridge(request.PrefabName);
                return;
        }
    }

    private void Finish(
        ExportReport report, ExportStateStore state, string operation, bool showMessage = true)
    {
        try
        {
            state.Save();
            report.Save(_gameMode.ToString(), operation);
        }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, $"{operation}: could not save operation state or diagnostics.");
        }

        if (report.FailedRoads != 0 || report.FailureDetails.Length != 0)
            Mod.Log.Error($"{operation} failed. {report.FailureDetails}\nSee ModsData/BridgeBuilder/last-export-report.txt.");

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
