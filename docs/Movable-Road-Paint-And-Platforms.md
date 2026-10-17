# Movable road paint and fixed platform repair

## Current implementation

Markings are part of the mechanism road geometry. The generator clips each receiver triangle against the evaluated paint footprints and replaces the covered portion with paint triangles. Painted and unpainted polygons partition the same receiver; no extra thickness, overlapping road surface, Z correction or independent decal object is emitted. Interpolated positions, source provenance and native bone indices survive the split. All three render LODs use this path. Preview and saved bridges share it.

White paint reuses the original B&P DrawBridge03 object surface `792a855e9d0c5c34d2c41d48b062139d`, with an interior white road-paint texel at `(3500/4096, 820/4096)`. Yellow paint reuses DrawBridge01 object surface `97291ab7f7938fc35cc670b15f2bca36`, at `(800/4096, 3000/4096)`. Native base-color inspection measured RGBA `(188,188,188,255)` and `(181,144,55,255)` respectively. These are original object materials, not DefaultDecal materials inserted into skinned mesh slots. Material channels and their dependencies remain original references. There is no material creation, texture baking or custom shader.

The existing lane-pattern measurements and accepted curve approximation determine paint footprints. Interior atlas sampling intentionally uses uniform native paint; it does not reproduce the original network decal's weathering, endpoint fade or infoview-specific decal behavior. Independent marking RenderPrefabs, StaticObjectPrefabs, projection-volume geometry and attachment construction have been removed. Additional paint material batches belong to existing mechanism RenderPrefabs and introduce no new Prefab nodes.

Managed receiver clipping runs independently per input triangle in parallel. Asset lookup, render creation and saving stay on the calling thread. No pixels are processed at runtime. The implementation does not claim GPU execution of polygon clipping.

## Road and fixed platforms

The road asphalt UV patch now excludes the prototype yellow edge stripe at U=0.1953125. Moving sidewalks continue to use the native grating appearance.

Both fixed sidewalk platforms retain their original structural faces, UVs and native material at every LOD. Their replacement masks and fragment overrides no longer remove these faces. The generated selected-road surface excludes the corresponding fixed sidewalk domain, preventing a duplicate surface. The fixed carriageway remains generated from the selected road. Platform width follows the existing measured structural transformation; original Z coordinates are unchanged. Road-object placement continues to use its original complete motion partition, independently of road-face exclusion.

Offline native face inspection identified 92, 70 and 28 retained fixed-sidewalk/curb faces at LOD0, LOD1 and LOD2 respectively. The immutable face lists are in `Draw01FixedRoadData.g.cs`; runtime does not infer them from geometry shape.

## Measured baseline and validation limits

The real saved sample `b62023176-6b2e-4187-a04b-8e28317ebb1d` predates this repair. Its log records 75 serialized Prefabs, 3 independent marking objects, 211 ms marking processing, 480 ms generator work and 720 ms through native publication. Its type log lists 76 owned nodes before serialization; these two counts are distinct stages. This confirms the earlier compaction improved the old 523-Prefab graph, but does not measure this repair.

Read-only decoding of that sample found positively wound replacement platform triangles covering X=10.400024..13.5 and Z=78.15039..90 on the positive end. Therefore the reported visual defect cannot honestly be attributed simply to absent vertices. This repair restores the prototype's complete fixed-platform rendering representation instead of adding another overlapping surface. Its visual effectiveness remains unverified.

Release compilation succeeded, and all 12 installed payload files matched the build output. The final installation backup contains 18 original files under `artifacts/backups/remove-marking-object-path-20261017-101637/`. No game-created sample exists for this build yet. Compilation does not verify painted appearance, open/closed movement, distant LODs or cold loading. These still require human verification with a newly created Draw01 bridge.

Earlier automatic approval review rejected affected-asset cleanup. No alternate deletion route was used. Existing generated bridges retain their old serialized geometry and must not be treated as samples of this repair.


## 2026-10-17 follow-up: fixed carriageway cap and prototype UV projection

The user's new comparison identifies the missing pale area across the carriageway, not the side platforms restored above. The prior repair did not address this area. Native LOD0 faces 2674-2677 place the fixed carriageway concrete between the leaf joint at absolute Z=83.740234375 and the node end at absolute Z=90. The appearance partition now assigns this region the native concrete atlas, while retaining selected-road geometry and lane layout. It is not an extra white overlay or a new material.

The previous converter repeated a native atlas rectangle using the selected network material's UVs. Those coordinates belong to a different shader representation, producing incorrect physical texture size. Converted road surfaces now use measured prototype object-space UV gradients before atlas repetition and clipping. Tangents and handedness follow the projected axes. Already-compatible existing object materials retain their own UVs.

Measured native references:

| Surface | Native LOD0 faces | Physical reference | Atlas direction |
| --- | --- | --- | --- |
| Asphalt | 1512/1513 | 3.3660278 m across, 4 m longitudinal | U increases with X; V increases with Z |
| Left grating | 1819/1820 | 3.0269777 m across, 4 m longitudinal | U decreases with X; V decreases with Z |
| Right grating | 2367/2368 | Same scale; separately measured lateral origin | U decreases with X; V decreases with Z |
| Fixed carriageway concrete | 2674/2675 | 3.4000244 m across, 4 m longitudinal | U increases with X; V decreases with Z |

Asphalt and concrete sampling exclude embedded prototype lane paint while retaining the measured UV-per-metre gradients. Left and right grating origins move with the corresponding structural side. All generated road LODs share this projection. Geometry coordinates and structural Z remain unchanged. Existing-material paint inlay still introduces no independent decal objects.

Branch commits: `25cd501`, `6d65b8c`, integrated into the main working tree. Release compilation passed and all 12 deployment files matched the build output; final backup: `artifacts/backups/native-road-projection-sides-20261017-103327/`. Native geometry and UV inspection support the implementation, but no newly generated in-game sample has been inspected after this deployment. Old serialized assets do not update automatically. The previous cleanup rejection remains unresolved; no alternate deletion route was attempted.


## 2026-10-17: projected tangent-frame regression

The 10:42 game log records both preview and formal creation failing for Large Road Divided / Draw01: `Moving road LOD 0: Road triangle 272 has invalid attributes, material index or mixed tangent handedness.` The previous projection increment recalculated a tangent and sign independently from each vertex normal. This can generate a zero tangent when the projection axis is parallel to a side normal, or inconsistent signs within a smoothed triangle. The combined diagnostic does not distinguish which predicate failed on triangle 272.

The repair keeps projected coordinates but constructs the tangent frame from each triangle's position/UV derivatives. A single handedness sign is shared by its vertices. The face tangent is rotated onto each vertex normal to retain a nonzero orthonormal frame. A vertical face with exactly collapsed projected UV area has no invertible UV derivative and retains its original valid tangent frame. This is explicit handling of a mathematically undefined UV derivative, not removal of validation or suppression of an error. Existing triangle validation remains unchanged. Projection scale, fixed cap materials, bone assignments and paint inlay are unchanged.

Release compilation and all 12 installed-file comparisons passed. Backup: `artifacts/backups/projected-tangent-frame-20261017-104421/`. The failing game sample has not been replayed in game after deployment; preview success and appearance therefore remain unverified. No visual unit tests were used as acceptance evidence.


## Prototype UV orientation domains

Native mesh inspection confirms that one global projection cannot describe the authored bridge. Positive-Z left asphalt faces 1512/1513 increase U with X and V with Z. Positive-Z right faces 2183/2184 reverse both directions. Negative-Z faces 9406/9407 and 10077/10078 mirror the corresponding longitudinal domain. Grating faces 1819/1820, 2367/2368, 9713/9714 and 10261/10262 retain distinct side origins and mirror Z between leaves. Fixed concrete faces 2674/2675 and 10568/10569 confirm the fixed-cap longitudinal mirror.

The appearance partition now cuts at the bridge centreline and central joint as well as the existing motion/material boundaries. Each domain uses its measured prototype orientation; the fixed caps mirror U across the carriageway, while asphalt mirrors both U and V. Shared interpolation, paint inlay and tangent handling consume the split triangles. No generated section-name mapping determines UV direction.

This is not a claim of exact per-face native UV duplication: the converter still tiles measured atlas regions, crops embedded native paint from asphalt/concrete, and applies the same logical appearance to generated LODs. Individual authored island boundaries, vertical-face projection and native LOD atlas differences have not been exhaustively reproduced or visually accepted. Those limits must remain explicit when describing prototype correspondence.

Branch increment `15cd2ff` was integrated. Release compilation and all 12 installed payload checks passed. Backup: `artifacts/backups/prototype-uv-domains-20261017-104655/`. In-game preview, formal generation and visual verification remain pending for this build.

## 2026-10-17: repeated pale gutter and selected-road sidewalks

The reported longitudinal pale stripes correspond to an atlas-selection defect. The previous
asphalt minimum U was 0.0124359130859375 (texel X=50.9375 in the original 4096 atlas).
Inspection of the original base-color pixels places that coordinate in the pale gutter adjacent
to the asphalt: at raw row 2800, X=51 is RGB (119,116,111), while X=100 is (71,71,71).
Repeating this rectangle repeated the gutter across the road independently of generated markings.
The asphalt minimum is now X=96/4096, inside the asphalt with an interior margin. The native
UV-per-metre gradient is retained by adjusting the physical repeat width for the cropped rectangle.
No source material, texture, pixel, depth or geometry position was changed for this correction.

Previously both material and motion domains selected a sidewalk using the prototype's symmetric
inner-curb X coordinate plus half-width delta. That does not represent a selected road with no
sidewalks, one sidewalk or unequal sidewalk widths. Road surface occurrences now select their
walkway role from actual native NetPieceLanes/PedestrianLane components; their evaluated geometry
retains its original side, position and width. No section-name or prefab mapping table is used.
Non-pedestrian occurrences use carriageway projection and longitudinal motion seams. Pedestrian
occurrences use walkway projection and the measured walkway seams; no occurrence is added for an
absent sidewalk. Native fixed structural platforms remain retained. Lane visual occurrences have
their own explicit partitions, rather than accidentally indexing road occurrences. Marking inlay
inherits the receiver's occurrence and binding. The shared material helper accepts occurrence
partitions and preserves its original behavior when none are supplied.

Read-only native data confirms Sidewalk Piece 3.5 contains Pedestrian Lane 3 and Sidewalk Piece 6
contains Pedestrian Lane 5. The selector uses component types, not those names or widths. This
increment does not claim support for every third-party mixed-use Piece: its actual geometry and
lane semantics must be inspected if it combines different surface roles within a single Piece.

Style commits: `e2dc87c`, `deb4be8` on `bridge/draw-01`, integrated into the main working tree.
Release build passed; all 12 installed payloads matched the output. Installation backup contains
18 original files under `artifacts/backups/selected-sidewalk-atlas-gutter-20261017-115415/`.
No new material, texture, marking Prefab or engine shader was created. Actual in-game verification
of the new stripe-free surface and zero/one/asymmetric sidewalk cases is still pending. Existing
serialized bridges remain unchanged; the previous cleanup rejection was not bypassed.

## Paint before atlas tessellation (2026-10-17)

The inspected original DrawBridge01 base-color atlas contains the yellow longitudinal lane
paint inside its road islands (surface `97291ab7f7938fc35cc670b15f2bca36`). It is not a
per-dash independent object. Native moving-node hidden composition suppresses ordinary lane
rendering through the LaneSystem/SecondaryLaneSystem paths documented in
[stationary lane-visual suppression](Movable-stationary-lane-visual-suppression.md).

The selected road cannot inherit the prototype's fixed lane pattern. Its measured paint
footprints still partition the receiver using existing object materials. Draw01 now performs
that inlay once, after hardcoded motion partitioning and receiver preparation, before any
road-atlas repetition. Each LOD then maps unpainted road into its own measured atlas projection.
Paint uses its existing compatible material and constant interior UV, so road atlas seams do
not subdivide it. The later lane snapshot retains all non-decal geometry and surfaces, with
consumed decal batches omitted to prevent applying paint again at every LOD. The sampler's
original snapshot is not mutated. No selected marking is removed by distance in this change.

Implementation: style branch `bridge/draw-01`, commit `988f00a`, integrated into dev.
Preview and persisted generation share the same path. Native bone ownership, motion gaps,
selected-road composition, material references and persistence format are retained. The
original atlas was inspected and Release compilation passed. New generated counts, near/far
appearance and opening/closing acceptance remain pending human-created game output; previous
triangle counts must not be presented as measurements of this reordered pipeline.
