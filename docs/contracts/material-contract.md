# Material boundary contract

Required companion: [Material Spec](../specs/material-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not change requirement strength, existing specialist exceptions or explicit user authorizations. Specifications and contracts are equally normative; the split introduces no new precedence hierarchy.

<a id="material-material-generation-contract"></a>

<a id="material-scope-and-authority"></a>

## Scope and authority

Related requirements: [Material Spec](../specs/material-spec.md#material-scope-and-authority-part-2).

[Trusted internal execution](../constitution.md#repository-trusted-internal-execution) applies throughout this
pipeline, including native memory calls, compilation and texture/GPU computation. Verification
requirements below are offline development evidence or actual file/network IO boundary checks;
they do not authorize runtime validation of internal results or repeated owned-data checks.

This contract governs materials for fixed and movable bridges, every part and LOD, and
preview, creation, regeneration and export. Geometry and native behavior remain governed by
[bridge generation](reading-map.md#bridge-generation-contract); movable appearance roles are defined in
[movable generation sections 17 and 24](../specs/movable-spec.md#movable-17-material-families-uv-density-and-texture-ownership).
Persistence follows the [asset persistence policy](reading-map.md#bridge-asset-persistence).

Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose road and rail running surfaces need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable-bridge road and rail running-surface processing scope.

The generation/material owner selects only the authorized [custom material scope and programs](../specs/custom-shader-spec.md#custom-shader-table).
Original prototype materials remain the default. Necessary non-Network conversion may use a
compatible native shader or the scoped mod-owned precompiled appearance program when native
semantics are insufficient, while preserving complete native dependencies and source assets.
This grants no runtime material baking, arbitrary recoloring, owner shader copies or runtime
geometry classification. Selected native road materials retain their existing scope.

## Native conversion responsibility

The material producer owns source/target consumer evidence and the complete per-slot/LOD
appearance result under [native consumers and instance palettes](../specs/material-spec.md#native-material-consumers-and-instance-palette-preservation).
It must supply the persistence owner with the complete native dependency graph and distinct
identities for changed resources, retaining unchanged sources and the no-owner-shader boundary.
The verification owner records native cold-read, GPU and visual evidence separately, including
preserved-channel checks, identity bypasses, approximation limits and storage/performance costs.
Serialized property presence alone does not fulfill this postcondition. Actual native/IO failures
retain the non-throwing result and cleanup obligations below; add no runtime internal checks.
Use the existing generation/preview operation boundary to contain and report failures. This
conversion does not require an unrelated generator API redesign, replacing every helper return
with null propagation, or changing successful-call interfaces. Never silently return an unchanged
incompatible material, omit a part or invoke baking as a failure fallback.

<a id="material-complete-material-processing-without-propagated-exceptions"></a>

## Complete material processing without propagated exceptions

Related requirements: [Material Spec](../specs/material-spec.md#material-complete-material-processing-without-propagated-exceptions-part-2), [Material Contract](material-contract.md#material-complete-material-processing-without-propagated-exceptions-part-3).

The complete material pipeline must have a non-throwing result contract, covering
classification, source/dependency resolution, adaptation, assembly, persistence,
preview, creation, regeneration and export at every part and LOD. Mod-owned material
code must not deliberately throw for a routing or adaptation outcome. Contain exceptions
from native APIs, external decoders and IO at their operation boundaries and translate
them into explicit failure results with diagnostics. Propagate those results through
the caller chain without throwing, silently omitting content or reporting success.

<a id="material-complete-material-processing-without-propagated-exceptions-part-3"></a>

## Resource failures and cleanup

Related requirements: [Material Contract](material-contract.md#material-complete-material-processing-without-propagated-exceptions), [Material Spec](../specs/material-spec.md#material-complete-material-processing-without-propagated-exceptions-part-2).

Genuinely missing/corrupt required resources, invalid descriptor data or a failed native write
may return a clear non-exception failure. Include request, stage, source identity, affected
part/LOD, dependency and cause. Release owned partial output and preserve sources. Localized
preview/creation failure applies to these fatal operations; compatibility differences are
offline diagnostics; do not add runtime internal-result checks or ERROR failures.

Recorded implementation limitations and isolated-test scope are retained in the [historical material audit](../archive/audits/material-implementation-records.md#native118-through-native121).

<a id="material-meaning-of-precomposition"></a>

## Meaning of precomposition

**Precomposition means material baking outside the game.** Bake the eligible road composite
with offline authoring/baking tools before distribution, save the finished native material,
texture and complete mip/VT dependencies, and include them in the mod package. The game loads,
copies when ownership requires it, and binds those finished resources without rebaking,
resampling or recompressing their texture payloads.

Baking at game startup, asset loading, first use, preview, bridge creation or export is runtime
work, not precomposition. An in-memory or disk cache of an in-game bake is not a precomposed
material. The separate [bounded runtime-composition exceptions](../specs/material-spec.md#material-prototype-appearance-composition) do not change this meaning.
Road precomposition remains subject to the prototype-supported-type gate; non-road bindings retain native texture references. Preserve applicable mapping rules and original
resolution, no full-road pattern expansion, independent persistence, owner size reporting and complete package-size reporting.

<a id="material-independent-persisted-rendering-without-runtime-synchronization"></a>

## Independent persisted rendering without runtime synchronization

Generated programs follow the [startup-registration requirements](material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders). The following native lifecycle restrictions also apply.

Save the bridge material representation with stable references to the mod-owned precompiled programs. Native rendering and motion operate after registration without a mod-owned
transform controller. Saving files alone does not prove successful startup registration.

Do not synchronize native network rendering data as bridge leaves move. This prohibition
includes per-frame or event-driven network-buffer updates, material/renderer rebinding,
runtime synchronization managers, and any equivalent controller required to preserve the
material after loading. Renaming such work as animation integration or cache maintenance does
not change the restriction. The game's own persisted native animation and rendering lifecycle
remains permitted; a mod-dependent synchronization layer does not.

The output must be a native persistable representation. Prefer complete source retention;
otherwise produce the authorized compatibility representation with explicit differences. Validate
motion, every LOD and mod-enabled cold loading for this implementation. Record the startup-registration dependency, missing equivalence and evidence limits without converting compatibility shortcomings into generation failure. An
approximation is not completion of exact original-material fidelity.
The earlier independent-loading experiment is historical evidence, not a current acceptance gate;
the [authorized movable-program implementation](material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders) is now active. These constraints do not relax the open-ended source-material
and non-throwing pipeline requirements above.

<a id="material-dynamically-generated-and-independently-persisted-material-shaders"></a>

## Mod-owned precompiled material programs

The mod owns and distributes the precompiled native program bundles. The [program specification](../specs/custom-shader-spec.md#native-program-graphs-stable-identity-and-registration) defines stable identities and queue variants. Bridges persist Surface references and ordinary appearance resources, never a program payload or registration recipe.

| Responsible boundary | Input | Postconditions and failure obligations |
| --- | --- | --- |
| Offline authoring and package | Authorized source semantics, complete native graph | Precompiles only the families in the [custom shader table](../specs/custom-shader-spec.md#custom-shader-table); variable-length outputs and matching layouts are packaged with the mod. No runtime compiler fallback. |
| Program/queue registry | Packaged program family, full native integer queue and deterministic logical-family/queue hash | Loads the packaged native graph on the main thread; specializes permitted queue/identity fields in memory, preserves source semantics, reuses identical registrations and never overwrites unrelated descriptions. Retains loaded resources while native consumers reference them. |
| Generation and preview | Actual source descriptor and native renderer priority | Bind the stable program/queue identity. BoneDecal uses queue 2000 + native rendererPriority. Do not create an owner program FileAsset, CID, dependency root or export payload. |
| Mod.OnLoad and game-load completion restoration | Currently discovered owned current Surface headers, explicit integer queues and embedded mod bundles | Both lifecycle boundaries own restoration under the [custom shader loading workflow](../specs/custom-shader-spec.md#custom-shader-loading-workflow): defaults and every discovered referenced family/queue variant are registered before dependent realization. Game-load completion includes Surfaces discovered during loading. No owner shader bundles or historical hash restoration. |
| Self-check | Confirmed owner and concrete obsolete custom-program protocol evidence | Applies [scoped retirement](self-check-contract.md#self-check-obsolete-custom-program-retirement), not shader migration. Shared registration failures do not select all bridges for removal. |

Native API and IO failures follow the existing non-throwing result contract. No HLSL compilation, Shader/Material recipe reconstruction, new external bootstrap or per-frame synchronization is introduced. Existing native attachment exceptions remain unchanged.

<a id="material-dynamically-generated-and-independently-persisted-material-shaders-part-3"></a>

## Authorized custom program scope

Related requirements: [Material Contract](material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders), [Material Spec](../specs/material-spec.md#material-dynamically-generated-and-independently-persisted-material-shaders-part-2), [Material Spec](../specs/material-spec.md#material-dynamically-generated-and-independently-persisted-material-shaders-part-4).

The [custom shader table](../specs/custom-shader-spec.md#custom-shader-table) is the single authority for program purpose, required consumers, native alternatives, implementation status and ownership. Ordinary moving mesh attachments, including recursively selected nested meshes, join the mechanism render graph with unit-weight mounting bones. Conditional visibility, actual independent animation, instance-color and emissive attachments, and native light effects explicitly retain their existing native attachment/BoneHistory behavior. These exceptions do not authorize a new Bridge Builder transform-tracking controller or per-frame synchronization service. Mere presence of an animation component does not establish actual independent motion.

Bridge Builder must remain installed and enabled to supply and register the packaged programs and referenced queue variants. Do not persist shader programs per bridge, mutate shared game resources, add an external bootstrap or synchronize transforms per frame. Verify creation, preview and cold loading separately; shader-function tests alone are not acceptance. Legacy owner-persisted custom-program bridges follow self-check retirement, not compatibility migration.

<a id="material-5-per-bridge-material-directory-and-mod-packaging"></a>

## Per-bridge material directory and mod packaging

Related requirements: [Material Spec](../specs/material-spec.md#material-5-per-bridge-material-directory-and-mod-packaging-part-2).

Measure the informational generated-owner material footprint over the complete saved bridge's material dependency
closure, including descriptors, non-built-in texture/VT payloads, required mip dependencies and
their persistence metadata. Count each shared file once within that owner. Built-in dependencies
that remain original references require no copied payload bytes; list these references separately.
The prepared files for a style are a separate measurement, not a substitute for each generated bridge's
complete informational material measurement. Include selected-road dependencies and permitted runtime
outputs in the owner measurement. If a complete bridge graph has not been generated, report that
measurement as unavailable rather than substituting the prepared-directory size.

Measure the complete actual official SDK content ZIP, including every bridge style, DLL,
material, dependency, symbol, UI file, license and archive overhead. Include required supplemental
downloads in aggregate delivery reporting. There is no hard package byte ceiling. Source checkout,
unpacked build and generated-owner sizes remain separate measurements. Reduce redundant storage
without losing source resolution, mapping-specific pattern scale, required LODs or independent
persistence. Size reduction never permits missing assets, runtime baking of known preparable
constants, or dependencies on deleted sources or other generated bridges.

<a id="material-packaging-and-source-resolution"></a>

## Packaging and source resolution

Related requirements: [Persistence Contract](persistence-contract.md#material-packaging-and-source-resolution-part-2), [Material Contract](material-contract.md#material-packaging-and-source-resolution-part-3).

Both the project build and `tools/Build.ps1`, and subsequent installation/publication packaging,
must recursively include the prepared payloads without changing their bytes or flattening the
style directories. Exclude documentation, `.gitkeep` placeholders, temporary files and offline
authoring intermediates. Omit directories left empty after filtering. Verify relative paths,
payload hashes and the absence of empty or placeholder-only directories in the final package. Directory
presence alone does not establish that usable materials have been prepared or packaged.

The road appearance owner must allocate variable composition assets in the owning bridge's
database with owner-specific names. It must never use the prepared source's database or name
for writes. Only locally created assets enter relocation, cleanup or ownership-transfer lists;
prepared materials and built-in textures never enter those lists.

<a id="material-packaging-and-source-resolution-part-3"></a>

## Packaging evidence

Related requirements: [Material Contract](material-contract.md#material-packaging-and-source-resolution), [Persistence Contract](persistence-contract.md#material-packaging-and-source-resolution-part-2).

Package acceptance follows [the prepared layout](../specs/material-spec.md#material-5-per-bridge-material-directory-and-mod-packaging-part-2)
and [material verification](../specs/material-spec.md#material-verification-requirements).
Recorded implementation status belongs to the [material audit](../archive/audits/material-implementation-records.md);
directory presence does not prove runtime use.

<a id="material-6-verification"></a>

## Verification

Acceptance criteria are maintained in [material verification requirements](../specs/material-spec.md#material-verification-requirements); required scope and evidence limits remain unchanged.

<a id="material-prepared-material-discovery-boundary"></a>

## Prepared material discovery boundary

Ship offline preparation inputs under `~Materials`, never the automatically discovered `Materials` directory. Native file-system discovery skips child directories beginning with `~`; the dedicated on-demand database uses `~Materials` itself as its root. Preserve original native filenames, CIDs and complete dependency graphs. Validate the selected variant before registration. Unselected DLC-dependent preparation inputs must not enter global startup VT loading. Build/stage/install tools must use the same root and back up/remove legacy exposed material directories before replacement. This boundary does not certify player cold-load acceptance or suppress native loading errors.

<a id="material-missing-optional-content-and-virtual-textures"></a>

## Missing optional content and virtual textures

Follow the [ordered required-source contract](../specs/cross-environment-spec.md#dlc-and-mod-compatibility) before accessing dependent materials. The material owner must establish availability of every entry in the selected style's declared source array through existing native state, not only its first/direct source. Normally functioning source mods supply complete, valid own resources; this trust does not waive explicitly registered additional requirements. Do not add UI/catalogue dependency-graph scans or internal-corruption defenses for functioning source mods.

Bridge Builder-owned shipped/prepared and persisted inputs retain their existing external-input checks and complete dependency handling at preparation, registration, persistence and self-check boundaries, including Surface, VT texture and MidMips/cache relationships. These obligations do not expand into an exhaustive source-mod integrity audit. Distinguish an absent external provider from its permitted on-demand database loading. Report actual file/native IO failures through the existing operation result contract; do not fabricate textures, silently substitute appearance, swallow errors or claim success. Unselected prepared resources remain unregistered under the discovery boundary above.

Resolve each registered source using its native identity and active state, including explicit DLC requirements on a mod-sourced style and the same array for prepared movable materials. Follow the actual native content requirement, including its DLC ownership check and conditional database requirement. A DLC whose native requirement does not require a separate database must not be rejected solely because no same-named database exists. Where the native requirement does require that database, its absence still makes the source unavailable; do not bypass native ownership or active-state checks. Do not infer availability from paths or cached geometry. Actual missing material resources remain operation failures at their existing IO boundaries, not a universal extra database gate for every DLC. At runtime, do not infer a recursive dependency graph or add gates from unrelated styles. The development owner must first complete the offline actual-reference/provider tracing required by the linked source contract, including material and native cache dependencies, and register its evidenced sources. Saved-bridge inspection and recovery follow the [self-check contract](self-check-contract.md#self-check-unavailable-optional-content).

<a id="repository-post-load-self-check-part-2"></a>

## Prepared variant dependency isolation

Related requirements: [Self Check Contract](self-check-contract.md#repository-post-load-self-check).

Prepared material variants may share unchanged texture payloads inside their own bridge style's
`Shared/` directory under the material generation contract. Keep every style's packaged dependency
graph complete and preflight it before native registration, including implicit Surface-to-MidMips
membership through the narrowly permitted shared read-only source cache. This does not permit
cross-style Texture storage or sharing generated-owner files.

<a id="contract-index-current-material-capability-boundary"></a>

## Current material capability boundary

Use [the capability boundary](../specs/material-spec.md#material-current-multilayer-compatibility-limit)
for diagnosed compatibility output, [program behavior](../specs/material-spec.md#material-dynamically-generated-and-independently-persisted-material-shaders-part-4)
for decal/layered semantics, and [registration ownership](#material-dynamically-generated-and-independently-persisted-material-shaders)
for the required mod lifecycle. These references retain their native-consumer and cold-load evidence limits.

<a id="contract-index-documentation-language-and-evidence-labels-part-4"></a>

## Mod program reference authority

Related requirements: [Constitution](../constitution.md#contract-index-documentation-language-and-evidence-labels), [Documentation Contract](documentation-contract.md#contract-index-documentation-language-and-evidence-labels-part-2), [Persistence Spec](../specs/persistence-spec.md#contract-index-documentation-language-and-evidence-labels-part-3).

Follow [mod-owned precompiled program requirements](material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders) for stable Surface references, mod package loading and queue-specific startup registration; geometry, texture and source ownership rules remain unchanged.


Compatibility scope, release provenance and current-format consumption follow the
[constitution](../constitution.md#backward-and-cross-environment-compatibility).

Legacy owner-persisted custom-program bridges are excluded from the compatibility scope above and follow [self-check-only retirement](self-check-contract.md#self-check-obsolete-custom-program-retirement).
