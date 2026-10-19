# Movable bridge generation contract

## Contents

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
- [16. Accepted generation pipeline and source map](#16-accepted-generation-pipeline-and-source-map)
- [17. Material families, UV density and texture ownership](#17-material-families-uv-density-and-texture-ownership)
- [18. Marking layout, mesh and native material](#18-marking-layout-mesh-and-native-material)
- [19. Common motion, stacking, visibility and underside](#19-common-motion-stacking-visibility-and-underside)
- [20. LOD, topology and prototype-specific extension](#20-lod-topology-and-prototype-specific-extension)
- [21. Preview, native persistence and failures](#21-preview-native-persistence-and-failures)
- [22. Performance and diagnostic requirements](#22-performance-and-diagnostic-requirements)
- [23. Acceptance record and contributor checklist](#23-acceptance-record-and-contributor-checklist)
- [Organization revision record](#organization-revision-record)

## 1. Authority, scope and implementation status

Current accepted generation baseline: **2026-10-18**. Sections 16–23 consolidate the latest
implemented flow and the user's acceptance after restoring native structural materials. Read
these sections before adding any movable style. They supersede earlier conflicting paint,
material-copy and texture-composition instructions in this document and its historical records.
Unrelated geometry, traffic, native persistence and archetype-evidence requirements remain in force.
Acceptance covers the latest reported Draw01 visual repair; it does not certify every input,
other mechanisms, traffic/shipping tests or manager-independent cold loading.


This is the normative supplement to [BRIDGE_GENERATION_CONTRACT.md](BRIDGE_GENERATION_CONTRACT.md) for bascule/draw
bridges, vertical lift bridges and any subsequently admitted movable design. Read it together with
the root [AGENTS.md](../../AGENTS.md), the complete project and bridge generation contracts and applicable directory rules.
The bridge generation contract remains authoritative for geometry, source selection, branches, persistence,
pricing, cleanup and acceptance. This supplement does not enable an unsupported generator.

User decision recorded on 2026-10-15: the moving road surface MUST be generated from the selected
road's closed-pose composition, converted into persistent object geometry, partitioned and bound to
the measured mechanism. Reusing the prototype's road surface as the generated road is not an
alternative implementation of this requirement.

Rollback baseline recorded before editing: branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; local reference
`refs/rollback/movable-bridge-contract-20261015`. Existing uncommitted changes are preserved;
the Git reference records the committed baseline, not those changes.

Revision baseline (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/movable-generation-contract-20261017`. Existing working-tree changes are preserved.

### Implementation status and evidence

This revision generalizes the implemented selected-road conversion pipeline and its observed
failures. Draw01 has a working generation path and user-confirmed normal appearance. That does
not establish full traffic, shipping, every road/LOD combination or mod-independent cold-load
acceptance. Other mechanisms require their own measured data and implementation evidence;
registered catalogue entries alone do not establish working generators. Completion status must
never disable a registered style or add unfinished-style notices (bridge generation contract section 22).

Implementation tracking began on 2026-10-16 for all ten audited B&P mechanisms. The independent
style IDs and branch mappings are reserved in bridge generation contract section 12. The
[implementation record](../Movable-Bridges-Implementation.md) distinguishes measured evidence,
code, deployment and human acceptance. It must be updated alongside implementation; a completed
backup or audit is not completed generator support. Pre-edit baseline for this addition: `dev` /
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`, rollback reference
`refs/rollback/movable-implementation-docs-20261016`.

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
| Assign appearance | Native structural references, measured repeating road units, bounded local composites and separate skinned paint | Follow sections 17–19; no road cuts for paint or texture repetition, no custom shaders. |
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

Revision baseline (2026-10-17): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`;
rollback reference `refs/rollback/movable-fixed-contract-20261017`. Implementation and pending
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

Identify authored mechanism parts offline. Commit reviewed immutable data using the existing
metaprogramming workflow; do not rediscover parts at generation time from bounds, nearest vertices,
names, connected components or non-zero coordinate thresholds. Preserve every variant's own data.
Do not apply DrawBridge03's hinge positions or LiftBridge01's travel to another variant.

The existing `x = 0` stretch/translate rule, tower-height/depth constraints and binary32 width audit
remain in force, with the user-approved central railing post exception in bridge generation contract section 8.
An independent vertical railing post centred on the bridge centreline retains its original width
and centred position in the highest-detail mesh and every LOD. Identify exact membership offline;
do not extend the exception to transverse rails, walls or other crossing parts. Approval baseline:
`dev/80eb1415e1a3b59263eb45693f7e2c5d80732447` (2026-10-16).
Motion partitioning is a separate operation from transverse structural widening.
It cannot change which authored parts stretch or translate. Missing evidence rejects the affected
variant; it does not authorize guessed geometry or an unrelated fallback.

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

The following reference was extracted read-only on 2026-10-15 from the local game's
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

Prototype-face intersection is prohibited by general bridge contract section 2. Use the
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

Reuse a source-road material when its vertex layout and native animated-object rendering path are
compatible. Otherwise choose an existing compatible native object material measured from the
prototype or another permitted native resource. Keep carried structural materials unchanged.
Generated native material descriptors and GPU extraction/composition from existing native textures
are permitted under section 17. Custom shaders and structural receiver-only descriptor rewrites
are prohibited. Native marking layout is evaluated before producing the skinned paint described
in section 18; this is not the native projector shader or a full reproduction of decal fading.

For converted surfaces, derive UV axes, origin and physical scale from prototype object-space
projection. Separate measured material, side and leaf-orientation domains; one global projection
or copied network-packed UVs is insufficient. Keep compatible object materials' own UVs. Exclude
embedded prototype paint and pale gutters from sampled asphalt/concrete regions to avoid repeated
false lines. Inspect the existing atlas interior and filtering margin, retaining measured physical
texture density when cropping its rectangle. Extract repeat units without enlarging them to cover
the complete bridge; preserve native channels, filtering and physical UV gradients (section 17).
Build tangent frames from triangle position/UV derivatives with consistent handedness; when the
projection has zero UV area, retain a valid source frame rather than divide by a singular mapping.
Do not claim exact authored UV-island or LOD-atlas reproduction without verifying it.

Preserve fixed platform/curb structural faces with their original UVs and materials. Exclude their
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
change. Cache equivalent conversions per source and conversion state within one bridge. Keep lane
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

Follow the project's revised error-boundary policy: contain corrupt external Prefabs and external
API failures; raise explicit ERROR for internal invariant violations, using exceptions where
appropriate. Do not introduce special cases or silent fallbacks solely to suppress internal errors.
Report the exact style, input road/state, affected prefab and failing stage. Do not publish an
incomplete road-only or mechanism-only bridge. Clean up partial new output only within that bridge's
owned paths, using existing persistence handling. Do not introduce a parallel registration store,
quarantine system or live-reference repair process.

Error-policy synchronization baseline (2026-10-16): `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`. This removes the obsolete blanket no-throw sentence;
the user's revised error-boundary policy remains authoritative.

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
Revision baseline (2026-10-16): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

## 12. Integration and update scope

Follow the [generator code organization contract](GENERATOR_CODE_CONTRACT.md). Every movable
generator inherits the shared movable generation base, itself derived from `BridgeGeneratorBase`.
Keep one concrete generator file per style, containing only its unique rules and hooks; common
generation lifecycle logic belongs in the shared bases.

Use existing generation/persistence extension points and the simplest implementation that satisfies
this contract. `BridgePreviewComposition` and `BridgePreviewNetMesh` provide useful composition and
preview paths, but neither proves persistent bone-bound mesh export. Trace their dependencies before
reuse; do not promote disposable preview assets into formal saved assets.

Develop genuinely shared machinery on `dev`. Before a style-specific implementation begins, record
its stable style ID and exact English `bridge/<style-slug>` mapping under bridge generation contract section
12. Existing discovery labels do not authorize a new generator or an invented branch mapping.

Deployment follows bridge generation contract section 13. Generation changes remove all mod-created bridges
only of the exact affected styles and their UUID-owned assets. Trace shared-code callers to identify
that set. A new movable style alone does not invalidate fixed bridges. No generation change means
no removal of existing bridges; documentation-only work requires no shutdown, cleanup, build or
deployment. Keep deployment cleanup separate from migration and user-requested deletion.

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

Follow bridge generation contract section 23 for vendor-neutral GPU bulk matrix/image work; never require
CUDA. Generation-time GPU extraction/composition is allowed; persisted bridges require no ongoing
baking or manager callbacks. Independent pure geometry operations
may process immutable triangle chunks with local output buffers and deterministic merges. Native
asset lookup, Unity/ECS operations, render creation and persistence retain their required thread
and graphics context; parallelism does not authorize unsafe native API calls. Do not claim
parallel execution or an end-to-end timing target without measuring the actual current path.

## 13. Acceptance and completion criteria

Follow bridge generation contract section 4's prototype-referenced mesh complexity rule for every
mechanism and converted road LOD. Excessive subdivision or triangle cutting is prohibited;
necessary subdivision and high-detail-derived LOD permissions do not waive this requirement.

Necessary subdivision of merged structural LOD triangles is permitted while preserving original
vertices, materials and interpolated attributes. The user revoked the prohibition on replacement
LODs derived from high-detail geometry on 2026-10-17. Deriving, simplifying or supplementing movable
bridge LODs from the measured prototype's high-detail geometry is permitted under bridge generation
contract section 5. Authored part classification and the prohibition on custom shaders remain unchanged. Marking
approximation follows section 7; historical decal permissions do not authorize independent objects.

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
| Variants | Prototype-width, wider/narrower and supported asymmetric roads; each claimed pedestrian/rail/double-deck mode |
| Persistence | Fresh cold start and existing save with Bridge Builder absent; required game/DLC content retained |
| Interrupted movement | Save/reload while opening, open and closing; return to valid native operation |
| Editing and lifecycle | Supported placement, reconnection, upgrade and demolition preserve native ownership and clear entities correctly |
| Regression | Unaffected fixed styles and their persisted assets remain usable; unsupported combinations fail explicitly |

Humans create and judge in-game samples. Agents must not open/control the game or issue construction
API/request-file operations. Do not run visual-generation unit tests or present compilation,
synthetic arithmetic or a preview image as acceptance. Relevant nonvisual persistence/reference
checks remain applicable where their paths change.

Delivery must state what is measured, implemented, built/deployed and manually accepted separately.
Attach the actual evidence and list remaining checks. A missing sample or untested native cycle
keeps the relevant support claim incomplete. Do not remove section 10's deferred status wholesale
when only one variant has passed.

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
- [Graph compaction](../Movable-Prefab-Graph-Compaction.md): native condition placement; its former decal-child implementation is superseded.
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

For each future repair, record the input, native evidence, root cause, changed generation rule and
verification limits. Update the corresponding normative section and this table together. Keep
style-specific dimensions, atlas coordinates, face lists and measured counts in implementation
evidence or immutable archetype data, rather than prescribing them as universal constants here.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.


## Approved native tiled road material descriptor (2026-10-17)

Historical decision record. For current implementation requirements, use sections 16–23;
conflicting details below are superseded, not alternative implementation choices.

The user explicitly permits a new native object-material descriptor that references existing
tileable road textures to eliminate atlas-repeat geometry cuts. This is a scoped exception to
the existing-material-only rule: do not create or bake textures, introduce custom shaders, modify
source assets, or change structural and marking materials. Keep one descriptor per required
appearance within each bridge owner and share it across LODs. Preview descriptors belong only to
the preview's transient database. Persist formal descriptors with private identities and original
texture references; native cold loading must not depend on Bridge Builder. Unchanged built-in
textures remain references, and external dependencies retain the normal snapshot policy.

Use continuous object-local UVs and the native sampler for whole-texture repetition; do not apply
vertex-wise fractional UVs or cut triangles at repeat boundaries. This exception does not permit
repeating a mixed atlas as though it were a tileable road texture. Other geometry, marking,
sidewalk, LOD and acceptance requirements remain unchanged.

Baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`; rollback reference
`refs/rollback/native-tiled-road-material-20261017`. This permission does not establish visual or
cold-load acceptance.

## Approved GPU composition from existing textures (2026-10-17)

Historical decision record. For current implementation requirements, use sections 16–23;
conflicting details below are superseded, not alternative implementation choices.

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.

## Native separated movable paint revision (2026-10-18)

Historical decision record. For current implementation requirements, use sections 16–23;
conflicting details below are superseded, not alternative implementation choices.

The user now requests separate native markings and prototype-based asphalt with preserved UV scale.
This supersedes the prohibition on independent marking objects only for native decal child renderers
attached to the mechanism's recorded motion connections. Keep each disjoint projector in its own native object-space clipping bounds. The native shader
clips against one box per object: grouping disjoint volumes is prohibited unless a compatible native
per-instance clipping representation is verified. Supply its normal and tangent attributes and bind
each projector to its motion connection. Keep geometry as closed projection volumes; never render those volumes
as road surface triangles. Only projection volumes may be partitioned at physical motion seams;
markings and texture repetition must never cut receiving road geometry.

Remove full-road transparent paint overlays. Reuse the native decal material and measured native
paint patches. Asphalt uses a repeatable unit extracted from the prototype atlas, including its
albedo, normal, mask and control channels. Preserve the measured per-metre UV gradients, direction
and phase on both leaves. BC block alignment may round the extracted pixel dimensions up; do not
stretch a low-resolution whole-span atlas over the road. Other appearance domains retain their
existing path. Keep native asset persistence and owner-scoped rollback/cleanup for the complete graph.
Preview, placement, opening/closing, LOD transitions and cold loading require human game verification;
a successful build does not establish visual acceptance.

Revision baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.

## Preserve original road skinning (2026-10-18)

Historical decision record. For current implementation requirements, use sections 16–23;
conflicting details below are superseded, not alternative implementation choices.

The user revoked moving the road onto marking child transforms. Keep Draw01 road geometry in its original procedural skeleton and skinning path, with all three LODs, UVs and materials retained. Markings must adapt to the original road bones; never move road receivers into history-driven child objects to compensate for decal lag. Native independent persistence remains mandatory, without runtime synchronization patches. Structure/underside surfaces use the original native descriptors, texture references and VT resources without receiver-only material copies or descriptor rewrites. Single-sided skinned paint replaces the old projectors; disabling structural decal reception is no longer required. The earlier rigid road child implementation is withdrawn. A native decal shader without procedural skinning must not be treated as supporting bone-bound paint merely by assigning indices.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.

## Authorized native skinned paint exception (2026-10-18)

Historical decision record. For current implementation requirements, use sections 16–23;
conflicting details below are superseded, not alternative implementation choices.

The user explicitly permits native skinned paint surfaces and minimum anti-coplanar clearance. Draw01 keeps its original road skeleton and receiver topology. Marking polygons alone may be partitioned at motion seams and bound with the road bone indices/bind poses. Use a native procedural-skinning material, single-sided paint, and 0.001 m clearance; no custom shader or runtime synchronization patch. Paint is an additional mesh on the same native mechanism object, with matching StackProperties, authored bounds and object transform, not a history-driven child projector. Preserve source marking layout/color; do not cut the road. A compact paint mesh remains present across road LOD transitions. This supersedes earlier prohibitions on overlapping marking surfaces and the native child-projector requirement for this implementation. Preview, motion, far views and cold loading still require verification.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.


## 16. Accepted generation pipeline and source map

Revision baseline: branch `dev`, HEAD `d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
The accepted implementation includes working-tree changes beyond that commit. The original
contract files were backed up before this revision; do not treat HEAD alone as the accepted code.
On 2026-10-18 the user reported acceptance after removal of `Structure No Decals` copies.

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
   may require clipping; texture periods and paint edges must not drive road subdivision.
5. Build the appearance resources in section 17 and retain all original structural surface slots.
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
| Marking material persistence | `CreateSkinnedPaintSurface`, `ReadNativePaintColor` in `TowerFactory.MovablePaint.cs` |
| Final attachment | `Draw01ObjectAssembly` and shared `TowerFactory` |

These are source navigation references, not permission to copy Draw01 dimensions, atlas patches,
bone indices or LOD counts into other styles. Retain the shared generator architecture of section 2
and the generator code contract.

## 17. Material families, UV density and texture ownership

Keep these four material paths distinct. A global switch to VT or a global material replacement
cannot implement them correctly.

| Family | Current generation/persistence treatment | Required invariant |
| --- | --- | --- |
| Carried prototype structure, underside, towers and supplements | Reference original native Surface assets in original slot order | Do not copy/rewrite descriptors just to disable decals; preserve native textures, VT identity, UVs and channels |
| Converted repeating asphalt | Extract a verified unpainted prototype atlas unit; save native non-VT textures and an object-compatible native Surface | Continuous UV repetition preserves measured prototype gradients and physical scale |
| Converted repeating metal grating | Extract the native grating unit with its channels; use native `DefaultCutoutDoubleSided`, non-VT Repeat | Preserve alpha/cutout holes and two-sided behavior; never turn the sidewalk into opaque asphalt |
| Other converted local road appearances | GPU composition of existing native inputs into bounded shared atlas pages; native VT persistence where currently used | Preserve fixed-cap/material domains, channel semantics, padding, mips and per-LOD UV assignment |

Paint is a fifth, separate family defined in section 18. Do not bake it into the asphalt or into
structural underside textures. `Draw01ClosedRoad` currently passes an empty marking-region list
to `MovableSurfaceAtlas.Compose` and attaches paint separately afterward.

For repeating units, derive UV axes, orientation and origin from measured prototype object-space
patches. The current extractor sizes the unit from the source atlas rectangle, rounded to BC7
block alignment; it does not stretch a full-span baked image. Record actual dimensions and density
for each generated material. A cropped unit need not equal the dimensions of the full source atlas.
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

Keep Repeat units non-VT in the current native object loading path: its VT sampler uses Clamp.
For remaining VT composites use native configuration, BC7 formats and native ZSTD compression
setting; never modify global compression configuration to speed one generation. Complete the
matching mid-mip cache (currently three levels), register/refresh VT resources and preserve the
whole dependency chain before publishing the render. A mismatch between requested and stored
mid mips is a generation failure, not a reason to suppress the error.

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

The current paint color source is the native No Parking surface. The implementation persists
its actual decoded color texture at the source resolution (2048 x 2048 in the measured source),
then samples measured white/yellow locations with constant UVs. Polygon outlines supply the
marking shape. It also writes a neutral 1 x 1 control texture and native non-VT Surface. This is
not a textured dash atlas baked across the full bridge, nor complete native decal shading.
Measured source identifiers/texel coordinates live in implementation data, not universal rules.
Complex symbols, additional colors, fades or infoview semantics require explicit implementation
and acceptance before claiming support; grouping metadata alone does not implement their shading.

A native VT surface's original Texture CID may be provenance only and unavailable as a real
.Texture asset. Resolve the actual VTSurface color layer or preprocessed VTTexture backing,
decode/read it through the existing GPU path, and persist the owned texture before referencing
it. Do not serialize a missing original color CID. Restore graphics state, balance texture leases
and destroy only privately created resources on success and failure.

## 19. Common motion, stacking, visibility and underside

The road retains the original mechanism skeleton and skinning path. Paint adapts to that road;
never move road geometry onto marking child bones or history-driven transforms.

The paint render must copy the original road's `ProceduralAnimationProperties`, use identical
bone numbering/rest transforms/bind poses and match its `ObjectMeshInfo` position and rotation.
Copy `StackProperties` when present and preserve the same authored root bounds. Matching bones
alone is insufficient when the native object applies stack placement or scale. Do not substitute
swept animation bounds for authored stacking bounds; preserve native animated culling behavior.

Why child projectors are withdrawn: the inspected native update order let relative child objects
read previous-frame BoneHistory while the skinned road used the current frame. The resulting
misalignment occurred during movement and disappeared on pause. Increasing projection depth or
moving the road into that delayed path does not meet the requirement. No Bridge Builder update
loop, Harmony synchronization patch or custom shader may be required after generation.

Keep paint single-sided and above its upper receiver under the same transform. Do not disable
decal reception by cloning every structural Surface; that obsolete workaround preceded the
large-scale structural material corruption report. Restoring original structural references was
the change subsequently accepted by the user. This acceptance does not establish a universal
engine defect in all Surface copying, and does not authorize editing built-in resources.

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
subdivision to encode paint outlines or texture periods. Any LOD supplement needs source geometry,
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

Preserve source-scale repeat units instead of baking the entire span. Share composed resources
across LODs within their owner; reuse original structure assets. Avoid one object/material per dash,
unnecessary native texture decoding, repeated VT compilation and redundant registration wrappers.
Bulk image work uses the existing vendor-neutral GPU path; native Unity/database calls retain
thread affinity. Do not reduce native texture quality or global compression settings to meet a target.

Record sampling, road conversion, GPU composition/readback, texture and VT writing, mid-mip
completion, paint generation, dependency persistence and registration timings separately. Include
selected road/style, cold versus warm generation, output texture dimensions, Repeat/VT treatment,
material and triangle counts by LOD, and native dependency errors. Count total time without double
counting nested stages. The user requested seconds-scale creation; do not assert that target is
met without a new measurement. Stale log labels such as 'paint uses native decals' are not proof
that the current renderer uses projectors; trace the actual `AttachMovablePaint` path.

## 23. Acceptance record and contributor checklist

User acceptance on 2026-10-18 follows the restoration of original structural materials after the
massive white/blue corruption report. Record this as acceptance of the latest Draw01 visual repair,
not blanket certification of every style/input, persistence scenario or performance target.

Before declaring a newly added mechanism complete:

- Compare closed, opening, fully open and closing states at near/far distances. Run motion as well
  as pause: pause alone masked the historical transform lag. Inspect both leaves and the underside.
- Check paint placement, backface absence, 1 mm clearance, distance visibility and no deck slicing.
  Confirm grating holes, caps, towers and structural materials retain prototype appearance.
- Measure native versus generated UV density in both axes and verify material slots at every LOD.
- Exercise preview and newly placed output separately, then save/restart with the manager absent
  and required DLC present. Verify all owned references and VT mip dependencies.
- Complete the traffic, shipping, supported upgrades/asymmetry and lifecycle checks of section 13.
- Report actual face/material/texture counts and generation timings, distinguishing sample results
  from universal rules. Compilation, hash verification and a static preview are not visual acceptance.

Do not use game UI automation or construction request files. Human users create and inspect game
samples under the project rules. If a check is pending, state its precise scope without undoing
already confirmed acceptance or claiming unrelated tests have passed.

### Grouped generated LOD directories (2026-10-18)

Historical layout; the owner-directory layout is now implemented as specified in persistence policy
sections 6–7. All owned LOD Prefabs share `Prefabs/`; native identities remain separate.

Generated Prefabs with the terminal naming suffix ` LOD<number>` share the main component's
ImportedData directory. For example, `Fixed Base 0 LOD1.Prefab` is stored alongside
`Fixed Base 0.Prefab` in the latter's directory. Each remains a separate native asset with its
own CID and unchanged LodProperties references. Geometry paths and material sharing are unchanged.
`BridgePrefabPersistence` is the common creation and migration path authority; the shared native
writer remains unchanged. Existing assets retain their identities; self-check layout migration
backs up and moves Prefabs plus CID sidecars without rewriting payloads and requests restart.
This is a storage-layout change, not geometry regeneration or a reason to remove healthy bridges.


### Preserve partitioned road triangles (2026-10-18)

The user requested removal of coplanar road coalescing. Road preparation, atlas input/output and
final render assembly now retain each partitioned input triangle rather than merging adjacent
faces and fan-triangulating the result. Texture charts may still share appearance resources;
chart sharing must not merge geometry. Motion/appearance-domain clipping, measured prototype
replacement fragments and separate paint polygon triangulation remain in place. This supersedes
earlier road-compaction descriptions; do not restore that optimization without a new user decision.
Actual post-change face counts and performance must be measured on newly generated assets.


### Approved next persistence layout (2026-10-18)

Follow [persistence policy sections 6–7](../Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
for UUID-contained Prefabs, Geometry, Appearance, Locales and Dependencies, exact filename-prefix
removal, paired CID migration and on-demand directories. This supersedes the prior grouped-LOD
folder design as a target specification, not as a statement of current implementation. Internal
names and references remain unchanged. VT/mid-mip placement must be verified before promising
single-directory packaging. No code or live asset migration accompanies this documentation update.


Persistence implementation revision (2026-10-19): generated appearance, VT and mid-mip resources
now reside below the owner UUID. See Bridge-Asset-Persistence.md sections 6–7 for the shared
creation/migration mapping and native streaming discovery evidence. This storage change does not
change geometry, bones, materials, marking depth or the accepted motion behavior. Cold-load
verification of the new directory layout remains pending.
