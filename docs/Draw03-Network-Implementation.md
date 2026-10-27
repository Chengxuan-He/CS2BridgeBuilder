# Draw03 network integration data

## Far replacement-road clearance (2026-10-20)

The far emitter now checks the complete composed 536-triangle output after each
of its four width transforms, rather than checking only source clipping. All 140
retained deck-level upward triangles at Y=10.356689453125 m stay wholly outside
X=+/-(12.5 + half-width delta), within source interpolation tolerance of 2 micrometres.
Checks pass at half-width deltas -0.1, 0, 2 and 8 m. Thus the retained far shoulder
does not cover the selected carriageway or sidewalk in these cases. The far shell
has no independent raised joint counterpart to add or separately resize.

Regeneration with the stronger checks produces text-identical runtime tables.
This does not prove arbitrary narrow widths or native LOD transitions. An additional
near-source component inventory found no complete independent component above the
sidewalk within the tested interior corridor; this is supporting identification,
not proof that every composite railing/support is already sidewalk-compatible.
The check changes no UI text, native material, persistence or existing asset.

## Selected-sidewalk joint adaptation (2026-10-20)

`Draw03JointLayout.py` now emits the near/middle joint maps and verifies native,
absent, left-only, right-only, unequal and widened sidewalk layouts. The runtime
`Draw03JointLayout.Apply` is connected after detailed structural construction and
road replacement. It receives the same `MovableSelectedRoadLayout` used by road
appearance/preparation through `Draw03RenderAssembly` and the generator caller.

Each raised end follows its own selected sidewalk span, including the measured
inset within the native 3.5 m section. When absent, only that side's 14 faces per
joint are removed and the carriageway extends to the road edge. Carriageway faces
are never removed. Source centre quantization residues remain stationary. All
original Y/Z, bone ownership, UV channels and face topology are retained. Existing
constructed structural vertices and removal masks are preserved when composing
with the structural plan. No additional runtime geometry classification is used.

Near/middle maps cover 292/284 vertices across the four joint strips. Six offline
layout checks preserve winding and carriageway geometry; native-layout positions
are unchanged. Both LODs were inspected in six-layout section views. Release and
12-language checks pass. These checks do not establish native visual acceptance.
The far structure has its separate existing plan and receives no new detailed joint
geometry. Far compatibility, other sidewalk-dependent structure, narrowing and
complete native generation/persistence validation remain open. No deployment or
existing-asset modification was performed.

## Stepped sidewalk joint identification (2026-10-20)

`Draw03SidewalkJoints.py` identifies four complete transverse joint strips in each
of the near/middle meshes. Two belong to moving bones 1/2 and two to fixed bone 0.
Each includes a carriageway-height centre and two raised sidewalk ends; they are
not independent sidewalk-only slabs. The retained source components are near
705/1479/2176/2948 and middle 745/1519/2264/3036 in the recorded part inventory.

Each side contributes 14 triangles. The centre contributes 12 near / 10 middle
triangles, giving 160/152 total joint triangles. Vertex ownership is exclusive to
these components, and none of their faces overlap the existing road-removal mask.
The sidewalk top is Y=10.208984375 m, with an inner step at |X|=9.389892578125 m.
The carriageway bevel extends to |X|=9.407958984375 m at its lower edge; this
0.01806640625 m bevel must not be mistaken for an additional sidewalk width.

The current own-span end-strip transform still stretches these sidewalk ends with
the whole bridge. A replacement must map each side to the selected sidewalk span,
remove only that side's raised faces when absent, and retain/adapt the carriageway
joint. Whole-component removal would delete road geometry. Bone ownership must stay
unchanged. The identification command validates both geometry hashes, face ownership,
height bounds and original bones and emits a reproducible offline face map. Runtime
layout adaptation and far-LOD correspondence remain unfinished. This identification
adds no UI text or runtime behavior and does not establish game acceptance.

## Generator and catalogue route (2026-10-20)

`Draw03BridgeGenerator` now connects the reconstructed source graph, closed-pose
sampling, road preparation, all-LOD mechanism structure, paint, stacking supports,
ordinary pillars, fixed approaches and native network assembly. The source road
width is 25 m and the closed road frame is Y=10 m. Sampling replaces Bottom and
Side layers. Column and footing use the Draw03 support API's explicit false/true
footing argument, rather than another style's opposite boolean convention.

The generator uses the shared movable lifecycle for both preview and creation,
retains native aggregate `Bridge` and icon `Media/Game/Icons/DrawBridge03.svg`, and
applies recorded Draw03 unlock inputs. `BridgeGeneratorRouter` has an exact Draw03
route. `BridgeStyleCatalog` creates its recorded variant from the built-in
BridgesAndPorts content prerequisite, without requiring the original DrawBridge03
network Prefab. Existing localized Draw03 labels and UI actions are retained.

Release compilation, all 24 generator-route source checks, 32 recorded-variant
checks and 12-language localization checks pass. Those checks do not execute native
sampling, animation, persistence or cold loading. The route is incremental: selected
sidewalk compatibility, narrower-road geometry and complete graph-size verification
remain open. Native game verification is deferred to the requested batch. No game
binary was replaced and no existing generated asset was changed by this increment.

## Approach layout and render assembly (2026-10-20)

Current correction (2026-10-21): the first-strip-only widening described below is
superseded by the user's complete fixed-truss stretch requirement. Families 0–3
scale local X and native piece widths by `1 + halfWidthDelta / 12.5`; family 4
retains its width. Native layout distributes the scale across all structural
strips, with no centre-gap expansion. The old central-brace override is no longer
called. All three LODs retain source topology, Y/Z, materials and native behavior.
The updated extraction validates all fifteen meshes and eighteen layout cases.
These checks establish geometry invariants, not in-game visual acceptance.

`Draw03ApproachLayout.py` records six structural sections and all 33 native
occurrence branches from network 118597. It preserves occurrence order, inverted
sides, median and half-length flags, offsets, hidden layers and native requirement
arrays, adding Elevated to the parallel structural layer. Five sections use the
owned transformed approach pieces; the zero-width median uses the original built-in
`Elevated Bottom Piece 0 - Intersection Middle` with its Median/Elevated and
Intersection/MedianBreak conditions.

Each original section's intersection-only road subsection is verified against its
recorded native ID and deliberately not duplicated in this structural layer. Selected
road/intersection sections continue to supply lanes, median and pedestrian surfaces.
The generated sections contain only the measured bridge structure. No original
Draw03 network Prefab lookup is needed.

`Draw03ApproachAssembly.Apply` now reconstructs all fifteen render inputs, applies
`Draw03ApproachGeometry`, creates factory-owned native render graphs, and assigns
the recorded parallel layout. Only family 0 gains half-width delta in its piece
width; its local geometry compensates for the resulting half-delta origin shift.
All outer families keep their native widths. Source Y/Z bounds, surface slots,
LOD properties, tiling flags and native piece fees are retained by the existing
render-graph pipeline. Temporary meshes and source holders are disposed.

Release compilation, localization and generator-structure checks pass. The existing
23 routes are unchanged: this assembler still requires the final Draw03 generator
caller. Selected-sidewalk compatibility and narrow-width limitations remain open;
no native-game acceptance is claimed or requested before the bridge batch.

## Approach base and side geometry (2026-10-20)

`Draw03ApproachGeometry.py` now emits the five-family, three-LOD geometry plan.
The connected first-strip shell includes the road-bottom base/end closure and
attached longitudinal members. Its measured bridge-centre vertices stay at X=0;
all positive-side shell vertices translate by the half-width delta. This applies
the base sign mapping instead of scaling the entire connected shell. It also keeps
side longitudinal members and the independent connector bodies rigid. The central
V braces use their separate own-span maps. All other strip meshes retain their
local coordinates and depend on the native section cursor for rigid translation.

The first-strip origin must move from 1.5 to 1.5 + delta/2, and only its width
increases by delta. The other four positive-side origins move by delta. Mirrored
occurrences produce the opposite side. These placement requirements are still to
be wired through the approach layout/assembler, not inferred at runtime.

The first shell has 605/410/120 vertices and 164/100/22 centre vertices at LOD0/1/2.
The nearest non-centre detailed shell coordinate is X=2 m. The plan rejects
half-width delta <= -2 m rather than publishing an intersecting base; unrestricted
narrow-road compatibility remains unfinished. End-only triangles number 16/14/8,
with 289/192/96 remaining shell triangles in the first strip. All fifteen original
mesh vertex and triangle counts are preserved; no subdivisions are introduced.

Four-width checks (-0.1/0/2/8 m half-width delta) verify centre placement, rigid-side
translation, unchanged Y/Z and nondegenerate face winding. Complete native/widened
cross-section views were inspected for all three LODs. Release compilation passes.
Existing native surface references and source channels remain unchanged. No UI text
or persistence format changed; localization checks pass. Layout, selected-sidewalk
compatibility and the final Draw03 generation route are still incomplete.

## Central approach braces (2026-10-20)

`Draw03ApproachBraces.py` identifies the sixteen central V braces from the native
first-strip geometry and emits immutable vertex ranges for all three LODs.
Near/middle contain one half-brace component per station; far contains two planar
profiles per station, representing the same complete mirrored brace. The near/middle
half-span is 2.626800537109375 m; far retains its authored 2.62939453125 m extent.

The transform operates in first-strip local coordinates. Its native positive-half
origin is X=1.5 m. When the strip width increases by the bridge half-width delta,
its origin moves by delta/2; the emitted local transform subtracts that displacement
so the complete brace still stretches about the bridge centre according to its own
span. The native inverted occurrence supplies the opposite half. The eventual layout
caller must retain this origin contract. No prototype Prefab lookup or runtime
geometric classification is introduced.

| LOD | First-strip brace vertices | Unchanged brace triangles |
| --- | ---: | ---: |
| 0 | 768 | 384 |
| 1 | 512 | 256 |
| 2 | 128 | 64 |

Native geometry hashes are checked against the recorded source audit. Offline checks
at half-width deltas -0.1, 0, 2 and 8 m verify placement, unchanged Y/Z, untouched
non-brace vertices and valid face winding. Native and widened brace views were inspected
at every LOD. Release compilation and localization checks pass. This stage has no new
user-visible text and does not alter existing assets or persistence.

The combined first/second/last strips also contain a connected end/longitudinal shell
spanning X=-9 to +9 m. Connectivity alone must not classify that entire shell as one
stretchable part. Its end closures and longitudinal members still require separate
mapping before the full approach assembler and final Draw03 route can be connected.
Human native-game validation remains deferred to the bridge batch.

## Ordinary pillar assembly and approach inputs (2026-10-20)

`Draw03FarNetPillars.py` records the native merged far top/column shells. The detailed
side-part envelopes are X=+/-4.672607421875 m for the top rim and
X=+/-0.373199462890625 m for the side-brace endpoints. Far faces crossing these
boundaries are subdivided offline; original vertices and surfaces remain intact,
and new edge vertices retain interpolated source channels. Outside regions translate
rigidly while the merged central connection region extends between the boundaries.

| Far part | Original vertices | Added edge vertices | Split original faces | Original triangles | Result triangles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Top | 388 | 50 | 23 | 300 | 350 |
| Column | 677 | 54 | 21 | 400 | 454 |

Source area, Y/Z, rigid outer regions and valid winding pass at half-width deltas
-0.1, 0, 2 and 8 m. Original and +16 m total-width far views were inspected offline.
The narrowed range remains bounded by the recorded side-part clearance.
`Draw03PillarAssembly.cs` now connects these plans, detailed top/column plans and
all-LOD rigid footing plans into the recorded native `PillarObject` recipe. It
retains native Y/Z stacking bounds and expands only the lateral bounds.

`Draw03ApproachSources.py` independently records native piece IDs 79451, 79457,
79454, 79448 and 79460 (first/second/last bottom strips, sidewalk bottom edge and
outer side). Its generated source reconstructs fifteen render inputs and preserves
native layers, dimensions, surface heights, height ranges, LOD settings, content,
asymmetry, fees and `NetPieceTiling.m_DisableTextureTiling`. In particular, these
pieces disable texture tiling, unlike an assumed generic repeating road strip.
No Draw02 section layout or dimensions are reused.

The approach sources are input reconstruction only. Their geometry transformation
and selected-road layout still require implementation before final Draw03 routing.
Release and localization checks pass. No runtime UI strings or existing saved assets
are changed; native game acceptance remains deferred to the bridge batch.

## Ordinary network pillar inputs and parts (2026-10-20)

The native network references `DrawBridge03NetPillar` (record 118751), distinct from
the mechanism's stacking column and footing. `Draw03PillarSources.py` reconstructs
its nine renders and validates all component sets and object mesh placements against
the hashed native audit. `Draw03PillarSources.g.cs` preserves original geometry/surface
CIDs, LOD bias, shadow bias, stack directions/orders/overlaps, forbid-scaling flags,
base properties, content requirements and `PillarObject` behavior. It does not depend
on the original object Prefab being installed. The native geometry and surfaces remain
required resources.

`Draw03NetPillarParts.py` records exact authored-part membership from the decoded
meshes. Side parts translate rigidly. The centre-crossing top beam and three central
column connection parts stretch according to their own spans; the two footing bodies
remain separate rigid pieces at every LOD. Original triangles and channels are retained.

| Part | LOD | Vertices | Triangles | Identified components |
| --- | ---: | ---: | ---: | ---: |
| Top | 0 | 2,480 | 2,888 | 23 |
| Top | 1 | 3,412 | 1,731 | 25 |
| Column | 0 | 2,036 | 1,552 | 35 |
| Column | 1 | 1,811 | 931 | 35 |
| Footing | 0 | 1,114 | 1,144 | 2 |
| Footing | 1 | 981 | 914 | 2 |
| Footing | 2 | 147 | 100 | 2 |

Source hashes match the native asset audit. Four-width checks retain Y/Z, rigid-side
edges and valid winding; original and +16 m total-width detailed views were inspected.
The column's closest independent side part is 0.373199 m from the centre, so unrestricted
narrowing is not established. The generated plan rejects collapsed/intersecting spans.
The merged far top (388 vertices/300 triangles) and column (677 vertices/400 triangles)
still require separate maps before ordinary-pillar assembly and final routing. This
stage changes no existing saved assets or localization keys. Release compilation passes;
game acceptance remains part of the deferred batch.

## Three-level support assembly (2026-10-20)

`Draw03SupportAssembly.cs` connects the measured near/middle support plans and
`Draw03FarSupports.g.cs` to the shared native render graph. It requires reconstructed
Draw03 column/footing sources and all three original LODs, with their original single
surface slots. It adjusts only X bounds; native Y/Z bounds and stacking metadata remain
unchanged. Native materials, UVs and other vertex channels follow the existing graph
writer. No original prototype network Prefab is needed by this stage.

`Draw03FarSupports.py` records the detailed inner side-body envelopes at
X=+/-11.728759765625 m (column moulding) and +/-10.161376953125 m (footing toe).
All detailed side-body vertices lie outside these envelopes. The native coarse shell
merges parts across these boundaries, so only crossing faces are subdivided. Outer
regions translate rigidly; the central connecting region extends between the two
boundaries. The coarse profile remains the native coarse profile, including its
authored simplification offsets; it is not replaced by the near mesh.

| Far support | Original vertices | Added edge vertices | Split source faces | Original triangles | Result triangles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Column | 854 | 178 | 73 | 800 | 994 |
| Footing | 158 | 120 | 30 | 200 | 320 |

Original vertices and source materials remain present. Added vertices interpolate
their original edge channels through `MovableStructureModel`; runtime code consumes
literal edge fractions and fragment indices, without intersections or classification.
Checks at half-width deltas -0.5, 0, 2 and 8 m preserve source area at subdivision,
Y/Z coordinates, valid triangle winding and rigid outer-region translation. Native
and +16 m total-width far views were inspected offline. Release compilation passes.
No existing saved asset, persistence path or UI text changes through this stage.
Final generator routing, ordinary approach/pillar assembly and selected-road width/
sidewalk compatibility still require implementation. Game acceptance remains deferred.

## Road appearance and preparation (2026-10-20)

`Draw03RoadAppearance.cs` defines 24 appearance patches: moving/fixed-inner/fixed-outer,
negative/positive end, carriageway/sidewalk and left/right side. Motion still uses
the original three bones through `Draw03RoadPartitions`; patch indices are only
appearance identifiers. `Draw03RoadPreparation.cs` passes both measured physical
partitions and the selected per-side sidewalk layout into the shared conversion
pipeline for every native mechanism LOD.

The source road atlas contains painted lane lines. Repeating the entire original
road rectangle would duplicate those markings beneath the independently skinned
selected-road paint. The inspected paint-free asphalt crop occupies native pixels
[3084,4052) x [1312,1476), with a bottom-left UV origin in the 4096x4096 atlas.
The 968x164 unit preserves the original atlas UV/metre gradient; it is not stretched
to the selected carriageway width. Existing repeating-surface persistence handles
the resulting native material. No custom shader or runtime motion patch is introduced.

Sidewalk patches retain their measured atlas rectangle and 3.10009765625 m
transverse period. Each side anchors to its own selected outer edge; absence and
asymmetry are handled by the shared selected-road occurrence layout. This does not
yet resolve unrelated bridge-authored fixed sidewalk geometry.

Moving and inner fixed units have an 8 m longitudinal period. Outer fixed units
span 88.041015625 to 96 m and retain their distinct 7.958984375 m period. Appearance
seams are transformed into the sampled-road frame; physical gaps remain unchanged.

`Draw03RoadAppearanceAudit.py` directly decodes native Float32 positions, Float16
UVs and original bone indices through the native pipeline decoder. It verifies
186 top faces: transverse UV agreement is below 1e-7, while maximum longitudinal
period phase deviation is 0.000593642558 (below one source texel), reflecting the
authored coordinate/UV quantization. The original geometry hash is enforced.
The clean crop was inspected visually offline. Release compilation passes.
There are no new localized UI strings. These helpers are not a complete generator;
far support mapping, structural sidewalk compatibility and final routing remain open,
with game verification deferred to the requested bridge batch.

## Detailed support geometry (2026-10-20)

`Draw03DetailedSupports.py` records the original column and footing near/middle
meshes by verified geometry hash. `Draw03DetailedSupports.g.cs` consumes immutable
vertex ranges; it does not classify geometry at runtime. Two side bodies translate
rigidly, while the native centre-to-side panels extend with their own edge spans.
The column's centre residues (-0.00000095367431640625 and
-0.0000019073486328125 m) and the footing's exact zero vertices stay stationary.
This avoids mistakenly translating a central seam to the negative side.

| Support | LOD | Original vertices | Unchanged triangles | Stationary centre vertices |
| --- | --- | ---: | ---: | ---: |
| Column | 0 | 3,856 | 4,056 | 144 |
| Column | 1 | 4,105 | 2,433 | 140 |
| Footing | 0 | 1,152 | 1,408 | 72 |
| Footing | 1 | 1,768 | 844 | 80 |

Offline checks at half-width deltas -0.5, 0, 2 and 8 m preserve side-face edges,
centre vertices, Y/Z coordinates and valid triangle winding. Native and +16 m
total-width offline views were inspected. No material, UV, index, bone or persistence
path is changed. The runtime plan preserves the original channel pipeline.

The coarse supports merge central panel boundaries and require a separate measured
mapping. Detailed plans reject LOD2 instead of applying their vertex ranges to it.
Support render assembly, final Draw03 routing and game acceptance remain incomplete.
There is no new user-visible text; existing localization behavior remains applicable.

### Coarse support boundary audit

`Draw03SupportProfiles.py` verifies all six native geometry hashes and extracts
source-indexed central panel faces, boundary vertices/segments and full-span coarse
faces into the research file `draw03-support-panel-profiles.json`.

| Support | Near centre-panel faces | Middle centre-panel faces | Far full-span faces |
| --- | ---: | ---: | ---: |
| Column | 176 | 127 | 37 |
| Footing | 112 | 80 | 30 |

Neither detailed level has a face spanning both nonzero sides of the centre seam.
Both far meshes omit all detailed centre vertices. The column's far inner wall
vertices also move to approximately X=+/-9.6604 at Z=83.205078125, whereas its
detailed inner column boundary is X=+/-12.1201171875 at that wall. Consequently a
sign-only far mapping would deform full-span faces, and copying detailed vertex
ranges is invalid. This evidence must inform the separate far boundary recipe;
the audit does not authorize a uniform scale, inferred runtime classification or
claim that far support generation is finished.

`Draw03NetworkData.g.cs` records the independently measured DrawBridge03 network from game 1.6.2f1. Source audit identity is recorded in the generated source. The emitter compared all 33 section entries as eleven independent three-branch groups before applying those branch rules to selected-road occurrences.

The implementation preserves selected-road sections and their own condition groups through the shared subsection converter. It also applies the recorded Bridge fields, appends native edge/node states to a private selected-road clone, and returns the four measured object placements with caller-supplied private pillar/mechanism and selected-road outside connection.

Draw03 has segment length 152, water elevation 10, and two pillar placements at native Y=-6.550000190734863 with Z=5 and Z=0. These are independent measurements; Draw01/Draw02 offsets are not reused. Every placement rotation, mode and flag is retained.

The source was authored on `bridge/draw-03` in an isolated worktree. The unchanged shared subsection converter is supplied from dev for compilation, without copying another bridge's uncommitted source. Only this new Draw03 source and document belong to the style commit.

The measured data now has a network assembly caller, described below. Structure replacement, selected-road surface/marking conversion, complete routing, persistence and human motion/traffic acceptance remain incomplete. No existing bridge output changes through this stage before full generator integration, so no bridge cleanup is required.

## Network assembly stage (2026-10-19)

`Draw03NetworkAssembly.Apply` composes the recorded section conversion, Bridge fields,
edge/node mechanism states and structural placements on the private selected-network clone.
It requires factory-owned pillar and mechanism objects and adopts generated section assets
through the existing persistence ownership path. The two original pillar offsets and shared
`MechanismPlacement` method are retained; sampling and final placement use the same method.

`StructurePlacements` deliberately excludes the prototype's outside-connection entry.
The selected network retains its own connection object, position, rotation and conditions;
the prototype road's Y=5 anchor is not imposed on another selected network. Existing
non-elevated attachments also survive, while elevated support placements are replaced.
Existing selected-network edge/node state arrays are extended rather than overwritten.

Release compilation, localization checks for all twelve languages and the 23-route structural
check pass. No user-visible text was added. This is not a complete Draw03 generator: main
and support render construction, road/paint conversion,
unlock/catalogue integration and final routing still need completion. No in-game validation
or deployment was performed for this uncalled assembly stage.

## Attachments, measured road width and barriers

`Draw03ObjectAssembly` now constructs the mechanism object from prepared main, column
and footing render roots. It uses the original component recipe and expands StandingObject's
leg width by twice the half-width delta. Twelve structural attachments (two flagpoles and
ten wall lights) retain their rotations, groups, probability and fixed parent connection 0;
their X coordinates follow their original side. Eight native lamps are kept separately as
placement evidence. Selected-road lamp types use the four distinct stations -60, -20, 20
and 60 m, without duplicating a station for each original lateral lamp. Road furniture and
paint receive the measured Draw03 carriageway partition and the shared road-to-object pose.

The attachment split was compared with native audit record 30008: all twenty positions,
rotations, parent connections, groups and probabilities are unchanged. Original lamp
Prefabs are not resolved merely to obtain station coordinates. All-LOD render assembly
and generator integration remain outstanding.

The old 78 m road-width entry counted the three mutually exclusive section branches as
parallel strips. The audited network 118597 contains eleven lateral groups, each repeated
for three conditions. Native pieces 79451/79454/79457 have width 3 m and piece 79448 has
width 3.5 m: six carriageways plus two sidewalks give 25 m. The two 0.5 m outward strips
(piece 79460) are excluded from road-surface width. Main-mesh sidewalk tops end at X=+/-12.5 m,
providing an independent geometric cross-check. `DrawBridge03` now uses this 25 m value;
unmeasured BXP variants are not changed. This affects new variant selection/width fitting,
not saved bridge geometry or UUID/CID identities.

Native sidewalk section 118596 references piece 120789. Its NetPieceObjects record 72428
contains the LevelCrossing producer for `BasculeBridgeBarrier01 Placeholder`. The assembly
now invokes the shared barrier-rule conversion before adopting the created section graph,
retaining selected-road placement and native node ownership. No runtime synchronization
is added. Localization remains unchanged; the recorded-variant checks now include Draw03's
measured width without claiming that its catalogue or generator route is complete.

## Native road motion boundaries

`Draw03RoadPartitions.py` verifies the original near Geometry hash and reads its native
triangle/bone records. At each of Y=10 and Y=10.199951171875 m, forty top faces belong
to each moving leaf. Their hinge ends lie at Z=+/-80.015625 m; the fixed top faces begin
at +/-80.041015625 m. The centre half-gap is 0.02740478515625 m for carriageway and
0.02741241455078125 m for sidewalk. These independent Draw03 measurements are not
inferred from skeleton bounds, which also contain structures beyond the road edges.

The emitted binary partitions leave both hinge gaps and the central seam empty, with
fixed bone index 0, positive leaf index 1 (connection 101) and negative leaf index 2
(connection 100). All generated LODs will use these same physical seams while retaining
selected-road heights. Object assembly now consumes this style-owned partition directly.
The sidewalk partition is available for road preparation; the full road-material/geometry
pipeline still needs integration. No structural-removal mask is inferred from these planes.

## Independent control-room glazing

`Draw03Glass.py` verifies each native Geometry hash and the separate glass submesh's
face ownership before emitting immutable vertex ranges. Near LOD has 704 vertices and
360 triangles in four side ranges; middle LOD has 160 vertices and 88 triangles in five
ranges. Every face stays wholly on one side, and all faces use fixed bone 0. Runtime
mapping applies recorded signed translations rather than classifying vertices by position.
The native far model has only its main material slot and no independent glass submesh.

`Draw03Glass.Create` returns a structure plan with unchanged source indices and no removed
faces or inserted vertices. Original channels remain available to the shared raw-channel
mesh builder. Four half-width deltas (-0.5, 0, 2, 8 m) preserve every triangle edge and
Y/Z coordinate; near/middle native and widened offline plots were inspected. These checks
do not establish in-game transparency or LOD transitions. The plan awaits the complete
Draw03 render assembler; it does not add a generation route. No user-visible strings
were introduced, and existing localization behavior is unchanged.

## Independent main-mesh side parts

`Draw03RigidParts.py` identifies exact-position-connected components offline, keeping
different original bone domains separate. Whole components strictly on one side of the
centre receive immutable signed-displacement ranges. After excluding the split positive
axle described below, the near mesh has 2,802 such parts with 130,422 vertices; middle
has 2,890 parts with 98,439 vertices. Every middle side
component fits inside a detailed side component's measured envelope on the same side
and bone. Its higher component count reflects disconnected simplified fragments and
does not change their displacement rule.

The far mesh has two independent fixed control-room components, each with 39 vertices.
Its remaining four components combine fixed bases and moving decks and are excluded
from this stage. Detailed LODs each retain 176 crossing components for subsequent
own-span/base mapping and replacement-road membership review. Side classification is
not a road-removal mask; selected-road surfaces and sidewalk adaptation still require
their own handling before assembly.

The generated runtime table contains 349/356/2 vertex ranges. At four half-width deltas
(-0.5, 0, 2, 8 m), all source triangle edges retain their shape under this stage, crossing
vertices remain unchanged, and bones and source indices are untouched. Three-LOD native
and widened offline plots were inspected with unmapped crossing geometry highlighted.
This isolated stage does not establish a complete widened bridge or material/LOD acceptance.
Release and localization checks pass; full structural/render integration remains open.

## Rollers and full-width leaf axles

The positive-leaf axle is disconnected at a 0.000049591064453125 m source coordinate
seam: its left half ends at X=-0.00002288818359375 m and right half begins at
X=0.000026702880859375 m. The negative-leaf counterpart is connected. Offline inspection
of the cylindrical parts therefore supersedes raw connected-component side classification:
both positive halves are excluded from rigid side ranges and handled as one full-width axle.
No source vertex is welded or moved to close that original coordinate seam.

`Draw03Shafts.py` emits six own-span maps: four fixed rollers (crossing records 4, 5,
92, 93), the negative leaf axle (91), and the positive axle assembled from near parts
268/270 or middle parts 272/274. Fixed rollers span 24.5615234375 m; each leaf axle spans
X=+/-13.215576171875 m. The six parts retain bones 0/0/0/0/2/1 and cover 23,016 near
and 16,937 middle vertices. Each part stretches over its own recorded span, preserving
native Y/Z, source triangles and all non-position channels. Four-width checks verify full
face ownership, unchanged bone membership and positive winding. This resolves the detached
axle-halves error in the preliminary side classification; full structure assembly and the
merged far representation still require implementation.

## Central members and connection plates

`Draw03CentreMembers.py` emits immutable own-span maps for 152 central members
per detailed/middle mesh, covering 8,388 and 7,287 vertices respectively.
The selection uses crossing records 9–73, 75–83, 86/87 and their counterparts
offset by 88 in the hash-checked source inventory. Road bodies and road replacement
surfaces remain separate.
The fixed end members retain bone 0; the positive and negative moving members retain
bones 1 and 2. No road is transferred onto another bone.

Near/middle correspondence compares each member's six-coordinate envelope. Maximum
deviation is 0.005859375 m; X endpoints differ only by one source quantization step
(0.00000762939453125 m) on records 36/124. Each runtime map uses its own LOD's exact
span rather than a global bridge width. No runtime geometry classification is introduced.

Offline checks at half-width deltas -0.1, 0, 2 and 8 m verify complete-face membership,
unchanged Y/Z, endpoint displacement and positive triangle winding. The smallest native
span is 0.5532913208007812 m, so a half-width delta at or below -0.2766456604003906 m
would collapse or invert that member. This remains a narrowing design issue, not evidence
that arbitrary selected widths are supported. The runtime guard rejects non-positive spans.

Release compilation, the 23-route structure check and all twelve localization checks pass.
No user-facing text, persistence format or existing generator call path changes in this stage.
Full Draw03 render construction, merged far correspondence and generator integration remain
unfinished. These checks do not establish in-game motion or appearance acceptance.

The four additional connected structures are longitudinal frames 9/97 and end housings
37/125. Offline source inspection identifies a complete centre-crossing frame or housing
in each record; each uses its own span, without merging it with adjacent members.
Frame X bounds are -0.808868408203125 to 0.809417724609375 m; housing bounds are
-0.8680572509765625 to 0.87255859375 m. Negative-leaf records mirror these bounds.
Near/middle bounds match exactly. These four records contribute 2,824 near and 2,639
middle vertices, all on their original leaf bones. Original and widened offline views
were inspected for both representations; the open frame and housing profiles remain
intact. The four-width geometry checks and Release/localization checks pass after inclusion.

## Road-adjacent base bodies

`Draw03Bases.py` records crossing components 0, 1, 3, 88 and 90: two positive-leaf
base halves, the negative-leaf base and the two fixed end bases. These are the bodies
immediately below the road, spanning X=+/-12.5 m and Y=7.562744140625 to approximately
9.96924 m. They are not the lower fixed support platforms. Near/middle bounds match
exactly, despite different tessellation. The tables cover 19,436/19,858 vertices and
retain original bones 1/1/0/2/0 and complete source triangle membership.

The original centre seam contains X residues from +/-0.00000762939453125 through
+/-0.000049591064453125 m. Those measured vertices are explicitly recorded in stationary
centre ranges; their positions are not snapped or welded. All other selected vertices
translate left or right by the half-width delta. Runtime code uses literal ranges,
without coordinate classification, and leaves every non-position channel unchanged.

Checks at deltas -0.1, 0, 2 and 8 m retain Y/Z, centre positions and positive triangle
winding. Native and +2 m offline plan views were inspected for both LODs. The initial
-0.5 m test produced 48 near and 28 middle inverted faces: the nearest non-centre
vertices are only 0.24432373046875 m from the centre. A runtime guard prevents crossing
that distance; unrestricted narrowing remains unfinished and must not be reported as
supported. This limitation is additional to the central-member span limitation.

Release compilation and all twelve localization checks pass. These uncalled geometry
stages do not modify existing generated assets, persistence, migration or cleanup.
Far merged shells, complete render assembly and route integration remain outstanding.

## End strips and main-mesh coverage

`Draw03EndStrips.py` records crossing components 7, 8, 84, 85 and their negative
counterparts 95, 96, 172, 173. They are the narrow transverse strips at the leaf hinges,
leaf centre seams and fixed outer ends. Native inspection distinguishes them from the
broad road tops and the lower fixed platform body. Their own X spans are 25 m,
25.42138671875 m and 25.4296875 m; near/middle envelopes match exactly. Each strip
retains its original bone (1/1/0/0/2/2/0/0), Y/Z and triangle ownership.

The maps cover 540 near and 512 middle vertices (296/276 triangles). Checks at half-width
deltas -0.5, 0, 2 and 8 m preserve winding and move the two ends by the requested delta.
This supplies structural width mapping only: selected-sidewalk height/edge compatibility
still needs to be resolved when constructing the replacement road surfaces.

An offline full-source partition check accounts for every main-mesh vertex exactly once,
and every triangle lies entirely within one group:

| Group | Near vertices / triangles | Middle vertices / triangles |
| --- | --- | --- |
| Rigid side parts | 130,422 / 83,716 | 98,439 / 52,026 |
| Rollers and axles | 23,016 / 18,944 | 16,937 / 6,533 |
| Central members and frames | 8,388 / 5,600 | 7,287 / 4,600 |
| Road-adjacent bases | 19,436 / 14,296 | 19,858 / 10,242 |
| End strips | 540 / 296 | 512 / 276 |
| Remaining components | 4,276 / 4,296 | 3,955 / 2,707 |

The remaining components are crossing records 2, 6, 74 and their mirrored counterparts
89, 94, 162. Record 6 is a fixed support/platform with substantial lateral legs and
rounded end structures; stretching its whole connected mesh would not preserve those
parts. Records 2/74 contain native road regions and require surface/curb identification
before selected-road replacement. This coverage audit is not a claim of complete render
assembly or all-width support. Release and twelve-language localization checks pass;
no new user-facing text or persistence changes were introduced.

## Fixed support platforms

`Draw03FixedPlatforms.py` now handles records 6/94 separately from the road-adjacent
bases. Source plan and elevation views identify the central wall's original panel edges
at X=+/-10.37841796875 m. The detailed mesh already has complete rings there, including
the front/back wall planes at |Z|=83.205078125 and 88 m. Lateral shoulders, rounded
ends and support legs retain their shapes under rigid translation. The connecting wall
uses its own measured span; the entire connected platform is not uniformly stretched.

The mapping covers 3,236 near and 2,951 middle original vertices, retaining fixed bone 0.
No near faces cross the authored boundaries. Twenty middle triangles merge across those
same boundaries; the emitted recipe subdivides only those faces and adds thirty edge
vertices. Their channels use the existing structural-plan interpolation path. No runtime
intersection, part classification, replacement material or source-channel reconstruction
is introduced. Every other original position is left to its owning mapping stage.

The emitter checks source hashes, complete-face ownership, original bone membership,
subdivision area and winding on all platform faces at half-width deltas -0.5, 0, 2 and
8 m. Native and +8 m plan views were inspected for both LODs: lateral round ends and
legs retain their profiles, and the connecting wall remains continuous. Release compilation
and twelve-language localization checks pass. This is still an uncalled assembly stage;
native road regions, merged far structures and complete generator integration remain open.

## Detailed structural composition and native road removal

`Draw03RoadReplacement.py` records the complete original moving road/curb regions
(crossing records 74/162, 128 faces each) and the in-corridor fixed road/curb regions
(records 2/89, 28 faces each). Both detailed LODs therefore remove 312 faces. These
regions occupy |X| <= 12.5 m between Y=10 and 10.199951171875 m. Each fixed region
starts at |Z|=80.041015625 m. Selection is offline; emitted runtime records validate
source triangle indices and never infer membership from coordinates or materials.

The fixed records also contain exterior edges. None of their triangles crosses the
road/exterior boundary. They retain 260 near / 158 middle faces and 416/380 vertices,
translated rigidly to their original side. No platform, underside or lower base face is
removed. Native plan views distinguish removed road from retained exterior edges.
The replacement plan preserves previous structural fragments and constructed positions,
and rejects overlapping removal/fragments rather than silently restoring old road faces.

`Draw03DetailedStructure.Create` now composes rigid parts, shafts, central members,
bases, end strips, fixed platforms and road removal for LOD0/LOD1. The offline combined
audit confirms disjoint mapping of all surviving original vertices: 185,454 near and
146,364 middle. Middle additionally retains thirty interpolated platform vertices.
Resulting structural triangle counts are 126,836 and 76,102, excluding replacement
road and paint. Full-source and subdivided face winding remain positive at half-width
deltas -0.1, 0, 2 and 8 m; Y/Z remain unchanged.

Release and twelve-language localization checks pass. There is still no complete Draw03
generator route: replacement-road appearance, selected-sidewalk compatibility, narrowing,
far and support render assembly remain unfinished. This composition is not evidence of
native rendering, motion, persistence or cold-load acceptance.

## Far structure and mechanism render assembly

`Draw03FarStructure.py` consumes the original 684-vertex, 448-triangle far main mesh.
Its six parts are two fixed platform shells, two side control rooms and two moving
leaf shells. Native bones remain 0/0/0/0/2/1. Control rooms translate rigidly; the fixed
platforms use the detailed central wall boundary at X=+/-10.37841796875 m. Their lateral
supports and rounded ends translate rigidly. Each moving proxy uses its own original
half-width (12.992919921875 or 12.923583984375 m), preserving native asymmetry and Y/Z.

Forty-eight broad top faces at Y=10.356689453125 contain road and exterior shoulders.
Only their interiors inside X=+/-12.5 m are removed; each retains measured outer fragments.
Constructed shoulder-edge vertices translate rigidly with the side, preserving shoulder
width. Fixed platform faces spanning the central-wall boundary receive separate necessary
subdivision. The complete recipe contains eighty fragment replacements and 160 interpolated
edge vertices, yielding 536 structural triangles before replacement road/paint. Original
far surfaces and channels remain in use; no detailed geometry or glass is inserted into far.

Source hashes, bone ownership, retained exterior membership, partition area, Y/Z and
positive winding pass at deltas -0.1, 0, 2 and 8 m. Native-width and +8 m offline views
were inspected. The far top's original height does not replace the physical near road
datum or motion seams used by selected-road preparation.

`Draw03RenderAssembly.CreateMechanism` now accepts three prepared native road levels,
checks reconstructed-source identities and supplies mesh-slot counts 2/2/1 to the shared
render assembly. Near/middle main meshes use `Draw03DetailedStructure`, far uses its own
plan, and native glass slots use `Draw03Glass`. The common pipeline retains original
structural materials and skeletons while adding separately prepared road and paint.
Release compilation and twelve-language localization checks pass. Road preparation,
support renders, selected-sidewalk/narrow-width compatibility and final routing remain
unfinished; this is not native appearance, motion or persistence acceptance.

## Detailed joint runtime sidewalk checks (2026-10-20)

The shared `tools/MovableSidewalkCheck` harness now links production
`Draw03JointLayout` alongside the selected-road layout resolver. Fixtures are
created by `Draw03SidewalkCheckInputs.py` from native mesh caches and the reviewed
`draw03-sidewalk-joint-map.json`, rather than emitted C# membership tables.

Twenty-four checks cover both detailed LODs, six native/absent/one-sided/unequal/
widened layouts and forward/reversed frames. Exact sidewalk removal masks match
the measured faces; carriageway faces remain. Unrelated vertices, Y/Z, finite X
and original indices are verified. All checks pass. This verifies these existing
runtime branches without requiring production changes. The harness substitutes
lightweight containers, not Unity's native renderer or mesh conversion; far LOD,
other composite parts and whole-bridge acceptance are not established by it.

Reproduce after generating the Draw02 fixtures used by the same harness:

```powershell
python tools/GeometryMetaprogram/Draw03SidewalkCheckInputs.py <research-directory>
dotnet run --project tools/MovableSidewalkCheck -c Release -- <research-directory>
```

No user-facing text or localization keys changed. No installed binaries or saved
assets were modified.

## Fixed mechanism end-truss correction (2026-10-22)

The approach piece meshes are not the complete fixed structure. The main mechanism
also contains 92 bone-zero end-truss components in each detailed LOD: longitudinal
beams, diagonal members and riveted bearing/connection plates. These were previously
left on rigid side translation or independent centre-member stretching. They now
use the same `x + (x / 12.5) * delta` mapping as the complete deck truss, through
`Draw03Truss.py` and its DLL-embedded membership ranges. This adds 4,680 near and
4,239 middle vertices without changing their bone, Y/Z, UVs, materials or topology.
The far native mesh represents these details as one merged support shell; it contains
no separate bearing plates or longitudinal end-truss components. Its existing continuous
shell mapping is retained rather than injecting detailed geometry into that LOD.

Regression checks cover all 92 components at multiple positive-span widths as well
as the five moving longitudinal frames. New generation consumes this correction;
existing saved geometry is not rewritten. In-game near/far acceptance remains manual.
No user-facing strings change, so existing localization remains intact.

## Native light road-wear bands (2026-10-22)

Draw03 now keeps the native light lane-wear profile instead of using only the
uniform asphalt crop. The measured source interval is atlas columns 3084..3380,
rows 818..1194 (4096 square texture, bottom-left UV origin). It excludes white
paint. The two lateral minima bound a 3.10781622 m native lane-profile period,
converted using face 58054's measured 9.399904251098633 m / V-span relation.
The light interior and dark margins are copied from the native material channels;
no invented white stripes, additional receiver geometry or runtime shader is used.

The private native road sample records visible, non-master, straight CarLane
centres. Draw03 repositions one fixed-width source profile per lane. Overlapping
profiles on closely spaced lanes are clipped at the midpoint, not narrowed.
The complete repeat texture covers the measured road surface span so parking or
wide margins do not wrap into additional wear lanes. Sidewalk domains keep their
previous material. Longitudinal native texel scale is retained. All detailed/far
road receivers share the same repeat resources through the existing appearance
pipeline and normal owner-local persistence. Existing saved bridges are unchanged.

`Draw03WearCheck` verifies native lateral scale, centres, contiguous atlas rows,
no-traffic behavior and sidewalk exclusion across multiple lane layouts. The
existing horizontal RepeatBand path remains the default for other styles. The new
BandRows path composes vertical bands without cutting triangles or adding batches.
This implementation is scoped to Draw03's measured native texture; other archetypes
must supply their own measured profile before enabling the effect. No localized
UI strings change. In-game color, motion, LOD and cold-load acceptance are pending.

### Tiled asphalt between wear bands (2026-10-22)

The dark four-row native asphalt crop previously mapped once across each complete
inter-lane gap, stretching its texels. RepeatBand now carries an explicit Y repeat
count. Draw03 computes this count from gap length and the same measured atlas-V per
metre gradient as the wear profile. GPU composition repeats the crop, including a
fractional final tile, and allocates texel density including all repetitions.
No triangle subdivision or per-tile dispatch is required. Other callers default to
one repetition. Wear width, lane centres, native material channels and LOD sharing
are unchanged. Draw03WearCheck verifies the gap and wear gradients match at multiple
lane spacings. No user-visible text changes; game visual acceptance remains pending.

### Inter-wear stripe regression (2026-10-22)

The preceding four-row crop failed visual acceptance: correct repeat density
alone does not prevent a four-row strip from degenerating to a single repeated
row under mip filtering. This creates correlated longitudinal streaks that look
like stretching. Replace that crop with the measured 296-by-32 dark interior at
native columns 3084..3380, rows 802..834 (bottom-left UV origin). Preserve the
same native density and fractional repeat handling. The complete wear profiles,
their widths and their lane centres are unchanged.

The crop height is stored in the existing embedded typed-array resource. The
new pixel regression `tools/Draw03WearCheck/verify-texture.py` reads the exact
decoded native texture, verifies its SHA-256, excludes white paint, and tests
transverse grain after mip-2 filtering. The former four-row crop has zero
transverse variation at that level; the new unit retains grain. This supplements
the UV/layout regression, which alone did not catch the defect. No new runtime
shader, receiver cuts, user-visible text or changes to the accepted rail repair
are introduced. Actual game appearance and cold loading remain manual checks.
