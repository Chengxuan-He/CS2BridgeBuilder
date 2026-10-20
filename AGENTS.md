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

Follow the [current implementation policy](docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#current-implementation-policy-2026-10-19). Prototype data must still be extracted from real sources and
hardcoded: other players may not have the same prototype files. Offline identification remains
required, but compact measured profiles/recipes may replace exhaustive per-vertex literals.
Do not depend on the original prototype Prefab at generation time. Preserve the required resource
graph through permitted packaged data and per-bridge dependency snapshots.

Mandatory per-style branches, blanket movable-family deferral, per-edit deployment, GPU-first
implementation and human-feedback gates on further coding are removed. These process changes
supersede conflicting historical routing records. They do not waive correct native behavior,
all-LOD geometry, original road bones, independent persistence or multilingual adaptation.

## Current documentation baseline

Bridge revision history is retained in the bridge generation contract under "Retained repository routing records".

## First rule: fix reported bugs immediately

The current requirements are defined in the numbered contract sections:

- Section 22: whenever a new bridge style is created, immediately synchronize the UI and connect
  the new style to the generator router. Update its catalogue, preview, creation actions and
  management labels in the same implementation increment. Provide
  style names in every supported UI language and refresh them on locale changes; keep IDs stable
  and unlocalized. A generator without its working UI entry is not a completed style.
  All registered styles use identical behavior regardless of completeness: never display unfinished-
  style notices or disable creation/preview because implementation is incomplete. Requests must reach
  the router; a missing generator must explicitly fail with ERROR through normal operation-failure
  handling, identifying the style and missing route. No silent return or substitute generator.
- Section 19: self-check attempts migration first, preserves UUID/CID and valid values, then backs
  up/removes only bridges whose migration failed or is impossible. Backup/move failure falls back
  to deletion of remaining selected source paths. Verify removal, report inaccessible paths, process
  bridges independently, never unregister live prefabs during self-check, and require restart.
- Section 20: the mod metadata name and any metadata-based heading are exactly `Bridge Builder`.
  Localized in-game labels remain permitted. Documentation and publishing drafts are in English.
- Section 16: preserve native construction, elevation and upkeep fees. No manager-defined totals,
  fee rewrites, multipliers, offsets, floors or pricing-only copies. Legacy conversion removes the
  custom cost component without guessing original fees or deleting referenced native copies.
- Sections 17 and 19: generated bridges are independent native assets; Bridge Builder is their
  manager, not a required runtime dependency. Preserve native unlocks, locales, thumbnails and fees.
  The known double-deck rail-seam limitation remains unfinished compatibility work.
- Section 19: legacy conversion is additive and preserves bridge/deck identity and geometry.
  Generation and migration share naming/layout helpers; migration is backed up and idempotent.
  Startup notices distinguish migration from damaged-asset repair/removal and request restart.
- The contract's legacy naming exception allows per-bridge copies of reachable pre-UUID components
  from the canonical root's CID graph, with new private CIDs and rewritten file references. Append
  `-b{uuid}` to the first space-delimited name token; retire original files only after reference
  checks. Do not mutate live prefabs or generalize this exception to ordinary dependency snapshots.

Deployment cleanup is governed by section 13, not by migration or manual deletion rules.

Every modification requires a compatibility review across the entire codebase, not only the edited
files. Trace all affected producers, consumers, callers and alternate branches; update incompatible
paths together. Check existing assets, persistence, loading, migration, cleanup and diagnostics when
affected. For example, changing bridge identification must also check every generated asset naming
and saving path. Compilation or preview success alone does not prove compatibility: perform the
relevant end-to-end checks and explicitly report any unverified in-game behavior.

Prefer the simplest implementation that satisfies the request. Overengineering is prohibited:
unnecessary complexity becomes a primary source of bugs that are only discovered through player
reports. For example, when Prefabs can be selected by string matching, using metadata to select them
is forbidden. Do not introduce extra abstractions, state, prerequisites or validation layers when
the direct implementation is sufficient.

Excessive defensive programming is prohibited. Defend boundaries involving external logic or data
outside the mod's control, including generated bridge Prefabs that can subsequently be damaged.
Validate such input and contain external failures where necessary. Internal mod logic should not
fail during correct operation: when an internal invariant is violated, raise an explicit ERROR
and preserve its diagnostic context. Throw an exception when appropriate; do not add special cases,
silent returns, fallback values or exception swallowing merely to prevent an internal error from
surfacing. Fix the underlying defect rather than hiding it. This distinction replaces the previous
blanket prohibition on explicit `throw` statements.

Fix every reported bug immediately. A user bug report is a request to implement a fix. After diagnosis,
immediately proceed with implementation and verification; do not stop at acknowledgment, analysis,
a repair plan or a proposed next step, and do not ask the user to authorize the same fix again.

Only an explicit user instruction to stop code generation permits the agent to stop the ongoing
code repair before completion. Otherwise, code repair remains the first priority. No other operation
may replace code repair or cause the agent to terminate it early, including contract updates,
documentation edits, analysis, status reports or verification planning. Treat these as supporting
work within the ongoing repair, never as cancellation or replacement of the authorized code task.
After such supporting work, continue implementation and verification without requesting renewed
authorization. Do not infer an instruction to stop code generation from a request to update a contract
or perform another supporting operation. A genuine blocker must be handled under the following
paragraph; it does not cancel the repair or make supporting work a substitute for it.

Fixes must still satisfy archetype-evidence, branching, safety and acceptance requirements. If evidence,
permission or an in-game action that only the user can perform is genuinely missing, first complete all
unblocked repair work, then identify the specific blocker and required action. Do not guess at the
implementation, bypass the contract or claim that an unverified change is a completed fix.

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
[Project contract: Native game UI windows](docs/agent-contract/PROJECT_CONTRACT.md#native-game-ui-windows-2026-10-19).
Baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a` (2026-10-19).

## Performance and deployment

Follow bridge generation contract sections 13 and 23. Choose CPU/GPU placement and parallelism from
correctness, native API affinity and measured cost; GPU conversion is not an implementation gate.
Build and deploy coherent buildable code increments, not every intermediate edit. Stop and verify
the game before replacing loaded binaries, back up original installed files and verify output hashes.
Preserve healthy generated assets unless a concrete incompatibility requires ownership-scoped recovery.
Documentation-only changes require no build, game shutdown or deployment.

## GitHub publication is optional

GitHub access, pushing branches, opening pull requests and remote synchronization are not
requirements for ordinary development or local delivery. Developers may deliver reviewed
local changes or a patch without GitHub permissions. Publish to GitHub only when explicitly requested
by the user and permitted by the account. The explicit release workflow below is unchanged; its
GitHub synchronization requirement applies to authorized releases, not ordinary contributions.
Local backups, rollback baselines, builds and verification remain required where applicable.
See [the project policy](docs/agent-contract/PROJECT_CONTRACT.md#optional-github-publication-2026-10-19).

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

The detailed checklist formerly here is preserved in the bridge generation contract under
"Detailed repository rule checklist (2026-10-16)". It is superseded by the current implementation policy where conflicting. Read the complete
numbered contract; this index does not replace or weaken its rules.

- Sections 1–5: preserve the measured archetype, its component/reference graph, native raw mesh
  attributes and width-only structural transformation; derive owned structural geometry.
- Sections 6–9: inspect real geometry at every LOD, diagnose by complete dumps/native IL, apply
  the mandatory authored-part crossing rule, and do not claim visual acceptance from compilation.
- Sections 10–11 and 21–22: preserve actual variant/arrangement requirements and registered-style
  UI parity; missing generator routes raise ERROR through normal operation handling.
- Sections 12–14: use a suitable branch and preserve unrelated changes. Deploy coherent increments
  and hash-check; recover only demonstrably incompatible owned assets. Record instruction-edit baselines and
  establish the full-span width invariant from real samples; no runtime audit corrections.
- Sections 15–20: preserve native lights, fees, independence, identity, dependency snapshots,
  UUID-string ownership, migration/removal order, source isolation and metadata naming.
- Sections 23–24: select CPU/GPU execution from measured needs, overlap safe independent work,
  and verify comparable total movable-bridge size including dependencies.

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

Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.

Generated meshes must reference the prototype bridge's corresponding meshes at every LOD.
Excessive subdivision or triangle cutting is prohibited, including in converted road surfaces.
Follow bridge generation contract section 4's prototype-referenced mesh complexity rule;
necessary subdivision and high-detail-derived LOD permissions do not waive it.

User-approved movable conversion exceptions (2026-10-16): where a merged structural LOD
triangle cannot represent the authored parts' separate width transforms, necessary triangle
subdivision is allowed. Preserve original vertices, materials and interpolated original
attributes; this does not permit changing the part-transform rule. On 2026-10-17 the user revoked
the prohibition on high-detail-derived movable LODs: deriving, simplifying or supplementing LODs
from measured prototype high-detail geometry is permitted under bridge generation contract section 5,
including its scoped vertex/index/topology exception. Other structural, material and acceptance
requirements remain in force. Moving-road paint must be integrated into mechanism appearance;
independent marking objects and custom runtime shaders are prohibited. The GPU composition
exception below permits new textures composed from existing native textures and native descriptors. Curve segmentation, endpoint-fade and existing-atlas pattern differences remain
accepted. Earlier independent object-decal permissions are superseded. Follow the general method
and failure-prevention rules in the movable generation contract sections 7, 11 and 15.
Revision baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/movable-generation-contract-20261017`.

Persistence also follows contract section 17: recursively copy external non-built-in asset
dependencies byte-for-byte with their original CIDs into `ImportedData/<bridgeUUID>/Dependencies/`.
Deduplicate by CID within one bridge only; different bridges retain separate copies. Stop at built-in
assets, preserve references and source files, and scope backup/cleanup to the owning bridge UUID.
Intentional identical-CID snapshots are not generated-prefab identity collisions. Never restore
in-memory integrity checks or quarantine as part of this persistence workflow.

- A part that reaches or crosses `x = 0` stretches about the centre according to its own span,
  except for an independent vertical railing post centred on the bridge centreline: retain its
  original width and centred position at every LOD. Identify it offline, never by a runtime heuristic.
  User approval baseline: `dev/80eb1415e1a3b59263eb45693f7e2c5d80732447` (2026-10-16).
- A side part that does not reach `x = 0` translates rigidly by half the width delta. It must not be
  thickened, thinned or distorted.
- The base is the structure immediately adjacent to and directly below the road deck. It is not a
  pier footing, pillar foot plate, tower foundation or a prefab selected by a name containing `Base`.
  A candidate that does not reach or cross `x = 0` is not the base.
- A base-width adjustment is a coordinate mapping, never direct width scaling:

      x -> x + sign(x) * delta

- Bridge-pier columns translate; they do not stretch.
- The highest-detail archetype identifies each authored part once. Every LOD must receive the same
  logical classification and compatible transformation.
- Defend against damaged external Prefabs and failures from game or third-party APIs at their
  boundaries. Internal invariant failures must surface as ERROR, with exceptions where appropriate;
  do not add special cases to suppress them. Stop failed operations before publishing invalid geometry.

## Workflow

1. Confirm the current branch and cleanly separate unrelated user changes.
2. Read the instruction files required by the table above.
3. Inspect the archetype and current generated output before changing transformation code.
4. For contract maintenance, record the branch and exact `HEAD` before the first instruction edit.
5. If a generated sample is absent, record the unverified acceptance item and continue implementation.
   Do not claim visual acceptance from compilation or stop unblocked work waiting for human feedback.
6. Make the requested change on a suitable working branch. Keep offline identification and hardcoded
   runtime construction separate; use compact recorded data when sufficient.
7. Apply the change consistently to the full-detail mesh and all LODs.
8. Build and deploy coherent code increments under section 13. Preserve healthy assets; identify actual
   incompatibilities before any scoped recovery. Documentation-only work does not trigger deployment.
9. After completing any code change, immediately build and deploy to the game, then verify the
   installed files against the build output. Compilation is only a loadability check.
10. Do not call a visual issue fixed until the in-game near/far result has been observed.

## Maintaining these instructions

- Preserve every existing normative rule. Reorganization may move text, but it may not delete, weaken
  or silently supersede a requirement.
- Keep repository-wide rules in this root file. Put path-specific additions in the nearest directory
  `AGENTS.md`; a nested file is additive unless it explicitly tightens a rule for that scope.
- Keep this automatically discovered instruction chain below Codex's default 32 KiB combined limit.
  Store long explanations and failure history in the normative project contract and route agents to
  the exact material they must read.
- Use a single H1 title, ordered H2 sections, CommonMark tables/lists, fenced or indented code only for
  literal commands and formulas, and repository-relative links.
- Do not add `AGENTS.override.md` as a permanent repository rule. Overrides take precedence and can
  silently hide the regular file in the same directory.
- When changing a rule, update this routing summary, the authoritative contract and any applicable
  directory-level file together.


## Post-load self-check

Follow bridge generation contract sections 17–19 and the runtime AGENTS.md for readiness, migration,
recovery/removal, source isolation and restart behavior. The full former routing text is
retained under "Retained repository routing records" in the contract.

## Documentation language and format

The movable-bridge requirements are defined in bridge generation contract section 21 and
`MOVABLE_BRIDGES_CONTRACT.md`. The six-lane mapping example is not a universal template.

All documentation must be written in English, including instructions, contracts, audits, reports,
release notes and publishing drafts. Preserve literal resource identifiers, file paths and quoted
diagnostic evidence exactly; explain them in English. This documentation rule does not change
runtime localization. The 2026-10-13 language-only baseline remains in the project contract.

Documentation-language revision (2026-10-15): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/all-english-docs-20261015`. Existing local changes are preserved.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.
Revision baseline (2026-10-16): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

## Accepted movable generation baseline (2026-10-18)

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–23](docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-accepted-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Those sections consolidate the accepted current pipeline and supersede conflicting historical
paint/material recipes: original structural Surface/VT references; measured non-VT repeating road
and grating units plus bounded native VT composites; separate single-sided native skinned paint
with 0.001 m clearance on the original road bones, matching stack metadata and transforms.
No receiver cuts for markings, no child projectors, no structural receiver-only material copies,
no moving the road onto marking bones, and no runtime synchronization dependency. Acceptance is
specific to the latest Draw01 visual repair; new styles still require their own archetype data and
acceptance. Baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a plus preserved working changes.

Prior long instruction records are preserved in [retained movable instruction records](docs/agent-contract/MOVABLE_INSTRUCTION_RECORDS.md).
Use the current accepted contract for conflicting appearance/marking decisions.


Persistence layout revision (2026-10-19): branch `dev`, HEAD
`d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`, preserving prior working changes.
Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.
