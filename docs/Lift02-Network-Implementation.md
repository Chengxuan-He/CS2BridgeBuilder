# Lift02 support implementation

## Measured side bodies (2026-10-20)

The three native column meshes contain two longitudinally separated shells. Each
shell consists of two rounded lateral bodies joined by a central wall. Detailed
side bodies terminate at X=+/-2.60003662109375 m. The simplified far mesh retains
two distinct sets of side vertices, with its nearest point at X=-2.4110107421875 m.
No column vertices lie inside the central gap.

`Lift02Columns.py` emits immutable vertex ranges from the decoded native files.
Each lateral body translates rigidly by the half-width delta; the existing central
wall triangles extend between their original side endpoints. No original vertex,
triangle or attribute is removed and no triangles are added. The implementation
rejects deltas at or below -2.4110107421875 m to prevent the side bodies crossing.
This is an explicit remaining narrowing limit, not unrestricted road compatibility.

| LOD | Vertices | Original and resulting triangles |
| --- | ---: | ---: |
| 0 | 298 | 264 |
| 1 | 296 | 237 |
| 2 | 303 | 150 |

All six tested source/LOD support files were hash-matched to the retained native
audit. Column checks at half-width deltas -1, 0, 2 and 8 m preserve side-triangle
edge vectors, all Y/Z coordinates and every nondegenerate triangle's winding.
Offline front/top/side views establish the column profile. These checks do not
constitute native game generation or visual acceptance.

`Lift02ColumnAssembly` consumes the reconstructed source render chain and passes
all three plans to the existing native render graph builder. Original Surface
slots, authored Y/Z bounds and stacking metadata are retained; X bounds follow
the transformed geometry. It does not load the original prototype Prefab.

## Remaining integration

The column and foundation assemblers are not yet called by a complete Lift02 generator. The
main structure, moving road, paint and final network route remain unfinished.
Footings have a different broad chamfered profile and must not use the column map.
The recorded 168 m sampling length matches the native network Bridge component;
the 106 m mechanism node offset is not a replacement sampling length.

Release compilation and the existing 12-language localization check pass. No new
user-visible strings were introduced; diagnostic exceptions remain technical.

## Continuous foundations (2026-10-20)

`Lift02Footings.py` emits the two continuous chamfered foundation shells at each
native LOD. Unlike the rounded column bodies, each complete foundation crosses
the centre and stretches by its own measured transverse span. Detailed half-spans
are 20.9267578125 m on both sides. The far source has asymmetric simplified
outlines; its own left/right extents are retained and each endpoint gains exactly
the requested half-width delta. Offline membership and per-vertex displacement
weights are hardcoded; runtime performs no geometry classification.

All original vertices, triangle connectivity, Y/Z coordinates and native Surface
slots remain. The three LODs retain 724/692/132 vertices and 664/597/100 triangles.
No new vertices or cuts are introduced. Four delta checks (-1, 0, 2, 8 m) verify
winding, exact outer displacement and coincident seams. Original and widened
near/middle/far top views were inspected offline. This is not game acceptance.

`Lift02FootingAssembly` connects these plans to the same native render graph and
stack metadata preservation as the column assembler. It consumes reconstructed
render inputs, not the original prototype Prefab. The main mechanism, road, paint
and complete Lift02 route remain unfinished. No user-facing text was added.

## Independent side members and moving glass (2026-10-20)

`Lift02RigidParts.py` emits disconnected components lying wholly on one side of
X=0. Original closed-pose OBJ indices are matched to the retained inventory and
Geometry hashes before emission. The map assigns only lateral displacement;
road replacement and selected-sidewalk inclusion remain separate responsibilities.
Crossing and merged components are excluded for their own authored-part plans.

| LOD | Side components | Mapped vertices | Mapped triangles |
| --- | ---: | ---: | ---: |
| 0 | 3710 | 158296 | 110628 |
| 1 | 3684 | 123746 | 61519 |
| 2 | 156 | 2968 | 1936 |

At deltas -0.25, 0, 2 and 8 m all mapped triangle edge vectors remain unchanged;
Y/Z and excluded vertices remain untouched. Offline front/side views of all three
levels were reviewed with mapped members highlighted separately from unresolved
crossing structure. These are partial displacement maps, not complete bridge
meshes, and are not yet connected to a full Lift02 generator.

`Lift02Glass.py` verifies three panes (12 vertices, 6 triangles) in mesh 1 at both
detailed levels. All three belong to native moving bone 1 on the positive-X side.
LOD1 reorders the first pane and its closing triangle; geometric triangle sets
match, but index buffers must not be interchanged. `Lift02Glass` translates the
native vertices and retains each level's original indices, raw attributes and
bone ownership. The far source has no separate glass slot.

Release compilation, 26-route checks and all twelve language checks pass. No new
user-visible text was added. Full structural composition, selected road/sidewalk
compatibility, native motion and independent-load verification remain open.

## Native counterweight pair (2026-10-20)

The two independent counterweight bodies are measured components 2120/2141 in
LOD0, 2093/2096 in LOD1 and 37/77 in LOD2. Native face bone records confirm bone 4
(`LiftBridge02_Weights`) in every level. Detailed and simplified front/top views
were inspected before assigning this complete centre-crossing member its own-span
transform. This does not change the native skeleton, animation, Y/Z or surfaces.

`Lift02Counterweights.py` decodes original binary32 positions and indices with the
native PipelinePlugin, checks source hashes against the retained audit, and emits
immutable vertex displacement weights. Each side uses its measured extent about
X=0, retaining the far level's slight native asymmetry. The detailed pair retains
352 vertices and 216 triangles per level; far retains 128 vertices and 64 triangles.
No topology is added or removed. This membership is disjoint from the disconnected
side-part map; final structural composition must also keep subsequent maps disjoint.

Four-width checks (-0.5, 0, 2, 8 m) preserve winding, all Y/Z and all excluded vertices.
Release compilation passes. This remains a partial main-mechanism implementation;
the counterweight helper is not yet called by a complete Lift02 generator. Tower
shells, crossing girders, road conversion and final render composition remain open.
No user-visible strings were added; localization behavior remains unchanged.

## Moving roof diagonal members (2026-10-20)

The recorded upper diagonal flange/web components comprise 60 disconnected pieces
at each detailed level. Their component IDs are literal emitter inputs. Exact native
binary positions and index topology are verified before emitting own-span weights;
all selected faces belong to original moving bone 1. Original and widened top views
confirm the fifteen longitudinal diagonal stations and their mirrored arrangement.
LOD0 retains 840 vertices/420 triangles; LOD1 retains 810 vertices/390 triangles.
The original far mesh omits this diagonal family, so this map has no LOD2 members;
its surviving transverse roof strips require a separate map, not these identities.

`Lift02RoofBraceGeometry` only updates the recorded member vertices. Four delta
checks (-0.5, 0, 2, 8 m) preserve Y/Z, excluded geometry and triangle winding. The
map is disjoint from the side-member and counterweight maps. No cuts, extra faces,
materials, bones or runtime geometry classification are introduced. Detailed
centre connectors and other roof members still require their own maps before
complete structural composition. This helper is not yet a complete generator.

Release compilation, route and twelve-language checks pass. No user-visible text
changed. Human game acceptance remains deferred until the complete bridge batch.

## Fixed tower crossbeams (2026-10-20)

`Lift02TowerBeams.py` records twenty independent transverse members in each detailed
LOD and eighteen corresponding simplified members in LOD2. The far source combines
the two lowest outer members at each end; this preserves its native simplification,
not a duplicate of detailed geometry. All selected faces retain fixed bone 0.
Exact decoded geometry is hash checked against the native audit before immutable
own-span displacement weights are emitted. Front views at original width and
+8 m half-width were reviewed across all three levels.

LOD0/1 retain 320 vertices and 160 triangles each; LOD2 retains 296 vertices and
152 triangles. Four-width checks preserve triangle winding, Y/Z and all vertices
outside this map. Membership does not overlap roof diagonals, rigid side components
or the bone-4 counterweights. Original Surface slots, skin channels and topology
remain unchanged. Tower diagonals, composite shells and platform structures still
need their own maps; this increment does not complete tower or generator integration.

Release compilation, 26 route checks and twelve-language checks pass. No new
user-visible strings were introduced. Native motion and persistence remain unverified.

## Recorded native track replacement (2026-10-20)

Native rails, sleepers, fasteners and their thin strips are separate from the
load-bearing bed in the detailed models. Offline inspection of original geometry
identified 1,596 components in each detailed LOD. The selected set contains
64,670 vertices/41,540 triangles in LOD0 and 50,850 vertices/25,498 triangles in
LOD1. These are replacement inputs for the selected road or track, not structural
simplification. The bed components 2435/2404 and all other structural members remain.

`Lift02TrackRemoval.py` verifies source Geometry hashes, exact OBJ index topology,
whole-component membership and fixed/moving face bones (0 and 1). It emits 58
literal face ranges per level. `Lift02TrackRemoval.RemoveDetailed` marks only these
ranges and checks their original index fingerprints before use. It performs no
runtime height/width classification, geometric intersection or receiver cutting.

The original far model has no separate rail/sleeper components. Its baked road
appearance and merged top shell require a separate conversion; applying detailed
face ranges to far geometry is explicitly unsupported. Native track replacement
must be coupled with the sampled selected-road geometry in final composition;
this helper alone does not form a valid generated bridge and is not yet routed.

Offline rail/structure overlays, source membership checks and Release compilation
pass. Existing route and twelve-language checks pass; no new user-visible text was
introduced. Native generation, persistence and visual acceptance remain unverified.

## Native road width and motion domain (2026-10-20)

The retained Lift02 network contains three mutually exclusive branches per lateral
occurrence; summing all branches produced the old, incorrect 32 m width. Native
piece 79483 has width 4 m and occurs twice, while piece 79475 has width 1 m and
occurs twice. Outward side piece 79478 (0.3 m) is excluded under the same measurement
convention as Draw02/Draw03. `BridgeMeasurements.Lift02RoadWidth` is therefore 10 m.
The original tower measurement is unchanged. This corrects future half-width delta
calculation; it does not migrate or rewrite any existing bridge assets.

Detailed bed-top faces at Y=9.2666015625 meet at Z=+/-82 m. Moving faces use bone 1;
fixed faces use bone 0 and extend to +/-106 m. Lower end protrusions reach 0.09375 m
past the top seam and must not redefine the road-top partition. LOD1 preserves
those top seams despite simplification. The native far shell uses +/-81.96875 m
and needs reconciliation with the detailed seam in its eventual structural plan.

`Lift02RoadPartitions.Carriageway` records the detailed fixed/moving/fixed intervals
with no invented road-top gap. It remains a motion-ownership input awaiting road
preparation and complete structural assembly; it is not a claim that far seams,
sidewalks, surface height or native loading have been completed. Original native
network sampling length remains 168 m and is distinct from the 212 m full shell.

## Tower diagonals and composition audit (2026-10-20)

`Lift02TowerDiagonalGeometry` records sixteen independent fixed tower diagonal
members at each detailed LOD. Exact source decoding verifies bone 0 for every
selected face. Near retains 384 vertices/192 triangles; middle retains 314/150.
The original far mesh omits this diagonal family. Original/widened front views
were inspected; four-width checks preserve winding, Y/Z and unselected vertices.

An offline composition audit now checks rigid side, roof diagonal, tower beam,
tower diagonal and counterweight memberships together. No displacement maps overlap.
After excluding the recorded replacement track components, the remaining unresolved
main-geometry sets are 397 components/31,124 vertices/18,224 faces in LOD0,
389/26,309/13,776 in LOD1 and 43/760/448 in LOD2. These counts are an implementation
inventory, not a completion percentage or proof that the remaining members are
homogeneous. Composite tower/deck shells, platforms and road appearance still need
independent rules. The offline inventory is `lift02-remaining-main-parts.json`,
reproduced by `lift02_main_coverage.py` in the research directory.

Release compilation, route checks and all twelve UI languages pass. No user-facing
text changed. The helpers remain awaiting complete render/generator composition;
there is no additional routed or accepted bridge style in this increment.

## Moving transverse frames (2026-10-20)

`Lift02MovingFrames.py` records fourteen independent transverse frames in each
 detailed native mesh. Exact binary positions and index topology are checked
against the recorded source. All selected faces retain native road-motion bone 1.
LOD0 retains 1,624 vertices/1,064 triangles and LOD1 retains 952/728. Each frame
stretches across its own measured span; no indices, Y/Z positions or bone data change.
The native far mesh omits these independent frames at Y=17.56–19.10 m. Its higher
roof strips are separate authored members and must not be treated as counterparts.

Four width deltas (-0.5, 0, 2, 8 m) pass winding and excluded-vertex checks. Original
and widened front projections were inspected. The combined offline membership audit
finds no overlap with prior displacement maps. Remaining main-geometry inventory is
383 components/29,500 vertices/17,160 faces at LOD0, 375/25,357/13,048 at LOD1,
and 43/760/448 at LOD2. This helper still awaits full structure/render assembly;
it does not add a routed generator or establish native motion/persistence acceptance.
No user-visible text or localization keys change.

## Joined moving and fixed shells (2026-10-20)

`Lift02MovingBase.py` records detailed component 0: all centre-crossing faces lie
in the road-adjacent base below Y=8 m and use only X=+/-5.2056884765625 endpoints.
There are no interior-X vertices. All upper faces belong entirely to one rigid
side. Sign-X translation therefore preserves side shapes and extends the base
without subdivision. LOD0 retains 4,128 vertices/2,080 triangles; LOD1 retains
4,108/2,062. Sixteen separate far base beams retain 192/96; the far side members
already belong to the disjoint rigid-side map. All selected faces use bone 1.

`Lift02TowerShell.py` records detailed joined towers 1092/1093 and 1069/1070.
Their only crossing faces are top beams at Y=72.69–73.86 m, using their two
X=+/-5.01171875 endpoints. Inner tower details stay on their respective rigid
sides. Translating those sides extends the top beams across their own spans.
LOD0 retains 3,812 vertices/2,256 triangles; LOD1 retains 3,706/2,044. Far top
beams 33/71 retain 32/16; other far tower sides are already rigid-mapped.
All selected faces retain bone 0. Neither helper changes indices, Y/Z, UVs,
material slots or skinning attributes; no new edge vertices are required.

Exact native source/index checks, four-width winding and side-face rigidity
checks pass. Original/widened near and far front projections were inspected.
The combined membership audit remains disjoint. Remaining main-geometry inventory
is 380 components/21,560 vertices/12,824 faces at LOD0, 372/17,543/8,942 at LOD1,
and 25/536/336 at LOD2. Platforms, roadbed replacement and remaining crossing
members still require implementation before full render/generator integration.
No user-visible text changes; native generation and independent reload are not
validated by these offline checks.

## Tower platform decks and transverse supports (2026-10-20)

`Lift02PlatformDecks.py` records six centre-crossing deck components per detailed
LOD, preserving their native perforations, index topology and fixed bone 0.
The initial whole-component span mapping was superseded by the measured
side-strip/central-span mapping below. Near retains 1,676 vertices/1,788 faces; middle retains 1,424/548.
The native far mesh omits these independent platform decks.

`Lift02PlatformSupports.py` records fourteen transverse support members per
detailed LOD (336 vertices/168 faces each). They retain bone 0 and stretch by
their individual measured spans. The original far model has no corresponding
independent members. These maps do not cover the joined platform perimeter
frames or central mechanical hardware, which still need separate treatment.

Both emitters verify original Geometry identity, exact native indices, whole-part
membership, source bones and four width deltas. Winding and excluded vertices
are preserved. Original/widened top projections were inspected. The combined
membership audit remains disjoint, leaving 360 components/19,548 vertices/10,868
faces at LOD0, 352/15,783/8,226 at LOD1 and 25/536/336 at LOD2. These counts are
remaining implementation inventory, not acceptance percentages. No user-visible
text changes. Full generator integration and batch native validation remain pending.


## Platform perimeter alignment (2026-10-20)

The combined platform review exposed a defect in the initial whole-component
scaling: internal deck boundaries moved differently from the support endpoints.
`Lift02PlatformDecks.py` now translates complete side strips rigidly, leaves native
centre residues (less than 0.00001 m) stationary and opens the central spans between
them. The detailed source has no other vertices inside the measured 2.47 m central
half-span. This preserves bevels, perforations and relative side-boundary offsets.
The previous whole-platform span mapping is no longer used.

`Lift02PlatformFrames.py` adds four joined perimeter/outline components per detailed
LOD. Inner junctions at X=+/-2.50323486328125 delimit the central spans; outer side
frames translate rigidly. The outline already contains these junction vertices;
the thick crossing frame contains endpoint vertices only. No subdivision is needed.
Near retains 756 vertices/408 faces; middle retains 650/332. The native far model
omits these platform frames. Bone 0 and original indices remain unchanged.

Four-width checks now explicitly verify rigid side faces as well as winding and
excluded vertices. A combined top-view review of decks, perimeter frames and
supports at original width and +8 m half-width confirms their boundary relationship.
The disjoint mapping audit leaves 356 components/18,792 vertices/10,460 faces at
LOD0, 348/15,133/7,894 at LOD1, and 25/536/336 at LOD2. This remains intermediate
structural implementation, not complete generator or native persistence validation.
No user-visible text or localization keys changed.

## Fixed and moving roadbed width (2026-10-20)

`Lift02Roadbed.py` records detailed roadbed components 2435/2404 and far components
12/50/78. Detailed original vertices occur at the centre or outside X=+/-3.78 m;
side edge profiles translate rigidly while centre vertices remain stationary.
Far bed faces use the corresponding sign-X rule. The emitter checks that source
face groups contain only native bones 0 and 1, and retains both unchanged.

Near retains 5,448 vertices/2,724 triangles, middle 5,019/2,435 and far 128/72.
Four-width winding, rigid side-face and excluded-vertex checks pass. Original and
widened near/far front projections were inspected. No original face, material,
UV, Y/Z coordinate or skinning channel is modified by this width helper.

This does not resolve the native far road-top height/seam differences or remove
its baked road appearance. Those remain selected-road replacement work, together
with surface preparation and paint. The combined displacement maps are disjoint;
remaining inventory is 355 components/13,344 vertices/7,736 faces at LOD0,
347/10,114/5,459 at LOD1 and 22/408/264 at LOD2. No new user-facing text is added.
Full Lift02 generator routing, native motion and independent reload remain pending.

## Selected-network assembly (2026-10-20)

`Lift02NetworkAssembly.Apply` now composes converted selected-road sections,
the recorded Bridge component and owned generated pillar/mechanism placements.
Native audit component 134669 refers to `LiftBridge02NetPillar` (118756), not a
built-in pillar placeholder. Both generated object arguments must belong to the
current factory. The native 168 m segment length and mechanism placement at
Y=70.599998474121094 remain unchanged.

`Lift02NetworkData.StructurePlacements` exposes the two pillar placements and
mechanism separately. The full four-entry native `Placements` recipe remains
available and retains the original railway outside-connection entry. Assembly
preserves every selected-network outside-connection rule and ground object,
replacing only elevated supports; it does not force the train's 8 m anchor on
other selected network types. No new edge/node state rules are invented.

`Lift02SectionRoles` uses the selected occurrence's median and requirement fields.
Only intersection-only medians receive the native single intersection branch;
ordinary medians, shoulders and side sections retain all three span branches.
`tools/Lift02SectionRolesCheck` verifies required-all and required-any cases,
a multi-alternative median, non-median intersections, source preservation and
missing-section failures against the actual role resolver.

This assembly still awaits the complete generated NetPillar, main appearance and
road preparation before generator routing. It is not native game creation or
independent reload acceptance. Release compilation passes; no user-visible text
or localization keys were added.

## Ordinary NetPillar source graph (2026-10-20)

`Lift02PillarSources.py` now reconstructs the nine native renders belonging to
`LiftBridge02NetPillar`: top, column and foundation, each with three LODs. Source
asset IDs, original bounds/counts/surface areas, LOD bias, stacking overlaps and
scaling flags are recorded from the hash-pinned native audit. Foundation
`BaseProperties` accepts the resolved built-in Default_Base; every level retains
the BridgesAndPorts content prerequisite. No original NetPillar Prefab is loaded.

The object's native component set is only PillarObject and ContentPrerequisite.
Unlike Lift01, it has no StandingObject; none is invented. `CreateObject` accepts
factory-owned generated top/column/foundation render graphs and reconstructs the
recorded three zero-offset mesh placements and pillar behavior.

All nine Geometry payloads were extracted from their original packages and
verified against audit hashes. Offline near/middle/far views show rounded-end
continuous cap, column and foundation shells. Their triangle counts are:

| Part | LOD0 | LOD1 | LOD2 |
| --- | ---: | ---: | ---: |
| Top | 440 | 263 | 100 |
| Column | 68 | 40 | 50 |
| Foundation | 272 | 163 | 100 |

Detailed cap/column side vertices stop near X=+/-2.97052 m. Far foundation has
additional interior vertices, so copying another style's width recipe is not
valid. Per-part width conversion and complete render assembly remain unfinished.
Reconstructing these source descriptors is not yet generated NetPillar completion,
asset distribution proof or native reload acceptance. No localized UI text changed.

## Ordinary NetPillar width and assembly (2026-10-20)

`Lift02NetPillarGeometry` now covers all nine ordinary-pillar meshes. Rounded
lateral cap/column/foundation ends translate rigidly and their connecting spans
expand. Eight meshes retain original topology. The far foundation has interior
simplification vertices: its central span uses the detailed measured boundary
X=+/-2.97039794921875, while outer fragments translate rigidly. Recorded edge
subdivision separates these domains without removing original vertices. Its
100 faces become 162, using 62 edge instances with original-channel interpolation.
No runtime geometric classification is performed.

The emitter checks all nine original Geometry hashes and tests half-width deltas
-1, 0, 2 and 8 m. Source-relative winding, rigid side-fragment displacement, Y/Z
preservation and native-width total surface area pass. Widened near/far views of
all three parts were inspected. The current noncollapsed input range requires
half-width delta greater than -2.73 m; narrower compatibility remains explicit
unfinished work rather than a claimed supported input.

`Lift02NetPillarAssembly.Create` reads only reconstructed source render descriptors,
creates all three native LOD chains with original Surface/stack/base metadata,
updates X bounds and preserves authored Y/Z bounds, then creates the recorded
owned object. All nine graph levels are generated; the source roots are not used
as final object meshes. Null render-graph failures propagate to the caller.
This completes this assembly stage, not its connection to a full Lift02 generator.
Native generation, serialized dependency availability and independent reload still
require the batch-level checks. No user-visible text or localization keys change.

## Moving transverse webs and end frames (2026-10-20)

`Lift02TransverseWebs.py` records the lower transverse beam flanges/webs and the
upper roof transverse beam flanges/webs. Each detailed LOD has 112 independent
members. Near retains 1,568 vertices/784 triangles; middle retains 1,456/728.
The native far representation retains fourteen upper roof strips (56/28); it
omits the lower family. This correspondence is distinct from the already-mapped
lower moving frames and the longitudinal roof diagonals.

`Lift02EndFrames.py` records both moving end frames in all levels. Near and middle
retain 392 vertices/224 triangles each, far 56/28. These preserve the original
simplified end profile rather than substituting another LOD's dimensions.
Both maps retain native bone 1 and stretch each complete member across its own
measured lateral span. All indices, Y/Z and original rendering attributes remain.

Exact Geometry identity/index/bone checks and four width cases pass. Original and
widened near/far front projections were inspected. The combined displacement audit
remains disjoint. Remaining main inventory is 241 components/11,384 vertices/6,728
faces at LOD0, 233/8,266/4,507 at LOD1 and 6/296/208 at LOD2. Remaining central
connectors, mechanical hardware and fixed bases still require explicit treatment;
these inventory reductions do not establish complete generator support. No new
user-visible strings were introduced; game generation/reload remain unverified.

## Fixed bases, vertical braces and mechanism object assembly (2026-10-20)

The fixed lower bases now retain bone 0 with literal membership at all three
LODs. Their lateral bodies translate rigidly while the connecting span expands;
1,560 / 1,249 / 164 native triangles are retained. Fourteen moving vertical-plane
braces per detailed LOD use each member's own span and original bone 1, retaining
336 / 224 triangles. The far native mesh omits this brace family. Both emitters
verify source hashes, indices, bone membership and four width cases.

`Lift02ObjectAssembly` now composes the recorded main, column and footing render
slots, native mechanism components and selected-road objects. The audited native
mechanism has no authored attachments. Standing-leg width follows the same full
width increment as the structure. Road furniture and native skinned paint use the
measured carriageway partition: bone 1 between -82 and +82 m, bone 0 at the fixed
ends through +/-106 m. Paint uses the shared single-sided, original-road-bone
pipeline and the same road-to-object frame; no projector or runtime synchronizer
is introduced.

This is a callable assembly stage, not a complete generator route. Remaining
structure membership, road replacement, far road alignment and final integration
are still required. Existing generated assets and persistence paths are unchanged.
No user-visible text or localization keys are added. Native generation, motion and
cold loading remain pending batch verification.

## Additional crossmembers (2026-10-20)

`Lift02CrossMembers.py` identifies six fixed-end transverse beams, fifteen moving
lower diagonals, sixteen end-roof flange/web members and two fixed tower-top
crossmembers in each detailed LOD. Their original groups retain bone 0 or bone 1,
respectively, without borrowing another style's component membership. Each member
crosses the centre and uses its own measured lateral span. The native far mesh
omits these members; no extra far geometry is fabricated for this stage.

The runtime literal map retains 1,372 vertices/700 triangles at LOD0 and
1,228 vertices/582 triangles at LOD1. Native hashes, exact indices, per-member
bone ownership, unchanged Y/Z, exclusion of other vertices and winding pass at
half-width deltas -0.5, 0, 2 and 8 m. Original and widened projections of all four
families were inspected. The combined membership audit finds no overlap with
existing displacement maps. Remaining main structure inventory is 186 components
(7,404 vertices/4,132 triangles), 178 (4,585/2,452), and four (80/44) for LOD0/1/2.
These counts exclude the previously identified track-removal components.

The helper is not yet consumed by a complete Lift02 render assembler. Remaining
central connectors, mechanical parts, road replacement and integration are still
open. No persistence, existing-asset, UI or localization behavior changes.

## Machinery housings and central joint plates (2026-10-20)

`Lift02Machinery.py` records eight fixed machinery housing/pedestal members in
both detailed levels and their four far simplified counterparts. Exact native
face-group evidence assigns every selected face to bone 0; rotating wheel bones
are not reassigned. Their own-span maps retain 772/686/80 vertices and 520/324/44
triangles. Source identity, native indices and four width cases (-0.5, 0, 2, 8 m)
pass. Native and widened front/side views were inspected at every LOD.

`Lift02JointPlates.py` records 30 roof, 28 vertical-brace, 15 lower-brace and
16 tower joint plates per detailed level. The first 73 use bone 1; the tower
plates use bone 0. Each independently authored crossing plate stretches across
its own span. Native far geometry omits these plates. Near/middle retain
3,912/1,785 vertices and 2,252/1,092 triangles. Four width checks (-0.25, 0, 2, 8 m)
preserve winding, other vertices, Y/Z and original indices. Original/widened
projections were inspected. The smallest plate half-span is 0.41493988037109375 m;
the helper explicitly rejects collapse. General narrow-road compatibility remains
unfinished; these positive-span tests do not prove it.

The combined displacement membership audit is disjoint. Remaining unassigned
crossing components number 89/81/0, with 2,720/2,114/0 vertices and
1,360/1,036/0 triangles. Zero far membership remainder means structural width
coverage only, not completion of far road replacement, seams, materials or the
full generator. Remaining detailed members include deck crossmembers, asymmetric
tower diagonals and central platform hardware. Both new helpers await the final
render assembler. No UI keys, persistence layout or existing assets change.

## Deck crossmembers and centre-reaching half diagonals (2026-10-20)

`Lift02DeckCrossMembers.py` records 53 below-bed transverse members in each detailed
LOD, with measured half-span 3.96502685546875 m. Near retains 1,728 vertices/864
triangles; middle retains 1,522/760. Native joint components 4219/4342 (near) and
4188/4311 (middle) contain faces on both fixed bone 0 and moving bone 1. The emitter
checks each component's exact recorded bone set and the runtime changes only X;
no face or skinning channel is replaced. Far omits this separate member family.

`Lift02HalfDiagonals.py` records fourteen fixed tower half-diagonals and four moving
under-deck half-diagonals per detailed LOD, retaining 544/400 vertices and 272/180
triangles. These centre-reaching members use their own outer endpoint as the
scale denominator on the entire member. A small bevel beyond the centre is not
interpreted as the span of a separate second member. Native bones 0/1 and all Y/Z
coordinates remain unchanged. Far omits these members.

Both emitters verify exact source hashes and index arrays, bone ownership,
unchanged unrelated vertices and winding for width deltas -0.5, 0, 2 and 8 m.
Original/widened views were inspected. The combined membership audit remains
disjoint; only 18 near and ten middle central platform hardware components remain
unassigned (448/192 vertices, 224/96 triangles). Their side/front views show a
longitudinal upper member, vertical supports, base plates and short braces;
their complete authored-part treatment is still pending. No far structure
membership remains, but far road conversion and full generator integration do.
No user-visible text, existing assets or persistence behavior changes.

## Main structural width composition (2026-10-20)

`Lift02CentralHardware.py` finishes the remaining central platform beam, support,
plate and brace memberships: 448/192/0 vertices and 224/96/0 triangles, all on
native bone 0. These are structural hardware, not the independently authored
central railing-post exception. Each crossing member uses its own outer endpoint
span. This exposes a small positive-span narrowing limit (0.0266017913818359375 m
for the smallest member); unrestricted narrowing is not solved by this stage.
Source identity/index/bone checks and deltas -0.01, 0, 2 and 8 m pass.

`Lift02StructureWidthPlan` now calls all 22 measured main-mesh transforms in one
place and returns a native-channel-preserving structural plan. It validates the
original per-LOD face counts. This width stage does not remove triangles: the
pending road stage must pair detailed track removal and native road replacement.
The offline combined inventory has no unassigned retained structural components
at any LOD and no overlapping displacement memberships. Removed native track
components are excluded from that coverage claim.

`tools/Lift02StructureCheck` compiles the actual runtime width composer and every
called map against minimal Vector3/Plan stubs. Export the audited mesh caches with
its `ExportInputs.py RESEARCH_DIRECTORY`, then run the project with that directory
as its argument. Four composed widths pass for 199,372 / 159,895 / 4,152 vertices
and 134,080 / 80,439 / 2,600 triangles: source immutability, unchanged Y/Z, finite X,
identity at native width and unchanged index/removal arrays. Individual emitter
winding checks and the independent membership audit remain separate evidence.
The harness does not exercise Unity rendering, skinning or serialization.

Road replacement, far-road alignment, selected-sidewalk compatibility and the
full Lift02 generator remain unfinished. This composition adds no UI text and
changes no persisted bridge, material descriptor or runtime synchronization path.

## Native road removal and mechanism render assembly (2026-10-20)

`Lift02StructurePlan` composes the width plan with native track replacement.
Detailed levels remove the previously audited rail/sleeper/fastener faces only
(41,540 / 25,498), keeping the supporting roadbed. There is no complete detailed
road sheet to remove at the far top datum. `Lift02FarRoadTops.py` records exactly
six upward road-sheet faces at Y=9.818359375: fixed 410/411/1178/1179 and moving
1618/1619. Their total native area is 1,861.04846 square metres. Native indices,
source hash, bone ownership and membership in bed components 12/50/78 are checked;
far underside and sidewalls remain. The far end stations and height still need
reconciliation with detailed construction and selected-road geometry.

`Lift02RenderAssembly` now connects prepared selected-road levels to the shared
native render assembler with the recorded 2/2/1 surface-mesh counts. Mesh 0 uses
the combined structure plan; detailed mesh 1 uses the original glass plan. Input
render names and reconstructed ownership are checked, and the original graph,
raw channels and materials continue through the shared factory. This adapter
requires fully prepared road levels; no complete Lift02 generator route exists yet.

The C# structure check now executes road removal at every LOD, verifies exact
removal counts and all six far face identities, and confirms a mutated native
index fails instead of applying the mask to changed topology. Width/source
invariants still pass. This does not verify selected-road appearance, Unity
rendering or persistence. No localization keys or existing saved assets change.

## Original roadbed material measurement (2026-10-20)

The native main Surface `ea923855f34e05d7a517992fe064ddb7` references
VTSurface `a5d80f9ec1223ac65124d799555c5d6c`, a 4096x4096, four-layer atlas
with 512px tiles and 85 total tiles including mips. It is not the separate
VTTexture layout used by the Lift05 investigation. Native BC7 albedo was decoded
without recompression and visually inspected with the original roadbed UVs.

All 212 upward roadbed faces at Y=9.2666015625 use the same blue painted-metal
strip: U=[0.92041015625,0.9990234375], V=[0.046112060546875,0.05438232421875].
A unit covers 4m longitudinally and 0.42156982421875m laterally, giving
80.5 and approximately 80.35445 pixels/metre respectively. The roadbed is not
Draw01 asphalt; its native appearance must not be inferred from that style.
`Lift02RoadAppearance` records this repeating projection, mirrors the lateral
coordinate on opposite sides and transforms the projection into the sampled
road frame. Appearance regions retain the physical end gaps; they do not change
road bones or cut receiver geometry for markings.

This helper is not a complete road preparation path. Selected sidewalks, far
road alignment and generator integration remain unfinished. The reproducible
local investigation is `work/bridge-research/lift02_material_probe.py` in the
research workspace; it records original source hashes and every measured face
in `lift02-roadbed-uv-audit.json`. No user-visible strings or saved assets change.

## Connected Lift02 generator increment (2026-10-20)

`Lift02BridgeGenerator` now connects the recorded source renders and unlocks,
168m closed-pose native sampling, `Lift02RoadPreparation`, mechanism render
assembly, column and footing render chains, ordinary network pillars and owned
network conversion. The source Prefabs are temporary reconstructed inputs;
output objects reference factory-owned render graphs. Ordinary pillar sources
are disposed after their generated graphs have been assembled. No original
LiftBridge02 Prefab lookup or runtime synchronization loop is introduced.

Road preparation uses the recorded five-bone skeleton and physical partitions:
fixed ends on bone 0, moving road on bone 1, gaps beyond +/-106m. Surface
appearance uses the measured native roadbed strip. The existing shared pipeline
produces separate skinned paint; the structural plan removes native tracks at
LOD0/1 and the six far road tops at LOD2. The original counterweight and wheel
bones are not repurposed. UI catalogue and all twelve style translations already
include Lift02; its exact route now dispatches this generator for preview and
creation instead of reporting a missing implementation.

Release build, 27-route architecture check, twelve-language UI check, six-case
Lift02 section-role check and all-LOD composed structure/removal checks pass.
These checks do not execute the Unity generation lifecycle or prove persistence
reload. This is the seventh routed movable style, not a seventh accepted style.
Far-road boundary alignment, selected-sidewalk-specific appearance/width rules,
general narrowing and complete native generation/persistence verification remain
unfinished. In particular, current road preparation gives sidewalks the same
appearance domain as the carriageway; that is an outstanding adaptation task.
Human visual acceptance remains deferred to the completed bridge batch.

## Far roadbed boundary reconstruction (2026-10-20)

`Lift02FarRoadBoundary.py` re-decodes the checked native LOD2 Geometry and emits
72 explicit vertex records in roadbed components 12, 50 and 78. End planes are
reconciled from +/-81.96875 to +/-82 and from +/-106.009765625 to +/-106, matching
the detailed fixed/moving partitions. Vertices on the coarse Y=9.818359375 roadbed
top are reconstructed at the detailed roadbed top Y=9.2666015625. This is the
underlying native metal bed, not an assumed height for every selected road.
All other Y/Z values and every X value remain unchanged by this stage.

`Lift02StructurePlan` applies this measured reconstruction only to LOD2 after the
six native top faces are marked for replacement. It opts into the existing
`ReconstructsAuthoredPositions` path; index arrays, bone attributes and native
material references remain unchanged. No sidewall or underside face is removed.
The boundary change does not solve the independent selected-sidewalk adaptation
or unrestricted narrowing requirements.

The offline emitter checks the original binary hash and decoded indices, exact
part membership and positive winding for all 2,600 source triangles. Before/after
side profiles were inspected. The runtime structure harness now checks the 72
changed vertices, detailed target datums, unchanged width coordinates and the
reconstruction flag, alongside all previous LOD topology/removal tests. Release
build passes. These checks do not claim native game rendering or cold-load
acceptance. No user-facing strings are introduced; localization remains shared.

## Support signed-width domain (2026-10-20)

Column, foundation and ordinary-pillar emitters no longer emit output-span rejection
conditions. Native source layout and finite delta checks remain. The generator now
reports support centre crossing without clamping the measured displacement. Original
part membership, topology recipes, surfaces and skeleton data remain unchanged.

All fifteen support part/LOD combinations pass seven width deltas (-4.99, -4, -3,
-1, 0, 2, 8 metres). The selected road baseline is 10 m, so positive road widths
require delta > -5 m. Foundation spans remain positive over this domain and retain
winding, bounds and coincident-seam checks. Columns and ordinary pillars retain
side rigidity and Y/Z checks; narrow signed cases report collapsed/reversed faces
rather than incorrectly requiring positive winding after centre crossing. At -4.99 m,
column levels report 8/8/16 such faces. This does not establish overlap-free geometry.

Main structure width limits and selected-sidewalk compatibility remain outstanding.
No generated asset or installed binary was replaced. No user-facing localization
keys changed; the added report note is a technical diagnostic.

## Base and platform signed-width continuation (2026-10-20)

Fixed lower bases, moving deck bases, platform decks, transverse platform supports
and perimeter frames no longer reject signed widths at their former span limits.
Both metaprograms and emitted runtime classes were updated. Original native geometry
is decoded and hash-checked again; the five generated membership/weight tables are
identical to their backed-up versions. No original triangles or bones are changed.

Seven deltas (-4.99, -4, -3, -0.5, 0, 2, 8 metres) verify unselected vertices, Y/Z,
finite positions and applicable rigid-side invariants. The moving base retains
positive winding at all seven widths and its original moving bone 1. Fixed parts
retain bone 0. Platform families absent in the original far mesh remain empty;
this change does not invent far-detail replacements. Narrow signed cases explicitly
report collapsed/reversed faces; this is not visual acceptance or an overlap-free
claim. The existing support diagnostic triggers before these fixed/platform limits.

Other main-structure guards still need correction before complete narrow-road
composition can be tested. User-visible text and localization keys are unchanged.

## Moving members and counterweight width domain (2026-10-20)

Additional transverse members, counterweights, end frames, moving transverse frames
and roof braces now accept finite signed deltas without an output-span gate. Their
five paired emitters retain source hash, native decode, exact part ownership and
bone checks. The emitted membership/weight tables match the backed-up originals.
Native source layout checks remain in runtime code.

All three LOD branches pass seven deltas (-4.99, -4, -3, -0.5, 0, 2, 8 metres),
including positive winding at every tested width, finite positions, unchanged Y/Z
and unchanged unrelated vertices. These families have native half-spans greater
than 5 m; they remain positive over Lift02's positive selected-road width domain.
Counterweights retain bone 4, moving frames retain bone 1, and mixed transverse
members retain their measured bone 0/1 ownership. Missing native far-detail families
remain empty. No surface or topology modifications are introduced.

Eleven other explicit main-structure width guards remain for subsequent correction.
This increment does not establish full narrow-road generation, game motion or
independent loading. No user-facing text or localization keys changed.

## Complete signed-width composition check (2026-10-20)

The remaining eleven output-span guards are removed from central hardware, joint
plates, machinery, half diagonals, deck crossmembers, roadbed, tower beams,
diagonals, shell, transverse webs and vertical braces. Paired emitters preserve
source hashes, part identities and bone checks. All eleven emitted membership and
weight tables match the originals. The generator diagnostic includes the measured
smallest central-hardware half-span, 0.0266017913818359375 m, without rejecting or
clamping the requested displacement.

Per-family seven-width checks preserve finite positions, unchanged unrelated
vertices and Y/Z, applicable side rigidity and positive winding in positive-span
cases. Narrow cases explicitly report collapsed/reversed faces. Signed mapping
correctness must not be confused with overlap-free appearance.

The production C# structure harness now checks eight deltas across all three LODs,
including -4.99 m and the central-hardware collapse boundary. All 24 combinations
pass native index preservation, source immutability, finite positions, unchanged
width-only Y/Z and combined road/track replacement counts (41,540 / 25,498 / 6).
Existing far-boundary datum and deliberately damaged topology rejection checks
also pass. No explicit output-span rejection remains in Lift02's geometry classes.

These are geometry-composition checks, not native renderer or asset persistence
verification. Selected-sidewalk adaptation, resource measurement and batch game
verification remain outstanding. No user-visible localization keys changed.
