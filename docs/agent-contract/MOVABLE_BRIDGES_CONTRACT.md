# Movable bridge generation contract

## No special road-width adaptation

Movable bridges must not perform any special adaptation based on road width.
Apply the ordinary measured width-difference rules without narrow-road-specific
compensation, limits, geometry substitution or bone reassignment. Remove dedicated
narrow-road diagnostic branches. Narrow-road adaptation and dedicated compatibility
tests are outside the batch scope and are not completion gates.
Preserve finite-input and native geometry validity checks. This does not assert
support for every narrow input. Non-movable bridge behavior is outside this change.

## Contents

- [No special road-width adaptation](#no-special-road-width-adaptation)
- [1. Authority, scope and implementation status](#1-authority-scope-and-implementation-status)
- [2. Required architecture](#2-required-architecture)
- [3. Archetype evidence and immutable mechanism data](#3-archetype-evidence-and-immutable-mechanism-data)
- [4. Resolve the selected road before mapping](#4-resolve-the-selected-road-before-mapping)
- [5. Per-archetype mapping and the six-lane example](#5-per-archetype-mapping-and-the-six-lane-example)
- [6. Network conversion and placement](#6-network-conversion-and-placement)
- [7. Convert the closed-pose road into persistent object geometry](#7-convert-the-closed-pose-road-into-persistent-object-geometry)
- [8. Skeletons, animation and attachments](#8-skeletons-animation-and-attachments)
- [9. Waterway and traffic compatibility](#9-waterway-and-traffic-compatibility)
- [10. Lift, rail, pedestrian and double-deck variants](#10-lift-rail-pedestrian-and-double-deck-variants)
- [11. Persistence, failures and compatibility](#11-persistence-failures-and-compatibility)
- [12. Integration and update scope](#12-integration-and-update-scope)
- [13. Acceptance and completion criteria](#13-acceptance-and-completion-criteria)
- [14. Evidence references and maintenance](#14-evidence-references-and-maintenance)
- [15. Observed failures and mandatory prevention](#15-observed-failures-and-mandatory-prevention)
- [16. Generation pipeline and source map](#16-generation-pipeline-and-source-map)
- [17. Material families, UV density and texture ownership](#17-material-families-uv-density-and-texture-ownership)
- [Offline repeat materials instead of period seams](#offline-repeat-materials-instead-of-period-seams)
- [18. Marking layout, mesh and native material](#18-marking-layout-mesh-and-native-material)
- [19. Common motion, stacking, visibility and underside](#19-common-motion-stacking-visibility-and-underside)
- [20. LOD, topology and prototype-specific extension](#20-lod-topology-and-prototype-specific-extension)
- [21. Preview, native persistence and failures](#21-preview-native-persistence-and-failures)
- [22. Performance and diagnostic requirements](#22-performance-and-diagnostic-requirements)
- [23. Acceptance and contributor checklist](#23-acceptance-and-contributor-checklist)
- [24. Track geometry, overhead supply and material selection](#24-track-geometry-overhead-supply-and-material-selection)

## 1. Authority, scope and implementation status

The [implementation policy](BRIDGE_GENERATION_CONTRACT.md#current-implementation-policy)
removes process gates on completing the remaining generators. Hardcoding is explicitly retained:
extract from real prototypes during development, then use immutable construction data without
requiring the player's identical original prototype files. Compact profiles/recipes are permitted;
runtime rediscovery of prototype parts is not. Supply required resource inputs through the permitted
packaged/persisted resource graph. A local developer file path is never a deployment dependency.

Per-style branches and exhaustive audit-before-implementation are not admission gates. Route actual
working implementations incrementally and record remaining acceptance work honestly. Existing
appearance, skeleton, persistence and localization outcomes remain mandatory.

Sections 16–24 define the generation, material, marking and track requirements.
The material generation contract governs original reuse, density, verified source finite/stretch
stretching, tiling, offline preparation,
bounded runtime conversion and the separate generated-owner material and total-package limits.
Implementation evidence and human acceptance remain specific to the tested style and input.

This is the normative supplement to [BRIDGE_GENERATION_CONTRACT.md](BRIDGE_GENERATION_CONTRACT.md) for bascule/draw
bridges, vertical lift bridges and any subsequently admitted movable design. Read it together with
the root [AGENTS.md](../../AGENTS.md), the complete project and bridge generation contracts and applicable directory rules.
The bridge generation contract remains authoritative for geometry, source selection, branches, persistence,
pricing, cleanup and acceptance. This supplement does not enable an unsupported generator.

The moving road surface MUST be generated from the selected
road's closed-pose composition, converted into persistent object geometry, partitioned and bound to
the measured mechanism. Reusing the prototype's road surface as the generated road is not an
alternative implementation of this requirement.

### Implementation status and evidence

The selected-road conversion pipeline and its observed failures define these requirements.
Draw01's recorded working path and user-confirmed normal appearance are evidence for the tested
build and input, not acceptance of subsequent material or generation changes. That evidence does
not establish full traffic, shipping, every road/LOD combination or mod-independent cold-load
acceptance. Other mechanisms require their own measured data and implementation evidence;
registered catalogue entries alone do not establish working generators. Completion status must
never disable a registered style or add unfinished-style notices (bridge generation contract section 22).

Track every mechanism by its stable style ID in the [implementation record](../Movable-Bridges-Implementation.md).
Distinguish measured evidence, code, deployment and human acceptance. Completed audits and
backups do not establish completed generation support.

## 2. Required architecture

Keep three responsibilities distinct while deriving them from one consistent road selection:

| Responsibility | Required result |
| --- | --- |
| Traffic network | Native network sections, lanes, connections and crossing signals remain usable in the closed pose |
| Visible moving surface | Selected-road geometry becomes persistent, bone-bound object geometry |
| Bridge mechanism | Measured archetype objects, bones, placement and native animation/signal behavior control movement |

The animated object supplies the visible surface in the moving span. The network still supplies
traffic connectivity. Do not rotate or raise the traffic curves with the visible leaves, delete and
rebuild lanes every cycle, or treat mesh visibility as traffic permission. Native crossing control
must stop entry, clear traffic and coordinate the movement with shipping.

Persisted operation must not require a Bridge Builder update loop, Harmony patch, custom animation
controller, signal timer, runtime component or regeneration callback. Required game/DLC content
remains a native prerequisite. Manager independence does not imply DLC independence.

### General generation flow

| Stage | Inputs and output | Required invariant |
| --- | --- | --- |
| Resolve | Selected road and supported native state to sections, pieces, lanes and evaluated closed geometry | Preserve conditional alternatives and selected layout; no prototype-section whitelist. |
| Prepare mechanism | Immutable prototype parts, motion domains, skeleton, materials and LOD data to width-adjusted structure | Preserve authored transforms, fixed platforms and native behavior. |
| Convert surfaces | Evaluated road to object-local triangles clipped by motion and appearance domains | One native bone per triangle; fixed and moving regions remain distinct. |
| Assign appearance | Native structural references, measured repeating road units, bounded local composites and separate skinned paint | Follow sections 17–19; no paint-driven road cuts or custom shaders; cuts solely for texture-period wraps are prohibited; use section 17's offline repeat materials. |
| Assemble | Structure, converted road, native attachments and per-material models to the prototype's mechanism hierarchy | Correct material batch counts, bones, bounds and all LODs. |
| Build network graph | Native crossing conditions plus selected-road sections to minimal changed Section/Piece nodes | Preserve lanes, signals, fees, upgrade logic and unlock AND/OR groups. |
| Preview or save | Same generated graph to a temporary open-pose assembly or registered native assets | Preview writes no formal assets; saving preserves owned identity and original dependency references. |
| Verify | Actual geometry, logs and saved graph to evidence for the claimed input/style | File checks and timings remain separate from human visual, traffic and cold-load acceptance. |

Stages describe dependencies rather than requiring a serial implementation. Independent work may
run concurrently subject to section 12 and native API thread affinity. Prototype-specific motion
and appearance data are parameters to this method, never dimensions copied from the first bridge.

### Non-moving parts follow the general bridge generation contract

Every non-moving part of a movable bridge must follow the
[general bridge generation contract](BRIDGE_GENERATION_CONTRACT.md). This includes ordinary
approach spans, connecting edges, fixed nodes, deck-adjacent bases, side structures, pillars and
their LODs. Movable conversion is not an exemption from ordinary bridge structure generation.

Generate the prototype's complete fixed structural representation around the selected road. Do
not retain the selected road's generic bottom in place of a required prototype base while installing
prototype pillars at a different vertical connection level. Preserve the measured base-to-pillar
interface and original Y/Z coordinates; raising pillars or changing depth to hide a missing base
is prohibited. Apply selected-road width and sidewalk adaptation under the general contract while
preserving source lane semantics. Keep fixed-span structural conditions separate from moving-node
hiding so stationary geometry cannot intrude into the opening. Preview and persisted generation
must include the same fixed structures at every LOD.

Implementation and pending
visual acceptance are recorded in [fixed approach bases](../Draw01-Fixed-Approach-Base.md).

## 3. Archetype evidence and immutable mechanism data

Before admitting a variant, record its exact network, static-object and render-prefab identities,
source content, game version, extraction date and reproducible extraction method. Retain the source
hashes or equivalent version evidence in the implementation's measurement record. Resource-file
path IDs are version-local evidence, never persistent identifiers or runtime constants.

The record must cover:

- `Bridge`, all section requirements, `NetSubObjects` placement and any `AuxiliaryNets`.
- `NetObject` composition states and node offsets, and `MoveableBridge` fields.
- Every relevant mesh/LOD, material, authored fixed/moving part and connection ID.
- Bone hierarchy, rest transforms, inverse bind matrices, motion type, speed, acceleration,
  source IDs and connection IDs. A depth-truncated dump is insufficient.
- Hinges, moving-span endpoints, fixed transition areas, opening envelope and closed deck datum.
- Mounted lights, barriers, effects, parent relationships and any counterweight/pulley coupling.

Identify authored mechanism parts offline. Compact measured profiles and recipes are permitted;
per-vertex enumeration is required only when needed to represent the part. Commit immutable data using the existing
metaprogramming workflow; do not rediscover parts at generation time from bounds, nearest vertices,
names, connected components or non-zero coordinate thresholds. Preserve every variant's own data.
Do not apply DrawBridge03's hinge positions or LiftBridge01's travel to another variant.

The existing `x = 0` stretch/translate rule, tower-height/depth constraints and binary32 width audit
remain in force, with the user-approved central railing post exception in bridge generation contract section 8.
The Draw03 complete moving/fixed truss and deck-adjacent base rules in that section also apply.
An independent vertical railing post centred on the bridge centreline retains its original width
and centred position in the highest-detail mesh and every LOD. Identify exact membership offline;
do not extend that exception to transverse rails, walls or other crossing parts.
Motion partitioning is separate from structural widening and cannot reclassify authored parts.
Missing evidence rejects the affected operation; it never authorizes guessed geometry or fallback.

## 4. Resolve the selected road before mapping

Resolve the road's native section and subsection requirements for the intended elevated composition,
including its own edge states and supported upgrades. Do not sum all alternatives, choose the widest
alternative or infer layout from the displayed lane count. Obtain pieces, lateral offsets, lane
definitions, direction/group flags, pedestrian paths, markings and relevant attachments from that
same resolved state.

Keep surface width and outward side extensions separate in generator inputs. Report which boundary
each width describes. A full composition width is not automatically the project's road-surface
width or the measured outer width of the bridge mechanism. Follow
[road width calculation](../road-width-calculation.md) and bridge generation contract section 14.

Preserve supported one-way/asymmetric arrangements and source lane restrictions. Do not mirror a
road merely because the mechanism is symmetric. Preserve tram/rail/bicycle/transport-lane behavior
only where the complete geometry, signals and persistence path supports it. Unsupported combinations
must report a specific reason before publication; silently removing an upgrade is forbidden.

Parking, stops, overhead wires and other special features require explicit handling. Use native
elevated selection where it removes a ground-only feature. If an active feature cannot operate on
the moving span, reject that combination rather than leaving parked vehicles, floating wires or
invisible traffic functions behind.

## 5. Per-archetype mapping and the six-lane example

Mapping is defined by the specific movable-bridge archetype together with the selected road and
its resolved state. Different drawbridge archetypes may require different mappings even when they
carry the same lane count. A shared mechanism category is not evidence of an identical network
layout. The mandatory technical route is selected-road surface generation; the six-lane mapping
below is an illustrative instance of that route, not its universal conversion algorithm.

Record each archetype's native lane roles, node/edge conditions, visible/hidden layers,
connection offsets and fixed/moving boundaries. Section correspondence tables are audit evidence,
not a runtime prerequisite or whitelist. Resolve arbitrary selected-road sections through native
composition, then assign their evaluated geometry to measured physical motion domains. A median,
sidewalk or third-party section absent from the prototype must use this same path. Absence from a
mapping table is not a failure; malformed external data or an actually unrepresentable feature
must identify its real failure. Do not infer bone ownership from a section name, centroid or lane
count. Do not assume one-to-one correspondence, three branches per source section or shared
placement dimensions across archetypes.

### Six-lane reference case

The following reference was extracted read-only from the local game's
`Cities2_Data/resources.assets`, with installed `Game.dll` behavior inspected separately. The
installed game reported `1.6.2f1`. These observations are a reference case, not universal dimensions
and not proof that Bridge Builder already implements the conversion.

Compare `Large Road` (ordinary undivided six-lane road) with `DrawBridge03` (six-lane drawbridge).
`Large Road Divided` is a separate input and must not be substituted for this case.

| Source section index | Large Road | DrawBridge03 counterpart | Target entry indices |
| --- | --- | --- | --- |
| 0 | Road Side 0 | DrawBridge03 Side 0.5 | 0, 1, 2 |
| 1 | Sidewalk With Parking 7 | DrawBridge03 Sidewalk 3.5 | 3, 4, 5 |
| 2 | Car Drive Section 3 - Transport Tram Option | DrawBridge03 Car Drive Last Section 3 | 6, 7, 8 |
| 3 | Car Drive Section 3 | DrawBridge03 Car Drive Second Section 3 | 9, 10, 11 |
| 4 | Car Drive Section 3 | DrawBridge03 Car Drive First Section 3 | 12, 13, 14 |
| 5 | Road Median 0 | DrawBridge03 Median 0 | 15, 16, 17 |
| 6 | Car Drive Section 3 | DrawBridge03 Car Drive First Section 3 | 18, 19, 20 |
| 7 | Car Drive Section 3 | DrawBridge03 Car Drive Second Section 3 | 21, 22, 23 |
| 8 | Car Drive Section 3 - Transport Tram Option | DrawBridge03 Car Drive Last Section 3 | 24, 25, 26 |
| 9 | Sidewalk With Parking 7 | DrawBridge03 Sidewalk 3.5 | 27, 28, 29 |
| 10 | Road Side 0 | DrawBridge03 Side 0.5 | 30, 31, 32 |

In this reference, the left five sections are inverted, the central section is the median, and the
right five are not inverted. Preserve the actual source direction flags. The 11-to-33 mapping is
three conditional entries per lateral section, not an increase in lane count. General conversion
must map actual section roles and resolved lane positions, not require exactly eleven source entries.

Without transport, bicycle or stop upgrades, both networks use `Car Drive Piece 3` and
`Car Drive Lane 3` for the six vehicle lanes. The lane position within that piece is `(0, -0.2, 0)`.
For the centered straight reference, lateral lane centers are
`-7.5, -4.5, -1.5, +1.5, +4.5, +7.5` metres. No transverse scaling of those lanes is needed.

The ground reference composition is 32 m: two 7 m sidewalk/parking sections plus six 3 m lanes.
Elevated selection replaces each sidewalk/parking section with a 3.5 m sidewalk and selects a
0.5 m side extension on each side. Its surface allocation is 25 m and its full lateral composition
is 26 m. DrawBridge03's elevated reference has the same allocation, with bridge-specific side and
bottom pieces. Do not compress a 32 m ground road into a 26 m bridge or hardcode a 3.5 m sidewalk
for other roads. Ordinary and NoTram sidewalk pieces share pedestrian lane definitions in this
case; that does not establish tram compatibility for the drawbridge.

The table explains native correspondence. The generated bridge must still derive its visible road
surface from the selected road, not replace it wholesale with DrawBridge03's road textures or mesh.

## 6. Network conversion and placement

The following branch table describes only the native fixed-node pattern observed in DrawBridge03.
It is not a mandatory three-branch template for all drawbridges, lift bridges or other mechanisms.
Each variant must use the conditions established by its own archetype evidence:

| Branch | Required conditions | Excluded conditions | Hidden layers | Half length |
| --- | --- | --- | --- | --- |
| Ordinary segment | Original applicable conditions | FixedNodeSize | Original visible behavior | false in the reference |
| Moving-span node | Node, FixedNodeSize | Original applicable exclusions | -1 | false |
| Connecting edge | FixedNodeSize | Node | Original visible behavior | true |

Merge the chosen archetype's measured conditions with the selected road's requirements rather than
overwriting them. Apply the table above only where that archetype's evidence establishes this pattern.
Preserve inversion, flip, median, offsets and relevant source semantics. Detect contradictory
conditions explicitly. Other archetypes require their own measured section scheme.

`NetCompositionHelpers` selects composition pieces; the mesh-reference path excludes hidden pieces.
Native lanes remain a separate output. Do not remove `NetPieceLanes` to hide road geometry or assume
that `m_HalfLength` literally identifies one animated leaf. Read the native mesh-generation behavior
when implementing the connection span.

### Mechanism placement and ordinary supports

Use the measured waterway-crossing object placement. The object's `NetObject` establishes the
fixed-node state and endpoint offsets; ordinary supports retain their separate non-crossing rules.
Do not place an animated mechanism at every road node or leave ordinary pillars in its shipping
opening. Preserve native connection geometry, editing and demolition behavior.

For DrawBridge03 specifically, `m_NodeOffset = 96 m` and the main object's recorded mesh bounds
extend from `z = -96 m` to `+96 m`. Hinge positions are approximately
`(0, 4.045972, +75.383911)` and `(0, 4.045972, -75.383904)`. These are different measurements:
the fixed-node extent, model bounds and moving-leaf extent must not be equated. Rounded values in
this prose must not become implementation constants; use the full measured transforms and bind data.

## 7. Convert the closed-pose road into persistent object geometry

The required generation sequence is:

1. Resolve the source road and supported states, and determine the measured mechanism's closed-pose
   span, fixed transitions and coordinate frame.
2. Compose visible road geometry for that span before applying the moving-node hiding branch.
   Retain its correspondence with traffic lanes, markings and attachments.
3. Evaluate native network deformation into explicit object-local geometry. Network-packed positions,
   curve/composition matrices and preview-only material properties are not a saved object mesh.
4. Partition at the measured fixed/moving interfaces and leaf seams. Split crossing triangles where
   necessary, preserving interpolated attributes and material/submesh ownership. Do not weld a
   moving leaf to the fixed approach or allow one triangle to span two independently moving leaves.
5. Bind each moving region to the correct native bone and fixed regions to their measured fixed
   frame. Derive all required LODs and mounted-object relationships consistently.
6. Save generated native nodes and geometry, retain unchanged resource references, and snapshot
   required external dependencies under section 11. Use existing per-bridge identity and layout
   helpers. Publish success only after the required persistence work has completed.

The user's selected-road geometry conversion requirement entails constructing new road-surface
topology, skinning channels and any necessary network-to-object attribute representation. This
operation is distinct from widening an existing archetype structure: bridge generation contract section 4's
raw-channel/index preservation continues to apply to carried structure meshes. Do not use road
conversion as permission to re-encode those meshes, replace their materials or guess authored parts.
Record the source and destination layouts and the reason for each road-data conversion.

Apply measured partition transforms as construction data, not runtime part-detection heuristics.
Authored mechanism membership must already be known offline. Generated road clipping may be
performed in the recorded seam's local frame against its zero plane; it must not infer a hinge or
part boundary from road bounds or introduce a new transverse widening criterion.

Prototype-face intersection remains prohibited by general bridge contract section 2. Use the
hardcoded motion/appearance frames directly on selected-road geometry; never intersect it
with a prototype mesh or a stored copy of its triangles to rebuild a road LOD. Preserve
selected-road heights, curbs, asymmetric sidewalks and bone ownership at every level.

### Integrated paint and receiver geometry

Markings remain part of the native mechanism render graph but use a separate single-sided
skinned mesh. Evaluate native selected-road marking footprints, partition only those polygons at
physical motion seams, match the evaluated upper road height and bind them to the original road
bones. The authorized clearance is 0.001 m along the local upper normal. Do not cut road receiver
triangles at marking edges, render projection boxes, create one child object per dash, or move
the road onto marking bones. Sections 18–19 define the complete current implementation.

### Selected-road sidewalk composition

The general sidewalk rule belongs to [bridge generation contract section 25](BRIDGE_GENERATION_CONTRACT.md#25-adapt-sidewalk-dependent-components-to-the-actual-elevated-road)
and the root AGENTS.md; it applies to all bridge types, not only movable bridges.
Apply measured longitudinal motion seams to those actual surface roles. Keep lane-visual and road
occurrence indices separate; paint follows its receiver. A mixed-use Piece requires its actual
subsurface semantics, not a blanket assumption that every face has the same role.

### Material selection, projection and render batches

Select the required appearance by surface type under section 24. Reuse the corresponding
original material directly when its vertex layout and native animated-object path are compatible.
For a prototype-supported road type, preserve the required composite through section 17's
native path. Other road types retain original materials; an incompatible path is unresolved,
not permission for composition or a visually similar substitute. Existing explicit track
descriptor conversions remain separate native representation rules. Carried non-road materials remain original by default; only section 17's narrow offline
repeat derivation may replace an evidenced repeating domain to avoid new period-wrap seams. Only prototype-supported road types enter section 17's composition/precomposition plan;
other road types retain original materials. Prepare known, preparable constant derived
resources offline. Genuinely variable composition and necessary conversion of selected third-party
road/track inputs unavailable before runtime follow the bounded exceptions in material contract
section 4, with direct reuse and descriptor-only conversion considered first. Custom shaders and structural receiver-only descriptor rewrites
are prohibited. Native marking layout is evaluated before producing the skinned paint described
in section 18; this is not the native projector shader or a full reproduction of decal fading.

For converted surfaces, derive UV axes, origin and physical scale from the applicable source
selected under section 24: the prototype for supported surface types, or the selected road/track
for types requiring original appearance. Separate measured material, side and leaf-orientation domains; one global projection
or copied network-packed UVs is insufficient. Keep compatible object materials' own UVs. Exclude
embedded prototype paint and pale gutters from sampled asphalt/concrete regions to avoid repeated
false lines. Inspect the existing atlas interior and filtering margin, retaining measured physical
texture density when a genuine repeat unit requires extraction. Establish its source pattern and
tileable boundaries independently of the receiver; a convenient rectangle is not sufficient.
Prepare each known, preparable unit only once offline and share it across all roads, triangles,
LODs and sampling variants using that unit. Unknown selected-input units remain subject to material
contract section 4 and share one conversion within the current owner. Do not enlarge a unit to
cover the complete bridge; preserve native channels,
the complete mip chain, filtering and physical UV gradients (section 17).
Build tangent frames from triangle position/UV derivatives with consistent handedness; when the
projection has zero UV area, retain a valid source frame rather than divide by a singular mapping.
Do not claim exact authored UV-island or LOD-atlas reproduction without verifying it.

Preserve fixed platform/curb structural faces and original material appearance. Retain original
UVs for rigid transforms only where the actual native mapping preserves both UV gradients,
chart coverage, phase and normal semantics; rigid position displacement alone is not proof.
Use measured density-preserving remapping when tiling geometry stretches. Verified source
finite/stretch mappings instead retain authored UVs under material contract section 2. Exclude their
occupied domain from replacement road geometry. A fixed carriageway cap is a separate appearance
domain: retain selected-road layout and use the measured native cap material/projection there.
Do not mistake a missing rendered face for missing vertices or add an overlapping white patch.

Emit material batches in the native importer layout: one generated model per used material slot,
local material index zero and its matching surface reference. Registered mesh counts must cover
all emitted batches at every LOD. Additional road/paint batches belong to existing mechanism render
nodes, not additional per-marking Prefabs.

Never assume changing a shader keyword makes a network material a valid animated object material.
Verify the native object vertex layout, skinning path and surface dependencies. If a source surface
cannot be represented persistently, return an explicit unsupported result; a preview-only workaround
does not satisfy generation. Keep temporary ECS entities, meshes and texture leases disposable.

## 8. Skeletons, animation and attachments

Preserve the measured bone hierarchy, rest transforms, inverse bind matrices, procedural animation
types, connection/source IDs, speeds and accelerations. The closed-pose skinning transform must
reproduce the converted road geometry in the mechanism's object space. Bone array indices, connection
IDs and mounted-object parent IDs are distinct identifiers; do not substitute one for another.

| Mechanism | Required motion behavior |
| --- | --- |
| Bascule/draw | Each longitudinal leaf rotates about its own authored hinge; each carries the full applicable transverse road layout |
| Vertical lift | The moving span translates with its deck bone; counterweights and pulleys preserve their recorded source-bone relationships |
| Other | Admit only with a measured real archetype and an independently verified native motion/control path |

DrawBridge03's positive-z leaf uses connection ID 101; its negative-z leaf uses 100. Their rest
rotations/bind matrices differ. Names such as LeftSide/RightSide do not mean the two traffic
carriageways. Do not split three lanes onto each bone or manufacture opposite angle signs from names.

Native draw variants use aim-driven procedural bones; lift variants can use movement-driven bones,
`ScaledMovement` counterweights and `RotationXFromMovementY` pulleys. Preserve each variant's actual
configuration rather than constructing a universal animator. `MoveableBridge.m_LiftOffsets` supplies
signal-group-dependent target heights; its three values are not an XYZ displacement vector.
`m_MovingTime` participates in native signal timing, not an animation-completion callback.

Moving lights, barriers, rails and other mounted items must follow their proper parent throughout
the cycle. Fixed towers/supports remain fixed. Preserve bridge-specific lighting and effects under
bridge generation contract section 15 and selected-road lighting as selected-road behavior. Avoid duplicate
streetlights where network and object paths overlap. Verify every LOD and motion-dependent culling
extent; a closed-pose bounding box alone does not establish correct open-pose rendering.

## 9. Waterway and traffic compatibility

Both ends of a movable bridge's moving span must use boom barriers. Traffic lights must not be used in place of these barriers. This requirement applies regardless of whether the selected road has sidewalks.

Use the native crossing recognition, lane-signal initialization and traffic-light simulation paths.
Inspect `SubObjectSystem`, `CompositionSelectSystem`, `TrafficLightInitializationSystem`,
`TrafficLightSystem` and `WatercraftNavigationSystem` for the target game version. A waterway drawn
under a bridge visually is not sufficient evidence that the native crossing node was created.

Acceptance must establish the native sequence: prevent new road/track/pedestrian entry, clear the
moving span, open it, allow the requesting vessels through, close it, then restore land traffic.
Preserve lane reservations and auxiliary-net signal grouping. Do not bypass occupied-span checks,
fake an empty deck or add a mod-owned timer to force progression.

Preserve native ship-clearance decisions. The inspected watercraft path compares water height plus
ship bounds against the node's elevation and network clearance data to determine signal need.
Small vessels may pass without opening where clearance permits. Do not invent a single ship-size
threshold or require every vessel to open every bridge. Opening must physically clear the ship as
well as satisfy signals; correct signal state alone does not establish geometric clearance.

Test a native supported narrow seaway crossing first. Wider or other waterway types, skew crossings,
curves, slopes and parallel bridges require explicit evidence before claiming support. Preserve
native restrictions; do not widen the waterway or fabricate a crossing to make an unsupported
placement appear functional. A failed supported crossing must be diagnosed, not masked as a success.

## 10. Lift, rail, pedestrian and double-deck variants

Apply [section 24](#24-track-geometry-overhead-supply-and-material-selection) for track
geometry, overhead supply, selected-track materials and absent surface types. These
requirements also apply when a road bridge is generated with a track input.

Use the selected pathway or track composition for pedestrian/rail variants, with its own lanes,
markings and attachments. Six-lane road results do not establish pedestrian, tram, train or metro
acceptance. Check stopped vehicles spanning the approach and deck, long trains, pedestrian occupancy
and both movement directions where applicable.

A double-deck movable design requires a genuine double-deck archetype under bridge generation contract
section 11. Preserve its main/auxiliary roles, offsets, inversion conditions, linked endpoints and
fixed separation. Bind both decks according to that archetype and coordinate all relevant native
signal groups. Do not hang an extra network below a single-deck mechanism. The existing double-deck
rail-seam limitation remains unfinished work and must not be hidden by this specification.

## 11. Persistence, failures and compatibility

Follow bridge generation contract section 17 and its normative
[asset persistence and reference policy](../Bridge-Asset-Persistence.md). Save newly generated or
modified owned Prefabs and geometry, including required skinning channels, LODs, bounds, native
skeleton configuration and attachment relationships in their actual containing asset formats.
Retain compatible original surface, material, texture, marking and animation resource references;
motion or geometry conversion alone does not require new assets for those resources.

New or modified resources receive private identities only where an actual native conversion or
materialization requires them. Record the changed data and reason. Unchanged external dependencies
retain original bytes/CIDs in per-bridge snapshots; built-in resources remain native references.
Never borrow another bridge's private Prefab, even when a CID matches. Do not mutate the selected
source road or original DLC objects. Preview-only material state is not a persisted native resource.

### Minimal native Prefab graph and unlocks

Match the prototype's responsibilities and structural hierarchy, not an arbitrary total node count.
Selected-road upgrade alternatives can legitimately require more network nodes than the prototype.
Place representable branch conditions directly on native section entries instead of allocating a
wrapper per occurrence. Preserve independent OR groups: `(a OR b) AND (c OR d)` cannot become one
OR list or overlapping lane-producing entries.

Return unchanged Sections, Pieces and Lanes by reference. Copy a Piece only when its fields or
object/track-end producers change; copy its parent Section only when references or other fields
change. Deduplicate equivalent conversions per source and conversion state within the current
bridge operation. Do not retain generated results for reuse by later previews or creation requests;
immutable offline-prepared material sources follow the material generation contract. Keep lane
semantics, requirements, native prices and geometry references. Hidden render layers alone do not
suppress stationary object producers: keep traffic signals network-owned, move visual attachments
with the mechanism, and retain required lighting through appropriate native lighting producers.

Do not merge assets by name, width, lane count or geometry reference alone. Establish equality of
all behavior-relevant fields and referenced resources. Distinguish lateral occurrences, conditional
upgrade alternatives and unique saved files when reporting Section/Piece counts.

Preserve native unlock AND/OR semantics. Put the outer gate directly on root/auxiliary networks
where native representation permits; deduplicate equal nested requirement groups and duplicate
children within the bridge. Keep original progression leaves as references and never flatten
independent OR groups or share private generated groups across bridge owners.

### Native serialization and reader compatibility

Register the complete owned graph before native Prefab serialization. Save generated Geometry,
locales and identity sidecars through the existing shared lifecycle; no parallel save registry or
custom animation format. Reader, migration, recovery and metadata paths must support the native
writer's format, including typed anonymous Vector3/quaternion values in Odin skeleton data. Valid
native text must not be classified as corruption because a helper assumes ordinary JSON syntax.
Fix shared reader compatibility rather than stripping animation or bypassing validation. Preserve
section 19's migration-first lifecycle and UUID/CID identity.

Verify real saved files and the full reference graph through the actual layout/migration readers;
check byte preservation and idempotence. Separately verify cold loading in the game. Parser and
filesystem success are not proof of native loading. Report owned Prefabs, external snapshots,
Geometry, locales and sidecars separately; predicted reductions are not measured saved counts.

Cold-start loading without Bridge Builder must retain construction, native fees/unlocks,
presentation, road connectivity, animation and ship coordination. A warm-session cache or a working
closed bridge is insufficient. Save/load during intermediate movement must recover to valid native
operation without a manager callback; the exact recovery pose is determined by native behavior.

### Failure handling and compatibility

Follow the project's error-boundary policy: contain corrupt external Prefabs and external
API failures; raise explicit ERROR for internal invariant violations, using exceptions where
appropriate. Do not introduce special cases or silent fallbacks solely to suppress internal errors.
Report the exact style, input road/state, affected prefab and failing stage. Do not publish an
incomplete road-only or mechanism-only bridge. Clean up partial new output only within that bridge's
owned paths, using existing persistence handling. Do not introduce a parallel registration store,
quarantine system or live-reference repair process.

Preserve native fees and pricing components on their actual generated geometry/object counterparts.
Do not add custom movement charges, zero fees, price-only copies or runtime fee overrides. Trace
generation, previews, saving/loading, self-check, migration, management, deletion and localization
together. Migration retains identities and follows bridge generation contract section 19; implementation of
a new mechanism alone does not authorize rewriting healthy existing assets.

### Total asset footprint

Bridge generation contract section 24 requires the entire generated Prefab and dependency footprint to
be comparable to the original movable bridge. Reuse existing appearances across LODs and compact equivalent generated graph nodes
within the bridge without losing selected-road semantics. Required LODs, native behavior and
appearance must remain intact. Record a complete, same-basis dependency comparison; root-only,
texture-only and incomplete reference graphs are insufficient evidence.
Material generation contract sections 2 and 5 additionally limit the complete persisted material
dependency graph of one generated owner to 10,000,000 bytes and the complete official SDK content
ZIP to 55,000,000 bytes. Neither a prepared-directory subtotal nor an incomplete graph proves
compliance; report unavailable generated-owner measurements explicitly.

## 12. Integration and update scope

Follow the [generator code organization contract](GENERATOR_CODE_CONTRACT.md). Every movable
generator inherits the shared movable generation base, itself derived from `BridgeGeneratorBase`.
Keep one concrete generator file per style, containing only its unique rules and hooks; common
generation lifecycle logic belongs in the shared bases.

Use existing generation/persistence extension points and the simplest implementation that satisfies
this contract. `BridgePreviewComposition` and `BridgePreviewNetMesh` provide useful composition and
preview paths, but neither proves persistent bone-bound mesh export. Trace their dependencies before
reuse; do not promote disposable preview assets into formal saved assets.

Use a suitable working branch for shared machinery and style-specific data. Per-style branches are
optional under bridge generation contract section 12. Preserve stable style IDs and keep each variant's hardcoded rules distinct.

Deployment follows bridge generation contract section 13: build/deploy coherent increments, back up installed originals and
verify hashes. Preserve healthy assets. Recover only specifically incompatible owned assets; adding
a new movable style does not invalidate fixed bridges. Documentation-only changes require no deployment.

### Preview and performance

Follow the [bridge preview specification](../Bridge-Preview-Specification.md). Assemble both fixed
ends and the complete central mechanism in an open pose; frame and centre the complete transformed
assembly, including skinned bounds. Preview and formal creation share geometry and material rules,
but preview does not publish permanent assets. Use the same localized style/name and normal error
handling as other bridges. A missing generator raises ERROR; it is not replaced by an ordinary road.

### Stage timing and parallel execution

Measure native sampling, geometric conversion, paint clipping, graph assembly, registration,
serialization, dependency copying and publication separately. Include input identity, LOD/triangle
counts, graph counts, cache state and total elapsed time. Do not sum nested stage timings twice.
Remove unnecessary texture/material generation and redundant Prefab wrappers before optimizing
remaining arithmetic. Compare full dependency footprints on the same basis with the prototype.

Follow bridge generation contract section 2 for CPU affine geometry and section 23 for
measured, vendor-neutral bulk image work; never require CUDA. Prepare known constant resources
offline. Generation-time variable composition and unknown selected-input conversion follow only
the bounded exceptions in material contract section 4. Persisted bridges require no
ongoing baking or manager callbacks. Independent pure geometry operations
may process immutable triangle chunks with local output buffers and deterministic merges. Native
asset lookup, Unity/ECS operations, render creation and persistence retain their required thread
and graphics context; parallelism does not authorize unsafe native API calls. Do not claim
parallel execution or an end-to-end timing target without measuring the actual current path.

## 13. Acceptance and completion criteria

Follow bridge generation contract section 4's prototype-referenced mesh complexity rule for every
mechanism and converted road LOD. Excessive subdivision or triangle cutting is prohibited;
necessary subdivision and high-detail-derived LOD permissions do not waive this requirement.

Necessary subdivision of merged structural LOD triangles is permitted while preserving original
vertices, material semantics and interpolated attributes. Deriving, simplifying or supplementing movable
bridge LODs from the measured prototype's high-detail geometry is permitted under bridge generation
contract section 5. Authored part classification and the prohibition on custom shaders remain unchanged. Marking
approximation follows section 7; independent marking objects are prohibited.

Record results per variant and supported input class. The following checks are requirements for
claiming support, not claims that they have already passed:

| Area | Required evidence |
| --- | --- |
| Provenance and anatomy | Complete archetype/generated comparison, measured skeleton/part mapping and versioned extraction record |
| Road fidelity | Selected-state lane positions/directions, sidewalks, markings and supported upgrades match the generated closed surface |
| Width | Real archetype and human-created generated sample; section 14's exact binary32 audit on matching boundaries |
| Geometry and motion | Closed, opening, open and closing views; no seam tearing, floating attachments or ship/structure intersection |
| LOD and lighting | Near/far views throughout motion, nighttime lights and correct culling/shadows |
| Traffic | Clear-before-open, no new entry while unavailable, safe resumption, queue/repeated-request recovery |
| Shipping | Clearance-sufficient vessels and vessels needing opening, repeated vessels and both supported travel directions |
| Variants | Prototype-width, wider and supported asymmetric roads; each claimed pedestrian/rail/double-deck mode. No dedicated narrow-road adaptation. |
| Persistence | Fresh cold start and existing save with Bridge Builder absent; required game/DLC content retained |
| Interrupted movement | Save/reload while opening, open and closing; return to valid native operation |
| Editing and lifecycle | Supported placement, reconnection, upgrade and demolition preserve native ownership and clear entities correctly |
| Regression | Unaffected fixed styles and their persisted assets remain usable; unsupported combinations fail explicitly |

Humans create and judge in-game samples. Agents must not open/control the game or issue construction
API/request-file operations. Automated geometry and serialization checks are permitted; do not
present compilation, synthetic arithmetic or a preview image as visual acceptance. Relevant persistence/reference
checks remain applicable where their paths change.

Delivery must state what is measured, implemented, built/deployed and manually accepted separately.
Readiness for human acceptance is a distinct delivery milestone: complete the authorized code,
applicable automated checks and deployment, then list the in-game checks that humans must perform.
When the user defines that milestone as the task's completion boundary, pending human checks do
not keep the implementation task open; they still prevent claims of completed in-game acceptance.
At that boundary, resolve known implementation gaps, pass applicable automated/native checks,
verify the complete official package and install the coherent candidate. Actual human-created
owner-footprint measurements may be handed off with the human visual/motion/cold-load checks;
report them as unmeasured, never substitute prepared-directory totals or claim their byte gate
passed. Entering the agreed manual-acceptance stage completes the implementation objective,
not the still-pending human acceptance or generated-owner audit.
Attach the actual evidence and list remaining checks. A missing sample or untested native cycle
keeps the relevant support claim incomplete. There is no family-wide deferred status. Implementation and verification status are recorded per
variant; missing human acceptance does not block coding or routing the remaining variants.

## 14. Evidence references and maintenance

Use these documents alongside this contract:

- [Bridge model](../BridgeModel.md): native component roles and current support boundaries.
- [Contributor workflow](../Adding-Bridge-Styles-With-Coding-Agents.md): implementation and submission.
- [Preview render path](../Bridge-Preview-Render-Path.md): network mesh/shader and marking dependencies.
- [Road width calculation](../road-width-calculation.md): selected-state measurement and boundary distinctions.
- [Bridge generation contract](BRIDGE_GENERATION_CONTRACT.md): mandatory geometry, ownership, lifecycle and acceptance rules.

- [Persistence policy](../Bridge-Asset-Persistence.md): normative asset/reference ownership.
- [Preview specification](../Bridge-Preview-Specification.md): normative composition and framing.
- [Mesh repair and performance](../Movable-Mesh-Repair-And-Performance.md): batch-registration and physical-domain evidence; old baking stages are historical.
- [Road paint and platforms](../Movable-Road-Paint-And-Platforms.md): shared paint geometry, fixed caps, measured UV projection and tangent repairs.
- [Graph compaction](../Movable-Prefab-Graph-Compaction.md): native condition-placement evidence; marking representation is defined in sections 18–19.
- [Network and unlock audit](../Movable-Network-Unlock-Persistence-Audit.md): actual saved nodes and unlock equivalence checks.
- [Odin compatibility](../Movable-Persistence-Odin-Compatibility.md): real-file parser/migration validation and remaining cold-load check.

Reproduce the six-lane reference by resolving the two named RoadPrefabs from the installed resource
map, following sections to selected pieces and `NetPieceLanes`, then inspecting DrawBridge03's
StaticObjectPrefab, `NetObject`, `MoveableBridge` and render-prefab procedural animation data.
Decode enum values against the installed assemblies. Follow `NetCompositionHelpers`, mesh-reference
selection, lane generation and the systems listed in section 9 to distinguish data from behavior.
Retain full-precision data in the implementation evidence; rounded examples here are explanatory.

Historical anatomy exports are useful comparisons but cannot establish current field values where
they are truncated or version-mismatched. Future changes must update the measured record and affected
acceptance results. Requirements and implemented fixes do not by themselves establish complete
in-game acceptance; record outstanding checks per style and build.

## 15. Observed failures and mandatory prevention

These rules generalize failures encountered in the implemented bridge. Evidence is linked in
section 14. They prescribe the generation method, not universal dimensions or proof that every
other bridge has passed acceptance.

| Observed problem | Established cause or diagnostic distinction | Required solution and verification |
| --- | --- | --- |
| A median absent from the prototype blocked generation | Motion assignment required a section/face whitelist | Compose the selected road and clip against measured physical domains; exercise unseen sections without adding name exceptions. |
| Road/sidewalk appeared fragmented although triangles existed | Model-container count did not register every material batch | Match native one-model-per-material layout and verify counts at every LOD; inspect rendered output as well as saved triangles. |
| Road parts followed the wrong leaf or split laterally | A longitudinal motion domain is not a traffic carriageway | Clip at measured seams; each triangle belongs to one bone, each leaf carries its full transverse layout. |
| Native grating was mistaken for damaged sidewalk | The prototype deliberately has transparent metal grating | Inspect prototype geometry/materials first; preserve the intended appearance while diagnosing actual missing batches separately. |
| Ordinary approach floated above its pillar | The native pillar was installed without the prototype network Bottom layer | Derive the fixed base and node pieces at every LOD, preserve the native support interface and separate fixed/moving conditions; never raise the pillar to hide the missing base. |
| Fixed base top appeared absent | Sidewalk platforms and carriageway concrete cap are different domains | Retain structural platforms, exclude duplicate road coverage, and assign cap appearance to the generated fixed carriageway; preserve Z. |
| Repeated pale lines unrelated to road markings | The repeated asphalt UV rectangle included a pale atlas gutter | Select verified asphalt interior with filtering margin and preserve physical UV density; inspect a newly generated bridge. |
| Missing or asymmetric sidewalks became symmetric prototype strips | Lateral role selection used prototype curb positions | Use actual selected pedestrian occurrences and geometry; do not fabricate a missing side or mirror its width. |
| Texture scale or direction differed from prototype | Network UV encoding and one global projection did not reproduce object-space atlas domains | Use measured UV-per-metre scale, separate side/leaf origins and mirrors, and verify all LODs. |
| Projection caused invalid tangents | Per-vertex axis projection can vanish or disagree in handedness | Derive per-triangle frames, preserve one handedness, handle singular UV derivatives explicitly without suppressing validation. |
| Paint looked like raised blocks and inflated the graph | Decal projection volumes were rendered as surface geometry; per-marking objects multiplied nodes | Use one compact single-sided skinned paint mesh on the original mechanism with 1 mm clearance; never cut road receivers for markings (sections 18–19). |
| Generation took seconds to minutes and dependencies grew | Historical appearance baking incurred texture upload, mip and save work; redundant wrappers/marking nodes multiplied registration | Reuse structural materials, extract bounded repeat units, share appearance across LODs and remove redundant nodes; measure GPU, VT and persistence costs separately (section 22). |
| Moving lights duplicated or failed at a seam | Network producers and moving visuals had separate ownership/placement | Preserve native traffic/light functions while suppressing duplicate stationary visuals; use measured native lamp mounts where approved, retaining selected model, lateral placement and orientation. |
| Preview omitted ends, remained closed or was off-centre | Complete open-pose assembly and transformed bounds are required | Assemble fixed ends plus moving centre and fit the camera to all transformed bounds; inspect actual panel output. |
| Persisted mechanism was scheduled for removal | The helper parser rejected valid typed anonymous Odin values | Support the native format in the shared reader, replay real Prefabs/layout migration without identity changes, then check native cold loading. |
| Section/Piece and unlock counts exceeded the prototype | Some copies change producers/upgrade references; other wrappers and equal gates are redundant | Copy only changed nodes, share equivalent conversions within the owner, preserve condition semantics, inline the outer unlock gate and deduplicate equal nested groups. |
| Native network structure lost a worldspace layer or used object-atlas density rules | A network Surface was converted despite remaining in a compatible native NetPiece render context | Preserve the measured native network Surface, encoded channels and composition context under section 17; test the complete preview/saved path rather than globally bypassing object conversion. |
| Width changes remapped a positive-V network's transverse atlas coordinate | Longitudinal native distance mapping was mistaken for a two-dimensional repeating atlas | Preserve the authored transverse mapping and evaluate longitudinal density through the evidenced native mapping context; do not apply the generic polar-frame V as an unproven substitute. |
| A finite profile axis was forced to repeat with its other axis | A whole-domain boolean could not express mixed finite/repeating sampling | Retain the evidenced finite axis and independently preserve density and phase on the repeating axis, including constructed fragments and every LOD. |
| A finite source domain was excluded merely because a neighboring chart crossed a repeat rectangle | Atlas overlap was used as physical source-part evidence | Classify the domain's actual source faces and sampling footprint; preserve distinct closure/terminal faces and do not expand an arbitrary crop to hide a true crossing. |

For each future repair, record the input, native evidence, root cause, changed generation rule and
verification limits. Update the corresponding normative section and this table together. Keep
style-specific dimensions, atlas coordinates, face lists and measured counts in implementation
evidence or immutable archetype data, rather than prescribing them as universal constants here.

## 16. Generation pipeline and source map

Follow this dependency order for a new mechanism:

1. Measure the exact prototype's structure, materials, every LOD, fixed platforms, skeleton,
   inverse binds, connection codes, stacking and native traffic/signal behavior offline.
2. Resolve the selected elevated road and upgrades. Evaluate closed-pose road geometry, lane
   visuals and marking layout in the same native sampling context. Keep traffic connectivity
   separate from visible moving geometry.
3. Define prototype-specific road-to-object transforms, physical motion domains and appearance
   frames. Preserve actual asymmetric sidewalks and fixed cap domains.
4. Prepare each road LOD from its appropriate native road input. Convert network attributes to
   object attributes and bind road regions to the original mechanism skeleton. Physical seams
   may require clipping. Paint edges must not drive road subdivision; texture-period wrap planes must not drive road subdivision.
5. Bind original non-road materials, or narrowly eligible offline repeat derivatives under
   material section 3 to avoid new period-wrap seams. Under section 17, compose/precompose supported road types
   and bind original input materials for unsupported road types.
   Apply only material
   contract section 4's bounded variable-content or unknown-selected-input conversions when needed,
   and preserve structural surface-slot semantics.
6. Produce the separate marking mesh in section 18 and append it to the same mechanism object
   using the binding and stacking rules in section 19. Keep existing mesh ordering unchanged.
7. Assemble native network conditions, approaches, lamps, barriers and mechanism references.
8. Use the same graph for temporary preview or native persistence. Register only after required
   dependencies and any generated VT mid-mip data are complete. Verify using section 23.

| Responsibility | Current source reference |
| --- | --- |
| Closed road sampling and per-LOD appearance domains | `Draw01ClosedRoad.Sample`, `PrepareRenderLevels` |
| Immutable mechanism rules | Draw01 measured data and `Draw01RoadPartitions`; replace with each new prototype's own measurements |
| Material/texture production | `MovableSurfaceAtlas`, `MovableRoadMaterial`, `MovableGpuBake` |
| Render models and original structural references | `MovableRoadAssets`, `MovableRoadModel` |
| Marking footprint and mesh generation | `MovableNativeDecals.Regions`, `TowerFactory.AttachMovablePaint` |
| Marking material preparation and persistence | `tools/Prepare-Paint.ps1`, `BridgePreparedMaterials.Paint` and the owner-local dependency snapshot path |
| Final attachment | `Draw01ObjectAssembly` and shared `TowerFactory` |

These are source navigation references, not permission to copy Draw01 dimensions, atlas patches,
bone indices or LOD counts into other styles. Retain the shared generator architecture of section 2
and the generator code contract.

## 17. Material families, UV density and texture ownership

### Non-road reuse and road composition

Here, **precomposition means baking the material outside the game**, saving its complete native
resources and packaging them before distribution. Game loading, preview, creation and export
only load/copy/bind that finished result; startup or first-use baking is not precomposition.
See the [material definition](MATERIAL_GENERATION_CONTRACT.md#meaning-of-precomposition).

The required generation scheme is **original non-road material reuse, composition/precomposition only for
prototype-supported road appearance types, and original materials for every other road type**. This is a generation requirement, not a reverse-engineering
finding. Follow the [material generation contract](MATERIAL_GENERATION_CONTRACT.md) for the
complete source, sampling, preparation, ownership and verification rules.

Retain original prototype Surface identities, shaders, parameters, texture bindings and native
dependencies on structure, towers, trusses, underside, railings and mechanisms, including each
LOD's corresponding material. Geometry adaptation alone must not trigger structural rebaking.
Only the [offline source-atlas repeat rule](MATERIAL_GENERATION_CONTRACT.md#offline-source-atlas-repeat-materials)
permits derived structural materials for evidenced repeat domains to avoid new period-wrap seams.
Preserve compatible native context; failed mapping alone is not eligibility for that rule.

The sole road-composition entry condition is that the prototype supports the actual road
appearance type and that type must use prototype appearance. Determine support separately for
each type and physical domain from immutable prototype evidence. Asphalt, sidewalk surfacing
and wheel-track/rut effects are examples: a prototype may support one, several or none of them.
Do not assume all prototypes share a support list, or turn one supported type into whole-road
composition. Unsupported types retain their original input materials and bypass composition.

For a supported type, resolve its measured prototype sample, masks and required source layers
under section 24. Precompose constant combinations offline; use only verified bounded native
composition for genuinely variable combinations. Preserve unrelated unsupported layers and
markings. This is neither blanket preservation of the selected road's complete appearance nor
blanket replacement with prototype asphalt. Native representation failures do not change the
support classification or authorize a substitute material.

Original road and structural faces may share one Surface or atlas. Keep that source unchanged
for structure except independently eligible offline repeat derivatives, and assign a supported
type's road composite only to its measured road faces/material batches.
Unsupported road types keep their original material bindings.
Do not edit source pixels or global parameters, remove structural details, or change bones to
separate materials. Physical location and a material-slot name alone do not identify road faces.
Fixed structural platforms remain distinct from road-cap appearance domains. Selected track and
attachment rules remain in section 24; marking layout and native skinned paint remain separate
under section 18. This scheme grants no new texture-period cuts; preserve necessary physical and actual material-domain boundaries.

### Preserve original UV projection and mapping mode

Apply the general bridge contract's [original UV projection and mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode).
It governs fixed/non-movable bridges as well as movable bridges. The following source-role
clarifications apply to movable generation without redefining or narrowing that common rule.

For non-road parts, the source is the corresponding prototype material/mapping. For road
composition, retain each contributing source layer's own mapping and the specified blend.
No layer inherits a different source's stretching classification. Measure original geometry,
UV seams and native sampling; texture Repeat/Clamp flags alone do not classify atlas regions.
Retain original texture resolution, channel alignment, normals and complete mip dependencies.
UV adaptation and material-content modification are separate operations.

Known compositions/conversions for prototype-supported road types and their genuinely required
road repeat units are prepared once
offline and shared across all sampling uses. A fully identical resolved region may bind its
original material without baking. Descriptor-only conversions retain compatible pixels. Only
material contract section 4's genuinely variable or unknown-selected-input cases permit bounded
runtime composition/conversion. These runtime exceptions permit neither full-road rasterization nor structural rebaking;
section 3's eligible structural repeat derivatives are prepared outside the game only.
Preserve independent owner snapshots, native loading and no custom shaders/runtime rebinding.

### Native network context and independent sampling axes

Decouple distinct material layers under material generation contract section 3: retain independent
coordinates, periods and phases for road, asphalt, worldspace detail and blend masks, while
preserving each layer's authored channel alignment. Do not impose a common period on all layers.
Replace the universal 0.01-pixel phase cutoff with a measured, explicitly documented per-layer
tolerance; its new numeric value remains pending validation. Verify the native skinned-object
and persistence paths rather than inferring capability from network rendering.

Material generation contract section 3 prohibits expanding any pattern across the complete
road/span into texture pixels, including mixed and non-repeating appearances. A failed repeat
test must not fall back to receiver-length baking. Splitting the same bake into smaller pages
or VT tiles does not comply. Runtime composition exceptions remain subject to this prohibition;
retain source content through a verified native representation and preserve its resolution.

Apply the following source-evidenced routes without changing geometry, topology, bones,
native texture pixels, mip chains or independent persistence:

- **Draw03 native network structures:** when an approach remains a native `NetPiecePrefab`
  through `CreateMovableRenderGraph`, retain its compatible original network Surface and
  network vertex encoding. Preserve the actual composition/curve context, signed-V mapping,
  transverse mapping, worldspace layers and native layout. Verify each affected render and
  LOD and both preview and persisted assembly. Do not force template 17/33 into template 0
  merely because the common mesh writer is also used for objects. Conversely, a NetPiece
  capability must not leak into a skinned-object path or justify a global conversion bypass.
- **Positive-V native network mappings:** preserve the authored transverse coordinate and
  original longitudinal texel density. Use the source's actual signed-V rate and resolved
  native longitudinal distance/phase, or retain the native renderer that evaluates them.
  Distinguish encoded vertex rates from evaluated fragment UVs; do not multiply a rate twice,
  normalize positive V as a finite chart, or substitute a two-dimensional polar-frame mapping
  without proof. A required object conversion must establish an equivalent longitudinal
  mapping and all material layers before it is accepted.
- **Lift04 finite U with repeating V:** express the two axes independently from measured
  source/domain data. Retain original corrected U, interpolated on permitted construction
  fragments, while preserving V's original physical density, phase and verified repeat
  boundaries. A whole-domain non-tiling flag cannot authorize stretching V. Keep the original
  Surface where it expresses the measured mixed axes without new period cuts. An eligible
  structural repeat domain may instead use the narrow offline derivative in material section 3.
  A separately identified prototype-supported road
  composition may use its evidenced genuine unit under material contract sections 3 and 4.
- **Domains touching repeat coverage:** classify the actual original source faces, authored
  part and full sampling footprint. A chart neighbor or bounding-rectangle overlap alone
  does not establish that a closure, terminal strip or other finite surface repeats. Apply
  finite retention only to the proven source faces/domains. A face genuinely crossing finite
  and repeating semantics needs an evidenced native representation or the already permitted
  measured construction; do not hide the crossing with an expanded arbitrary crop, a blanket
  allow flag or new per-receiver pixels. New cuts solely for atlas-period wraps are prohibited; use eligible offline source-atlas repeat materials.

Store immutable axis and native-mapping decisions separately from texture identities. Keep
whole-source retention compatible with the existing finite-domain path; unknown or unchanged
classifications do not silently gain permissions. The retained component of a mixed mapping
must come from the corrected/interpolated source UV, never from an already density-remapped
coordinate. Verify both axes after final native mesh upload, not only eligibility predicates.
Native network checks additionally cover material identity, vertex-stage semantics, worldspace
mask/layer behavior and the actual render-context binding. Keep exact original face/domain
provenance in the observer so retained, density-mapped and unresolved output remain distinct.

Apply the supported-type gate before the conversion families below. Unsupported road types
retain original materials; none of these conversion rows admits them. Keep the following paths
distinct; a global switch to VT or a global material replacement cannot implement them correctly.

| Family | Current generation/persistence treatment | Required invariant |
| --- | --- | --- |
| Carried prototype structure, underside, towers and supplements | Retain original Surface and texture identities by default; only evidenced repeat domains needing avoidance of new period-wrap seams may use material section 3's offline derivative | Preserve per-LOD source projection: tiling stays tiling, stretching stays stretching, including mixed axes. Record narrow derivative eligibility and validate its sampling; no general structural material replacement |
| Converted repeating asphalt | Reuse a compatible original material, or copy the once-prepared genuine unpainted repeat unit and native non-VT object Surface shared by all its uses | Continuous UV repetition preserves measured prototype gradients and physical scale |
| Converted repeating metal grating | Reuse the original where compatible; otherwise copy the once-prepared genuine grating unit and `DefaultCutoutDoubleSided` non-VT Repeat material shared by all its uses | Preserve alpha/cutout holes and two-sided behavior; never turn the sidewalk into opaque asphalt |
| Other converted local road appearances | Copy known prepared constants; apply material contract section 4's bounded variable-content or unknown-selected-input conversion only when necessary | Preserve fixed-cap/material domains, channel semantics, padding, mips and per-LOD UV assignment |

Paint is a fifth, separate family defined in section 18. Do not bake it into the asphalt or into
structural underside textures. `Draw01ClosedRoad` currently passes an empty marking-region list
to `MovableSurfaceAtlas.Compose` and attaches paint separately afterward.

For repeating units, derive UV axes, orientation and origin from measured patches of the applicable
source selected under section 24. Establish each genuine unit using the material generation
contract section 3: authoritative source identity/version, measured region and physical period,
channel semantics, and tileable-boundary evidence through filtering and the complete mip chain.
A rectangular chart, a triangle's extent or an arbitrary crop does not establish a new unit.
Unit identity must not depend on the road, receiver, triangle, LOD or sampling variant.

When extraction is necessary, the offline extractor sizes the unit from its verified source atlas
rectangle, rounded to BC7 block alignment; it does not stretch a full-span baked image. Prepare
each real unit once, then reuse the finished payload for every sampling use of that unit. Different
real units in the same atlas may each have their own necessary conversion; do not bake per triangle,
per sampler or per receiver. Retain one original payload wherever unchanged source texels and
supported native sampling already serve the uses correctly. Keep unit provenance and per-use
orientation, phase and UV mapping separate from the shared finished payload. Per-style and
per-owner byte-identical copies do not authorize rebaking. Record actual dimensions and density
for each generated material. A cropped genuine unit need not equal the dimensions of the full source atlas.
Do not claim every material is 1024 x 1024 or 409.6 pixels/metre: these are not universal constants
and the accepted prototype-derived path must be measured on its own inputs.

For each physical unit direction d, verify density using the final texture dimensions and UV
Jacobian: sqrt((width * du/dm)^2 + (height * dv/dm)^2). Check both longitudinal and transverse
axes, including mirrored leaves, after cropping, packing and persistence. Preserve native scale
within necessary block-alignment rounding; document rounding rather than claiming bit-exact density.
Do not add triangles at Repeat boundaries or expand units to 16384-square full-span images.

Color is sampled with its proper sRGB semantics; normal, mask and control data are linear.
Preserve the native channel meanings and valid normals/tangents. Generated object meshes use
packed octahedral normals/tangents, so the saved material must carry `_TANGENTSPACE_OCTO`.
Preview-only keyword injection is insufficient. Native object skinning support must be verified
in the selected template; toggling a keyword cannot add missing shader functionality.

Prepared road units that require whole-texture Repeat use the verified non-VT object path.
This does not require converting original VT structure: its mesh-authored atlas repetition can
remain valid with Clamp. Do not infer region-local repetition from a VT allocation transform.
For remaining VT composites use native configuration, BC7 formats and native ZSTD compression
setting; never modify global compression configuration to speed one generation. Complete the
matching mid-mip cache (currently three levels), register/refresh VT resources and preserve the
whole dependency chain before publishing the render. A mismatch between requested and stored
mid mips is a generation failure, not a reason to suppress the error.

Known converted VT surfaces use offline-prepared MidMips dependencies. Material contract section 5
permits only these immutable cache sources to share the read-only packaged
`Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` path and CID sidecar. Preflight primary
and duplicate Surface membership plus matching native mip levels before registration. Each bridge
copies only its required cache dependencies into its own independent snapshot, retaining bytes,
CIDs and native tag-bearing filenames. Other per-style resource rules and runtime baking limits
remain unchanged; the owner material limit still includes these dependencies.

### Offline repeat materials instead of period seams

Apply [material section 3](MATERIAL_GENERATION_CONTRACT.md#offline-source-atlas-repeat-materials)
to road and narrowly eligible structural repeat domains. Bake independent tiling materials from
the original atlas outside the game instead of adding cuts solely for period wraps. Preserve
existing native seams and necessary motion, shape and actual material boundaries. Keep independent
axes/layers, phase, original density, finite terminals, original attributes and bones. Fully reusable
original materials remain unchanged; this is not general structural rebaking. Derived assets have
new identities while source assets stay unchanged. Complete all PBR/filter/mip, native persistence
and 10 MB owner / 55 MB package checks; no full-span bake or relaxed global tolerance is allowed.
Current feasibility and implementation must be verified separately from this specification.

## 18. Marking layout, mesh and native material

The current implementation uses native **layout and texture input**, rendered by an object
material that supports procedural skinning. It does not run native child decal projectors.

1. Read selected-road lane/decal evaluation batches. `MovableNativeDecals.Regions` supplies the
   supported white/yellow footprints in the road-to-object frame. Preserve spacing, direction,
   color and supported footprint shape; do not derive lane positions from a fixed lane count.
2. Partition marking polygons only at measured physical motion seams, discarding gap regions.
   Find each footprint vertex's upper receiver height on the evaluated generated road. This is
   not Boolean intersection with prototype geometry. Missing receivers must fail explicitly.
3. Offset the paint by **0.001 m** along the local upper normal, then triangulate the paint
   polygons and assign the same physical-domain bone as the road beneath them. This adds paint
   triangles; it neither cuts receiver triangles nor moves the original road.
4. Combine supported footprints into a compact paint model with the native object vertex layout,
   UInt32 bone indices, valid normal/tangent channels and matching winding. Append one paint
   RenderPrefab/ObjectMeshInfo to the mechanism rather than one StaticObjectPrefab per dash.
5. Use the native `Default` object template with procedural skinning, opaque single-sided paint,
   backface culling and decal reception disabled on **paint only**. Do not render projection
   volume sidewalls or enable double-sided markings. The underside must remain unpainted.

The current paint color source is the native No Parking surface. Prepare its actual decoded color texture offline and persist it at the source resolution (2048 x 2048 in the measured source),
then sample measured white/yellow locations with constant UVs. Polygon outlines supply the
marking shape. Prepare the neutral 1 x 1 control texture and native non-VT Surface with it.
Preview and creation copy the finished constant resources without decoding or rebaking. This is
not a textured dash atlas baked across the full bridge, nor complete native decal shading.
Measured source identifiers/texel coordinates live in implementation data, not universal rules.
Complex symbols, additional colors, fades or infoview semantics require explicit implementation
and acceptance before claiming support; grouping metadata alone does not implement their shading.

A native VT surface's original Texture CID may be provenance only and unavailable as a real
.Texture asset. During offline preparation, resolve the actual VTSurface color layer or
preprocessed VTTexture backing, decode it through the GPU path and persist the finished native
resource. Runtime uses that packaged resource without decoding or rebaking it. Do not serialize a missing original color CID. Restore graphics state, balance texture leases
and destroy only privately created resources on success and failure.

## 19. Common motion, stacking, visibility and underside

The road retains the original mechanism skeleton and skinning path. Paint adapts to that road;
never move road geometry onto marking child bones or history-driven transforms.

The paint render must copy the original road's `ProceduralAnimationProperties`, use identical
bone numbering/rest transforms/bind poses and match its `ObjectMeshInfo` position and rotation.
Copy `StackProperties` when present and preserve the same authored root bounds. Matching bones
alone is insufficient when the native object applies stack placement or scale. Do not substitute
swept animation bounds for authored stacking bounds; preserve native animated culling behavior.

Child projectors are prohibited because the inspected native update order let relative child objects
read previous-frame BoneHistory while the skinned road used the current frame. The resulting
misalignment occurred during movement and disappeared on pause. Increasing projection depth or
moving the road into that delayed path does not meet the requirement. No Bridge Builder update
loop, Harmony synchronization patch or custom shader may be required after generation.

Keep paint single-sided and above its upper receiver under the same transform. Do not disable
decal reception by cloning every structural Surface. Preserve the original structural references
and their source projection under material contract sections 1 and 2; road composition must not rewrite them. The
observed copying defect does not establish a universal engine defect or authorize built-in edits.

The current compact paint mesh remains present across road LOD transitions rather than being
culled dash by dash. Verify native group bounds and visibility at all distances and during motion.
The previously measured 384 paint triangles are one sample, not a fixed count or future budget.

## 20. LOD, topology and prototype-specific extension

Draw01 currently prepares three road render levels from corresponding native road inputs and
shares generated appearance resources. Original structure slots and native far structure remain
separate from road conversion. Another prototype may have a different graph; measure it.

Maintain these distinctions when reporting face counts:

- Carried/width-adjusted prototype structural triangles, including each actual LOD.
- Converted selected-road triangles, including required physical/appearance-domain partitioning
  without coplanar coalescing or its retriangulation.
- Deliberately supplied structural supplements, if the current measured style requires them.
- Additional single-sided paint triangles, generated from marking footprints only.

Do not claim that generation adds no triangles: paint does add them. Do not introduce road
subdivision to encode paint outlines. Cuts solely for texture-period wraps are prohibited; use qualifying offline repeat materials. Any LOD supplement needs source geometry,
material and part provenance and a stated purpose; never automatically copy a mid-distance bottom
plate into every far LOD. Retain native simplified underside where present and test underside
appearance at transitions. The paint exception does not authorize arbitrary structure replacement.

For each new style supply its own fixed/moving partitions, upper receiver roles, stack semantics,
source atlas regions and mirrored projection frames. Keep source grating, caps and railings on the
correct side according to the actual selected sidewalk layout. Exercise seam-crossing paint and
both motion directions before generalizing from the Draw01 implementation.

## 21. Preview, native persistence and failures

Preview and placed assets must use the same geometry, material families, bone binding and stack
rules. Preview resources belong to its private database/capture lifetime; never rely on private
preview texture references resolving through the global database after disposal.

Persist generated Geometry, Surface, Texture and any VTSurface/mid-mip dependencies through the
existing UUID-owned native asset graph. Keep unchanged built-in material references; snapshot
required external dependencies according to the general persistence contract. Persist paint
geometry/material/texture before publishing their references. Roll back owned partial outputs
on failure and report the unavailable dependency/stage rather than publishing a broken bridge.

Generated bridges remain independent native assets with required game/DLC content as prerequisites.
No manager-driven material rebinding, per-frame animation repair or regeneration is allowed.
On a generation repair follow owner-scoped backup/cleanup and deployment rules; stale saved
assets do not acquire new serialized materials just because the DLL was replaced. Never delete
unrelated bridges or native resources. Cold-load tests remain a separate acceptance requirement.

## 22. Performance and diagnostic requirements

Structural coordinate generation follows the shared
[CPU affine execution contract](BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution).
Keep separate upper-deck, lower-deck and structure width parameters where measured. The sequence
is main-thread mesh snapshot, independent numerical LOD planning through `AffineGeometry`,
worker completion, then main-thread native render/asset construction. Within a worker, nested
vertex batches execute serially. Preserve recipe ordering, original road bones, marking bindings,
structural material slots and all LOD topology. Affine coefficients may affect any XYZ axis;
do not force Y/Z constant or invent coefficients for a prototype that lacks them.

Log `snapshotMs`, `parallelPlanMs` and `totalMs` from `MovableRenderAssembly` alongside the
other generation stages. The structural subtotal excludes later native render-graph creation;
it is not total preview or permanent-generation time and must not be counted twice.

Preserve source-scale repeat units instead of baking the entire span. Share composed resources
across LODs within their owner; reuse original structure assets. Avoid one object/material per dash,
unnecessary native texture decoding, repeated VT compilation and redundant registration wrappers.
Choose CPU or the existing vendor-neutral GPU path for bulk image work from measured cost,
correctness and native API affinity under bridge generation contract section 23. Known, preparable
constant material preparation remains offline; CPU/GPU placement does not expand material contract
section 4's bounded runtime exceptions or authorize rebaking known prepared sources. Native Unity/database
calls retain thread affinity. Do not reduce native texture quality or global compression settings
to meet a target.

Record sampling, road conversion, GPU composition/readback, texture and VT writing, mid-mip
completion, paint generation, dependency persistence and registration timings separately. Include
selected road/style, cold versus warm generation, output texture dimensions, Repeat/VT treatment,
material and triangle counts by LOD, and native dependency errors. Count total time without double
counting nested stages. The user requested seconds-scale creation; do not assert that target is
met without a new measurement. Stale log labels such as 'paint uses native decals' are not proof
that the current renderer uses projectors; trace the actual `AttachMovablePaint` path.

## 23. Acceptance and contributor checklist

Acceptance is specific to a tested style, input, build and scenario; it must not be
generalized to every bridge, persistence scenario or performance target.

Before declaring a newly added mechanism complete:

- Compare closed, opening, fully open and closing states at near/far distances. Run motion as well
  as pause: pause alone masked the historical transform lag. Inspect both leaves and the underside.
- Check paint placement, backface absence, 1 mm clearance, distance visibility and no deck slicing.
  Confirm grating holes, caps, towers and structural materials retain prototype appearance.
- Measure native versus generated UV density in both axes and verify material slots at every LOD.
  Audit verified source finite/stretch uses under material contract section 2 separately, reporting
  their allowed density change and retained source UVs rather than a density-preserving pass.
- Exercise preview and newly placed output separately, then save/restart with the manager absent
  and required DLC present. Verify all owned references and VT mip dependencies.
- Complete the traffic, shipping, supported upgrades/asymmetry and lifecycle checks of section 13.
- Report actual face/material/texture counts and generation timings, distinguishing sample results
  from universal rules. Compilation, hash verification and a static preview are not visual acceptance.

Do not use game UI automation or construction request files. Human users create and inspect game
samples under the project rules. If a check is pending, state its precise scope without undoing
already confirmed acceptance or claiming unrelated tests have passed.

### Preserve partitioned road triangles

Road preparation, atlas input/output and final render assembly must retain each partitioned input triangle rather than merging adjacent
faces and fan-triangulating the result. Texture charts may still share appearance resources;
chart sharing must not merge geometry. Motion/appearance-domain clipping, measured prototype
replacement fragments and separate paint polygon triangulation remain in place. Do not coalesce these road triangles.
Actual post-change face counts and performance must be measured on newly generated assets.

### Native persistence layout

Follow [persistence policy sections 6–7](../Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
for UUID-contained Prefabs, Geometry, Appearance, Locales and Dependencies, safe owned filename
stems, paired CID migration and on-demand directories. All owned LOD Prefabs share `Prefabs/`
while retaining independent CIDs. VT/mid-mip resources belong below the same owner UUID.
Layout changes preserve internal references and do not establish native cold-load acceptance.

## 24. Track geometry, overhead supply and material selection

These requirements apply to every movable style and every deck,
including road archetypes generated with railway, subway or tram inputs. It does not
change unrelated non-movable bridges.

### 24.1 Track geometry and ownership

Resolve the selected track's actual native composition and upgrades. Preserve steel
rails, sleepers and applicable native track attachments with their geometry, UVs and
material assignments. Visible lane geometry must not disappear when the corresponding
network rendering is hidden to avoid duplication. Keep logical lanes, restrictions,
signals and native traffic behavior; visible track meshes are not a substitute for them.

Bind generated rails and sleepers to the original road motion domains and bones.
Preserve every required LOD and the source material-slot mapping. Where the native
batch limit requires separate render graphs, use independent native LOD groups within
the same mechanism, with the same original skeleton. This is not authorization to
split the track into independently updated objects, discard surfaces, omit distant
LODs or introduce runtime synchronization patches.

### 24.2 Overhead supply and support placement

Preserve overhead supply when the selected track uses it, including railway and tram
inputs. Do not assume that every subway or other track uses overhead supply. Lift03
may provide implementation evidence, but its dimensions, deck frames and pole layout
must not be copied to another archetype without that archetype's own evidence.

Wires must pass through each retained pole's native hanging points. Resolve pole and
wire coordinates in the same closed-pose frame, including lateral offset, elevation
and orientation. Preserve the original motion binding and verify contacts through
opening and closing, not only in the closed pose.

Keep longitudinal support placement symmetric. A pole whose original mounting
anchor lies in an opening gap must be removed, not shifted to another station.
Never move utility poles to lamp stations or resolve a nearest-station tie to one side.
When the selected track provides poles, supplement its support rows at both outer ends
of the moving span. Use equal inward clearance derived from native support dimensions
and the measured motion domains; avoid duplicates and keep the support clear of the gap.
These endpoint additions do not reintroduce the pole removed from the central seam.
Preserve native transverse placement, including legitimately one-sided rows, instead
of inventing a mirror across the road width. Do not add poles to a track without them.

### 24.3 Material selection order

Apply these rules by surface type and physical motion domain, independently of the
section containing the surface. These rows select road-composition inputs and non-road reuse;
they do not impose preservation of the selected road's complete final appearance.
Only prototype-supported appearance types enter composition/precomposition; other types keep
their original input materials. Asphalt, sidewalks and wheel-track/rut effects are classified
separately, using each prototype's own measured support. Record prototype appearance from real source data;
players need not possess the original files. Compare each deck separately.

| Geometry and location | Required material appearance |
| --- | --- |
| Selected rails and sleepers | Original selected-track materials and UVs. |
| Road appearance type absent from the applicable prototype domain | Original input material and mapping; bypass composition/precomposition. |
| Road appearance type supported by the applicable prototype domain | Use measured prototype appearance through composition/precomposition; retain each contributing source's tiling/stretch mapping. |
| Asphalt, including empty lanes and sidewalk parking areas | Classify as asphalt, then apply the supported/unsupported rule above; asphalt is not automatically supported. |
| Sidewalk surfacing and wheel-track/rut effects | Test each type independently; support for asphalt does not imply support for either of these. |
| Stationary edge abutments/platforms | Existing stationary appearance rules. |
| Other bridge structure | Reuse corresponding original prototype materials by default; only material section 3's evidenced offline repeat derivative may avoid new period-wrap seams. |

A stationary abutment does not become a moving surface merely because both are stored
in the same mechanism. Determine the boundary from recorded physical partitions and
original bone ownership, not appearance-region indices or object membership. Never
use the absent-surface-type rule to replace the material of the entire mechanism.

Original appearance includes texture references, parameters, UV mapping, worldspace
and atlas blending, and relevant upgrades. Retain native receiver UVs until the path
is selected. Native skinning-compatible conversion must reproduce the original appearance.
Known, preparable constant conversions are prepared offline, once per required source conversion
or evidenced genuine repeat unit and shared by all its uses. Genuinely variable composition and
necessary conversion of selected third-party inputs unavailable before runtime follow material
contract section 4, prioritizing direct reuse and descriptor-only conversion. Do not fall back
to bridge material or another visually similar material. Preserve VT headers, backing
resources and ordered texture stacks where applicable. Do not modify source assets.
Persist owner-local derived descriptors and all non-built-in dependencies under the
existing independent-persistence policy. Previews and all saved LODs use the same rule.

### Native track material conversion

Rails and sleepers retain selected railway, subway or tram appearance at every LOD.
Reuse compatible native object materials directly. Where conversion is required after native
curve geometry evaluation, use the measured template mapping: Curved (16) to Default (0),
CurvedCutout (31) to DefaultCutout (1), and CurvedHashedCutout (34) to DefaultHashedCutout (35).
Reject unsupported templates instead of substituting bridge material. The measured textureless
cable conversion is separate. Prepare known, preparable constant converted resources offline;
preview and creation copy and bind them. A previously unknown selected-track input may receive
the necessary descriptor-only conversion during the current operation under material contract
section 4, retaining its compatible textures without pixel baking.

Preserve the complete source descriptor, VT header, VTSurface reference, texture CIDs and property
overrides. Check identical ordered texture stacks, preserve native appearance defaults where the
object template differs, and enable packed tangent encoding. SaveData preserves VT provenance;
ordinary Save(vt:false) clears it. Do not change source assets or texture payloads. Preserve UVs,
geometry, bones and LODs, and include external dependencies in the independent owner snapshot.

### 24.4 Acceptance coverage

Acceptance must cover railway, subway and tram inputs as applicable; an unsupported-surface-type
moving section beside its stationary abutment; a supported-surface-type control; near/far LODs;
closed/open poses; native wire contacts and symmetric end supports; separate decks;
and cold loading of persisted assets. Existing bridges do not acquire new geometry
or materials merely by updating the DLL; regenerate a sample for these checks without
deleting healthy saved assets. Do not report pending behavior as fixed from a build.

All user-visible implementation text must support every UI language under AGENTS.md.

### 24.5 Surface-material implementation

Dispatch road types by measured prototype support: supported types enter composition/precomposition;
others retain original materials. Keep non-road material bindings unchanged except eligible
offline repeat derivatives under material section 3. Retain evaluated native UVs on moving receivers. Evaluate native worldspace-layer
masks before substituting a measured prototype road sample: a section may contain
both asphalt and other surface types. Keep unsupported layers original. Physical
bone connections exclude stationary edge platforms from this path.

Texture projection and repeated texture extraction must use independent horizontal
and vertical dimensions. All LODs and saved assets use the same native material output;
no runtime shader or synchronization dependency is introduced. Rebuild the generation
GPU bundle when its kernels change. Game appearance and cold-load checks remain human
acceptance items; compilation alone does not establish visual correctness.

### 24.6 Native texel density on every movable component

The density requirement applies to every generated component and every LOD of
all movable styles: deck tops and undersides, stationary portions within movable
bridges, trusses, supports, towers, railings, tracks and attachments. Retain the
corresponding prototype's physical texture resolution except for the verified non-tiling
UV uses allowed by material contract section 2. Preserve native texture resolution in both paths.
For surfaces intentionally
preserved from the selected road/track, that selected source is the reference.

Unchanged UV coordinates do not prove compliance after geometry deformation. Compare
the two-dimensional texel metric before and after transformation on each non-degenerate
surface. Include final texture dimensions, both UV gradients, filtering/mip behavior,
normal/mask/control maps and atlas packing. Translation/rotation may retain original
UVs; stretching tiling geometry requires a measured density-preserving repeat/remapping path.
Verified source finite/stretch uses retain authored UVs, interpolated for permitted construction
fragments; audit source/part/LOD/axis evidence and their expected physical-density changes separately.
Use section 17's native network and independent-axis routes for signed-V longitudinal mappings,
mixed finite/repeating axes and source-face boundaries. Do not apply ordinary two-dimensional
atlas density rules to encoded native network coordinates before their renderer semantics are resolved.
Do not sample across atlas-island seams. Degenerate authored UVs must be identified
separately rather than reported as passing a density comparison.

Do not reduce chart resolution to satisfy a one-page or material-batch target. Do not
replace a native texture with an upscaled low-resolution bake and call it restored.
Use native-scale bounded units and supported native LOD/material grouping. Preserve
geometry, bones, material semantics and independent persistence. Build success,
yellow-pixel absence and UV continuity are not texel-density acceptance.

Do not fit mixed charts by reducing their packing scale.
Mixed receivers with a verified common period use the same once-prepared genuine repeating unit
at native density; changing a receiver or its sampling does not create another unit.
Verified source finite/stretch receivers retain their original native texture/channel footprint
and authored UVs under material contract section 2; geometry stretching needs no full-span bake.
Other nonperiodic receivers retain their full channel footprint and are never silently reduced
to fit a page. Oversized cases without a verified non-tiling mapping remain unresolved and fail
explicitly until a bounded native representation is provided. Missing periodicity is not proof
of non-tiling authoring. This is
not permission to leave a supported style unbuildable or to call the whole repair complete.

Measured rectangular and nonrectangular structural domains must follow the same evidenced
tiling/non-tiling classification. Tiling domains use the native-density path; verified non-tiling
domains retain source UVs under section 2 of the material contract.
A structural UV domain is not automatically a genuine repeat unit: establish
the source pattern and tileable boundaries under section 17 before extracting one. Nonrectangular
domains use existing source triangles without physical
period cuts. The catalogue includes 190 original Geometry files / 210 mesh streams,
including stationary approach and network-pillar parts. Additional native network
appearance groups occupy the same section with identical dimensions, offsets and
requirements; construction/upkeep components remain on the original group only.

Complete generated-output density, size and all-style build audits remain outstanding.
Prior Draw01 underside seam checks and source-frame numerical tests do not substitute
for those audits. Native near/far rendering, movement and cold-load observations are
also pending. No blanket all-style acceptance is recorded.
