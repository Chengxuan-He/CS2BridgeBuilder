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

