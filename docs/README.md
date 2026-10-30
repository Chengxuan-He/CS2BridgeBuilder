# Project documentation

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
| Native ownership, snapshots and cold loading | [Persistence policy](Bridge-Asset-Persistence.md) |
| Other specialist contracts | [Contract index](agent-contract/README.md) |

The movable material scheme is original non-road reuse plus composition/precomposition (offline material baking outside the game) only
for prototype-supported road appearance types. Other road types use original materials.
Asphalt, sidewalks and wheel-track/rut effects are classified separately per prototype/domain.
The original source mapping rule applies to non-movable bridges too. Package and owner material
limits are 55,000,000 and 10,000,000 bytes respectively, with different measurement scopes.

## Research and implementation evidence

| Document | What it establishes |
| --- | --- |
| [Native movable material repetition](Native-Movable-Material-Repetition.md) | Original meshes, UV seams, shaders and per-LOD measurements; not generated-output acceptance |
| [Material reuse assessment](Movable-Nonroad-Material-Reuse-Assessment.md) | Adopted scheme and current code paths that still need integration; not a new reverse-engineering claim |
| [Prepared materials](Prepared-Materials.md) | Packaging guidance and retained producer/fixture stages; historical counts are not a live inventory |
| [Shared material handoff](../tools/PreparedMaterials/SharedCurrent.md) | Recorded repairs, artifacts and remaining material/performance work; no automatic claim about today's live installation |
| [Package measurements](Paradox-Package-Size.md) | Exact packages and reporting scopes; a small partial package is not a deliverable |
| [Movable implementation record](Movable-Bridges-Implementation.md) | Version-specific development evidence; early absent-caller statements are historical |
| [UV repair evidence](Movable-UV-Density-Repair.md) | Recorded defects and implementation stages, including paths that fail the current contract |
| [Preview optimization](Preview-Optimization-Plan.md) | Optimization constraints and scoped timing evidence; no unmeasured end-to-end speedup |

Per-style network/render implementation reports retain their source measurements, hashes and
stage-specific tests. Read them with the current contracts and shared handoff. Preserve measured
facts; do not turn historical thresholds, uncalled-stage statements or previous readiness into
current requirements. Every acceptance statement must identify its build, source and tested scope.
