# Bridge asset persistence and reference policy

## Safe owned asset filename stems

After removing one owner token, replace each Windows-invalid filename character
(`<>:"/\|?*` and control characters U+0000 through U+001F), and every period in
the filename stem, with `_`. Preserve other characters and append the real native
extension separately. For example, `RB_Empty_Section_2.5` becomes
`RB_Empty_Section_2_5.Prefab`. Internal Prefab names and CIDs remain unchanged.

Creation, generated-resource relocation and backed-up file-only migration share
`BridgeAssetInfo.FileStem`. Collision checks still apply: never overwrite a
different payload or CID. External dependency snapshots remain byte-identical and use the dependency layout
in sections 3 and 7; stem sanitization does not rename their payloads. Native mid-mip tag filenames remain unchanged. Extensions
and paired `.cid` suffixes are not part of the stem replacement.

## Write completion before reads

Generated native resources must finish content initialization and synchronous native writing,
including closing the payload stream and writing the CID sidecar, before property reload,
relocation consumers or dependency traversal can read them. Asset registration alone does not
prove that a file exists. Verify the completed native path and type extension; never embed a
source URI or its extension into the generated filename stem. A failed write must stop that
owner transaction before readers/publication proceed. Do not hide ordering failures with sleeps
or retries. Tests must exercise actual native filesystem save, unload, relocation and reload,
not only an in-memory material binding.
## Contents

- [Safe owned asset filename stems](#safe-owned-asset-filename-stems)
- [Write completion before reads](#write-completion-before-reads)
- [1. Four distinct operations](#1-four-distinct-operations)
- [2. Component and resource decisions](#2-component-and-resource-decisions)
- [3. Evidence from the existing creation path](#3-evidence-from-the-existing-creation-path)
- [4. Application to movable-road conversion](#4-application-to-movable-road-conversion)
- [5. Review and acceptance](#5-review-and-acceptance)
- [Movable bridge size acceptance](#movable-bridge-size-acceptance)
- [6. Approved owner-directory layout](#6-approved-owner-directory-layout)
- [7. Legacy-to-owner-directory migration](#7-legacy-to-owner-directory-migration)
- [Bridge ZIP export](#bridge-zip-export)

This document is a normative supplement to bridge generation contract section 17. It defines persistence
scope for existing bridge creation and for future movable-bridge generation. It does not establish
that any unfinished movable generator has passed acceptance.

The owner-directory layout in sections 6–7 applies to creation and self-check migration.
Sibling `_Dependencies` and per-component LOD directories are legacy migration inputs.
Section 3 documents the creation and dependency paths. VT and mid-mip caches are included in the owner
folder; native cold loading and packaging with Bridge Builder enabled still require game acceptance.

## 1. Four distinct operations

A saved bridge must retain the data and references necessary for native operation. This does not
mean that every referenced resource becomes a newly generated asset.

1. **Serialize owned Prefabs.** Save generated or modified native Prefabs, their serialized component
   values and references. An embedded component is not automatically a separate asset file.
2. **Save generated resource data.** Save new or modified geometry and other genuinely changed
   resources with private identities, then reference them from owned Prefabs.
3. **Retain unchanged built-in references.** Keep original game/DLC resources as native references.
   Do not export duplicate material, texture, marking or other built-in resource files.
4. **Snapshot unchanged external dependencies.** Copy required non-built-in dependency bytes under
   the bridge owner, preserving their original CIDs and references, as required by section 17.
   These snapshots are not newly authored materials or converted resources.

In this document, "do not persist a resource" means do not create a separate private copy of its
unchanged built-in contents. Its reference and any required native configuration still belong in
the saved graph. Conversely, an unchanged external material can require a dependency snapshot
without requiring material conversion or a new material identity.

## 2. Component and resource decisions

| Part or data | Required saved representation | What must not be saved or duplicated |
| --- | --- | --- |
| Generated main road and modified auxiliary deck | Native Prefabs in the owned save plan, with serialized network configuration and references | Do not overwrite the selected road or depend on another owner's unsnapshotted files |
| Generated or modified sections, pieces, objects and render Prefabs | Save each owned node that actually needs saving; remap references to those nodes | Do not clone every source section, piece or lane merely because its type appears in the graph |
| Derived tower, cable, deck or other mesh geometry | Save generated `Geometry` data and reference it from the corresponding render Prefab | Do not treat a temporary Unity mesh as an exported geometry asset |
| LODs and bounds | Save required generated LOD geometry and native LOD references, counts and bounds with their owners | Do not assume a highest-detail mesh alone represents all LODs; unchanged referenced LOD resources follow provenance rules |
| Native network rules, transforms, sub-objects and lane configuration | Serialize required values and references inside their owning Prefabs | Do not invent one asset file per component, transform or scalar field |
| Unchanged original `surfaceAssets`, materials and textures | Keep built-in references; snapshot non-built-in dependencies when required | Do not create new surfaces or textures solely because their geometry has changed |
| Marking and decal source resources | Keep original references and required lane/decal configuration; apply external snapshot rules where applicable | Do not automatically export new marking textures or material libraries |
| New moving marking geometry or placement | Save genuinely generated geometry and native attachment/bone configuration, if that representation is needed | Do not confuse a moving representation with a requirement to duplicate its source material or texture |
| Native skeleton, skinning and animation configuration | For a movable generator, save newly generated geometry channels and required native configuration/references in their actual owning asset formats | Do not invent a standalone skeleton or animation file when the native representation is embedded; unchanged built-in animation resources remain references |
| Lights, barriers, props and attachments | Save changed placement/parenting in owned Prefabs; save a private asset only when the asset itself changes or must be materialized from runtime-only data | Do not duplicate all unchanged source props, effects or their textures |
| Native fees and unlocks | Preserve required native components and generated native nodes according to the existing economy contract | Do not add price-only copies, custom movement charges or manager callbacks |
| Display name and description | Save native locale data through the existing presentation path | Do not depend on a Bridge Builder localization callback after loading |
| Prototype thumbnail/icon | Preserve the usable native icon reference under existing presentation rules | Do not blindly export built-in icon textures or retain a manager-only UI URI |
| Unchanged external Prefab, material, geometry or texture dependency | Copy original bytes and original CID sidecar into the owner's dependency directory | Do not reserialize, rename internally, assign a new CID or rewrite references for an unchanged snapshot |
| Asset already saved under this bridge's ownership | Reuse it and traverse its supported serialized dependencies | Do not create a second redundant dependency snapshot of the owned asset |
| Preview objects, ECS staging entities, temporary meshes/materials, property blocks and texture leases | No permanent bridge asset; dispose through the existing temporary lifecycle | Do not serialize preview-only shader state or treat cached objects as cold-start dependencies |
| Logs, audit reports and manager settings | Separate operational/documentation storage when needed | Do not make them prerequisites for native bridge animation or network operation |

The decision is based on modification, provenance and native serialization, not the resource's name,
bridge style or nominal component type. A required external dependency must not be omitted merely
because the corresponding built-in resource category normally remains a reference.

## 3. Evidence from the existing creation path

The following paths are relative to the repository root.

### Owned Prefab graph

`vendor/CS2ModShared/src/Conversion/PrefabGraphCloner.cs`, `CloneRuntimePrefab`, explicitly clones
requested roots. Non-root built-in Prefabs and Prefabs that already have an asset are retained as
references. Runtime dependencies that require materialization are tracked in the clone plan.
Composition can add other private nodes when it modifies native structures. This is selective graph
construction, not an export of every asset reachable from the selected road.

`src/BridgeBuilder/Systems/BridgeGenerationSystem.Composition.cs`, `TryBuildBridge`, combines the
cloner's nodes, additional native assets and created structure Prefabs into the save plan. Its
preview branch returns before permanent persistence. The permanent path applies native metadata
and presentation preparation, saves the owned nodes and completes dependency persistence before
publication. Successful persistence of this newly generated graph is the dependency guarantee;
do not repeat the same graph resolution, global cache indexing or independence traversal before
publishing it. Preserve save failures, write completion and required native initialization checks.
This trust is local to the successful creation call, not a cached exemption for old or other-owner assets.

`vendor/CS2ModShared/src/Export/PrefabAssetWriter.cs`, `Save`, filters by `NeedsSave`, registers the
planned nodes first and then saves each Prefab. Its save call deliberately avoids recursive native
saving of shared donor Prefabs. Registration makes target asset identities available when references
are serialized. A reference in a `.Prefab` is not the embedded contents of its target material or
texture.

### Generated geometry with original surfaces

`src/BridgeBuilder/Bridges/Common/Geometry/TowerFactory.Mesh.cs` takes the original
`surfaceAssets` array, builds derived models and, for permanent generation, saves a new `Geometry`
asset. The derived render Prefab points to that geometry and retains the original surface references
through `widened.surfaceAssets = surfaces`. Preview models take a temporary path.

### External dependency snapshots

#### Forward dependency handoff for new bridges

During permanent creation the generator supplies the saver with the bridge roots and the actual sources and implicit dependency edges established while building that bridge. Traverse serialized forward CIDs recursively. Follow each prepared Surface's declared MidMips CIDs and each generated Surface's explicitly created owner cache. An explicitly empty cache set represents a Surface with no cache requirement. For an external material, follow its serialized texture and VT references and resolve required native cache membership using the selected material's exact provenance. Missing required sources, positive-level cache memberships or conflicting bytes are persistence errors.

Resolve a requested CID through the recorded asset or an exact database lookup. Valid other-owner snapshots are permitted source streams; copy required bytes into the target owner. Normal creation, including lane conversion, must not enumerate all registered assets or MidMips caches, probe unrelated cache payloads, or scan owner directories to discover dependency edges. Keep the handoff scoped to the current generation. Startup self-check and historical migration follow each bridge's serialized dependency chain and the native cache provenance described below. Use current-owner direct paths, exact CID lookups and the selected native material's indexed cache provenance. A once-per-inspection discovery of registered bridge prefab identities and historical owner-file locations may establish roots and migration candidates. Reuse those candidates throughout the pass. Do not build a global all-asset/CID index, reread every Prefab per owner, or repeatedly scan all owner directories. The bounded inspection-only MidMips fallback below is the sole cache-index exception. Retain unowned shared legacy sources when their other consumers cannot be proven without a global reverse scan; migrate only the selected bridge closure. Preserve the separate discovery and handling of orphaned legacy components and the existing migration-first recovery and retirement semantics. Do not persist cache relations or an inspection index. Include dependency, validation, portable migration, name/layout migration and retirement costs in the self-check timings.

`src/BridgeBuilder/Runtime/BridgeDependencyPersistence.cs` resolves source assets and stops at
built-in provenance. Other-owner sources remain readable external dependencies and are
snapshotted independently, including materials and textures with identical CIDs.

`src/BridgeBuilder/Runtime/BridgeDependencyCopies.cs` traverses CIDs, deduplicates within each owner,
preflights destination conflicts, and copies unchanged external bytes with original CID sidecars.
Use the owner-local dependency destination shared by creation and migration:

```text
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>.cid
```

For native `.MidMips` only, retain the tag-bearing original `TS*_MB*.MidMips` basename
under `Dependencies/<CID>/`, with the unchanged payload and original CID sidecar. The native
cache records implicit Surface dependencies through primary and duplicate Surface CID memberships;
traverse these alongside serialized references. Current-owner caches remain discoverable without
the installed prepared-material map; other owners' files may supply independent dependency copies.

Prefab references are extracted from UTF-8 and UTF-16 serialized text views. Native `.Surface`
and `.Material` resources use `BridgeSurfaceReferences`, which reads binary texture references,
virtual-texture headers and the native Hash128 representation without loading or rewriting a
material. Normal creation consumes the successful dependency-save result without a second traversal.
Existing-asset migration and post-load self-check retain their separate validation responsibilities.

In VT mode, traverse the VTSurface and preprocessed VTTexture payloads and ordinary texture bindings
outside the material template's VT stacks. Original texture IDs inside those stacks are provenance
identifiers, not requests to load separate original payloads: native `IsHandledByVirtualTexturing`
uses the VT representation. The installed 209-surface audit contains 95 VT surfaces and 248 such
original IDs without standalone payloads; all 571 required reference edges resolve in game packages.
Do not treat these source IDs as missing files or recreate their textures. Non-VT texture bindings
remain ordinary required dependencies. The two VT payload formats contain their pixel data; their
texture identity fields are streaming keys, not further source-file dependencies.

The parser follows version 1 dictionaries and the native legacy property-record reader through
version 11; actual resource-fixture verification currently covers version 1. Unsupported versions,
truncated data and invalid records fail persistence explicitly. Other extensions remain terminal
in this implementation. Built-in `UnityGUID` references are terminal too. This does not prove that
arbitrary binary formats lack dependencies; inspect and implement each newly required format.

An already-owned asset is not copied again, but its supported serialized references are still
traversed. Separate bridges retain separate physical snapshots of a shared external CID. Original
source assets remain unchanged. Missing sources and byte conflicts are failures, not permission to
substitute an unrelated asset.

Other mods' materials and Bridge Builder's packaged materials both require these owner-local
snapshots, together with their non-built-in dependencies. Startup/manual self-check runs the same
traversal for every inspected bridge before validating live references, including healthy bridges
whose installed source mod currently hides a missing local copy. Supplement missing payloads and
CID sidecars without changing geometry, identities or live objects. Complete snapshots remain
unchanged on repeated checks; new copies are reported as migration and require restart. The existing
migration failure policy applies when permitted sources are unavailable or destination bytes conflict.
Verify that copies remain readable without source-mod registrations; native cold-load acceptance
is separate and cannot be inferred from file equality alone.

The source-linked `tools/DependencySourceCheck` exercises the production resolver, surface parser
and copy traversal with an owned root referencing an external material and texture. It verifies
byte/CID preservation, missing-sidecar supplementation, no writes on repetition, resolution from
current-owner snapshots with the source-mod registrations absent, and independent copies from other-owner sources, with missing data and destination conflicts
remaining failures. Ownership classification does not reject source streams.
`tools/SelfCheckLifecycleCheck` exercises the actual startup system with dependency-service doubles:
healthy references still trigger supplementation, and the migration/restart notice survives a
subsequent unchanged check. This lifecycle check does not itself verify native texture loading.

### Self-check without a persisted cache ledger

`BridgeCacheEdges.txt` must not be read or written by current dependency resolution.
Remove an existing obsolete file only after that owner's complete inspection and migration
succeed; preserve its original bytes in the normal backup area first. Failed or unfinished
inspection retains the file without relying on its contents. No other relationship file,
extra native payload copy, path alias, cache layout change or persisted index replaces it.

Follow root Prefab CIDs and exact required Surface/VT dependencies. Prefer the game's
already-built Surface-CID atlas index and exact native/prepared cache provenance. Reusing
that native dictionary is not permission to enumerate all assets or rebuild it.
When necessary, one inspection-scoped fallback may traverse MidMips caches **at most once
for the entire self-check across all owners**. Parse native primary/duplicate membership
headers into a shared ephemeral map; include relevant owner cache paths already discovered
for inspection, without another directory scan per owner. Read no unrelated asset types
for this fallback. Complete this shared cache-relation preparation at the inspection
entry, after native asset-batch loading and before owner dependency migration or structural
checks. Reuse the map for every owner and discard it after inspection. A failed preparation
aborts the whole inspection with an ERROR before owner mutations; do not create a per-owner
cache-not-ready/unfinished branch or infer damage from initialization failure.

Original cache payloads and CIDs remain in their existing native paths, one dependency
copy per owner. A snapshot Surface needs no live native handle to resolve its cache.
Use native VT headers to distinguish a genuine zero-mid-mip material from unavailable
cache provenance. After successful shared preparation, a required absent cache is an explicit
missing-dependency diagnostic, not a per-owner readiness deferral. Missing
required resources and corrupt payloads retain contextual failures. Creation continues
using its explicit source handoff and must not invoke the inspection-wide fallback.

<a id="native-presentation-and-manager-independence"></a>

### Native presentation and startup registration

`src/BridgeBuilder/Runtime/BridgeNativePresentation.cs` prepares native presentation, saves native
locale data and validates existing/migrated serialized references for manager-dependent types and UI paths.
Normal creation prepares native presentation before saving and does not call that additional
existing-asset validator after successful dependency persistence.
`src/BridgeBuilder/Runtime/BridgeAssetCatalog.cs` invokes the native presentation save path when
writing catalog metadata. These responsibilities are separate from material export.

Native persisted presentation must survive mod-enabled cold loading; generated programs use Bridge Builder startup registration. Required
game/DLC content remains a prerequisite. Copying an external file also does not supply a missing
runtime class or an unsupported third-party behavior.

## 4. Application to movable-road conversion

The selected-road geometry conversion route is mandatory. Persist the resulting new object geometry,
required skinning channels, LODs, bounds, native network configuration and attachment relationships.
Serialize native skeleton and animation configuration according to their actual containing assets.
There is no blanket requirement to create new material, texture, marking or animation resource files.

### Native material resources

Follow [Scope and authority](MATERIAL_GENERATION_CONTRACT.md#scope-and-authority) for this requirement.

Follow the [material generation contract](MATERIAL_GENERATION_CONTRACT.md).
Movable non-road materials retain original identities and dependencies. Only prototype-supported
road appearance types enter composition/precomposition outside the game, with source-density
tiling and all-LOD embedded-marking removal. Other road types use compatible originals or the
corresponding native object road replacement under material section 4. Asphalt, sidewalks and
wheel-track/rut effects are independent support decisions. Persist eligible authored road repeats
with new identities and complete native periodic mip dependencies, without modifying shared originals.
The [general source-mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode)
applies to fixed and movable bridges alike. Persist the required mapping and dependencies;
ownership copies do not justify replacement materials or texture baking.

Known eligible road compositions are prepared offline. Bounded runtime exceptions do not widen
the supported-type gate or authorize full-road rasterization. The material contract authorizes two generated shader programs with owner-local dependencies and mod-enabled startup registration; per-frame manager-driven rebinding remains prohibited. Preserve complete texture/VT/mip dependencies and unchanged source bytes.
Only native MidMips sources may share `Materials/Shared/MidMips/<cacheCID>/` across styles.
Each owner independently snapshots its required cache dependencies with original CIDs and native
filenames. Owner material bytes are informational, with no hard ceiling; the complete SDK ZIP limit is 55,000,000 bytes.

### Resource decision procedure

For each visual resource, apply this sequence:

1. Determine whether the existing resource and its native inputs can represent the generated object.
2. If unchanged use is valid, retain its reference. Apply provenance-based external snapshots.
3. If only geometry, UVs or native placement must change, save those changes and retain compatible
   source resources.
4. If an actual surface/material change is necessary, record the incompatible source inputs, exact
   changed fields and evidence that the native object path requires the change. Save only the
   genuinely modified resource with a private identity. Retain compatible texture/resource
   references and apply external snapshots to unchanged external dependencies.
5. If the native persisted representation is not established, report that combination unsupported.
   Temporary preview materials are not a substitute for a verified saved representation.

Markings must follow the moving span where required. Their visual geometry or attachment can need
conversion while their original textures and material resources remain references. Preserve traffic
lanes independently and prevent duplicate stationary visuals in the hidden moving span.

These are admission rules for future complete generators, not claims that the current partial
conversion helpers already implement material conversion, marking export or native animation reload.

## 5. Review and acceptance

- Inventory newly generated assets separately from unchanged references and external snapshots.
- For each new resource, identify the changed data or runtime-only data that requires materialization.
  "The bridge moves" is insufficient justification for duplicating an unchanged source resource.
- Verify that all required owned nodes are in the save plan and their serialized references resolve.
- Verify derived geometry, LODs and configuration survive reload; unchanged surface references retain
  their intended identities.
- Verify built-in resources are not copied and external snapshots retain exact bytes and CIDs.
- Preserve existing ownership, missing-dependency failure, migration and manager-independence rules
  in bridge generation contract sections 17 through 19. Other-owner input never substitutes for the target's required independent snapshot.
- Use cold-start and save/load acceptance to establish native operation. A successful build, preview
  or file scan alone does not prove animation, road connectivity or shipping compatibility.

Dependency traversal must not rewrite or remove healthy bridges. Deployment recovery requires
a concrete incompatibility and remains owner-scoped under bridge generation contract section 13.

## Movable bridge size acceptance

Bridge generation contract section 24 requires the complete movable bridge Prefab/dependency size to be
comparable to its native counterpart. Count private generated resources and external snapshots,
and include referenced built-in payloads in the logical comparison without copying them.
Report physical exported files separately. Sharing unchanged appearances across LODs and exact
constant textures within an owner is permitted; cross-owner snapshot deduplication remains
forbidden.

## 6. Approved owner-directory layout

This specification applies to fixed and movable bridges. Native cold-load acceptance
requires in-game evidence.

```text
ImportedData/<UUID>/
  Prefabs/         All owned Prefabs, including every LOD
  Geometry/        Owned generated geometry
  Appearance/      Owned Surface, Material and ordinary Texture files
    VT/            Owned VTSurface and VTTexture payloads
    MidMips/       Owned native mid-mip caches (original tag-bearing filenames)
  Locales/         Owned native locale assets
  Dependencies/    Byte-identical external non-built-in dependency snapshots
```

These are optional categories, not a mandatory pre-created tree. Do not introduce extra folders
for mechanism, supports, attachments, material types or individual LODs. Names distinguish those
assets. Keep each Prefab/geometry/texture independently identified by its CID. Do not merge payloads.

### Filename and identity rules

- For owned filenames, remove exactly one (the first) match of `-?b{uuid}[-_ ]?`, where
  `b{uuid}` is the escaped owning identity and comparison is case-insensitive. Both optional
  characters are greedy: consume a preceding hyphen and one following hyphen, underscore or ASCII space when present.
  Then replace invalid filename-stem characters and periods with `_` under the safe-stem rule.
  Preserve all other characters, including spaces, hyphens, underscores and further owner tokens.
  Do not trim the result. Append the native extension separately. Names without an owner match
  still undergo stem sanitization. The explicitly named root Prefab exception below
  remains `Bridge.Prefab`; an empty name for another asset is an error, not an invented name.
  New creation and migration share this rule. Migration transforms the existing disk basename,
  never substitutes the serialized Prefab name for it.
- The main Prefab whose stem equals the owner UUID becomes `Prefabs/Bridge.Prefab`.
- A `.cid` sidecar follows its payload's new path/name. Its bytes and the payload's CID do not change.
- Internal Prefab names, serialized references, versions, geometry, materials and animation remain
  unchanged during layout migration. The on-disk basename may therefore differ from the internal name;
  validators must not require equality. Retain UUID-bearing internal names for native identity.
- Flattening or UUID removal can produce a filename collision. Reuse only byte-identical
  payload/CID pairs; otherwise report the conflict without overwriting or inventing a suffix.
  Unknown types are handled under section 7.
- Dependency snapshots keep their CID-based filenames and original contents; native `.MidMips`
  keeps its original tag-bearing basename inside a CID subdirectory as specified above. Never apply owned-file
  renaming or type-category routing inside `Dependencies`. Built-in assets remain references.

Example (sidecars follow the same mapping):

```text
ImportedData/<UUID> Fixed Base 0 LOD1/<UUID> Fixed Base 0 LOD1.Prefab
  -> ImportedData/<UUID>/Prefabs/Fixed Base 0 LOD1.Prefab
ImportedData/<UUID>/<UUID>.Prefab
  -> ImportedData/<UUID>/Prefabs/Bridge.Prefab
ImportedData/<old component directory>/GoldenGateDouble-38_5-<UUID>.Prefab
  -> ImportedData/<UUID>/Prefabs/GoldenGateDouble-38_5.Prefab
ImportedData/<UUID>/Locales/<UUID>-en-US.loc
  -> ImportedData/<UUID>/Locales/en-US.loc
```

### Empty directories and ownership

Create a category directory only when its first actual payload is written. No placeholder files
or empty directory skeletons. Missing optional directories are not damage; validate required
references and payloads instead. Failed generation removes only its own created files and then
its newly empty directories. Successful migration removes emptied source directories only after
verification, within verified owner-scoped boundaries. Never remove unrelated or nonempty folders.

An asset is owned by the UUID path context even when its basename no longer contains a UUID.
Update ownership, cleanup, export, audit and source-isolation code accordingly before deployment.
Keep logs, manager state, reports, backups and preview caches outside the active bridge directory.

### VT and packaging boundary

Generated `.VTSurface` and `.VTTexture` files use `Appearance/VT/`; native `.MidMips`
files use `Appearance/MidMips/`. These optional directories are created only when needed.
The native file data source enumerates supported extensions recursively. Surface headers resolve
VT by CID; `TextureStreamingSystem` discovers mid-mip caches by the tag assigned from the cache
filename by `MidMipCacheAsset.PostCreate`. Preserve that filename, tag, payload and CID.

The native VT writer derives temporary streaming filenames from the full Surface/Texture name.
Keep owner-qualified names during that write, then relocate the generated resources before native
streaming captures file descriptors. New mid-mip caches are written directly to their final path.
Do not use native `MoveTo` within one database: it is a cross-database transfer that deletes the
source registration. The generation helper copies payload/sidecar, switches the native data-source
entry with the original CID, and restores the original on a registration failure. This is a creation
operation only; saved bridges require no manager updates or patches to resolve their resources.

The intended packaging boundary is the complete UUID directory, with required game/DLC resources
present. External non-built-in dependencies retain their byte-identical CID snapshots under
`Dependencies`. File-layout and native API inspection are not cold-load acceptance: verify loading
and packaging with Bridge Builder enabled in game before claiming that acceptance.

## 7. Legacy-to-owner-directory migration

Use the owner UUID, dependency provenance and file extension. No bridge-style, bone, LOD or
component-name classification is necessary. New creation and migration must share the same path
mapping. The following table describes the implemented target layout.

Apply rules in this order:

| Priority | Source | Destination |
| --- | --- | --- |
| 1 | Files under `ImportedData/<UUID>_Dependencies/` | `ImportedData/<UUID>/Dependencies/`, preserving every relative path and filename |
| 2 | Files already under `<UUID>/Dependencies/` | Leave in place; never parse or rename an external Prefab as an owned component |
| 3 | Owned `.Prefab`, from old per-Prefab, grouped-LOD or nested directories | `<UUID>/Prefabs/<short name>.Prefab`; exact UUID root becomes `Bridge.Prefab` |
| 4 | Owned `.Geometry`, including files in the old `BridgeBuilder/` tree | `<UUID>/Geometry/<short name>.Geometry` |
| 5 | Owned `.Surface`, `.Material`, `.Texture` | `<UUID>/Appearance/<short name>.<extension>` |
| 6 | Owned `.loc` (native) or legacy `.Locale` | `<UUID>/Locales/<short name>.<original extension>` |
| 7 | `.cid` sidecar | Follow its corresponding payload; never map it independently |
| 8 | Owned `.VTSurface`, `.VTTexture`, including old `StreamingData~/VT/` files | `<UUID>/Appearance/VT/<short name>.<extension>` |
| 9 | Owned `.MidMips`, including old flat or `BridgeBuilder/<UUID> Road Surface/` caches | `<UUID>/Appearance/MidMips/<unchanged cache CID>/<original filename>`; preserve the native tag basename and payload/CID pair, with backed-up migration and conflict protection |
| 10 | Unrecognized types | Retain in place for explicit review; never delete unknown payloads |

`<short name>` follows section 6's owner-UUID removal rule. Resolve ownership using existing approved
UUID/path rules plus the new UUID-directory context. Ambiguous or foreign ownership must not be
guessed for mutation. Reading another bridge's input is permitted; persist its required
independent snapshot without modifying that source. Legacy internal-name
conversion is a separate existing operation; this layout migration does not rename serialized objects.

### Transaction and collision rules

1. Inventory the selected owner's files and required references; construct the full mapping before
   writing. Keep originals and CID sidecars paired. Missing required payloads/sidecars are errors.
2. Check that all resolved absolute paths remain in the intended source/target scope. Preflight
   flattened filename collisions, including two old folders mapping to the same short basename.
3. If already at the target path, do nothing. An existing destination may only be reused after
   proving identical payload bytes and identical CID sidecar bytes. Different bytes or identities
   stop this owner's layout migration and report the conflict; do not overwrite or invent suffixes.
4. Back up every affected original file and sidecar before moving anything. Backups contain originals
   only, outside the active asset tree. Retain source bytes for rollback until the transaction succeeds.
5. Move without reserializing payloads, reallocating CIDs or rewriting references. Verify target
   bytes/identity and all mapped reference resolutions. Remove old duplicates only after verified
   identity and backup. On failure restore affected paths; report any incomplete rollback explicitly.
6. Remove verified empty old owner directories. Re-running migration must be a no-op. Unknown
   files stay in place for review; their presence must not trigger blanket deletion.
7. Request restart after a file-only migration that affects cached native assets. Do not continue
   reading stale database streams, mutate live Prefabs, or represent a directory move as rebuilding
   a bridge. A layout conflict alone does not authorize deletion of a healthy bridge; distinguish
   it from the existing damaged-asset repair/removal policy.

### Required implementation review and acceptance

Before implementing this layout, update all producers and consumers together: Prefab/geometry/
material/texture/locale writers, dependency writer and exclusions, output ownership isolation, disk audit,
legacy-name handling, file-layout migration, pending-creation cleanup, UUID deletion/backup, and
pack/export discovery. Disk filename and internal Prefab name must be allowed to differ.

Verify migrations from both the old per-Prefab layout and the grouped-LOD layout, already-migrated
inputs, identical and conflicting destinations, missing sidecars, rollback after partial IO failure,
unknown types, dependency Prefabs with unrelated internal names, and preservation of other owners.
Then verify native cold loading and packaging with Bridge Builder enabled. Keep documentation approval,
implementation, deployment and game acceptance as separate statuses.

### Shared implementation responsibilities

- `BridgePrefabPersistence` and `BridgeAssetInfo` share category/short-name rules across Prefabs,
  geometry, paint resources, locales, movable material finalization and file-only migration.
- `BridgeAssetLayout` handles old per-component/grouped LOD folders, ordinary appearance files,
  VT, mid-mip caches and dependency directories. Complete payload/CID pairs are preflighted and
  backed up; identical pairs may be reused. Conflicts preserve healthy bridges. Partial moves
  roll back, and successful reruns do nothing. Internal names and native references are unchanged.
- Root lookup accepts `Prefabs/Bridge.Prefab` and the legacy UUID filename. Dependency provenance
  and source reading and target ownership accept both layouts. Legacy-name migration recognizes short-named
  owned geometry, avoiding unnecessary new identities after layout migration.
- Automated checks cover layout, resource relocation, self-check lifecycle, dependency copying,
  output ownership isolation, legacy conversion and scoped cleanup. Native VT discovery/tag handling was
  inspected in the installed game code. This record does not assert in-game cold-load acceptance.

### Filename mapping examples

Remove only one match of `-?b{uuid}[-_ ]?`, sanitize the stem as above, and preserve
other characters. For example, `Name-b{uuid}_LOD1`
becomes `NameLOD1`, while `b{uuid}-en-US.loc` becomes `en-US.loc`.
`b{uuid} Fixed Base 0 LOD1.Prefab` becomes `Fixed Base 0 LOD1.Prefab`.
Only one following separator is consumed. Additional spaces remain unchanged; tabs are
control characters and become underscores under the safe-stem rule.
Already-short names without an owner match are not trimmed retroactively.
The native locale extension is `.loc`; migration recognizes it as well as legacy `.Locale`.
Serialized names, resource contents and CID sidecars remain unchanged. Locale lookup accepts
legacy UUID-bearing names, previous short locale IDs and the exact new filename.

## Bridge ZIP export

Export is an action button named **Export** inside **Manage bridges**, alongside Build
and Delete. There is no separate export tab or page. Select a bridge and click Export,
then confirm its editable absolute ZIP path and top-level archive folder before any output
is created. The default
path is `<configured export directory>/<bridge display name>.zip`; characters invalid
in a filename are replaced with underscores. The mod settings directory picker defaults
to `Documents/BridgeBuilder/Exports`.

Export copies all files recursively from `ImportedData/<bridge UUID>/`, including CID
sidecars and owner-local dependency, VT and mid-mip files. Archive entry names are
prefixed by exactly one player-selected top-level folder, defaulting to the selected
bridge identity `b{uuid}`: `b{uuid}/Prefabs/Bridge.Prefab`, for example. The folder name
is independent of the ZIP filename and is editable for each export. It must be one
nonempty valid Windows directory name, not a path: reject separators, control characters,
reserved device names, `.`/`..`, and trailing spaces or periods. Invalid names produce the
normal export-failure popup without creating an archive. Do not silently rewrite the name.
Empty directories are omitted. Export does not alter
source files, asset identities, references, geometry or migration rules.

Compression runs on a file-only background worker. A temporary archive is completed
before it is moved to the chosen destination. Existing destination files are not
overwritten. Failures remove the temporary archive when accessible, report a warning
and show a failure popup; they do not use the ERROR channel. Successful export shows
the destination in a popup. File checks cover the archive root, unchanged file bytes,
CID sidecars, nested dependencies, collision protection and failed-export cleanup.
In-game interaction remains subject to player acceptance.

The wrapper name does not change internal bridge ownership or asset identities.

Export interaction uses the native `cs2/ui.ConfirmationDialog`, native TextInput and
Button controls, and native `DirectoryBrowserPanel` with mod-owned `WidgetBindings`, rendered by
`EditorWidgetListRenderer` inside the native directory-picker panel/backdrop theme,
with native `FocusRoot`, `AutoNavigationScope` and `BackConsumer`. Do not put the directory
page view in a message dialog text slot; it needs the native picker layout constraints. Do not use the settings-only
`OptionsUISystem.OpenDirectoryBrowser`: it does not update outside the options screen.
Both export text inputs are single-line. The dialog mounts separately from the non-interactive bridge
panel. Choose folder preserves the editable ZIP filename and custom archive-root name;
canceling the browser leaves those fields intact. The confirmation action is **Export**.

The export-success notification uses a native confirmation dialog with **Open folder**
and **Close** actions. Render the formatted success text as a plain React text child,
not the native rich-text message property: Windows backslashes must remain literal.
Open folder uses the parent directory of the completed export path held by the backend,
not a path supplied by the click event. Failure to open that directory is reported as
an open-folder failure without changing the successful export status.

Follow [generated material program requirements](MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders) for program persistence, Player loading and mod-enabled registration; geometry, texture and source ownership rules remain unchanged.