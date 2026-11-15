# Documentation and authority contract

This document is normative within its stated scope. Responsibility-based placement does not change requirement strength, existing specialist exceptions or explicit user authorizations. Specifications and contracts are equally normative; the split introduces no new precedence hierarchy.

<a id="contract-index-contract-documents"></a>

## Scope and reading obligations

[Project-wide reading scope](reading-map.md#project-contract) governs project-wide workflow, documentation and metadata.
[Bridge generation reading scope](reading-map.md#bridge-generation-contract) is the independent general bridge
contract and the routing authority for all bridge generation work. Read it for fixed and movable
bridges alike, then follow its links to the applicable specialist contracts.

Apply the latest explicit user requirements within their stated scope and keep overlapping
normative paragraphs and AGENTS.md summaries synchronized. File modification time alone does
not establish a new requirement. The material contract governs material reuse, preparation,
bounded runtime conversion and owner size reporting and complete package-size reporting; the general bridge contract retains
geometry, ownership and complete-Prefab comparison requirements. Implementation reports, handoffs,
candidate inventories and old acceptance records are evidence for their recorded build/input only;
they do not override these contracts or establish acceptance of a later material migration.

## Document filenames

Use lowercase kebab-case Markdown filenames. Contracts use `topic-contract.md`, specifications use
`topic-spec.md`, and navigation uses `reading-map.md`. Keep the conventional automatically discovered
`AGENTS.md` and `README.md` names; `constitution.md` and `architecture.md` already follow this rule.
Use titles that describe the current subject rather than a superseded implementation state.
Stable asset/style directory identifiers are not document filenames and need not be renamed.

When renaming a document, update repository links and active tool references with exact filename
case, retain applicable section anchors, and check for collisions before moving it. Preserve historical
path/name literals in recorded evidence; update their navigational links without rewriting history.

<a id="contract-index-contents"></a>

## Contents

- [Scope and reading obligations](#contract-index-contract-documents)
- [Current requirements](#documentation-index-current-requirements)
- [Specialist contracts](#contract-index-specialist-contracts)
- [Directory](#documentation-index-directory)
- [Implementation records](#contract-index-implementation-records)
- [Research and implementation evidence](#documentation-index-research-and-implementation-evidence)

<a id="documentation-index-current-requirements"></a>

## Current requirements

Related requirements: [Material Spec](../specs/material-spec.md#documentation-index-current-requirements-part-2), [Documentation-Material-Evidence](../archive/audits/documentation-material-evidence.md#documentation-index-current-requirements-part-3).

Read the contracts for requirements; research findings and successful historical tests do not
replace them. Documentation updates do not establish runtime implementation or game acceptance.

| Topic | Authoritative document |
| --- | --- |
| Repository workflow, documentation and lifecycle | [Project contract](reading-map.md#project-contract) and [AGENTS](reading-map.md#repository) |
| All fixed and movable bridge generation | [General bridge contract](reading-map.md#bridge-generation-contract) |
| Non-Network virtual prototypes before actual bridge generation | [Virtual bridge prototype preparation](reading-map.md#non-network-bridges-contract) |
| Source UV projection, tiling/stretching, every part/LOD/layer/axis | [General source-mapping rule](../specs/generation-spec.md#generation-preserve-original-uv-projection-and-mapping-mode) |
| Movable source roles, composition gate, mechanisms and markings | [Movable contract](reading-map.md#movable-bridges-contract) |
| Material reuse, preparation, channels, budgets and verification | [Material contract](reading-map.md#material-generation-contract) |
| Native ownership, snapshots and cold loading | [Persistence policy](reading-map.md#bridge-asset-persistence) |
| Self-check, migration/recovery and restart | [Self-check contract](reading-map.md#bridge-self-check-contract) |
| Other specialist contracts | [Contract index](reading-map.md#former-contract-index) |

<a id="contract-index-specialist-contracts"></a>

## Specialist contracts

Related requirements: [Material Spec](../specs/material-spec.md#contract-index-specialist-contracts-part-2), [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts-part-3), [Generation Spec](../specs/generation-spec.md#contract-index-specialist-contracts-part-4), [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts-part-5).

The paired [self-check contract and specification](reading-map.md#bridge-self-check-contract) are the authority for inspection,
historical migration/recovery, normal completion, result UI and mandatory full-process restart.

[Cross-environment testing](reading-map.md#cross-environment-testing-contract) governs explicit manual requests
and one round before a requested release, with one independent agent per environment and explicit
release decisions for unresolved required cases. It does not run on every game startup.

See [Scope and authority](material-contract.md#material-scope-and-authority).

See [Runtime invariants](movable-contract.md#movable-runtime-runtime-invariants).

<a id="contract-index-specialist-contracts-part-3"></a>

## Material obligations

Related requirements: [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts), [Material Spec](../specs/material-spec.md#contract-index-specialist-contracts-part-2), [Generation Spec](../specs/generation-spec.md#contract-index-specialist-contracts-part-4), [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts-part-5).

Read the complete [material generation scope](reading-map.md#material-generation-contract) for every material path, including preview, creation, export and all LODs. That scope owns reuse, resolution and mapping, offline preparation, bounded runtime exceptions and separate owner/package measurements; this navigation does not replace any of those requirements.

<a id="contract-index-specialist-contracts-part-5"></a>

## Generator and lifecycle obligations

Related requirements: [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts), [Material Spec](../specs/material-spec.md#contract-index-specialist-contracts-part-2), [Documentation Contract](documentation-contract.md#contract-index-specialist-contracts-part-3), [Generation Spec](../specs/generation-spec.md#contract-index-specialist-contracts-part-4).

[Generator code organization](reading-map.md#generator-code-contract) governs base-class inheritance,
one concrete generator file per style, shared lifecycle ownership and style-specific hooks.

[Movable bridge reading scope](reading-map.md#movable-bridges-contract) is the bridge generation contract's normative movable-bridge
supplement, incorporated by section 21. It defines a general method derived from the implemented
pipeline: selected-road geometry conversion, physical motion domains, existing materials and
prototype UV projection, separate native skinned paint, native Prefab graph construction, selective persistence
and unlocks. **Read sections 16–24 for material, marking, binding, LOD, track and persistence requirements,
together with the material generation contract.** Section 15 records
encountered failures and required solutions. Acceptance is recorded
per style; the general method does not claim every registered generator is complete. Supporting
audits retain measurements and implementation history rather than overriding current requirements.
[Bridge asset persistence and reference policy](reading-map.md#bridge-asset-persistence) is the normative
supplement incorporated by [bridge generation contract section 17](persistence-contract.md#generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge). It distinguishes owned generated data,
unchanged native references, external dependency snapshots and temporary preview objects.
The repository and directory-level AGENTS.md files route agents to the applicable requirements.

[Bridge preview specification](reading-map.md#bridge-preview-specification) consolidates the existing preview
requirements, including complete approaches and open movable mechanisms, temporary resource
ownership, UI errors and human acceptance. Implementation history is maintained separately.

<a id="contract-index-measured-fixed-bridge-rules"></a>

## Measured fixed-bridge rules

Use measured source geometry, assembly membership and LOD correspondence for fixed bridges. The general generation contract governs project requirements. Bridge-specific research records are maintained separately as implementation evidence.

For static-object and other non-Network sources, follow the normative
[virtual bridge prototype contract](reading-map.md#non-network-bridges-contract) before actual Bridge Builder generation.

<a id="contract-index-documentation-language-and-evidence-labels-part-2"></a>

## Contributor workflow

Related requirements: [Constitution](../constitution.md#contract-index-documentation-language-and-evidence-labels), [Persistence Spec](../specs/persistence-spec.md#contract-index-documentation-language-and-evidence-labels-part-3), [Material Contract](material-contract.md#contract-index-documentation-language-and-evidence-labels-part-4).

For a contributor workflow, see [Adding a bridge style with a coding agent](../guides/adding-bridge-styles-with-coding-agents.md).

<a id="documentation-index-project-documentation"></a>

<a id="documentation-index-directory"></a>

## Directory

[Manual and pre-release cross-environment testing](reading-map.md#cross-environment-testing-contract)
defines environment ownership, isolated permission/mapping fixtures and truthful release acceptance.

| Area | Contents |
| --- | --- |
| [Contracts](reading-map.md#former-contract-index) | Project rules, including persistence, preview and deletion |
| [Contributor guides](../guides/README.md) | Adding a style and inspecting geometry |
| [Specifications](../specs/README.md) | Generation, materials, persistence and interface behavior |
| [Publishing](../publishing/README.md) | SDK delivery, package reporting and release records |
| [Research and history](../archive/README.md) | Audit evidence, incidents, dated implementation records and style archives |

<a id="documentation-index-workflow-design"></a>

## Generation workflow

[Bridge generation workflow](generation-contract.md) explains request scheduling, fixed
and movable generation, double-deck branches, offline inputs, preview, persistence and cleanup
with workflow diagrams. Read its lifecycle obligations with the paired behavioral specification.

<a id="contract-index-implementation-records"></a>

## Implementation records

Record implementation and verification by stable style ID, build and tested scope. Keep admission and acceptance requirements in the contracts.

<a id="documentation-index-research-and-implementation-evidence"></a>

## Research and implementation evidence

| Document | What it establishes |
| --- | --- |
| [Dependency persistence verification](../archive/audits/dependency-persistence-verification.md) | Observed candidate86 self-check timing and recorded verification scope |
| [Historical material checkpoints](../archive/implementation/development-material-checkpoint.md) | Archived candidate-specific measurements; not current installation claims |
| [Native movable material repetition](../archive/audits/native-movable-material-repetition.md) | Original meshes, UV seams, shaders and per-LOD measurements; not generated-output acceptance |
| [Material reuse assessment](../archive/audits/movable-nonroad-material-reuse-assessment.md) | Adopted scheme and current code paths that still need integration; not a new reverse-engineering claim |
| [Prepared material evidence](../archive/audits/prepared-materials-evidence.md) | Retained producer/fixture stages; historical counts are not a live inventory |
| [Shared material handoff](../archive/implementation/shared-material-integration-20261028.md) | Recorded repairs, artifacts and remaining material/performance work; no automatic claim about today's live installation |
| [Package measurements](../publishing/paradox-package-size.md) | Exact packages and reporting scopes; a small partial package is not a deliverable |
| [Movable implementation record](../archive/implementation/movable-bridges-implementation.md) | Version-specific development evidence; early absent-caller statements are historical |
| [UV repair evidence](../archive/audits/movable-uv-density-repair.md) | Recorded defects and implementation stages, including paths that fail the current contract |
| [Preview optimization](../archive/implementation/preview-optimization-plan.md) | Optimization constraints and scoped timing evidence; no unmeasured end-to-end speedup |

<a id="documentation-index-reading-a-single-style-report"></a>

## Reading a single-style report

For current movable-style behavior, start with the [registered generators](../../src/BridgeBuilder/Bridges/BridgeGeneratorRouter.cs), follow the selected generator into its network and render assemblies, and use the [current contracts](reading-map.md#former-contract-index) for requirements. The ten registered movable styles have generator routes. Per-style branches, implementation-stage checklists and visual acceptance results describe their recorded builds. Native measurements, bone and part memberships, LOD identities, hashes and photographs remain evidence at their recorded scope. The [material handoff](../archive/implementation/shared-material-integration-20261028.md) records its own tested inputs. Compilation or a prior material test alone does not establish current in-game appearance.

For historical reports, measurements and style-specific tool notes, browse the [bridge-style archive](../archive/bridge-styles/README.md). Current requirements are in the [general generation contract](reading-map.md#bridge-generation-contract).

