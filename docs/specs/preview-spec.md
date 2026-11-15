# Bridge preview specification

Required companion: [Preview Contract](../contracts/preview-contract.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](../contracts/reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="preview-contents"></a>

## Contents

- [1. Scope and authority](../contracts/preview-contract.md#preview-1-scope-and-authority)
- [2. Preview identity and entry points](preview-spec.md#preview-2-preview-identity-and-entry-points)
- [3. Complete bridge assembly](preview-spec.md#preview-3-complete-bridge-assembly)
- [4. Movable bridges](preview-spec.md#preview-4-movable-bridges)
- [5. Geometry and appearance fidelity](preview-spec.md#preview-5-geometry-and-appearance-fidelity)
- [6. Materials and temporary resource ownership](../contracts/preview-contract.md#preview-6-materials-and-temporary-resource-ownership)
- [7. Rendering, framing and performance](preview-spec.md#preview-7-rendering-framing-and-performance)
- [8. UI and errors](preview-spec.md#preview-8-ui-and-errors)
- [9. Verification and change scope](preview-spec.md#preview-9-verification-and-change-scope)
- [10. Supporting records](preview-spec.md#preview-10-supporting-records)

<a id="preview-2-preview-identity-and-entry-points"></a>

## Preview identity and entry points

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

<a id="preview-3-complete-bridge-assembly"></a>

## Complete bridge assembly

The preview must show a coherent bridge, including its road surface, markings, structural members,
supports and visible attachments. Assemble native conditional pieces and actual object graphs;
do not stretch source network meshes as if they were ordinary object meshes.

Preserve fixed-segment order, lengths, requirements and placement rules. Evaluate each segment's
state independently. Respect native mesh states, stacked supports and placeholder selection.
A meshless parent can legitimately have visible children; an inactive mesh is not a missing asset.
Missing required geometry or material data is an error, not permission to show a partial bridge.

Root pillar placeholder selection uses the native network's `NetGeometryData.m_ElevatedWidth`,
as native sub-object creation and alignment do. Composition width can include overhead structure;
it is not the road width for ranking horizontal replacements or spacing paired vertical pillars.
Use the actual sampled network value, preserving native candidates and requirements. Do not fix
an incorrectly selected replacement by excluding a mod/name, hiding its children, or moving the
prototype's support offset. Diagnose the selected parent and every child draw together.

Traverse `NetPieceObjects` on every selected network piece, including overhead and meshless
carrier pieces. Evaluate each occurrence's native requirements, probability, spacing, offsets,
rotation and curve alignment in the corresponding edge/node context. Include the referenced
objects and their visible descendants through the common object assembly path. A repeated
piece object is distinct from a root `NetSubObjects` placement; do not omit it or draw either
occurrence twice. Preserve selected conditional composition and authored placement semantics
instead of adding a bridge-specific preview replacement. Snapshot native inputs on the main
thread and prepare detached placement arithmetic on bounded workers before native assembly.

For double-deck bridges, show both decks with their stored separation, offsets and direction.
Use a real supported double-deck arrangement. Do not invent an extra deck or alter its height
for presentation convenience.

The current illustrative course uses a main-deck elevation of 30 m above flat presentation ground.
This is a preview setting, not a modification of the bridge's permitted construction elevations,
terrain behavior or saved geometry. The preview is not a city simulation or an audit sample.

Present fixed spans and movable approaches as continuing networks, not isolated dead-end roads.
Their visible ends must not acquire rounded road caps merely because the presentation course
stops there. Establish connected continuation context in the private native topology before
composition and endpoint geometry are calculated. Do not draw continuation road surfaces or include
those surfaces in camera bounds. Boundary object occurrences follow the visible-domain rule below.
Keep the visible span's real native node/edge transforms, structural node
pieces, supports and attachments; removing all node geometry or node objects is not a valid fix.
This presentation topology does not modify the generated prefab, the city or saved bridge assets.

Use one longitudinal placement and visible-domain convention for the selected road surface,
lane meshes, paint, node composition and structural attachments. Derive both endpoint transforms
from the same native course. Retain pre-pack piece records for native initialization: native
composition packing mutates offsets and must not receive already packed records. Road rendering,
lane adjacency/marking selection and object placement must use the same initialized composition
and its presentation boundaries. Continuation context must not leak an extra
road panel or marking segment into only one visible end. Include native structural object occurrences
on adjacent context edges when their anchors belong to the visible course, including its endpoints;
retain each whole object, including its authored extension beyond the presentation cut. Do not clip
load-bearing members or fabricate dead-end requirements to fill a missing boundary occurrence.
Preserve genuine prototype asymmetry
and native endpoint requirements; do not mirror geometry or discard structural node pieces merely
to make an image symmetric. Test both ends, reversed direction and successive visible spans.

<a id="preview-4-movable-bridges"></a>

## Movable bridges

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
native attachment behavior under [movable contract section 19](movable-spec.md#movable-19-common-motion-stacking-visibility-and-underside). Do not preselect probabilistic
branches permanently merely to make preview deterministic.

Opening is temporary presentation state. It must not rewrite the generated prefab, drive city
traffic signals, open placed bridges, or imply that shipping compatibility has been verified.

<a id="preview-5-geometry-and-appearance-fidelity"></a>

## Geometry and appearance fidelity

See [Scope and authority](../contracts/material-contract.md#material-scope-and-authority).

Use the generated bridge's geometry and native material behavior. Preserve source vertex formats,
material-slot semantics, normals, transparency and required LOD relationships except for the
conversions explicitly required by the generation contracts. Rigid geometry retains its original
UVs where permitted by the material contract; processed movable-road surfaces use the same
source-density tiled, unpainted materials as formal generation at every LOD. Deformed geometry
uses the same applicable measured mapping and original or prepared material as formal generation.
Preserve normal selected-road paint and lane-aligned fixed-width wear. Unchanged UV values must not be used to justify stretched
textures. Preview must not introduce additional approximations.

Fixed-network previews retain the selected road's actual marking assets, conditional lane/submesh
selection, colors, UV projection, native repeat length and phase, and decal/receiver rendering
protocol. Include the native dashed/solid lines, arrows and other markings selected by that road
and course state; do not replace them with generic lines or infer artwork from lane counts.
Trace missing or incorrect markings through sampled lanes, native flags, geometry, materials and
the isolated render path. Use the same visible course boundaries as the road surface. The movable
road appearance exceptions above remain scoped to their existing generation rules.

Perforated grating in an original bridge is not inherently damaged geometry. Distinguish it from
missing triangles, stretching, overlapping surfaces and material errors using original and actual
generated data. The current selected-road conversion replaces donor road surfaces with the selected
road's surfaces. This specification does not change that rule to retain donor sidewalks. Any such
change requires a separate explicit requirement and corresponding generation work.

Do not hide a generation defect with preview-only replacement geometry, depth adjustments,
material substitution or omitted components. A correct preview must not conceal an incorrect
persisted bridge. Conversely, diagnose preview-only failures independently from saved geometry.

<a id="preview-7-rendering-framing-and-performance"></a>

## Rendering, framing and performance

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

<a id="preview-ready-work-budget-and-detached-background-computation"></a>

## Ready-work budget and detached background computation

Preview uses the same [complete three-LOD road/lane reuse contract](generation-spec.md#generation-reuse-identical-selected-road-computation-across-lods)
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

Use the [generation timing boundaries](generation-spec.md#generation-preview-background-execution-and-timing-boundaries):
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

<a id="preview-viewport-sized-capture"></a>

## Viewport-sized capture

Render only the isolated preview viewport. Measure its visible CSS width and device pixel ratio in the UI and report a debounced physical-pixel width to the native renderer. The existing viewport aspect is 2:1. Round width upward to an even pixel and bound it to 64–2048 pixels; derive height as half the width. Before measurement use a bounded 768x384 fallback. Do not allocate a full-screen capture or crop away bridge geometry. Retain full posed camera framing and the existing 2x spatial supersampling plus four subpixel projections, including matching transparent coverage. Resolution changes rerender the existing request-owned draw list without regenerating the bridge. Release the old camera, render targets and stage before replacing them; discard obsolete sizes and preserve cancellation, panel closure and native frame ordering. Report output and raster sizes in render diagnostics. No global city rendering or antialiasing settings may change.

<a id="preview-posed-camera-bounds"></a>

## Posed camera bounds

For procedural moving geometry, camera framing must use indexed vertices transformed by their own native bone indices. Applying every bone to the entire mesh bounds creates nonexistent space and is forbidden. Aggregate framing bounds in the fixed camera basis, including both approaches and mounted objects. The existing bulk skin-transform and bound-reduction path uses the GPU; execution changes must follow measured cost and native API affinity.

<a id="preview-8-ui-and-errors"></a>

## UI and errors

The preview frame contains no text in idle, loading, success or failure states. Loading uses the
explicit loading state. Stop it on failure and reject stale results. Keep the image accessibility
label without exposing broken-image fallback text.

Report operation failures through the existing localized dialogs and ERROR logging. Permanently
omit the old inline error footer below the action buttons. Messages and controls follow the same
behavior for every registered style, regardless of completeness.

Logs must preserve the request identity, failure stage and underlying exception. Identify the
missing prefab, material slot or resource when known. Do not replace an actual operation failure
with a silent fallback, partial preview or false success. GPU pixel contents are trusted results,
not a runtime visibility or appearance acceptance gate.

<a id="preview-9-verification-and-change-scope"></a>

## Verification and change scope

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

<a id="preview-10-supporting-records"></a>

## Supporting records

- [Runtime UI and localization](ui-spec.md): selection, loading and dialog behavior.

Follow [generated material program requirements](../contracts/material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders) for stable Surface program references, Player loading and mod-owned precompiled program registration; geometry, texture and source ownership rules remain unchanged.

<a id="movable-preview-and-performance"></a>

## Preview and performance

Follow the [bridge preview specification](../contracts/reading-map.md#bridge-preview-specification). Assemble both fixed
ends and the complete central mechanism in an open pose; frame and centre the complete transformed
assembly, including skinned bounds. Preview and formal creation share geometry and material rules,
but preview does not publish permanent assets. Use the same localized style/name and normal error
handling as other bridges. A missing generator raises ERROR; it is not replaced by an ordinary road.

<a id="repository-performance-and-deployment-part-4"></a>

## Request-local preview execution

Related requirements: [Development Contract](../contracts/development-contract.md#repository-performance-and-deployment), [Generation Spec](generation-spec.md#repository-performance-and-deployment-part-2), [Development Contract](../contracts/development-contract.md#repository-performance-and-deployment-part-3).

[Request-local road/lane LOD reuse](generation-spec.md#generation-reuse-identical-selected-road-computation-across-lods) covers source resolution through final output. [Preview execution](../contracts/preview-contract.md#preview-continuous-background-generation-and-asynchronous-native-dispatch) uses continuous detached CPU work and asynchronous native dispatch, without artificial frame slicing or a 6 ms budget. Real GPU/render completion may await engine updates. Batch lane decoding/deformation and decal volumes, preserve cancellation and owned data, and render the measured physical viewport with supersampling. These are requirements, not claims of live acceptance.

