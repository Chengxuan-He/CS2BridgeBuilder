# Bridge preview optimization plan

## Priorities and measurement

Follow the [material generation contract](agent-contract/MATERIAL_GENERATION_CONTRACT.md).
First eliminate duplicate original materials and runtime work on constant derived appearances.
Use source-density repeat units rather than full-span baked images. Prepare reusable constant
resources offline and package them by style; generation copies their native bytes without
decoding, rebaking or recompressing. No generated-result cache is permitted.

The recorded Draw03 preview on 2026-10-21 took 36 ms for sampling, 798 ms for the mechanism,
462 ms for supports/objects/network, about 103 ms before rendering and 1,182 ms for rendering
(four samples, eleven frames). The 2.58 s sum is a subtotal, not click-to-image latency.
Mechanism snapshot/planning took 20/97 ms. A separate Draw01 sample took 4,238 ms for the
mechanism and 4,157 ms for appearance; its 4096x4096 page spent 1,365 ms saving textures and
2,305 ms in native VT creation. These are retained measurements, not current timing claims.
Nested stages must not be added twice.

## Execution and ownership

1. Snapshot native inputs on the main thread.
2. Run independent affine geometry plans and detached pixel-array processing in bounded CPU
   workers. Workers must not create Mesh, Texture, Material, ECS or native asset objects.
3. Join workers before native construction, saving or rendering. Preserve dependent arithmetic order.
4. Keep generated resources local to the current preview/bridge. Immutable measured source arrays,
   packaged prepared assets and per-call deduplication are distinct from caching generated results.
5. Preserve game API thread affinity, all LODs, source density, channels and independent persistence.

## Preview image processing

- Reuse two CPU readback Texture2D objects within the capture lifetime and release them on disposal,
  cancellation or failure. ReadPixels follows GPU submission on the main thread; no Apply upload is
  needed for CPU-only readback textures.
- Fuse coverage masking, visibility and premultiplied linear accumulation over independent rows.
  Preserve per-pixel addition order and all four samples. Native gamma conversion and PNG encoding
  stay on the main thread.
- Retain eight warmup renders and next-frame readback until measured native readiness permits a
  safe replacement. Do not reduce resolution, samples or streaming completeness for speed.
- Validate each road surface once per render graph, discarding the per-call validation set afterward.
  Record readback, accumulation, resolve, PNG encoding and complete render time separately.

PreviewPixelsCheck compares four 1536x768 samples with serial accumulation, including HDR,
nonfinite inputs and transparent masks. One local Release CPU run measured 75.1 ms serial and
28.0 ms parallel with exact float equality. This is not an in-game or end-to-end speedup claim.

## Remaining variable-material work

Only prototype-supported road appearance types enter composition/precomposition (offline material baking outside the game). Unsupported
road types and non-road structure retain original materials. Within that supported-type route,
only genuinely variable local content or the permitted unknown input can require runtime pixel
work. The following optimizations do not permit full-road rasterization, even across many pages:

1. Reuse unchanged native materials directly. Bind prepared constant resources without texture work.
2. Keep repeatable bases separate from variable content. Do not bake repetition across a bridge.
3. Bound necessary local content by its actual extent, never the whole road or receiver length.
   Cropping unused allocation is not a remedy for prohibited full-span baking. Preserve
   chart positions, padding, density and final UV normalization at every LOD. Preserve the native
   VT tile/mip constraints of the actual format; a page-size target cannot override source quality.
4. For newly produced temporary preview intermediates only, native TextureAsset.Save(compress:false)
   may omit secondary lossless file compression while retaining identical BC7 blocks and mip bytes.
   This must not rewrite prepared payloads. Formal generated assets retain native compression.
5. Keep VT registration, matching mid-mips and native resource completion intact before publication.
   Do not change global compression settings or move native database calls onto workers.

The inspected native no-compression branch writes a -1 marker followed by the same raw payload.
Cropping can be a no-op when content reaches page edges. Neither operation implies a universal
speedup, and temporary files can occupy more bytes before cleanup.

## Bounded material-channel pipeline

Where new variable content requires pixel processing, submit mip generation, BC7 encoding and
asynchronous readback for at most two channels. Drain/save the oldest while already-submitted GPU
work for the other channel progresses; refill only after disposal. Keep channel order, shader
kernels, per-channel dispatch order, color space, formats, sampler settings and mip dimensions.
All ComputeShader bindings, Unity calls and asset saves remain on the main thread. This is CPU/GPU
overlap, not concurrent native asset creation. Do not rerun it for offline-prepared constant assets.

Each pending channel owns its buffers, targets, requests and CPU texture. Finish submitted
readbacks before freeing their buffers on success or failure; clean partial submissions and drain
other pending work. Borrowed page targets remain page-owned. Native VT creation waits for all
required saved channels. Cap the queue at two channels, not two bridges or unbounded pages.

MovableAtlasChannel and MovableSurfaceAtlas implement the submission/drain lifecycle.
AtlasChannelCheck exercises queue bounds, mip order, wrap/color settings, partial failures and
abandoned-readback cleanup with test doubles. It does not execute shaders or prove visual parity.
Record queue peak, submission, readback/drain and texture-save durations, plus peak memory.

## Verification and delivery

Run relevant numerical, native resource, packaging and localization checks. Changes to code require
the project's coherent build/deployment workflow with original-file backups and installed hash
verification. Documentation-only changes do not. Preserve healthy saved bridges.

Use fresh game logs for cold/warm comparisons with identical styles, roads and output requirements.
Report source preparation separately from runtime cost and avoid counting nested stages twice.
Human checks cover all channels, LODs, motion, preview disposal, permanent creation and cold loading.
Offline tests and microbenchmarks do not establish game speed, visual equivalence or completion of
the remaining migration documented in [prepared materials](Prepared-Materials.md).
