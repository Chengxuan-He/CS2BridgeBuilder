# Native156 cooperative preview generation

## Scope and evidence

The preceding game log measured a single synchronous preview generation call at 5,269 ms: sampling 878 ms, mechanism 4,268 ms and supports/objects/network 123 ms. Rendering was already distributed over 11 frames. The implementation now yields the generation path itself rather than relying on a spinner to conceal that call.

All ten movable styles use asynchronous sampling/preparation and shared assembly. Native composition initialization, geometry, primary lanes, overlaps, signals and secondary lanes are separate steps. Road pieces and lane vertex/triangle blocks, appearance classification/output, per-LOD assembly, per-model publication, scene children, lane draws and renderer groups have explicit yield points. GPU clip/bind/encode computation is unchanged and GPU requests use asynchronous readbacks. Formal creation retains synchronous compatibility wrappers over the same operations; this change does not claim formal creation is asynchronous.

Request-local AsyncLocal scopes and a private main-thread SynchronizationContext prevent parameter and material contexts from leaking to unrelated updates. A prepared-file reference survives its IO callback. Changing selection cancels old work before another request is admitted. GPU callbacks own their resource lease independently and release it without requiring another update from a destroyed generation system. No Unity/ECS work is sent wholesale to Task.Run.

## Verification

- Main and frozen-source builds passed; existing two nullable warnings in SurfaceAtlas remain.
- Unity 2022.3 Mono: 26 scheduler/context checks passed, including nested awaits, outside-scope restoration, failure, cancellation, invalidated revision with no stale publication, old cancellation independent of the new request, and a worker-posted continuation returning through the explicit main-thread pump.
- Real 4 MiB GPU readbacks passed. Cancellation completed without waiting for GPU and no subsequent scheduler Tick was performed; the eventual native callback released buffers and did not publish cancelled data.
- Actual native DLL prepared-file test passed: callback disposal did not close a retained request; final request disposal closed the file lease exactly once.
- Actual captured four-track input: 380,576 input triangles; 382,732 output triangles; 88 canonical material groups in the controlled alias scenario. All six native attribute streams and indices matched the reference byte for byte. Explicit final bound capture matched as well. Malformed input returned failure with no prepared output.
- Three asynchronous runs including native model creation took 305.711, 183.303 and 185.325 ms. These are isolated Unity harness wall times, not full bridge previews. GPU construction used 35–37 pump steps; maximum active step was 20.486–23.706 ms. Model construction used 2–3 more steps, maximum 6.021–6.425 ms. Extra diagnostic full-bound capture was tested separately and is not the ordinary production path.
- Tower tests and all 12-language/UI checks passed. No user-visible wording changed.

## Limits and manual acceptance

The approximately 6 ms checkpoint budget is not preemption. A native API/job, resource import, individual mesh upload/bounds operation or support-object assembly can run longer. Full sampling, scene expansion and final rendering require a loaded game ECS world and were not end-to-end executed by the offline harness. In-game maximum-step logs and responsiveness must still be checked by the user; no game or launcher was started by the agent.

The new `Preview generation scheduling` log records request identity, step count, cancellation, elapsed wall time, active main-thread time, maximum active step and its stage. Older assembly stage clocks now include frame waits and must not be interpreted as pure CPU duration. Existing rendering/readback/encoding costs remain separately logged.

The production algorithm, textures, FP64 GPU operations, strict request-local LOD reuse and independently persisted bridge format remain unchanged. No saved bridge was modified, no publication or GitHub push was performed.

## Reproduction

The source-linked checks are in `tools/GenerationWorkCheck` and the native captured-input probe is `tools/RoadGpu/RoadGpu156.Editor.cs`. The latter accepts the same game-managed-directory, mod-assembly, replay and report arguments as the native155 probe. Run only in an isolated Unity test project, never by launching the game. Test data, timing logs, frozen candidate and deployment receipt are retained under the native156 task artifact directories; they are evidence, not runtime dependencies.

## Local installation

Native156 was installed after confirming the game was absent. The deployment verified all 964 manifest files; only `BridgeBuilder.dll` and `BridgeBuilder.pdb` changed, with original-only backups. Installed and staged DLL SHA-256:

`87506F724DBDDC68C393851C20821495039C0552487160BCE8448464B20B200D`

The postprocessed candidate also passed the actual captured-input and prepared-snapshot tests. The official SDK local ZIP is 77,242,551 bytes. There was no publication. Candidate manifest, original-file backup and installation receipt are under `artifacts/material-remap/shared/native156/`. `git diff --check` passed. No game, launcher or player bridge asset was modified or started for verification.
