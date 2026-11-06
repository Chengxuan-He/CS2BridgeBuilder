# Movable bridge Prefab graph compaction

The grouped-decal description below is retained historical evidence. The 2026-10-17 repair removes all independent marking objects; see [Movable road paint and fixed platforms](Movable-Road-Paint-And-Platforms.md). The real follow-up sample logged 75 serialized Prefabs and 720 ms through native publication before that repair.

## Evidence and scope

The saved Large Road Divided / Draw01 sample `be0359a68-19df-426e-a991-3f23ea1e0d74` has 523 owned Prefabs, plus one unchanged external AssetPack dependency snapshot. The 523 count predates the grouped-marking deployment; it is not a measurement of the new build.

| Owned type | Recorded sample | Same-input expectation after both increments |
| --- | ---: | ---: |
| RoadPrefab | 1 | 1 |
| RenderPrefab | 210 | 21 |
| StaticObjectPrefab | 198 | 9 |
| NetSectionPrefab | 81 | 12 |
| NetPiecePrefab | 28 | 28 |
| Unlock AssetPackPrefab | 5 | 5 |
| Total | 523 | 76 |

The expectation assumes the same resolved road, upgrades, theme and marking groups. It is not a persisted-output measurement. The external snapshot remains separate and unchanged.

## Prototype comparison

The installed 1.6.2f1 audit identifies RoadPrefab `DrawBridge01` (resource path ID 118584), mechanism StaticObjectPrefab 118746 and ordinary support StaticObjectPrefab 118747. Path IDs are offline evidence only.

The prototype has 21 root section entries referencing four distinct sections. Ordinary, moving-node and connecting-edge conditions are stored directly in `NetSectionInfo`; repeated and mirrored occurrences share the section references. The mechanism and ordinary support each have three render roots with their existing LOD graphs. The generated bridge already preserves these six structural render roots and 18 structural RenderPrefabs.

The old generated sample has 69 root section entries because its selected road has 23 source entries. Each root entry previously referenced a newly allocated wrapper section, adding 69 Prefabs that had no corresponding prototype responsibility. The selected road's 23 entries must not be reduced to the prototype's seven lateral occurrences: doing that would change its road layout and upgrades.

## Implemented network compaction

The shared converter now puts the conjunction of the selected-road and measured mechanism conditions directly on each native root section entry. It retains offsets, inversion, flipping, median status, hidden layers and half-length behavior. Repeated unchanged sections reuse their original references. Sections and pieces with modified moving-object producers retain one private copy per source identity, through the existing conversion caches.

Every currently recorded B&P branch has an empty `RequireAny`, so none of these branches needs an additional condition-wrapper Prefab. For a future input with two independent OR groups, the converter retains one shared conjunction subsection: flattening `(a OR b) AND (c OR d)` into a single OR list would be incorrect, and overlapping root entries would duplicate lanes. This is semantic preservation rather than a bridge-name exception.

The original graph remains unchanged. New private assets retain bridge ownership and ordinary persistence. Preview and formal creation use the same conversion; disposal, saving, dependency copying and publication consume the returned created-node list without a parallel registration path.

## Markings and remaining differences

The preceding increment groups projection volumes by bone, paint color, priority and infoview behavior. For the recorded sample, 192 marking objects and 192 marking RenderPrefabs correspond to three groups, replacing 384 Prefabs with six. Each group preserves its separate closed projection volumes and its native motion parent. Existing materials are reused; no appearance baking is restored.

This is not literal node-for-node identity with the prototype. The prototype's markings are integrated into its authored surface; the generated selected-road markings still use three native child decal objects in this sample. The additional lamp/lighting-producer copies preserve selected-road lighting without duplicate stationary lamps, and the 12 modified sections/28 pieces preserve selected-road behavior in the moving span. Unlock dependency copies preserve native independent loading. These nodes must not be deleted merely to match a count.

The network wrapper layer is removed and the structural render hierarchy remains that of the prototype. Exact equality of the complete graph is not established: the remaining marking representation differs as described above. A future change that folds markings into the mechanism geometry must first establish a compatible existing object material and correct coverage at every LOD; inserting a DefaultDecal projector into an ordinary skinned structural material slot is not equivalent.

## Verification

Read-only inspection of the real saved sample checked all 69 wrapper/root pairs. All 7,860 combinations of their referenced requirement tokens produce the same result before and after direct conjunction. This checks condition logic, not geometry or visual acceptance. The native audit confirms the prototype's direct condition placement and shared section references. All recorded B&P converters were inspected for their OR-group shape.

New logs report root section entries, unique root sections, condition wrappers, modified network Prefabs and a type-by-type owned save graph. Existing stage timings distinguish generation, serialization and publication. The next human-created sample is required to verify the actual new count, generation time, closed/open appearance, traffic signals and cold loading.

Only Draw01 currently reaches the implemented movable generator. The shared helper is also consumed by the reserved Draw02, Draw03, Lift01–05 and PedestrianDraw01–02 network converters when those implementations are connected; this increment does not implement those bridges. Fixed generators do not call this converter.

Previously attempted affected-asset cleanup was rejected by automatic approval review. No alternate deletion route was attempted. Existing saved samples therefore do not acquire this new architecture automatically.

## Build and deployment

Release build passed. All 12 installed payload files match the build output. The installation backup contains 18 original files under `artifacts/backups/prefab-graph-compaction-20261017-015428/`. No runtime measurement or new human-created bridge is available after this deployment.

A follow-up read-only material check located the prototype yellow road stripe at UV `(0.1953125, 0.732421875)` in its existing main atlas. A visually pale candidate at `(0.95703125, 0.341796875)` instead belongs to control-house wall faces (including face 6999), not road paint. It was not substituted for white markings. Removing the remaining decal children requires verified compatible paint coverage/material data; reducing Prefab count does not justify an unverified wall-material replacement.
