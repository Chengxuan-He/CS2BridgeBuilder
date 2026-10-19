# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md) and
[`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
This file replaces neither. Numbered bridge rules refer to the bridge generation contract.

## Runtime invariants

Read the [retained runtime invariants](../../docs/agent-contract/MOVABLE_INSTRUCTION_RECORDS.md#runtime-invariants-retained-from-srcbridgebuilderagentsmd)
in full before code changes. They remain binding except where the accepted movable contract
sections 16–23 explicitly supersede historical marking/material recipes. Preserve native asset
ownership, API thread affinity, source provenance, skeletons, structure channels and independent
persistence. This relocation changes no unrelated runtime requirement.

## GPU computation and parallel generation

Follow bridge generation contract section 23: expensive repeated matrix/pixel/image calculations must run
on the GPU. Assume a game-capable GPU, never a specific vendor or CUDA availability. Use GPU
facilities compatible with supported vendors. Run independent bridge-generation steps in parallel
wherever possible while respecting data dependencies, resource ownership and native API thread
and graphics-context requirements. This does not authorize parallel calls to thread-affine APIs.

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

Before modifying runtime code, run `taskkill /IM Cities2.exe /F` even if a city/save is running,
without asking whether to save or waiting for a manual exit. The user explicitly authorizes losing
unsaved progress for this workflow. Verify the process is absent; still perform the post-edit
shutdown and deployment procedure required below.

Preserve native construction, elevation and upkeep fees. Do not allocate pricing-only prefabs,
persist a custom scalar total or override runtime prices. See bridge generation contract section 16.

After any source-code edit, follow contract section 13: invoke the exact `Cities2.exe` kill command,
verify the game is stopped, and determine whether bridge-generation code changed. If it did, remove
all mod-created bridges only for the affected style IDs and their UUID-owned artifacts; preserve
unaffected styles. If generation code did not change, remove no existing bridges. Shared generation
changes require tracing the affected style IDs before cleanup. After completing any code change,
immediately build and deploy to the game
without waiting for another request; back up the existing installation and verify installed files
against the build output as required by section 13. Compilation is not visual verification. For visual work, do not run unit
tests; inspect generated geometry and confirm both near and far views in game.

Documentation-only edits require no game shutdown, bridge removal, build or deployment. The
2026-10-15 cleanup revision is recorded in contract section 13 at branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`, rollback reference
`refs/rollback/scoped-cleanup-docs-20261015`.


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
