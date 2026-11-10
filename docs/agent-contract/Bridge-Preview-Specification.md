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
new generation exceptions or changes to persisted bridge behavior. The material contract records dynamic shaders as unavailable under current independent-loading requirements.

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

Opening is temporary presentation state. It must not rewrite the generated prefab, drive city
traffic signals, open placed bridges, or imply that shipping compatibility has been verified.

## 5. Geometry and appearance fidelity

Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose roads need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable road-processing scope.

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
the native rigid-decal attachment rules in movable contract sections 18–19, including child
placement through the actual parent bone in the open pose.

Reuse original material instances when no state differs. Private Unity instances are permitted
only when preview bindings, animation or rendering state must differ from shared game materials. Never mutate the game's shared instances. Such copies do
not by themselves authorize dynamic shaders or permanent asset export; the shared material contract records why dynamic shaders are currently unavailable.

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

### Cooperative generation and cancellation

Preview generation must release the game main thread between bounded work units, not merely show a spinner before running the entire synchronous builder. Use the same geometric and material operations as formal creation, with an explicit request-owned asynchronous execution context. Yield between native preparation, per-LOD assembly, per-model publication and scene construction units. GPU dispatch stays on the main thread; poll asynchronous readback completion across updates instead of blocking that thread with full-pipeline readback waits. All CPU work that can be detached from Unity/ECS/native APIs must run on background workers using immutable owned inputs. Mixed helpers must separate native snapshots/publication from pure computation; never borrow mutable native request state on a worker.

Every resumed step verifies request cancellation and world lifetime before accessing native state. A changed selection, closed preview or world change invalidates the old result immediately. Do not publish it, begin another request on its temporary state, or dispose resources while its GPU request still uses them. Complete required native cleanup, release its private graph and generation-parameter scope, then start the latest selection. Cancellation is not a generation error and must not display a failure for a superseded selection.

Keep formal creation behavior and independently persisted output unchanged. A synchronous compatibility entry may use the same operations without frame yields; it must never synchronously wait for a continuation scheduled to the thread it blocks. Never run Unity/ECS generation wholesale through `Task.Run`, use nested player-loop pumping, or fake responsiveness with an unrelated animation.

Record request identity, resumed-step count, total elapsed time, active main-thread time and maximum single main-thread work interval. Distinguish GPU queue waiting across frames from active work and separately identify indivisible native API calls that exceed the target frame budget. Total generation time alone does not demonstrate responsiveness. Offline tests must exercise cancellation before and during pending work, stale completion, cleanup and progression over multiple updates; human in-game input/render responsiveness remains a separate acceptance check.

The request pump uses a private synchronization context and execution-context-local parameter, selected-road and prepared-file scopes. Restoring the outer context after each step is mandatory: another game system must not inherit the suspended request. Keep a retained prepared-file lease until request completion; the IO callback's shorter lifetime is insufficient. Pending GPU callbacks own only their buffer/shader lease and do not require the disposed ECS generation system to resume. Cancellation must unwind queued state machines without waiting for GPU completion; a callback releases the remaining native GPU lease on the main thread.

Budget checkpoints currently target approximately 6 ms between checks, not a hard frame deadline. Native job execution, resource imports, one mesh upload/bounds operation remain indivisible main-thread operations; support-object assembly must expose its separable stages. Record their containing step; do not describe those calls as preemptible. Existing stage wall clocks include frame waits after this change. Use `activeMainThreadMs` and `maxMainThreadStepMs` to diagnose blocking rather than comparing an asynchronous stage's elapsed wall time with an older synchronous stage.

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

A preview update drains ready main-thread continuations within one aggregate frame budget,
rather than delaying each logical stage by a whole frame. Completed worker results are eligible
at the next available main-thread dispatch point without an additional mandatory frame hold.
Unfinished GPU requests or workers must not busy-poll in that budget. Use asynchronous completion
or bounded later-update observation, never synchronous waits or joins on the game thread. GPU
completion/publication still obeys native thread affinity and actual engine-frame ordering.
Record maximum individual step time and maximum complete pump time; neither budget is preemption
of an indivisible native API call.

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
and make their surrounding loops cooperatively schedulable.

Use the [generation timing boundaries](BRIDGE_GENERATION_CONTRACT.md#preview-background-execution-and-timing-boundaries):
report wall time, active main-thread/worker work, queue wait, continuation/frame wait and GPU
completion wait from their own observations. Label unmeasured or overlapping intervals. Do not
present elapsed-minus-active time as separately measured scheduling or GPU latency. Test worker
thread identity, bounded concurrency, same-frame ready progress, no polling spin, cancellation
with pending work, lifetime after system destruction and stale-result suppression, alongside
real-input output equivalence and manual game responsiveness.

**Implementation status:** request-local source and appearance stage reuse now begins with the
sampling result. Detached lane deformation/expansion, composition evaluation, receiver roles,
coverage, appearance grouping/binding, chart projection, final UV output and bounds use worker
jobs in preview. CPU completion posts ready work without mandatory next-frame polling; GPU
completion retains asynchronous polling. Native mesh extraction, Surface snapshots, ECS work and native publication remain on the main
thread. Packed vertex streams are copied once per native stream; channel gather and stateless
`NativeMath` array decoding use pinned owned buffers on the worker, without Mesh/ECS access.
The shared request cache does not connect a preview request to a later formal creation.
Offline scheduler and geometry checks do not establish maximum game-frame time. Full live
request stage counts and responsiveness must be checked from game diagnostics; unmeasured
native preparation/publication remains explicit, rather than claimed as background work.

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

Dynamic material shaders are governed by [the scoped material requirements](MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders): actual source generation, Player-capable loading and independent owner-local cold loading, without a continuing mod synchronization service. This permission does not relax geometry, texture-content or source-ownership rules.
