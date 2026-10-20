# Bridge asset persistence and reference policy

## Safe owned asset filename stems (2026-10-20)

After removing one owner token, replace each Windows-invalid filename character
(`<>:"/\|?*` and control characters U+0000 through U+001F), and every period in
the filename stem, with `_`. Preserve other characters and append the real native
extension separately. For example, `RB_Empty_Section_2.5` becomes
`RB_Empty_Section_2_5.Prefab`. Internal Prefab names and CIDs remain unchanged.
This supersedes earlier preservation of every non-owner character for disk stems.

Creation, generated-resource relocation and backed-up file-only migration share
`BridgeAssetInfo.FileStem`. Collision checks still apply: never overwrite a
different payload or CID. External dependency snapshots remain byte-identical with
their original paths. Native mid-mip tag filenames remain unchanged. Extensions
and paired `.cid` suffixes are not part of the stem replacement.

## Contents

- [1. Four distinct operations](#1-four-distinct-operations)
- [2. Component and resource decisions](#2-component-and-resource-decisions)
- [3. Evidence from the existing creation path](#3-evidence-from-the-existing-creation-path)
- [4. Application to movable-road conversion](#4-application-to-movable-road-conversion)
- [5. Review and acceptance](#5-review-and-acceptance)
- [6. Approved owner-directory layout](#6-approved-owner-directory-layout)
- [7. Legacy-to-owner-directory migration](#7-legacy-to-owner-directory-migration)
- [Movable bridge size acceptance](#movable-bridge-size-acceptance)
- [Organization revision record](#organization-revision-record)

This document is a normative supplement to bridge generation contract section 17. It defines persistence
scope for existing bridge creation and for future movable-bridge generation. It does not establish
that any unfinished movable generator has passed acceptance.

Reviewed on 2026-10-16 against the local working tree on branch `dev`, committed baseline
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`. Rollback reference:
`refs/rollback/asset-persistence-scope-20261016`. The reference records committed history, not the
pre-existing uncommitted implementation or documentation changes.

The owner-directory layout in sections 6–7 is implemented in creation and self-check migration
(2026-10-19). It supersedes sibling `_Dependencies` and per-component LOD directories.
Section 3 retains historical path evidence. VT and mid-mip caches are included in the owner
folder; native cold loading and packaging without Bridge Builder still require game acceptance.

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
| Generated main road and modified auxiliary deck | Native Prefabs in the owned save plan, with serialized network configuration and references | Do not overwrite the selected road or reuse another bridge's private Prefab |
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
and presentation preparation, saves the owned nodes, saves dependencies and validates the saved
representation before publication.

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

This existing behavior is the baseline: changing vertices does not by itself require a new material,
surface or texture. It does not prove that every network shader can render an animated object;
that separate compatibility question must be measured for movable-road conversion.

### External dependency snapshots

`src/BridgeBuilder/Runtime/BridgeDependencyPersistence.cs` resolves source assets and stops at
built-in provenance. It also excludes another bridge's private Prefab as a dependency source.

`src/BridgeBuilder/Runtime/BridgeDependencyCopies.cs` traverses CIDs, deduplicates within each owner,
preflights destination conflicts, and copies unchanged external bytes with original CID sidecars.
The current implementation uses the following legacy destination; section 6 moves it inside the UUID directory:

```text
ImportedData/<bridgeUUID>_Dependencies/<CID>.<original extension>
ImportedData/<bridgeUUID>_Dependencies/<CID>.<original extension>.cid
```

Prefab references are extracted from UTF-8 and UTF-16 serialized text views. Native `.Surface`
and `.Material` resources use `BridgeSurfaceReferences`, which reads binary texture references,
virtual-texture headers and the native Hash128 representation without loading or rewriting a
material. Saved-asset independence validation uses the same dependency results as copying.

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

### Native presentation and manager independence

`src/BridgeBuilder/Runtime/BridgeNativePresentation.cs` prepares native presentation, saves native
locale data and validates serialized references for manager-dependent types and UI paths.
`src/BridgeBuilder/Runtime/BridgeAssetCatalog.cs` invokes the native presentation save path when
writing catalog metadata. These responsibilities are separate from material export.

Manager independence means the saved bridge operates without Bridge Builder callbacks. Required
game/DLC content remains a prerequisite. Copying an external file also does not supply a missing
runtime class or an unsupported third-party behavior.

## 4. Application to movable-road conversion

The selected-road baking route remains mandatory. Persist the resulting new object geometry,
required skinning channels, LODs, bounds, native network configuration and attachment relationships.
Serialize native skeleton and animation configuration according to their actual containing assets.
There is no blanket requirement to create new material, texture, marking or animation resource files.

### Existing materials only (2026-10-17)

The user requires movable-bridge surfaces and markings to use existing materials and explicitly
accepts marking pattern-detail differences when using an existing game marking atlas. This
supersedes earlier requirements to retain original marking textures when no compatible existing
object material exists. Neither preview nor formal generation may bake appearance textures,
clone surfaces or rewrite material templates. Retain compatible source materials; otherwise
select an existing native object material and adapt generated geometry/UVs. Keep structural
materials unchanged. Never modify source materials. Persist generated geometry and attachments;
retain built-in references and snapshot unchanged external dependencies under existing rules.
This does not change traffic behavior, structural width rules or human visual acceptance.

Pre-edit baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

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
  in bridge generation contract sections 17 through 19. Never borrow another bridge's private Prefab.
- Use cold-start and save/load acceptance to establish native operation. A successful build, preview
  or file scan alone does not prove animation, road connectivity or shipping compatibility.

The original clarification was documentation-only. The 2026-10-16 binary surface dependency update
implements the traversal described above. Neither authorizes rewriting or removing healthy existing
bridges. The affected-style cleanup policy continues to apply when generation code actually changes.

## Movable bridge size acceptance

Bridge generation contract section 24 requires the complete movable bridge Prefab/dependency size to be
comparable to its native counterpart. Count private generated resources and external snapshots,
and include referenced built-in payloads in the logical comparison without copying them.
Report physical exported files separately. Sharing unchanged appearances across LODs and exact
constant textures within an owner is permitted; cross-owner snapshot deduplication remains
forbidden. See [the optimization record](Movable-Bridge-Performance.md) for implemented changes
and the distinction between measured subtotals and outstanding full-size acceptance.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.


## Approved native tiled road material descriptor (2026-10-17)

The user explicitly permits a new native object-material descriptor that references existing
tileable road textures to eliminate atlas-repeat geometry cuts. This is a scoped exception to
the existing-material-only rule: do not create or bake textures, introduce custom shaders, modify
source assets, or change structural and marking materials. Keep one descriptor per required
appearance within each bridge owner and share it across LODs. Preview descriptors belong only to
the preview's transient database. Persist formal descriptors with private identities and original
texture references; native cold loading must not depend on Bridge Builder. Unchanged built-in
textures remain references, and external dependencies retain the normal snapshot policy.

Use continuous object-local UVs and the native sampler for whole-texture repetition; do not apply
vertex-wise fractional UVs or cut triangles at repeat boundaries. This exception does not permit
repeating a mixed atlas as though it were a tileable road texture. Other geometry, marking,
sidewalk, LOD and acceptance requirements remain unchanged.

Baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`; rollback reference
`refs/rollback/native-tiled-road-material-20261017`. This permission does not establish visual or
cold-load acceptance.

## Approved GPU composition from existing textures (2026-10-17)

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.

### Grouped generated LOD directories (2026-10-18)

Historical implemented layout. Sections 6–7 supersede this directory design; migration to the
new target layout is implemented in the shared persistence and migration paths.

Generated Prefabs with the terminal naming suffix ` LOD<number>` share the main component's
ImportedData directory. For example, `Fixed Base 0 LOD1.Prefab` is stored alongside
`Fixed Base 0.Prefab` in the latter's directory. Each remains a separate native asset with its
own CID and unchanged LodProperties references. Geometry paths and material sharing are unchanged.
`BridgePrefabPersistence` is the common creation and migration path authority; the shared native
writer remains unchanged. Existing assets retain their identities; self-check layout migration
backs up and moves Prefabs plus CID sidecars without rewriting payloads and requests restart.
This is a storage-layout change, not geometry regeneration or a reason to remove healthy bridges.


## 6. Approved owner-directory layout

Decision date: 2026-10-18. Documentation baseline: branch `dev`, HEAD
`d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`, with existing working-tree changes preserved.
This specification applies to fixed and movable bridges. Do not infer implementation or cold-load
acceptance from its approval. Original documents are backed up separately from active assets.

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
  All characters outside this one match remain exactly unchanged, including spaces, hyphens,
  underscores, further occurrences and the extension. Do not trim or normalize the result.
  Names without a match remain unchanged. The explicitly named root Prefab exception below
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
- Dependency snapshots keep their CID-based filenames and original contents. Never apply owned-file
  renaming or type-category routing inside `Dependencies`. Built-in assets remain references.

Example (sidecars follow the same mapping):

```text
ImportedData/<UUID> Fixed Base 0 LOD1/<UUID> Fixed Base 0 LOD1.Prefab
  -> ImportedData/<UUID>/Prefabs/ Fixed Base 0 LOD1.Prefab
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
and packaging without Bridge Builder in game before claiming that acceptance.

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
| 9 | Owned `.MidMips`, including old `BridgeBuilder/<UUID> Road Surface/` caches | `<UUID>/Appearance/MidMips/<original filename>` |
| 10 | Unrecognized types | Retain in place for explicit review; never delete unknown payloads |

`<short name>` follows section 6's owner-UUID removal rule. Resolve ownership using existing approved
UUID/path rules plus the new UUID-directory context. Ambiguous or foreign ownership must not be
guessed. References to another bridge's private resources remain disallowed. Legacy internal-name
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
material/texture/locale writers, dependency writer and exclusions, source isolation, disk audit,
legacy-name handling, file-layout migration, pending-creation cleanup, UUID deletion/backup, and
pack/export discovery. Disk filename and internal Prefab name must be allowed to differ.

Verify migrations from both the old per-Prefab layout and the grouped-LOD layout, already-migrated
inputs, identical and conflicting destinations, missing sidecars, rollback after partial IO failure,
unknown types, dependency Prefabs with unrelated internal names, and preservation of other owners.
Then verify native cold loading and packaging without Bridge Builder. Keep documentation approval,
implementation, deployment and game acceptance as separate statuses.

### Implementation and verification record (2026-10-19)

- `BridgePrefabPersistence` and `BridgeAssetInfo` share category/short-name rules across Prefabs,
  geometry, paint resources, locales, movable material finalization and file-only migration.
- `BridgeAssetLayout` handles old per-component/grouped LOD folders, ordinary appearance files,
  VT, mid-mip caches and dependency directories. Complete payload/CID pairs are preflighted and
  backed up; identical pairs may be reused. Conflicts preserve healthy bridges. Partial moves
  roll back, and successful reruns do nothing. Internal names and native references are unchanged.
- Root lookup accepts `Prefabs/Bridge.Prefab` and the legacy UUID filename. Dependency provenance
  and foreign-source isolation accept both layouts. Legacy-name migration recognizes short-named
  owned geometry, avoiding unnecessary new identities after layout migration.
- Automated checks cover layout, resource relocation, self-check lifecycle, dependency copying,
  source isolation, legacy conversion and scoped cleanup. Native VT discovery/tag handling was
  inspected in the installed game code. This record does not assert in-game cold-load acceptance.

### Filename rule revision (2026-10-19)

The latest user instruction supersedes the earlier global-removal/trim rule. Remove only one
match of `-?b{uuid}[-_ ]?` and preserve every other character. For example, `Name-b{uuid}_LOD1`
becomes `NameLOD1`, while `b{uuid}-en-US.loc` becomes `en-US.loc`.
`b{uuid} Fixed Base 0 LOD1.Prefab` becomes `Fixed Base 0 LOD1.Prefab`.
Only one following separator is consumed; additional spaces and tabs remain unchanged.
Already-short names without an owner match are not trimmed retroactively.
The native locale extension is `.loc`; migration recognizes it as well as legacy `.Locale`.
Serialized names, resource contents and CID sidecars remain unchanged. Locale lookup accepts
legacy UUID-bearing names, previous short locale IDs and the exact new filename.
Baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a` plus preserved working changes.

## Bridge ZIP export (2026-10-19)

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

ZIP wrapper revision baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`,
preserving existing working changes. This supersedes the earlier wrapper-free ZIP rule.
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
