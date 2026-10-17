# Bridge asset persistence and reference policy

## Contents

- [1. Four distinct operations](#1-four-distinct-operations)
- [2. Component and resource decisions](#2-component-and-resource-decisions)
- [3. Evidence from the existing creation path](#3-evidence-from-the-existing-creation-path)
- [4. Application to movable-road conversion](#4-application-to-movable-road-conversion)
- [5. Review and acceptance](#5-review-and-acceptance)
- [Movable bridge size acceptance](#movable-bridge-size-acceptance)
- [Organization revision record](#organization-revision-record)

This document is a normative supplement to bridge generation contract section 17. It defines persistence
scope for existing bridge creation and for future movable-bridge generation. It does not establish
that any unfinished movable generator has passed acceptance.

Reviewed on 2026-10-16 against the local working tree on branch `dev`, committed baseline
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`. Rollback reference:
`refs/rollback/asset-persistence-scope-20261016`. The reference records committed history, not the
pre-existing uncommitted implementation or documentation changes.

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
The destination is:

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
