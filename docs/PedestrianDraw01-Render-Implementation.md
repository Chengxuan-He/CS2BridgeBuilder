# PedestrianDraw01 mechanism render configuration

The generated render-data class configures fresh private render graphs for the mechanism,
column and footing. It preserves each native level's StackProperties, BaseProperties,
LOD references and content prerequisite. The caller supplies generated geometry and applies
the separately recorded procedural skeleton to each main-render level. Do not apply this
configuration to shared native renders or to a render already carrying copied components.

| Part | Native render records in LOD order |
| --- | --- |
| Mechanism | 81053, 81054, 81055 |
| Column | 81056, 81057, 81058 |
| Footing | 81059, 81060, 81061 |

The names Column and Footing identify measured object mesh slots; they are not runtime name
matching or permission to infer geometry. Footing is distinct from the road-adjacent deck base.

Source audit: game 1.6.2f1, SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
The extractor follows actual LOD references and checks the complete component list. No runtime
archetype lookup, width inference or new material is introduced.

This class has no generator caller yet. It changes no existing bridge output. Compilation
does not establish geometry, native animation, shipping or cold-start acceptance.

## Measured support generation (2026-10-20)

`PedestrianDraw01SupportGeometry` and `PedestrianDraw01SupportAssembly` implement
the native column and foundation width stages at all three LODs. Eight separate
side columns exist at every level and translate rigidly by their recorded side.
Two foundations cross the centre line and stretch across their own measured
half-span, 4.0821533203125m. Foundations are not the road-adjacent deck base.
No column is widened and no support is reclassified from its coarse silhouette.

| Part | LOD0 vertices / triangles | LOD1 | LOD2 |
| --- | --- | --- | --- |
| Columns | 384 / 192 | 384 / 192 | 128 / 64 |
| Foundations | 224 / 112 | 208 / 100 | 40 / 20 |

`tools/GeometryMetaprogram/PedestrianDraw01Supports.py` validates the audited
native Geometry hashes, decoded component bounds and all-LOD membership before
emitting immutable ranges. Deltas -1, 0, 2 and 8m preserve triangle winding and
all Y/Z values; column triangles retain their shapes. Near and far before/after
front projections were inspected. The assembler keeps native surfaces, original
indices and stack/base semantics through the owned render-graph factory.
Release compilation passes. No user-visible text is added.

The main-mesh offline inventory contains 437/420/57 connected components, of
which 50/52/12 reach or cross the centre line. These are identification candidates,
not approved whole-component transforms: merged bodies still need detailed
part separation. The mechanism has 27,721/16,583/1,068 triangles in mesh 0 and
six glass triangles at each detailed level. Support completion does not complete
main structure, road replacement, appearance, selected sidewalks, bone partitions
or the generator route. The current support assembler has no generator caller.
No existing saved bridge is modified, and no game acceptance is claimed.

## Independent side parts and glass (2026-10-20)

`PedestrianDraw01RigidParts.py` emits separate main-mesh side translations for
387/368/45 native disconnected components, covering 25,355/23,660/618 vertices
and 21,377/11,183/436 triangles. No component reaching the centre line is admitted
to this map. Each source triangle is either wholly inside or wholly outside the
selected memberships. Deltas -0.25, 0, 2 and 8m preserve side triangle edges,
Y/Z, unrelated vertices and native indices. Near/middle/far plan views were
inspected with selected parts highlighted. This is a displacement map only:
sidewalk presence and the joined-body changes are separate unfinished stages.

`PedestrianDraw01Glass.py` verifies the three original glass quads (12 vertices,
six triangles) on native fixed bone 0 at both detailed levels. Their coordinates
are all on the negative-X side, so the runtime map translates them by -delta.
It retains the original surface and indices, and never binds them to a moving
road bone. Four displacement checks preserve all triangle edges. LOD2 contains
no separate glass surface; no invented far glass mesh is added.

Both new helpers compile in the Release build. They await the complete main
render composition; adding these helpers does not add a generator route or prove
native animation/persistence. Joined fixed/moving bodies, centre machinery,
transverse members, road surfaces and selected-sidewalk conversion remain. No
user-visible string, persistence path or existing saved bridge is changed.

## Moving deck crossbeams (2026-10-20)

`PedestrianDraw01Crossbeams.py` records eighteen complete transverse members in
each detailed main mesh, nine per leaf. Each level contributes 432 vertices and
216 triangles. Their exact near/middle bounds correspond. Positive-Z members
retain native bone 1 and negative-Z members retain bone 2. Each member stretches
across its measured half-span 3.886474609375m; unrelated vertices and all Y/Z
coordinates remain unchanged. The emitter verifies source hashes, component and
triangle ownership, per-member bones, and winding for deltas -1, 0, 2 and 8m.
Original/widened plan views were inspected. Runtime consumes explicit ranges,
not component/bounds analysis. The far native mesh has no separate members in
this family; its merged deck bodies still require their own detailed-derived plan.

The helper is not yet consumed by a complete main structure plan. Remaining
cross-centre candidates number 32/34/12 after excluding these members and the
previously recorded side parts. This inventory count is not a completion claim:
joined bodies may contain several logical parts. Release compilation and existing
localization checks pass; there are no added player-visible strings or changes
to saved assets. Full generator integration and batch acceptance remain pending.

## Detailed road motion domains and joined-body investigation (2026-10-20)

The positive detailed walking surface ends at Z=60.28515625 on bone 1; the
negative leaf mirrors it on bone 2. Fixed surface faces start at
+/-60.3857421875 on bone 0 and extend to +/-76. The central leaf endpoints are
+/-0.029998779296875. `PedestrianDraw01RoadPartitions` records these stations and
preserves the native 0.1005859375m hinge gaps and 0.05999755859375m centre gap.
The constants come from actual Y=10 surface faces, not the skeleton pivot or
whole mechanism bounds. Both detailed levels have identical moving top areas
(approximately 444.256743m2 per leaf). Native far tops merge the raised edge with
the walking surface at Y=10.10009765625 and start at +/-0.06464385986328125;
that proxy still needs a detailed-derived road replacement plan.

Joined-body views confirm that connected components include independent logical
parts: the moving body combines a road sheet, raised edges, an A-frame and the
under-deck mechanism. A whole-component scale is therefore unsuitable. Fixed
body components also include four moving-bone faces at each detailed end.
The original main meshes contain 64/58 mixed-bone triangles at LOD0/1; their raw
skinning channels must survive geometry processing. No per-component bone
replacement is permitted. The partitions apply to newly sampled road surfaces,
not to these structural triangles. Selected sidewalks and objects still need
style-specific domain review before final assembly.

No generator route is added by this increment. Release build and localization
checks pass; native generation/animation/persistence remain unverified.

## All-LOD native walking-sheet removal (2026-10-20)

`PedestrianDraw01RoadTops.py` emits exact triangle/index records for 136/133/16
native walking-sheet faces. Detailed moving leaves each contribute 40 upward
faces on bones 1/2; fixed ends contribute 56/53. Far moving leaves each contribute
four faces, with eight additional fixed-end faces. The source hashes, areas,
longitudinal endpoints and bone membership are checked offline. Runtime verifies
the recorded index triples before marking removal; it never classifies surfaces
by height, bounds or material.

Further face-level inspection corrects the earlier coarse "merged surface"
description: far walking sheets and edge strips share the same height and
connected body, but already have separate triangle boundaries at
X=+/-3.68646240234375. Therefore no additional subdivision or edge interpolation
is required for this removal. The original far raised edge strips survive intact.
The far central endpoint still differs from the detailed endpoint and requires
separate alignment when the complete structure plan is assembled.

No downward face, raised edge, glass or mixed-bone connector is included in the
removal lists. The helper must be consumed with replacement selected-road geometry;
it does not by itself supply a complete structure plan or generator route.
Release compilation and localization checks pass. No game assets are changed.

## Independent end cross-connectors (2026-10-20)

`PedestrianDraw01EndConnectors.py` records twelve independently authored crossing
connectors per detailed LOD: four lower fixed-end plates on bone 0 and eight
moving end members divided between bones 1 and 2. Near/middle contribute
248/234 vertices and 144/128 triangles. Each complete member stretches across
its measured symmetric half-span (2.46075439453125, 2.5693359375 or
2.6719970703125m), with explicit vertex ranges. No centre machinery, joined main
body or side component is included in this map. The native far representation
has no independent member in this family; its joined proxies remain a separate
unfinished reconstruction task.

Source hashes, component closure, original bone sets and four width deltas
(-0.5, 0, 2, 8m) are checked offline. Y/Z, unrelated vertices and winding are
preserved. Front views of all six positive-end member types were inspected.
The runtime helper does not alter skinning/material channels or triangle indices.
Release build and localization checks pass. It awaits composition with the
remaining main-body/machinery rules; no complete generator or game acceptance
is claimed and no saved assets change.

## Central wheel assemblies and far envelope (2026-10-20)

`PedestrianDraw01Machinery.py` decodes the native binary positions and indices,
checks the recorded asset hashes, and emits fourteen independently authored
central parts at each detailed LOD. These comprise the upper wheel housing,
wheel rings/discs, axle, cap plate and two fixed pulleys at each end. The map
covers 2,992/2,781 vertices and 2,828/1,507 triangles at LOD0/1. Moving hardware
retains bones 1/2; the lower pulleys retain bone 0. Mixed-bone ropes and joined
main bodies are excluded rather than assigned a replacement bone.

The far wheel envelope consists of two authored shells (76 vertices, 40
triangles), each with the same +/-0.3176116943359375 X boundary as the detailed
housing. Every far shell vertex lies on one of those two boundary planes.
Its own-span stretch therefore preserves the detailed housing width at both
ends without inventing separate far wheel rings. The near/middle cap is
slightly asymmetric; its measured maximum absolute X supplies the crossing
span and the scaling origin remains X=0, as required by the contract.

The emitter verifies component closure, exact bone membership, unchanged Y/Z,
unchanged excluded vertices and positive winding at width deltas -0.03, 0, 2
and 8m. Near/middle/far X/Y views before and after widening were inspected.
Original indices, material assignments and skin channels are unchanged.
Narrowing beyond a part's positive span is not resolved by this helper and
remains part of the full width-compatibility work.

This is measured input for the unfinished main structure composer, not a new
generator route. Main joined bodies, ropes, remaining far reconstruction,
road appearance and complete assembly are still outstanding. No saved bridge
or installed binary is changed by this helper-only increment. It adds no
user-facing text; existing localization behavior remains unchanged.

## Middle-LOD rope ownership (2026-10-20)

The two middle-LOD connected rope groups 404/413 are not centre-crossing authored
members. Detailed groups 421/422/431/432 are independent left/right ropes, with
inner X planes -0.0000020000152 and +0.0000030000228. Middle simplification
connects their position-welded components by reusing these almost-coincident
planes across opposite sides. Eighteen middle triangles cross X=0, but none
spans both ropes' exterior points. Every triangle contains exterior vertices
on exactly one detailed side. No actual vertex index belongs to both sides.

`PedestrianDraw01Ropes.py` records that detailed-side ownership for 777 middle
vertices and 521 triangles, checking source hashes, closure and conflict-free
vertex ownership. The generated helper translates each recovered side rigidly;
it does not stretch the merged component. Detailed ropes already belong to the
existing rigid-part map, and the far mesh has no rope representation. No new
far ropes are invented. All original triangle indices and mixed skinning
channels are retained. There are no added vertices, cuts or bone substitutions.

Four width deltas (-0.25, 0, 2, 8m) preserve each triangle's edge vectors, Y/Z
and every excluded vertex. This resolves the rope transform input to the still
unfinished structure composer; joined bodies and full generation integration
remain outstanding. No user-facing strings or persistence paths are changed.

## Deck-adjacent joined bases and edges (2026-10-20)

`PedestrianDraw01DeckBases.py` records the below-deck portions of the joined
fixed/moving bodies, the detailed longitudinal centre members and the far
under-deck proxies. Detailed fixed-control enclosure faces are explicitly
excluded, including their bottom vertices at walking-surface height. Moving
upper-frame vertices are excluded above the measured edge height. The offline
profile selection emits literal vertex ranges; runtime contains no height or
bounds classifier.

The map covers 4,556/4,494/636 vertices across the three LODs. It applies the
contract's deck-adjacent base mapping X + sign(X) * delta and leaves centre-line
vertices fixed. Y/Z, material and skin channels remain unchanged. Walking-sheet
triangles remain available for the separate recorded removal step; this helper
alone does not remove faces or generate replacement road geometry.

Four width deltas (-0.03, 0, 2, 8m) verify unchanged excluded positions and Y/Z,
fixed centre vertices and retained winding of fully covered triangles. Front
views of the native and widened near/middle/far bases were inspected. Faces
joining the unfinished upper frame still require the composed-plan test; these
partial checks do not establish complete all-LOD coverage or arbitrary narrow
road compatibility. No new generation route, saved-asset mutation or installed
binary update is included. There are no new user-visible strings.

## Fixed control enclosure and remaining coverage (2026-10-20)

`PedestrianDraw01ControlHousing.py` reads native binary positions and records
both fixed control enclosures on bone 0. Their measured half-span is
0.25726318359375m at all three levels. The map covers 340/309/40 vertices and
208/181/20 enclosure triangles. Near/middle enclosure foot vertices also occur
in 24 walking-sheet faces at each level; every such shared face is in the
existing road removal list. No retained structural triangle crosses the ownership
boundary. Enclosure vertices are disjoint from the deck-base map.

Source hashes, decoded-index correspondence, fixed bone membership, positive
winding at four widths, unchanged Y/Z and excluded vertices are checked. The
runtime map changes only X and retains native indices/material/skin channels.

A combined membership audit of rigid parts, crossbeams, end connectors,
machinery, deck bases, control enclosures and middle ropes finds zero overlaps.
Main mesh coverage is 33,923/34,931, 32,687/33,598 and 1,370/1,702 vertices.
All remaining 1,008/911/332 vertices belong to the two upper A-frame assemblies
(detailed joined moving bodies, independent far frame proxies). This is width
mapping coverage only, not full generation or sidewalk compatibility coverage.
The frame rules, full composer and generator integration remain unfinished.
No user-facing text, persistence path, installed binary or saved bridge changes.

## A-frame maps and width composition (2026-10-20)

`PedestrianDraw01Frames.py` covers the remaining 1,008/911/332 original main
vertices. Detailed side-leg inner stations determine the transverse web boundary;
legs outside that boundary translate rigidly. The separate central shaft-seat
faces use their own 0.24104309m span and retain their original motion bones.
The top cross-member retains the measured 0.4543686m inner corner. Far merged
faces use the same detailed part boundary instead of treating the whole A-frame
as a single stretched member.

Offline clipping emits explicit edge/interior barycentric recipes at profile
stations and inner-leg boundaries. The three levels add 140/118/80 edge and
4/4/0 interior vertices, replacing 144/127/48 original faces with fragments.
These are structural member-boundary subdivisions, not marking or texture-period
cuts. Original source vertices and Y/Z remain unchanged; native attribute
interpolation is delegated to MovableStructureModel. Four width deltas retain
positive winding, and front views at native width and +2m per side were inspected.

`PedestrianDraw01StructureWidthPlan` now composes the frame plan with all seven
other disjoint main-body maps, preserving edge/interior recipes and fragment
indices. The earlier zero-overlap audit plus the remaining-vertex frame map
covers all original main vertices. Walking-sheet removal remains a separate
assembly stage. Complete composed native mesh/attribute checks, far road boundary
reconciliation, selected-road appearance and final generation routing are still
outstanding; width coverage does not establish completion of those tasks.

Release build and twelve-language checks pass. No new user-facing text or saved
asset change is introduced. This helper/composer increment is not deployed as a
new supported generator; there is no native generation or visual acceptance claim.

## Structural road replacement and render assembly (2026-10-20)

`PedestrianDraw01StructurePlan` combines the complete width plan with exact
native walking-sheet removal. `PedestrianDraw01RenderAssembly` validates all
three reconstructed source render names and dispatches the native 2/2/1 mesh
layout through MovableRenderAssembly: main structure in slot 0, original fixed
glass in slot 1 for LOD0/1. It retains the established native material/attribute
pipeline and does not look up the original mechanism Prefab.

A temporary executable harness links the actual generated helpers/composer and
extracts the current MovableStructureModel.Plan implementation for validation,
using only a Vector3/Mathf substitute outside Unity. Twelve cases (three LODs,
four widths) pass native source-index preservation, unchanged original Y/Z,
finite constructed positions, valid barycentric recipes and fragment membership.
Road replacement adds exactly 136/133/16 removed faces beyond frame replacement;
there is no removal overlap. This harness does not execute the raw Unity mesh
attribute builder or native persistence, which remain separate checks.

Further far-face inspection clarifies the earlier road-height description:
fixed walking sheets are already at Y=10; only the moving sheets are at
Y=10.10009765625. Their inner X boundaries and central Z endpoints require
separate review against the detailed replacement. Do not lower all far bases
or all edge strips using one height rule. Far boundary reconciliation and
selected-road appearance remain outstanding before final generator integration.
No generator route or installed binary is changed by this assembly-only increment.

## Measured road appearance units (2026-10-20)

The detailed Surface 15c63410efe103a46b109b0dd6aa246d uses separate native
VTTexture backing, unlike Lift02's full VTSurface layout. Its original 4096-square
BC7 albedo was decoded and inspected. `PedestrianDraw01RoadAppearance` records
four repeat units from this prototype, not another pedestrian bridge's atlas.
Fixed faces 1035/1036 and 13243/13244 supply 4m units; moving faces 1091/1092 and
13299/13300 supply a 3.68646240234375m lateral by 6.7943115234375m longitudinal
unit. The atlas rectangle is inside the native road material island.

Both leaves have a negative longitudinal UV gradient; their origins differ.
The negative leaf must not inherit a sign-flipped projection from another style.
Fixed source density is 192 px/m laterally and 192.125/192.0625 px/m longitudinally
(the latter native half-precision difference is retained). Moving density is
208.3298/208.4464 px/m. These are measured native gradients before BC7 crop-size
rounding, not a claim about final persisted texture density. Eight measured
triangles reproduce their recorded UVs with maximum error 1.12e-8 using the new
patch parameters. Opposite lateral halves differ by an integer repeat period.

Physical partitions still own bone assignment. Appearance conversion maps fixed
positive/negative ends and moving bones 1/2 to separate patch indices, preserving
gaps. The helper awaits road preparation integration and final persisted-density
verification. Far-road boundary reconciliation remains unfinished. No new user
strings, saved resources or installed binaries are changed by this increment.
