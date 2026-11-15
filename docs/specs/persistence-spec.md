# Persistence Spec

Required companion: [Persistence Contract](../contracts/persistence-contract.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](../contracts/reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

## Operational configuration

Configuration uses `.json`; export settings are stored as `settings.json` under the game-derived
mod data directory. Normal settings readers accept only JSON and use in-memory defaults when the
file is absent. Startup self-check migrates published `settings.ini` values before subsequent world
loading reads settings. It preserves the original INI and does not overwrite an existing JSON file.
Migration preserves the existing parsing, defaults and range rules; atomic replacement preserves
the previous JSON if a write fails. This configuration-only change does not alter bridge assets or
trigger the bridge-repair restart notice. Export-state records remain current-schema TSV data.

<a id="persistence-safe-owned-asset-filename-stems"></a>

## Safe owned asset filename stems

After removing one owner token, replace each Windows-invalid filename character
(`<>:"/\|?*` and control characters U+0000 through U+001F), and every period in
the filename stem, with `_`. Preserve other characters and append the real native
extension separately. For example, `RB_Empty_Section_2.5` becomes
`RB_Empty_Section_2_5.Prefab`. Internal Prefab names and CIDs remain unchanged.

Creation, generated-resource relocation and backed-up file-only migration share
`BridgeAssetInfo.FileStem`. Collision checks still apply: never overwrite a
different payload or CID. External dependency snapshots remain byte-identical and use the dependency layout
in [sections 3](persistence-spec.md#persistence-3-evidence-from-the-existing-creation-path) and 7; stem sanitization does not rename their payloads. Native mid-mip tag filenames remain unchanged. Extensions
and paired `.cid` suffixes are not part of the stem replacement.

<a id="persistence-2-component-and-resource-decisions"></a>

## Component and resource decisions

| Part or data | Required saved representation | What must not be saved or duplicated |
| --- | --- | --- |
| Generated main road and modified auxiliary deck | Native Prefabs in the owned save plan, with serialized network configuration and references | Do not overwrite the selected road or depend on another owner's unsnapshotted files |
| Generated or modified sections, pieces, objects and render Prefabs | Save each owned node that actually needs saving; remap references to those nodes | Do not clone every source section, piece or lane merely because its type appears in the graph |
| Derived tower, cable, deck or other mesh geometry | Save generated `Geometry` data and reference it from the corresponding render Prefab | Do not treat a temporary Unity mesh as an exported geometry asset |
| LODs and bounds | Save required generated LOD geometry and native LOD references, counts and bounds with their owners | Do not assume a highest-detail mesh alone represents all LODs; unchanged referenced LOD resources follow provenance rules |
| Native network rules, transforms, sub-objects and lane configuration | Serialize required values and references inside their owning Prefabs | Do not invent one asset file per component, transform or scalar field |
| Unchanged original `surfaceAssets`, materials and textures | Keep built-in references; snapshot non-built-in dependencies when required | Do not create new surfaces or textures solely because their geometry has changed |
| Mod-owned precompiled shader programs | Persist stable program-family/queue references in Surfaces; the installed mod supplies programs | No owner program bundle, FileAsset, CID, registration recipe or export entry; no runtime compiler |
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

<a id="persistence-3-evidence-from-the-existing-creation-path"></a>

## Creation-path persistence requirements

The following paths are relative to the repository root; concrete inspections are linked from each topic.

<a id="persistence-owned-prefab-graph"></a>

## Owned Prefab graph

The [recorded creation-path inspection](../archive/audits/persistence-implementation-records.md#owned-prefab-creation-path) documents selective graph construction. Preview must return before permanent persistence; permanent creation saves required owned nodes and completes presentation/dependencies before publication. Trust successful persistence within this creation call without a second graph traversal; preserve real save failures and write completion. This trust never exempts old or other-owner assets.

<a id="persistence-generated-geometry-with-original-surfaces"></a>

## Generated geometry with original surfaces

See the [geometry source-reference inspection](../archive/audits/persistence-implementation-records.md#derived-geometry-source-surfaces). Geometry changes do not independently authorize replacing original surfaces; preview resources remain temporary.

<a id="persistence-external-dependency-snapshots"></a>

## External dependency snapshots

Creation and post-load self-check pass an explicit `BridgeCreationDependencies` context to the saver. Material producers record generated cache handles and resolve selected native cache entries through exact metadata or package URI.

#### Forward dependency handoff for new bridges

During permanent creation the generator supplies the saver with the bridge roots and the actual sources and implicit dependency edges established while building that bridge. Traverse serialized forward CIDs recursively. Follow each prepared Surface's declared MidMips CIDs and each generated Surface's explicitly created owner cache. An explicitly empty cache set represents a Surface with no cache requirement. For an external material, follow its serialized texture and VT references and resolve required native cache membership using the selected material's exact provenance. Missing required sources, positive-level cache memberships or conflicting bytes are persistence errors.

Resolve a requested CID through the recorded asset or an exact database lookup. Valid other-owner snapshots are permitted source streams; copy required bytes into the target owner. Normal creation, including lane conversion, must not enumerate all registered assets or MidMips caches, probe unrelated cache payloads, or scan owner directories to discover dependency edges. Keep the handoff scoped to the current generation. Self-check discovery and historical migration follow the [self-check contract](../contracts/reading-map.md#bridge-self-check-contract).

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
uses the VT representation. The [installed-resource audit](../archive/audits/persistence-implementation-records.md#installed-surface-reference-audit) records concrete counts.

Do not treat these source IDs as missing files or recreate their textures. Non-VT texture bindings
remain ordinary required dependencies. The two VT payload formats contain their pixel data; their
texture identity fields are streaming keys, not further source-file dependencies.

Validate external resource formats at their IO boundary; unsupported, truncated or invalid required resources remain explicit persistence failures. Native/third-party source formats are distinct from historical Bridge Builder compatibility. [Parser inspection scope](../archive/audits/persistence-implementation-records.md#surface-parser-inspection-scope) records actual coverage, not universal format support.

An already-owned asset is not copied again, but its supported serialized references are still
traversed. Separate bridges retain separate physical snapshots of a shared external CID. Original
source assets remain unchanged. Missing sources and byte conflicts are failures, not permission to
substitute an unrelated asset.

Other mods' materials and ordinary Bridge Builder packaged materials require these owner-local
snapshots, together with their non-built-in dependencies. Self-check supplementation follows the [self-check contract](../contracts/reading-map.md#bridge-self-check-contract).
Verify that copies remain readable without source-mod registrations; native cold-load acceptance
is separate and cannot be inferred from file equality alone.

Recorded fixture coverage and limitations are retained in the [persistence audit](../archive/audits/persistence-implementation-records.md#dependency-and-lifecycle-fixture-evidence).

<a id="persistence-4-application-to-movable-road-conversion"></a>

## Application to movable-road conversion

The selected-road geometry conversion route is mandatory. Persist the resulting new object geometry,
required skinning channels, LODs, bounds, native network configuration and attachment relationships.
Serialize native skeleton and animation configuration according to their actual containing assets.
There is no blanket requirement to create new material, texture, marking or animation resource files.

<a id="persistence-resource-decision-procedure"></a>

## Resource decision procedure

For each visual resource, apply this sequence:

1. Determine whether the existing resource and its native inputs can represent the generated object.
2. If unchanged use is valid, retain its reference. Apply provenance-based external snapshots.
3. If only geometry, UVs or native placement must change, save those changes and retain compatible
   source resources.
4. If an actual surface/material change is necessary, record the incompatible source inputs, exact
   changed fields and evidence that the native object path requires the change. Save only the
   genuinely modified resource with a private identity. Retain compatible texture/resource
   references and apply external snapshots to unchanged external dependencies.
5. Apply the material contract's [open-ended source handling](material-spec.md#material-complete-material-processing-without-propagated-exceptions-part-2)
   when exact equivalence is unavailable: produce the authorized persistable compatibility output
   with explicit differences rather than rejecting an otherwise valid source. Temporary preview
   materials are not a substitute for that saved representation or evidence of native acceptance.

Markings must follow the moving span where required. Their visual geometry or attachment can need
conversion while their original textures and material resources remain references. Preserve traffic
lanes independently and prevent duplicate stationary visuals in the hidden moving span.

These requirements do not establish implementation or native animation reload acceptance.

<a id="persistence-5-review-and-acceptance"></a>

## Review and acceptance

- Inventory newly generated assets separately from unchanged references and external snapshots.
- For each new resource, identify the changed data or runtime-only data that requires materialization.
  "The bridge moves" is insufficient justification for duplicating an unchanged source resource.
- Verify that all required owned nodes are in the save plan and their serialized references resolve.
- Verify derived geometry, LODs and configuration survive reload; unchanged surface references retain
  their intended identities.
- Verify built-in resources are not copied and external snapshots retain exact bytes and CIDs.
- Preserve existing ownership, missing-dependency failure, migration and manager-independence rules
  in [bridge generation contract sections 17](../contracts/persistence-contract.md#generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge) through 19. Other-owner input never substitutes for the target's required independent snapshot.
- Use cold-start and save/load acceptance to establish native operation. A successful build, preview
  or file scan alone does not prove animation, road connectivity or shipping compatibility.

Dependency traversal must not rewrite or remove healthy bridges. Deployment recovery requires
a concrete incompatibility and remains owner-scoped under [bridge generation contract section 13](../contracts/development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets).

<a id="persistence-movable-bridge-size-acceptance"></a>

## Movable bridge size acceptance

Bridge generation contract section 24 requires the complete movable bridge Prefab/dependency size to be
comparable to its native counterpart. Count private generated resources and external snapshots,
and include referenced built-in payloads in the logical comparison without copying them.
Report physical exported files separately. Sharing unchanged appearances across LODs and exact
constant textures within an owner is permitted; cross-owner snapshot deduplication remains
forbidden.

<a id="persistence-6-approved-owner-directory-layout"></a>

## Approved owner-directory layout

This specification applies to fixed and movable bridges. Native cold-load acceptance
requires in-game evidence.

```text
ImportedData/<UUID>/
  Prefabs/         All owned Prefabs, including every LOD
  Geometry/        Owned generated geometry
  Appearance/      Owned Surface, Material and ordinary Texture files
    VT/            Owned VTSurface and VTTexture payloads
    MidMips/       Owned native mid-mip caches, grouped by unchanged cache CID
  Locales/         Owned native locale assets
  Dependencies/    Byte-identical external non-built-in dependency snapshots
```

These are optional categories, not a mandatory pre-created tree. Do not introduce extra folders
for mechanism, supports, attachments, material types or individual LODs. Names distinguish those
assets. Keep each Prefab/geometry/texture independently identified by its CID. Do not merge payloads.

Here `<UUID>` denotes the full permanent owner identity `b{uuid}`. The root is relative to
the game-provided persistent data directory, not a hardcoded user profile. Programs are supplied by the mod and have no owner-directory category. Native mid-mip files
retain their tag-bearing basename under `Appearance/MidMips/<cacheCID>/`.

## Mod program references and startup registration

The [material program specification](material-spec.md#material-dynamically-generated-and-independently-persisted-material-shaders-part-2)
owns the stable template/full-integer-queue format, startup ordering and acceptance criteria.
The [persistence boundary](../contracts/persistence-contract.md#native-shader-payload-and-registration-recipe)
defines the reference-only exception and preservation of ordinary appearance dependencies.

<a id="persistence-filename-and-identity-rules"></a>

## Filename and identity rules

Permanent BridgeBuilder prefabs use `b{uuid}` exclusively; temporary previews keep
`tmp{uuid}`. Display names remain independent. There is no runtime `r{uuid}` alias
or legacy registry acceptance. Road Builder source-road identifiers are unchanged.

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
  Unknown types are handled under [section 7](persistence-spec.md#persistence-7-legacy-to-owner-directory-migration).
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

<a id="persistence-7-legacy-to-owner-directory-migration"></a>

## Legacy-to-owner-directory migration

This conversion is invoked only by self-check. Normal loading, generation, export and UI consume
the resulting current layout. Historical interpretation belongs to migration, while native game
and third-party source serialization versions remain a separate input domain.

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

Owner-persisted custom-program bundles and their old protocols are not layout-migration inputs. Self-check classifies them for scoped retirement under the explicit compatibility exception. An arbitrary `.bundle` extension alone is not a legacy-program signature and never authorizes deletion.

`<short name>` follows [section 6](persistence-spec.md#persistence-6-approved-owner-directory-layout)'s owner-UUID removal rule. Resolve ownership using existing approved
UUID/path rules plus the new UUID-directory context. Ambiguous or foreign ownership must not be
guessed for mutation. Reading another bridge's input is permitted; persist its required
independent snapshot without modifying that source. Legacy internal-name
conversion is a separate existing operation; this layout migration does not rename serialized objects.

<a id="persistence-filename-mapping-examples"></a>

## Filename mapping examples

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

<a id="persistence-bridge-zip-export"></a>

## Bridge ZIP export

Export is an action button named **Export** inside **Manage bridges**, alongside Build
and Delete. There is no separate export tab or page. Select a bridge and click Export,
then confirm its editable absolute ZIP path and top-level archive folder before any output
is created. The default
path is `<configured export directory>/<bridge display name>.zip`; characters invalid
in a filename are replaced with underscores. The mod settings directory picker defaults
to `Documents/BridgeBuilder/Exports`.

Export copies all files recursively from `ImportedData/<bridge UUID>/`, including CID
sidecars and owner-local dependency, VT and mid-mip files. Mod-owned program payloads are excluded. Archive entry names are
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

Follow [mod-owned precompiled program requirements](../contracts/material-contract.md#material-dynamically-generated-and-independently-persisted-material-shaders) for stable Surface references, mod package loading and queue-specific startup registration; geometry, texture and source ownership rules remain unchanged.

<a id="movable-native-serialization-and-reader-compatibility"></a>

## Native serialization and reader compatibility

Related requirements: [Movable Contract](../contracts/movable-contract.md#movable-native-serialization-and-reader-compatibility-part-2), [Persistence Spec](persistence-spec.md#movable-native-serialization-and-reader-compatibility-part-3).

Register the complete owned graph before native Prefab serialization. Save generated Geometry,
locales and identity sidecars through the existing shared lifecycle; no parallel save registry or
custom animation format. Reader, migration, recovery and metadata paths must support the native
writer's format, including typed anonymous Vector3/quaternion values in Odin skeleton data. Valid
native text must not be classified as corruption because a helper assumes ordinary JSON syntax.
Fix shared reader compatibility rather than stripping animation or bypassing validation. Preserve
the [self-check contract](../contracts/reading-map.md#bridge-self-check-contract) for migration and identity preservation.

<a id="movable-native-serialization-and-reader-compatibility-part-3"></a>

## Cold-start native behavior

Related requirements: [Persistence Spec](persistence-spec.md#movable-native-serialization-and-reader-compatibility), [Movable Contract](../contracts/movable-contract.md#movable-native-serialization-and-reader-compatibility-part-2).

Cold-start loading with Bridge Builder enabled must retain construction, native fees/unlocks,
presentation, road connectivity, animation and ship coordination. A warm-session cache or a working
closed bridge is insufficient. Save/load during intermediate movement must recover to valid native
operation without a manager callback; the exact recovery pose is determined by native behavior.

<a id="movable-native-persistence-layout"></a>

## Native persistence layout

Follow [persistence policy sections 6–7](persistence-spec.md#persistence-6-approved-owner-directory-layout)
for UUID-contained Prefabs, Geometry, Appearance, Locales and Dependencies, safe owned filename
stems, paired CID migration and on-demand directories. All owned LOD Prefabs share `Prefabs/`
while retaining independent CIDs. VT/mid-mip resources belong below the same owner UUID.
Layout changes preserve internal references and do not establish native cold-load acceptance.

<a id="contract-index-documentation-language-and-evidence-labels-part-3"></a>

## Owner directory and migration layout

Related requirements: [Constitution](../constitution.md#contract-index-documentation-language-and-evidence-labels), [Documentation Contract](../contracts/documentation-contract.md#contract-index-documentation-language-and-evidence-labels-part-2), [Material Contract](../contracts/material-contract.md#contract-index-documentation-language-and-evidence-labels-part-4).

The [persistence directory and migration specification](persistence-spec.md#persistence-6-approved-owner-directory-layout)
places owned resources and dependency snapshots below each UUID. Creation and migration share
that layout, including VT and mid-mip resources. Native cold-load acceptance requires separate
in-game verification. Section 7 defines migration safeguards.


## Dependency persistence diagnostics

Dependency save logs retain exclusive traversal, resolution, source-read, destination
preflight, write, verification, footprint and remaining-time buckets. The historical
`globalIndexBuilds`, `midMipIndexBuilds`, `midMipCacheReads` and related time fields remain
compatible log fields; the forward path leaves them zero. A material footprint includes
native payloads and CID sidecars, separately from packaged-resource size.

## Missing bridge save repair

At the city loading-complete callback, `BridgeMissingAssetSystem` checks placed
network references against the game's obsolete prefab IDs. It does not use the local bridge
registry as proof of absence. A live prefab is always preserved.

Scope: exact `b{uuid}`, `b{uuid}_Lower`, `b{uuid}_Upper` network prefabs only. Road Builder `r`/`t`
IDs, temporary previews, arbitrary suffixes and unrelated missing assets are excluded.
Obsolete static objects bearing an exact missing bridge ID are included even if owner links are lost.
The old `r{uuid}` bridge naming scheme is not reintroduced.

Removal uses `BridgeInstanceRemoval` and native `Deleted` processing, not immediate entity
destruction. Shared junctions survive; a missing junction prefab is replaced from an adjacent
surviving valid network when possible. Recursive Owner/SubNet/SubLane/SubObject descendants are
explicitly collected, marked Deleted and tracked. Applied/Created/Updated tags are removed from
deletion targets as in native SubElementDeleteSystem. Shared composition/prefab caches are excluded.
UI updates collect late descendants and verify every tracked entity is gone. Exceptions and a
30-second incomplete cleanup produce CRITICAL diagnostics. A single
localized dialog is shown after all tracked entities cease to exist. State is reset
before loading another city. No save file is overwritten: the user must save the repaired city.

## Missing bridge repair acceptance

- Load a save with a missing single-deck bridge: remove its segments and isolated endpoints;
  display one dialog. Check road connections, lanes, vehicles and orphan bridge objects.
- Repeat with a double-deck bridge, including a missing auxiliary `_Lower` or `_Upper` prefab.
- Connect the missing bridge to ordinary roads: preserve the roads and shared junction; confirm
  native junction rebuilding is correct.
- Keep a valid bridge prefab but remove its registry row: do not remove the bridge.
- Include an unrelated missing mod network: do not remove it.
- Load paused; verify deletion completes and the dialog appears (also check after unpausing).
- Load a second city without quitting; no counts or entity handles from the first city may leak.
- Save under a new name and reload: removed networks stay absent; no repeated removal dialog.

Compilation does not establish these runtime outcomes. In-game acceptance is still required.

## Native unlocking and persistence

The selected bridge prototype supplies the unlock policy. `BridgeUnlockSnapshot` captures its
requirements, and `BridgeNativeUnlock` persists the nested AND/OR expression using native
`Unlockable` components and bridge-owned native group assets. Generated main and owned auxiliary
networks receive those conditions. Do not flatten them into a current unlocked flag or require the
Bridge Builder assembly to evaluate them after a cold start.

For non-Network source structures without a native network unlock graph, preserve the player's
selected network's applicable native gate. Do not invent requirements for the virtual prototype.

Create-and-build and Manage/Build check readiness before activating the construction tool in game
mode. `BridgeUnlockPolicy.TryPrepareBuild` evaluates the persisted native rules and synchronizes
the generated networks' locked state. Explicit owned groups use their persisted expression; carried
native gates with implicit dependencies use the initialized `UnlockRequirement` buffer and native
current `Locked` bits. Do not use portable snapshot capture as a current-state query: its source
readiness and cycle rules are intentionally stricter. Native self/cyclic locked requirements remain
locked, and are not evidence of unavailable data. Publication completion follows the scheduled
native unlock pass rather than preceding it at the end of prefab initialization.
Missing or unavailable requirements are a readiness failure;
they must not silently unlock the bridge. Editor placement is outside this game-mode guard.

The optional manager setting to remove development restrictions operates while the mod is loaded;
the persisted native requirements remain the independent asset behavior. Required game/DLC content
and the documented double-deck rail-seam limitation remain separate compatibility concerns.

## Native economy and unlock acceptance

Relevant existing checks include `tools/CheckNativePricing.mjs`, `tools/UnlockSnapshotCheck`,
`tools/PortableMigrationCheck`, `tools/NativeLocaleCheck` and `tools/SelfCheckLifecycleCheck`.
These checks do not establish in-game billing or unlock behavior. Human acceptance covers
single- and double-deck placement, locked/unlocked requirements, save/load, and a cold start with
Bridge Builder enabled while required native content remains installed.

## Recorded persistence evidence

The [one-time identity migration](../archive/audits/bridge-identity-migration.md), [dependency verification](../archive/audits/dependency-persistence-verification.md), and [resource-loading observations](../archive/audits/resource-loading-order.md) retain their original historical scope and do not establish current native acceptance.

## Legacy custom-program compatibility exception

The [constitution's scoped compatibility exception](../constitution.md#backward-and-cross-environment-compatibility)
and [self-check retirement](../contracts/self-check-contract.md#self-check-obsolete-custom-program-retirement)
govern legacy custom programs. Current writer obligations follow the
[program persistence boundary](../contracts/persistence-contract.md#native-shader-payload-and-registration-recipe).
