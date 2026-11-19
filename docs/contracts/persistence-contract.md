# Asset persistence contract

Required companion: [Persistence Spec](../specs/persistence-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="repository-source-input-ownership"></a>

## Source input ownership

Follow [source input and independent output ownership](persistence-contract.md#generation-source-input-and-independent-output-ownership): accept valid other-owner copies as inputs,
without a backend provenance gate. Persist independent target-owner snapshots and preserve
all write/delete boundaries; source access never authorizes mutation of the source.

<a id="runtime-materials-ownership-and-persistence"></a>

## Materials, ownership and persistence

Read the complete [material scope](reading-map.md#material-generation-contract) for appearance requirements
and program registration, [native lifecycle](#generation-native-lifecycle-and-additive-conversion)
for persisted presentation, fees and unlocks, and [source/output ownership](#generation-source-input-and-independent-output-ownership)
for independent dependency copies. [Deletion](reading-map.md#bridge-deletion) and
[self-check](reading-map.md#bridge-self-check-contract) retain their separate removal, recovery and restart responsibilities.

<a id="persistence-write-completion-before-reads"></a>

## Write completion before reads

Generated native resources must finish content initialization and synchronous native writing,
including closing the payload stream and writing the CID sidecar, before property reload,
relocation consumers or dependency traversal can read them. Asset registration alone does not
prove that a file exists. Verify the completed native path and type extension; never embed a
source URI or its extension into the generated filename stem. A failed write must stop that
owner transaction before readers/publication proceed. Do not hide ordering failures with sleeps
or retries. Tests must exercise actual native filesystem save, unload, relocation and reload,
not only an in-memory material binding.

<a id="persistence-contents"></a>

## Contents

- [Safe owned asset filename stems](../specs/persistence-spec.md#persistence-safe-owned-asset-filename-stems)
- [Write completion before reads](persistence-contract.md#persistence-write-completion-before-reads)
- [1. Four distinct operations](persistence-contract.md#persistence-1-four-distinct-operations)
- [2. Component and resource decisions](../specs/persistence-spec.md#persistence-2-component-and-resource-decisions)
- [3. Evidence from the existing creation path](../specs/persistence-spec.md#persistence-3-evidence-from-the-existing-creation-path)
- [4. Application to movable-road conversion](../specs/persistence-spec.md#persistence-4-application-to-movable-road-conversion)
- [5. Review and acceptance](../specs/persistence-spec.md#persistence-5-review-and-acceptance)
- [Movable bridge size acceptance](../specs/persistence-spec.md#persistence-movable-bridge-size-acceptance)
- [6. Approved owner-directory layout](../specs/persistence-spec.md#persistence-6-approved-owner-directory-layout)
- [7. Legacy-to-owner-directory migration](../specs/persistence-spec.md#persistence-7-legacy-to-owner-directory-migration)
- [Bridge ZIP export](../specs/persistence-spec.md#persistence-bridge-zip-export)

This document is a normative supplement to [bridge generation contract section 17](persistence-contract.md#generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge). It defines persistence
scope for existing bridge creation and for future movable-bridge generation. It does not establish
that any unfinished movable generator has passed acceptance.

The owner-directory layout in [sections 6–7](../specs/persistence-spec.md#persistence-6-approved-owner-directory-layout) applies to creation and self-check migration.
Sibling `_Dependencies` and per-component LOD directories are legacy migration inputs.
[Section 3](../specs/persistence-spec.md#persistence-3-evidence-from-the-existing-creation-path) documents the creation and dependency paths. VT and mid-mip caches are included in the owner
folder; native cold loading and packaging with Bridge Builder enabled still require game acceptance.

<a id="persistence-1-four-distinct-operations"></a>

## Four distinct operations

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
   These snapshots are not newly authored materials or converted resources. The mod-owned precompiled programs are the explicit reference-only exception defined below.

In this document, "do not persist a resource" means do not create a separate private copy of its
unchanged built-in contents. Its reference and any required native configuration still belong in
the saved graph. Conversely, an unchanged external material can require a dependency snapshot
without requiring material conversion or a new material identity.

<a id="persistence-self-check-routing"></a>

## Self-check routing

Follow the [self-check contract](reading-map.md#bridge-self-check-contract) for discovery, cache-relation
preparation, read failures, owner recovery, reporting and restart. No separate inspection policy
is defined here; the file layout and transaction mechanics below remain shared with generation.

<a id="native-presentation-and-manager-independence"></a>

<a id="persistence-native-presentation-and-startup-registration"></a>

## Native presentation and startup registration

Every file-only root migration must carry its persisted bridge metadata onto the replacement
root, including migrations triggered only by unlocking, icon, pack or component cleanup.
Native component cloning resets component names; an already complete source metadata field
does not remove the need to assign it on the clone. Preserve the player's name, selected-road
identities, style and creation history. A cold reread after a successful migration must require
no further metadata or missing-description writes.

`src/BridgeBuilder/Runtime/BridgeNativePresentation.cs` prepares native presentation, saves native
locale data and validates existing/migrated serialized references for manager-dependent types and UI paths.
Normal creation prepares native presentation before saving and does not call that additional
existing-asset validator after successful dependency persistence.
`src/BridgeBuilder/Runtime/BridgeAssetCatalog.cs` invokes the native presentation save path when
writing catalog metadata. These responsibilities are separate from material export.

Native persisted presentation must survive mod-enabled cold loading; mod-owned programs use Bridge Builder startup registration. Required
game/DLC content remains a prerequisite. Copying an external file also does not supply a missing
runtime class or an unsupported third-party behavior.

<a id="persistence-native-material-resources"></a>

## Native material resources

The [material scope](reading-map.md#material-generation-contract) governs road/non-road routing,
prototype support, mapping, offline preparation, bounded runtime exceptions and program registration.
Persist the representation it selects under [resource decisions](../specs/persistence-spec.md#persistence-2-component-and-resource-decisions)
and [independent material ownership](#material-identity-and-independent-ownership).
The [shared MidMips source exception](../specs/material-spec.md#material-shared-immutable-midmips-source-cache)
does not relax per-owner snapshots. Report owner and complete SDK package bytes separately under
[material size reporting](material-contract.md#material-5-per-bridge-material-directory-and-mod-packaging).

<a id="persistence-empty-directories-and-ownership"></a>

## Empty directories and ownership

Create a category directory only when its first actual payload is written. No placeholder files
or empty directory skeletons. Missing optional directories are not damage; validate required
references and payloads instead. Failed generation removes only its own created files and then
its newly empty directories. Successful migration removes emptied source directories only after
verification, within verified owner-scoped boundaries. Never remove unrelated or nonempty folders.

An asset is owned by the UUID path context even when its basename no longer contains a UUID.
Update ownership, cleanup, export, audit and source-isolation code accordingly before deployment.
Keep logs, manager state, reports, backups and preview caches outside the active bridge directory.

<a id="persistence-vt-and-packaging-boundary"></a>

## VT and packaging boundary

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

<a id="persistence-transaction-and-collision-rules"></a>

## Transaction and collision rules

The writer owns the complete owner-scoped transaction and its rollback outcomes.

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
   Self-check failure handling follows the [self-check contract](reading-map.md#bridge-self-check-contract).
6. Remove verified empty old owner directories. Re-running migration must be a no-op. Unknown
   files stay in place for review; their presence must not trigger blanket deletion.
7. Inspection lifecycle, stale cached reads, recovery outcomes and restart follow the
   [self-check contract](reading-map.md#bridge-self-check-contract). A layout transaction does not rebuild a bridge.

<a id="persistence-required-implementation-review-and-acceptance"></a>

## Required implementation review and acceptance

Whenever the layout changes, update all producers and consumers together: Prefab/geometry/
material/texture/locale writers, dependency writer and exclusions, output ownership isolation, disk audit,
legacy-name handling, file-layout migration, pending-creation cleanup, UUID deletion/backup, and
pack/export discovery. Disk filename and internal Prefab name must be allowed to differ.

Verify migrations from both the old per-Prefab layout and the grouped-LOD layout, already-migrated
inputs, identical and conflicting destinations, missing sidecars, rollback after partial IO failure,
unknown types, dependency Prefabs with unrelated internal names, and preservation of other owners.
Then verify native cold loading and packaging with Bridge Builder enabled. Keep documentation approval,
implementation, deployment and game acceptance as separate statuses.

<a id="persistence-shared-implementation-responsibilities"></a>

## Shared implementation responsibilities

- `BridgePrefabPersistence` and `BridgeAssetInfo` share category/short-name rules across Prefabs,
  geometry, paint resources, locales, movable material finalization and file-only migration.
- `BridgeAssetLayout` handles old per-component/grouped LOD folders, ordinary appearance files,
  VT, mid-mip caches and dependency directories. Complete payload/CID pairs are preflighted and
  backed up; identical pairs may be reused. Conflicts preserve healthy bridges. Partial moves
  roll back, and successful reruns do nothing. Internal names and native references are unchanged.

- Normal root lookup accepts only `Prefabs/Bridge.Prefab`. Self-check alone discovers the legacy UUID
  filename and interprets historical dependency layouts before migrating to the current layout. Legacy-name migration recognizes short-named
  owned geometry, avoiding unnecessary new identities after layout migration.
Recorded automation and native inspection limits are retained in [layout verification evidence](../archive/audits/persistence-implementation-records.md#layout-verification-scope).

<a id="generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge"></a>

## Persist external dependencies as identical-CID copies owned by each bridge

The existing bridge persistence workflow must also preserve its external asset dependencies locally.
A bridge saved in `ImportedData` can be deserialized before a subscribed asset pack becomes available.
A nonempty CID in a valid bridge file does not make that dependency available at that earlier phase.
Persist the required external files alongside the bridge instead of relying solely on the later
registration of the subscribed source.

<a id="generation-persistence-scope-generated-data-references-and-dependency-snapshots"></a>

## Persistence scope: generated data, references and dependency snapshots

Apply the [four operations](#persistence-1-four-distinct-operations) and
[component/resource decisions](../specs/persistence-spec.md#persistence-2-component-and-resource-decisions)
to fixed and movable generation. A changed native representation requires a private identity;
unchanged external dependencies retain original bytes and CIDs under the graph-copy contract below.
The [mod program boundary](#native-shader-payload-and-registration-recipe) is the explicit reference-only exception.
These obligations do not permit saving preview-only state or omitting required native references.

<a id="generation-copy-the-serialized-dependency-graph-without-changing-asset-identity"></a>

## Copy the serialized dependency graph without changing asset identity

- Start with all persisted prefabs belonging to the bridge, including both decks and derived assets.
  Recursively follow serialized external CID references through non-built-in dependencies until each
  branch reaches a built-in asset or an asset with no further serialized dependencies. Include the
  necessary non-prefab dependencies, such as external geometry and materials; copying only the first
  external prefab is insufficient.
- Identify built-in assets from the game's asset provenance, not from names or apparent similarity.
  Built-in resource-map `UnityGUID` references are terminal. Do not copy or modify original game/DLC
  assets. Already-persisted assets owned by this bridge are traversed without making a redundant copy.
- Read original asset bytes, including entries inside source packages. Copy those bytes unchanged and
  retain the original CID in the `.cid` sidecar. Do not reserialize the prefab, change its name, replace
  its CID, or rewrite its references. Do not mutate, move or delete the source asset.
- This rule concerns unchanged dependency snapshots. Newly generated or geometrically modified
  bridge assets retain their own identities under the existing generation rules. It does not permit
  a modified asset to impersonate its donor by reusing the donor's CID. The shared references required
  by [section 15](../specs/generation-spec.md#generation-15-bridge-lighting-is-copied-from-the-archetype-as-behavior) remain unchanged; this rule supplies local copies of their non-built-in targets.

<a id="generation-deduplicate-within-a-bridge-never-across-bridges"></a>

## Deduplicate within a bridge, never across bridges

Store copies under:

```text
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>.cid
```

Native `.MidMips` snapshots are the narrow filename exception: use
`Dependencies/<CID>/<original TS*_MB*.MidMips basename>` and its `.cid` sidecar.
Native streaming derives the tile-size/mip-count tag from that basename. Preserve the
original cache bytes and CID; do not rename the cache to `<CID>.MidMips` or fabricate empty data.
Traverse Surface-to-MidMips dependencies through the cache's native primary and duplicate Surface
CID memberships, including current-owner snapshots when the source package is absent. The same
resource validity and destination conflict checks apply before copying these caches.

[Shared source requirements](../specs/material-spec.md#material-shared-immutable-midmips-source-cache) permit a read-only packaged cache at
`~Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` with its CID sidecar and define the separate retained paint-package boundary.
These narrow source exceptions do not allow general cross-style texture sharing. Each owner still copies only its required native cache
dependencies independently; the source exception does not permit shared generated-owner files,
general cross-style texture storage or additional runtime baking.

Use a visited-CID set for each bridge traversal. Multiple references to the same CID within one bridge
produce one copy, including when a dependency is reachable through several paths. Traversal must
terminate even if a future dependency graph contains a cycle; do not assume every source graph is a
DAG. Separate bridges must each retain their own physical copy of a shared external CID, in their own
UUID directory. A global deduplication cache must not eliminate those per-bridge copies.

The unchanged source and the per-bridge copies deliberately share a CID. Equality of CID alone is not
proof of equality of content: if an existing destination has different bytes for that CID, report the
conflict and fail persistence rather than silently overwriting it. Preserve all required references;
an unavailable dependency is an explicit persistence failure, not permission to omit a section,
sub-object, light, material or other dependency. Do not report successful bridge creation until the
required copies have been saved and verified.

For a newly generated bridge, successful completion of this creation call's normal native
persistence establishes its dependency guarantee. Proceed to native publication without a
second independence/dependency traversal or global cache re-indexing of that same output.
Keep required write-completion, byte/CID conflict and copy checks inside persistence; failure or
exception must prevent publication. This is not cross-owner or cross-run trust. Existing-asset
loading, migration and post-load self-check retain their independent inspection requirements.

<a id="generation-ownership-self-check-and-acceptance"></a>

## Ownership, self-check and acceptance

Ownership of these copies comes from the exact bridge UUID directory, not the copied prefab's original
name or CID. Intentional identical-CID snapshots are distinct from unintended identity collisions;
inspection follows the [self-check contract](reading-map.md#bridge-self-check-contract).

Back up and retire dependency copies with their owning bridge. For explicit player deletion, follow [the shared owner lifecycle](deletion-contract.md#deletion-normal-shutdown-physical-cleanup). Ownership-scoped maintenance and
cleanup tools must include the dependency directories, including partial files from interrupted saves.

Dependency copying is a file persistence operation. Do not add live integrity inspection,
reference repair, quarantine or registration rejection to that operation. The separate post-load
self-check follows the [self-check contract](reading-map.md#bridge-self-check-contract). Resolving an asset identifier
to its original data stream for copying is distinct from inspecting a live prefab for damage.

Verification must cover recursive text/binary references, stopping at built-in assets, byte and CID
preservation, per-bridge deduplication, independent copies across bridges, cycle termination, explicit
failure on missing/conflicting sources, and owner-scoped cleanup. Cold-start and old-save acceptance
remain required to establish that the game discovers and resolves the copies before network
initialization. File equality, a DAG check or a successful build alone does not establish that timing.

<a id="generation-18-bridge-asset-file-ownership-is-determined-only-by-buuid-text"></a>

## Asset file ownership

A bridge asset file belongs to Bridge Builder if its relative asset path contains
`b{uuid}`. For one bridge, use literal substring matching with the fixed bridge UUID.
For all bridge assets, use the unanchored regex
`b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}`.
Do not require token boundaries, exact filenames, metadata, components, registration,
CID contents, hashes, dependency graphs or successful deserialization to establish ownership.
A matching ancestor directory owns its entire subtree, including empty directories and
CID-named dependency copies. Apply this rule before every bridge asset read, write, move
or delete, including startup recovery, management, generation and maintenance scripts.

Keep operation scope restricted to intended asset roots; paths outside those roots and
unsafe destinations are not authorized by an incidental match. Path containment and IO
errors are operational safeguards, not additional asset ownership criteria. Native SDK
asset location information may locate bytes but must not classify ownership. Reading
external source assets for dependency copying does not make those sources bridge-owned.
The shared UI asset pack and non-asset settings/logs are not individual bridge assets.

Metadata may be read or written as payload (for example, display names and creation style IDs), but
its presence, absence or values never establish or revoke file ownership. [Section 19](persistence-contract.md#generation-19-independent-native-persistence-and-additive-legacy-conversion) governs
field-by-field migration, including native payloads and file layout; [section 17](persistence-contract.md#generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge)'s dependency-copy
operation alone does not rewrite source metadata. For deployment cleanup, [section 13](development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets) selects the
affected style IDs and bridge UUIDs first; this section then determines the files owned by those
UUIDs. Move selected files/directories as whole units with collision-free backup names. Do not
inspect contents, hashes or tree snapshots to establish ownership. Report real move failures.

<a id="generation-uuid-reservation-and-mapped-directories"></a>

## UUID reservation and mapped directories

Generate a new UUID after every occupied identity or native directory-name collision, with no
attempt limit. Keep the atomic OS directory claim: an already-existing directory/file retries,
whereas real permission, IO or native errors terminate allocation with their actual diagnostics.
Do not describe collisions as allocation exhaustion. Normal game roots, ImportedData, auxiliary
owner roots and deletion-ledger directories may be directory mappings/junctions; their presence
alone must not reject creation or management.

Preserve logical owner containment under configured roots and apply
[mapped-directory equivalence](../specs/cross-environment-spec.md#always-applicable-portability-requirements)
to the actual owned payloads, including owner and nested directory mappings. Writers and recovery
operations must fulfill the same persistence, backup and removal postconditions as unmapped paths;
moving or removing only a link does not fulfill those postconditions. Never treat a mapping target's
unrelated contents as owned. Preserve other owners and source assets through resolved IO boundaries.
Deletion consent records themselves retain their non-link format checks. See
[deletion](reading-map.md#bridge-deletion) for durable intent and shutdown timing.

<a id="generation-source-input-and-independent-output-ownership"></a>

## Source input and independent output ownership

Source provenance is not a backend admission gate. Generation, preview, dependency
copying and recovery may read a valid resource from another generated bridge,
including an identical-CID snapshot. Do not reject, filter or skip a source solely
because its name/path contains another bridge UUID or because it is not built-in.
Continue checking actual resource existence, native handles, schemas, content needed
by measured recipes and IO outcomes. Resolve exact references without global scans.

Reading a source does not transfer ownership. Persist every required non-built-in
dependency into the target owner's independent snapshot before reporting success;
a readable foreign copy alone does not establish target-owner independence. Do not
mutate, move, retire or delete another owner's input. Keep destination containment,
UUID claims, conflict detection and deletion boundaries. Owner-local cache metadata
must point to the target's completed copies, not permanent foreign paths. For deleted
resources follow [the shared owner lifecycle](deletion-contract.md#deletion-normal-shutdown-physical-cleanup).

<a id="generation-19-independent-native-persistence-and-additive-legacy-conversion"></a>

## Independent native persistence and additive legacy conversion

Here, independent persistence means each bridge owns its required snapshots and does not borrow another generated bridge’s private data. It does not require operation with Bridge Builder disabled or uninstalled.

<a id="manager-independence-and-additive-conversion"></a>

<a id="generation-native-lifecycle-and-additive-conversion"></a>

## Native lifecycle and additive conversion

Generated bridges must cold-load and operate with Bridge Builder enabled. Disabling, unsubscribing from or uninstalling Bridge Builder is not a supported-operation acceptance requirement. Bridge Builder may restore mod-owned precompiled program registration at startup; no per-frame transform or renderer synchronization service is authorized. Required game/DLC content remains a prerequisite, and external snapshots follow [section 17](persistence-contract.md#generation-17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge). Verify a cold start and existing save with Bridge Builder enabled; warm-session caches are not evidence.

Persist native Unlockable AND/OR conditions, unchanged native fees, LocaleAsset names and thumbnails. Do not substitute runtime unlocking, price overrides, mod-only localization or a custom UI host for these native payloads. The double-deck railway seam remains an unresolved compatibility limitation. [Section 16](../specs/generation-spec.md#generation-16-preserve-native-construction-and-upkeep-pricing) prohibits fee rewrites and pricing-only copies; preserve donors, shared component fees and byte-identical external dependencies with original CIDs.

Normal persistence readers and writers use only the current Bridge Builder representation.
Old-version interpretation and transformation are confined to self-check migration. Current game
and third-party source serialization formats remain supported input domains; they are not old
Bridge Builder release adapters.

Self-check and legacy conversion follow the [self-check contract](reading-map.md#bridge-self-check-contract).

<a id="generation-permanent-creation-identity"></a>

## Permanent creation identity

Create, Create and Build, and the settings export action share the same allocator. It checks loaded
prefab owners, export state, the canonical user asset registration, pending creation/removal and
retired session identities. It also checks immediate names under ImportedData, BridgeBuilder and
StreamingData~/VT for owner residue and checks the native95 removal ticket for that UUID.

The claimed directory remains the owner boundary during generation and asynchronous publication. Partial output is left for startup recovery. Test collision cases with `dotnet run --project tools/UuidReservationCheck`.

All permanent creation entry points use [atomic UUID reservation](#generation-uuid-reservation-and-mapped-directories)
before writing bridge assets. In-flight owners, pending removal, incomplete files and older layouts
all count as occupied. Failed attempts preserve existing assets and never schedule cleanup of the colliding owner.

For confirmed player requests, follow [Bridge deletion](reading-map.md#bridge-deletion).

<a id="material-identity-and-independent-ownership"></a>

## Identity and independent ownership

Whenever a required material can be reused in full, reference it directly. Do not create a
clone, replacement identity, baked equivalent or duplicate texture. A supported-type composition plan
may resolve an unchanged region to an original material without baking only when its mapping
and all-LOD marking content satisfy [sections 2–3](../specs/material-spec.md#material-2-preserve-source-resolution-and-pattern-scale). Unsupported types bypass
prototype composition and use compatible originals or [section 4](../specs/material-spec.md#material-4-persist-constant-reusable-materials-before-runtime)'s corresponding road replacement. Sharing textures while changing a descriptor is a
conversion, not full reuse.

Keep built-in resources as original native references. Snapshot external resources, including
ordinary Bridge Builder resources but excluding its mod-owned precompiled program graphs, byte-for-byte with their original CIDs under each current bridge owner.
Copy all required non-built-in texture, VT and mip dependencies. Reuse complete current-owner
copies; never borrow another generated bridge's files or read deleted resources. Copies for
ownership are not newly authored materials and must not modify the source.

Self-check supplementation and recovery follow the [self-check contract](reading-map.md#bridge-self-check-contract).
Copying assets does not supply a missing third-party shader or runtime class.

<a id="material-packaging-and-source-resolution-part-2"></a>

## Installed source ownership

Related requirements: [Material Contract](material-contract.md#material-packaging-and-source-resolution), [Material Contract](material-contract.md#material-packaging-and-source-resolution-part-3).

Resolve the installed source directory relative to the loaded mod's installation location, never
a developer path, current working directory or another generated bridge. Treat it as read-only.
Copy required external resources into the new bridge's independent dependency snapshot under
the existing persistence policy; deleting a bridge must never delete packaged source materials.

<a id="movable-11-persistence-failures-and-compatibility"></a>

## Persistence, failures and compatibility

Movable output follows the same [four persistence operations](#persistence-1-four-distinct-operations),
[resource decisions](../specs/persistence-spec.md#persistence-2-component-and-resource-decisions),
[selected-road conversion requirements](../specs/persistence-spec.md#persistence-4-application-to-movable-road-conversion)
and [source/output ownership](#generation-source-input-and-independent-output-ownership).
These scopes retain skinning, LOD, bounds, skeleton and attachment persistence, private identities
for changed resources, unchanged source roads/DLC assets and the exclusion of preview-only state.

<a id="movable-runtime-runtime-invariants-part-10"></a>

## Mod-enabled native lifecycle

Follow [native lifecycle and additive conversion](#generation-native-lifecycle-and-additive-conversion)
and [the self-check contract](reading-map.md#bridge-self-check-contract). Additive migration preserves
root/deck CID, UUID, version and geometry. Saved Prefabs must not require a BridgeBuilder component
or custom UI host; the mod program registrar remains the explicit runtime dependency.
Deployment recovery remains limited to demonstrated incompatibility under
[the deployment contract](development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets).

<a id="native-shader-payload-and-registration-recipe"></a>

## Mod program reference persistence boundary

The bridge writer saves the stable logical-family/full-integer-queue template hash and the ordinary integer Surface queue property referencing mod-owned precompiled variants; it must not save, copy, relocate or export a custom shader program, native graph bundle, program CID or registration recipe under an owner. This is an explicit exception to external-dependency snapshots for the mod-owned programs authorized by the [custom shader table](../specs/custom-shader-spec.md#custom-shader-table), not a waiver for ordinary source materials, textures, VT or mip dependencies.

The mod registry owns packaged program loading and in-memory lifetime. Cross-owner Surface copies preserve the stable reference and ordinary appearance dependencies without copying program bytes. Preview uses the same mod programs with private temporary drawing state, not a preview program FileAsset.

Old owner-program protocols have no compatibility adapter. Self-check alone identifies affected owners and applies [obsolete custom-program retirement](self-check-contract.md#self-check-obsolete-custom-program-retirement). Do not restore old hashes, convert old containers or substitute current program references into an old bridge. Healthy noncustom bridges retain existing compatibility and ownership guarantees.


## Native asset pack membership

- The shared persistent AssetPackPrefab is named BridgeBuilder Asset Pack, with
  localized asset title BridgeBuilder and a dedicated blue-bordered pack icon. The
  toolbar keeps its separate, tightly cropped white mask. Existing pack UIObject
  icon references are updated in place without reinitializing network entities.
  The pack is saved and
  registered before a newly created bridge references it.
- Every generated permanent NetGeometryPrefab receives exactly that AssetPackItem.
  Preview graphs never enter this persistence path. Prototype dependencies and
  ContentPrerequisite components are unchanged.
- On catalogue refresh, exact export-state/registration identities and their exact
  generated carried-deck names receive the same membership. Only loaded writable
  assets are changed. Unregistered UUID-looking names, source roads, archetypes,
  saves and mod caches are not modified.
- Live migration updates only AssetPackElement, matching the decompiled native
  AssetPackItem.LateInitialize behavior (clear buffer, append pack entity). It does
  not replace or reinitialize a live network prefab entity.

## Legacy custom-program compatibility exception

The [constitution's scoped compatibility exception](../constitution.md#backward-and-cross-environment-compatibility)
and [self-check retirement](../contracts/self-check-contract.md#self-check-obsolete-custom-program-retirement)
govern legacy custom programs. Current writer obligations follow the
[program persistence boundary](../contracts/persistence-contract.md#native-shader-payload-and-registration-recipe).
