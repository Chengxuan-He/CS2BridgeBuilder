# Lift05 network integration data

## Lift05 incremental generator route (2026-10-20)

Lift05 now connects reconstructed source data, selected-road sampling, native road
appearance, all-LOD structural/glass rendering, supports, original mechanism,
skinned paint, section conversion and unlocks through its own generator route.
The native intersection-only median uses its recorded exception; other occurrences
use the three span branches while the common converter preserves selected-road
conditions. The built-in pillar placeholder retains native reverse references.

The measured road width is 8 m: two 3 m tram strips and two 1 m shoulders. The old
30 m entry counted incompatible branches and is corrected in the shared table and
generator constant. Evidence: native section IDs 121114/121115 and the fixed
carriageway X=-4..4; zero-width side sections and intersection median add no width.

There are now six routed movable styles (Draw01, Draw02, Draw03, Lift01, Lift05,
PedestrianDraw02), but only Draw01 is user-accepted. Release compilation, 26 exact
route checks, 42 recorded-variant checks and all twelve UI languages pass. Lift05
still requires selected-sidewalk and narrow-road compatibility, full resource-size
measurement, native generation/persistence verification and later human batch
acceptance. Routing is not a claim that these remaining requirements are satisfied.
No new UI labels were needed: the registered localized Lift05 entry now reaches
its concrete generator. Earlier status entries below are historical increments.


The independently measured LiftBridge05 network contains nineteen section entries: six span occurrences with three conditional branches each and one intersection-only median. Every occurrence was checked before emission; this is not an unverified application of another bridge's mapping.

`Lift05NetworkData` requires an explicit role for every selected-road occurrence. Span roles retain ordinary, hidden moving-node and half-length connection branches. IntersectionMedian retains only `Intersection` with `FixedNodeSize` excluded. Source sections and their conditions remain independent through the shared subsection converter. Assigning selected-road roles remains generator work; runtime names, positions and widths are not used to guess them.

The native network has no additional edge/node state rules. Its Bridge segment length is 168.0 and water elevation is 10.0. All other Bridge fields are emitted from this network's audited record.

| Placement | Native object role | Position | Mode |
| --- | --- | --- | --- |
| 0 | Pillar Placeholder | {'x': 0.0, 'y': -3.0, 'z': 0.0} | 17 |
| 1 | Pillar Placeholder | {'x': 0.0, 'y': -3.0, 'z': 0.0} | 18 |
| 2 | LiftBridge05 | {'x': 0.0, 'y': 68.75, 'z': 0.0} | 16 |

The caller supplies the generated mechanism and the appropriate pillar placement object, preserving any native placeholder/replacement relationship. Where the native network has an outside-connection slot, the selected network supplies its corresponding object. No extra slot is invented for variants without one. Flags, rotations and placement modes are retained from the audit.

This class has no generator caller yet. It does not complete structure generation, moving road/marking conversion, generator routing, persistence acceptance or native traffic/shipping checks. No existing generated output is affected, so no bridge removal is needed. No visual-generation unit tests or game control are performed.

Authored on isolated branch `bridge/lift-05`. The shared converter is supplied unchanged from dev for compilation and is not included in this style commit. Pre-existing dirty edits remain in the main worktree.

## Support and object assembly increment

Status: partial implementation. This is not a completed or visually accepted generator.
Native evidence is from game 1.6.2f1, audit SHA-256
`47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070`.

## Support geometry

`Lift05Supports.py` emits immutable vertex ranges for the original column and footing
meshes. Offline position-welded analysis identifies eight separate components in each
mesh at every LOD. Each component lies entirely on one side of the centre plane.
Generation translates those components by the signed half-width delta; it does not
stretch columns, join triangles or replace lower LODs with the near mesh.

| Part | LOD0 vertices / triangles | LOD1 vertices / triangles | LOD2 vertices / triangles |
| --- | --- | --- | --- |
| Columns | 896 / 768 | 1097 / 690 | 256 / 192 |
| Footings | 1792 / 1536 | 1638 / 1382 | 448 / 384 |

The emitter checks native source hashes against the extraction audit and verifies
unchanged triangle edge vectors and Y/Z coordinates at half-width deltas -0.5, 0, 2
and 8 metres. `Lift05SupportAssembly` reads reconstructed native render inputs,
retains their original material slots and raw channels, and changes only X positions
and X bounds. Native Y/Z bounds and stack metadata remain intact. Other styles with
cross-centre supports must not reuse this classification.

## Mechanism object assembly

`Lift05ObjectAssembly` assembles the prepared main, column and footing render graphs
using `Lift05MechanismData`. It preserves the native lift offsets, timing, composition
requirements, node offset, content prerequisite and leg gap. Only the standing object's
X leg span changes with the requested width.

All twelve authored wall lights and sixteen roof vents are retained with their original
rotations, parent connections, probabilities and groups. Their nonzero X placements
follow the signed side displacement required by the attachment contract. Original
resource resolution excludes another generated bridge's assets.

Selected-road objects and markings use the shared native binding pipeline after the
final mechanism exists. The caller supplies the measured physical partition used by
the road itself. Paint therefore uses the same original road bones, single-sided
native skinned material and 0.001 m clearance; it is not a child projector. The shared
factory owns all generated nodes for preview cleanup and independent persistence.

## Remaining integration and verification

The main structure's complete all-LOD authored-part width maps, road replacement and
appearance, physical partition and concrete generator route are
still required. These support and object assembly helpers are not yet called by a
complete Lift05 generator. They do not establish motion, visual or cold-load acceptance.
Human verification remains deferred until the requested movable bridge batch is ready.

This increment introduces no display labels or localized asset names. Technical
identifiers and exception diagnostics are not UI translations; existing operation
failure handling and localization remain in use.

## Network assembly and counterweights

`Lift05NetworkAssembly` consumes explicit selected-section roles through the recorded
section converter and adopts every generated section/object-producing piece into the
factory's persistence graph. It applies the measured 168 m segment and bridge fields,
retains selected-network ground and outside-connection placements, and replaces
ordinary elevated supports with the native Lift05 placements. The built-in `Pillar
Placeholder` identity is retained: copying it would lose the game's reverse spawnable
replacement references. This helper still needs the concrete generator's call site.

`Lift05Counterweights.py` checks the native Geometry hash, decodes the original
position and index buffers and verifies their correspondence with the offline component
inventory. The visually inspected positive/negative-Z counterweights are components
0/396 in LOD0, 0/239 in LOD1 and 3/22 in LOD2. They cover 880, 833 and 32 vertices
respectively, each with its own X span of -6.0191650390625 to +6.0191650390625 m.
`Lift05CounterweightGeometry` applies that measured span to these immutable memberships.
It changes no triangle connectivity, Y/Z coordinate, skin channel or material.
Checks cover half-width deltas -0.5, 0, 2 and 8 m; complete mechanism assembly remains
unfinished. The tower and moving-span components contain welded logical parts and must
not be uniformly stretched merely because their combined bounds cross the centre.

## Detailed window mesh

`Lift05Glass.py` emits the independently ordered mesh-1 memberships for LOD0 and
LOD1. Each contains 68 disconnected four-vertex panes: 64 side panes translate
rigidly and four centre-crossing panes stretch over their own measured span
(-0.47966766357421875 to +0.47966766357421875 m). Both levels retain 272 vertices,
136 triangles and native root bone 0. The emitted `Lift05Glass` plan changes only X;
the assembly must preserve its native surface slot and all original raw channels.

Offline checks verify face ownership and unchanged Y/Z, and check rigid side-pane
edge vectors at half-width deltas -0.25, 0, 2 and 8 m. Original and widened front
views were inspected for both detailed levels. The central panes intentionally
stretch under the contract's own-span rule; their corresponding frame and tower
wall mappings must match before the complete mechanism is integrated. A narrowing
that collapses the central pane is rejected, not emitted with inverted triangles.

LOD2 has no independent glass slot and retains its native tower-atlas representation.
This helper must not introduce detailed glass into LOD2 or substitute the near tower
for it. Tower-frame mapping and full near/far appearance remain unfinished. No
user-visible text or localization keys are added by this geometry increment.

## Disconnected side-part transforms (2026-10-20)

`Lift05RigidParts.py` checks the native Geometry hashes and the exact retained OBJ
position/index inventory before emitting immutable source vertex ranges. The main
mesh has 860/535/22 independent side components in near/middle/far LODs, covering
21,780/14,329/1,136 vertices and 16,832/7,038/836 triangles. These include lateral
mechanism hardware, longitudinal truss members and side details. Each selected
component lies wholly on its original side; no selected face shares a vertex with
an excluded component. Joined towers, moving base, counterweights and every
centre-crossing component remain outside this map and require their own rules.

`Lift05RigidParts.Apply` changes only X by the signed half-width delta. Original
indices, Y/Z and non-position attributes are untouched. This map specifies the
width transform, not final inclusion: sidewalk-dependent components still need
the selected-road presence/layout handling before full assembly. It must not be
used as evidence that every structural part has been implemented.

Four offline half-width deltas (-0.25, 0, 2 and 8 m) preserve all selected triangle
edge vectors and unselected vertices. Complete native-source views and separate
native/+16 m side-part views were inspected at all three LODs. Research artifacts:
`lift05-side-components-review.png`, `lift05-side-width-review.png`,
`lift05-rigid-parts.json` and `lift05-rigid-<lod>-<delta>.npz`.
Release, route and 12-language checks pass. No display strings were added.
This partial helper is not yet called by a complete Lift05 generator; joined
structural parts, road appearance, far fixed ends and overall integration remain.

## Independent transverse members (2026-10-20)

`Lift05TransverseParts.py` decodes the native binary32 positions and exact indices,
checks the source hashes and OBJ bone/index correspondence, then emits reviewed
member memberships and their individual X spans. The generated helper handles
32/20/5 members and 1,848/1,530/160 vertices in near/middle/far respectively.
Detailed levels include seventeen under-deck transverse girders and three
independent upper transverse beams. Near also includes twelve centre window bars;
the far source supplies five separate upper beams. The other two detailed upper
beams are joined into the moving shell and still need their own membership there.
These differences do not authorize dropping source LOD parts or treating merged
parts as uniformly stretchable.

Each member stretches over its own measured span, retaining original Y/Z, indices,
bone channels and material ownership. Four half-width deltas (-0.25, 0, 2, 8 m)
pass endpoint and winding checks. Native component close-ups and original/+16 m
views were inspected offline (`lift05-crossing-members-review.png` and
`lift05-transverse-width-review.png`). The smallest centre window bars still
limit narrowing; a collapsed member raises an explicit failure rather than
publishing inverted faces. Unrestricted narrow-road compatibility remains open.

The eight detailed V-brace assemblies are explicitly excluded. Index-component
inspection of native near component 893 shows separate negative-side diagonal,
positive-side diagonal and centre horizontal segments (six attribute-side index
groups). Middle component 556 has thirteen simplified index groups with the same
logical division. The diagonals do not reach the centre and require rigid lateral
translation; the centre bar requires its own span. Scaling the entire position-
welded V assembly would distort those side diagonals. Their detailed per-member
maps, joined towers/base and complete all-LOD assembler are still pending.

Release compilation, 25 exact routes and all twelve language checks pass. This
helper adds no UI strings and is not a complete or accepted Lift05 generator.

## V-brace member maps and remaining source coverage (2026-10-20)

`Lift05VBraces.py` now identifies each detailed V assembly's independent index
islands offline, groups those islands into the measured left diagonal, centre
bar and right diagonal, and emits exact membership. Near/middle each contain
24 members (eight assemblies), with 288/282 source vertices. Diagonals translate
rigidly; each centre bar stretches over its own binary32 X span. No triangle is
shared between transform modes, subdivided or removed. Source hashes, original
indices and bone-group ordering are checked before emission. The native far
source does not retain these small V braces; this stage adds no far substitute.

Four-width checks preserve side edge vectors, Y/Z and all unselected vertices,
with positive winding for every nondegenerate original triangle. Native and
+16 m views of the end assemblies were inspected in
`lift05-vbrace-width-review.png`. Release, 25 route and twelve-language checks
pass; there is no new user-visible text. Full Lift05 generation remains unrouted.

The source coverage audit confirms no overlap among rigid-side, transverse,
V-brace and counterweight maps. Their union leaves exactly five complete native
position-welded components at every LOD:

| LOD | Source vertices | Mapped vertices | Remaining vertices | Remaining component IDs |
| --- | ---: | ---: | ---: | --- |
| Near | 47,062 | 24,796 | 22,266 | 1, 2, 70, 398, 464 |
| Middle | 36,479 | 16,974 | 19,505 | 1, 2, 70, 241, 307 |
| Far | 2,500 | 1,328 | 1,172 | 0, 2, 6, 20, 25 |

These are the moving road/structure shell, two tower shells and two tower-top
ring assemblies. Their own part maps remain to be implemented; treating a
compound component as one crossing member is prohibited. Coverage is a count
of source vertices with width rules, not a completion percentage or proof of
road appearance, sidewalk adaptation, native motion or persistence acceptance.
Research reproduction: `lift05_map_coverage.py` and
`lift05-main-map-coverage.json`.

## Tower-top ring plans (2026-10-20)

`Lift05RoofRings.py` records the two native ring components independently at every
LOD. The measured inner corner interfaces are X=-11.25/+11.25 m. Side bands and
bevelled corners translate rigidly; the straight transverse spans between those
interfaces stretch. Native bone 0, Y/Z, original vertices and source material
ownership remain. Runtime uses immutable vertex weights, edge interpolation
records and triangle fragments; it performs no source-geometry classification
or intersection. The shared structural model interpolates original raw channels
for only the required boundary vertices.

The boundary partitions preserve source area and winding and prevent corner
triangles from mixing a stretched interior with a rigid side. Ring-only counts:

| LOD | Original vertices | Added edge vertices | Original triangles | Result triangles |
| --- | ---: | ---: | ---: | ---: |
| Near | 1,760 | 304 | 880 | 1,184 |
| Middle | 1,309 | 228 | 590 | 818 |
| Far | 160 | 16 | 160 | 176 |

These are local structural-boundary subdivisions, not road/paint/UV cuts.
Four half-width deltas (-0.25, 0, 2, 8 m) pass source-area, winding, Y/Z and
side-fragment rigidity checks. The native and widened ring views at all three
LODs were inspected (`lift05-roof-ring-review.png` and
`lift05-roof-ring-width-review.png`). Original ring face ownership is exclusive
of the earlier side/counterweight/transverse/V-brace maps.

The new Plan retains unrelated source positions and faces for later composition;
it is not a completed main-mechanism assembler. The moving road/structure shell
and both tower shells remain unimplemented compound groups, along with road
appearance, sidewalk adaptation and full generator routing. Release compilation,
25 route checks and twelve-language checks pass; no UI strings changed.
Native motion/material/persistence acceptance remains pending for the full batch.

## Moving shell width plan (2026-10-20)

`Lift05MovingShell.py` now emits immutable width memberships for the native moving
shell at all three LODs (components 1/1/2; 8,734/7,147/500 source vertices).
The source Geometry hashes, binary32 positions and original index order are checked.
The original road skeleton, Y/Z coordinates and source attributes remain unchanged.
The road-adjacent base uses sign-X displacement; measured centre residues below
0.00002 m remain stationary. Longitudinal side structures and transverse end
connectors translate rigidly. The two joined detailed upper crossmembers stretch
between their measured X interfaces at -2.70941162109375/+2.70941162109375 m.
Far upper crossmembers are separate components already covered by the transverse
member table, so this shell does not duplicate them.

Index islands alone are insufficient to identify whole authored members: the
middle-detail simplification joins two transverse faces across the connector
interface. Offline subdivision adds two interpolated edge vertices and increases
shell triangles from 3,594 to 3,596. Near remains 4,714 triangles and far remains
380; no near/far shell subdivisions are needed. These are structural interface
subdivisions, not marking or UV cuts. The plan retains road-top faces for the
existing exact road mask to remove during final mechanism composition.

Four half-width deltas (-0.25, 0, 2, 8 m) pass winding, Y/Z, source-area and
rigid-side checks. The native/+16 m elevations were inspected in
`lift05-moving-shell-width-review.png`. This verifies the offline structural
mapping, not selected-road sidewalk adaptation or in-game motion. The complete
assembler still needs both fixed tower shells, appearance integration and route
registration. There are no new user-facing strings or changes to localization.

## Fixed tower shell width plans (2026-10-20)

`Lift05TowerShells.py` emits the two fixed tower shell maps at all three LODs,
covering 11,772/11,049/512 original vertices. Detailed shells preserve side-window
openings and legs with rigid side displacement and stationary native centre-line
residues. Both levels' 272 independent glass vertices were checked against these
displacements; the centre-pane boundaries agree with the existing glass mapping.
Near/middle keep their original 6,616/5,842 tower triangles.

The far room merges the side panels into full-width faces. Its measured detailed
interfaces at X=-5.75/+5.75 separate rigid outer panels from the central span;
offline subdivision adds 112 edge vertices, changing 312 tower triangles to 424.
No runtime source classification is used. Source hashes, indices, four-width
winding, original area, Y/Z preservation and rigid-side translations pass.
Native/+16 m room elevations were inspected in
`lift05-tower-shell-candidate-review.png`.

This is a structural width plan. Far fixed roadway is still the original merged
sheet and must be reconstructed before the full mechanism is publishable; its
height and lateral domains cannot be solved by this width map. Exact road masks,
selected sidewalks, source materials and all partial Plans also need final
composition. No user-visible text is introduced. Native motion, appearance and
cold-load acceptance remain pending for the complete generation batch.

## Complete main-mesh width composition (2026-10-20)

`Lift05StructureWidthPlan.Create` combines the ring, moving-shell and fixed-tower
Plans, rebases their constructed vertex references and preserves each stage's
fragment replacement mask and measured constructed X coordinates. It then applies
the independent rigid-side, counterweight, transverse-member and V-brace tables.
The original source positions and indices remain the identity reference; runtime
composition does not classify any source geometry.

`lift05_complete_width_coverage.py` verifies that all 47,062/36,479/2,500 original
vertices belong to exactly one of these seven maps. It also checks unique replaced
source faces, every fragment's original triangle ownership, edge interpolation
ranges and rebased indices. Combined edge counts are 304/230/128; combined main
triangle counts before road removal are 31,302/18,722/1,952. Zero-displacement
centre vertices remain part of their explicit maps even when no position write
is necessary. This replaces the earlier incomplete five-component coverage audit.

Width coverage is now complete for mesh 0, with mesh 1 handled separately by the
existing glass plan. It does not complete Lift05 generation: far fixed road domain
reconstruction, exact road removal, selected-road replacement/sidewalk adaptation,
material integration and complete generator routing remain outstanding. No UI text
changed. Release, route and localization checks remain required for integration;
native motion and cold-load validation remain part of the later full batch.

## Far fixed-road boundary and structural entry point (2026-10-20)

`Lift05FarRoadBoundary.py` decodes and checks the exact far Geometry, then emits
replacement fragments for the sixty fixed-base faces. The candidate produces
148 base faces and 176 interpolated edge instances. The road opening matches the
detailed receiver: X +/-4 m, Y 9.800048828125 m, absolute Z 62.580078125–84 m.
Only added boundary vertices receive measured Y/Z reconstruction; original
vertices, outer corners and the simplified underside remain. Raw attributes
continue to interpolate on the recorded original edges.

The offline candidate has twelve road-opening segments and twelve other boundary
segments. All twelve other segments match the original base subset exactly, with
no nonmanifold edges. Four widths pass source-relative winding checks. The sixty
replacement faces are disjoint from the existing width fragments and moving-road
removal. Runtime checks original face identities and rebases the new edge indices
after the combined width plan's existing constructed vertices.

`Lift05StructurePlan.Create` now composes width changes, far fixed-road boundary
reconstruction (or detailed fixed-road removal), and moving-road removal. It is
the main-mesh entry point for subsequent render assembly. Selected road geometry,
material preparation, sidewalk compatibility and complete generator routing are
still pending; compilation does not establish game acceptance. Release, 25 route
checks and twelve-language checks pass. No user-visible strings were added.
