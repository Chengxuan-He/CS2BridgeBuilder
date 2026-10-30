# Lift03 mechanism render configuration
This report retains source measurements and stage-specific implementation evidence. Early
statements about missing callers, private branches or unfinished routing describe their recorded
stage, not the current ten-style router. Use the [documentation index](README.md),
[current contracts](agent-contract/README.md) and [shared handoff](../tools/PreparedMaterials/SharedCurrent.md)
for current requirements and delivery evidence. Recorded tests do not certify the new material scheme.


The generated render-data class configures fresh private render graphs for the mechanism,
column and footing. It preserves each native level's StackProperties, BaseProperties,
LOD references and content prerequisite. The caller supplies generated geometry and applies
the separately recorded procedural skeleton to each main-render level. Do not apply this
configuration to shared native renders or to a render already carrying copied components.

| Part | Native render records in LOD order |
| --- | --- |
| Mechanism | 79685, 79686, 79687 |
| Column | 79697, 79698, 79699 |
| Footing | 79700, 79701, 79702 |

The names Column and Footing identify measured object mesh slots; they are not runtime name
matching or permission to infer geometry. Footing is distinct from the road-adjacent deck base.

Source audit: game 1.6.2f1, SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
The extractor follows actual LOD references and checks the complete component list. No runtime
archetype lookup, width inference or new material is introduced.

This class has no generator caller yet. It changes no existing bridge output. Compilation
does not establish geometry, native animation, shipping or cold-start acceptance.

## Main mesh inventory and detailed glass (2026-10-20)

The offline component inventory now covers the three native main meshes:
474534/349267/996 vertices and 382928/229516/852 triangles. Detailed levels each
contain 7836 connected authored components, with 46 crossing the centre plane.
The far mesh contains 17 components, five crossing. These are identification
inputs, not runtime classification or proof of complete part correspondence.

Lift03Glass.py verifies and emits the independent mesh-1 glass transform. Each
native detailed level contains 300 panes (1200 vertices, 600 triangles), all on
fixed bone 0. The exact side ranges are 0..299 negative, 300..599 positive,
600..899 negative and 900..1199 positive. Every pane lies entirely on one side;
no crossing pane is stretched. Both detailed levels contain the same pane
triangles, and retain their original index buffers, Y/Z, channels and material.
Native LOD2 has no independent glass mesh.

Source Geometry SHA-256 values are pinned by the emitter:
LOD0 f8bc2882fcba043d1d57a17fd27a43631b492ab4292f8fc6b9c3beeb00af53cd;
LOD1 455d25f8ec83f91ea097551fb09a9f41abc79e9f15e836b19c0a527ad50ff9fa.
Four width deltas (-0.5, 0, 2 and 8m) preserve all pane edges and Y/Z coordinates.
Original and widened views were inspected. Runtime consumes the four literal
index ranges and does not infer glass membership from bounds or positions.

Release compilation and twelve-language checks pass. This helper introduces no
user-facing text and is not yet used by a complete Lift03 generator. The remaining
structure, both decks' road/material conversion, selected-layout compatibility
and final generation/persistence remain unfinished. Installed binaries and saved
bridges are unchanged by this helper-only increment.

## Disconnected side-part width map (2026-10-20)

Lift03RigidParts.py emits immutable index ranges for the disconnected main-mesh
parts that remain wholly on one side of the original centre plane. Detailed levels
contain 7790 such components each, with 424232/316429 vertices and
339836/205571 triangles. LOD2 contains twelve side assemblies, covering 608
vertices and 528 triangles. Crossing components are excluded at every level;
merged far centre-crossing bodies require their own measured rules.

The emitter pins all three source Geometry hashes, checks OBJ topology and
component membership, and records the original bone sets. It groups vertices and
triangles once before visiting components, avoiding a full-mesh scan for each of
the 7790 detailed parts. Runtime receives only literal vertex ranges and side
signs. It performs no geometric classification and changes only X by the signed
half-width delta. This helper does not decide sidewalk inclusion or retain original
track geometry in the final selected-road result; those masks remain separate work.

All selected triangle edges, winding and Y/Z coordinates remain unchanged at
four deltas (-0.25, 0, 2 and 8m), and all excluded vertices stay untouched. Native
and widened front/plan views covering every selected vertex were inspected for
all LODs. Detailed crossing assemblies, the five far crossing assemblies, both
road decks and selected-layout compatibility remain unfinished.

Release compilation and all twelve language checks pass. No user-facing text was
introduced. This helper-only increment is not deployed and is not evidence of
native animation, persistence or visual acceptance.

## Detailed dual-deck bases (2026-10-20)

Lift03DeckBases.py now emits separate upper/lower width maps for six measured
road-adjacent bases in LOD0 and LOD1. Parts 65/778/2826 belong to the upper deck;
66/779/2827 belong to the lower deck. The two fixed ends retain bone 0 and the
central moving bases retain bone 3. Each map applies x + sign(x) * deckDelta;
no base is scaled, no original triangles are cut, and other channels are untouched.

The emitter pins native Geometry hashes and verifies component vertex/triangle
counts, complete face membership and bone ownership. Five width pairs, including
unequal deck deltas and modest narrowing, retain winding, Y/Z and excluded vertices.
LOD0 covers 5668 vertices / 4862 triangles; LOD1 covers 2962 vertices / 1868 triangles.
Release compilation and all twelve localization checks pass; no user-facing text
was added. This detailed-only helper remains unwired and is not deployed.

The far merged bodies still require high-detail-derived part separation. Full
structural correspondence, dual-road appearance, selected-layout compatibility,
native generation, persistence and batch visual acceptance remain unfinished.

## Detailed crossing member families (2026-10-20)

Lift03CrossingMembers.py emits 31 independently identified components per detailed
LOD: seventeen transverse U frames, two wide end beams, three upper-deck support
troughs, three lower-deck support troughs and six lower-deck end strips. Each
component stretches across its own measured span. The helper accepts separate
frame, upper-deck and lower-deck deltas; choosing compatible final deck envelopes
and verifying their connections remains generation integration work.

LOD0 covers 11206 vertices and 10836 triangles; LOD1 covers 6691 vertices and
5727 triangles. Native Geometry hashes, OBJ vertex/index correspondence, complete
component membership, single-bone ownership and matching LOD stations are checked.
LOD1 component identifiers differ for several frames, so they are matched offline
by measured family and station rather than reusing LOD0 component IDs. Runtime
uses immutable vertex ranges and float32 spans only.

Five width combinations preserve winding and original Y/Z; excluded vertices do
not move. Front projections of both detailed LODs at native and unequal widened
deck settings were inspected. These maps do not overlap the existing rigid-side
or deck-base maps. They introduce no triangle cuts or material changes. Release
compilation and twelve-language checks pass; no user-facing text was added.

Nine detailed centre-crossing assemblies remain outside these maps. Inspection
shows merged longitudinal side members and transverse ribs in the shell bodies,
and joined side walls in the fixed support bodies. Applying one uniform stretch
to these whole components would distort side parts, so they still require authored
part separation. The five far crossing bodies also remain unfinished. This helper
increment is not routed or deployed and does not establish playable Lift03 support.

## Crossing-map correction and dual-deck replacement tops (2026-10-20)

This entry supersedes the whole-component mapping in the previous crossing-member
entry. Further inspection against contract section 8 found that the seventeen U
frames and two arched end beams contain side arms; scaling their whole connected
components would distort those arms. They have been removed from the emitted map.
The remaining twelve continuous support/strip components cover 3700/2548 vertices
in LOD0/LOD1. Their original faces and bones remain unchanged. There are now 28
unresolved detailed crossing components, not nine. No deployed output used the
withdrawn helper, and no generated bridge was changed by this correction.

Lift03RoadTops.py decodes the pinned native Geometry positions and indices and
identifies upward planar road-bed faces within the six recorded base components.
Upper and lower elevations are exactly 19.7998046875 and 9.800048828125 in source
object space. LOD0 has 312 upper and 512 lower faces; LOD1 has 144 upper and 426
lower faces. Fixed faces retain bone 0; central moving faces retain bone 3.
Per-deck top areas match between both detailed levels. Plan projections of all
selected faces were inspected. The upper source envelope is not a full rectangle;
its alternating inset edges are preserved by the exact face list.

The emitted removal helper checks every original triangle's indices before marking
it for road replacement. It neither subdivides receiver faces for paint nor selects
undersides, sidewalls, raised shoulders or other components. Final replacement
geometry, UV/material extraction and LOD2 top treatment remain separate work.
Release compilation and twelve-language checks pass. No user-visible text was
introduced, and these helpers remain unwired and undeployed.

## Shared dual-deck road motion partitions (2026-10-20)

The road-top extractor also emits Lift03RoadPartitions.g.cs. All six base top
regions are compared at both detailed LODs using decoded float32 positions and
original face bone ownership. Both decks have identical longitudinal boundaries:
central bone-3 road ends at +/-79.966796875, fixed bone-0 road starts at
+/-79.998046875 and ends at +/-127.96484375. Each expansion gap is exactly
0.03125 m. Regions beyond the fixed ends are excluded.

Lift03RoadSampling.PrepareDeck now uses this immutable carriageway partition and
the native four-bone count directly. Callers cannot substitute another style's
carriageway partition or bone count. Both decks retain moving bone array index 3,
connection 100; indices 1 and 2 remain counterweights. The shared preparation
path supplies the same physical carriageway partition to generated road and paint.
Sidewalk and appearance partitions are still explicit inputs and remain to be
measured; this change does not assume their boundaries match the carriageway.

The source skeleton, both selected-network origins and runtime animation are not
modified. Release compilation and twelve-language checks pass. This sampling
method still awaits the complete Lift03 generator, so no native generation or
motion acceptance is claimed. The helper increment is not deployed.

## Native dual-deck appearance units (2026-10-20)

The main Surface d5708265307c0c422270f918fb8ca8e8 resolves to the shared
LiftBridge03PillarBase_BaseColor VT, CID 8c8c7c348b3a3114bc610b7dcc7f305f.
Both its serialized VT reference and original texture hash match the decoded
4096-square source. Selecting a texture by a presumed LiftBridge03 name would
have selected no texture; actual references establish the relationship.

Upper road UVs sample asphalt containing baked white lines. Lower UVs sample
unpainted ballast. Lift03RoadAppearance defines separate block-aligned clean units:
upper pixels (3136,40)..(3520,1032), lower (2820,1048)..(4060,2040). The upper crop
lies between baked lines; the new independent native paint must not inherit those
original markings. Original source crops were inspected. No image synthesis or
structural Surface replacement is involved.

Upper reference face 23857 supplies density 124 pixels/m laterally and 124.359375
pixels/m longitudinally. Lower reference face 27296 supplies 249 pixels/m on both
axes. Normalized crop projections compensate for crop dimensions, preserving
these source densities. Four appearance regions preserve lateral/longitudinal
mirroring independently of the physical bone partitions. These are representative
repeat units: irregular source cell phases and quantized gradients are not all
reproduced. Repeat-edge appearance remains unaccepted.

Release compilation and twelve-language checks pass. No user-facing text was
introduced. This appearance helper is not yet connected to final material assembly
or the generator and is not deployed. Sidewalk appearance, merged structures,
far geometry and the full native generation/persistence path remain unfinished.

## Height-dependent U-frame arm separation (2026-10-20)

Lift03Frames.py replaces the withdrawn whole-component U-frame stretch with
measured opening profiles. All seventeen detailed frames are matched by closed/open
top family and longitudinal station. Horizontal slices identify the inner left/right
arm boundaries. The first/last opening boundary continues through the bottom/top
connecting member; vertices outside the boundary translate and interior connecting
material widens. Runtime receives literal weights and barycentric constructions,
not a geometric classifier.

LOD1 retains the same authored frame identities but uses its own reduced arm outline.
Its slight left/right asymmetry is preserved. Forcing LOD0's curved boundary into a
reduced side-arm chord produced inverted fragments during development and was rejected.
Wholly rigid side triangles remain untouched. Only triangles connecting carried arms
and central material are subdivided along the measured profile. Existing vertices,
Y/Z coordinates, native bones and Surface references are retained; constructed vertices
use the shared native-channel interpolation path.

LOD0 covers 5310 original vertices, with 228 constructed vertices and 160 split faces.
LOD1 covers 3145 original vertices, with 296 constructed vertices and 112 split faces.
Source-area preservation, noninverted winding at deltas -0.25/0/2/8 m, rigid side-face
translation, bone 0/3 ownership and disjointness from all other completed maps pass.
Native and widened front projections of both levels were inspected. Eleven detailed
crossing assemblies still await their own maps, together with far merged geometry.

Release compilation and all twelve localization checks pass. No user-facing text was
added. The new plan is not yet composed with a complete Lift03 structure/generator,
and is not deployed. Native animation and full bridge acceptance remain unproven.

## Executed structural-plan composition (2026-10-20)

The C# integration check executes the generated rigid-part, deck-base, continuous
support and U-frame maps together, then applies exact road-top removal. The test
extracts the current production MovableStructureModel nested data types and Plan
constructor without modifying their validation logic; only Unity vector arithmetic
is supplied by a minimal managed test type. It does not exercise native mesh import.

This exposed an integration defect in the initial frame emitter: all 228/296 new
points lie on original edges, while Plan correctly requires strictly interior
weights for TriangleVertex. The emitter now distinguishes edge and interior recipes,
orders them as required by Plan and remaps fragment indices. Current frame output
contains 228/296 EdgeVertex entries and no interior vertices. Constructor validation
now passes rather than relying on compilation alone.

Eight composed cases (two detailed LODs, deltas -0.25/0/2/8 m with distinct deck
increments) pass source immutability, unchanged original Y/Z, finite constructed
positions, original index preservation and nonoverlapping removal. The complete
replacement masks contain 984/682 faces: 824/570 road tops plus 160/112 frame
subdivisions. These are partial structure checks: the remaining unimplemented
assemblies are unchanged and no complete width-coverage claim is made.

Release compilation and twelve-language checks pass. Installed binaries and healthy
saved bridges are untouched by this helper-only correction. Full native geometry,
animation, persistence and batch acceptance remain outstanding.

## Arched moving end beams (2026-10-20)

The height-profile emitter now has an explicit end-beams mode for native components
7596 and 7662. The pair uses bone 3 at both detailed LODs. Its original curved
shoulders translate; the central connecting region widens using the measured opening
profile rather than scaling the complete arched component. Detailed LOD identities
match by measured station and bounds, allowing the recorded 0.001953125 m endpoint
simplification in LOD1. Original frame-mode output remains byte-identical.

LOD0 preserves 2196 original vertices without extra vertices or split faces. LOD1
preserves 998 original vertices and adds 232 edge vertices across 124 source faces.
Native and widened front views were inspected. Source area, four-width winding,
rigid shoulders, original bone ownership and disjointness from completed maps pass.
No structural materials or original channels are replaced.

The actual production Plan constructor also accepts composition with the U-frame
plans in eight C# cases. Combined extra-vertex counts are 228/528; source-triangle
fragment addresses remain valid after concatenation. This verifies data-plan assembly,
not native GPU import, animation or completion of the remaining geometry.

Release compilation and twelve-language checks pass. Nine detailed merged crossing
bodies, far merged bodies and final generation integration remain unfinished. This
helper increment is not deployed and does not change existing generated bridges.

## Dual-deck object and paint assembly (2026-10-20)

`Lift03ObjectAssembly.Create` now assembles the prepared main, column and footing
render roots in native order and applies the recorded mechanism components. Its
24 fixed tower attachments (four warning lights and twenty wall lights) retain
native rotations, parent/group/probability fields and measured lateral side
membership. The structural half-width delta translates their anchors and adjusts
the recorded StandingObject leg envelope. Original attachment sources are subject
to the existing foreign-bridge source exclusion.

The selected objects of both decks are transformed into closed mechanism space and
bound in one operation. This preserves the original road-bone connection mapping
and avoids resetting the moving-lamp clone-name counter for the second deck. Source
placement records remain unchanged. The final generator still needs to supply the
completed geometry and coordinated sampling result to this stage.

`MovablePaintDeck` supplies a deck's decals, pose, physical partition and receiver
triangles to the shared paint writer. The existing single-deck entry point wraps one
such input. Lift03 supplies two inputs: each is projected only against its own deck,
then both triangle lists are written as one native skinned paint render with one
appearance resource set. This avoids duplicate paint resource names and avoids
selecting the other deck's receiver by proximity. Both use fixed bone 0 and moving
road bone 3; original stack and skeleton data, single-sided geometry and the existing
0.001 m clearance remain intact. No runtime synchronization is added.

Release compilation, 29-route structure checks and twelve-language checks pass.
A normalized source comparison against the pre-edit backup confirms unchanged
single-deck projection/partition/triangulation and persistence bodies; this is a
compatibility review, not native execution or visual validation. No user-visible
text was introduced. These helpers remain unrouted and undeployed pending complete
Lift03 structure and generator integration. Motion, receiver placement, game resource
registration and cold-load persistence are still unverified.

## Native footing geometry at all LODs (2026-10-20)

The six native column/footing files were decoded and hash-matched to the original
asset audit. Plan views distinguish the paired solid footing slabs from the separate
column lobes and their cross-centre joining walls. The two slab identities persist
at every LOD despite reversed component ordering and asymmetric far simplification.
They must not be confused with the six fixed support shells in the main mechanism.

`Lift03Footings.py` now emits `Lift03FootingGeometry` for the two solid foundations.
Each slab stretches across its own measured min/max X span, moving the two extremes
outward by the requested half-width delta. Exact native spans are retained separately
for both end stations and each LOD; far simplification is not forced to the near span.
Original vertices, indices and Y/Z coordinates remain unchanged except for X widening.
There is no road-adjacent-base sign transform, column scaling or new subdivision here.

| Footing LOD | Vertices | Original/result triangles |
| --- | ---: | ---: |
| 0 | 2152 | 2608 |
| 1 | 2256 | 2086 |
| 2 | 424 | 400 |

Four half-width deltas (-1, 0, 2, 8 m) pass finite mapping, identity-at-zero,
unchanged Y/Z and nondegenerate winding checks. Native and widened plan views were
inspected at all three LODs. The footprint scales continuously without replacing
coarse triangles with detailed geometry. These checks do not establish game acceptance.

`Lift03FootingAssembly` applies the plans through the existing render-graph builder,
retains each native Surface and stack configuration, and updates X bounds from the
result. Release compilation and twelve-language checks pass. No user-visible text
was added. Column geometry, main fixed shells, far main structure and final Lift03
generator integration are still unfinished; this helper increment is not deployed.

## Column side bodies and transverse walls (2026-10-20)

`Lift03Columns.py` emits immutable signed vertex ranges for the native column at all
three LODs. Near and middle meshes have twelve disconnected components: eight side
bodies/strips and four U-shaped transverse wall groups. A whole U-shaped component
must not scale: its longitudinal returns translate with the side columns, while the
front/back crossing faces extend between their endpoints. The 80 recorded centre
seam vertices in each detailed LOD retain their original tiny authored X offset.

The far mesh welds each end station into one component. Its 48 cross-centre faces
still belong to the same thin front/back walls, with maximum longitudinal extent
below 0.6 m. None belongs to the longitudinal side columns. The near/middle meshes
have 80 crossing faces each, with maximum longitudinal extent 0.357421875 m. The
far level contains no centre-seam vertex; its walls extend directly between the
translated endpoints. Plan-view comparisons confirm the same part interpretation
at every LOD. Runtime performs no coordinate or component classification.

| Column LOD | Vertices | Original/result triangles |
| --- | ---: | ---: |
| 0 | 1816 | 1288 |
| 1 | 1302 | 772 |
| 2 | 708 | 500 |

Four half-width deltas (-1, 0, 2, 8 m) preserve original Y/Z, same-side triangle
edge vectors and nondegenerate winding. Native and widened plan views were inspected.
The current negative-delta bound is -7.1646728515625 m, the nearest far side vertex;
this preserves separated side bodies and is an explicit remaining narrowing limit.
It must not be reported as unrestricted input compatibility.

`Lift03ColumnAssembly` consumes the recorded render inputs and preserves original
Surface slots, authored Y/Z bounds and native stack behavior. X bounds follow the
new positions. Release compilation and twelve-language checks pass. This stage
adds no user-facing text and is not yet called by a completed Lift03 generator.
Main fixed support shells, main far structure, final dual-deck integration and native
operation remain unfinished. No installed assets or healthy saved bridges changed.

## Joined-shell validation increment (2026-10-20)

The detailed joined-shell candidate covers the two fixed end shells and the moving
central shell (near component IDs 3, 716 and 3815). Their curved transverse ribs
connect longitudinal side rails and a central longitudinal beam. The measured
near central beam outer faces lie at X = +/-2.08642578125 m in all three shells.
The emitted opening profile retains this central connecting span where the curved
opening closes; it does not replace a zero denominator with an epsilon. Middle LOD
inherits the near beam boundary despite coarse welding.

`Lift03JoinedShells` retains rigid side longitudinal faces and records constructed
edge/interior vertices for crossing profile boundaries. Its production `Plan`
constructor was exercised with half-width deltas -0.25, 0, 2 and 8 m at both
levels. All eight cases retain source arrays, original Y/Z and finite output.
These checks validate construction recipes, not native GPU attribute import.

| Level | Added edge vertices | Added interior vertices | Replaced source faces |
| --- | ---: | ---: | ---: |
| LOD0 | 13356 | 1140 | 1492 |
| LOD1 | 14930 | 1216 | 1200 |

Original and widened front views were inspected at both levels. Side arms retain
compatible silhouettes and the bottom connecting region extends between them.
The number of constructed vertices is substantial: topology cost and composition
with other plans remain review items before final integration. Release compilation
and all twelve localization checks pass; there are no new user-visible strings.
This candidate is not yet called by a complete Lift03 generator or deployed.

The six fixed support shells remain separate, unresolved parts. Three-dimensional
inspection confirms that their longitudinally varying surfaces cannot be classified
by the joined-shell front-opening profile alone. Their part-specific mapping, the
five far crossing assemblies and complete dual-deck generation remain unfinished.

### Removal of redundant profile subdivisions (2026-10-20)

The joined-shell emitter now removes collinear opening-profile samples before
subdividing crossing faces. Both left and right boundaries must agree with linear
interpolation within 1e-10 m at a removed sample; the complete reduced profile is
then checked against every original knot within 1e-9 m. This removes no original
mesh vertices or authored profile corners and changes no other emitted family.
It only avoids new cuts at redundant heights, including the constant central-beam
boundary. The earlier construction counts above are superseded by:

| Level | Added edge vertices | Added interior vertices | Replaced source faces | Result shell triangles |
| --- | ---: | ---: | ---: | ---: |
| LOD0 | 2488 | 152 | 900 | 24784 |
| LOD1 | 3362 | 152 | 800 | 17610 |

Total constructed vertices fall from 14496 to 2640 at LOD0 and from 16146 to 3514
at LOD1. Original-face area, four-width winding, rigid-side translation, finite
coordinates and the production Plan constructor checks still pass. Regenerated
original/widened front views retain the measured silhouettes. Release compilation,
29 generator route checks and all twelve localization checks pass. No user-facing
strings changed. Native attribute import, full part-plan composition and final
generation remain unverified; this helper-only change is not deployed.

## Detailed part-plan composition (2026-10-20)

`Lift03DetailedStructure.Create` now combines the existing rigid parts, independently
sized upper/lower road bases and crossing members, U-frames, end beams and joined
shells. It removes only the recorded detailed native road tops. Fixed support shells
and selected-sidewalk inclusion are still required before a final render caller can
use this stage; LOD2 is explicitly outside its scope.

Each constituent plan numbers edge vertices before interior vertices. Composition
therefore remaps these two domains separately: all combined edge recipes precede
all combined interior recipes. Fragment indices and constructed X values follow the
same mapping. Original parent vertex indices and interpolation weights remain intact,
so native attribute interpolation still refers to each original source triangle.
The final plan uses the fully transformed original positions, not an earlier snapshot
captured before a later family ran. Duplicate fragment ownership is rejected by the
existing Plan constructor; overlap with recorded road-top removals is rejected here.

An isolated harness links the production composition and generated tables. At both
detailed LODs and four structural deltas (-0.25, 0, 2, 8 m), it compares every fragment
vertex position and attribute-construction recipe against the constituent plans.
Upper and lower deltas are independently exercised at 0.75 and 0.5 of the structural
delta. All eight cases preserve source arrays and indices, produce disjoint road
removal/structural replacement masks, and retain every original transformed vertex.
Combined construction counts are 2716 edge plus 152 interior vertices for LOD0,
and 3890 edge plus 152 interior vertices for LOD1.

Release compilation and all twelve localization checks pass; no user-visible text
was introduced. This validates composition and addressing, not native attribute
upload or full bridge generation. The stage is not yet routed or deployed, and
healthy generated assets remain unchanged.

## Tall fixed support shells (2026-10-20)

The paired tall fixed supports (components 102 and 815 at both detailed levels)
now have a separate measured recipe, `Lift03TallSupports`. They are not grouped
with the four longitudinally sloped end supports. Both inner vertical walls lie
at X = +/-5.3037109375 m, from Y = 7.781982421875 to 13.1591796875 m.
Each near wall has 108 native triangles; each middle wall has 20. Their mirrored
longitudinal bounds and height endpoints agree across LODs.

Those inner walls delimit the bottom transverse connecting bowl. Below their
opening, that connection uses the measured wall-to-wall span; higher curved side
arms follow the native opening profile. Collinear profile samples are removed
without changing the measured boundary. Side longitudinal faces at and outside
the inner wall retain signed rigid translation; the connecting region widens.
This is a part-specific recipe, not a zero-denominator fallback for all supports.

The two detailed levels preserve 3964/2180 original vertices and add 456/360
constructed vertices, replacing 152/124 source faces. Resulting support triangle
counts are 3248/1674. Source-face area, winding at four widths, unchanged Y/Z,
rigid longitudinal sides and disjoint ownership checks pass. Original/widened
front views were inspected at both levels.

`Lift03DetailedStructure` now includes this family. The production-plan composition
harness again compares every fragment position and original attribute recipe at
both LODs and four widths, using different upper/lower deck deltas. All eight cases
pass. Combined counts are now 3156 edge plus 168 interior vertices at LOD0 and
4234 edge plus 168 interior vertices at LOD1. Release compilation and all twelve
localization checks pass; no user-visible text changed.

The four sloped fixed support shells, far structure and complete generation remain
unfinished. This detailed helper is not a complete Lift03 route or an in-game
acceptance result. No installed binary or healthy saved bridge was changed.

## Sloped supports and detailed membership coverage (2026-10-20)

`Lift03SlopedSupports` now covers the remaining four detailed fixed-support shells.
The two families have distinct inner longitudinal wall planes at X = +/-9.994384765625
and +/-11.77099609375 m. These native faces extend from Y = 3.371826171875 to
10.58642578125 m at both detailed levels. Their opposite longitudinal slope directions
and original Y/Z remain intact. The outer wall bands translate rigidly; each transverse
connection widens between its own measured inner-wall limits. Runtime consumes literal
vertex weights and interpolation recipes, with no position-based part classification.

The emitter checks all vertices at or outside each wall boundary for exact signed
translation, preserves original-face area and passes four-width winding checks.
Original and widened front views were inspected for both families and detailed LODs.
The new family covers 2832/1496 original vertices, adds 516/408 edge vertices (no
interior vertices), and replaces 272/218 source faces. The separate tall-support
recipe remains unchanged.

Detailed main-mesh membership is now exhaustive and exclusive: the rigid parts,
deck bases, crossing members, frames, end beams, joined shells, tall supports and
sloped supports together assign every one of the 474534/349267 original vertices
exactly once. This proves width-map coverage, not selected-road compatibility or
correct native rendering. The far main mesh still needs its own complete recipe.

The production combination harness passes all eight LOD/width cases, comparing
every fragment's positions and native interpolation recipe with its constituent
plan and checking disjoint road-top removal. Combined construction now contains
3672 edge plus 168 interior vertices at LOD0 and 4642 edge plus 168 interior vertices
at LOD1. Release compilation and all twelve localization checks pass. There is no
new user-facing text or deployment. Selected-sidewalk inclusion, far geometry,
complete generation integration and native verification remain outstanding.

## Far road-top audit and removal recipe (2026-10-20)

The hash-pinned native far main mesh has 996 vertices and 852 triangles. Its three
joined shells (components 3, 4 and 5) contain sixteen upward horizontal upper-road
faces at Y = 20.02392578125 m, with X limits +/-9.935546875 m. Eight belong to fixed
bone 0 and eight to moving road bone 3. This top is 0.22412109375 m above the detailed
upper carriageway, so retaining it beneath the replacement would cause occlusion.

`Lift03FarRoadTops.py` decodes the original native payload, verifies its source hash,
checks the OBJ face ordering and bone records, and emits exact original triangle
triples in `Lift03FarRoadTops`. The runtime removal checks native index count and
triangle identity before setting those sixteen mask entries. It does not remove
bottom faces, side shells or the separate far support components.

No independent horizontal lower carriageway was found in the joined far shells.
The two fixed support components instead include broad surfaces at Y = 11.28271484375 m,
above the detailed lower road at Y = 9.800048828125 m. These cannot simply be kept
under a newly added lower road. Their merged shape needs a measured reconstruction
that retains the detailed support openings. Removing the sixteen upper faces alone
is not a complete far-road conversion and is not routed into generation yet.

Release compilation and twelve-language checks pass; no user-facing strings changed.
Native near/far acceptance remains outstanding and this helper has not been deployed.

## Far fixed-support supplement (2026-10-20)

`Lift03SupportFaces.py` now records the six middle-source support families and the
two obstructing native far shells from hash-pinned decoded payloads. It validates
original OBJ ordering and fixed bone 0. The middle selection contains exactly 2406
source triangles, excludes every recorded road top, and the far removal contains
exactly 32 triangles. Runtime selection uses recorded triangle triples rather than
position, normal, material or name classification.

`Lift03FarSupports.CreateSupplement` applies the already measured tall/sloped width
plans to those middle-source supports only. It reuses the detailed composition's
edge/interior remapping and is intended for the existing native structural Supplement
path, retaining original source attributes and Surface. The eventual far assembler
must remove the corresponding native far faces when attaching the supplement; this
helper alone is not a completed or connected far render implementation.

At four tested widths the supplement produces 3190 triangles and 768 constructed
vertices. Checks confirm that no unrelated middle-source face or fragment is retained.
Detailed composition regression checks also pass at both LODs and four widths after
extracting the shared Lift03 composition method. The source support opening shapes
are unchanged from the reviewed middle support recipes. This is a measured higher-LOD
supplement, not a claim of mesh simplification: the cost increases over the native
32-face support proxy and remains part of the final geometry/resource budget review.

Release compilation and all twelve localization checks pass. No user-visible text,
installed binary or existing saved bridge changed. Far joined-shell mapping, lower-road
clearance, final supplement attachment and full Lift03 generation remain unfinished.

## Far joined-shell opening candidate (2026-10-20)

The three native far joined shells contain 132 end-cap triangles, 44 per shell.
Their cap stations are +/-80.00390625 and +/-127.96484375 m for the fixed shells,
and +/-79.9609375 m for the moving shell. The caps fill the space below the upper
road down to the simplified bottom, so replacing only the separate fixed supports
cannot expose the lower carriageway.

An offline candidate now separates the measured near inner-opening profile from
the width-deformation boundary. The former removes cap interiors; the latter retains
the central beam's measured span for width mapping. Using the clamped width boundary
as the physical opening produced an artificial bottom step and was rejected. The
revised candidate retains the actual curved inner bottom. Collinear knots in both
profiles are removed before splitting native faces, reducing unnecessary subdivisions.

The current candidate retains 3004 joined-shell triangles, constructs 2848 vertices,
and has 196 source-face fragment replacements plus the sixteen already recorded
upper-road removals. Four tested width deltas (-0.25, 0, 2, 8 m) retain nondegenerate
winding. Original-width and widened front views were inspected. The candidate is
still an offline research result: native Plan recipe validation, original attribute
interpolation, complete far assembly and final render integration remain outstanding.
It has not been emitted into the runtime generator or deployed. The earlier fixed
support supplement remains separate and must be included in final complexity totals.

## Far structure emission and three-level render assembly (2026-10-20)

`Lift03FarStructure.py` now emits the reviewed far-shell candidate into the runtime
`Lift03FarStructure` plan. It re-decodes and hash-checks the native far geometry,
separates measured opening boundaries from width boundaries, and writes literal
vertex weights plus native edge/interior interpolation recipes. Runtime retains
all original vertices, applies the existing twelve rigid side-assembly rules, removes
the recorded upper-road and old support faces, and substitutes the measured cap
fragments. Edge vertices are numbered before interior vertices.

The actual production Plan constructor passes four tested deltas (-0.25, 0, 2, 8 m).
It yields 3532 main-mesh triangles with 2248 edge and 600 interior vertices. Checks
preserve source arrays, original Y/Z and native interpolation parent coordinates;
all positions are finite. The 196 fragment replacements are disjoint from the 16
upper-road and 32 support removals. Together with the 3190-triangle middle-source
support supplement, far structural output is 6722 triangles before selected roads
or markings. This is not the complete bridge resource-size measurement.

`Lift03RenderAssembly.CreateMechanism` now connects the detailed main/glass plans
and far main/support plans through the common native render builder. It requires
three reconstructed source levels with native mesh counts 2/2/1. The support plan
is attached once through the existing Supplement path, preserving its middle source
Surface and fixed bone 0; far shell fragments retain the original far Surface and
native channels. Private source meshes are released on both success and failure.

This render stage expects both prepared selected decks in mechanism coordinates.
It does not yet have a complete generator caller. Dual-deck preparation/lifecycle,
selected-sidewalk adaptation, unrestricted narrowing and native generation/persistence
verification remain outstanding. Release compilation, 29 route checks and all twelve
localization checks pass. No user-visible text, installed binaries or healthy saved
bridges were changed by this helper integration.

## Prepared dual-deck level composition (2026-10-20)

`Lift03RoadLevels.Combine` now joins the outputs of the two `PrepareDeck` calls,
and `Lift03RenderAssembly.CreateMechanism` accepts both deck level lists directly.
Each deck's evaluated geometry enters mechanism coordinates through its own pose;
the combined level uses an identity pose. UVs, color, tangent handedness, source
triangle IDs and original Surface references remain unchanged. Upper and lower
road material slots are concatenated, as are the separate lane material slots.
Road occurrence indices and lane occurrence indices are offset independently using
their respective partition-array lengths. Physical partitions remain in mechanism
coordinates and retain the original bone ownership.

The combiner operates before structural plans and paint attachment. It rejects
inputs carrying structure, projectors or triangle-specific binding overrides rather
than silently discarding them. Both decks must share the mechanism source and closed
world transform. The resulting bounds include both already prepared deck bounds.

The offline `work/bridge-research/lift03-deck-check` harness links the production
combiner and road geometry/binding code, extracts the production Level definition,
and substitutes only resource/container types. Six deck/LOD combinations cover
distinct deck heights, identity and reversed auxiliary orientation, unequal road
and lane occurrence counts, material remapping, UV/provenance preservation, original
bone binding, bounds and source immutability. These checks pass; they do not exercise
native resource initialization or GPU rendering. Release compilation, 29 route
checks and twelve-language localization checks also pass. No user-visible strings
were added or changed.

Lift03 still has no complete generator caller. The shared movable lifecycle currently
receives only one selected network, while the outer composition attaches the second
deck after generator execution. Coordinated sampling requires passing both selected
networks through that lifecycle and preserving one owned auxiliary network instead
of attaching a second replacement afterwards. This integration, selected-sidewalk
adaptation, narrowing compatibility and native persistence verification remain open.
This helper increment was not deployed and does not change existing saved bridges.

## Ordinary pillar source reconstruction (2026-10-20)

`Lift03PillarSources.py` now emits all nine native render inputs for object 118758:
three LODs each for NetPillarTop, NetPillar and NetPillarBase. The generated
`Lift03PillarSources` preserves geometry/Surface CIDs, native bounds, LOD bias,
stack ordering and scaling flags, default-base references, DLC prerequisites,
object mesh transforms and PillarObject parameters. It does not require the original
network/object Prefab. It uses the existing source-resource resolver and owned-output
factory path; width-adjusted render graphs are still required before CreateObject.

The native foundation LOD1 (79692) carries its own BaseProperties component, unlike
the ordinary Lift02 pillar. This is explicitly preserved along with the root's
BaseProperties. The source emitter validates exact per-level component membership
against the hash-pinned audit rather than applying another style's component layout.

Nine native geometry payloads were independently extracted from their recorded game
packages, SHA-256 checked and decoded for offline review. Triangle counts are:

| Part | LOD0 | LOD1 | LOD2 |
| --- | ---: | ---: | ---: |
| Top | 8088 | 4852 | 400 |
| Column | 144 | 115 | 100 |
| Foundation | 748 | 447 | 100 |

The reviewed original meshes show a joined U-shaped top, a continuous capsule-like
column and a continuous foundation. They are not the separated side columns of
Lift02. Reusing Lift02's sign-only displacement would therefore be incorrect; the
Lift03-specific crossing connectors, curved sides and top opening still require
measured width plans across all LODs. Research artifacts are
`work/bridge-research/lift03-netpillar-inventory.json`, the nine decoded NPZ inputs,
and `lift03-native-netpillar.png`. These establish source topology, not generated
appearance acceptance. Release compilation and all twelve localization checks pass;
there are no new user-visible strings. No deployed binaries or saved bridges changed.

## Ordinary column width plan (2026-10-20)

`Lift03NetColumnGeometry.py` emits the three ordinary-column plans, independently
of the joined top and foundation. The near native side-arc junction is at
X=+/-3.3563232421875 m. At Y=+/-5 m its Z values are +/-3.45721435546875 m;
the central connector reaches Z=+/-3.47113037109375 m at X=0. The near intermediate
connector vertices at X=+/-3.02069091796875 m retain their interpolated position.
The two outside arcs translate rigidly; the centre connector changes span. Neither
the complete column nor its curved sides are uniformly scaled.

The same measured boundary applies at all three LODs. Native near/middle topology
already separates these regions, retaining 144/115 triangles with no added vertices.
The far source mixes the regions, so boundary interpolation adds 32 edge vertices
and produces 132 triangles from 100. Original vertices, Y/Z, source surface and
native interpolation channels remain retained through the existing Plan path.
The plan rejects a collapsed connector (half-width delta at or below
-3.3563232421875 m); unrestricted narrow-road compatibility remains unfinished.

Offline checks cover delta -1, 0, 2 and 8 m: original-width area, winding, side
rigidity, finite positions and Y/Z preservation. A C# harness runs the generated
plan with the production Plan definition at all twelve LOD/width combinations,
checking interpolation recipes and unchanged source arrays. Original and widened
three-level views were inspected in `lift03-netcolumn-width-review.png`. Release
compilation passes. This change adds no user-visible strings or locale keys and
preserves the existing localization path. It is not deployed. The ordinary top
and foundation plans, complete pillar assembly and Lift03 generator remain pending.

## Ordinary foundation connection plan (2026-10-20)

`Lift03NetFootingGeometry` now preserves the two side lobes and widens their central
connection at all three LODs. The measured top rim shares the column boundary
X=+/-3.3563232421875 m. A trial whole-foundation scaling was rejected because its
rim would no longer follow the translated column contour. The emitted final plan
retains side shape and uses native interpolation recipes at the connection boundary.
It does not modify Y/Z, surface references or stacking metadata.

The final triangle counts are 948/609/160, from native 748/447/100. Boundary splits
add 200/162/60 edge vertices respectively; original source vertices are retained.
Four tested deltas (-1, 0, 2, 8 m) pass area-at-zero, winding and rigid-side checks.
Twelve actual Plan-construction cases also pass source-immutability and interpolation
checks. Eight additional C# cases compare the production column and foundation plans
at matching near/middle rim stations and confirm no separation after deformation.
The native detailed levels share 66 and 38 distinct contour stations respectively;
this is not a claim that independently simplified far rims have identical topology.

The final original/widened views were inspected in `lift03-netfooting-width-review.png`.
Release and twelve-language checks pass. This adds no localized UI strings. The
joined top opening plan, whole-pillar integration, unrestricted narrowing and final
Lift03 generator remain unfinished; this increment was not deployed.

## Ordinary U-shaped top and complete pillar assembly (2026-10-20)

`Lift03NetTopGeometry.py` measures the near source's actual U-shaped opening and
emits its side/connection width boundary. The bottom connects to the measured
column at X=+/-3.3563232421875 m; higher portions follow the measured inner profile.
Redundant collinear profile knots are removed. All three LODs inherit that measured
boundary. Entire rigid side triangles remain untouched by subdivision, preserving
the coarse native arm chords rather than replacing their interiors with a detailed
curve. Required crossing triangles retain original-channel edge/interior recipes.

The final top triangle counts are 8572/5340/884, from native 8088/4852/400. Added
edge/interior vertices are 468/8, 470/9 and 392/46. Offline area, winding, Y/Z and
rigid-side checks pass at deltas -0.25, 0, 2 and 8 m. Twelve C# cases run the generated
recipes through the production Plan definition and check source immutability and
interpolation coordinates. Original and widened views at all three LODs were
inspected in `lift03-nettop-width-review.png`. These checks do not establish native
rendering, unrestricted narrowing or full bridge clearance acceptance.

`Lift03NetPillarAssembly` now combines the top, column and foundation plans with all
nine reconstructed source levels and creates the recorded native PillarObject.
Each part retains its own original Surface, source metadata and LOD graph. Bounds
are updated from the resulting X coordinates; authored Y/Z bounds and stacking
behavior remain. The complete source/part assembly compiles; localization behavior
is unchanged and twelve-language checks pass. Lift03 still needs its concrete
shared-lifecycle generator and catalogue route, selected-layout compatibility and
native persistence validation. This increment has not been deployed.

## Independent far road-base supplement (2026-10-20)

Inspection of the actual far source shows that components 3/4/5 merge the two deck
regions into coarse shells. They have no separate upper/lower base identities.
The measured middle bases (65/66, 778/779, 2826/2827) retain their own bounds:
upper Y=19.200195..20.399902 m and lower Y=9.399902..10.100098 m. The source
comparison was inspected in lift03-far-deck-comparison.png.

Lift03FarDeckBases.py pins the middle Geometry hash and emits exact topology for
1868 base faces. The existing detailed road-top exclusion removes 570 of them,
leaving 1298 side/underside faces. The shared middle-source far supplement now
includes these bases with their independent upper/lower deltas, original UVs,
Surface and native fixed/moving bone indices. It reuses the same supplement mesh
as the six fixed supports; no additional texture or material is created. The
supplement now contains 4488 triangles instead of 3190; 768 interpolation vertices
remain unchanged. Main far-shell geometry is unchanged.

Five offline independent width pairs pass winding and Y/Z checks. The production
Plan harness passes eight detailed LOD/width cases and four combined supplement
cases, checking retained-face scope, original source immutability, fragment channel
recipes and equality with the detailed base-width mapping. Release compilation
passes. This does not establish shell/base intersection clearance, selected-sidewalk
adaptation, native motion, resource-size parity or cold-load acceptance. No UI text
changes; localization behavior remains unchanged. This increment is not deployed.

## Prototype track replacement (2026-10-20)

The lower-deck native rails, guard rails within each track, sleepers, longitudinal
track fittings and fasteners were inspected in both detailed sources. The local
review image lift03-track-identification.png shows the two track assemblies in
plan and cross-section. The selected membership is 5786 complete components at
each level: 198808/163824 vertices and 130080/92620 triangles. The surrounding
outer railing, full-width transition members, deck bases and supporting structure
are excluded. These are offline source identifications, not runtime bounds filters.

Lift03TrackRemoval.py verifies source hashes against the native audit, exact OBJ
triangle ordering, complete-component membership, base exclusion and the recorded
fixed/moving bones 0/3. It emits 152 immutable triangle ranges with topology hashes
per level. DetailedStructure removes those prototype track faces after road-top
replacement, so selected native lane geometry is the only track source. Original
source vertices and channels are retained; no new road cuts or runtime dependence
are introduced. Far geometry has no separate track components and is unchanged.

Release compilation and eight production detailed-plan cases pass, including
fragment/removal disjointness and original source preservation; four far supplement
regressions also pass. All 30 route checks and twelve UI-language checks pass. This
adds no user-visible text. Native rendering and selected-road behavior still require
the deferred batch verification; sidewalk adaptation and narrow-road compatibility
remain incomplete. No deployment or saved-bridge modification occurred.

## Separate deck-edge safety railing widths (2026-10-20)

The native main network contains Highway Drive/Median/Shoulder/Side sections;
its auxiliary contains Train Track/Median/Shoulder/Side sections. Neither contains
a Sidewalk-family section. A piece named Highway Shoulder To Sidewalk does not
turn its parent Highway Shoulder into the required Sidewalk family. This evidence
does not exempt selected sidewalk layouts from compatibility checks, nor does it
justify deleting native safety railings or machinery maintenance platforms.

The two detailed sources contain 100 upper deck-edge railing components and 374
lower components. Their measured native bands are upper X=+/-10.2907715..10.406006,
Y=20.386719..21.17627 m and lower X=+/-5.1761475..5.291382,
Y=10.100098..11.793945 m. The identification view was inspected in
lift03-deck-railing-review.png. These are complete rigid rail/posts, not deck bases
or tower decorations. Their original triangle counts are 5104/4254 upper and
10776/10244 lower for the two detailed LODs.

Lift03DeckRailings.py emits exact source vertex ranges. DetailedStructure applies
the corresponding upper/lower delta after the generic rigid-side map, correcting
the prior use of the common structure delta for both decks. Side shape, Y/Z,
topology, materials and bone indices are unchanged. Runtime contains no spatial
classification. Four independent-width pairs preserve each complete part's rigid
translation. Eight production detailed-plan cases and four supplement regressions
pass; Release compilation passes. No user-visible strings change. Far merged-shell
railing representation and selected-sidewalk clearance remain pending, so this is
not a claim of complete all-LOD compatibility or native acceptance. Not deployed.

## Column provenance correction and direct decode check (2026-10-20)

An integration audit found that Lift03Columns.py checked the unrelated LiftBridge02
column file while consuming the correctly named Lift03 support arrays. This was a
source-validation defect, not evidence that the resulting vertex map came from
Lift02. The emitter now pins the three actual LiftBridge03Pillar Geometry hashes
and independently pins decoded position and index byte hashes for each level.
The native PipelinePlugin decoder was run directly against all three source files;
every decoded position and triangle index equals the previously measured arrays.

Correct raw hashes are 9b7bd06b4b0cb3fde25d376504d798e62447b4c42bac137290751866a1cc1976,
910e1f71683f0ed479fde02b894498fd31c191ffb243819898295eaa3da8bc91 and
c4f8c29fbda4a2a51f8ae3b6c6f6eec0befc6a99eb99a17420685d2a85dc2e34.
Regeneration passes all three LOD/four-width checks. Comparing non-comment generated
C# lines confirms that the runtime map is unchanged. The incorrect LiftBridge02
header and hash comments are corrected. Release compilation passes; this change
has no user-visible text and does not change localization or installed assets.
Full native generation and independent persistence validation remain pending.

## Signed narrowing policy alignment (2026-10-20)

The current bridge contract's base/centre-crossing rule explicitly preserves the
requested signed width mapping and reports crossings rather than holding the whole
tower back or stopping a side at the centre. Lift03's earlier geometry helpers had
extra positive-output-span guards that rejected such combinations before generation.
Those guards are removed from mechanism columns/footings, transverse members and
all three ordinary pillar parts, in both emitters and emitted C#. Source topology,
LOD identity and finite-input validation remain unchanged. Native denominators are
constant measured source spans; no division by a generated zero span is introduced.

The concrete generator records the measured ordinary-connector or transverse-member
centre-crossing conditions with the actual upper/lower/structure deltas in the
technical export report. It does not clamp, scale side bodies, replace a prototype,
or alter the requested road width. Negative or collapsed output spans can naturally
produce overlap, inversion or zero-area faces; this is not represented as clearance
acceptance. Existing base maps still apply their own complete deck delta.

Production ordinary-pillar Plan checks now include delta -4 m and the exact measured
-3.3563232421875 m contact boundary, in addition to prior widths: 54 part/LOD/width
cases and twelve actual column/foundation joint checks pass. They verify source
immutability, channel interpolation, retained topology and matching joints, not
nonintersection at extreme narrowing. Release compilation passes. The new text is
a technical report diagnostic; no player UI label or localization key is added.
Native batch validation, selected sidewalks and far-shell clearance remain pending.
No installed files or saved bridges changed.
