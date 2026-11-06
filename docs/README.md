# Project documentation

## Directory

| Area | Contents |
| --- | --- |
| [Contracts](agent-contract/README.md) | Project rules, including persistence, preview and deletion |
| [Contributor guides](guides/README.md) | Adding a style and inspecting geometry |
| [Design and reference](design/README.md) | Generation, materials, persistence and interface behavior |
| [Publishing](publishing/README.md) | SDK delivery, package reporting and release records |
| [Research and history](archive/README.md) | Audit evidence, incidents, dated implementation records and style archives |

## Current requirements

Read the contracts for requirements; research findings and successful historical tests do not
replace them. Documentation updates do not establish runtime implementation or game acceptance.

| Topic | Authoritative document |
| --- | --- |
| Repository workflow, documentation and lifecycle | [Project contract](agent-contract/PROJECT_CONTRACT.md) and [AGENTS](../AGENTS.md) |
| All fixed and movable bridge generation | [General bridge contract](agent-contract/BRIDGE_GENERATION_CONTRACT.md) |
| Source UV projection, tiling/stretching, every part/LOD/layer/axis | [General source-mapping rule](agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode) |
| Movable source roles, composition gate, mechanisms and markings | [Movable contract](agent-contract/MOVABLE_BRIDGES_CONTRACT.md) |
| Material reuse, preparation, channels, budgets and verification | [Material contract](agent-contract/MATERIAL_GENERATION_CONTRACT.md) |
| Native ownership, snapshots and cold loading | [Persistence policy](agent-contract/Bridge-Asset-Persistence.md) |
| Other specialist contracts | [Contract index](agent-contract/README.md) |

The movable material scheme is original non-road reuse plus composition/precomposition (offline material baking outside the game) only
for prototype-supported road appearance types. Other road types use original materials.
Asphalt, sidewalks and wheel-track/rut effects are classified separately per prototype/domain.
The original source mapping rule applies to non-movable bridges too. The package limit is 55,000,000 bytes. Complete owner material bytes are measured separately
for information, without a per-owner ceiling.

The native96 local build's official SDK ZIP measured 77,080,830 bytes. It exceeded the
55,000,000-byte publication limit and was deployed locally only. The owner-size measurement
and the complete ZIP measurement serve different purposes.

## Workflow design

[Bridge generation workflow](design/generation/Bridge-Generation-Workflow.md) explains request scheduling, fixed
and movable generation, double-deck branches, offline inputs, preview, persistence and cleanup
with workflow diagrams. It describes execution; the contracts above remain authoritative.

## Research and implementation evidence

| Document | What it establishes |
| --- | --- |
| [Dependency persistence and self-check](design/persistence/Dependency-Persistence-Implementation.md) | Forward dependency traversal, migration boundaries and observed candidate86 self-check |
| [Historical material checkpoints](archive/implementation/Development-Material-Checkpoint.md) | Archived candidate-specific measurements; not current installation claims |
| [Native movable material repetition](archive/audits/Native-Movable-Material-Repetition.md) | Original meshes, UV seams, shaders and per-LOD measurements; not generated-output acceptance |
| [Material reuse assessment](archive/audits/Movable-Nonroad-Material-Reuse-Assessment.md) | Adopted scheme and current code paths that still need integration; not a new reverse-engineering claim |
| [Prepared materials](design/materials/Prepared-Materials.md) | Packaging guidance and retained producer/fixture stages; historical counts are not a live inventory |
| [Shared material handoff](../tools/PreparedMaterials/SharedCurrent.md) | Recorded repairs, artifacts and remaining material/performance work; no automatic claim about today's live installation |
| [Package measurements](publishing/Paradox-Package-Size.md) | Exact packages and reporting scopes; a small partial package is not a deliverable |
| [Movable implementation record](archive/implementation/Movable-Bridges-Implementation.md) | Version-specific development evidence; early absent-caller statements are historical |
| [UV repair evidence](archive/audits/Movable-UV-Density-Repair.md) | Recorded defects and implementation stages, including paths that fail the current contract |
| [Preview optimization](archive/implementation/Preview-Optimization-Plan.md) | Optimization constraints and scoped timing evidence; no unmeasured end-to-end speedup |

### Reading a single-style report

For current movable-style behavior, start with the [registered generators](../src/BridgeBuilder/Bridges/BridgeGeneratorRouter.cs), follow the selected generator into its network and render assemblies, and use the [current contracts](agent-contract/README.md) for requirements. The ten registered movable styles have generator routes. Per-style branches, implementation-stage checklists and visual acceptance results describe their recorded builds. Native measurements, bone and part memberships, LOD identities, hashes and photographs remain evidence at their recorded scope. The [material handoff](../tools/PreparedMaterials/SharedCurrent.md) records its own tested inputs. Compilation or a prior material test alone does not establish current in-game appearance.

For historical reports, measurements and style-specific tool notes, browse the [bridge-style archive](archive/bridge-styles/README.md). Current requirements are in the [general generation contract](agent-contract/BRIDGE_GENERATION_CONTRACT.md).
