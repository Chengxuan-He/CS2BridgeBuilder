All documentation must be written in English.

### Packaged textures belong to road running surfaces

A packaged `.Texture` must have a measured road-surface consumer. Check the actual
faces, appearance type, LOD and native Surface channel that references it. Asphalt,
walkable sidewalk paving, running grating, running caps and selected-road paint
qualify only on their measured road-facing domains. A structural platform, curb
sidewall, truss, cable or underside uses its original game or DLC texture references.
This requirement also applies when a road and a structural face share the same atlas.
A descriptor conversion needed for native rendering may bind original native channels.
It must never give a non-road consumer a dependency on a packaged texture.

Retain valid native UV projection, sampling and mip identities. A local repeating
atlas island that cannot be sampled at a requested width through its original
representation retains its authored coordinates. Record any resulting change in
physical density. Do not bake a structural repeat or wrap individual vertices into
an atlas chart to fill that gap. This supersedes earlier structural repeat permissions,
including the earlier Lift05 profile recipe. Independently persisted external owner
copies remain governed by the dependency persistence contract.


Follow [player-controlled generation](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#player-controlled-generation-without-structural-rejection-gates): do not reject a generation request
because of structural shape, width, signed scale, degeneracy, overlap, clearance or appearance
quality. Such checks belong to offline verification or non-blocking diagnostics. Material
no-match selects Original. Preserve actual file/schema/index, nonrepresentable arithmetic,
write and native API errors; never fabricate successful output. This applies to all bridge
families and LODs without changing historical dependency recovery.

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

Follow the [all-bridge mapping rule](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode)
and the complete [material contract](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md):


Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose roads need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable road-processing scope.

Resolve the elevated road-level branch once at the common generation entry, before width
measurement, cloning or style construction. Every fixed and movable bridge uses that elevated
context for geometry, bridgeheads, decks, materials and every LOD. Do not enumerate, rank or
fall back to ground/other road-level branches; no downstream re-selection.
Keep all physical contributions and section-local branches within the selected road branch.
Material matching within it is noexcept: a corresponding match selects that route; no match
selects the original source-material route, without selecting a different road branch.
The [complete material pipeline](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#complete-material-processing-without-propagated-exceptions) must return explicit outcomes through
resolution, adaptation, assembly, persistence, preview and creation without propagated exceptions.
Valid unknown materials use compatible originals, equivalent conversion, or a visible processed
native compatibility material with non-blocking difference diagnostics. Compatibility gaps must
not reject valid input. Name/CID whitelist adapters, incompatible blind shader binding and
unreported appearance losses are prohibited; genuine resource/IO failures remain explicit.
Branch mechanism decks and native network bridgeheads
by their own source/domain and render context; do not classify a mixed piece from a section name.
Require both actual receiver classification and matching measured prototype material at the target
physical part. Moving deck, fixed cap, moving sidewalk, fixed platform and external approach do
not inherit a bridge-wide material override; unmatched external approaches retain Original.
Keep deliberate part-specific appearances and all-LOD PBR/alpha, density and paint handling.
Cover each replaced component's measured edge/sidewall/cap material domains as well as its top;
do not omit matching sidewalk or median edges or blanket-replace unmatched edges.
Resource corruption and native realization failures return explicit non-exception failure results. Follow
[the material routing rule](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#total-material-routing-by-part-and-actual-source).


- Processed movable-road appearances supported by the prototype use prototype content tiled at original resolution and
  physical texel density, never stretched, at every LOD and in fixed caps/finite terminals.
  Remove embedded road markings from every road-material channel/mip; retain intended selected-road
  paint through its native path (native rigidly attached source decals for movable mechanisms).
- Test asphalt, sidewalk and wear support independently. Wear uses the same complete measured
  source and matching same-source background. Align its measured visual center to actual native
  lane centers; preserve physical width, internal relationships and longitudinal density.
  Do not infer lanes from paint or place wear outside applicable vehicle lanes.
- Section 3 permits bounded offline authored road repeats from pure original road content, with
  aligned PBR donor coordinates and complete periodic mips. Record new periods as authored, not
  original evidence. Keep source/PBR, density and phase coherent across domains sharing the same evidenced prototype appearance.
- Non-road materials retain original identity/modes by default; only evidenced repeat domains
  must use original native references, including at widths beyond a finite atlas chart.
  Unsupported road types use compatible originals or corresponding native object road materials
  under section 4, preserving source layers/projection where representable, otherwise reporting compatibility differences.
- No full-span pixels, new period cuts, arbitrary downsampling, unauthorized custom shaders or changed original
  assets. Changed derivatives receive new CIDs. Preserve complete PBR, frames, filtering and mips;
  no global tolerance relaxation or acceptance from metadata alone. Integer-grid resampling and
  bounded two-period storage follow section 3, not an unrestricted boundary-blending permission.
- Prepare known constants outside the game; runtime copies/binds them. Section 4's bounded variable
  or unknown-selected-input conversions do not excuse missing known assets or cross-owner caches.
  Separate texture content from sampling; one identical payload per style/current owner, not per use.
- Measure complete persisted owner material dependencies for information, without a per-owner ceiling; complete official
  SDK package ZIP at most 55,000,000 bytes. Prepared-directory bytes prove neither. Preserve independent
  native cold loading and healthy saved owners; report remaining quality/budget gates honestly.
- Package complete immutable resources in `assets/Materials/<BridgeStyleId>/` and ship beside the
  DLL as `Materials/<BridgeStyleId>/`. Resolve sources relative to the loaded mod, not developer or
  other-owner paths. Keep no empty/placeholder material directories. Preflight full native dependency
  graphs before registration; byte-identical shared payloads stay within each style. Section 5 permits three identical skinned-road paint channels under read-only
  `Materials/Shared/Paint/<cid>.Texture` and the native cache under `Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` plus CID;
  each generated owner still snapshots only its required dependencies independently.

Structure coordinate generation uses the common data-driven SIMD pipeline in the bridge
generation contract's CPU affine execution section. Styles supply binary floating-point
parameters, memberships and topology recipes, not separate numerical callbacks or SIMD
adapters. Use actual interleaved XYZ SIMD stages and direct owned target-buffer consumption;
runtime expression-recognition fast paths and axis-column execution are prohibited.
Preserve existing runtime intermediate precision and ordered float-rounding
boundaries; verify real packed execution and all-LOD numerical equivalence.

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

Preview and formal creation follow [complete request-local road/lane LOD reuse](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#reuse-identical-selected-road-computation-across-lods),
from source resolution and sampling through final road output, not only a downstream GPU cache.
Preview follows [background execution and ready-work scheduling](docs/agent-contract/Bridge-Preview-Specification.md#ready-work-budget-and-detached-background-computation):
all safely detached pure CPU work belongs on bounded workers; native API boundaries remain on
the main thread. These are required outcomes, not claims of current implementation completeness.

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

Follow bridge generation contract section 8's pedestrian truss rules:
PedestrianDraw02's central truss scales as a complete assembly in X; both
pedestrian movable styles distribute their moving-deck underside longitudinal
trusses evenly across X with rigid member sections and an unchanged central
longitudinal truss. Use measured membership at every LOD, not runtime inference.

Lift01 and Lift02 central-truss memberships follow the explicit complete-assembly rule in
bridge generation contract section 8, overriding default own-span stretching,
side-part translation and base mapping for those measured members. Internal
braces and narrow connectors share their complete assembly's `x -> k*x`; only the outermost
edge trusses retain their original mapping. Record assembly spans and connection
residuals offline, apply the rule at every LOD including far supplements, and
preserve bones/material modes with the corresponding affine normal/tangent frame.
Measure each style's complete source and target assembly spans; never use a small
connector's thickness or another style's span as its scale denominator.


For movable-bridge missing-face repairs and audits, follow the complete
[source-face coverage requirement](docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#source-face-coverage-and-prevention-of-missing-structure).
Preserve measured non-road parts, remove superseded fragments when restoring original faces,
and verify actual final indices/attributes plus independent renderer LOD ownership. Coarse
road-height/bounds deletion and capacity fallbacks that cause part-visibility gaps are not permitted.

Recorded hardcoded numbers must use DLL-embedded typed arrays under bridge generation contract
section 2. Store uncompressed BBN1 resources by bridge type; let Paradox Mods compress the complete
publication package. On first demand, validate and read only the needed embedded resource into
request-owned arrays without extracting files. Reuse demanded resources within that request,
then release decoded arrays and all derived parameter caches on completion, failure or cancellation.
Never preload unused bridge types or retain permanent static numerical holders. Any development decompression must use
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
exceptions are defined in section 5. Other structural, material and acceptance
requirements remain in force. Moving-road paint must be integrated into mechanism appearance;
Independent marking objects remain prohibited. The authorized dynamic material shader route is currently not implementable under the material contract's independent cold-load requirement; a continuing runtime synchronization controller remains prohibited. Material preparation follows the material generation contract. Curve segmentation and endpoint-fade differences remain accepted; original decal artwork must be retained. Follow the general method
and failure-prevention rules in the movable generation contract sections 7, 11 and 15.

Persistence also follows contract section 17: recursively copy external non-built-in asset
dependencies byte-for-byte with their original CIDs into `ImportedData/<bridgeUUID>/Dependencies/`.
Deduplicate by CID within one bridge only; different bridges retain separate copies. Stop at built-in
assets, preserve references and source files, and scope backup/cleanup to the owning bridge UUID.
Intentional identical-CID snapshots are not generated-prefab identity collisions. Never restore
in-memory integrity checks or quarantine as part of this persistence workflow.

Apply the complete [authored-part mapping and error-boundary rules](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre),
including deck-adjacent bases, centred posts, Draw03 trusses and the section 5 Golden exceptions.
For approved longitudinal and transverse member maps, retain measured widths, contact boundaries, closure faces and LOD coverage.
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
persistence. All retained selected-road decals use original native decal appearance on rigid child
components attached through the mechanism's native bone connection IDs. Preserve source alpha
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

Follow [dynamic material shaders](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders). **Not implementable under current requirements:** the current game cannot independently restore the generated shader/template from its owner-local assets with Bridge Builder disabled or uninstalled. Dynamic source compilation and bounded sampling succeeded, but complete multilayer adaptation remains unsolved under the current game/project constraints: restoration of the custom shader/template would depend on Bridge Builder registration. The complete custom-shader solution is unavailable and must not be deployed or described as a density fix. The user's permission to investigate dynamic shaders remains; it does not waive independent cold loading. Independent loading with the mod disabled or uninstalled is a non-negotiable project constraint at all times. Do not offer a startup loader dependency or relaxation of independence as a trade-off. Experimental or partial success never substitutes for it. Reconsider implementation only when an evidenced native loading capability satisfies this unchanged constraint. A fixed precompiled shader is not dynamic generation. Editor compilation or raw GPU bytecode alone does not establish Player loading or independent cold loading. No continuing renderer synchronization, custom runtime controller, game launch or launcher launch is authorized.

Selected-road decals follow [source identity and coverage](docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#source-preserving-selected-road-decals): preserve each original texture, alpha and evaluated UV; no name/type/CID filtering or generic white/yellow replacement. Distinct transparent layers cannot erase each other by footprint alone.

The movable mechanism includes its stationary end caps: selected-road deterioration
must not be added there as object decals. Only measured prototype WheelTrack road
composition supplies mechanism wear. External fixed elevated approaches retain
their original native network wear; this is not a rule to erase wear from fixed roads.
Line art, rail artwork and colored-lane decals retain their source-preserving route.
