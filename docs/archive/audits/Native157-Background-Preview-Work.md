# Native157 background preview work

## Changes and limits

The preceding in-game four-track preview recorded 412 steps, 20,029.628 ms elapsed, 7,652.431 ms active main-thread work and a 2,229.228 ms maximum supports/network step. These are baseline observations, not measurements of this update.

Ready continuations now share one approximately 6 ms budget per complete scheduler Tick. Pending GPU and worker polling resumes on a later Tick, so draining the ready queue cannot busy-poll a request. An indivisible native call can exceed the budget; this is cooperative scheduling, not preemption.

Detached structure plans/bounds, GPU partition input arrays, stable material grouping arrays and decal receiver intersections run on a worker. All ten movable generator support assembly routes now await their support graphs and decal publication. Native source reads, snapshots, Unity/ECS objects, material operations, GPU submission, native model publication, network prefab mutation and file operations remain on the game thread. Historical unused head-road helpers retain their synchronous implementation. Whole-mesh numerical parallelism is capped at two jobs. Formal creation retains synchronous compatibility over the same algorithms.

Worker execution suppresses the caller ExecutionContext. It receives managed arrays/numerical partition graphs, not permission to query live assets. Source mesh arrays already obtained by the snapshot are reused without another whole-array copy. Cancellation never joins a running worker. A separately retained parameter session and a context-free completion observer keep its inputs alive and release them even if the owning system never ticks again. Stale output is not published. Native cleanup remains on its established main-thread path.

## Verification

- Main-tree build passed, with the two existing nullable SurfaceAtlas warnings.
- All 43 source-linked Unity Mono scheduling checks passed. They cover aggregate frame budget, several ready continuations in one Tick, next-frame polling, worker thread/context isolation, success, fault and cancellation. Real 4 MiB GPU readback tests cover normal completion and cancellation followed by no further scheduler Tick.
- The actual four-track BRR1 capture has 380,576 input and 382,732 output triangles. All six native attribute streams and indices match the existing GPU computation plus CPU grouping reference byte for byte, including 88 canonical material groups in the controlled alias fixture. Invalid input remains a failure with no prepared result. Retained prepared-file snapshot lifetime also passed.
- Three isolated harness runs, including GPU preparation and native model output, took 276.848, 184.584 and 184.280 ms. The older GPU plus CPU grouping reference took 1,721.073, 1,653.121 and 1,645.063 ms; this reference is not native156, so it does not measure the incremental gain of this update. Native156 used the same GPU pipeline and had comparable warm total times.
- In those runs the GPU preparation phase used two background jobs (29.805–30.692 ms combined), main thread ID 1 and worker IDs 13/14. Its maximum main-thread step was 12.943–18.668 ms; native model publication maximum was 6.001–6.431 ms. Ordinary stream readback remains 55,113,408 bytes, with no full-bound diagnostic readback.
- Tower and twelve-language/UI checks passed. No user-visible text changed.

This harness does not contain a complete loaded game ECS world. Sampling, all native support reads/network publication and scene expansion have not been timed end-to-end here. The previous 2.229-second support step is split by the new async support routes, but its final in-game maximum and complete preview responsiveness require manual acceptance. No game was launched by the agent.

The scheduling log now includes frames, maximum whole-pump time, background job count/time and main/last worker thread IDs in addition to existing step/active/elapsed values. Background time is summed worker duration, not a claim of exclusive CPU usage. Existing stage wall clocks include waits.

## Reproduction and installation

Source-linked tests: `tools/GenerationWorkCheck`. Actual-DLL captured-input probe: `tools/RoadGpu/RoadGpu156.Editor.cs`, retained because the input and public test contract are unchanged. Evidence is under `work/preview157`; frozen build, official SDK package and installation receipt are under `artifacts/material-remap/shared/native157`. These are not runtime dependencies. Installation status and final hashes are recorded in the receipt after verification.

No saved bridge changes, publication or GitHub push are part of this update.

## Verified local deployment

The final postprocessed candidate also passed the actual-DLL prepared-snapshot test and complete 382,732-triangle bound capture, plus exact native channel/index comparison and malformed-input failure handling. Diagnostic capture is not enabled in normal generation.

After fresh process checks confirmed Cities2 was absent, deployment verified all 964 installed manifest files. Only the DLL and PDB changed; both original files were backed up. No other installed file or saved bridge changed. Installed and candidate DLL SHA-256:

`58619BF5D41BB0DB6E2247F6B7B14F9FF42F088610CE007954D1399C18135453`

The official SDK ZIP is 77,281,435 bytes. The existing 55 MB publication gate still fails; this is a local installation, not a publication. Receipt: `artifacts/material-remap/shared/native157/deployment-native157.json`.
