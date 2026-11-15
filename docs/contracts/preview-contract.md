# Preview Contract

Required companion: [Preview Spec](../specs/preview-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="preview-1-scope-and-authority"></a>

## Scope and authority

This document consolidates the existing requirements for creation and management previews of
fixed, movable and supported double-deck bridges. It specifies required behavior, not a claim
that every implementation or visual check is complete.

Follow the [project contract](reading-map.md#project-contract) and
[bridge generation contract](reading-map.md#bridge-generation-contract), the
[movable bridge contract](reading-map.md#movable-bridges-contract), and the
[asset persistence policy](reading-map.md#bridge-asset-persistence). These contracts govern generation,
source isolation, ownership, error handling and acceptance. This specification does not authorize
new generation exceptions or changes to persisted bridge behavior. The material contract governs the two authorized custom programs and their mod-enabled startup-registration requirement.

<a id="preview-6-materials-and-temporary-resource-ownership"></a>

## Materials and temporary resource ownership

Reuse compatible native assets. Geometry motion alone does not justify creating new textures,
materials or shaders. Necessary native network-to-object conversions follow the persistence policy
and must document the actual representation change. Material reuse, native conversions and
offline-prepared resources follow the material generation contract. Moving markings follow
the direct-skeleton rules in [movable contract sections 18–19](../specs/movable-spec.md#movable-18-marking-layout-mesh-and-native-material). Their shader consumes the same
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

<a id="preview-existing-material-selection"></a>

## Existing material selection

Preview and formal generation follow the same [material generation contract](reading-map.md#material-generation-contract).
Full reuse creates no new material; constant converted materials are copied from offline-prepared
resources. No preview-only fallback may change appearance or texel density.

<a id="preview-continuous-background-generation-and-asynchronous-native-dispatch"></a>

## Continuous background generation and asynchronous native dispatch

Preview generation runs continuously on detached background computation and asynchronously dispatched native operations. Do not split ready generation work across frames or impose a 6 ms generation pump budget. Ordinary stage boundaries check cancellation and record diagnostics without forcing a frame yield. Keep dependent pure CPU computations together on workers; native snapshots, Unity/ECS calls and publication remain on the game thread through the engine synchronization context. Worker completion wakes the native dispatcher asynchronously; do not wait for the mod generation system's next tick solely to consume a ready result. A fallback update drain may support environments without a host context. Only actual GPU/render completion that requires engine frame progress may wait for later updates. Never block or busy-wait the game thread for workers. Never run the complete mixed native generation routine through Task.Run.
Every resumed step verifies request cancellation and world lifetime before accessing native state. A changed selection, closed preview or world change invalidates the old result immediately. Do not publish it, begin another request on its temporary state, or dispose resources while its GPU request still uses them. Complete required native cleanup, release its private graph and generation-parameter scope, then start the latest selection. Cancellation is not a generation error and must not display a failure for a superseded selection.

Keep formal creation behavior and independently persisted output unchanged. A synchronous compatibility entry may use the same operations without frame yields; it must never synchronously wait for a continuation scheduled to the thread it blocks. Never run Unity/ECS generation wholesale through `Task.Run`, use nested player-loop pumping, or fake responsiveness with an unrelated animation.

Record request identity, resumed-step count, total elapsed time, active main-thread time and maximum single main-thread work interval. Distinguish GPU queue waiting across frames from active work and separately identify indivisible native API calls that cause long main-thread blocking. Total generation time alone does not demonstrate responsiveness. Offline tests must exercise cancellation before and during pending work, stale completion, cleanup and progression over multiple updates; human in-game input/render responsiveness remains a separate acceptance check.

The request pump uses a private synchronization context and execution-context-local parameter, selected-road and prepared-file scopes. Restoring the outer context after each step is mandatory: another game system must not inherit the suspended request. Keep a retained prepared-file lease until request completion; the IO callback's shorter lifetime is insufficient. Pending GPU callbacks own only their buffer/shader lease and do not require the disposed ECS generation system to resume. Cancellation must unwind queued state machines without waiting for GPU completion; a callback releases the remaining native GPU lease on the main thread.

Native operations remain indivisible; measure their blocking duration rather than pretending a frame budget can preempt them. Stage elapsed times include actual worker/GPU/native dispatch waits. Report active execution and dispatch latency separately.

