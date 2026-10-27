# Contract documents

## Contents

- [Shared sidewalk requirements](#shared-sidewalk-requirements)
- [Specialist contracts](#specialist-contracts)
- [Measured fixed-bridge rules](#measured-fixed-bridge-rules)
- [Style registration and UI](#style-registration-and-ui)
- [Implementation records](#implementation-records)
- [Documentation language and evidence labels](#documentation-language-and-evidence-labels)

[PROJECT_CONTRACT.md](PROJECT_CONTRACT.md) governs project-wide workflow, documentation and metadata.
[BRIDGE_GENERATION_CONTRACT.md](BRIDGE_GENERATION_CONTRACT.md) is the independent general bridge
contract and the routing authority for all bridge generation work. Read it for fixed and movable
bridges alike, then follow its links to the applicable specialist contracts.

Apply the latest explicit user requirements within their stated scope and keep overlapping
normative paragraphs and AGENTS.md summaries synchronized. File modification time alone does
not establish a new requirement. The material contract governs material reuse, preparation,
bounded runtime conversion and material/package byte limits; the general bridge contract retains
geometry, ownership and complete-Prefab comparison requirements. Implementation reports, handoffs,
candidate inventories and old acceptance records are evidence for their recorded build/input only;
they do not override these contracts or establish acceptance of a later material migration.

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

[Material generation](MATERIAL_GENERATION_CONTRACT.md) governs every bridge material path,
including preview, creation, export and all LODs. Its first rule prohibits creating new materials
when reusing an original material in full. It also requires original resolution and pattern
scale, repeat units tiled at original density rather than very large baked images, and durable
known, preparable constant materials prepared once offline and copied during bridge creation.
Section 4 separately permits bounded variable-content composition and necessary conversion of
selected third-party road/track inputs unavailable before runtime, prioritizing direct reuse and
descriptor-only conversion. These exceptions never cover missing known prepared assets.
Sections 2 and 5 require at most 10,000,000 bytes of persisted material dependencies per generated
bridge and at most 50,000,000 bytes for the complete official SDK content ZIP. Prepared-directory
size, complete bridge size and package size are separate measurements. Native rendering
and persistence remain governed by their corresponding contracts.

The [CPU affine geometry execution rules](BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
apply to fixed and movable bridges. They define XYZ parameter matrices, sparse embedded-array
storage, independent numerical batches, native API thread boundaries and verification.
The [contributor generation step](../Adding-Bridge-Styles-With-Coding-Agents.md#affine-matrix-generation-step)
connects these requirements to extraction, regeneration and runtime assembly.

[Generator code organization](GENERATOR_CODE_CONTRACT.md) governs base-class inheritance,
one concrete generator file per style, shared lifecycle ownership and style-specific hooks.

[MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md) is the bridge generation contract's normative movable-bridge
supplement, incorporated by section 21. It defines a general method derived from the implemented
pipeline: selected-road geometry conversion, physical motion domains, existing materials and
prototype UV projection, separate native skinned paint, native Prefab graph construction, selective persistence
and unlocks. **Read sections 16–24 for material, marking, binding, LOD, track and persistence requirements,
together with the material generation contract.** Section 15 records
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

## Measured fixed-bridge rules

The general contract incorporates the scoped rules in these measured specifications:

- [Golden support mapping](GOLDEN_SUPPORT_MAPPING.md) and [return alignment](GOLDEN_RETURN_ALIGNMENT.md).
- [Golden ornament mapping](GOLDEN_ORNAMENT_MAPPING.md), including its attached lamps.
- [Grand reference width](GRAND_REFERENCE_WIDTH.md), the specifically authorized audit exception.
- [Extradosed02 cable anchors](EXTRADOSED02_CABLE_ANCHOR.md), using each assembly's measured reach.

[Elevated footway and support evidence](RAILING_AND_GOLDEN_TRUSS.md) and the width audits retain
source measurements and verification limits. Measurement timestamps and resource-version evidence
are not specification revision records. Supporting reports do not introduce competing rules.

## Style registration and UI

Bridge generation contract section 22 requires immediate UI synchronization and generator-router integration
whenever a new bridge style is created, with names in every supported UI language in the same
implementation increment.

## Implementation records

[Movable bridge implementation status](../Movable-Bridges-Implementation.md) tracks the ten B&P
mechanisms, their stable style identities, implementation evidence and remaining work.
Update this record with implementation and verification evidence; keep admission requirements
in the contracts.

## Documentation language and evidence labels

All project documentation is written in English. Preserve literal source identifiers, paths
and diagnostic quotations, with English explanations. Runtime localization is separate.

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

The [persistence directory and migration specification](../Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
places owned resources and dependency snapshots below each UUID. Creation and migration share
that layout, including VT and mid-mip resources. Native cold-load acceptance requires separate
in-game verification. Section 7 defines migration safeguards.
