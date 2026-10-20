# Project contract

## Bridge implementation policy synchronization (2026-10-19)

For bridge work, the [current implementation policy](BRIDGE_GENERATION_CONTRACT.md#current-implementation-policy-2026-10-19)
supersedes conflicting historical process gates. Hardcoded data from real prototypes and independence
from the original prototype files are retained. Optional style branches, coherent-increment deployment,
measured CPU/GPU choices and incremental integration replace the former procedural restrictions.
Correct native behavior, resource ownership, persistence and localization requirements remain in force.

## Optional GitHub publication (2026-10-19)

Ordinary development and local delivery do not require GitHub access, branch pushes,
pull requests or synchronization of remote branches. A reviewed local working tree or patch is a
valid delivery. GitHub contribution is optional and requires an explicit user request and available
permissions. Do not automatically push ordinary contributions to all existing branches. Lack of GitHub permissions must
not block implementation, validation or local delivery. Existing local backup, rollback-baseline,
build, localization and verification requirements remain in force. Paradox Mods release requirements
and the original four-step release workflow remain unchanged, including synchronization of release
code to every existing GitHub branch. These requirements apply only to an authorized release;
contributors without publishing permissions can complete development through local delivery.

This policy replaces mandatory GitHub publication instructions for ordinary development and
contributions only. It does not override the release workflow in AGENTS.md. Historical publication
records describe past actions only, not additional future obligations.
Pre-edit baseline: branch `dev`, HEAD `541f91039a9a1ab981b9785eb940e76e6da5e23f`;
local rollback reference `refs/rollback/optional-github-20261019`.

## Contents

- [Optional GitHub publication (2026-10-19)](#optional-github-publication-2026-10-19)
- [Contract routing and scope](#contract-routing-and-scope)
- [First rule: fix reported bugs immediately](#first-rule-fix-reported-bugs-immediately)
- [Computer-use prohibition](#computer-use-prohibition)
- [Native game UI windows (2026-10-19)](#native-game-ui-windows-2026-10-19)
- [20. Immutable mod metadata name](#20-immutable-mod-metadata-name)
- [All documentation is written in English (2026-10-15)](#all-documentation-is-written-in-english-2026-10-15)
- [Default documentation format (2026-10-15)](#default-documentation-format-2026-10-15)
- [English documentation maintenance (2026-10-13)](#english-documentation-maintenance-2026-10-13)
- [Retained root-instruction revision records](#retained-root-instruction-revision-records)
- [Organization revision record](#organization-revision-record)

## Contract routing and scope

This document governs project-wide rules, documentation and metadata. Bridge generation rules
have moved to the independent [bridge generation contract](BRIDGE_GENERATION_CONTRACT.md).
Read that contract for every bridge, geometry, network, preview, persistence or asset-lifecycle task;
its general routing links to movable-bridge, persistence and preview supplements. The project
contract does not duplicate those technical rules. Repository and directory AGENTS.md files remain
mandatory entry points.

Split baseline (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/split-bridge-contract-20261017`. Existing working-tree changes are preserved.
The reference records committed history, not those local changes.

## First rule: fix reported bugs immediately

### Compatibility review

Every modification requires a compatibility review across the entire codebase, not only the edited
files. Trace all affected producers, consumers, callers and alternate branches; update incompatible
paths together. Check existing assets, persistence, loading, migration, cleanup and diagnostics when
affected. For example, changing bridge identification must also check every generated asset naming
and saving path. Compilation or preview success alone does not prove compatibility: perform the
relevant end-to-end checks and explicitly report any unverified in-game behavior.

Compatibility-rule rollback baseline: branch `dev`, HEAD
`673bc85e84a83ab14c957a6a9565a013f69209e5`; local reference
`refs/rollback/project-wide-compatibility-20261007`. Existing uncommitted changes are preserved.

### Bug reports and completion

Fix every reported bug immediately. A user bug report authorizes the corresponding repair. After
diagnosis, immediately proceed with implementation and verification; acknowledgment, analysis, a plan
or a proposed next step must not replace the fix. Do not require the user to request the same fix again.

This rule does not waive archetype-evidence, branching, safety, cleanup or actual in-game acceptance
requirements. When a specific blocker exists, first complete all unblocked work, then identify the
missing evidence, permission or necessary manual action. Do not guess at the implementation or falsely
report a successful fix.

Instruction-edit rollback baseline: branch `dev`, HEAD
`7cfb9afcffc9c87c69801cc1a0c5621c7d1d322d` (recorded before this edit).

Rules this mod is held to. They are not style preferences; each one is here because breaking it
produced a bridge that was wrong in a way nothing reported, and finding out why cost a round of
guessing.

This document is the normative project contract referenced by the repository-wide `AGENTS.md`.
Every Agent must read the sections routed by that file and obey them before changing the project.

The independent bridge generation contract retains the numbered geometry, diagnosis, native
behavior and bridge workflow rules. Its later rules have the same force as its original rules.

## Computer-use prohibition

Instruction-edit rollback baseline: branch `dev`, HEAD
`b5e8ad4be13e8ff3cb7305a01b72c76d241b24e6` (recorded before this edit).

Computer-use tools are prohibited for this project. This includes `mcp__cua_repl`,
native desktop control, and browser UI automation through computer-use. Use file
tools, command-line tools, or purpose-built APIs instead. When no permitted
alternative exists, request manual UI interaction from the user. Do not circumvent
this prohibition with another UI automation tool.

## Native game UI windows (2026-10-19)

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

Instruction-edit baseline: branch `dev`, HEAD
`d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`. Existing working-tree changes are preserved.
This revision records the user's project-wide requirement; it does not establish acceptance
of existing windows or authorize unrelated code changes in this documentation-only task.

## 20. Immutable mod metadata name

User instruction 2026-10-07. Baseline: `dev`, `99bed35b96074f4954ec51c06ec171437ff5c79e`.
Rollback reference: `refs/rollback/fixed-mod-metadata-name-20261007`. Preserve existing local edits.

The mod metadata name must always be exactly `Bridge Builder`. It must never be changed,
translated, replaced, or extended with any prefix, suffix or additional language. Publishing
metadata and any page or field using the metadata title, including the mod description heading,
must use this exact English name. Non-English translations must not replace or accompany the
name in these title positions. Before publishing or updating metadata or descriptions, verify
that both the metadata name and the description heading satisfy this requirement.

Localized in-game UI names remain permitted and must not be copied into metadata title fields
or metadata-based page headings. The 2026-10-15 documentation-language rule below requires
repository publishing descriptions and other documentation to be written in English.

## All documentation is written in English (2026-10-15)

All documentation must be written in English. This applies to instructions, contracts, README files,
development notes, audits, research reports, release notes and publishing drafts, including
project-related deliverables outside the repository. Translate existing non-English prose and
consolidate duplicate translated publishing sections into their equivalent English text without
losing unique facts, requirements or historical evidence. This supersedes the narrower instruction-
and-contract-only language policy and permission for translated documentation bodies.

Keep exact resource names, identifiers, file paths and quoted diagnostic evidence unchanged when
translation would falsify the source; provide their explanation in English. Runtime localization
and third-party license text are not rewritten by this documentation policy.

Pre-edit baseline: branch `dev`, HEAD `0f4b10ae47c5dc264249e0d877aacc326e3e9525`;
rollback reference `refs/rollback/all-english-docs-20261015`. Existing local changes are preserved.
This is documentation maintenance only: no shutdown, bridge cleanup, build or deployment.

## Default documentation format (2026-10-15)

Unless otherwise specified, all project documentation must be saved as Markdown (.md).

Pre-edit baseline: branch `dev`, HEAD `0f4b10ae47c5dc264249e0d877aacc326e3e9525`;
rollback reference `refs/rollback/markdown-docs-default-20261015`. Existing local changes are preserved.

## English documentation maintenance (2026-10-13)

Instruction and contract prose is maintained in English. This language-only revision preserves
existing requirements, exceptions, identifiers and historical evidence. Baseline: branch `dev`,
HEAD `5bea1d0bef04dd2ac718809852d3f657c7388e58`; rollback reference
`refs/rollback/english-contracts-guide-20261013`. Existing local changes are preserved.

## Retained root-instruction revision records

The following records were moved verbatim from the root instruction file during the 2026-10-15
English-documentation revision to keep the automatically loaded instruction chain within its
32 KiB budget. The operative requirements remain in the root rules and numbered contract sections.

Compatibility-rule rollback baseline: branch `dev`, HEAD
`673bc85e84a83ab14c957a6a9565a013f69209e5`; local reference
`refs/rollback/project-wide-compatibility-20261007`. Existing uncommitted changes are preserved.

Instruction-edit rollback baseline for this rule: branch `dev`, HEAD
`0f889f67b7498afa7126ca5fa78e441cb240efe8`; local reference
`refs/rollback/simplest-implementation-rule-20261006`. Existing uncommitted changes are preserved.

Instruction-edit rollback baseline: branch `dev`, HEAD
`0f889f67b7498afa7126ca5fa78e441cb240efe8`; local reference
`refs/rollback/release-workflow-20261007`. Existing uncommitted changes are preserved.

Movable-bridge specification revision (2026-10-15): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/movable-bridge-contract-20261015`. Bridge generation contract section 21 incorporates the
movable-bridge supplement. Generate the moving road surface from the selected road's closed-pose
composition, bake persistent object geometry and bind it to the measured native mechanism.
This is a development requirement, not an admission of the currently deferred generators.
Map each selected road/state using its specific bridge archetype's measured rules; the six-lane
Large Road to DrawBridge03 example must not become a universal mapping for other movable bridges.

Source isolation (2026-10-13): creating bridges/copies must never select another bridge's Prefab from ImportedData (including UUID dependency directories), even with the same CID. Filter before reading; use original/permitted sources or fail. Ownership remains path-string-only. See contract's Source isolation subsection; baseline dev/5bea1d0bef04dd2ac718809852d3f657c7388e58, rollback refs/rollback/no-foreign-bridge-source-20261013.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.
