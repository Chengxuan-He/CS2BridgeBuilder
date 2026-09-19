# Track preview ownership and deck direction

## Evidence

The September 19 log immediately preceding preview revision 68 reported that `RoadBuilder.Domain.Prefabs.TrackBuilderPrefab:t2ced4a70-2c9e-462f-92a4-fa072dfb884c-76561199197854251` was kept as an external runtime dependency. The next failure was Unity `Object.GetName` inside `BridgePreviewRenderResources.LoadPrivateMeshes`, while visiting an auxiliary deck object.

Inspection of the installed Road Builder assembly shows `TrackBuilderPrefab : Game.Prefabs.TrackPrefab`. The shared graph cloner previously projected only `RoadBuilderPrefab` onto `RoadPrefab`. Other external roots were returned unchanged. `AttachSecondDeck` then prepared that shared track in place and attached preview-owned structures. Disposing the preview destroyed those structures, leaving the shared track with dead mesh references for the next preview. The same source alias was unsafe for formal generation as well.

## Changes

- The shared cloner projects Road Builder network subclasses onto their nearest native game base class, including `TrackPrefab` and path networks. Native inherited serialized fields are deeply cloned, with the existing Road Builder component stripping retained. No assembly dependency on Road Builder is introduced.
- Both root and auxiliary deck construction reject source/target identity before bridge composition mutates them. `CloneRoad` also refuses to change the icon if an unsupported external source was returned unchanged.
- Drawing a destroyed Unity render prefab now fails explicitly without reading its native name. A missing model still reaches the existing once-per-revision CRITICAL path; no partial model is accepted as success.
- Double-deck direction is a localized UI toggle, included in both preview and formal creation recipes and preview cache identity.
- Preview auxiliary reversal follows the installed game's `NetUtils.ShouldInvert` and `CourseSplitSystem.GetAuxCourse`: reverse the straight course endpoints, rotate frames by PI, and use `NetCompositionHelpers.InvertCompositionFlags` for the corresponding section states. Authored vertical separation is unchanged.

## Verification boundary

Compilation checks loadability. UI/localization/request-state checks cover both toggle states, both creation actions, and stale preview rejection; they do not validate meshes. No visual geometry unit tests or game-control operations are used. Restart the game to replace source prefabs damaged in memory by the old version, then repeatedly switch previews using the reported Road Builder road and track, in both directions. Check a formal double-deck bridge and confirm the original track remains unchanged. Existing saved bridges and mod caches are not deleted or rewritten by this repair.
