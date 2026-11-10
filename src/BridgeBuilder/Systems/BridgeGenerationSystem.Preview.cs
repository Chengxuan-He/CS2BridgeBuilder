using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;







using CS2Mods.Shared.Infrastructure;




using Game.Prefabs;

using System;



using System.Linq;



namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private GenerationWork? _previewWork;

    private bool UpdatePreviewWork()
    {
        var work = _previewWork;
        if (work == null) return false;
        if (_previewRevision != BridgePreviewState.Revision || BridgePreviewState.Selection == null)
            work.Cancel();
        else work.Tick();
        if (work.IsComplete)
        {
            _previewWork = null;
            try { work.Completion?.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { _previewReleasePending = true; }
            catch (Exception exception)
            {
                Mod.Log.Error(exception, "Bridge preview continuation failed.");
                FailPreview(_previewRevision, "PreviewFailed");
            }
            work.Report();
            if (_previewReleasePending) ClearPreview();
        }
        // Do not admit another request into the same update as an old continuation.
        return true;
    }

    private void ClearPreview()
    {
        if (_materialPreviewRevision.HasValue) CancelMaterialPreparation();
        var work = _previewWork;
        if (work != null)
        {
            work.Cancel();
            if (!work.IsComplete) { _previewReleasePending = true; return; }
            _previewWork = null;
            try { work.Completion?.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Mod.Log.Error(exception, "Cancelled preview cleanup failed."); }
            work.Report();
        }
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
            if (string.IsNullOrEmpty(request.PrefabName) && BridgePreparedMaterials.RequestFiles == null &&
                BridgeStyleCatalog.Find(request.StyleId) != null)
            {
                PrepareMaterialFiles(request.StyleId, () => BuildPreview(request, revision), exception =>
                {
                    Mod.Log.Error(exception, "Bridge preview material preparation failed.");
                    FailPreview(revision, "PreviewFailed");
                }, revision);
                return;
            }
            var work = new GenerationWork("preview/" + revision,
                () => World.IsCreated && revision == BridgePreviewState.Revision && BridgePreviewState.Selection != null);
            _previewWork = work;
            work.Start(() => BuildPreviewAsync(request, revision));
        }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, "Bridge preview setup failed.");
            FailPreview(revision, "PreviewFailed");
        }
    }

    private async System.Threading.Tasks.Task BuildPreviewAsync(BridgeRuntimeRequest request, int revision)
    {
        // The material preparation callback releases its own reference as soon as it returns.
        using var files = BridgePreparedMaterials.RequestFiles?.Retain();
        try
        {
            await GenerationWork.Next("preview setup");
            if (!string.IsNullOrEmpty(request.PrefabName))
            {
                var assetInfo = BridgeAssetCatalog.Find(request.PrefabName);
                var existing = assetInfo == null ? null : PrefabCatalog.GetAll(_prefabSystem)
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
                if (!await BuildBridgeCoreAsync(upper, lower, style, _previewSession.Name, options, false,
                        new ExportReport(), _previewSession))
                {
                    FailPreview(revision, "PreviewBuildFailed");
                    _previewReleasePending = true;
                    return;
                }
            }
            await GenerationWork.Next("preview scene expansion");
            _previewDraws = new BridgePreviewDrawList(_previewSession);
            if (!await BridgePreviewScene.BuildAsync(_previewSession, _previewDraws))
            {
                FailPreview(revision, "PreviewAssemblyFailed");
                _previewReleasePending = true;
                return;
            }
            // Own the renderer before native allocation starts, so ClearPreview
            // also releases a partially initialized scene/camera after an error.
            await GenerationWork.Next("preview renderer initialization");
            _previewRenderer = new BridgePreviewRenderer();
            await _previewRenderer.InitializeAsync(_previewSession.Name, _previewDraws);
            await GenerationWork.Next("preview renderer start");
            _previewRenderer.Start((image, error) =>
            {
                if (revision != BridgePreviewState.Revision || BridgePreviewState.Selection == null) return;
                if (error.Length != 0 || string.IsNullOrEmpty(image))
                    FailPreview(revision, error.Length != 0 ? error : "RenderEmpty");
                else
                    BridgePreviewState.Publish(revision, image, "PreviewReady");
            });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, "Bridge preview failed.");
            FailPreview(revision, "PreviewFailed");
            _previewReleasePending = true;
        }
    }

    private void FailPreview(int revision, string stage)
    {
        // A cancelled/superseded selection is not a model generation failure.
        // Publish each failed request once; the panel publisher records ERROR.
        if (revision != BridgePreviewState.Revision || BridgePreviewState.Selection == null ||
            _previewFailedRevision == revision) return;
        _previewFailedRevision = revision;
        // Never destroy a camera inside its rendering callback. Release the
        // isolated preview on the next update; no city/saved assets are touched.
        _previewReleasePending = true;
        BridgePreviewState.Publish(revision, string.Empty, stage);
    }

}
