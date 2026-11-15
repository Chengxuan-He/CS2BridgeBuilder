# Recorded persistence implementation evidence

Historical evidence relocated from linked source documents; no new audit or game test was performed. Statements describe only their recorded revision and inspection scope, not current acceptance. Unspecified original revision/artifact provenance remains unspecified. Current requirements remain in the originating contracts and specifications.

## Owned Prefab creation path

Source: [docs/specs/persistence-spec.md](../../specs/persistence-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> `vendor/CS2ModShared/src/Conversion/PrefabGraphCloner.cs`, `CloneRuntimePrefab`, explicitly clones
> requested roots. Non-root built-in Prefabs and Prefabs that already have an asset are retained as
> references. Runtime dependencies that require materialization are tracked in the clone plan.
> Composition can add other private nodes when it modifies native structures. This is selective graph
> construction, not an export of every asset reachable from the selected road.
> 
> `src/BridgeBuilder/Systems/BridgeGenerationSystem.Composition.cs`, `TryBuildBridge`, combines the
> cloner's nodes, additional native assets and created structure Prefabs into the save plan. Its
> preview branch returns before permanent persistence. The permanent path applies native metadata
> and presentation preparation, saves the owned nodes and completes dependency persistence before
> publication. Successful persistence of this newly generated graph is the dependency guarantee;
> do not repeat the same graph resolution, global cache indexing or independence traversal before
> publishing it. Preserve save failures, write completion and required native initialization checks.
> This trust is local to the successful creation call, not a cached exemption for old or other-owner assets.
> 
> `vendor/CS2ModShared/src/Export/PrefabAssetWriter.cs`, `Save`, filters by `NeedsSave`, registers the
> planned nodes first and then saves each Prefab. Its save call deliberately avoids recursive native
> saving of shared donor Prefabs. Registration makes target asset identities available when references
> are serialized. A reference in a `.Prefab` is not the embedded contents of its target material or
> texture.

## Derived geometry source surfaces

Source: [docs/specs/persistence-spec.md](../../specs/persistence-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> `src/BridgeBuilder/Bridges/Common/Geometry/TowerFactory.Mesh.cs` takes the original
> `surfaceAssets` array, builds derived models and, for permanent generation, saves a new `Geometry`
> asset. The derived render Prefab points to that geometry and retains the original surface references
> through `widened.surfaceAssets = surfaces`. Preview models take a temporary path.

## Installed Surface reference audit

Source: [docs/specs/persistence-spec.md](../../specs/persistence-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The installed 209-surface audit contains 95 VT surfaces and 248 such
> original IDs without standalone payloads; all 571 required reference edges resolve in game packages.

## Dependency and lifecycle fixture evidence

Source: [docs/specs/persistence-spec.md](../../specs/persistence-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The source-linked `tools/DependencySourceCheck` exercises the production resolver, surface parser
> and copy traversal with an owned root referencing an external material and texture. It verifies
> byte/CID preservation, missing-sidecar supplementation, no writes on repetition, resolution from
> current-owner snapshots with the source-mod registrations absent, and independent copies from other-owner sources, with missing data and destination conflicts
> remaining failures. Ownership classification does not reject source streams.
> `tools/SelfCheckLifecycleCheck` exercises the actual startup system with dependency-service doubles:
> healthy references still trigger supplementation, and the migration/restart notice survives a
> subsequent unchanged check. This lifecycle check does not itself verify native texture loading.

## Surface parser inspection scope

Source: [docs/specs/persistence-spec.md](../../specs/persistence-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The parser follows version 1 dictionaries and the native legacy property-record reader through
> version 11; actual resource-fixture verification currently covers version 1. Unsupported versions,
> truncated data and invalid records fail persistence explicitly. Other extensions remain terminal
> in this implementation. Built-in `UnityGUID` references are terminal too. This does not prove that
> arbitrary binary formats lack dependencies; inspect and implement each newly required format.

## Layout verification scope

Source: [docs/contracts/persistence-contract.md](../../contracts/persistence-contract.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> - Automated checks cover layout, resource relocation, self-check lifecycle, dependency copying,
>   output ownership isolation, legacy conversion and scoped cleanup. Native VT discovery/tag handling was
>   inspected in the installed game code. This record does not assert in-game cold-load acceptance.
