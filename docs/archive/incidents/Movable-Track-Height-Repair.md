# Movable track height preservation

## Defect and native evidence

The movable lane snapshot retained `EdgeLane` but discarded `NodeLane` and
`Game.Net.Elevation`. Node visuals consequently used the untyped
`BatchDataHelpers.BuildCurveParams(size)` overload. Its Y parameter is one,
which makes `MovableLaneDeformation` project the lane centre onto the road.
This erases the native rail datum: negative-local-Y rail and sleeper geometry
can become buried in the bridge deck. The Draw01 sampler obtains its moving
span from node contributions, so this affects its rail inputs directly.

The installed game's `BatchDataSystem.UpdateLane` rendering path selects
parameters in this order: node, edge, explicit elevation, untyped fallback.
`BuildCurveParams(size, NodeLane)` keeps Y zero and retains the native shared
endpoint parameters. Node curve scale also uses `NodeLane` and `NetLaneData`.
The fix calls those native helpers rather than inventing a rail height offset.

## Implementation

`BridgePreviewLanes.Lane` now retains node and elevation metadata and lane data.
Both private-network extraction and standalone preview sampling populate it.
`CurveParameters` and `CurveScale` share native parameter selection between
the ordinary preview renderer and the movable geometry converter. This applies
to train, subway, tram and other lane visuals according to their actual native
components, without selecting a track by name or changing non-movable assets.

All movable LODs use the same extraction path. Existing non-decal lane geometry,
surface conversion, physical seam clipping, original road-bone binding and
native persistence are preserved. No runtime synchronization or new shader is
introduced. Existing generated assets retain their saved geometry; regenerate
an affected bridge to receive the correction.

## Verification and limits

`tools/MovableLaneDeformationCheck` links the production lane snapshot and
deformation evaluator with the installed game's real parameter helpers. It
checks node endpoint factors, node scale, edge/elevation/fallback selection and
positive rail/sleeper height above the receiver for both ordinary and tiled
geometry. The synthetic height fixture deliberately uses negative local Y
vertices; it is a regression fixture, not a claimed prototype measurement.
The existing 612 cable cases remain unchanged and pass.

No user-visible text is added or changed; existing localization paths remain
unchanged. Native preview, newly generated track geometry, motion, all-distance
appearance and cold loading still require manual game verification.
