# Native158 request-stage reuse and detached preview computation

This is an implementation and offline verification record, not a project contract or a game-frame acceptance result.

## Changed execution boundaries

A `MovableNetworkContext` owns source/piece/lane snapshots and `GenerationStageCache` entries. The LOD0 sampling compilation populates the same source stages later requested by LOD1/LOD2. Keys resolve actual render LODs; they do not use requested LOD number as a reason to repeat identical work. Actual render identity, contribution/occurrence order, coordinate origin and source-face options remain inputs. Lane projection includes the receiver identity when the deformation consumes receiver height. There is no cache across separate preview/formal requests or bridge owners.

The appearance path reuses equivalent receiver kinds, stable family groups, partition binding, domain offsets, selected-road bounds and complete atlas inputs. Complete input equality includes material identity, all patch values, supported domains, receiver roles, retained color regions and exact frame/vertex bits. Different appearance inputs remain separate. Deduplicated appearance capture callbacks retain every original LOD index. Native output ownership remains with the existing single model owner.

Worker jobs now perform lane frame decoding, deformation and triangle expansion; composition frame decoding/deformation/expansion; receiver role/coverage calculation; appearance classification/grouping/partition binding/chart projection/final UV output; and bounds reduction. Surface identities in chart outputs are opaque handles; native Surface/header/shader access is snapshotted on the main thread. Mesh stream access is also main-thread-only. Each source stream is copied once, then workers gather its channels and call stateless `NativeMath` conversion functions on pinned owned arrays. No Mesh, ECS state or native renderer is passed to those conversions.

CPU completion posts a ready continuation rather than mandating a next-frame poll. The existing aggregate main-thread budget still applies. Adjacent lane deformation and expansion, and appearance classification and projection, each run in a combined worker job. Stages requiring material/native decisions still return to the main thread; this is not a claim that arbitrary asynchronous chains bypass frames. GPU completion remains asynchronous with bounded next-frame polling. Cancellation never joins a running CPU worker; its input lease survives until the worker completes, and stale results are not published.

## Verification and measurement scope

- Main-tree build passed with the existing nullable warning area in SurfaceAtlas; material/resource guards passed (351 Surfaces, 103 packaged Texture files, 29 non-road descriptors with zero packaged texture edges).
- Scheduler tests exercise three identical in-flight LOD consumers executing once, distinct inputs, faults, clearing, context isolation, aggregate frame budget, cancellation and late-worker resource release. The independent Unity/Mono host passed 48 assertions plus actual GPU readback/cancellation.
- The actual mod assembly Compose entry was tested with three equal empty-geometry inputs: shared output, callbacks at original indices 0/1/2, and rejection of equality for a one-ULP frame change. This tests routing/ownership, not rendered material appearance.
- Packed normal/tangent decoding is compared byte-for-byte with the installed game's `MeshNormal.Unpack` and `MeshTangent.Unpack`, using its PipelinePlugin. The probe covers 1,025 vectors, interleaved offsets/strides, repeated worker execution and GC between calls. These are synthetic packed channels; the test does not claim a captured source mesh at this stage.
- The real four-rail BRR1 capture contains 380,576 input triangles and produces 382,732 output triangles. Direct framed bounds exactly match the previous transformed-triangle reduction. All six final native attribute streams and 88 material groups match the existing GPU-plus-CPU grouping reference. The unchanged production stream readback is 55,113,408 bytes; bound capture is diagnostic-only.
- The three-round pre-freeze replay reported new-path totals of 273.4665, 204.0834 and 188.4783 ms. The comparison path reported 1722.4277, 1665.3875 and 1647.4777 ms. This comparison is against the older GPU-plus-CPU grouping path, **not** native157 versus native158, and excludes source sampling, appearance preparation and asset saving. It therefore cannot establish this change's full-preview speedup.
- Tower tests and the 12-language/runtime UI checks passed. No user-facing text changed.

Native Unity/ECS construction, asset/header access, mesh getters, GPU API submission/readback and native resource publication remain on the main thread. Small orchestration and material/slot decisions surrounding those APIs remain there too. Their whole-game maximum step time is not measured by the isolated probe. Actual game request counts, missed reuse reasons and responsiveness require manual game generation; the agent did not start the game.

## Diagnostics and reproduction

`Selected-road request stages` reports executions, hits, stage wall time and key lookup time. `Selected-road preparation stages` reports its independent stage counts. `Selected-road appearance stages` reports distinct complete inputs and consumers. `Preview generation scheduling` reports active main-thread time, maximum step/pump time, worker execution and queue time, and separately observed ready/frame queue aggregate wait. Queue aggregates can overlap stage wall time and must not be added as disjoint totals. Elapsed-minus-active is not reported as a measured GPU or frame wait.

Relevant source-linked checks are `tools/GenerationWorkCheck` and `tools/MovableLaneDeformationCheck`; the actual-assembly replay is `tools/RoadGpu/RoadGpu158.Editor.cs`. The latter runs only in the retained isolated Unity test project with the game's managed assembly/plugin paths and the previously captured BRR1 file. It does not launch Cities2. Machine-local logs, candidate manifests and deployment receipt are kept under `work/preview158` and `artifacts/material-remap/shared/native158`; they are not runtime dependencies.

## Frozen candidate and local installation

The final postprocessed native158 candidate passed the actual-assembly replay again, including
Float32/Float16 unpacked channels and octahedral normal/tangent decode on up to two concurrent
workers, interleaved gathering, explicit GC, Compose callback mapping, exact real-input bounds
and all six native output streams. The final replay was a correctness/capture run, not a stable
full-preview performance measurement.

The game process was absent at installation. The installer verified all 964 manifest files,
replaced only DLL/PDB, backed up their two original installed files, and removed/added no files.
No saved bridge or player asset was changed. Candidate and installed DLL SHA256:
`D7FFCDB195E7E710B69B6838D54AE50933B47654909DDB748B8F93C39EF25E0C`.
The official SDK ZIP is 77,305,331 bytes; the existing 55 MB publication gate remains exceeded.
This was a local installation only; no publication or GitHub push occurred.
