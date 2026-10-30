# Lift01 network integration data
This report retains source measurements and stage-specific implementation evidence. Early
statements about missing callers, private branches or unfinished routing describe their recorded
stage, not the current ten-style router. Use the [documentation index](README.md),
[current contracts](agent-contract/README.md) and [shared handoff](../tools/PreparedMaterials/SharedCurrent.md)
for current requirements and delivery evidence. Recorded tests do not certify the new material scheme.


## Incremental generator integration (2026-10-20)

`Lift01BridgeGenerator` now connects the existing source, sampling, road/material,
all-LOD structure, paint, support, pillar, approach, network and unlock stages
through `MovableBridgeGeneratorBase`. The explicit router and recorded catalogue
variant no longer require the original LiftBridge01 network Prefab. Required
native DLC geometry/material/content references remain. The existing localized
Lift01 identity is reused in all 12 languages.

`Lift01SectionRoles` resolves selected-network branches from native piece layers,
including subsection wrappers: nonempty all-Side sections use the measured outer
extension branch, while surface/mixed/empty sections retain deck branches. It
does not classify geometry or use section names/widths. Native Train Side 0,
Highway Side 0 and Alley Side 0 were inspected as all-Side; Train Shoulder 2
contains Surface/Bottom/Top. Conditions remain on their original piece/section
entries. The existing barrier producer conversion applies to moving-node branches.
External missing/cyclic subsection graphs fail explicitly.

Release compilation, 25 route checks, 42 recorded-variant checks, five section
role checks and all 12 localization checks pass. These do not execute native
generation. Sidewalk/curb compatibility, unrestricted narrowing, whole-graph
size/performance, motion and cold loading remain unfinished. Historical progress
entries below describe their original stage, not present routing status.

## Independent far tower core (2026-10-20)

`Lift01FarBaseProfile` supersedes the earlier destructive seam trim. Offline
inspection established that the native coarse road-adjacent shells are already
closed, but their end stations and inner-rim datum differ from the detailed
road. Cutting away the overshoot removed the authored end caps. The replacement
uses the approved measured LOD-reconstruction exception: retain all 176 native
base vertices and original topology, record 152 reconstructed vertex positions
from the matching near/middle road boundary, then apply the existing base width
displacement. This deliberately reconstructs coarse X/Y/Z profile coordinates;
it is not a claim that ordinary width deformation may change Y or Z.

Both detailed levels independently establish X = +/-3 m, Y = 9.800048828125 m,
fixed intervals +/-[80.02734375, 95.99609375] m and moving interval
[-79.9921875, 79.9921875] m. Attribute-duplicate cap/inner-wall corners receive
the same recorded profile. The source far caps, outer walls and simplified
underside survive with their original indices, raw attributes and Surface;
only the existing eight road-top faces are removed. No new faces or vertices
are needed. The 92 retained non-road faces replace the old trim's 98 fragments
and 168 added vertices. Other platform fragments remain untouched.

Offline checks at four widths establish original bone ownership (0/4), preserved
face winding, closed shells before top removal and open edges exclusively at
the measured upper receiver after removal. This closes the prior coarse-profile
end/vertical gap at the native road envelope. Selected-road sidewalk/curb height
compatibility, complete assembled appearance, final routing and native reload
remain separate unfinished checks; topology alone does not establish them.
The original/reconstructed far cross-sections and retained fixed caps were inspected
offline at native width and half-width delta 8 m. Release compilation, 24 exact
route checks and all 12 language checks pass. This changes no user-facing strings.
The Lift01 assembly is now routed; no game acceptance is claimed.

`Lift01FarEndLips` now retains six native middle-detail road-end lips in the same
far supplement: crossing-part ordinals 30/31/32/33/493/640, 504 vertices and 288
original triangles. Four fixed lips retain bone 0 and the two moving lips retain
road bone 4. Their middle/far rest transforms and motion settings match. The
shared native model path preserves the original Surface, raw channels and indices;
the base displacement matches the existing detailed-level sign mapping. Offline
checks verify near/middle part bounds, exclusive whole-face membership, absence of
overlap with tower-core or removed road-top faces, and winding at four widths.
The tower/lip supplement now contributes 13,270 triangles. No receiver triangles
are restored and no runtime animation update is added. These lips cover only the
native shallow end profile. The retained deep caps and inner-wall closure now
come from `Lift01FarBaseProfile`, described above.
Release compilation, 24 route checks and all 12 localization checks pass after
integration. The native-width and widened end-lip geometry was inspected offline;
the close-up shows the original shallow profiles, not proof of a fully closed
bridge. No user-visible strings changed. This uncalled Lift01 stage is locally
built, not deployed or game-accepted; native motion and cold-load verification
remain part of the deferred batch acceptance.

`Lift01FarTowerCore.g.cs` now supplies a measured geometry plan for 264 complete
native middle-detail parts: tower crossbar/centre-column assemblies, independent
crossbars, lower arched members, top housings, longitudinal side frames and beams,
upper transverse members and connectors, and the two end
roof grids with their twelve central infill sheets. The source is
`LiftBridge01_LOD1 Mesh`, SHA-256
`c5694dce731ce05218de15761665cc1d234723049c48bd707c87587509ed8e4e`.
The plan retains 24,494 original vertices and 12,854 original triangles before
roof-boundary subdivision. The two roof grids use the same measured rigid-side /
central-span boundaries as the detailed model: 56 source faces become recorded
fragments with 128 interpolated edge vertices, giving 12,982 output triangles.
Source-area checks preserve the roof surface. This is a targeted tower contribution;
it does not copy the whole middle-detail bridge or simplify its road.

Offline checks establish exclusive complete-face membership and fixed bone 0 for
all selected vertices. Centre columns remain stationary; complete crossing members
retain their existing own-span motion. The four side parts translate rigidly,
with exact relative-position checks and matching near/middle part bounds. Compact
immutable index ranges encode motion without runtime part discovery. Original Y/Z,
indices and source channels are retained except for those measured roof fragments,
whose new channels use the shared native interpolation path. Native and widened
front, side and roof projections were inspected; winding and
identity checks pass at half-width deltas -0.1, 0, 2 and 8 metres. Release compilation
and the 12-language localization check pass; this stage adds no user-visible text.

`Lift01RenderAssembly` now attaches this plan through the existing compact supplemental
structure path, using middle mesh slot 0 and its unchanged native Surface. The recorded
middle/far root and road-bone rest transforms are identical; tower vertices use
bone 0 and end lips preserve bone 0/4. No separate child transform or runtime update is introduced. The assembler
also connects the detailed structure/glass plans and far side/base/moving/platform
plans to the prepared selected-road levels.

`Lift01FarStructure` removes the 36 native tower-proxy faces (0–17 and 388–405).
An ownership audit verifies their 72 exclusive vertices include all 40 otherwise
unmapped far vertices, and their faces do not overlap road removal or platform
fragments. Native fixed diagonal groups 2/4/14/16 remain intact: their corresponding
middle-detail parts 3254/3256/3258/3260 are deliberately excluded from the supplement,
avoiding duplicate braces. Other native rooms, stairs and moving parts remain.

Selected-layout compatibility and native generation/persistence checks remain
unfinished. The 12,982 triangles are the tower-core contribution,
not the final far bridge triangle count or an accepted performance measurement.
Its cost is materially larger than the original baked far proxy and must be included
in the final all-LOD geometry/resource-graph and generation-cost review. Crossing
connectors retain the detailed pipeline's own-span stretch, including visibly wider
central connector plates at large positive deltas; offline winding does not establish
human appearance acceptance or unrestricted narrowing support.

The new projection/motion research demonstrates why a single affine deformation of
the existing far tower atlas is insufficient: its projected side columns and crossing
bars require different displacement at overlapping locations. A diagnostic 800x2400
projection at half-width delta 8 m finds about 1.7 m disagreement. This is evidence
for separate part representations, not a runtime classifier or full correspondence
proof. Human motion, appearance and cold-load verification remain deferred.


`Lift01NetworkData.g.cs` preserves the native LiftBridge01 network's independently audited section rules. Its eleven entries comprise two side occurrences with one branch each and three deck occurrences with three branches each. The original native layout uses train side, shoulder and track sections; that is evidence for these roles, not a requirement to replace the selected road with train sections.

| Role | All | None | Hidden layers | Half length |
| --- | --- | --- | --- | --- |
| Side | Node | FixedNodeSize | 0 | false |
| Deck ordinary node | Node | FixedNodeSize | 0 | false |
| Deck moving node | Node, FixedNodeSize | empty | -1 | false |
| Deck edge | empty | Node | Bottom (2) | false |

The caller supplies an explicit role for every selected-road occurrence through
`Lift01SectionRoles`. Source requirements, inversion, median and placement are
retained by the shared subsection converter. The data class does not infer a role
from a section name, position or width.

## Elevated width baseline

The generation measurement is **6 m**, replacing the old catalogue value of 26 m.
The native `Train Shoulder 2` section (path ID 121088) selects `Train Shoulder Piece 1`
(121071) when Elevated is active and SidePlatform is absent. Its 2 m piece excludes
Elevated and therefore must not contribute to this measurement. Count the two shoulder
occurrences once across their conditional branches, together with the 4 m centre track:
`1 + 4 + 1 = 6`. Outer side structure and bottom layers do not add road-surface width.
The native near and middle bone-4 horizontal road surfaces independently span X=-3 to
3 m at Y=9.800048828125, with Z endpoints +/-79.9921875. The far proxy has different
geometry and must not redefine this high-detail baseline. `BridgeMeasurements.Lift01RoadWidth`
is the shared recorded value for the forthcoming generator and existing catalogue consumers.

## Measured longitudinal road ownership

`Lift01RoadPartitions.Carriageway` records the actual near/middle road intervals:

| Object-space Z interval | Bone array index | Native connection |
| --- | --- | --- |
| -95.99609375 to -80.02734375 | 0, fixed root | 0 |
| -80.02734375 to -79.9921875 | Gap | None |
| -79.9921875 to 79.9921875 | 4, TrainTracks | 100 |
| 79.9921875 to 80.02734375 | Gap | None |
| 80.02734375 to 95.99609375 | 0, fixed root | 0 |

Outside these intervals is not mechanism-owned road. Binary geometry decoding confirms
64/32 fixed-top triangles and 320/160 moving-top triangles at near/middle LOD, respectively,
all at Y=9.800048828125. Fixed projected area is 191.625 square metres and moving area is
959.90625 square metres in both levels. The two motion gaps are each 0.03515625 m.
All generated LODs must use these physical boundaries; the original far proxy's different
height and extents are not a new road datum. Bone 3 is a counterweight, not the moving road.
This partition does not yet establish sidewalk ownership, far-shell replacement or complete
structural triangle membership. The incomplete Lift01 generator is not registered.

`Lift01RoadTops.g.cs` now records the verified 384 near and 192 middle horizontal road-top
triangles, plus eight reviewed far road-sheet triangles (fixed root and moving bone 4).
`tools/GeometryMetaprogram/Lift01RoadTops.py`
checks the native Geometry SHA, exported topology/counts, bone ownership, positive winding,
surface height, width, interval endpoints and projected area before emission. Runtime checks
the recorded triangle indices and changes only the caller-owned removal mask. It does not
classify by height, remove undersides/side walls, change UVs or cut triangles. The far fixed
sheet consists of faces 320/321/708/709; the moving sheet is 342/343/730/731. Moving sheets
slope from Y=10.240234375 near the centre to 10.2001953125 at the ends and therefore cannot
be identified by the horizontal-plane audit. The far moving underside faces 338/339/726/727
remain intact, as do the high side caps at Y=12.8779296875. The far inner rim is above the
near road datum and is wider/longer than its physical boundary, so removing these sheets
alone is not a complete far replacement. Side/end closure must be reconciled with the
near geometry before the complete assembler uses this stage; no visual acceptance is claimed.

No extra edge/node states exist on the audited native network, so no Draw01/02/03 state list is appended. Bridge segment length is 168 and water elevation is 10. Native pillar placements are Y=-2.75 with Z=13.5 and Z=0. The mechanism placement is Y=77.87999725341797, and the outside connection is Y=8. All other placement fields are retained. The caller supplies the generated pillar/mechanism and the selected network's appropriate outside connection.

The original records were authored on isolated branch `bridge/lift-01`. `Lift01NetworkAssembly` now composes these records on the private selected-network clone, adopts every generated section/piece into the factory persistence graph and installs the owned ordinary pillar and mechanism. Structure placements are separate from the retained native outside-connection evidence: assembly preserves the selected network's complete outside-connection rules instead of imposing the prototype train connection's 8 m anchor. No additional bascule state rules are introduced. Callers still must provide resolved occurrence roles and completed appearance objects.

This is measured network data and an explicit conversion entry point, not a completed generator. Structure generation, selected-road visible motion and marking conversion, route integration, cold-start persistence and human traffic/shipping acceptance remain incomplete. No caller exists yet; existing bridge output is unaffected and no bridge cleanup is required. No game control or visual-generation unit tests are performed.

## Complete transverse web plates

`Lift01TransverseParts.g.cs` records 132 complete transverse members at near and middle LOD.
The offline review separates these solid web plates from the joined U frames with side
uprights. Forty-eight plates span X=+/-3.62957763671875 m; the two wider moving end plates
span X=+/-5.593505859375 m. The latter are crossing groups 492 and 639. Thirty complete
upper transverse beams additionally span X=+/-6.1463623046875 m, with 2,656 vertices
in each LOD and identical member bounds. Their explicit ordinals are recorded separately
from joined vertical/horizontal assemblies. Another 52 transverse tie plates span
X=+/-5.9893798828125 m: 26 shaped plates and 26 flat horizontal sheets, together adding
1,264 vertices per LOD. Their exact envelopes correspond across levels and their complete
faces are exclusive to those parts. Each member uses
its own original span for horizontal affine widening, with Y/Z unchanged. No triangle is
cut and no bone, index, UV or material channel is replaced by this stage.

`Lift01TransverseParts.py` emits 16,778 near and 15,696 middle vertex memberships from
explicit reviewed component ordinals. Both LODs have exactly matching member envelopes.
The offline checks cover exclusive whole-face ownership, disjointness from rigid side
parts and road bases, and preserved winding/nondegeneracy at half-width deltas -0.5, 0,
2 and 8 m. The generated stage is not yet called: joined frames, far geometry, remaining
members and the final render assembler still need completion. This is not visual acceptance.

## Joined transverse U frames

`Lift01JoinedFrames.py` identifies 52 fixed/moving U frames from the detailed prototype,
with exactly matching middle-LOD bounds. The inner upright faces lie at X=+/-3.39190673828125 m;
ten frames use the quantized value +/-3.3919677734375 m in both levels. Those measured
surfaces separate the rigid side uprights from the two crossing beams. Side vertices translate
by +/-half-width delta; the beams stretch over their own interval between the inner faces.

The emitted `Lift01JoinedFrames.Complete` uses immutable vertex weights and recorded edge
interpolation recipes. It preserves 24,833 / 15,544 original near/middle vertices and adds
1,628 / 1,062 edge vertices to subdivide 916 / 635 original triangles crossing the part
boundaries. Runtime performs no intersection or classification. `MovableStructureModel`
retains original channels and interpolates attributes at the recorded edges. The assembler
must retain these fragments when it adds road removal and replacement stages.

Offline checks cover complete face ownership, disjointness from rigid side groups and road
bases, source area conservation, unchanged Y/Z, rigid side displacement, and positive winding
for all nondegenerate frame faces at deltas -0.5, 0, 2 and 8 m. A native-geometry plot compares
both levels before/after 8 m total widening and confirms unchanged upright widths. This is
geometric inspection only. Far-LOD correspondence, remaining components and final render
integration are still outstanding; no in-game acceptance or deployment is claimed.

## Road-adjacent end lips

The base displacement also covers the six measured end lips (crossing groups 30, 31,
32, 33, 493 and 640). They directly adjoin the fixed/moving road shells, with identical
near/middle bounds, and retain the original stepped side profile. Their additional
512 / 504 vertices bring `Lift01Bases` to 4,240 / 4,026 vertices. The mapping remains
`x + sign(x) * delta`, with recorded centre vertices stationary; it is not uniform scaling.
Whole-face membership, winding and four-width checks still pass, and the minimum noncentre
base distance remains 2 m. The transverse/frame emitters verify disjointness against these
additional base groups. Road/sidewalk integration and far-shell closure remain unfinished.

Prototype-independence review: removing the movable base class's `variant.IsAvailable`
condition alone would not implement prototype-independent generation. Catalogue/UI/settings,
preview/creation gates, `BridgeUnlockPolicy` and composition consumers also read the original
variant donor. Those consumers require a coordinated measured-source change preserving native
content ownership and unlock rules; no prerequisite bypass has been introduced.

## Independent crossing connectors

`Lift01Connectors.g.cs` covers 152 reviewed independent connection plates, bracket bodies and truss webs:
diamond plates, upper/lower brackets, flanged brackets, tall tapered plates and the two
repeated tower-connection plate profiles, plus 26 independent vertical truss webs. Their half-spans are recorded as 1.32733154296875,
0.7254638671875, 1.04193115234375, 0.85174560546875, 0.6304473876953125 and
1.247161865234375 m; the truss webs have half-span 0.3841094970703125 m. Every complete selected member crosses the centreline and therefore
uses its own-span horizontal affine transform, rather than a global bridge-width factor.
The stage does not classify or stretch joined tower uprights.

`Lift01Connectors.py` emits explicit membership for 13,316 near and 11,800 middle vertices.
All 152 near/middle bounds agree exactly. The offline checks verify exclusive whole-face
ownership, exclusion of side/base groups and transverse/frame ordinals, unchanged Y/Z,
expected span, and positive winding at three nonnegative half-width deltas (0, 2, 8 m)
and a narrowing sample of max(-0.5 m, -quarter original span). The narrower web parts
therefore use a valid shrinking sample, not the collapsing -0.5 m displacement. No new vertices,
indices, material or skinning assignments are produced. A narrower width that collapses a
member fails explicitly; full selected-road width compatibility is not established by these
four samples. These tables still await the complete assembler and far-LOD treatment.

## Glass submesh

`Lift01Glass.Create` supplies the independent mesh-1 plan for near and middle LOD.
Both native exports contain exactly 40 four-vertex glass quads (160 vertices, 80 triangles),
all on fixed bone 0, with identical vertex positions and index ordering across the two levels.
Vertices 0..79 belong to the negative-X control-room/tower windows; 80..159 belong to the
positive-X windows. Each side translates rigidly by its half-width delta. The plan changes
positions only and retains native indices, bone channels and the separate glass Surface slot.

`Lift01Glass.py` verifies quad topology, original bone groups, side membership, exact LOD
correspondence and triangle-edge preservation for deltas -0.5, 0, 2 and 8 m before emission.
No geometry classification runs during generation. LOD2 has no separate glass mesh and must
retain its native single-surface representation in the far plan. The completed glass plan
still awaits the full Lift01 render assembler; it does not establish runtime acceptance.

## Joined tower crossbars and centre columns

Crossing groups 280 and 308 contain twelve horizontal crossbars joined geometrically to two
centre columns. Original index ownership separates their faces: at each LOD, 1,152 vertices
belong to crossbars and 672 to columns, with no triangle sharing the two categories.
`Lift01TowerFrames.py` verifies this separation against both native levels before emitting
`Lift01TowerFrames.Apply`. The centre columns keep their original positions and width.
Crossbar vertices occur only at the centre or their recorded outer endpoints; centre vertices
stay fixed and outer endpoints move by +/-delta. This is the crossbars' own-span mapping,
not scaling of the whole joined tower. No additional vertices or triangle subdivision is needed.

The three original endpoint magnitudes (7.6124267578125, 7.612548828125 and
7.6126708984375 m) and the column half-width 0.339599609375 m are checked offline.
Runtime uses immutable index ranges only. Four-width checks preserve all column positions,
Y/Z and positive face winding. Both LODs have identical component envelopes. This stage
still awaits full structure assembly, along with the other remaining crossing groups and
far-LOD correspondence; it does not establish visual acceptance.

## Complete tower crossmembers and counterweights

`Lift01TowerBodies` handles the complete middle crossbars (groups 287/315), arched lower
crossmembers (334/335) and counterweight shells (344/345). Their measured half-spans are
7.6124267578125, 7.5927734375 and 6.3062744140625 m, respectively. Each entire member
crosses the centreline and uses its own-span horizontal affine mapping. Both native LODs
have identical member envelopes, with 1,024 near / 890 middle vertices in total.

The offline emitter verifies whole-face ownership and disjointness from previously completed
side, base, connector and frame groups. It also reads the exported native face bone groups:
the four fixed crossmembers belong to bone 0, and both counterweights belong to bone 3.
The runtime plan changes only X positions; it never rebinds the counterweights to the road's
bone 4. Four-width checks preserve Y/Z, span and positive winding without additional cuts.
Final render assembly and far-LOD correspondence are still required.

The tower-body table additionally covers twelve horizontal roof sheets (groups 23..28,
337..342) and four sloped connecting panels (169/170/263/264). All are fixed bone 0;
near/middle bounds match exactly and no faces share unrelated vertices. Together with the
six earlier bodies, the table now covers 22 parts and 1,104 / 970 vertices. The asymmetric
quantized bounds of the negative-end roof sheets are retained rather than made symmetric.
The independent vertical truss webs in `Lift01Connectors` are distinct from the joined
centre columns in `Lift01TowerFrames`; the latter remain stationary. Width compatibility
for narrow selected roads remains unresolved and must not be inferred from valid samples.

## Tower roof grids

Joined groups 29 and 343 are continuous roof grids, not solid transverse plates. Their
outer grid bars retain their widths under rigid +/-delta translation; only the central
connecting strips widen. Group 29's inner boundaries are X=-0.8794708251953125 and
+0.879486083984375 m. Group 343 uses +/-0.879486083984375 m. Both detailed LODs
have the same boundary coordinates and 1,112 original vertices per grid.

`Lift01JoinedFrames` now includes both grids with its 52 U frames. Combined counts are
27,057 / 17,768 original vertices, 1,756 / 1,190 added edge vertices and 972 / 691
subdivided original triangles. Relative to the U-frame stage, each LOD adds only 56 split
triangles and 128 interpolated edge vertices. The offline emitter preserves the asymmetric
grid boundary, validates original areas and verifies rigid outer vertices and positive
winding at four width samples. No runtime intersections or material/skin reassignment are
introduced. A narrow road collapsing the grid's central span remains explicitly unsupported
pending the full width-compatibility work.

## Tower housings and railway overhead removal

The tower-body table additionally includes top housings 171 and 265, bringing it to
24 parts with 1,668 near / 1,360 middle vertices. The middle housings have slightly
different quantized X bounds and one lower top edge; each LOD uses its own measured
span instead of forcing the near envelope onto simplified geometry.

`Lift01Catenary.py` identifies seven original railway pole assemblies (24 side groups
and four crossing arms per station), two longitudinal wires and six droppers. The
seven stations include four fixed approach poles and three moving-deck poles. The
reviewed assembly projection confirms the mast, foot, insulators and crossarms as
railway facilities, distinct from the structural trusses. Side group starts are
2138, 2162, 2186, 2210, 7074, 9768 and 9902; wire/dropper groups are
2234..2237, 7072..7073 and 9766..9767. Crossing groups are 74..89,
488..491, 635..638 and 641..644.

The generated `Lift01CatenaryRemoval` table removes 12,476 near / 5,999 middle
whole original triangles (10,199 / 6,487 vertices). Offline checks verify matching
LOD membership envelopes, exact source hashes, exclusive whole-face ownership,
and disjointness from the road-top and rail/fastener removal stages. No road cuts,
new material, skinning reassignment or runtime spatial classification is introduced.
The table must be consumed by the final road-replacement assembly together with
rail and sleeper removal; selected-road facilities must then supply the requested
electrification. Far-LOD handling and final generator integration remain unfinished.
This stage adds no user-visible strings and does not change localization behavior.

## Detailed road replacement composition

`Lift01RoadReplacement.RemoveDetailedRoad` combines all four detailed-LOD removal
tables on an already constructed main-mesh plan. It validates the source layout and
rejects any road-removal face also owned by a structural replacement fragment before
modifying the plan. It then unions the mask in place, retaining the plan's original
and constructed positions, interpolated attribute recipes, fragments and prior removals.
In particular, explicit widened X coordinates on roof grids and joined U frames are
not recomputed by interpolating widened endpoints.

`Lift01RoadReplacementCheck.py` checks the emitted top-face indices against the native
mesh, verifies mutually disjoint removal stages and verifies that none overlaps the
972 near / 691 middle structural replacement fragments. Combined removal counts are
60,636 / 42,715 triangles, leaving 319,842 / 185,539 original structural triangles
before joined-frame subdivision. These counts are not final generated bridge counts:
selected-road meshes, paint and constructed fragments must still be included.

The compositor deliberately accepts only LOD0 and LOD1. LOD2 needs its own measured
shell/closure construction; feeding detailed removal tables to the far mesh is invalid.
Full render assembly, the remaining platform transforms and generator routing are still
pending. No persisted asset, shared generator or existing Draw01 route changes in this stage.

## Rounded end-platform construction

Joined groups 20 and 21 contain the two fixed rounded platforms, at Z approximately
+/-80.31 m and Y=4.6015625..7.15576171875 m. The innermost perimeter tangent
lies at X=+/-9.082763671875 m in both detailed LODs. Other beveled perimeter rings
start farther out (+/-9.0859375 and +/-9.101318359375 m). Preserve every curved
ring by translating the portions outside the innermost tangent rigidly. Widen the
central connecting portion between those measured boundaries. The near-only fan
vertices at X=+/-8.6328125 m are interior triangulation, not perimeter landmarks.

Both platforms now participate in `Lift01JoinedFrames.Complete`, using original
attributes and offline edge subdivision. They add 1,168 / 946 original vertices,
216 / 108 edge vertices and 116 / 61 split triangles. The combined joined plan has
28,225 / 18,714 original vertices, 1,972 / 1,298 edge vertices and 1,088 / 752
split triangles. The road-removal composition check is updated for these counts;
all added platform fragments remain disjoint from road/facility removals.

Four-width checks preserve Y/Z, rigid end coordinates, source area and face winding.
Native/widened X/Z projections were inspected for both LODs; the rounded perimeter
remains unchanged in shape. This does not establish in-game visual acceptance or
complete the far platform, support-column, sidewalk or final generator work.

## Centreline pier columns

All three native pillar LODs contain two complete capsule-shaped columns at the
positive and negative longitudinal ends. Offline welded-position connectivity finds
480+480 near vertices, 690+684 middle vertices and 110+114 far vertices. Each
column extends across X=0; these are not a left/right pair of independently placed
columns. Their authored lateral placement is centred, so the column-translation rule
leaves them in place rather than widening their bodies or splitting them into halves.
Small native far-LOD quantization asymmetries remain unchanged.

`Lift01SupportAssembly.CreateColumn` consumes the privately reconstructed native
render graph from `Lift01SourceRenders` and builds three unique owner geometry levels.
It preserves all original positions, indices, material slots, native bounds and raw
attributes through `MovableStructureModel`; no triangles are removed or added.
Counts are 960 vertices / 1,296 triangles, 1,374 / 776 and 224 / 100.
The common render persistence path carries the original LOD and vertical stacking
configuration. Temporary decoded meshes are released on both success and failure.

This differs from the widened road-adjacent base and platform inside the mechanism.
Footing construction and final generator integration remain outstanding. No original
prototype Prefab is needed by this column stage; built-in geometry/material CIDs still
require the native DLC resources. No user-visible strings or localization keys change.

## Detailed footing collars and fenders

Native footing geometry contains 70 parts in each detailed LOD: two capsule collars,
eight curved transverse fenders and sixty side posts/brackets. The collars' straight
segments end at tangent-ring X coordinates 9.08837890625..9.111328125 m; fenders use
8.011962890625..8.0263671875 m. Their original straight-segment vertices are already
at the centre or tangent ends. Every triangle beyond these tangent bands belongs to
one rigid side. Consequently the original topology represents central widening and
rigid curved ends without subdivision. Side posts and brackets translate as complete
parts. This is distinct from the unchanged centreline pier columns.

`Lift01Footing.py` emits immutable directional ranges for every original vertex:
484 ranges for 10,836 near vertices and 251 ranges for 11,530 middle vertices.
`Lift01Footing.CreateDetailed` consumes these ranges without classifying geometry at
runtime. Original triangle counts remain 8,960 / 5,376. Offline checks cover every
vertex once, exclusive whole-face ownership, measured tangent boundaries, rigid side
shapes, unchanged Y/Z and face winding at four width samples. Native X/Z projections
were inspected to distinguish collar rings, curved fenders and posts.

The far footing has only two merged components (717 vertices / 500 triangles); it
must receive its own measured correspondence. It is not supported by this detailed
constructor yet. Full three-level footing assembly and generator integration remain
pending. No user-facing text or localization behavior changes in this stage.

## Three-level footing assembly

`Lift01FarFooting.py` now records offline correspondence from the 717 native far
vertices to the detailed collar, fender and post surfaces. It evaluates the detailed
piecewise motion on those measured source triangles and emits 717 immutable weights.
Runtime never performs this proximity analysis. Far post vertices retain rigid side
weights; coincident native seam vertices receive identical weights. The original
500 far triangles, their native positions at zero delta, Y/Z coordinates and material
attributes are retained. The native simplified shell is not replaced by detailed meshes.

The simplified shell differs geometrically from the detailed input: the maximum
correspondence distance is 0.684176 m (median approximately 0.063473 m). This records
native LOD approximation, not exact geometric identity. Native/widened projections
were inspected and four width samples (-0.5, 0, 2, 8 m per side) preserve positive
triangle orientation and coincident seams. These checks do not establish final
in-game appearance or acceptance of all road widths.

`Lift01SupportAssembly.CreateFooting` now assembles all three native footing LODs,
using the detailed transforms for LOD0/1 and the recorded correspondence for LOD2.
The support factory shares decoding/cleanup with `CreateColumn`, preserves native
materials, LOD order and stacking/base properties, and generates owner-local geometry
through the existing common persistence path. Column bounds remain unchanged; footing
bounds follow the requested lateral displacement. Main mechanism far geometry and
the final concrete generator remain unfinished. No user-visible strings were added.

## Detailed main-mechanism composition

`Lift01DetailedStructure.Create` now runs the measured side, base, transverse,
connector, tower-frame and tower-body transforms, finishes joined-frame/platform
subdivision and applies combined native-road removal. It preserves constructed
vertices and fragment recipes when removing road facilities. Glass remains a separate
mesh handled by `Lift01Glass`; the new entry point accepts main-mesh LOD0/1 only.

The body table additionally covers small fixed plates 22 and 336, whose bounds
cross X=0 and are identical in both LODs. Under the crossing-part rule they use their
own measured spans, not a global bridge span. Total body coverage is now 26 parts /
1,708 near and 1,400 middle vertices. These small spans further constrain narrowing;
four-width checks use a noncollapsing negative sample for each member. This is not
proof that narrower selected roads are supported, or that large widening has received
visual acceptance.

The road-replacement checker now verifies exclusive transform ownership in addition
to removal/fragment separation. All 377 retained crossing groups and all authored
side groups are covered; every vertex of every surviving original near/middle
triangle has exactly one measured part assignment. The other crossing groups are
the 240 removed sleepers and 28 removed catenary arms. Main-mechanism LOD2, final
appearance/network assembly and the concrete generator route remain outstanding.

## Native far moving-part ownership

The native main far mesh has 1,110 vertices / 776 triangles. Its exported face
groups contain 710 fixed-root vertices, 136 vertices on each wheel bone (1/2),
48 counterweight vertices on bone 3 and 80 moving-road-shell vertices on bone 4.
Every exported triangle index is checked against the decoded native geometry;
no vertex belongs to more than one bone group.

`Lift01FarMovingParts.py` records 59 immutable ranges for all 400 moving vertices
(324 original triangles). Each wheel triangle lies wholly on one side, so the
left and right wheels translate without changing their shape. Far counterweight
boxes use only their own X endpoints at +/-6.4205322265625 m, whose affine
weights are exactly +/-1. Moving road-shell sides follow their measured lateral
base displacement. The generated runtime stage changes X only and never rewrites
bone indices, bind poses, materials or triangle indices.

Four width samples preserve wheel shape, Y/Z and triangle winding. This stage
does not remove the far road top or construct the necessary closure to the selected
road height; it is not a complete far structure plan. The 710 fixed vertices still
need measured transformations before final three-level mechanism assembly.

## Native far fixed-side membership

Offline connectivity and position review identify 25 fixed-root components in the
native far main mesh. Twenty are wholly lateral stair/room/attachment components;
their exact memberships are recorded by `Lift01FarSideParts.py`. These 424 vertices
use 19 immutable signed ranges and rigid half-width displacement. Every involved
face is wholly owned by its component, all source vertices belong to bone 0, and
four width samples preserve complete relative vertex coordinates.

The remaining five fixed components are excluded deliberately: the merged tower
walls (72 vertices), two rounded platforms (59 vertices each), and two fixed road
shells (48 vertices each). The tower walls carry both lateral and crossing parts in
their native far representation and must not be stretched as one undifferentiated
box. The side table and moving table together cover 824 of the 1,110 original far
vertices; the remaining 286 and road closures still require completion before routing.
No materials, bone indices, source topology or user-visible text change in this stage.

## Far rounded platforms

`Lift01FarJoinedParts.Complete` now includes both fixed rounded platforms, adding
118 original vertices to measured far coverage. The far tangent-ring boundary is
X=+/-9.111083984375 m; the original outer envelope remains +/-16.85595703125 m
at zero width delta. These native simplified coordinates correspond to the detailed
platform's tangent rings; they are not replaced with high-detail positions.

Offline subdivision separates the rigid rounded ends from the widening straight
portion on 44 original triangles, adding 96 interpolated edge vertices. At this
stage the complete main mesh would contain 888 triangles before road removal and
remaining tower/closure work. Original area, Y/Z, complete platform width change,
rigid ends and winding pass four-width checks. All non-platform source vertices
remain untouched by this stage, so the prior moving and fixed-side transforms can
be supplied before completion. Existing source attributes are interpolated through
the shared structural model; no new material or bone assignment is introduced.

Measured far original-vertex coverage is now 942/1,110. The remaining 168 vertices
are the merged tower walls and fixed road shells. Road closure and complete render
assembly remain outstanding; these counts are not final generated-asset counts.

## Far fixed and moving road bases

`Lift01FarBases` now owns the two fixed road shells (96 vertices, bone 0) and the
moving road shell (80 vertices, bone 4). These are the far representations of the
road-adjacent bodies handled by `Lift01Bases` in LOD0/1. The native simplified
coordinates are retained: X endpoints include +/-4.0416259765625 m and inner
edges at +/-3.245361328125 and +/-3.28533935546875 m. The far shells have different
height and end approximations from detailed geometry; no detailed coordinates are
substituted into their existing vertices.

The emitter records 56 immutable signed ranges for 176 vertices / 100 triangles.
Checks compare decoded face order against the native bone-group export, verify
whole-face ownership, preserve Y/Z and unrelated vertices, and test four widths
(-0.5, 0, 2, 8 m per side) for winding, side rigidity and complete width increments.
Road-base processing changes positions only; it does not remap bones or materials.

`Lift01FarMovingParts` now covers only wheels and counterweights (320 vertices /
280 triangles / 33 ranges). Its former 80 road vertices moved to the base stage;
they must not be processed twice. Together with fixed side parts and platforms,
recorded original-vertex coverage reaches 1,038/1,110. The remaining 72 belong to
the merged tower walls. Native road-top removal and selected-road closures remain
separate unfinished integration work, so these stages are not a complete generator.
No user-visible text, persistence format or existing generated asset is changed.

## All-LOD native road removal

`Lift01RoadReplacement.RemoveNativeRoad` now accepts all three main-mesh LODs.
LOD0/1 retain their existing combined top, sleeper, rail and catenary removal;
LOD2 removes only its eight recorded road sheets. Detailed facility ordinals
must never be applied to the unrelated native far topology. The operation mutates
only the removal mask after validation, preserving constructed X coordinates,
interpolated vertex recipes and existing structural fragments.

The replacement checker now includes far topology and bone ownership, upward
sheet orientation, native side-cap/underside preservation and nonoverlap with
all 44 platform fragment recipes. LOD2 retains 768 original faces before the
platform subdivision is applied. This is a topology check, not a complete road
closure or in-game acceptance result. The detailed caller uses the same renamed
interface; no other style or asset lifecycle path changes.

Further inspection of the remaining tower component confirms that its 72 vertices
form 36 triangles spanning both end towers and the upper longitudinal truss.
The native upper panels extend from Z=0 to +/-82.88671875 m, with lower panel
edges near Y=67.41 m and upper edges at Y=80.578125 m. Lower end panels cover
Y=12.73681640625..67.51171875 m. These are merged representations of multiple
authored parts, not one crossing beam. A whole-component affine stretch would
also stretch tower columns and is not an acceptable implementation. Its measured
part correspondence or permitted derived LOD representation remains outstanding.

## Far tower lateral panels

The native far tower component has now been separated by face ownership. Faces
0–3, 10–13, 388–391 and 398–401 are lateral proxy panels on fixed bone 0. Their
32 vertices do not share indices with the transverse end or roof panels. They
represent the translated sides of the detailed towers and upper longitudinal
structure, so `Lift01FarSideParts` applies rigid signed translation to them.
The existing 424 side vertices plus these 32 vertices produce 26 signed ranges.
No UV, material, skeleton, triangle connectivity or Y/Z coordinate changes are
introduced by this step.

The emitter checks the native Geometry hash, compares face order with the bone
export, verifies complete-face ownership and checks rigidity at deltas -0.5, 0,
2 and 8 metres. The remaining 40 tower vertices belong to 20 transverse panels;
they are deliberately excluded from the rigid side table. Across the existing
far stages, original-vertex coverage is now 1,070/1,110, not a complete far mesh.

Offline inspection also resolved the original far Surface
`e0101c8ca7e9dc14016d187d012704a4` to VTSurface
`945c11faccb3838b5e4514358482af2e` and decoded its native 512-by-512 BC7 albedo
with original mesh UV0. These panels combine textured structural features;
their bare triangle outlines do not justify uniform width scaling. The
transverse mapping, road closures and complete generator connection remain
unfinished. No deployment or in-game acceptance is claimed. This change adds
no user-visible strings and does not alter localization or asset persistence.

## Selected-road appearance preparation

`Lift01RoadAppearance` records a two-by-four metre repeat unit from the native
near floor, rather than stretching the whole atlas to the selected road width.
Its atlas rectangle is (0.57421875, 0.260009765625) to
(0.6796875, 0.469970703125). At the original 4096 texture resolution its measured
gradients are 216 texels/metre across the road and 215 texels/metre along it.
This is Lift01's prototype floor density, not the generic native Road texture's
409.6 texels/metre. The source albedo was inspected: rails and sleepers are
separate geometry, not painted into this gravel floor unit.

Eight patches preserve the measured transverse and longitudinal mirroring and
the distinct fixed/moving longitudinal origins. Fixed sample faces are 11144,
13500, 15858 and 18220; moving samples are 259030, 259630, 371276 and 371876.
`Lift01RoadAppearanceCheck.py` decodes the actual native Float16 UV0 channel and
checks all eight projections exactly against these original triangles. The
same near-derived repeat density is supplied to every generated LOD.

`Lift01RoadPreparation.PrepareRenderLevels` connects this recipe to the shared
selected-road preparation pipeline. All selected deck roles use the existing
fixed-bone-0/moving-bone-4 partition and its two physical gaps; Lift01 has no
separately rotating pedestrian leaf. The selected occurrence and lane layout is
retained, and the existing single-sided skinned marking stage remains separate.
No receiver cuts are introduced for texture repetition. The original five-bone
layout is retained, including the counterweight on bone 3.

The preparation entry point still awaits the complete Lift01 generator and
render assembly. Sidewalk appearance, asymmetric inputs, closure geometry,
all-LOD rendering and native motion require the deferred game validation; this
entry point does not claim those results. Existing asset loading, migration and
deletion are unaffected. There are no new user-visible strings.

## Ordinary network pillar generation

`Lift01PillarSources` reconstructs the nine render inputs of native object
`LiftBridge01NetPillar` (audit object 118754), independently of the mechanism's
stacking supports. Its measured top/column/foundation LOD graphs retain their
original Geometry and Surface CIDs, native bounds, stacking, base and content
components. The object retains PillarType 2, zero anchor offset, vertical offset
range [-1, 1], and its StandingObject footprint (15, 0.5, 15), zero leg gap and
non-circular leg. These records come from the native 1.6.2f1 audit, not another
bridge's support. Runtime reconstruction resolves built-in resources; it does
not resolve the original Lift01 pillar Prefab.

`Lift01NetPillarGeometry` and `Lift01PillarAssembly` implement the ordinary
support's width transform. Inspection of the actual three-level geometry shows
one central hollow column and its foundation. Both retain their original shape
and location. The road-adjacent top cap uses the base sign-displacement rule.
The near and middle top each contain forty separately identified lateral
braces/connection plates; each is translated rigidly on its original side.
The coarse cap's thirty triangles crossing the centre are subdivided at the
recorded centre seam. Its added vertices interpolate native attributes, keep
original Y/Z and stay at X=0, matching the detailed cap's existing centre seam.
No geometry classification or prototype intersection runs during generation.

| Ordinary support part | LOD0 triangles | LOD1 triangles | LOD2 triangles |
| --- | ---: | ---: | ---: |
| Top, generated | 1,076 | 644 | 360 |
| Top, original | 1,076 | 644 | 300 |
| Central column, unchanged | 144 | 86 | 100 |
| Foundation, unchanged | 112 | 66 | 100 |

The source emitter validates the complete component sets and reconstructs all
native resource identities. The geometry emitter checks original hashes/counts,
complete detailed part membership, unchanged Y/Z, unchanged column/foundation,
native-width surface area and winding at half-width deltas -1, 0, 2 and 8 metres.
Release compilation passes. This adds no user-visible strings or asset naming
policy; existing multilingual and persistence behavior is retained.

The ordinary-support assembly is ready for the full Lift01 generator caller.
This does not complete the mechanism's outstanding far transverse mapping and
road closures, approach structure, section-role integration or full generation
route. No deployment or in-game motion/cold-load acceptance is claimed here.

## Fixed-span network structure inputs

The actual fixed-span source is `LiftBridge01Net Mesh` (79466), referenced by
`LiftBridge01 Section` (118601) through native `OverheadNetSections` (11942).
It is not a Draw02-style bottom-piece family. Its native layer value 2 is
`NetPieceLayer.Top`; the layer-mask value for Top is 4, not 2. The source section
places it at Y=-10 with RequireAny Edge/Elevated and RequireNone Node. The two
overhead occurrences retain median placement: the ordinary occurrence excludes
FixedNodeSize, while the FixedNodeSize occurrence uses HalfLength. These rules
must remain separate from the selected-road section conversion.

`Lift01ApproachSources` reconstructs its NetPiece and two render LODs from the
same hash-checked native audit. It retains the 8 m piece width, 64 m length,
original height range and bounds, zero offsets/surface heights, native material
identities and `NetPieceTiling.m_DisableTextureTiling=true`. Native mesh sizes are
80,210 / 38,399 / 1,484 vertices and 71,900 / 24,540 / 1,020 triangles.
The native piece has no PlaceableNetPiece component; this step introduces no
construction-price override or inherited bascule pricing data.

`Lift01ApproachLayout.Apply` accepts only a generated factory-owned piece,
creates and adopts its private content-gated section, and records both native
overhead occurrence branches. It does not replace selected traffic lanes or
read the original Lift01 network Prefab. As with the ordinary support, the
runtime resource inputs resolve only built-in assets, while generated section
identity and persistence remain owned by the normal factory pipeline.

The new source/layout code compiles. Width classification, derived fixed-span
geometry and final assembly remain outstanding; these inputs are not themselves
a completed fixed-span generator. No runtime route or installed asset was
changed by this increment. No user-visible strings were introduced.

## Fixed-span authored-part transforms

`Lift01ApproachParts` now records rigid lateral groups at all three LODs and the
sixteen complete transverse web plates at each detailed level. Offline native
Geometry hash checks and whole-face ownership checks precede emission. The
lateral groups number 1,542 / 742 / 30; they include their own connector and
rivet geometry and move rigidly by the corresponding signed half-width delta.
Each complete transverse plate spans X=+/-3.62957763671875 m and stretches over
that measured span. This stage covers 70,522 / 31,282 / 780 original vertices
and 66,696 / 19,336 / 420 original triangles, with no topology changes.

`Lift01ApproachFrames` separately handles sixteen joined U frames in each of
the near/middle meshes. Their measured inner upright boundaries are either
+/-3.39190673828125 or +/-3.3919677734375 m, recorded per actual LOD/part rather
than borrowed blindly from the mechanism frame. The uprights translate; the
crossing beams stretch between those boundaries. It covers 7,992 / 5,597
original vertices, adding 540 / 602 interpolated edge vertices to subdivide
300 / 334 original faces. Original vertices, Y/Z, material slots and packed
attributes remain preserved through the shared structural-plan implementation.

The lateral/plate and joined-frame stages have disjoint original membership.
Together they cover every detailed fixed-span vertex except the joined base
and end-frame component (1,696 near / 1,520 middle vertices). The far stage
still excludes its 704-vertex merged base/frame component. Those components
need their own measured mapping; neither whole-component scaling nor an
unchanged-width fallback has been substituted for that unfinished work.

The emitters check source area for subdivisions, rigidity, complete-face
ownership, unchanged Y/Z and positive winding across four width deltas.
Release compilation and the existing localization checks pass. This remains
an implementation stage awaiting the final fixed-span assembler and full
Lift01 route; it is not in-game acceptance or a deployment.

## Fixed-span base and end-frame completion

The detailed joined base/end component is now separated offline using the actual
native base cross-section and its two authored end caps. `Lift01ApproachBaseParts.py`
records 864 base faces and 312 end-frame faces per level. Their vertex indices are
disjoint: 1,060 base vertices at each level, plus 636 / 460 end-frame vertices.
The six coincident contact positions are at X=0 or +/-3.39190673828125 m,
Y=9.61572265625 m, Z=+/-32 m. Both mappings give the same displacement there.

`Lift01ApproachBases` applies signed half-width displacement, retaining stationary
centre residues without modifying their original coordinates. The end frames join
the existing frame stage: rigid outer uprights and stretched crossing beams now
cover 8,628 / 6,057 original vertices, with 596 / 658 interpolated edge vertices
and 332 / 366 subdivided source faces. Material slots and raw attributes continue
through the existing structural plan; no replacement material or runtime part
classification is introduced.

`Lift01ApproachStructure.CreateDetailed` combines lateral parts/plates, bases and
frames. Their disjoint membership covers all 80,210 / 38,399 detailed vertices.
The 704-vertex far merged shell, final approach assembly and full Lift01 route
remain outstanding. No in-game or cold-load acceptance is claimed. This change
adds no user-visible strings; existing localization remains applicable.

## Fixed-span far shell and assembly

The far native shell contains 704 vertices and 600 triangles, accompanied by
780 lateral-detail vertices / 420 triangles already handled by the side-part
stage. Its original vertices all lie on the two side profiles. The bottom
profile's inner edges are X=+/-2.12420654296875 m at Y=5.862548828125 m.
The simplified end panels span several detailed parts; scaling the complete
shell would change upright thickness and the sloping side profile.

`Lift01ApproachFarShell` preserves those outer profiles under signed rigid
translation and stretches only the central panel span. Eight native end-panel
triangles are subdivided at the recorded inner edges, adding 24 interpolated
vertices. The complete far mesh has 1,044 triangles versus the native 1,020.
No high-detail replacement mesh or material is used. Offline checks cover the
native hash, source area, winding and rigid translation at half-width deltas
-0.5, 0, 2 and 8 m. An offline mesh render at 0 and 8 m was inspected; this
does not establish game-rendered material or LOD acceptance.

`Lift01ApproachStructure.Create` now dispatches all three levels, and
`Lift01ApproachAssembly` builds the native NetPiece render graph through the
existing factory. It preserves each level's source Surface, carries native
tiling/LOD metadata, updates piece width by twice the half-width delta, and
attaches both measured overhead-section branches. Source Prefab reconstruction
and temporary Unity meshes are disposed through their existing ownership paths.
The full Lift01 generator still needs mechanism completion and final routing;
this assembly alone does not make the style available for successful generation.

## Remaining far tower panel correspondence

`Lift01FarTowerPanels.py` now records the remaining native proxy quads with their
exact source vertices, triangles, UVs and coordinates. The positive-Z pairs
4/5, 6/7, 8/9, 14/15 and 16/17 correspond to an upper end, upper bottom,
upper top, lower outer end and lower inner end. The mirrored pairs are
392/393, 394/395, 396/397, 402/403 and 404/405. Each quad owns four separate
vertex indices; together they cover exactly the 40 vertices excluded from the
existing lateral mapping, without sharing indices with its 32 vertices.

The upper top/bottom proxies span Z=0 to approximately +/-82.9 m, whereas
the detailed roof grids (parts 29/343) occupy only the tower-end interval
abs(Z)=71.904296875..82.697265625 m. Therefore assigning the detailed roof
grid's entire width rule to a coarse panel based only on its label would be
unsupported. The original far atlas combines structural features in these
proxies. The audit retains their native UV mapping for the next feature-level
correspondence step; no uniform-scale fallback has been emitted.

Source independence also remains incomplete outside geometry reconstruction:
`MovableBridgeGeneratorBase.Apply` selects an available donor-backed variant,
`BridgeStyleCatalog` discovers variants through registered network Prefabs,
and `BridgeUnlockPolicy.Apply` captures initialized rules from `variant.Donor`.
Removing only the availability predicate would leave the other dependencies
and could bypass content eligibility. Replace these together with measured
style metadata and native unlock prerequisites, preserving real DLC resource
requirements. Temporary render reconstruction alone does not prove generation
without the original network Prefab. No UI text changes accompany this audit.

## Recorded unlock requirement snapshot entry point

`BridgeUnlockSnapshot.CaptureRequirements` accepts a measured implicit dependency
list, explicit all/any lists and the native ignore-dependencies flag, without
requiring a source network entity. Native `PrefabSystem.IsUnlockable` filters
resource dependencies; initialized requirement graphs are then captured by the
existing bounded snapshot traversal. It retains AND/OR flags and progression
leaf identities, never their current unlocked state. Missing unlockable resources
fail capture; the ignore-dependencies flag omits only implicit requirements.

The behavior follows inspected `PrefabInitializeSystem.LateInitializePrefab`,
`Unlockable.LateInitialize` and `UnlockableBase.DefaultLateInitialize`. Callers
must supply the complete measured dependency list after the native component
dependency filtering; supplying only serialized `m_RequireAll` is insufficient.
This entry point does not discover or reconstruct missing style metadata itself.
The existing donor-backed capture and migration paths remain unchanged until
complete style requirements and catalogue reconstruction can be connected.

Eight new fake-native-buffer regressions bring `UnlockSnapshotCheck` to 53 passing
checks, including native progression changes and independent rule arrays. Release
compilation passes. These are not in-game unlock or cold-load acceptance results.
No user-visible strings were added; localization behavior is unchanged.

## Measured LOD reconstruction validation fix (2026-10-20)

The far-base profile changes recorded Y/Z endpoints, but the shared structural
builder previously rejected every original-vertex Y/Z change. The profile therefore
could not pass the runtime builder despite passing offline geometry checks.
`MovableStructureModel.Plan` now provides an explicit measured-reconstruction flag,
defaulting to false. Only `Lift01FarBaseProfile` opts in. Source identity, finite
positions, topology, fragment ownership and raw-channel validation remain enabled;
ordinary widening plans still reject Y/Z changes. The profile emitter is updated
alongside its generated source so regeneration preserves this explicit choice.

Re-running the native profile emitter verifies 152 recorded profile vertices,
92 retained non-road triangles, bone ownership, closed caps, road-only open edges
and four-width winding. Release compilation, 25 routes and all twelve localization
checks pass. This removes the identified builder rejection; it is not evidence of
successful in-game generation or cold-load acceptance. No UI text changed.

## Support width domain correction (2026-10-20)

The detailed/far footings, far joined platforms and detailed tower crossbars no
longer reject width deltas solely because a transformed span would collapse.
Source topology and finite-input checks remain. Their offline emitters and runtime
outputs use the same existing measured mappings; this change adds no subdivisions,
material replacements or bone changes.

Seven deltas (-2.99, -2.75, -2, -0.5, 0, 2, 8 metres) pass source-area, winding,
rigid-side and stationary-centre checks where applicable. Lift01's selected road
has a 6 m native baseline, so positive selected widths require delta > -3 m.
These four families retain positive spans throughout that domain. Far-platform
reference arithmetic uses float64 multiplication to avoid float32 intermediate
roundoff in the strict extent comparison; emitted float weights are unchanged.

Far tower core, joined frames and ordinary pillar narrowing still require work.
These offline checks do not establish native motion, selected-sidewalk compatibility
or cold-load persistence. No user-visible text or localization keys changed.

## Remaining signed-width guards removed (2026-10-20)

Joined frames, the far tower core and ordinary pillar geometry now retain their
measured signed mappings without an output-span rejection. The paired emitters
publish centre-crossing thresholds consumed by Lift01's generation diagnostic.
Finite-input, source-layout, source-area, ownership, fixed-column and bone checks
remain. No material, topology recipe or skeleton ownership was changed.

Seven width deltas per family pass finite-coordinate and applicable rigid-side /
stationary-column checks. Positive-span cases retain the original winding checks.
Narrow signed cases explicitly report collapsed/reversed faces instead of treating
positive winding as a requirement inconsistent with the full-displacement rule.
At delta -2.99 m, joined frames report 112 such faces per detailed LOD and the far
core reports 4,796; ordinary cap LODs report 28 each. These are measured consequences
of retaining signed motion, not proof of overlap-free narrow geometry. Ordinary
pillar columns and footings remain unchanged at every tested width.

The previous seven explicit Lift01 output-width rejection sites are now removed.
Selected-sidewalk compatibility, whole-assembly native generation, resource-size
measurement and independent persistence still require verification. This increment
adds technical diagnostics only; existing user-facing localization is unchanged.
