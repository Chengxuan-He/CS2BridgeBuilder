# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md) and
[`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
This file replaces neither. Numbered bridge rules refer to the bridge generation contract.

## Runtime invariants

Read the [retained runtime invariants](../../docs/agent-contract/MOVABLE_INSTRUCTION_RECORDS.md#runtime-invariants-retained-from-srcbridgebuilderagentsmd)
in full before code changes. Apply the current implementation policy first; historical process gates are retired.
Prototype parameters and part maps remain hardcoded and independent of the original prototype file.
The retained requirements remain binding except where the accepted movable contract
sections 16–23 explicitly supersede historical marking/material recipes. Preserve native asset
ownership, API thread affinity, source provenance, skeletons, structure channels and independent
persistence. This relocation changes no unrelated runtime requirement.

## Performance and iteration

Follow bridge generation contract sections 13 and 23. Choose CPU/GPU execution and parallelism from
native API affinity, correctness and measured performance. Neither GPU conversion nor a specific
style branch is a prerequisite for implementation. Build/deploy coherent increments, not every edit.

## Completion

The user-approved movable LOD subdivision exception in bridge generation contract section 5 applies here. Preserve original structural vertices, materials and interpolated
attributes when necessary subdivisions separate merged LOD parts. Keep all authored-part width
rules and all-LOD coverage. The user revoked the prohibition on high-detail-derived movable LODs
on 2026-10-17: deriving, simplifying or supplementing LODs from measured prototype high-detail
geometry is permitted under bridge generation contract section 5 and its scoped topology exception.
Native bone ownership, existing materials and visual acceptance requirements remain unchanged.
Compilation of partial stages is not completion.

Persistence must implement contract section 17. Traverse serialized external dependencies to the
built-in boundary and save byte-identical files plus original CID sidecars under
`ImportedData/<bridgeUUID>/Dependencies/`. One bridge stores one copy per CID; two bridges store
separate copies of a shared CID. Do not deserialize or modify prefab instances to make these copies.
Do not rename copied prefabs, allocate replacement CIDs or rewrite their references. Keep generated
bridge/geometry identities distinct from these unchanged dependency snapshots. Missing dependencies
or differing contents at an existing same-CID destination fail persistence explicitly. File self-check
and cleanup must recognize UUID-directory ownership and intentional identical-CID duplicates.

Preserve native construction, elevation and upkeep fees under section 16.

Before replacing game-loaded binaries, stop `Cities2.exe`, verify it is absent, back up originals
and verify installed hashes. Build and deploy each coherent buildable code increment. Do not remove
healthy existing bridges merely because generation code changed; recover only concretely incompatible
style/UUID-owned assets under section 13. Documentation-only changes require no deployment.
Automated geometry and serialization checks are permitted but do not prove visual acceptance.
Keep unfinished native motion, near/far and cold-load checks explicit and continue unblocked work.

## Post-load self-check

Contract section 19 defines migration-first self-check and its backup/removal fallback. Wait for
native loading, mod initialization, PDX caching and active asset batches; observe both current
readiness and completion events so late mod loading is covered. Include cached user PrefabAssets
that failed registration without force-loading or republishing them. Validation of live references
is read-only and is separate from dependency persistence under section 17.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters section 19's per-UUID backup/move, then deletion fallback on backup/move failure.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Source isolation (2026-10-13): creating bridges/copies must never select another bridge's Prefab from ImportedData (including UUID dependency directories), even with the same CID. Filter before reading; use original/permitted sources or fail. Ownership remains path-string-only. See contract's Source isolation subsection; baseline dev/5bea1d0bef04dd2ac718809852d3f657c7388e58, rollback refs/rollback/no-foreign-bridge-source-20261013.

## Documentation language

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.
The project contract records the expanded 2026-10-15 documentation policy. Preserve current requirements and
historical evidence; the 2026-10-13 language-only baseline remains in the project contract.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.
Revision baseline (2026-10-16): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.


## Accepted movable generation baseline (2026-10-18)

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–23](../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-accepted-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Those sections consolidate the accepted current pipeline and supersede conflicting historical
paint/material recipes: original structural Surface/VT references; measured non-VT repeating road
and grating units plus bounded native VT composites; separate single-sided native skinned paint
with 0.001 m clearance on the original road bones, matching stack metadata and transforms.
No receiver cuts for markings, no child projectors, no structural receiver-only material copies,
no moving the road onto marking bones, and no runtime synchronization dependency. Acceptance is
specific to the latest Draw01 visual repair; new styles still require their own archetype data and
acceptance. Baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a plus preserved working changes.

Prior long instruction records are preserved in [retained movable instruction records](../../docs/agent-contract/MOVABLE_INSTRUCTION_RECORDS.md).
Use the current accepted contract for conflicting appearance/marking decisions.


Persistence layout revision (2026-10-19): branch `dev`, HEAD
`d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`, preserving prior working changes.
Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.
