# Repository Agent Instructions

## Scope

This file applies to the entire repository. It contains the repository-level rules that Codex must
load before doing any work. More specific `AGENTS.md` files may add instructions for their directory,
but they may not weaken or replace this file or the project contract.

Project-wide rules are defined in
[Project-wide reading scope](docs/contracts/reading-map.md#project-contract). All bridge generation rules and
their specialist routing are defined separately in
[Complete generation reading scope](docs/contracts/reading-map.md#bridge-generation-contract).
The complete [LOD specification](docs/specs/lod-spec.md) is mandatory for bridge generation, geometry and LOD work.
Both are normative. The summaries here do not replace their detailed requirements. Numbered
technical references below retain their original numbers in the [complete generation reading map](docs/contracts/reading-map.md#bridge-generation-contract); [project section 20](docs/constitution.md#project-20-immutable-mod-metadata-name) is the metadata rule.

## Mandatory reading

Read the required documents completely before the first edit. Do not rely on memory or on a previous
session.

Read [the constitution](docs/constitution.md), [development obligations](docs/contracts/development-contract.md),
and the complete [cross-environment specification](docs/specs/cross-environment-spec.md) for every task,
including documentation-only work and resumed or delegated tasks. Its design requirements always
apply; the separate compatibility test suite retains its manual/pre-release activation rules.

For every documentation change, read [Documentation standards](docs/documentation-standards.md)
and every instruction document being changed before editing.

| Work being performed | Required instructions |
| --- | --- |
| Any bridge, mesh, prefab, tower, cable, LOD, geometry, export or generation change | This file, the complete project and bridge generation contracts, and the nearest directory-level `AGENTS.md` |
| Runtime code under `src/BridgeBuilder` | This file, the complete project and bridge generation contracts, and [`src/BridgeBuilder/AGENTS.md`](src/BridgeBuilder/AGENTS.md) |
| Geometry identification or metadata generation under `tools/GeometryMetaprogram` | This file, contract sections 1–9 and 13, and [`tools/GeometryMetaprogram/AGENTS.md`](tools/GeometryMetaprogram/AGENTS.md) |
| Diagnosis or visual validation | Contract sections 1 and 6–9, plus the nearest directory-level `AGENTS.md` |
| Double-deck bridge work | The complete project and bridge generation contracts, especially section 11 |
| Non-Network virtual bridge prototypes | The complete project and bridge generation contracts and [virtual prototype preparation before generation](docs/contracts/reading-map.md#non-network-bridges-contract) |
| Movable bridge work (draw, lift, pedestrian or other mechanisms) | The complete project and bridge generation contracts and [movable-bridges-contract.md](docs/contracts/reading-map.md#movable-bridges-contract) |
| Cross-environment testing or a requested release | [Cross-environment testing contract](docs/contracts/reading-map.md#cross-environment-testing-contract); one manually triggered/pre-release round, with one independent agent per concrete environment |
| Git, branching, commit or integration work | Contract section 12 before changing files or branches |
| Build, install or generated-asset cleanup after a source update | Contract sections 6 and 13 |
| Self-check, historical migration or recovery notices | [Self-check contract](docs/contracts/reading-map.md#bridge-self-check-contract) |
| Instruction-document maintenance only | This file and every instruction file being changed |

When a task spans more than one row, combine their reading requirements. When scope is uncertain,
read the complete project and bridge generation contracts.

## Principles and applicable obligations

Apply the topic-specific reading table above before editing; combine rows when scopes overlap.
Detailed requirements formerly in this entry point now live in the following authorities.
Read the sections applicable to the task; their original scope remains unchanged.

- [Non-negotiable project rules](docs/constitution.md#repository-non-negotiable-project-rules)
- [Trusted internal execution](docs/constitution.md#repository-trusted-internal-execution)
- [Packaged textures belong to road running surfaces](docs/specs/material-spec.md#repository-packaged-textures-belong-to-road-running-surfaces)
- [Multilingual adaptation is required for every change](docs/contracts/development-contract.md#repository-multilingual-adaptation-is-required-for-every-change)
- [Current implementation policy](docs/specs/generation-spec.md#repository-current-implementation-policy)
- [First rule: fix reported bugs immediately](docs/contracts/development-contract.md#repository-first-rule-fix-reported-bugs-immediately)
- [Computer-use tools are prohibited](docs/constitution.md#repository-computer-use-tools-are-prohibited)
- [Native game UI windows](docs/specs/ui-spec.md#repository-native-game-ui-windows)
- [Costly operations require advance disclosure](docs/contracts/development-contract.md#repository-costly-operations-require-advance-disclosure)
- [Performance and deployment](docs/contracts/development-contract.md#repository-performance-and-deployment)
- [Parallel agents for multi-bridge tasks](docs/contracts/development-contract.md#repository-parallel-agents-for-multi-bridge-tasks)
- [Game and launcher startup is manual](docs/constitution.md#repository-game-and-launcher-startup-is-manual)
- [GitHub publication is optional](docs/contracts/development-contract.md#repository-github-publication-is-optional)
- [Release workflow](docs/contracts/development-contract.md#repository-release-workflow)
- [Elevated-road sidewalks: all bridge generators](docs/specs/generation-spec.md#repository-elevated-road-sidewalks-all-bridge-generators)
- [Geometry invariants that must remain visible at repository scope](docs/specs/generation-spec.md#repository-geometry-invariants-that-must-remain-visible-at-repository-scope)
- [Workflow](docs/contracts/development-contract.md#repository-workflow)
- [Maintaining these instructions](docs/constitution.md#repository-maintaining-these-instructions)
- [Source input ownership](docs/contracts/persistence-contract.md#repository-source-input-ownership)
- [Post-load self-check](docs/contracts/self-check-contract.md#repository-post-load-self-check)
- [Documentation location and authority](docs/constitution.md#repository-documentation-location-and-authority)
- [Documentation language and format](docs/specs/movable-spec.md#repository-documentation-language-and-format)
- [Movable bridge size](docs/specs/generation-spec.md#repository-movable-bridge-size)
- [Movable generation requirements](docs/specs/generation-spec.md#repository-movable-generation-requirements)
- [Offline source-atlas repeat materials](docs/specs/material-spec.md#repository-offline-source-atlas-repeat-materials)
- [Mod-owned precompiled material programs](docs/specs/material-spec.md#repository-dynamically-generated-persistent-material-shaders)
