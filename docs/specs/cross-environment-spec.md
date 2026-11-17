# Cross Environment Spec

Required companion: [Cross Environment Contract](../contracts/cross-environment-contract.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](../contracts/reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

## Always-applicable portability requirements

Read this complete specification for every task under the [constitution](../constitution.md#backward-and-cross-environment-compatibility)
and repository agent entry point. Design constraints apply to development, repair, review and
documentation regardless of whether a compatibility test round is requested.

CUDA is prohibited, including optional acceleration paths, runtime dependencies and project tools.
Do not assume an NVIDIA GPU is available. Use supported vendor-neutral graphics/compute APIs or
CPU execution as appropriate; an NVIDIA-only development machine does not justify a CUDA path.

Directory mappings are permitted for system-drive space management and any other user purpose.
Do not ban mappings, require their removal, or reject a valid operation merely because a directory
is redirected. Apply the same behavior to configured roots, owner directories, nested directories,
backup/export destinations and state directories, including junctions, directory symbolic links
and mapped storage paths.

For equivalent inputs, permissions and storage availability, mapped and unmapped paths must yield
identical discovery, reads, creation, persistence, reload, export, migration, backup and deletion
outcomes. Preserve logical owner identities and relative references while operating on the actual
owned payloads. Moving a link alone is not a completed backup, and unlinking alone is not completed
deletion while the selected owner's payloads remain. A backup must retain the original files
independently of the active source mapping. Logical drive-letter differences must not prevent an
otherwise valid move; cross-volume operations must fulfill the same move/backup postconditions.

Mapping resolution does not grant ownership of unrelated target contents or other owners' files.
Preserve operation scope, actual IO errors, partial-failure reporting, durable deletion intent,
backup-first recovery and the existing permission-specific deletion fallback. Mapping support
changes no self-check or player-deletion workflow, lifecycle boundary or authorization.

Acceptance compares an unmapped baseline with mappings at each affected directory boundary,
including same-volume, cross-volume and available network destinations. Inspect actual retained,
backed-up and removed payloads, not only link existence or success flags. Exercise subsequent reads
and unchanged reruns, partial failures and preservation of unrelated files. Keep unavailable
environments explicit under the evidence rules below.

## Local coverage and delivery

In each manually requested or pre-release compatibility round, test every applicable cross-environment scenario that can be exercised locally and verify normal project operation in those conditions. Cover available local permission, mapping, dependency-presence/absence, compiler/runtime and other environment variations; do not omit a locally testable case merely because other users' environments cannot be reproduced. Retain the existing safe-fixture and manual game-start boundaries.

A local environment cannot cover every user's hardware, operating system, DLC/mod combination or cloud session. Unavailable environments and untested external combinations must not block GitHub upload or publication and do not require a separate user approval solely for those coverage gaps. Keep their actual Blocked/NotRun status and evidence limits; never convert missing coverage into Passed or a universal compatibility claim. This rule does not waive locally executable tests or excuse an actual observed product failure. Diagnose and repair actual failures under the existing development obligations, separately from unavailable coverage.

## DLC and mod compatibility

Players may install and enable different DLC and asset-mod combinations. The project must continue normal operation for available supported content when some required external DLC or mods are absent or disabled; missing content must not cause unrelated features to fail or trigger native missing-reference errors. Partial availability includes an incomplete set of registered required sources for one style, as well as unavailable sources elsewhere in the catalogue. It does not mean corrupt or incomplete internals of a normally functioning source mod.

Distinguish ownership/subscription or files installed on disk from content enabled and successfully loaded in the active playset. Check every registered source through its existing native identity and content state. Assume an installed, enabled and normally functioning source mod supplies complete, valid own resources; do not add a full resource-graph scan to the UI/catalogue or defensive checks for hypothetical source-mod internal corruption. Cached Prefabs and known CIDs alone do not establish that their external provider is active. Optional integrations must not become global mandatory dependencies.

Each style declares an ordered required-source array. Base-game styles use an empty array (`[]`); in a nonempty array, the first entry is the direct source of the prototype, followed by its explicitly registered additional required DLC or asset mods. Preserve that order for source presentation, normally placing the source mod before its required DLC. Entries are cumulative requirements, not alternative providers or fallback candidates: traverse the complete array and require every entry to be available. For example, `GoldenGateDouble` declares `[Bridge Expansion Pack, San Francisco Set DLC]`, with the mod as its direct source. Use verified native identities for binding, never display titles as keys. At runtime, use this finite declaration without recursively discovering or auditing mod dependency chains. Conversely, trusting a functioning mod does not exempt any DLC or mod explicitly registered in the style array. Unregistered, unrelated content must not become a gate.

During development, recursively trace each style's actual prototype references offline to establish its complete required-source closure before registering or revising the array. Follow referenced Prefabs, nested objects, meshes, every LOD, Surfaces, textures and native dependency/cache edges to their actual providers; handle repeated identities and cycles without truncating coverage. Record source versions/identities, reference paths, provider ownership and unresolved references as reproducible evidence. Add every evidenced required external DLC/mod once, retaining the direct source first and a stable order for the remaining entries. Base-game-only closure uses `[]`. A package title, top-level owner or assumption that its mod functions normally is not proof of a complete declaration; do not add unrelated contents of the same package merely because they exist. Missing evidence remains explicit and must not be reported as a completed closure. Recheck the evidence when the prototype or consumed reference graph changes. This offline authoring duty does not add a runtime resource-graph scan, internal-corruption defense or new saved-owner recovery policy.

The recursive investigation is a development/build-time operation only. Hardcode its reviewed result as the fixed ordered source declaration in code or a packaged data file. For source prerequisite management, that declaration is the only audit-derived runtime payload: exclude recursive audit scripts/tools, asset/provider indexes, reference graphs, closure evidence and intermediate scan outputs from the release package and compiled runtime resources. Runtime must not invoke those tools, traverse an index or graph, or dynamically derive, expand or recalculate source dependencies. It may only iterate the already registered sources and check each source's native availability. This adds no startup audit or new startup-check workflow and does not replace the existing native loading, persistence or self-check dependency handling. Keep offline evidence in development records and verify the package excludes these audit artifacts.

Bind the same ordered source array consistently for UI and new generation under the [style source-binding rules](generation-spec.md#generation-22-new-bridge-styles-require-immediate-ui-synchronization-generator-routing-and-multilingual-names). If any registered source is missing, disabled or not successfully loaded, omit the style from new-creation selection and forbid new generation and its preview. Direct, queued or stale programmatic requests must enforce the same all-sources boundary before accessing dependent resources, including after asynchronous preparation when availability may have changed. Report an unavailable-source failure; do not silently switch to another provider or ignore later array entries. This prerequisite rule does not authorize hiding an otherwise available style merely because implementation is incomplete.

Dependent resources must not be loaded, registered, realized or offered for use while any registered required source is unavailable. Check the complete source array before dependent native loading. This is not a new exhaustive scan of a functioning source mod's resource graph. Retain actual file/native IO failures and existing Bridge Builder-owned prepared/persisted input checks at their operation boundaries. Use the existing [material availability boundary](../contracts/material-contract.md#material-missing-optional-content-and-virtual-textures) and [prepared-input discovery isolation](../contracts/material-contract.md#material-prepared-material-discovery-boundary); do not turn unselected DLC resources into global startup loads or fabricate substitutes to mask missing references.

This is distinct from absence of the original authoring Prefab: [hardcoded source independence](../contracts/generation-contract.md#generation-hardcoding-and-source-independence-are-retained) remains valid when declared content prerequisites and required runtime resources are satisfied. Healthy saved bridges with complete independent owner snapshots are not invalidated merely because their original source mod is absent. Saved-owner inspection still follows the existing [self-check recovery workflow](../contracts/self-check-contract.md#self-check-unavailable-optional-content), including permitted repair sources, owner-scoped recovery and permission rules. This section changes no migration, deletion, restart or preservation authorization.

Acceptance covers all-required-content present, DLC absent, source mod absent, installed but disabled/not loaded, a multi-source style with each registered entry independently absent or disabled, an empty-array base-game style, a catalogue with only some sources available, and content restored on a subsequent supported load. Check creation-list visibility, stale/direct requests, selected and unselected resource loading, preview, ordinary available-content operation and saved independent owners separately. Include an active, natively available DLC whose requirement does not require a separate content database: its absence must not hide the style or reject preview/creation. Also cover a DLC whose native requirement does require a database, with that database absent; it must remain unavailable. Preserve native ownership and active-state requirements in both cases. Record actual dependency identities, native requirement results and whether database presence is required; a developer machine with every DLC installed is not missing-content evidence. Follow the existing manual/pre-release activation and evidence limits below.

<a id="cross-environment-3-required-matrix-and-evidence-limits"></a>

## Required matrix and evidence limits

Use Windows x64 as the current native host baseline. Record the actual OS/game support separately
from what a harness can compile. Select concrete host and configuration combinations, not an
unbounded claim to every driver, compiler or processor.

| Environment or variation | Required evidence | Insufficient substitute |
| --- | --- | --- |
| Windows with an AMD GPU | Actual AMD device, driver and native game rendering/compute run | NVIDIA hardware, a vendor label, API fakes or a successful build |
| Windows with an NVIDIA GPU | Actual NVIDIA device, driver and native game rendering/compute run | Host inventory without running the tested graphics path |
| GeForce NOW | Actual cloud session that permits loading this code mod, candidate identity and session-persistence behavior | A local NVIDIA GPU, Windows VM, game catalogue listing or assumed mod support |
| Restricted file access | Actual denied reads/writes in isolated paths, operation outcome, retained data and restored permissions | A read-only attribute used as proof that every directory permission is denied |
| Folder mapping | Actual supported redirection/junction/mapped path, resolved source/output locations and ownership behavior | String replacement or a fake asset database alone |
| Compiler and architecture variation | Actual compiler DLL/version/hash, flags, host architecture and resulting shader/container behavior | Different flags described as different compiler versions; x86/ARM compilation described as a runnable game host |

GeForce NOW is a cloud service, not a GPU brand. Establish current code-mod loading and storage
capabilities from authoritative service information and an actual session before testing Bridge
Builder there. If those prerequisites cannot be established, record Blocked. Do not infer support
from availability of Cities: Skylines II in a service catalogue.

Include available differing Windows/compiler versions and processor architectures in the declared
matrix. Mark unavailable requested combinations explicitly. Do not claim x86 or ARM game support
from cross-compilation, emulation, AnyCPU metadata or a standalone harness. Record emulation as a
separate environment if it is actually exercised.

Keep evidence tiers distinct: static review, managed harness with native API fakes, real OS IO,
real shader compilation, isolated Unity execution and native game end-to-end acceptance. A .NET
harness is not the game's Mono runtime; successful D3DCompile/container reconstruction is not GPU
loading, rendering or vendor-driver certification. Preserve these limits in every summary.

<a id="cross-environment-5-candidate-and-end-to-end-cases"></a>

## Candidate and end-to-end cases

Record for every environment:

- Mod version, exact candidate DLL/package hashes and source commit; game version and required content.
- OS version/build, host/process architecture, CPU, actual GPU model, driver and graphics backend.
- Actual compiler DLL location, version/hash and options; native/managed runtime and harness versions.
- Logical and resolved input/output roots, permissions, filesystem/mapping type and isolation boundaries.
- Commands, inputs, expected and observed outcomes, logs/captures, cleanup results and evidence tier.

Run a baseline with ordinary permissions before each fault variation. Use the same declared inputs
across comparable environments. Cover loading, bridge selection and localized UI, preview, creation,
saving/export where applicable, native placement, and a user-performed restart/cold load of the
saved bridge. Check near/far appearance and movable motion on applicable inputs separately from
serialization. Preserve original bridge UUIDs, dependencies and existing unrelated assets.

Exercise the real variable-length shader compiler/container path with shorter and longer output,
multiple slots and repeated reconstruction. Vary actual available compiler configurations and
record what changed; do not restore fixed-length slots or old-bytecode fallback. Test native
loading/rendering independently. An isolated compiler test need not create persistent game assets.

Use offline assertions and purpose-built test harnesses. Do not add production assertions,
internal-result verification, startup benchmark loops or invasive probes to satisfy this contract.
The repository's trusted internal execution policy and existing external IO boundaries remain
unchanged. Tests must not become a runtime prerequisite for ordinary player operations.

