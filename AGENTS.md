All documentation must be written in English.
Unless otherwise specified, all project documentation must be saved as Markdown (.md).
Backups must contain only the original files being backed up. Do not add any other files,
including documentation, README files, manifests, checksum files or additional metadata.

# Repository Agent Instructions

## Multilingual adaptation is required for every change

Every project modification must include a localization review. All new or changed user-visible
text, including labels, settings, tooltips, dialogs, validation messages, success/failure notices
and generated asset display text, must use the project's localization system and provide complete
translations for every supported UI language in the same change. Do not hardcode user-facing
strings or treat English fallback text as completed multilingual support. Check affected callers
and alternate/error paths, preserve format placeholders, and run the applicable localization checks
before delivery. Changes with no user-visible text must still verify that existing localization
behavior is preserved. Keep technical identifiers and asset identities unlocalized; project
documentation remains in English as required above.

## Current implementation policy

Follow the [current implementation policy](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#current-implementation-policy). Prototype data must still be extracted from real sources and
hardcoded: other players may not have the same prototype files. Offline identification remains
required, but compact measured profiles/recipes may replace exhaustive per-vertex literals.
Do not depend on the original prototype Prefab at generation time. Preserve the required resource
graph through permitted packaged data and per-bridge dependency snapshots.

Style branches are optional. Implement movable families incrementally, deploy coherent buildable
increments, and choose CPU/GPU execution from measured needs. Continue unblocked coding while
human visual acceptance is pending. Preserve correct native behavior, all-LOD geometry, original
road bones, independent persistence and multilingual adaptation.

## First rule: fix reported bugs immediately

Deleted bridge files must never be read again through stale asset registrations, previews
or pending consumers. Follow [the deleted-file lifecycle requirement](docs/agent-contract/PROJECT_CONTRACT.md#deleted-bridge-files-must-never-be-read-again), including partial-deletion and exception paths.

Read the [implementation requirements](docs/agent-contract/PROJECT_CONTRACT.md#implementation-requirements)
in full: contract routing, immediate bug fixes, compatibility review, simplest implementation,
external-input boundaries and explicit internal errors remain mandatory. Bug reports authorize
implementation and verification without repeated permission. Documentation does not cancel
authorized repairs; report missing evidence while continuing independent work.

## Computer-use tools are prohibited

Do not use computer-use tools for this project, including `mcp__cua_repl`, native
desktop control, or browser UI automation through computer-use. Use file tools,
command-line tools, or purpose-built APIs instead. If an operation requires UI
interaction and no permitted alternative is available, ask the user to perform it
manually; do not bypass this prohibition through another UI automation tool.

## Native game UI windows

All UI windows must be created through Cities: Skylines II native UI APIs and components,
including panels, dialogs and file/directory pickers. Do not build custom HTML/CSS window
shells or substitute operating-system dialogs. Compose mod-specific content within native
containers and preserve native focus, input and modal behavior. Follow
[Project contract: Native game UI windows](docs/agent-contract/PROJECT_CONTRACT.md#native-game-ui-windows).

## Performance and deployment

All material work must follow the [material generation contract](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md):
full reuse of an original material must create no new material;
preserve native texture resolution; tile genuine repeats at original density. Section 2 permits
verified prototype non-tiling UVs to stretch with geometry, retaining authored UVs and identity.
Missing repeat evidence is not proof. Prepare known constant conversions once offline; copy and
bind finished assets for every bridge, part, LOD, preview and export.
Texture content and per-use sampling parameters must be stored separately. Preserve native
Surface/texture/VT identities and complete mip dependencies; sampling metadata must be applied
by a verified native rendering path, not merely recorded. The same texture under different sampling methods must use one shared texture payload file
per style; retain sampling differences in UVs/native material settings or sampling metadata.
An archive containing separate resampled copies does not satisfy consolidation. Follow material
generation contract section 4 for source identity, native sampling and verification requirements.
Section 4 permits bounded runtime composition for genuinely variable content and necessary conversion
of selected third-party road/track inputs unavailable before runtime. Prefer full reuse, then
descriptor-only conversion; missing known prepared resources never qualify for these exceptions.
Preserve section 2 sampling, channels, mips and current-owner dependencies, with no cross-bridge
generated-result cache or per-sampler baking.
The complete persisted material dependency graph of one generated bridge must not exceed
10,000,000 bytes (working budget 5–10 MB; do not pad smaller outputs). The complete
compressed Paradox Mods deliverable for all included styles must not exceed 50,000,000 bytes
(50 MB), including code, materials and archive overhead. This is a package acceptance gate,
separate from the generated-owner material limit. Prepared-directory bytes do not prove either
limit. Continue measuring the complete bridge footprint, including non-material assets.
Prepared materials belong in `assets/Materials/<BridgeStyleId>/`, separately for each bridge
style, and must ship as `Materials/<BridgeStyleId>/` beside the mod DLL with all dependencies.
Keep only payload-bearing material directories; empty folders and placeholder-only folders are prohibited.

Bridge geometry uses the [CPU affine execution contract](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution).
Preserve measured XYZ coefficient arrays and sparse recipes; run independent LOD/vertex work
through `AffineGeometry`, with native mesh/asset operations on the main thread. Regeneration
must retain these kernels and exact arithmetic order. Small or dependent work may remain serial;
matrix execution does not authorize geometry, bone, material or persistence changes.

Follow bridge generation contract sections 13 and 23. Choose CPU/GPU placement and parallelism from
correctness, native API affinity and measured cost; GPU conversion is not an implementation gate.
Build and deploy coherent buildable code increments, not every intermediate edit. Stop and verify
the game before replacing loaded binaries, back up original installed files and verify output hashes.
Preserve healthy generated assets unless a concrete incompatibility requires ownership-scoped recovery.
Documentation-only changes require no build, game shutdown or deployment.

## Parallel agents for multi-bridge tasks

For multi-bridge work, assign one independent sub-agent per bridge, batching when slots are full.
A separate shared-logic sub-agent owns shared implementation, integration and common verification.
The main agent manages assignments, requests, progress and reports only; it does not implement
bridge-specific or shared changes. Sub-agents communicate only through the main agent, which
relays every shared interface/behavior change and required consumer adjustment to affected agents.
Keep dependencies pending asynchronously and continue independent work.

Follow the complete [coordination and environment requirements](docs/agent-contract/PROJECT_CONTRACT.md#implementation-requirements).
Provide independent Unity projects, baker copies, writable caches, outputs and logs first; share
only when independent environments cannot be provided, and tell agents their assigned setup.
Each agent may schedule baking in its own environment. The main agent owns environment lifecycle
and verified cleanup after completion: preserve deliverables outside environments, confirm processes
have exited, then delete task-created environments while preserving pre-existing tools, licenses,
source files and retained deliverables.

## GitHub publication is optional

GitHub access, pushing branches, opening pull requests and remote synchronization are not
requirements for ordinary development or local delivery. Developers may deliver reviewed
local changes or a patch without GitHub permissions. Publish to GitHub only when explicitly requested
by the user and permitted by the account. The explicit release workflow below is unchanged; its
GitHub synchronization requirement applies to authorized releases, not ordinary contributions.
Local backups, builds and verification remain required where applicable to code and deployment.
See [the project policy](docs/agent-contract/PROJECT_CONTRACT.md#optional-github-publication).

## Release workflow

The release version is specified by the user. Never invent or automatically increment it.
When the user requests a release, complete these four steps in order:

1. Remove redundant and dead code, then build and run the applicable checks using the user's version.
2. Commit and upload the release code to every existing GitHub branch; verify the remote results.
3. Upload that version to Paradox Mods with a short changelog. Verify that the corresponding version
   is available; an accepted upload that is still processing is not a completed release.
4. Download the corresponding published version from Paradox Mods and replace the local Bridge Builder
   installation with it. Back up the previous installation and verify the installed files against the
   downloaded package. A local build is not a substitute for the Paradox Mods package in this step.

## Scope

This file applies to the entire repository. It contains the repository-level rules that Codex must
load before doing any work. More specific `AGENTS.md` files may add instructions for their directory,
but they may not weaken or replace this file or the project contract.

Project-wide rules are defined in
[PROJECT_CONTRACT.md](docs/agent-contract/PROJECT_CONTRACT.md). All bridge generation rules and
their specialist routing are defined separately in
[BRIDGE_GENERATION_CONTRACT.md](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
Both are normative. The summaries here do not replace their detailed requirements. Numbered
technical references below point to the bridge generation contract; section 20 alone remains in
the project contract as the project metadata rule.

## Mandatory reading

Read the required documents completely before the first edit. Do not rely on memory or on a previous
session.

| Work being performed | Required instructions |
| --- | --- |
| Any bridge, mesh, prefab, tower, cable, LOD, geometry, export or generation change | This file, the complete project and bridge generation contracts, and the nearest directory-level `AGENTS.md` |
| Runtime code under `src/BridgeBuilder` | This file, the complete project and bridge generation contracts, and [`src/BridgeBuilder/AGENTS.md`](src/BridgeBuilder/AGENTS.md) |
| Geometry identification or metadata generation under `tools/GeometryMetaprogram` | This file, contract sections 1–9 and 13, and [`tools/GeometryMetaprogram/AGENTS.md`](tools/GeometryMetaprogram/AGENTS.md) |
| Diagnosis or visual validation | Contract sections 1 and 6–9, plus the nearest directory-level `AGENTS.md` |
| Double-deck bridge work | The complete project and bridge generation contracts, especially section 11 |
| Movable bridge work (draw, lift, pedestrian or other mechanisms) | The complete project and bridge generation contracts and [MOVABLE_BRIDGES_CONTRACT.md](docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md) |
| Git, branching, commit or integration work | Contract section 12 before changing files or branches |
| Build, install or generated-asset cleanup after a source update | Contract sections 6 and 13 |
| Instruction-document maintenance only | This file and every instruction file being changed |

When a task spans more than one row, combine their reading requirements. When scope is uncertain,
read the complete project and bridge generation contracts.

## Non-negotiable project rules

Read all numbered bridge requirements and the
[repository checklist](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#current-repository-checklist).
They remain normative; section 20 stays in the project contract.

## Elevated-road sidewalks: all bridge generators

Follow bridge generation contract section 25 for every fixed or movable bridge. A road may have no sidewalks,
a sidewalk on only one side, or unequal sidewalk widths. Resolve the actual elevated-mode road
composition and selected upgrades independently on each side. Adapt sidewalk-dependent components,
including inner bridge railings and bridge-authored sidewalk portions, to that side's actual
sidewalk presence, position and width. Do not generate them on a side without a sidewalk, mirror
one side, or impose the prototype's sidewalk width. Keep unrelated structural and outer safety
railings distinct. Apply the same layout to previews, saved bridges and every LOD.
Sidewalk portions embedded in fixed bases, end platforms or other composite components are also
subject to section 25. Only `Sidewalk` sections count as sidewalks; curbs do not. Adapt each side
independently without removing unrelated structural parts.

## Geometry invariants that must remain visible at repository scope

Recorded hardcoded numbers must use DLL-embedded typed arrays under bridge generation contract
section 2. Store uncompressed BBN1 resources by bridge type; let Paradox Mods compress the complete
publication package. On first demand, validate and read only the needed embedded resource into
class-owned static arrays without extracting files. Any development decompression must use
temporary files deleted on success or failure. Retain exact numeric bits
and ordering, and reconstruct large tables with bounded loops. Offline extraction remains required.
This storage migration does not change any bridge's geometry or runtime independence.

Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.

Generated meshes must use the prototype bridge's corresponding meshes as the shape and complexity reference at every LOD.
Excessive subdivision or triangle cutting is prohibited, including in converted road surfaces.
Follow bridge generation contract section 4's prototype-referenced mesh complexity rule;
necessary subdivision and high-detail-derived LOD permissions do not waive it.

User-approved movable conversion exceptions: where a merged structural LOD
triangle cannot represent the authored parts' separate width transforms, necessary triangle
subdivision is allowed. Preserve original vertices, materials and interpolated original
attributes; this does not permit changing the part-transform rule. Deriving, simplifying or supplementing LODs
from measured prototype high-detail geometry is permitted under bridge generation contract section 5,
including its scoped vertex/index/topology exception. The measured Golden ornament/support
exceptions are defined in section 5 and its linked specifications. Other structural, material and acceptance
requirements remain in force. Moving-road paint must be integrated into mechanism appearance;
independent marking objects and custom runtime shaders are prohibited. Material preparation follows the material generation contract. Curve segmentation, endpoint-fade and existing-atlas pattern differences remain
accepted. Follow the general method
and failure-prevention rules in the movable generation contract sections 7, 11 and 15.

Persistence also follows contract section 17: recursively copy external non-built-in asset
dependencies byte-for-byte with their original CIDs into `ImportedData/<bridgeUUID>/Dependencies/`.
Deduplicate by CID within one bridge only; different bridges retain separate copies. Stop at built-in
assets, preserve references and source files, and scope backup/cleanup to the owning bridge UUID.
Intentional identical-CID snapshots are not generated-prefab identity collisions. Never restore
in-memory integrity checks or quarantine as part of this persistence workflow.

Apply the complete [authored-part mapping and error-boundary rules](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre),
including deck-adjacent bases, centred posts, Draw03 trusses and the section 5 Golden exceptions.
Bridge-pier columns translate, never stretch. Offline membership governs every LOD; runtime
heuristics and silent internal-error fallbacks remain prohibited.

## Workflow

1. Inspect the branch, preserve unrelated changes and read all applicable instructions.
2. Inspect the real archetype and generated output before transformation changes. Missing samples
   leave visual acceptance pending; continue independent implementation without claiming a fix.
3. Work on a suitable branch using offline identification and immutable measured construction data.
   Apply each visual change consistently to the full-detail mesh and every LOD.
4. Build/deploy coherent code increments under bridge generation contract section 13, backing up
   installed originals and verifying hashes. Preserve healthy assets; scope recovery to demonstrated
   incompatibilities. Documentation-only work requires no build, shutdown or deployment.
5. Report automated evidence separately from human near/far, motion and cold-load acceptance.

## Maintaining these instructions

- Integrate every specification revision directly into its normative text. Do not add revision
  dates, changelogs or revision-history entries to specification files.
- Do not record or retain rollback information for specification revisions, including branch/HEAD
  baselines, rollback references or separate rollback records. Code and deployment backups and
  failed asset-transaction recovery remain governed by their own rules.
- Preserve effective requirements. Integrate user-authorized changes into the original rule and
  remove superseded contradictory text; do not retain obsolete instructions as alternative rules.
- Keep repository-wide rules in this root file. Put path-specific additions in the nearest directory
  `AGENTS.md`; a nested file is additive unless it explicitly tightens a rule for that scope.
- Keep this automatically discovered instruction chain below Codex's default 32 KiB combined limit.
  Store detailed requirements in the relevant contract and diagnostic evidence in supporting reports;
  route agents to the exact material they must read.
- Use a single H1 title, ordered H2 sections, CommonMark tables/lists, fenced or indented code only for
  literal commands and formulas, and repository-relative links.
- Do not add `AGENTS.override.md` as a permanent repository rule. Overrides take precedence and can
  silently hide the regular file in the same directory.
- When changing a rule, update this routing summary, the authoritative contract and any applicable
  directory-level file together.

## Post-load self-check

Follow bridge generation contract sections 17–19 and the runtime AGENTS.md for readiness, migration,
recovery/removal, source isolation and restart behavior, including its post-load self-check requirements.
Self-check must supplement missing copies of other mods' materials and their non-built-in dependencies,
even when live references are valid. Use the creation dependency traversal, preserve identities and
complete copies, and report supplementation as migration requiring restart.

Prepared material variants may share unchanged texture payloads inside their own bridge style's
`Shared/` directory under the material generation contract. Keep every style's packaged dependency
graph complete and preflight it before native registration.

## Documentation language and format

The movable-bridge requirements are defined in bridge generation contract section 21 and
`MOVABLE_BRIDGES_CONTRACT.md`. The six-lane mapping example is not a universal template.

Write all documentation in English; preserve literal identifiers, paths and quoted evidence.
Runtime localization is unchanged.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.

## Movable generation requirements

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–24](docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Read the [movable runtime invariants](docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md) and material
contract together: preserve original road bones, native material semantics and independent
persistence. Paint is a separate single-sided skinned mesh on the same mechanism with 0.001 m
clearance and matching stack/transform metadata. No receiver cuts for paint, child projectors,
receiver-only structural material copies or runtime synchronization dependency. Acceptance remains
specific to the tested style, input and build. Material conversion follows section 4's bounded rules.

Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.
