# Runtime Bridge Generation Instructions

## Scope and required reading

Applies to `src/BridgeBuilder/**` in addition to [root instructions](../../AGENTS.md). Before editing, read the complete [project contract](../../docs/agent-contract/PROJECT_CONTRACT.md), [generation contract](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md) and [runtime invariants](../../docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md). Numbered rules refer to the generation contract. Read the full [material contract](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md) for material work and [movable sections 16–24](../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-generation-pipeline-and-source-map) before movable changes.

## Generation and execution

Follow [trusted internal execution](../../AGENTS.md#trusted-internal-execution): remove internal
memory/computation/result validation, retaining actual file/network IO boundary validation and
operation failure handling. Offline assertions are development tools, not runtime invariants.
Business/native rules, cancellation and resource lifetimes remain unchanged.

Use measured hardcoded prototypes, memberships, BBN1 resources and construction recipes, without runtime prototype rediscovery or dependence on original authoring files. Generation sections 2, 4, 5 and 8 govern exact numerical lifetime, prototype complexity, LOD exceptions and authored-part mapping. The root-visible pedestrian/Lift01/Lift02 truss and sidewalk invariants apply at every LOD. Preserve adjoining boundaries, bones, materials and native behavior; compilation alone is not completion.

[CPU affine execution](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution) requires shared packed XYZ processing with ordered rounding, immutable snapshots, joined workers and owned native output. Deferred preparation acquires the request parameter scope only when construction begins; callbacks own copied output data. No permanent numerical caches, forced GC or promised immediate RSS decrease. Independent requests cannot clear one another's resources.

[Continuous preview execution](../../docs/agent-contract/Bridge-Preview-Specification.md#continuous-background-generation-and-asynchronous-native-dispatch) uses detached background computation and asynchronous native dispatch; no artificial frame slicing/6 ms budget. Preserve cancellation, native affinity, request-local reuse and measured viewport supersampling. Formal creation retains the same data semantics.

## Materials, ownership and persistence

The material contract governs packaged road-surface-only textures, original non-road sampling, one elevated branch, noexcept source/receiver routing, independent asphalt/sidewalk/wear support, all-LOD marking removal/PBR/mips, offline repeats and bounded runtime conversions. Its two generated programs require mod-enabled startup registration; disabling/uninstalling Bridge Builder is not an acceptance condition. No per-frame transform/rebinding controller, receiver cuts, structural receiver-only copies or generic paint replacement. Preserve explicit native attachment exceptions and their behavior.

Generation sections 16–19 and [asset persistence](../../docs/agent-contract/Bridge-Asset-Persistence.md) govern unchanged native fees/unlocks/locales, byte-identical external copies with original CIDs, per-owner dependencies, short filenames, VT/mips, collision handling and additive legacy conversion. Valid other-owner inputs are permitted; never mutate their sources. [Deletion](../../docs/agent-contract/BRIDGE_DELETION.md) governs player removal.

Self-check waits for native/mod/PDX readiness and active batches; include failed-registration cached assets without force-loading. Complete missing owner-local dependency copies before live-reference validation, using creation traversal. Preserve UUID/CID/geometry; migration precedes backed-up removal, backup failure still falls through to deletion, and restart is required. No live unregistration. Follow the complete [reporting rule](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#self-check-result-summary): twelve languages, one count per UUID, only nonzero categories, one restart reminder, silent no-action runs, no invented owner counts for global errors.

## Verification and delivery

Compiled shader payloads follow the root variable-length compiled-output rule. Rebuild every
affected nested record and container; equal-length replacement is not a compatibility requirement.
Verify shorter and longer outputs, multiple slots and repeated compilation as well as malformed input.

Generation section 24 requires complete-Prefab size comparison, including dependencies. Material owner bytes are informational without a ceiling; the complete SDK ZIP is reported separately without a hard ceiling. A prepared directory or root-only size proves neither.

Follow root localization and cross-codebase compatibility review for every change. Documentation remains English. Build/deploy coherent increments under sections 13 and 23, preserving healthy assets. Never close or launch the player's game: deploy only after confirming it is absent, backing up installed originals and verifying hashes. Documentation-only changes need no build/deployment. Report native motion, near/far, mod-enabled cold-load and visual acceptance separately from automated checks; continue unblocked work while human checks remain pending.
