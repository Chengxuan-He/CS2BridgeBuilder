# Project contract

## Bridge implementation policy

For bridge work, the [current implementation policy](BRIDGE_GENERATION_CONTRACT.md#current-implementation-policy)
requires hardcoded data from real prototypes and independence from the original prototype files.
Use optional style branches, coherent-increment deployment, measured CPU/GPU choices and
incremental integration.
Correct native behavior, resource ownership, persistence and localization requirements remain in force.

## Affine geometry execution

Fixed and movable bridge work follows the authoritative
[CPU affine geometry execution rules](BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution).
The measured XYZ parameter matrices, embedded sparse arrays, bounded CPU scheduling,
main-thread native API boundary and numerical verification requirements apply to new
styles and regenerated recipes as well as existing implementations. This generation-time
change does not add a mod dependency to independently persisted bridges or require GPU
geometry execution. Keep runtime and metaprogram instructions synchronized with that section.

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

The release workflow in AGENTS.md applies only to authorized releases. Historical publication
records describe past actions only, not additional future obligations.

## Contents

- [Bridge implementation policy](#bridge-implementation-policy)
- [Affine geometry execution](#affine-geometry-execution)
- [Optional GitHub publication](#optional-github-publication)
- [Contract routing and scope](#contract-routing-and-scope)
- [First rule: fix reported bugs immediately](#first-rule-fix-reported-bugs-immediately)
- [Computer-use prohibition](#computer-use-prohibition)
- [Native game UI windows](#native-game-ui-windows)
- [20. Immutable mod metadata name](#20-immutable-mod-metadata-name)
- [All documentation is written in English](#all-documentation-is-written-in-english)
- [Default documentation format](#default-documentation-format)
- [Implementation requirements](#implementation-requirements)

## Contract routing and scope

This document governs project-wide rules, documentation and metadata. Bridge generation rules
are defined in the [bridge generation contract](BRIDGE_GENERATION_CONTRACT.md).
Read that contract for every bridge, geometry, network, preview, persistence or asset-lifecycle task;
its general routing links to movable-bridge, persistence and preview supplements. The project
contract does not duplicate those technical rules. Repository and directory AGENTS.md files remain
mandatory entry points.

## First rule: fix reported bugs immediately

### Deleted bridge files must never be read again

After a bridge-owned file has been deleted, subsequent preview, generation, export,
asset discovery and texture-streaming work must not attempt to read that file through
a stale reference. Deletion must coordinate file removal with native asset registration,
preview lifetime and pending consumers; deleting the directory alone is insufficient.
Use supported native lifecycle APIs and scope cleanup to the exact owning directory
and local database. Preserve other bridges, built-in assets and same-CID copies elsewhere.

This applies to single and bulk deletion and to failure paths. Partial directory deletion
or a later prefab-unregistration exception must still retire registrations for files already
removed. Preserve registrations for surviving files; an access error is not evidence that
a file is missing. Do not mask stale-read exceptions, recreate deleted payloads, substitute
another bridge's material or require a restart as the normal deletion implementation.
Do not unload or destroy shared live render objects to bypass native lifecycle requirements.

Validate create/delete/create, repeated deletion and partial failure. Compilation alone
does not establish that native asynchronous texture requests have drained; report any
remaining in-game lifecycle verification explicitly. This requirement does not authorize
changes to startup self-check or its separate recovery policy.

### Compatibility review

Every modification requires a compatibility review across the entire codebase, not only the edited
files. Trace all affected producers, consumers, callers and alternate branches; update incompatible
paths together. Check existing assets, persistence, loading, migration, cleanup and diagnostics when
affected. For example, changing bridge identification must also check every generated asset naming
and saving path. Compilation or preview success alone does not prove compatibility: perform the
relevant end-to-end checks and explicitly report any unverified in-game behavior.

### Bug reports and completion

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

## Computer-use prohibition

Computer-use tools are prohibited for this project. This includes `mcp__cua_repl`,
native desktop control, and browser UI automation through computer-use. Use file
tools, command-line tools, or purpose-built APIs instead. When no permitted
alternative exists, request manual UI interaction from the user. Do not circumvent
this prohibition with another UI automation tool.

## Native game UI windows

All UI windows must be created through Cities: Skylines II native UI APIs and components.
This includes panels, modal dialogs, confirmation/message windows and file/directory pickers.
Do not substitute custom HTML/CSS window shells or operating-system dialogs for native game
windows. Mod-specific content, localized labels and bindings may be composed inside native
containers; styling alone does not make a custom window native.

Use the native window lifecycle, focus, input handling and modal backdrop. File or directory
selection must use the game's native picker interface rather than only a text field. Inspect
the installed game's API and existing native usage before choosing an interface; do not invent
unsupported calls. For example, the export flow uses `cs2/ui.ConfirmationDialog`, native input
and button components, and `DirectoryBrowserPanel` with mod-owned `WidgetBindings` and
`EditorWidgetListRenderer` inside the native directory-picker panel/backdrop theme,
with native `FocusRoot`, `AutoNavigationScope` and `BackConsumer`. Do not put the directory
page view in a message dialog text slot; it needs the native picker layout constraints. The options-only directory browser
does not update while the options screen is closed; do not use it for gameplay dialogs.
Keep dialogs out of non-interactive parents that suppress pointer events. Verify clicking,
text entry, selection, cancellation and confirmation in game before claiming interaction works;
compilation alone is insufficient. The computer-use prohibition still applies.

## 20. Immutable mod metadata name

The mod metadata name must always be exactly `Bridge Builder`. It must never be changed,
translated, replaced, or extended with any prefix, suffix or additional language. Publishing
metadata and any page or field using the metadata title, including the mod description heading,
must use this exact English name. Non-English translations must not replace or accompany the
name in these title positions. Before publishing or updating metadata or descriptions, verify
that both the metadata name and the description heading satisfy this requirement.

Localized in-game UI names remain permitted and must not be copied into metadata title fields
or metadata-based page headings. The documentation-language rule below requires
repository publishing descriptions and other documentation to be written in English.

## All documentation is written in English

All documentation must be written in English. This applies to instructions, contracts, README files,
development notes, audits, research reports, release notes and publishing drafts, including
project-related deliverables outside the repository. Translate existing non-English prose and
consolidate duplicate translated publishing sections into their equivalent English text without
losing unique facts, requirements or historical evidence.

Keep exact resource names, identifiers, file paths and quoted diagnostic evidence unchanged when
translation would falsify the source; provide their explanation in English. Runtime localization
and third-party license text are not rewritten by this documentation policy.

## Default documentation format

Unless otherwise specified, all project documentation must be saved as Markdown (.md).

Integrate specification changes directly into the applicable normative paragraphs. Do not retain
revision dates, changelogs, superseded alternative rules or specification rollback records, including
branch/HEAD baselines and rollback references. Synchronize routing summaries and specialist rules
when their requirements overlap. Preserve source measurements, resource identities and verification
limits; evidence timestamps are not specification revision dates. Operational code/deployment backups
and failed asset-transaction recovery remain governed by their respective rules.

## Implementation requirements

Read the following requirements before implementation.

For tasks involving multiple bridges, assign one sub-agent to each bridge and run their
bridge-specific work in parallel. Dispatch further bridges as concurrent agent slots become
available. Each bridge sub-agent works independently within its own bridge-specific files
and code; sub-agents must not communicate directly with one another. Requests to modify
shared bridge logic, code or resources must go to the main agent, rather than being edited
directly or coordinated with another sub-agent. A separate shared-logic sub-agent implements
shared changes, integration and common verification under the main agent's direction.
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
surfacing. Fix the underlying defect rather than hiding it.

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
