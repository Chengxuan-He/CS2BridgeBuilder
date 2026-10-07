using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;







using CS2Mods.Shared.Infrastructure;




using Game.Prefabs;

using System;



using System.Linq;



namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
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
        catch (Exception exception)
        {
            Mod.Log.Critical(exception, "Bridge preview failed.");
            FailPreview(revision, "PreviewFailed");
            ClearPreview();
        }
    }

    private void FailPreview(int revision, string stage)
    {
        // A cancelled/superseded selection is not a model generation failure.
        // Publish each failed request once; the panel publisher records CRITICAL.
        if (revision != BridgePreviewState.Revision || BridgePreviewState.Selection == null ||
            _previewFailedRevision == revision) return;
        _previewFailedRevision = revision;
        // Never destroy a camera inside its rendering callback. Release the
        // isolated preview on the next update; no city/saved assets are touched.
        _previewReleasePending = true;
        BridgePreviewState.Publish(revision, string.Empty, stage);
    }

}
