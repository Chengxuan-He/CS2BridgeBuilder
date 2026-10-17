# Movable Road Mesh Repair and Generation Performance

Current appearance selection: [Existing Material Reuse](Movable-Road-Material-Reuse.md). Earlier
baking/descriptor-generation descriptions below are historical and superseded by this revision.

Date: 2026-10-17

## Scope and acceptance status

This increment fixes a demonstrated render-batch registration defect and removes selected-road prefab mapping from the active Draw01 motion-binding path. Other movable generators remain outside this increment.

The Release build and deployment succeeded; all 12 installed payload files match their build outputs. These checks establish deployment, not visual acceptance. A newly generated bridge must still be checked in-game at near and far distances, closed and open, including both sidewalks, median, markings and attachments. Existing saved geometry is not rewritten by replacing the mod DLL.

## Confirmed missing-surface defect

The actual generated sample is `baee1331d-d46e-469a-be7e-6f6361bb2f88`, created from `Large Road Divided` with `Draw01`. Its highest-detail mechanism geometry SHA-256 is `92506B809E8EBF8EEB1467E009F45E51C50D96368688CB6D4AEB13EDB3E6FC35`.

The generated road contains 2,646 vertices and 882 triangles. Sampled cross-sections at Z = 10, 20, 30, 40, 50, 60 and 70 metres cover X = -13.5 to +13.5 metres without gaps larger than 0.01 metres. Actual geometry inspection found no cross-bone triangles in any of the three saved mechanism LODs. This alone does not establish correct rendering.

The engine log supplies the decisive additional evidence:

| Render level | Registered mesh count | Material count |
| --- | ---: | ---: |
| Highest detail | 3 | 6 |
| LOD1 | 3 | 6 |
| LOD2 | 2 | 5 |

`SceneFlow.log`, 2026-10-17 00:35:38.682–684, reports these exact mismatches. Native `Game.Prefabs.MeshSystem` copies `RenderPrefab.meshCount` into `MeshData.m_SubMeshCount`. `Game.Rendering.RequiredBatchesSystem` consumes that count when requesting draw batches. Although `RenderPrefab.ObtainMesh` can address submeshes inside a mesh, that lookup does not compensate for missing registered draw batches.

The generator appended one road model containing four material submeshes, then recorded the number of model containers as `meshCount`. Consequently, only the initial material batches were registered; later road groups disappeared in-game even though their triangles existed in the geometry file.

The correction emits one generated model per used material slot, matching the native importer layout. Each model has local material index zero and a matching surface reference. Empty slots are omitted. The same construction handles road surfaces and lane geometry at every LOD. Existing structure materials, triangle positions, normals, UVs and bone assignments are not altered by this layout correction. No shader or engine code is replaced.

## Motion binding without section prefab tables

The current path does not consult `MovableNativeRoadRoles` to assign moving road faces, lane geometry or attachments. The former mandatory geometry-CID, source-face and attachment-producer lookups have been removed from this generator.

Instead, evaluated closed-road geometry is transformed into mechanism space and clipped against a complete physical motion-domain tree derived from the prototype:

- Longitudinal seams retain the measured fixed sections, leaf sections and expansion gaps.
- The prototype's inner curb boundary, X = +/-3.4000244 metres, moves laterally with the corresponding native structure.
- Geometry crossing a domain boundary is clipped and its attributes interpolated; the entire source prefab is not assigned by name or centroid.
- Each emitted triangle belongs to exactly one native bone. The same domain tree applies to every generated road LOD and to lane geometry.
- Attachments use the same spatial domain. The separately approved native lamp longitudinal positions remain unchanged.

A median or another selected-road piece need not exist in the prototype or a prefab whitelist to obtain a motion assignment. Its sampled geometry is handled by the same clipping path. This removes the earlier `Median Piece 2` mapping prerequisite. It does not claim that every possible third-party shader or malformed external prefab has been validated; native sampling and material representation requirements still apply.

Source-face tracing is now optional in the shared sampling API and disabled for this generator because motion binding no longer consumes it. Existing callers retain the previous default. Source occurrence identities and ordinary native sampling remain intact.

This supersedes earlier Draw01 implementation notes that required `ForRoad`, `ForLanes` or attachment role-table coverage. Offline prototype structure classification and widening data remain required and are separate from selected-road section mapping.

## Measured performance

The timings below come from the actual six-lane divided-road generation log, before this increment. They are stage timings, not an end-to-end generation benchmark.

| Stage | Measured time | Interpretation |
| --- | ---: | --- |
| Road appearance evaluation, readback and persistence | 18.945 seconds | Four atlas pages; zero reused material or texture sets |
| Native texture upload, mip generation and save | 17.507 seconds | Included in the preceding row, approximately 92% of that stage |
| Native marking conversion | 0.101 seconds | 14 batches, 42 objects, zero baked marking textures |
| External dependency snapshot | 0.082 seconds | Not the dominant measured cost |

The appearance stage wrote 13 texture files totalling 71,957,083 bytes. The two lower structural LODs already reuse the same road appearance result. Adding their reported reuse entries to the baking time would double-count work.

Ordinary bridge construction can retain native road/network materials and mainly transform structural geometry. This movable path additionally samples the closed road, creates object geometry, assigns bones, converts network appearance into object-compatible surfaces, uploads generated textures, builds mip levels and persists those assets. The measured texture path dominates the current difference. This increment does not claim to have eliminated appearance baking or achieved the user's requested complete material reuse or native-comparable dependency size.

## Parallelism and optimization priorities

| Work | Parallelism assessment |
| --- | --- |
| Triangle clipping and packing | Independent chunks can use immutable input, local output buffers and a deterministic merge; avoid shared append lists. Measure this stage before adding scheduling overhead. |
| Bulk vertex transforms and pixel evaluation | Use vendor-neutral GPU dispatch, not CUDA. Existing pixel evaluation already uses the GPU; moving arithmetic to the GPU does not remove subsequent native persistence costs. |
| Atlas evaluation and readback | Existing code overlaps up to two pages. It already overlaps some GPU work with persistence. |
| Native texture upload, mip generation and asset database save | Respect Unity graphics-context and asset database thread requirements. Do not wrap these calls in `Parallel.For` without a supported thread-safe API. |
| Road sampling and prefab publication | EntityManager, Unity objects and native generation calls remain on their required execution thread. |
| Repeated source-face matching | Removed from this generator because its result is no longer required. Runtime speed improvement remains unmeasured until a new sample is generated. |

The highest-impact remaining change is to avoid the atlas upload/save path by using compatible existing object materials and correct texture coordinates. That requires an explicit appearance choice between the selected road and the prototype bridge; changing a network shader reference alone is not equivalent. Geometry parallelism can help a measured geometry bottleneck, but cannot eliminate the currently dominant 17.507-second texture stage.

## Delivery record

Bridge-specific source commit: `bridge/draw-01` / `6a22779`.

Shared changes: material-separated road/lane models in `MovableRoadAssets`; optional source-face tracing in `MovableNetworkContext.Surface`.

Installation backup: `artifacts/backups/movable-material-mesh-domains-20261017-004952`, containing the 18 original installation files only. Twelve deployed payload files were verified by hash.

Existing generated bridges were retained. A previous ownership-scoped deletion operation was rejected by automatic approval review; this increment does not bypass that rejection. Recreating the affected test bridge is necessary to exercise the new geometry layout. No visual acceptance or post-change performance measurement is claimed.
