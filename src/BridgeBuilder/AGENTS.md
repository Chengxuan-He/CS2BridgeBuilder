# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md) and
[`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
This file replaces neither. Numbered bridge rules refer to the bridge generation contract.

## Runtime invariants

Recorded numeric data uses versioned, checksum-validated, uncompressed BBN1 typed-array resources,
organized by bridge type and embedded in the DLL. Validate and read only a demanded resource
directly from its embedded stream into static arrays; no extraction files are needed. Do not
eagerly load unused bridge data. Follow bridge generation contract section 2; no loose-file
or installed-prototype dependency, giant initializer, or change to independently saved bridges.

Read the [runtime invariants](../../docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md#runtime-invariants)
in full before code changes. Apply the current implementation policy.
Prototype parameters and part maps remain hardcoded and independent of the original prototype file.
The movable and material contracts define appearance requirements. Preserve native asset
ownership, API thread affinity, source provenance, skeletons, structure channels and independent
persistence.

## Performance and iteration

Apply the [material generation contract](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md)
to every material path: full original-material reuse must create no new material;
tile genuine repeats at source density; section 2 permits verified prototype non-tiling UVs
to stretch with geometry while retaining source UVs, native textures and material identity.
Missing repeat evidence is not proof. Copy prepared constants without runtime rebaking. Keep their sources independent of
generated bridge ownership and deletion. Section 4 permits bounded variable-content composition
and necessary conversion of selected third-party road/track inputs unavailable before runtime,
after direct reuse and descriptor-only conversion have been considered. These exceptions do not
permit runtime rebuilding of known prepared resources, per-sampler baking or cross-bridge caches.
Use the packaged `Materials/<BridgeStyleId>/` directory relative to the mod installation;
source files are read-only and must be copied into independent bridge dependency snapshots.
Material directories exist only for actual payloads; do not require empty or placeholder-only folders.
Variants may share byte-identical textures through their own style's `Shared/<CID>.Texture`
directory. Preflight all dependencies before registration; do not resolve missing packaged
resources through another style or a generated bridge.

Apply [CPU affine geometry execution](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
through `AffineGeometry`: each XYZ axis may have independent width coefficients or remain
constant. Snapshot native data before scheduling independent LOD plans/disjoint vertex batches,
then join workers before creating or saving native assets. Keep dependent recipe stages in order,
including repeated-index semantics. Do not assume matrix execution requires dense per-vertex
storage, GPU work, or changes to topology, original road bones or materials.

Follow bridge generation contract sections 13 and 23. Choose CPU/GPU execution and parallelism from
native API affinity, correctness and measured performance. Neither GPU conversion nor a specific
style branch is a prerequisite for implementation. Build/deploy coherent increments, not every edit.

## Completion

The user-approved movable LOD subdivision exception in bridge generation contract section 5 applies here. Preserve original structural vertices, materials and interpolated
attributes when necessary subdivisions separate merged LOD parts. Keep all authored-part width
rules and all-LOD coverage. Deriving, simplifying or supplementing LODs from measured prototype high-detail
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
Before live-reference validation, supplement missing owner-local copies of other mods' materials
and required non-built-in textures/VT payloads, even for healthy current bridges. Use creation's
dependency traversal, preserve original bytes/CIDs and geometry, leave complete copies unchanged,
and report supplementation as migration requiring restart.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters section 19's per-UUID backup/move, then deletion fallback on backup/move failure.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Creation and copy operations must exclude another generated bridge's ImportedData resources,
including UUID dependency directories, before reading them even when CIDs match. Use original
or permitted packaged sources; fail if unavailable. Ownership remains UUID/path-string based.

## Documentation language

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.
Separately enforce material generation contract sections 2 and 5: at most 10,000,000 bytes for
one generated owner's complete persisted material dependencies and 50,000,000 bytes for the
complete official SDK content ZIP. A style's prepared-directory size proves neither limit.

## Movable generation requirements

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–24](../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Those sections define the generation pipeline: compatible original structural Surface/VT references
or required offline conversions under material contract section 2; measured non-VT repeating road
and grating units, plus the bounded variable-content and unknown-selected-input conversions
permitted by material generation contract section 4;
separate single-sided native skinned paint
with 0.001 m clearance on the original road bones, matching stack metadata and transforms.
No receiver cuts for markings, no child projectors, no structural receiver-only material copies,
no moving the road onto marking bones, and no runtime synchronization dependency. Acceptance is
specific to the tested style, input and build; every style requires its own evidence.

Read the [movable runtime invariants](../../docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md).
Use the material generation contract for material reuse, tiling and offline preparation.

Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.
