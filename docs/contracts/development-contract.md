# Development and delivery contract

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="project-optional-github-publication"></a>

## Optional GitHub publication

Ordinary development and local delivery do not require GitHub access, branch pushes,
pull requests or synchronization of remote branches. A reviewed local working tree or patch is a
valid delivery. GitHub contribution is optional and requires an explicit user request and available
permissions. Do not automatically push ordinary contributions to all existing branches. Lack of GitHub permissions must
not block implementation, validation or local delivery. Code/deployment backup, build, localization and verification requirements remain in force.
Paradox Mods release requirements
and the original four-step release workflow remain unchanged, including synchronization of release
code to every existing GitHub branch. These requirements apply only to an authorized release;
contributors without publishing permissions can complete development through local delivery.

Authorized releases additionally require one pre-release round under the
[cross-environment testing contract](reading-map.md#cross-environment-testing-contract). Explicit testing requests
trigger the same manual workflow; it is not an every-startup test. One independent agent owns each
concrete environment, and unresolved required cases need an explicit user release decision.
The manual game-start rule and trusted internal execution policy remain unchanged.

The [release workflow](#repository-release-workflow) applies only to authorized releases. Historical publication
records describe past actions only, not additional future obligations.

<a id="project-contents"></a>

## Contents

- [Bridge implementation policy](generation-contract.md#project-bridge-implementation-policy)
- [Affine geometry execution](../specs/generation-spec.md#project-affine-geometry-execution)
- [Optional GitHub publication](development-contract.md#project-optional-github-publication)
- [Contract routing and scope](development-contract.md#project-contract-routing-and-scope)
- [First rule: fix reported bugs immediately](development-contract.md#project-first-rule-fix-reported-bugs-immediately)
- [Computer-use prohibition](../constitution.md#project-computer-use-prohibition)
- [Native game UI windows](../specs/ui-spec.md#project-native-game-ui-windows)
- [20. Immutable mod metadata name](../constitution.md#project-20-immutable-mod-metadata-name)
- [All documentation is written in English](../constitution.md#project-all-documentation-is-written-in-english)
- [Default documentation format](../constitution.md#project-default-documentation-format)
- [Costly operations](development-contract.md#project-costly-operations)
- [Implementation requirements](development-contract.md#project-implementation-requirements)

<a id="project-contract-routing-and-scope"></a>

## Contract routing and scope

This document governs project-wide rules, documentation and metadata. Bridge generation rules
are defined in the [bridge generation contract](reading-map.md#bridge-generation-contract).
Read that contract for every bridge, geometry, network, preview, persistence or asset-lifecycle task;
its general routing links to movable-bridge, persistence and preview supplements. The project
contract does not duplicate those technical rules. Repository and directory AGENTS.md files remain
mandatory entry points.

<a id="project-first-rule-fix-reported-bugs-immediately"></a>

## First rule: fix reported bugs immediately

For single and bulk player deletion, including stale-reader prevention and partial failure, follow [Bridge deletion](reading-map.md#bridge-deletion).

<a id="project-compatibility-review"></a>

## Compatibility review

Every modification requires a compatibility review across the entire codebase, not only the edited
files. Trace all affected producers, consumers, callers and alternate branches; update incompatible
paths together. Check existing assets, persistence, loading, migration, cleanup and diagnostics when
affected. For example, changing bridge identification must also check every generated asset naming
and saving path. Compilation or preview success alone does not prove compatibility: perform the
relevant end-to-end checks and explicitly report any unverified in-game behavior.

For every development, repair or refactoring change, apply the
[compatibility principles](../constitution.md#backward-and-cross-environment-compatibility).
Identify affected published-release generated bridges and saves, persisted formats, CIDs/UUIDs and references;
identify their self-check migration into the current format; do not add normal-runtime old-format readers. Review migration, recovery and
cleanup together under the [persistence reading scope](reading-map.md#bridge-asset-persistence)
and [self-check reading scope](reading-map.md#bridge-self-check-contract). Exercise relevant published-release
fixtures and cold-load/migration cases, including a subsequent unchanged run where applicable;
report unsupported inputs and unverified native behavior explicitly. A successful new bridge is
not evidence that older bridges remain usable.

Review environmental assumptions at the affected external boundaries and select the applicable
[cross-environment contract](cross-environment-contract.md) and
[test specification](../specs/cross-environment-spec.md) cases. Record actual OS/GPU/host,
permission and directory-mapping conditions, compiler/runtime and architecture evidence where
relevant; do not equate a simulated case with a native hardware/cloud result. Keep the manual and
pre-release test triggers, required-permission exception policy and documented support limits.
This review does not add startup tests or require support for every historical game API.

<a id="project-bug-reports-and-completion"></a>

## Bug reports and completion

Fix every reported bug immediately. A user bug report authorizes the corresponding repair. After
diagnosis, immediately proceed with implementation and verification; acknowledgment, analysis, a plan
or a proposed next step must not replace the fix. Do not require the user to request the same fix again.

This rule does not waive archetype-evidence, branching, safety, cleanup or actual in-game acceptance
requirements. When a specific blocker exists, first complete all unblocked work, then identify the
missing evidence, permission or necessary manual action. Do not guess at the implementation or falsely
report a successful fix.

Rules this mod is held to. They are not style preferences; each one is here because breaking it
produced a bridge that was wrong in a way nothing reported, and finding out why cost a round of
guessing.

This document is the normative project contract referenced by the repository-wide `AGENTS.md`.
Every Agent must read the sections routed by that file and obey them before changing the project.

The independent bridge generation contract retains the numbered geometry, diagnosis, native
behavior and bridge workflow rules. All its applicable rules are normative.

<a id="project-costly-operations"></a>

## Costly operations

Before recommending that the developer perform an operation, or performing it yourself, explicitly
disclose material cost and risk when the operation can be expensive to reverse. This includes
unchecking or uninstalling DLC, deleting or moving files, removing caches, and discarding build
inputs when recovery requires substantial downloading, reconstruction, time or storage.

Explain the actual affected scope and side effects, including possible permanent loss, remote
redownloads and expensive rebuilds. Report known sizes and expected time, bandwidth and storage
costs with their evidence; label unknowns as unknown rather than inventing estimates. Verify whether
a complete, recoverable backup exists, identify its location and validation status, and explain the
restoration steps and prerequisites. Partial extracts, manifests and preallocated download files
are not complete backups. Never describe restoration as easy or immediate without evidence.

In particular, unchecking DLC in Steam can uninstall its local files. Rechecking it may require a
remote download; do not present this as a harmless availability toggle. Before suggesting it for
a test, compare lower-cost reversible alternatives and state their limitations: hiding files or
changing a mod setting may not reproduce the same native ownership, database or loading state.
Do not claim equivalent test coverage without evidence.

Prefer a suitable low-cost reversible alternative when it satisfies the intended test. Request
confirmation only for material side effects outside the developer's informed authorization; do
not repeat approval requests for a scope whose risks were already disclosed and authorized.
This rule does not add approval gates to ordinary low-impact operations. Existing process,
ownership, backup and manual game-start rules continue to apply.

<a id="project-implementation-requirements"></a>

## Implementation requirements

Read the following requirements before implementation.

For tasks involving multiple bridges, assign one sub-agent to each bridge and run their
bridge-specific work in parallel. Dispatch further bridges as concurrent agent slots become
available. Each bridge sub-agent works independently within its own bridge-specific files
and code; sub-agents must not communicate directly with one another. Requests to modify
shared bridge logic, code or resources must go to the main agent, rather than being edited
directly or coordinated with another sub-agent. A separate shared-logic sub-agent implements
shared changes, integration and common verification under the main agent's direction.
Prioritize reusing already-created sub-agents to reduce context overhead. Shared work resumes
the existing shared-logic sub-agent; work on a bridge resumes the existing sub-agent assigned
to that same bridge. Completion of a previous assignment does not require a replacement agent.
Create a new sub-agent only when no suitable existing agent is available, and do not reassign
another bridge's agent merely because it is idle.
The main agent only manages sub-agents: task assignment, request routing, progress tracking
and result coordination; it does not implement shared or bridge-specific changes itself.
The shared-logic sub-agent reports every shared change, affected interface, behavior and
required consumer adjustment to the main agent, which forwards the notification to every
affected bridge sub-agent. Sub-agents must not notify each other directly.
Dependencies on other agents must be awaited asynchronously through
the main agent: keep the request pending and continue independent work for the assigned
bridge instead of blocking on the response. When shared logic changes, the main agent
must relay the shared-logic sub-agent's report to all sub-agents that need to use it.
Each sub-agent may schedule its own bridge's Unity baking with a separate Unity project,
output directory and log; avoid concurrent writes to shared project state or resources.
When an independent task needs an environment, provide each sub-agent with an independent
environment first, including its own Unity project, baker copy, writable caches, outputs
and logs. Consider a shared environment only when independent environments cannot be
provided. Notify each sub-agent of its assigned environment and usage instructions.
The main agent centrally manages the lifecycle of shared and sub-agent environments.
After declaring the task complete, preserve deliverables outside the environments, confirm
all related processes have exited, and permanently delete every task-created shared and
sub-agent environment file and directory. Verify resolved target paths and completed
deletion; preserve pre-existing tools, licenses, source files and retained deliverables.

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
- Self-check and historical migration follow the [self-check contract](reading-map.md#bridge-self-check-contract).
- [Section 20](../constitution.md#project-20-immutable-mod-metadata-name): the mod metadata name and any metadata-based heading are exactly `Bridge Builder`.
  Localized in-game labels remain permitted. Documentation and publishing drafts are in English.
- Section 16: preserve native construction, elevation and upkeep fees. No manager-defined totals,
  fee rewrites, multipliers, offsets, floors or pricing-only copies. Legacy conversion removes the
  custom cost component without guessing original fees or deleting referenced native copies.
- Sections 17 and 19: generated bridges are independent native assets; Bridge Builder is their
  manager and generated-program registrar. Preserve native unlocks, locales, thumbnails and fees.
  The known double-deck rail-seam limitation remains unfinished compatibility work.

Deployment cleanup is governed by section 13, not by migration or manual deletion rules.

See [Compatibility review](development-contract.md#project-compatibility-review).

Prefer the simplest implementation that satisfies the request. Overengineering is prohibited:
unnecessary complexity becomes a primary source of bugs that are only discovered through player
reports. For example, when Prefabs can be selected by string matching, using metadata to select them
is forbidden. Do not introduce extra abstractions, state, prerequisites or validation layers when
the direct implementation is sufficient.

Follow [trusted internal execution](../constitution.md#repository-trusted-internal-execution). All non-IO steps
and results, including memory and pure computation, are trusted. Runtime validation belongs only
at actual external file/network IO boundaries; do not assert or recheck internal inputs, results,
counts, ranges, finite values or invariants. Retain actual operation failure handling and normal
business, native-rule, cancellation and lifetime control flow. Standalone offline development
tests may verify results; runtime-consumed tool code follows the production rule. Do not report
failed operations as success or treat this policy as a measured crash/performance diagnosis.
The [complete material pipeline](material-contract.md#material-complete-material-processing-without-propagated-exceptions)
is explicitly non-throwing: contain native/external exceptions at operation boundaries,
return diagnosed failure results and preserve complete support for valid unknown sources.
This specific material result contract takes precedence over the general throw guidance above.

Apply [Bug reports and completion](#project-bug-reports-and-completion).

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

<a id="repository-multilingual-adaptation-is-required-for-every-change"></a>

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

Source-mod display follows [full published source names](../specs/ui-spec.md#generation-full-published-source-mod-names): use the complete Paradox Mods title, verify it through web search and the official page/API, and retain technical IDs only for binding. Apply this to every source, localize the prefix, preserve access to the full long title, and never replace a missing name with a numeric ID or guessed abbreviation.

<a id="repository-first-rule-fix-reported-bugs-immediately"></a>

## First rule: fix reported bugs immediately

For player-requested bridge removal, follow [Bridge deletion](reading-map.md#bridge-deletion).

Read the [implementation requirements](development-contract.md#project-implementation-requirements)
in full: contract routing, immediate bug fixes, compatibility review, simplest implementation,
external-input boundaries and explicit internal errors remain mandatory. Bug reports authorize
implementation and verification without repeated permission. Documentation does not cancel
authorized repairs; report missing evidence while continuing independent work.

<a id="repository-costly-operations-require-advance-disclosure"></a>

## Costly operations require advance disclosure

Before recommending or performing costly operations, disclose risks to the developer under
[the project policy](development-contract.md#project-costly-operations): actual side effects,
recovery costs, verified backups and reversible alternatives. Unchecking Steam DLC can uninstall
files and require remote downloads; it is not merely a toggle. Respect existing informed authorization.

<a id="repository-parallel-agents-for-multi-bridge-tasks"></a>

## Parallel agents for multi-bridge tasks

Follow the complete [coordination and environment requirements](#project-implementation-requirements), including one agent per bridge, shared-logic ownership, main-agent routing, reuse, independent environments and lifecycle cleanup.

<a id="repository-github-publication-is-optional"></a>

## GitHub publication is optional

Follow [Optional GitHub publication](#project-optional-github-publication) for ordinary local delivery and the separate authorized-release obligations.

<a id="repository-release-workflow"></a>

## Release workflow

The release owner must use the user-specified version and complete the authorized release in the prescribed order: verified code and cross-environment checks, all-branch publication, verified Paradox Mods publication, then installation of the downloaded published package. Unresolved required checks need an explicit user release decision.

The [required procedure](../guides/development-release-procedures.md#repository-release-workflow) retains the complete steps and acceptance checklist.

<a id="repository-workflow"></a>

## Workflow

The implementer owns instruction and branch review, archetype evidence, consistent all-LOD changes, coherent verified delivery and separate reporting of automated and human acceptance.

The [required procedure](../guides/development-release-procedures.md#repository-workflow) retains the complete steps and acceptance checklist.

<a id="runtime-verification-and-delivery"></a>

## Verification and delivery

Custom material programs are precompiled offline; runtime only loads/registers their native
containers in the current format, without a compiler fallback. Self-check retires legacy owner-persisted custom-program bridges; other supported published formats retain ordinary migration. New Surfaces refer to stable mod-owned program/queue identities without owner shader files.
Offline compiled shader payloads follow the root variable-length compiled-output rule. Rebuild every
affected nested record and container; equal-length replacement is not a compatibility requirement.
Verify shorter and longer outputs, multiple slots and repeated compilation as well as malformed input.

Generation section 24 requires complete-Prefab size comparison, including dependencies. Material owner bytes are informational without a ceiling; the complete SDK ZIP is reported separately without a hard ceiling. A prepared directory or root-only size proves neither.

Follow root localization and cross-codebase compatibility review for every change. Documentation remains English. Build/deploy coherent increments under sections 13 and 23, preserving healthy assets. Never close or launch the player's game: deploy only after confirming it is absent, backing up installed originals and verifying hashes. Documentation-only changes need no build/deployment. Report native motion, near/far, mod-enabled cold-load and visual acceptance separately from automated checks; continue unblocked work while human checks remain pending.

<a id="metaprogram-iteration-and-deployment"></a>

## Iteration and deployment

Follow the current [bridge generation contract sections 12–13](development-contract.md#generation-12-branches-organize-work-without-blocking-integration). Style branches are optional; keep
variant-specific measured data separate. Build and deploy coherent code increments. Documentation
and analysis artifacts alone require no deployment. Preserve healthy bridge assets unless an
identified incompatibility requires scoped, backed-up recovery. Do not pause implementation merely
because another style's tables or human visual acceptance are unfinished.

<a id="generation-incremental-implementation"></a>

## Incremental implementation

- No blanket deferred-design refusal for draw, lift or pedestrian movable styles. Implement and
  route each actual generator incrementally; specific missing data or failed stages remain errors.
- No requirement to complete every style's exhaustive audit before coding or integrating the next
  working stage. Continue toward full generation instead of indefinitely producing research-only work.
- Per-style branches are optional. A multi-style task may integrate shared and style-specific changes
  in one working branch while keeping style data separate and preserving unrelated changes.
- CPU/GPU placement and parallelism follow correctness, API affinity and measured performance.
  GPU conversion is not a prerequisite to implement a correct bounded path.
- Automated geometry/serialization checks and isolated native integration checks are allowed.
  Missing human feedback does not stop coding or routing. Do not equate those checks with human
  visual acceptance or claim an untested motion/cold-load result.
- Build and deploy coherent buildable increments, not each intermediate edit. Have the user exit the game before
  replacing in-use binaries, back up original installed files and verify deployment hashes. Preserve
  healthy bridge assets; cleanup requires an identified incompatibility and remains ownership-scoped.

The selected road/upgrades, native motion/signals, original road bones, single-sided native skinned
paint, [material contract section 2](../specs/material-spec.md#material-2-preserve-source-resolution-and-pattern-scale)'s mapping-specific density, every required LOD, independent persistence, native fees/unlocks,
output ownership isolation and complete localization remain required. These are outcomes to implement and
verify, not grounds to label an unfinished route complete.

<a id="generation-6-nothing-is-claimed-as-fixed-until-it-has-been-seen-working"></a>

## Nothing is claimed as fixed until it has been seen working

A change that compiles and passes the offline tests is a change, not a fix. The offline tests cover
arithmetic and recorded data; they cannot see a mesh, a material, or a prefab in a running world.

Automated checks may verify recorded data, topology, skinning and serialization. They do not prove
visual correctness. Inspect real prototype/generated geometry and all LODs, read diagnostics, and
record in-game near/far and motion acceptance separately. Compilation is not visual verification.
Missing human feedback does not suspend implementation of remaining generator stages.

**Near and far views are one implementation and must be changed together.** Any change to a visual
part — including its geometry, transform, component selection, material-facing mesh data or bounds —
must be applied consistently to the highest-detail mesh and to every LOD that can replace it. Changing
only the near mesh, only one LOD, or otherwise fixing a single viewing distance is forbidden. The
highest-detail archetype decides the identity and transform of the authored part once; every far-view
representation inherits that same decision. Completion requires inspecting both the near and far
generated geometry and then checking both viewing distances in game. A correct result at one distance
does not compensate for a defect at another.

The implementer must state both what the evidence establishes and what it does not. A measured
output alone does not establish that the reported in-game fault has been resolved.

<a id="generation-7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it"></a>

## A fault is found by dumping both and diffing, not by reasoning about it

Diagnosis must be grounded in observed differences between real archetype and generated output, rather than unsupported geometric inference.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it).

<a id="generation-dump-both-normalise-diff"></a>

## Dump both, normalise, diff

The investigator owns comparison of the whole prefab, excluding identity-only names, the four GUID fields and serialization version. Component presence belongs to that comparison, not just numbers or the visually suspect part.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-dump-both-normalise-diff).

<a id="generation-read-the-games-il-to-learn-what-a-field-does"></a>

## Read the game's IL to learn what a field does

Field meanings and enum values must be established from native implementation evidence, not inferred from names or metadata field order.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-read-the-games-il-to-learn-what-a-field-does).

<a id="generation-a-component-that-reads-as-cosmetic-may-be-structural"></a>

## A component that reads as cosmetic may be structural

A component must not be dismissed as cosmetic based on its name. Carrying scalar fields does not establish that the component itself has been preserved.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-a-component-that-reads-as-cosmetic-may-be-structural).

<a id="generation-ruling-a-suspect-out-counts-and-a-shared-code-path-is-the-cheapest-way"></a>

## Ruling a suspect out counts, and a shared code path is the cheapest way

The investigator must record excluded hypotheses and their supporting evidence, including applicable shared-code-path evidence.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-ruling-a-suspect-out-counts-and-a-shared-code-path-is-the-cheapest-way).

<a id="generation-check-that-the-instrument-can-see-the-thing-before-trusting-its-silence"></a>

## Check that the instrument can see the thing before trusting its silence

The investigator must establish that diagnostics actually cover the inspected subject before trusting an absence of findings. Budgets, caps, depth limits and filters must provide per-subject coverage rather than exhaust a shared per-run allowance.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-check-that-the-instrument-can-see-the-thing-before-trusting-its-silence).

<a id="generation-a-check-that-reports-is-an-instrument-and-is-wrong-until-it-has-met-its-cases"></a>

## A check that reports is an instrument, and is wrong until it has met its cases

Validate diagnostic quantities against the actual authored parts and representations they measure:

- Compare the same mesh and part membership before and after transformation; do not compare a
  whole section including anchorages with one isolated output mesh.
- Distinguish a spanning member's intended width change from a side member's thickness.
- When sampled runs overlap or merge across the centre, inspect their recorded membership and
  transformations before interpreting the aggregate run length as thickness.
- A vertical member must receive one consistent authored-part mapping at every height. Merging
  runs caused by incompatible displacements within that member indicate a real defect; do not
  dismiss the measurement merely because another case produced a false positive.

Exercise applicable coarse-LOD and spanning-surface cases before promoting a diagnostic to ERROR.
Respect the movable no-special-width rule: diagnostic validation does not authorize a narrow-road
correction or an additional compatibility gate. Inspect the reported geometry before changing or
removing a check; an indirect measurement can expose a defect that aggregate width misses.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-a-check-that-reports-is-an-instrument-and-is-wrong-until-it-has-met-its-cases).

<a id="generation-say-plainly-when-a-fix-is-not-the-reported-fault"></a>

## Say plainly when a fix is not the reported fault

A discovered and repaired secondary defect must be identified as such; it must not be presented as resolution of a different reported fault without evidence.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-say-plainly-when-a-fix-is-not-the-reported-fault).

<a id="generation-when-a-fault-survives-several-rounds-change-category"></a>

## When a fault survives several rounds, change category

After two unsuccessful attempts within one diagnostic category, the investigator must change category and obtain an archetype/output comparison rather than repeat unsupported corrections.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-when-a-fault-survives-several-rounds-change-category).

<a id="generation-two-archetypes-of-different-sizes-are-a-test-oracle"></a>

## Two archetypes of different sizes are a test oracle

Where native archetypes of different sizes exist, the investigator must use that pair as an oracle before forming a transformation theory. Identity at the archetype width requires measured evidence, not assertion.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-two-archetypes-of-different-sizes-are-a-test-oracle).

<a id="generation-a-field-that-describes-the-result-is-computed-not-carried-across"></a>

## A field that describes the result is computed, not carried across

The producing code owns recomputation of fields describing changed output, including bounds, extents, counts, hashes and totals. Separate copied, changed and computed fields; source preservation remains subject to generation section 4 and its explicit conversions.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-a-field-that-describes-the-result-is-computed-not-carried-across).

<a id="generation-a-default-constructed-value-is-an-assertion-not-a-blank"></a>

## A default-constructed value is an assertion, not a blank

The producer is responsible for required fields omitted by convenience constructors. Defaults must not be mistaken for deferred automatic computation.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-a-default-constructed-value-is-an-assertion-not-a-blank).

<a id="generation-read-the-flags-the-callee-is-passed"></a>

## Read the flags the callee is passed

The caller is responsible for establishing actual native callee and passed-flag semantics, including fully decoding numeric flags. Familiar API names do not establish which calculations occur.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-read-the-flags-the-callee-is-passed).

<a id="generation-worked-example-the-cables-end-to-end"></a>

## Worked example: the cables, end to end

The cable investigation, suspension measurements and zero-bounds findings are retained as historical evidence, not current verification results.

[Historical source evidence](../archive/incidents/suspension-tower-cable-diagnostics.md#generation-worked-example-the-cables-end-to-end).

<a id="generation-12-branches-organize-work-without-blocking-integration"></a>

## Branches organize work without blocking integration

Inspect the current branch and preserve unrelated changes. Existing `bridge/<style-slug>` branches
remain optional organization, not prerequisites for reading, editing or building a style. A multi-style
task may implement shared machinery and style-specific immutable data on one suitable working branch.
Use focused commits and keep every variant's evidence distinct. Repeated branch switching and
cherry-picking must not replace implementation work.

<a id="13-stop-the-game-clean-up-affected-bridge-styles-and-deploy-code-changes"></a>

<a id="generation-13-deploy-coherent-increments-and-preserve-healthy-assets"></a>

## Deploy coherent increments and preserve healthy assets

Build and deploy each coherent, buildable code increment before delivering it. Intermediate source
edits within that increment do not each require shutdown or deployment. Before replacing game-loaded
binaries, ask the user to close the game and verify the process is absent; do not terminate a
player-started instance. Back up only original installed files, copy the
build outputs and verify their hashes. Documentation-only changes require none of these operations.

A new style or uncalled generation stage does not invalidate existing assets. Do not delete healthy
bridges merely because generator code changed. When a concrete incompatibility requires recovery,
identify the affected style IDs and UUID-owned artifacts, follow the existing backed-up recovery
procedure, verify the result and preserve unrelated assets. This changes no self-check, migration,
explicit deletion or ownership semantics. Deployment is not in-game visual acceptance. Requested releases additionally require one
[cross-environment test round](reading-map.md#cross-environment-testing-contract), with independent environment
agents, explicit evidence tiers and a user decision for unresolved required cases. This is not a
normal-startup probe or permission to start/close the game, and does not add runtime internal checks.

<a id="generation-current-repository-checklist"></a>

## Current repository checklist

Delivery requires recorded prototype data, available resource graphs, selected-road and native-motion correctness, preserved native attributes and fees, routing/localization, independent persistence, ownership-scoped recovery and explicitly bounded validation evidence.

The [required procedure](../guides/development-release-procedures.md#generation-current-repository-checklist) retains the complete steps and acceptance checklist.

<a id="repository-performance-and-deployment"></a>

## Performance and deployment

Related requirements: [Generation Spec](../specs/generation-spec.md#repository-performance-and-deployment-part-2), [Development Contract](development-contract.md#repository-performance-and-deployment-part-3), [Preview Spec](../specs/preview-spec.md#repository-performance-and-deployment-part-4).

Read the complete [material contract](reading-map.md#material-generation-contract): elevated branch selection, noexcept source/receiver routing, original non-road references, prototype-supported road processing, all-LOD PBR/alpha/mips, source-density tiling, marking removal and wear, allowed runtime conversions, packaged/shared resources and owner-local snapshots are mandatory. Do not replace this contract with source-name/CID adapters or geometry-only checks. Report owner bytes and the complete official SDK ZIP separately; neither has a hard byte ceiling.

<a id="repository-performance-and-deployment-part-3"></a>

## Deployment responsibilities

Related requirements: [Development Contract](development-contract.md#repository-performance-and-deployment), [Generation Spec](../specs/generation-spec.md#repository-performance-and-deployment-part-2), [Preview Spec](../specs/preview-spec.md#repository-performance-and-deployment-part-4).

Follow generation contract sections 13 and 23: build/deploy coherent increments, verify the game is absent, back up installed originals and verify hashes. The user closes/starts the game under the [manual-start rule](../constitution.md#repository-game-and-launcher-startup-is-manual). Preserve healthy bridges; only demonstrated incompatibility permits ownership-scoped recovery. Documentation-only changes require no build, shutdown or deployment.


## Legacy custom-program compatibility exception

Apply the constitution's [compatibility exception](../constitution.md#backward-and-cross-environment-compatibility) through the [self-check retirement boundary](self-check-contract.md#self-check-obsolete-custom-program-retirement). Review healthy noncustom assets under ordinary preservation obligations.
