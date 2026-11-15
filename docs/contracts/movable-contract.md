# Movable bridge contract

Required companion: [Movable Spec](../specs/movable-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not change requirement strength, existing specialist exceptions or explicit user authorizations. Specifications and contracts are equally normative; the split introduces no new precedence hierarchy.

<a id="movable-movable-bridge-generation-contract"></a>

<a id="movable-contents"></a>

## Contents

- [No special road-width adaptation](../specs/movable-spec.md#movable-no-special-road-width-adaptation)
- [1. Authority, scope and implementation status](movable-contract.md#movable-1-authority-scope-and-implementation-status)
- [2. Required architecture](movable-contract.md#movable-2-required-architecture)
- [3. Archetype evidence and immutable mechanism data](../specs/movable-spec.md#movable-3-archetype-evidence-and-immutable-mechanism-data)
- [Source-face coverage and prevention of missing structure](../specs/movable-spec.md#movable-source-face-coverage-and-prevention-of-missing-structure)
- [4. Resolve the selected road before mapping](../specs/movable-spec.md#movable-4-resolve-the-selected-road-before-mapping)
- [5. Per-archetype mapping and the six-lane example](../specs/movable-spec.md#movable-5-per-archetype-mapping-and-the-six-lane-example)
- [6. Network conversion and placement](../specs/movable-spec.md#movable-6-network-conversion-and-placement)
- [7. Convert the closed-pose road into persistent object geometry](../specs/movable-spec.md#movable-7-convert-the-closed-pose-road-into-persistent-object-geometry)
- [8. Skeletons, animation and attachments](../specs/movable-spec.md#movable-8-skeletons-animation-and-attachments)
- [9. Waterway and traffic compatibility](../specs/movable-spec.md#movable-9-waterway-and-traffic-compatibility)
- [10. Lift, rail, pedestrian and double-deck variants](../specs/movable-spec.md#movable-10-lift-rail-pedestrian-and-double-deck-variants)
- [11. Persistence, failures and compatibility](persistence-contract.md#movable-11-persistence-failures-and-compatibility)
- [12. Integration and update scope](movable-contract.md#movable-12-integration-and-update-scope)
- [13. Acceptance and completion criteria](../specs/movable-spec.md#movable-13-acceptance-and-completion-criteria)
- [14. Evidence references and maintenance](movable-contract.md#movable-14-evidence-references-and-maintenance)
- [15. Observed failures and mandatory prevention](../specs/movable-spec.md#movable-15-observed-failures-and-mandatory-prevention)
- [16. Generation pipeline and source map](../specs/movable-spec.md#movable-16-generation-pipeline-and-source-map)
- [17. Material families, UV density and texture ownership](../specs/movable-spec.md#movable-17-material-families-uv-density-and-texture-ownership)
- [Offline repeat materials instead of period seams](../specs/movable-spec.md#movable-offline-repeat-materials-instead-of-period-seams)
- [18. Marking layout, mesh and native material](../specs/movable-spec.md#movable-18-marking-layout-mesh-and-native-material)
- [19. Common motion, stacking, visibility and underside](../specs/movable-spec.md#movable-19-common-motion-stacking-visibility-and-underside)
- [20. LOD, topology and prototype-specific extension](../specs/movable-spec.md#movable-20-lod-topology-and-prototype-specific-extension)
- [21. Preview, native persistence and failures](movable-contract.md#movable-21-preview-native-persistence-and-failures)
- [22. Performance and diagnostic requirements](../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements)
- [23. Acceptance and contributor checklist](movable-contract.md#movable-23-acceptance-and-contributor-checklist)
- [24. Track geometry, overhead supply and material selection](../specs/movable-spec.md#movable-24-track-geometry-overhead-supply-and-material-selection)

<a id="movable-1-authority-scope-and-implementation-status"></a>

## Authority, scope and implementation status

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-1-authority-scope-and-implementation-status-part-2).

The [implementation policy](generation-contract.md#generation-current-implementation-policy)
removes process gates on completing the remaining generators. Hardcoding is explicitly retained:
extract from real prototypes during development, then use immutable construction data without
requiring the player's identical original prototype files. Compact profiles/recipes are permitted;
runtime rediscovery of prototype parts is not. Supply required resource inputs through the permitted
packaged/persisted resource graph. A local developer file path is never a deployment dependency.

Per-style branches and exhaustive audit-before-implementation are not admission gates. Route actual
working implementations incrementally and record remaining acceptance work honestly. Existing
appearance, skeleton, persistence and localization outcomes remain mandatory.

See [Scope and authority](material-contract.md#material-scope-and-authority).

[The complete movable material and persistence scope](reading-map.md#movable-sections-16-24) defines the generation, material, marking and track requirements.
The material generation contract governs original non-road reuse, source-density road tiling, all-LOD marking removal,
lane-aligned wear, offline preparation,
bounded runtime conversion and informational generated-owner material measurements and complete package-size reporting.
Implementation evidence and human acceptance remain specific to the tested style and input.

This is the normative supplement to [Bridge generation reading scope](reading-map.md#bridge-generation-contract) for bascule/draw
bridges, vertical lift bridges and any subsequently admitted movable design. Read it together with
the root [AGENTS.md](reading-map.md#repository), the complete project and bridge generation contracts and applicable directory rules.
The bridge generation contract remains authoritative for geometry, source selection, branches, persistence,
pricing, cleanup and acceptance. This supplement does not enable an unsupported generator.

<a id="movable-implementation-status-and-evidence"></a>

## Implementation status and evidence

Evidence applies only to its tested build, style and input; it does not establish untested traffic, shipping, road/LOD or cold-load behavior. Registered catalogue entries and completed audits do not establish completed generators. Completion status must never disable a registered style or add unfinished-style notices under the UI specification.

Track each mechanism’s implementation and acceptance by its stable style ID.
Distinguish measured evidence, code, deployment and human acceptance. Completed audits and
backups do not establish completed generation support.

<a id="movable-2-required-architecture"></a>

## Required architecture

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
remains a native prerequisite. Bridge Builder registration does not replace required DLC content.

### Purpose and boundaries

Selected-road conversion needs native endpoint and intersection geometry before node lanes,
markings and network attachments can be resolved. `MovableNetworkContext` owns a disposable ECS
world for this calculation. It does not create a city bridge, replace native geometry algorithms,
run city simulation or introduce a persisted manager component. A straight-edge preview is not
a substitute for calculated node geometry.

The [generation contract](generation-contract.md),
[movable specification](../specs/movable-spec.md) and
[persistence contract](persistence-contract.md) govern required behavior.
The [historical development record](../archive/audits/movable-network-geometry-development-record.md)
contains native field counts, concrete bridge measurements, source hashes, dated deployments and
verification limits. Those records do not establish current visual, motion or cold-load acceptance.

### Context construction and ownership

The context accepts explicit measured nodes, edges, mechanisms, middle connections and parent
relationships. It does not infer hinges, impose a universal topology or classify parts by style
name. Nodes carry source prefab, pose, seed and optional elevation; edges carry endpoint indices,
curve, seed, elevation and optional native upgrades. Mechanisms carry the generated object,
owner relationship, original placement, owner transform, native signal state and seed.

The caller supplies initialized terrain, completes its writer, and supplies theme, traffic
handedness, random seed and building configuration. Terrain arrays remain borrowed. The context
imports prefabs, creates connectivity and ownership, stages mechanisms, imports theme/configuration
references, then calculates the prepared network. Deferred construction stores that calculation
until `CompleteCalculationAsync`; immediate construction completes it through the synchronous
wrapper. Construction failure disposes the world. Successful consumers dispose the context after
extraction. Private entities, archetypes, geometry components and job buffers never enter saved assets.

The prepared context is network-only: building/area owners and their city connection work are
outside this calculation. Mechanism SubObject/PointOfInterest relationships exist before signals
are initialized. Flat terrain, fabricated mechanism relationships or guessed endpoint geometry
are not substitutes for supplied inputs.

### Prefab and mechanism staging

`MovableNetworkPrefabs` retains original prefab provenance while importing explicit native records.
A source is memoized before recursive imports so cycles retain identity. Entity references are
remapped to private entities; archetypes are rebuilt in the private world. Recursive imports finish
before target buffers are acquired, avoiding invalidation across structural changes. Optional null
references are not traversed. Missing external dependencies fail the operation; a partial context
is discarded, never reused. Original prefabs, names, CIDs, materials and fees are not rewritten.

`StageMechanism` accepts the assembled `StaticObjectPrefab`. Generated render graphs and LOD
metadata are staged in the same private world and registered in its PrefabSystem. Active native
component initializers provide placement, pillar/standing-leg, movement, network and fee data.
The native object-geometry initializer consumes that generated render metadata. Source mesh bounds
cannot describe widened or replaced geometry. All initialization writes remain private, including
LOD entities; source-world prefab indices are not treated as private registry indices.

Render staging follows native bounds/count, LOD ordering, bias, layer, stack, skeleton, decal and
base-flag semantics, with stable ordering for equal counts and native operation order. It does not
start a second live rendering/batching pipeline. Full ObjectInitializeSystem updates on the mixed
network world are not used. Renderer batches, material indices, complete child-object requirement
initialization and saved-asset publication remain separate responsibilities.

Mechanism placement applies native alignment and the standalone-pillar top anchor using initialized
object geometry and placement data. The animation target follows native traffic-light behavior.
`RoadToMechanism` applies inverse rotation and position to convert origin-relative road coordinates
into the anchored mechanism frame; a universal height correction cannot replace that transform.

### Geometry and appearance handoff

`CompileSurface` selects actual edge, start-node or end-node geometry and obtains native composition
parameters. Each visible occurrence is packed through the native mesh helper and evaluated against
those parameters, not preview-only straight bounds. Native source surfaces and source-face identity
remain attached to explicit triangles. Callers select the visible closed-road composition before
suppressing replaced moving-network layers. Invisible contributions remain empty.

The non-terrain evaluator retains native curve selectors, synchronization groups and breakpoint
ordering, Bezier position/derivative evaluation, endpoint skew, tangent-frame fallbacks and mapping
distance semantics. Geometry and UV mapping coordinates are not interchangeable. The resulting
object-space geometry has no private Entity references. Unsupported terrain-dependent sampling and
surface-template paths report operation failures; unrelated height values do not replace GPU
sampling. Source-instruction correspondence is preserved in the audit.

`CompileRoad` combines ordered surface contributions in one common closed-pose frame, adjusting
material and occurrence indices while retaining source-triangle identity. Composition records,
object rules and occurrence boundaries remain parallel. Diagnostic strings do not classify geometry.
Requested lane owners are extracted once, with native hidden/deleted/master rejection and provenance.
Non-geometry traffic lanes remain valid; unexpected render payload is not silently discarded.

Lane compilation uses the complete combined receiver to avoid edge/node seams. Curved decals retain
their explicit representation; projector boxes are not substituted for unresolved appearance work.
The result hands receiver triangles, surfaces and lane geometry to `MovableRoadAssets.Level` and the
existing partition, skinning, appearance and independent owned-asset pipeline. A successful context
alone does not establish full per-style generator or decal support.

### Attachment extraction

`EdgeObjects` and `NodeObjects` use the same origin as surface/lane compilation and original-world
theme provenance. Native secondary-object seeds, selection/group operations and node streetlight
flags are retained. Edge extraction includes composition rules, track-endpoint objects and utility
junction arbitration. Lane curves and surface offsets determine transforms; membership and priorities
come from native lane data rather than bridge-name or coordinate heuristics.

Placements are resolved before context disposal and bound to measured mechanism partitions. Callers
suppress duplicate stationary visual producers where required. Original permitted prefab references
may leave the context; temporary entity identities may not. All per-style measured arrangement,
render assembly, motion, traffic, shipping, persistence and near/far acceptance obligations remain.

### Implementation references

- [Context and ownership](../../src/BridgeBuilder/Bridges/Common/Movable/MovableNetworkContext.cs)
- [Native calculation sequencing](../../src/BridgeBuilder/Bridges/Common/Movable/MovableNetworkLanes.cs)
- [Surface compilation](../../src/BridgeBuilder/Bridges/Common/Movable/MovableNetworkContext.Surface.cs)
- [Prefab import](../../src/BridgeBuilder/Bridges/Common/Movable/MovableNetworkPrefabs.cs)
- [Generated object geometry](../../src/BridgeBuilder/Bridges/Common/Movable/MovableNetworkPrefabs.Geometry.cs)

<a id="movable-non-moving-parts-follow-the-general-bridge-generation-contract"></a>

## Non-moving parts follow the general bridge generation contract

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-non-moving-parts-follow-the-general-bridge-generation-contract-part-2).

Every non-moving part of a movable bridge must follow the
[general bridge generation contract](reading-map.md#bridge-generation-contract). This includes ordinary
approach spans, connecting edges, fixed nodes, deck-adjacent bases, side structures, pillars and
their LODs. Movable conversion is not an exemption from ordinary bridge structure generation.

<a id="movable-3-archetype-evidence-and-immutable-mechanism-data-part-2"></a>

## Variant admission evidence

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-3-archetype-evidence-and-immutable-mechanism-data), [Movable Spec](../specs/movable-spec.md#movable-3-archetype-evidence-and-immutable-mechanism-data-part-3).

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

<a id="movable-native-serialization-and-reader-compatibility-part-2"></a>

## Saved-file and cold-load verification

Related requirements: [Persistence Spec](../specs/persistence-spec.md#movable-native-serialization-and-reader-compatibility), [Persistence Spec](../specs/persistence-spec.md#movable-native-serialization-and-reader-compatibility-part-3).

Verify real saved files and the full reference graph through the actual layout/migration readers;
check byte preservation and idempotence. Separately verify cold loading in the game. Parser and
filesystem success are not proof of native loading. Report owned Prefabs, external snapshots,
Geometry, locales and sidecars separately; predicted reductions are not measured saved counts.

<a id="movable-failure-handling-and-compatibility"></a>

## Failure handling and compatibility

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-failure-handling-and-compatibility-part-2).

Follow the project's error-boundary policy: contain corrupt external Prefabs and external
API failures; raise explicit ERROR for data/API invariant violations, not structural or appearance
quality outcomes, using exceptions where
appropriate. Do not introduce special cases or silent fallbacks solely to suppress internal errors.
Report the exact style, input road/state, affected prefab and failing stage. Do not publish an
incomplete road-only or mechanism-only bridge. Clean up partial new output only within that bridge's
owned paths, using existing persistence handling. Do not introduce a parallel registration store,
quarantine system or live-reference repair process.

<a id="movable-12-integration-and-update-scope"></a>

## Integration and update scope

Follow the [generator code organization contract](reading-map.md#generator-code-contract). Every movable
generator inherits the shared movable generation base, itself derived from `BridgeGeneratorBase`.
Keep one concrete generator file per style, containing only its unique rules and hooks; common
generation lifecycle logic belongs in the shared bases.

Use existing generation/persistence extension points and the simplest implementation that satisfies
this contract. `BridgePreviewComposition` and `BridgePreviewNetMesh` provide useful composition and
preview paths, but neither proves persistent bone-bound mesh export. Trace their dependencies before
reuse; do not promote disposable preview assets into formal saved assets.

Use a suitable working branch for shared machinery and style-specific data. Per-style branches are
optional under [bridge generation contract section 12](development-contract.md#generation-12-branches-organize-work-without-blocking-integration). Preserve stable style IDs and keep each variant's hardcoded rules distinct.

Deployment follows [bridge generation contract section 13](development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets): build/deploy coherent increments, back up installed originals and
verify hashes. Preserve healthy assets. Recover only specifically incompatible owned assets; adding
a new movable style does not invalidate fixed bridges. Documentation-only changes require no deployment.

<a id="movable-stage-timing-and-parallel-execution"></a>

## Stage timing and parallel execution

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-stage-timing-and-parallel-execution-part-2).

Measure native sampling, geometric conversion, paint clipping, graph assembly, registration,
serialization, dependency copying and publication separately. Include input identity, LOD/triangle
counts, graph counts, cache state and total elapsed time. Do not sum nested stage timings twice.
Performance work must preserve required appearance and native graph semantics. Compare full dependency footprints on the same basis with the prototype.

<a id="movable-13-acceptance-and-completion-criteria-part-2"></a>

## Per-variant acceptance evidence

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-13-acceptance-and-completion-criteria).

Record results per variant and supported input class. The following checks are requirements for
claiming support, not claims that they have already passed:

| Area | Required evidence |
| --- | --- |
| Provenance and anatomy | Complete archetype/generated comparison, measured skeleton/part mapping and versioned extraction record |
| Road fidelity | Selected-state lane positions/directions, sidewalks, markings and supported upgrades match the generated closed surface |
| Width | Real archetype and human-created generated sample; [section 14](movable-contract.md#movable-14-evidence-references-and-maintenance)'s exact binary32 audit on matching boundaries |
| Geometry and motion | Closed, opening, open and closing views; no seam tearing, floating attachments or ship/structure intersection |
| LOD and lighting | Near/far views throughout motion, nighttime lights and correct culling/shadows |
| Traffic | Clear-before-open, no new entry while unavailable, safe resumption, queue/repeated-request recovery |
| Shipping | Clearance-sufficient vessels and vessels needing opening, repeated vessels and both supported travel directions |
| Variants | Prototype-width, wider and supported asymmetric roads; each claimed pedestrian/rail/double-deck mode. No dedicated narrow-road adaptation. |
| Persistence | Fresh cold start and existing save with Bridge Builder enabled for startup material registration; required game/DLC content retained |
| Interrupted movement | Save/reload while opening, open and closing; return to valid native operation |
| Editing and lifecycle | Supported placement, reconnection, upgrade and demolition preserve native ownership and clear entities correctly |
| Regression | Unaffected fixed styles and their persisted assets remain usable; unusual combinations are not structurally gated; actual resource/API failures remain explicit |

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

<a id="movable-14-evidence-references-and-maintenance"></a>

## Evidence references and maintenance

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-14-evidence-references-and-maintenance-part-2), [Movable Contract](movable-contract.md#movable-14-evidence-references-and-maintenance-part-3).

Use these documents alongside this contract:

- [Bridge model](../specs/generation-spec.md): native component roles and current support boundaries.
- [Contributor workflow](../guides/adding-bridge-styles-with-coding-agents.md): implementation and submission.
- [Road width calculation](../specs/generation-spec.md): selected-state measurement and boundary distinctions.
- [Bridge generation contract](reading-map.md#bridge-generation-contract): mandatory geometry, ownership, lifecycle and acceptance rules.

- [Persistence policy](reading-map.md#bridge-asset-persistence): normative asset/reference ownership.
- [Preview specification](reading-map.md#bridge-preview-specification): normative composition and framing.

<a id="movable-14-evidence-references-and-maintenance-part-3"></a>

## Maintaining measured source evidence

Related requirements: [Movable Contract](movable-contract.md#movable-14-evidence-references-and-maintenance), [Movable Spec](../specs/movable-spec.md#movable-14-evidence-references-and-maintenance-part-2).

Historical anatomy exports are useful comparisons but cannot establish current field values where
they are truncated or version-mismatched. Future changes must update the measured record and affected
acceptance results. Requirements and implemented fixes do not by themselves establish complete
in-game acceptance; record outstanding checks per style and build.

<a id="movable-15-observed-failures-and-mandatory-prevention-part-2"></a>

## Repair evidence requirements

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-15-observed-failures-and-mandatory-prevention).

For each future repair, record the input, native evidence, root cause, changed generation rule and
verification limits. Update the corresponding normative section and the [failure audit](../archive/bridge-evidence/generation-source-observations.md#movable-generation-failure-observations) together. Keep
style-specific dimensions, atlas coordinates, face lists and measured counts in implementation
evidence or immutable archetype data, rather than prescribing them as universal constants here.

<a id="movable-21-preview-native-persistence-and-failures"></a>

## Preview, native persistence and failures

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

<a id="movable-22-performance-and-diagnostic-requirements-part-2"></a>

## Structural timing evidence

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements), [Movable Spec](../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements-part-3), [Movable Contract](movable-contract.md#movable-22-performance-and-diagnostic-requirements-part-4).

Log `snapshotMs`, `parallelPlanMs` and `totalMs` from `MovableRenderAssembly` alongside the
other generation stages. The structural subtotal excludes later native render-graph creation;
it is not total preview or permanent-generation time and must not be counted twice.

<a id="movable-22-performance-and-diagnostic-requirements-part-4"></a>

## Material and persistence timing evidence

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements), [Movable Contract](movable-contract.md#movable-22-performance-and-diagnostic-requirements-part-2), [Movable Spec](../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements-part-3).

Record sampling, road conversion, GPU composition/readback, texture and VT writing, mid-mip
completion, paint generation, dependency persistence and registration timings separately. Include
selected road/style, cold versus warm generation, output texture dimensions, Repeat/VT treatment,
material and triangle counts by LOD, and native dependency errors. Count total time without double
counting nested stages. The user requested seconds-scale creation; do not assert that target is
met without a new measurement. Stale log labels such as 'paint uses native decals' are not proof
that the current renderer uses projectors; trace the actual `AttachMovablePaint` path.

<a id="movable-23-acceptance-and-contributor-checklist"></a>

## Acceptance and contributor checklist

Acceptance is specific to a tested style, input, build and scenario; it must not be
generalized to every bridge, persistence scenario or performance target.

Before declaring a newly added mechanism complete:

- Apply [source-face coverage](../specs/movable-spec.md#movable-source-face-coverage-and-prevention-of-missing-structure) to retained, replaced and intentionally omitted faces through the final native path; test independent renderer LOD selections, not only matching LOD indices.

- Compare closed, opening, fully open and closing states at near/far distances. Run motion as well
  as pause: pause alone masked the historical transform lag. Inspect both leaves and the underside.
- Check paint placement, backface absence, 1 mm clearance, distance visibility and no deck slicing.
  Confirm grating holes, caps, towers and structural materials retain prototype appearance.
- Measure native versus generated UV density in both axes and verify material slots at every LOD.
  Audit verified non-road or unsupported-input finite/stretch uses under [material contract section 2](../specs/material-spec.md#material-2-preserve-source-resolution-and-pattern-scale) separately, reporting
  their allowed density change and retained source UVs rather than a density-preserving pass.
- Exercise preview and newly placed output separately, then save/restart with Bridge Builder enabled
  and required DLC present. Verify all owned references and VT mip dependencies.
- Complete the traffic, shipping, supported upgrades/asymmetry and lifecycle checks of [section 13](../specs/movable-spec.md#movable-13-acceptance-and-completion-criteria).
- Report actual face/material/texture counts and generation timings, distinguishing sample results
  from universal rules. Compilation, hash verification and a static preview are not visual acceptance.

Do not use game UI automation or construction request files. Human users create and inspect game
samples under the project rules. If a check is pending, state its precise scope without undoing
already confirmed acceptance or claiming unrelated tests have passed.

<a id="movable-244-acceptance-coverage"></a>

## Acceptance coverage

Acceptance must cover railway, subway and tram inputs as applicable; an unsupported-surface-type
moving section beside its stationary abutment; a supported-surface-type control; near/far LODs;
closed/open poses; native wire contacts and symmetric end supports; separate decks;
and cold loading of persisted assets. Existing bridges do not acquire new geometry
or materials merely by updating the DLL; regenerate a sample for these checks without
deleting healthy saved assets. Do not report pending behavior as fixed from a build.

All user-visible implementation text must support every UI language under AGENTS.md.

<a id="movable-runtime-movable-runtime-invariants"></a>

## Movable runtime invariants

Selected-road attachments use the [common measured-gap rule](../specs/movable-spec.md#movable-attachment-anchors-in-measured-gaps):
omit every kind of attachment at an unsupported gap anchor, preserve supported moving/fixed
mounts, and keep external native approach ownership separate. No type/name/CID gap exceptions.

See [Packaged textures belong to road running surfaces](../specs/material-spec.md#material-packaged-textures-belong-to-road-running-surfaces).

These runtime requirements supplement the [movable bridge contract](reading-map.md#movable-bridges-contract)
and [material generation contract](reading-map.md#material-generation-contract). Read their detailed rules
for source independence, geometry, native rendering and per-owner persistence.

<a id="movable-runtime-runtime-invariants"></a>

## Runtime invariants

Related requirements: [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-3), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-5), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-7), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-9), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-11).

See [3. Archetype evidence and immutable mechanism data](../specs/movable-spec.md#movable-3-archetype-evidence-and-immutable-mechanism-data).

<a id="movable-runtime-runtime-invariants-part-3"></a>

## Material batch and scope obligations

Related requirements: [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-5), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-7), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-9), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-11).

See [material batches](../specs/movable-spec.md#movable-material-selection-projection-and-render-batches).

See [Scope and authority](material-contract.md#material-scope-and-authority).

See [Preserve original UV projection and mapping mode](../specs/generation-spec.md#generation-preserve-original-uv-projection-and-mapping-mode).

<a id="movable-runtime-runtime-invariants-part-5"></a>

## Non-road material reuse obligations

Related requirements: [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-3), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-7), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-9), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-11).

For movable bridges, reuse original non-road materials by default. Only evidenced repeat
domains that would otherwise require new period-wrap seams may use the narrow offline
source-atlas derivative in [material contract section 3](../specs/material-spec.md#material-3-tile-repeatable-materials-at-their-original-density); do not add those seams. Keep source
assets unchanged and give derived resources independent identities. Road composition/precomposition (offline material baking outside the game) has
two necessary correspondence conditions: the actual receiver appearance type is verified and
the measured target prototype part has a matching material. Use that part's appearance, not a
bridge-wide type override. Follow [physical part correspondence](../specs/material-spec.md#material-physical-prototype-part-correspondence). Test asphalt, sidewalk surfacing and wheel-track/rut effects independently
per prototype and physical domain. Other road types reuse compatible input materials or use the corresponding native
animated-object road material under [material contract section 4](../specs/material-spec.md#material-4-persist-constant-reusable-materials-before-runtime); this replacement must preserve complete network-layer blending and dynamic worldspace projection, together with the actual
appearance category, sampling density and tiling/stretch behavior. Supported road types instead require fixed-density tiling and all-LOD marking removal;
non-road source modes remain unchanged. This is a generation rule, not a reverse-engineering finding or proof that the
current implementation complies. Road processing must not mutate a shared structural Surface.

<a id="movable-runtime-runtime-invariants-part-7"></a>

## Track material conversion authority

Related requirements: [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-3), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-5), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-9), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-11).

See [Native track material conversion](../specs/movable-spec.md#movable-native-track-material-conversion).

<a id="movable-runtime-runtime-invariants-part-9"></a>

## Native pricing authority

Related requirements: [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-3), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-5), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-7), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-11).

Follow the native-pricing policy in [bridge generation contract section 16](../specs/generation-spec.md#generation-16-preserve-native-construction-and-upkeep-pricing), including its restrictions
on legacy conversion and fee restoration. The complete policy remains in the contract.

<a id="movable-runtime-runtime-invariants-part-11"></a>

## Cross-codebase review and delivery obligations

Related requirements: [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-2), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-3), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-4), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-5), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-6), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-7), [Movable Spec](../specs/movable-spec.md#movable-runtime-runtime-invariants-part-8), [Movable Contract](movable-contract.md#movable-runtime-runtime-invariants-part-9), [Persistence Contract](persistence-contract.md#movable-runtime-runtime-invariants-part-10).

- Every modification requires the project-wide compatibility review mandated by the root instructions.
  Trace all affected generation, naming, persistence, loading, migration, cleanup and UI/error paths,
  including alternate bridge branches and legacy assets; fix incompatible callers and consumers
  together. Build or preview success alone is insufficient; report pending in-game verification.
- Use immutable, hardcoded archetype and component metadata produced by the metaprogramming step.
  Runtime code must not rediscover parts from bounds, topology, nearest vertices, connected components,
  height bands, ratios, names or other geometric resemblance.
- Runtime spatial tests for authored-part identification may compare `x`, `y` or `z` only with
  zero. Applying recorded construction coordinates and affine transforms is permitted; it is not
  runtime part identification.
  For stretch versus translation, whether the authored part reaches or crosses `x = 0` is the
  default decision, subject to the explicit base, centred-post, Lift01/Lift02 complete-assembly and complete Draw03 truss rules
  in contract section 8 and the measured Golden mappings in section 5.
  An independently authored vertical railing post centred on the bridge centreline retains its
  original width and centred position at every LOD. Use immutable measured membership, not runtime
  shape/position heuristics. This does not exempt transverse rails or any other crossing structure.
- Outside those explicit exceptions, stretch a centre-crossing part against its own authored span.
  Translate a non-crossing side part rigidly. Never use one family's boundary, profile or measured
  constant to classify another family.
- Treat the base as the complete structure immediately adjacent to and below the deck;
  its individually recorded side blocks need not cross the centre.
  Transform its x coordinates with `x -> x + sign(x) * delta`, except for the explicit Lift01/Lift02 complete-assembly mapping and Draw03 complete
  moving/fixed truss stretch in contract section 8. Do not
  misclassify bridge-pier columns, footings or foundations as the base.
- Translate bridge-pier columns. Do not stretch them.
- Apply every part classification and transform to the highest-detail mesh and every LOD. Do not allow
  reduced LOD topology to reclassify a part.
- Follow [trusted internal execution](../constitution.md#repository-trusted-internal-execution). These invariants
  describe required construction and offline verification, not runtime assertions. Trust memory and
  computed results; retain validation only at external file/network IO boundaries. Preserve actual
  failure handling, native business rules, cancellation and disposal.
- Preserve the archetype's complete prefab behavior. A generated bridge changes requested
  geometry and the selected road structure, together with the native representation, UV-density,
  LOD and persistence conversions explicitly required by the general and specialist contracts.
  Derived tower, cable, piece and LOD prefabs remain unique to that generated bridge; compatible
  original materials and prepared constant resources retain their shared source identities.
- Preserve the archetype-specific constant `bridge width - deck width`. The width definition and the
  constant must come from a same-basis comparison of a real archetype and a real newly generated
  bridge, not runtime geometry inference or source arithmetic alone. Width is the complete
  left-to-right span `maxX - minX`, and the audited width increment is the increment of that complete
  span; it is not a per-side x-coordinate correction. If the absolute full-span width increment is
  greater than 1 m, do not change that bridge; report and record the bridge for review. Absence of a
  generated sample does not satisfy that skip condition:
  the invariant pass stays incomplete until a human creates the missing bridge from a selected road
  and its real report and geometry are retained. The Agent must not open or control the game or issue
  an API/request-file bridge-construction request for sample creation.
- Never apply an audit or geometry correction in runtime bridge-generation code. Metaprogramming must
  finish the correction and fold it into the original immutable archetype parameters or generated
  source metadata. Runtime may consume those final parameters as ordinary generation inputs, but it
  must not contain an audit-correction table, add a post-measurement delta, double a per-side result,
  or otherwise know that a correction step existed.
- Preserve bridge-specific lighting from the same archetype. Carry tower/pylon `ObjectSubObjects` and
  their referenced light/effect prefabs without filtering by names or rebuilding effect fields. Keep
  rotations, parent-mesh indices, groups, probabilities and activation data unchanged. A mounted
  object's non-zero x position follows its parent mesh's rigid half-width displacement; an object on
  `x = 0` remains centred. The recorded Golden ornament-mounted lamps instead follow the measured
  ornament XY mapping under [general contract sections 5](../specs/generation-spec.md#generation-5-a-generated-tower-differs-from-its-archetype-in-width-alone) and 15, with rotation and effects unchanged.
  Do not replace the selected road's ordinary street lighting with the
  archetype road's street lighting.

Follow [generated material program requirements](material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders) for stable Surface program references, Player loading and mod-owned precompiled program registration; geometry, texture and source ownership rules remain unchanged.

