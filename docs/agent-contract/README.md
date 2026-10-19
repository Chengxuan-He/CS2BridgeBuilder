# Contract documents

## Contents

- [Shared sidewalk requirements](#shared-sidewalk-requirements)
- [Specialist contracts](#specialist-contracts)
- [Style registration and UI](#style-registration-and-ui)
- [Implementation records](#implementation-records)
- [English-language revision](#english-language-revision)
- [Organization revision record](#organization-revision-record)

[PROJECT_CONTRACT.md](PROJECT_CONTRACT.md) governs project-wide workflow, documentation and metadata.
[BRIDGE_GENERATION_CONTRACT.md](BRIDGE_GENERATION_CONTRACT.md) is the independent general bridge
contract and the routing authority for all bridge generation work. Read it for fixed and movable
bridges alike, then follow its links to the applicable specialist contracts.

## Shared sidewalk requirements

**Mandatory for every bridge generator:** roads may have no sidewalks, a sidewalk on only one
side, or sidewalks of unequal widths. Sidewalk-dependent components, including inner bridge
railings and bridge-authored sidewalk portions, must adapt independently to each side's actual
sidewalk presence, position and width in the selected road's **elevated-mode composition** and
upgrade state. Do not fabricate a sidewalk-dependent component on a side without a sidewalk or
assume symmetry. This applies to fixed and movable bridges, previews, saved assets and all LODs.
See [bridge generation contract section 25](BRIDGE_GENERATION_CONTRACT.md#25-adapt-sidewalk-dependent-components-to-the-actual-elevated-road)
for the authoritative rule and the distinction from unrelated structural or outer safety railings.
This is a general contract routing requirement, not a movable-bridge-only rule.

## Specialist contracts

[Generator code organization](GENERATOR_CODE_CONTRACT.md) governs base-class inheritance,
one concrete generator file per style, shared lifecycle ownership and style-specific hooks.

[MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md) is the bridge generation contract's normative movable-bridge
supplement, incorporated by section 21. It defines a general method derived from the implemented
pipeline: selected-road geometry conversion, physical motion domains, existing materials and
prototype UV projection, separate native skinned paint, native Prefab graph construction, selective persistence
and unlocks. **Read sections 16–23 first for the accepted 2026-10-18 material, marking, binding,
LOD and persistence pipeline.** Earlier conflicting recipes are historical. Section 15 records
encountered failures and required solutions. Acceptance is recorded
per style; the general method does not claim every registered generator is complete. Supporting
audits retain measurements and implementation history rather than overriding current requirements.
[Bridge asset persistence and reference policy](../Bridge-Asset-Persistence.md) is the normative
supplement incorporated by bridge generation contract section 17. It distinguishes owned generated data,
unchanged native references, external dependency snapshots and temporary preview objects.
The repository and directory-level AGENTS.md files route agents to the applicable requirements.

[Bridge preview specification](../Bridge-Preview-Specification.md) consolidates the existing preview
requirements, including complete approaches and open movable mechanisms, temporary resource
ownership, UI errors and human acceptance. Implementation history is maintained separately.

## Style registration and UI

Bridge generation contract section 22 requires immediate UI synchronization and generator-router integration
whenever a new bridge style is created, with names in every supported UI language in the same
implementation increment.

## Implementation records

[Movable bridge implementation status](../Movable-Bridges-Implementation.md) tracks the ten B&P
mechanisms, their reserved style/branch identities, backup and remaining conversion work.
Update this record with implementation and verification evidence; keep admission requirements
in the contracts.

## English-language revision

The 2026-10-15 revision requires all documentation to be written in English, including audits,
research reports, release notes and publishing drafts. Literal identifiers, paths and diagnostic
quotes are preserved as source evidence and explained in English. Runtime localization is unchanged.
Baseline: `dev` / `0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference:
`refs/rollback/all-english-docs-20261015`.

On 2026-10-13, remaining Chinese contract prose was translated without changing requirements.
Existing English documents were reviewed and retained. Baseline: branch `dev`, HEAD
`5bea1d0bef04dd2ac718809852d3f657c7388e58`; local rollback reference:
`refs/rollback/english-contracts-guide-20261013`. Earlier uncommitted changes were preserved.

The two width-measurement TSV files now use the English sample-name alias
`TwoCarriagewaySixLane`. This is a documentation label, not a renamed game asset. To recover the
exact original sample names, replace that token with the Unicode string represented by these escapes:

```text
\u4e24\u5757\u677f\u516d\u8f66\u9053
```

No measurements, numeric tokens, bit patterns, sample suffixes or dispositions were changed.
Hashes in the original audit reports describe the original generated reports, not the translated
TSV bytes. Reverse the alias substitution before comparing original sample names or file hashes.

For a contributor workflow, see [Adding a bridge style with a coding agent](../Adding-Bridge-Styles-With-Coding-Agents.md).

Sidewalk routing revision (2026-10-17): baseline `dev` /
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/elevated-sidewalk-contract-20261017`. Existing local changes are preserved.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.


The [approved persistence directory and migration specification](../Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
consolidates assets under a UUID, removes that prefix from owned filenames and places external
snapshots in its Dependencies subdirectory. This 2026-10-18 decision is not yet implemented;
VT/mid-mip placement remains pending verification. Read section 7 for migration safeguards.
