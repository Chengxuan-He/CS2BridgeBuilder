# Repository Agent Instructions

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

All documentation must be written in English.

### Packaged textures belong to road running surfaces

Follow [Packaged textures belong to road running surfaces](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#packaged-textures-belong-to-road-running-surfaces) for this requirement.

Follow [player-controlled generation](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#player-controlled-generation-without-structural-rejection-gates): do not reject a generation request
because of structural shape, width, signed scale, degeneracy, overlap, clearance or appearance
quality. Such checks belong to offline verification or non-blocking diagnostics. Material
no-match selects Original. Preserve actual file/schema/index, nonrepresentable arithmetic,
write and native API errors; never fabricate successful output. This applies to all bridge
families and LODs without changing historical dependency recovery.

Unless otherwise specified, all project documentation must be saved as Markdown (.md).
Backups must contain only the original files being backed up. Do not add any other files,
including documentation, README files, manifests, checksum files or additional metadata.

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

For player-requested bridge removal, follow [Bridge deletion](docs/agent-contract/BRIDGE_DELETION.md).

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

Read the complete [material contract](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md): elevated branch selection, noexcept source/receiver routing, original non-road references, prototype-supported road processing, all-LOD PBR/alpha/mips, source-density tiling, marking removal and wear, allowed runtime conversions, packaged/shared resources and owner-local snapshots are mandatory. Do not replace this contract with source-name/CID adapters or geometry-only checks. Its owner-byte reporting has no ceiling; the official SDK ZIP limit remains 55,000,000 bytes.

Follow [CPU affine execution](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution): shared interleaved XYZ SIMD, measured binary coefficients/memberships, direct owned buffers, original arithmetic/rounding and all-LOD equivalence. No runtime expression recognition, axis-column execution or per-style numerical adapters. Native asset access stays on the main thread; measured cost and correctness govern CPU/GPU placement and bounded parallelism. GPU conversion and style branches are not implementation gates.

Follow generation contract sections 13 and 23: build/deploy coherent increments, verify the game is absent, back up installed originals and verify hashes. The user closes/starts the game under the manual-start rule below. Preserve healthy bridges; only demonstrated incompatibility permits ownership-scoped recovery. Documentation-only changes require no build, shutdown or deployment.

[Request-local road/lane LOD reuse](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#reuse-identical-selected-road-computation-across-lods) covers source resolution through final output. [Preview execution](docs/agent-contract/Bridge-Preview-Specification.md#continuous-background-generation-and-asynchronous-native-dispatch) uses continuous detached CPU work and asynchronous native dispatch, without artificial frame slicing or a 6 ms budget. Real GPU/render completion may await engine updates. Batch lane decoding/deformation and decal volumes, preserve cancellation and owned data, and render the measured physical viewport with supersampling. These are requirements, not claims of live acceptance.

## Parallel agents for multi-bridge tasks

For multi-bridge work, assign one independent sub-agent per bridge, batching when slots are full.
A separate shared-logic sub-agent owns shared implementation, integration and common verification.
Prioritize reusing already-created sub-agents to reduce context overhead: resume the existing
shared-logic sub-agent for shared work, and resume the existing sub-agent assigned to the same
bridge for that bridge's work. Completion of a previous assignment is not a reason to create
a replacement. Create a new sub-agent only when no suitable existing agent is available;
do not substitute another bridge's agent merely because it is idle.
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

## Game and launcher startup is manual

Agents must never start Cities: Skylines II or its launcher. This applies to executables, Steam commands and URLs, shortcuts, scripts, UI automation and delegated actions. Agents cannot reliably reproduce a normal player launch. Explicitly decline any request to launch either application. If verification requires a launch or restart, stop at that step immediately and ask the user to perform it manually. Resume verification after the user has started the game. Do not close a player-started instance to facilitate testing.

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

Read generation contract sections 2, 4, 5 and [8](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre), plus movable contract sections 7, 11, 15 and 19.

- PedestrianDraw02's central truss scales as a complete X assembly. Both pedestrian movable styles distribute underside longitudinal trusses evenly across X, retaining rigid member sections and the unchanged central truss. Lift01/Lift02 measured central trusses use complete-assembly scaling; outermost edge trusses retain their recorded mapping. Preserve measured memberships and all LODs.
- Apply [source-face coverage](docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#source-face-coverage-and-prevention-of-missing-structure): retain non-road faces, remove superseded fragments, verify final indices/attributes and independent renderer LOD ownership. No coarse height/bounds removal or capacity fallbacks that hide parts.
- Use on-demand DLL-embedded, uncompressed BBN1 arrays and request-local parameter caches. Validate exact numeric bits/order; release on success/failure/cancellation. No unused-style preload, permanent numerical holders or runtime Boolean operations with prototype geometry. Development decompression uses temporary files removed on success/failure.
- Prototype meshes govern shape and complexity at every LOD. No excessive subdivision, including road conversion. Section 5 permits necessary subdivision of merged structural faces to express separate authored transforms, and measured high-detail-derived/supplemented LODs; preserve original attributes/materials and mapping rules. Golden ornament/support exceptions remain scoped there.
- Preserve authored widths, contact boundaries, closure faces, deck-adjacent bases, centred posts and Draw03 trusses. Pier columns translate, never stretch. No runtime membership inference or silent internal-error fallback.
- Moving paint, poles, lamps, cables and ordinary nested meshes use the mechanism's native motion sample. A parent connection ID is insufficient. Only the explicit native attachment exceptions retain historical behavior. No transform-tracking controller. Keep source decal artwork; accepted curve-segmentation/endpoint-fade differences remain scoped by the material/movable contracts.
- Section 17 requires byte-identical external dependency snapshots with original CIDs under each owner UUID, deduplicated only within that owner and stopping at built-ins. Preserve source files/references. Intentional copied-CID duplicates are not generated-identity collisions; do not restore in-memory integrity checks or quarantine.

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

## Source input ownership

Follow [source input and independent output ownership](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#source-input-and-independent-output-ownership): accept valid other-owner copies as inputs,
without a backend provenance gate. Persist independent target-owner snapshots and preserve
all write/delete boundaries; source access never authorizes mutation of the source.

## Post-load self-check

Follow [self-check result reporting](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#self-check-result-summary): count each UUID once per run
as migrated to the latest format, removed with complete backup, removed without complete backup,
or unfinished. Show only nonzero categories with counts and one final restart reminder in a native
dialog, localized in all twelve languages. Successful no-action runs stay silent, including manual
checks. Reset reporting each run while retaining session stale-read protections; never invent an
owner count for a global exception. Backup failure still falls through to deletion.

Follow bridge generation contract sections 17–19 and the runtime AGENTS.md for readiness, migration,
recovery/removal, independent output ownership and restart behavior, including its post-load self-check requirements.
Self-check must supplement missing copies of other mods' materials and their non-built-in dependencies,
even when live references are valid. Use the creation dependency traversal, preserve identities and
complete copies, and report supplementation as migration requiring restart.

Prepared material variants may share unchanged texture payloads inside their own bridge style's
`Shared/` directory under the material generation contract. Keep every style's packaged dependency
graph complete and preflight it before native registration, including implicit Surface-to-MidMips
membership through the narrowly permitted shared read-only source cache. This does not permit
cross-style Texture storage or sharing generated-owner files.

## Documentation location and authority

All project documents must reside in the appropriate subdirectories of `docs/`:
contracts in `docs/agent-contract/`, maintained designs and references in `docs/design/`,
guides and publishing documents in their respective directories, and historical audits,
incidents and style evidence in `docs/archive/`. Root `AGENTS.md` and `README.md` remain
instruction and navigation entry points; directory-level `AGENTS.md` files remain scoped
instructions. They must not become duplicate design documents.

Chat `outputs/` is not a maintained project-documentation location. Do not maintain
project designs there, duplicate their bodies there, or create pointer documents there
as substitutes for project navigation. Keep each design's canonical text in one project
location and link to it from the appropriate project index. Raw test artifacts and
previously delivered evidence are not automatically current project documentation.

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
persistence. Retained selected-road decals must use the authorized direct-skeleton shader route with
source decal appearance; the historical relative-child route is not the target motion implementation. Preserve source alpha
blending, artwork and evaluated UV density; partition footprints by actual motion/support domains.
This user-authorized route supersedes the former child-projector prohibition. No receiver cuts,
receiver-only structural material copies or runtime synchronization dependency. Acceptance remains
specific to the tested style, input and build. Material conversion follows section 4's bounded rules.

Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.

## Offline source-atlas repeat materials

Follow [supported-road tiling](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#prototype-supported-road-surfaces-must-tile),
[all-LOD marking removal](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#remove-embedded-road-markings-at-every-lod)
and [offline source-atlas derivation](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#offline-source-atlas-repeat-materials).
Road repeats may be authored from pure prototype road content at original texel density;
structural surfaces retain original native texture references.
Preserve actual shape/motion/material boundaries, native loading, complete PBR/mips, informational owner measurements and the package byte limit.
Keep original assets unchanged and assign new identities to changed derivatives. This specification
does not certify implementation, quality or acceptance. No runtime strings change with these rules.

## Dynamically generated persistent material shaders

Follow [dynamic material shaders](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders).

Selected-road decals follow [source identity and coverage](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#source-preserving-selected-road-decals): preserve each original texture, alpha and evaluated UV; no name/type/CID filtering or generic white/yellow replacement. Distinct transparent layers cannot erase each other by footprint alone.

The movable mechanism includes its stationary end caps: selected-road deterioration
must not be added there as object decals. Only measured prototype WheelTrack road
composition supplies mechanism wear. External fixed elevated approaches retain
their original native network wear; this is not a rule to erase wear from fixed roads.
Line and rail artwork retain their source-preserving route. Mechanism colored asphalt,
including stationary running caps, uses ordinary asphalt replacement without reapplying
the selected road's lane-color overlay; external native approaches remain unchanged.

Native light effects and the four attachment exceptions in movable contract section 19 retain their existing native attachment/BoneHistory behavior. Ordinary nested meshes use direct skeleton binding; no new mod-driven tracking controller is permitted.

Follow [preview execution and viewport capture](docs/agent-contract/Bridge-Preview-Specification.md) for continuous background dispatch, batched lane preparation and viewport-sized supersampling.