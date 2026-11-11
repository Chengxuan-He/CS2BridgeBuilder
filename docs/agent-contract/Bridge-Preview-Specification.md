# Bridge preview specification

## Contents

- [1. Scope and authority](#1-scope-and-authority)
- [2. Preview identity and entry points](#2-preview-identity-and-entry-points)
- [3. Complete bridge assembly](#3-complete-bridge-assembly)
- [4. Movable bridges](#4-movable-bridges)
- [5. Geometry and appearance fidelity](#5-geometry-and-appearance-fidelity)
- [6. Materials and temporary resource ownership](#6-materials-and-temporary-resource-ownership)
- [7. Rendering, framing and performance](#7-rendering-framing-and-performance)
- [8. UI and errors](#8-ui-and-errors)
- [9. Verification and change scope](#9-verification-and-change-scope)
- [10. Supporting records](#10-supporting-records)

## 1. Scope and authority

This document consolidates the existing requirements for creation and management previews of
fixed, movable and supported double-deck bridges. It specifies required behavior, not a claim
that every implementation or visual check is complete.

Follow the [project contract](PROJECT_CONTRACT.md) and
[bridge generation contract](BRIDGE_GENERATION_CONTRACT.md), the
[movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md), and the
[asset persistence policy](Bridge-Asset-Persistence.md). These contracts govern generation,
source isolation, ownership, error handling and acceptance. This specification does not authorize
new generation exceptions or changes to persisted bridge behavior. The material contract governs the two authorized custom programs and their mod-enabled startup-registration requirement.

## 2. Preview identity and entry points

- Creation previews use the selected road or track, bridge style, supported deck arrangement and
  direction through the same recipe and generator route as formal creation. A catalogue icon or
  generic road model is not a substitute for the requested bridge.
- Management previews show the existing bridge by its exact UUID and stored arrangement. Borrow
  its asset graph read-only; do not regenerate it from a recipe or replace its persisted data.
- Every registered style participates in preview regardless of implementation completeness.
  Do not hide or disable preview, display unfinished-style notices, or substitute another style.
  A missing generator route raises ERROR through the ordinary failure path.
- Selection changes invalidate earlier requests. A late render must not overwrite the current
  selection. Include geometry-affecting options, including lower-deck direction, in preview identity.
- Search, filters and locale changes must not regenerate an unchanged bridge. Keep identity keys
  unlocalized; use the project's localization system for visible labels and dialogs.

## 3. Complete bridge assembly

The preview must show a coherent bridge, including its road surface, markings, structural members,
supports and visible attachments. Assemble native conditional pieces and actual object graphs;
do not stretch source network meshes as if they were ordinary object meshes.

Preserve fixed-segment order, lengths, requirements and placement rules. Evaluate each segment's
state independently. Respect native mesh states, stacked supports and placeholder selection.
A meshless parent can legitimately have visible children; an inactive mesh is not a missing asset.
Missing required geometry or material data is an error, not permission to show a partial bridge.

For double-deck bridges, show both decks with their stored separation, offsets and direction.
Use a real supported double-deck arrangement. Do not invent an extra deck or alter its height
for presentation convenience.

The current illustrative course uses a main-deck elevation of 30 m above flat presentation ground.
This is a preview setting, not a modification of the bridge's permitted construction elevations,
terrain behavior or saved geometry. The preview is not a city simulation or an audit sample.

## 4. Movable bridges

A movable preview must simultaneously show:

1. The approach bridge section on each end, with the relevant stationary structure and supports.
2. The complete central mechanism, including its fixed parts.
3. The moving parts in their open pose, with their attached road, markings and mounted objects.

Do not return a mechanism-only scene or an ordinary closed road spanning the opening. Do not add
stationary road geometry or ordinary intermediate supports inside the shipping opening.

Derive the crossing extent, transition alignment and approach arrangement from the generated
network and its measured native mechanism data. Do not assume one bridge family's span, hinge,
opening angle, lift height or section mapping applies to another. Presentation span selection
must preserve the native placement rules and maintain continuous fixed approach joins.

Evaluate the native procedural skeleton and its parent/connection relationships. Rotate bascule
leaves around their authored hinges; translate lift spans according to their native motion data.
Fixed towers, supports and control buildings remain fixed. Attachments follow their correct
parent transforms. Expand render and camera bounds to include the open pose.

Ordinary nested attachment meshes use the same request-owned recursive placement plan and
mechanism skeleton as formal creation. Metadata-only native carriers must not draw the merged
visuals again, but traversal must retain their exceptional descendants. Conditional visibility,
actual independent animation, instance-color/emissive attachments and light effects retain their
native attachment behavior under movable contract section 19. Do not preselect probabilistic
branches permanently merely to make preview deterministic.

Opening is temporary presentation state. It must not rewrite the generated prefab, drive city
traffic signals, open placed bridges, or imply that shipping compatibility has been verified.

## 5. Geometry and appearance fidelity

Follow [Scope and authority](MATERIAL_GENERATION_CONTRACT.md#scope-and-authority) for this requirement.

Use the generated bridge's geometry and native material behavior. Preserve source vertex formats,
material-slot semantics, normals, transparency and required LOD relationships except for the
conversions explicitly required by the generation contracts. Rigid geometry retains its original
UVs where permitted by the material contract; processed movable-road surfaces use the same
source-density tiled, unpainted materials as formal generation at every LOD. Deformed geometry
uses the same applicable measured mapping and original or prepared material as formal generation.
Preserve normal selected-road paint and lane-aligned fixed-width wear. Unchanged UV values must not be used to justify stretched
textures. Preview must not introduce additional approximations.

Perforated grating in an original bridge is not inherently damaged geometry. Distinguish it from
missing triangles, stretching, overlapping surfaces and material errors using original and actual
generated data. The current selected-road conversion replaces donor road surfaces with the selected
road's surfaces. This specification does not change that rule to retain donor sidewalks. Any such
change requires a separate explicit requirement and corresponding generation work.

Do not hide a generation defect with preview-only replacement geometry, depth adjustments,
material substitution or omitted components. A correct preview must not conceal an incorrect
persisted bridge. Conversely, diagnose preview-only failures independently from saved geometry.

## 6. Materials and temporary resource ownership

Reuse compatible native assets. Geometry motion alone does not justify creating new textures,
materials or shaders. Necessary native network-to-object conversions follow the persistence policy
and must document the actual representation change. Material reuse, native conversions and
offline-prepared resources follow the material generation contract. Moving markings follow
the direct-skeleton rules in movable contract sections 18–19. Their shader consumes the same
current mechanism bones in the open pose, without a historical parent-transform follower.

Reuse original material instances when no state differs. Private Unity instances are permitted
only when preview bindings, animation or rendering state must differ from shared game materials. Never mutate the game's shared instances. Such copies do
not by themselves authorize dynamic shaders or permanent asset export. The material contract
separately authorizes the two custom programs and mod-enabled registration, with mod-enabled startup registration required. Preview must exercise their actual shaders and binding protocols.

Keep generated preview assets in their private temporary scope. Where serialized CID lookup cannot
resolve a private asset, retain its direct object references and balanced property leases for the
preview lifetime. Do not globally publish temporary assets to repair a lookup failure.

Destroy renderers and release render resources before disposing their owning meshes, surface leases
and temporary database. Apply cleanup on success, failure, cancellation, selection change and panel
closure. Borrowed game assets and existing bridges must survive preview disposal unchanged.

### Existing material selection

Preview and formal generation follow the same [material generation contract](MATERIAL_GENERATION_CONTRACT.md).
Full reuse creates no new material; constant converted materials are copied from offline-prepared
resources. No preview-only fallback may change appearance or texel density.

## 7. Rendering, framing and performance

- Frame the complete bridge with its open moving parts and both ends visible. Include attachments
  and deformed bounds; normalized network bounds are insufficient. Do not crop required geometry.
- Use an isolated presentation scene and camera, with a transparent background and readable colors.
  Preserve translucent and cutout surfaces; do not use color-key transparency or repaint materials.
- Preview lighting must be independent of the city. The existing independent-shadow requirement
  remains; any implementation limitation must be recorded rather than silently treated as compliant.
  Do not modify city lights, camera settings, global HDRP settings or engine shadow code.
- Submit rendering and GPU readback in the proper engine-frame order. Do not capture before the
  relevant rendering work has completed or reuse stale pixels as a successful result.
- Keep the current preview for unchanged inputs; do not cache generated bridge results for later
  creation. Choose CPU/GPU execution from measured cost and native API affinity; overlap independent work where thread affinity and resource ownership permit.
  Record measured timings when investigating stalls. This specification introduces no invented
  latency threshold and does not equate faster generation with visual correctness.

### Continuous background generation and asynchronous native dispatch

Preview generation runs continuously on detached background computation and asynchronously dispatched native operations. Do not split ready generation work across frames or impose a 6 ms generation pump budget. Ordinary stage boundaries check cancellation and record diagnostics without forcing a frame yield. Keep dependent pure CPU computations together on workers; native snapshots, Unity/ECS calls and publication remain on the game thread through the engine synchronization context. Worker completion wakes the native dispatcher asynchronously; do not wait for the mod generation system's next tick solely to consume a ready result. A fallback update drain may support environments without a host context. Only actual GPU/render completion that requires engine frame progress may wait for later updates. Never block or busy-wait the game thread for workers. Never run the complete mixed native generation routine through Task.Run.
Every resumed step verifies request cancellation and world lifetime before accessing native state. A changed selection, closed preview or world change invalidates the old result immediately. Do not publish it, begin another request on its temporary state, or dispose resources while its GPU request still uses them. Complete required native cleanup, release its private graph and generation-parameter scope, then start the latest selection. Cancellation is not a generation error and must not display a failure for a superseded selection.

Keep formal creation behavior and independently persisted output unchanged. A synchronous compatibility entry may use the same operations without frame yields; it must never synchronously wait for a continuation scheduled to the thread it blocks. Never run Unity/ECS generation wholesale through `Task.Run`, use nested player-loop pumping, or fake responsiveness with an unrelated animation.

Record request identity, resumed-step count, total elapsed time, active main-thread time and maximum single main-thread work interval. Distinguish GPU queue waiting across frames from active work and separately identify indivisible native API calls that cause long main-thread blocking. Total generation time alone does not demonstrate responsiveness. Offline tests must exercise cancellation before and during pending work, stale completion, cleanup and progression over multiple updates; human in-game input/render responsiveness remains a separate acceptance check.

The request pump uses a private synchronization context and execution-context-local parameter, selected-road and prepared-file scopes. Restoring the outer context after each step is mandatory: another game system must not inherit the suspended request. Keep a retained prepared-file lease until request completion; the IO callback's shorter lifetime is insufficient. Pending GPU callbacks own only their buffer/shader lease and do not require the disposed ECS generation system to resume. Cancellation must unwind queued state machines without waiting for GPU completion; a callback releases the remaining native GPU lease on the main thread.

Native operations remain indivisible; measure their blocking duration rather than pretending a frame budget can preempt them. Stage elapsed times include actual worker/GPU/native dispatch waits. Report active execution and dispatch latency separately.

### Ready-work budget and detached background computation

Preview uses the same [complete three-LOD road/lane reuse contract](BRIDGE_GENERATION_CONTRACT.md#reuse-identical-selected-road-computation-across-lods)
as formal generation, independently within each request rather than sharing mutable results
between preview and creation. This includes applicable railway/lane/cable geometry and retained
decal source/material plans, without changing their rendering protocols. Source identity resolution, extraction/unpacking, curve deformation,
appearance/mapping, clipping/binding, grouping/encoding, bounds and final road results are
shared within the request whenever their consumed inputs are identical. Reuse already-compiled
sampling output instead of recompiling LOD1/LOD2 and relying on a later cache. Genuine road
LOD, appearance or motion-domain differences remain distinct; structural LODs remain unchanged.

Keep the main-thread portion minimal: required Unity/ECS/native reads and writes, immutable
snapshots, native publication and GPU API submission. Move every separable pure CPU stage to
bounded workers, including managed decoding, curve/coordinate arithmetic, appearance-domain
calculations, clipping preparation, grouping, encoding preparation, bounds reductions and scene
transform preparation where those operations do not require native APIs. A stage name alone
is not proof of thread safety. Snapshot exact data and scalar semantics first; do not read live
Mesh/Prefab/Surface objects, ECS or native asset registries from a worker. Preserve arithmetic,
ordering and output ownership. Avoid redundant whole-array copies when an existing immutable
snapshot already supplies the required lifetime.

Bound concurrency and suppress nested fan-out so generation does not starve rendering or input.
The current implementation permits one detached job at a time with whole-mesh work capped at
two concurrent jobs; this is an implementation limit, not permission to leave other eligible
calculations on the main thread. Independent pure-data stages may overlap within a measured
bounded schedule. Dependent pure-data stages must continue on workers when their inputs become
ready, without an unnecessary main-thread/frame round trip merely to schedule the next worker.

Batch request-owned lane mesh snapshots before scheduling packed-frame decoding and deformation. Keep dependent decoding and triangle expansion in one bounded worker job instead of returning to the main thread between source meshes. Native reads and releases remain on the main thread; workers receive owned arrays. Preserve source-cache reuse, cancellation leases and exact output ordering. Batch independent decal occurrence volume calculations into one worker dispatch after native source-policy and priority snapshots; apply returned parts in the original stable order on the main thread. Measure dispatch counts and live latency separately.

The asynchronous native dispatcher drains ready main-thread continuations without an artificial frame budget,
rather than delaying each logical stage by a whole frame. Completed worker results are eligible
at the next available main-thread dispatch point without an additional mandatory frame hold.
Unfinished GPU requests or workers must not busy-poll. Use asynchronous completion
or bounded later-update observation, never synchronous waits or joins on the game thread. GPU
completion/publication still obeys native thread affinity and actual engine-frame ordering.
Record maximum individual native-step and complete dispatch times; neither measurement implies preemption of native calls.

Acquire parameter/data leases before scheduling workers. Suppress unrelated execution-context
propagation and attach only explicit request data; never expose the preview pump or native
contexts to worker code. Cancellation invalidates publication immediately and does not wait for
a running worker. Retain its arrays and leases until it finishes, observe faults and discard stale
results. Native cleanup stays on a reachable main-thread path even after the original system is
destroyed; a pure-data completion must not call into destroyed Unity/ECS state. One cancelled
request must not clear another request's resources. Formal generation uses the same reusable
stage results and semantics; a synchronous compatibility entry cannot block a continuation
scheduled to its own thread.

Support/network assembly and scene construction must separate source snapshots, pure numerical
plans, bounds/transforms, native graph publication, attachments and composition. Do not classify
an entire mixed helper as an indivisible native operation. Keep remaining native calls measured
and execute their ready stages continuously through the appropriate worker or native dispatcher.

Use the [generation timing boundaries](BRIDGE_GENERATION_CONTRACT.md#preview-background-execution-and-timing-boundaries):
report wall time, active main-thread/worker work, queue wait, continuation/frame wait and GPU
completion wait from their own observations. Label unmeasured or overlapping intervals. Do not
present elapsed-minus-active time as separately measured scheduling or GPU latency. Test worker
thread identity, bounded concurrency, continuous ready-stage progress and asynchronous host-context dispatch, no polling spin, cancellation
with pending work, lifetime after system destruction and stale-result suppression, alongside
real-input output equivalence and manual game responsiveness.

**Implementation status:** request-local source and appearance stage reuse now begins with the
sampling result. Detached lane deformation/expansion, composition evaluation, receiver roles,
coverage, appearance grouping/binding, chart projection, final UV output and bounds use worker
jobs in preview. CPU completion wakes the native engine-context dispatcher without artificial frame slicing; the update drain is explicitly logged as fallback when no host context is available. GPU
completion retains asynchronous polling. Native mesh extraction, Surface snapshots, ECS work and native publication remain on the main
thread. Packed vertex streams are copied once per native stream; channel gather and stateless
`NativeMath` array decoding use pinned owned buffers on the worker, without Mesh/ECS access.
The shared request cache does not connect a preview request to a later formal creation.
Offline scheduler and geometry checks do not establish maximum game-frame time. Full live
request stage counts and responsiveness must be checked from game diagnostics; unmeasured
native preparation/publication remains explicit, rather than claimed as background work.

### Viewport-sized capture

Render only the isolated preview viewport. Measure its visible CSS width and device pixel ratio in the UI and report a debounced physical-pixel width to the native renderer. The existing viewport aspect is 2:1. Round width upward to an even pixel and bound it to 64–2048 pixels; derive height as half the width. Before measurement use a bounded 768x384 fallback. Do not allocate a full-screen capture or crop away bridge geometry. Retain full posed camera framing and the existing 2x spatial supersampling plus four subpixel projections, including matching transparent coverage. Resolution changes rerender the existing request-owned draw list without regenerating the bridge. Release the old camera, render targets and stage before replacing them; discard obsolete sizes and preserve cancellation, panel closure and native frame ordering. Report output and raster sizes in render diagnostics. No global city rendering or antialiasing settings may change.

### Posed camera bounds

For procedural moving geometry, camera framing must use indexed vertices transformed by their own native bone indices. Applying every bone to the entire mesh bounds creates nonexistent space and is forbidden. Aggregate framing bounds in the fixed camera basis, including both approaches and mounted objects. The existing bulk skin-transform and bound-reduction path uses the GPU; execution changes must follow measured cost and native API affinity.

## 8. UI and errors

The preview frame contains no text in idle, loading, success or failure states. Loading uses the
explicit loading state. Stop it on failure and reject stale results. Keep the image accessibility
label without exposing broken-image fallback text.

Report operation failures through the existing localized dialogs and ERROR logging. Permanently
omit the old inline error footer below the action buttons. Messages and controls follow the same
behavior for every registered style, regardless of completeness.

Logs must preserve the request identity, failure stage and underlying exception. Identify the
missing prefab, material slot or resource when known. Do not replace an internal invariant failure
with a silent fallback, partial preview or false success.

## 9. Verification and change scope

Inspect the relevant original and generated geometry, component graph, native behavior and logs.
A successful build, an emitted image or valid indices alone does not establish visual acceptance.
Automated geometry, data and serialization checks are allowed; they do not establish visual
acceptance. Do not use computer-control tools. Humans perform the in-game checks.

| Area | Required human check |
| --- | --- |
| Fixed bridge | Complete connected spans, supports, markings and attachments; no clipping |
| Movable bridge | Both approaches and central mechanism visible together; moving parts open; fixed parts stationary |
| Double deck | Both decks visible with correct native separation and selected direction |
| Appearance | Correct surface coverage, grating/transparency, colors and markings; no torn or overlapping surfaces |
| Bounds and LODs | Full open envelope framed; relevant generated near/far geometry consistent |
| Lifecycle | Repeated selection, cancellation, close/reopen and create/manage switching without stale images or leaked resources |
| Isolation | City lighting, source roads, saved bridges and formal asset files remain unchanged by preview |
| Failure | Ordinary error dialog and useful ERROR log; no inline footer, unfinished notice or partial success |

State separately what was inspected, implemented, built/deployed and visually accepted. Keep unresolved
appearance or animation findings explicit. Preview acceptance does not prove vessel coordination,
traffic clearance or cold-start persistence.

Preview-only or documentation-only changes remove no existing bridges. Code changes require the
project's build/deployment workflow; documentation-only updates require no shutdown or deployment.
Generation changes require recovery only for demonstrably incompatible UUID-owned assets;
a source change alone does not authorize deleting healthy bridges.

## 10. Supporting records

- [Runtime UI and localization](../design/interface/Runtime-UI-Localization.md): selection, loading and dialog behavior.

Follow [generated material program requirements](MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders) for program persistence, Player loading and mod-enabled registration; geometry, texture and source ownership rules remain unchanged.