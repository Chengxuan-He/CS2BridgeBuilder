# Movable production-plan UV audit

This .NET 8 tool loads the built mod and invokes its actual geometry plans using
offline source fixtures. It links the production UV frame, catalogue, footprint
and page-packing implementations. It does not start the game or prove visual
acceptance, native asset registration or serialized cold loading.

The report distinguishes geometry metric failures, final-map round-trip failures
and insufficient pixel-footprint failures. Any of those failures gives a nonzero
exit code after writing the report. Physically rejected negative-width plans are
reported separately. The inventory includes synthetic selected-sidewalk layouts
and independently sized Lift03 decks; it is not an exhaustive selected-road list.

Run from the repository root, supplying the installed game's managed directory:

```powershell
dotnet run --project tools/MovablePlanUvCheck -c Release "-p:GameManagedDir=<game>/Cities2_Data/Managed" -- src/BridgeBuilder/bin/Release/BridgeBuilder.dll "<game>/Cities2_Data/Managed" artifacts/uv-density/layout-plans.bin artifacts/uv-density/production-packing-plans.json
```

An optional exact render name after the report path limits diagnosis to that
render's delta-8 symmetric cases. An additional numeric argument selects another
delta. Filtered reports record the evaluated input count and filter, and emit
per-mesh cell diagnostics beside the report.

`Test-GpuPacking.ps1` runs `tools/GpuBake/PackingCheck.Editor.cs` in an existing
Unity GPU test project. It copies the production packing code and compute shader,
checks independent repeating pages and validates sampled colors and normals at two
source resolutions. Expanding receiver bounds must not change page dimensions.
It does not rebuild or install the runtime GPU bundle.
Exact discontinuity probes are explicitly excluded from the CPU/GPU sample
comparison; seam appearance, mip filtering and native persisted outputs still
require separate validation.

```powershell
./tools/MovablePlanUvCheck/Test-GpuPacking.ps1 -ProjectPath <Unity-test-project> -LogPath artifacts/uv-packing-production-gpu.log
```

All diagnostics are developer-only. This tool adds no runtime UI text.

The `.samplers.json` companion records only changed charts that actually require conversion
after testing all chart corners for original-material reuse and Float32 gradient preservation.
Constructed fragments use their immutable parent sampler through the linked production
`MovableParentUv` implementation. Their physical charts remain separate, but the sampler key
contains only the original source domain, never interpolated fragment corners or road dimensions.
The emitted `fragment` flag is false for these immutable samplers; legacy inventories with
layout-dependent fragment entries must not be treated as an exhaustive constant catalogue.

`--probe-parent-samplers` additionally measures the conditioned repeat representation before
original-material selection. `parentTested`, `parentFailed` and `parentWorst` report its density;
`parentPhaseTested`, `parentPhaseFailed` and `parentPhaseWorst` compare continuous source phase
at vertices and interior barycentric samples on a synthetic 4096-pixel source grid. Integer
repeat offsets are restored for that comparison; per-vertex modulo would fail the interior probes.
Parent UVs also pass through the native attribute codecs. The primary production checks separately
cover the selected original/repeat path. Inspect both primary and parent counters: the process
exit code describes the primary checks, not the additional parent counters. Actual source sizes,
GPU seams, native material registration and cold loading still need their corresponding checks.

`--road-partition` checks the production nonperiodic chart partitioner without
loading a game world or geometry fixture. It validates face ownership, image
capacity, retained density and source-phase round trips, including an oversized
original face which must still fail instead of being shrunk or cut.

```powershell
dotnet run --project tools/MovablePlanUvCheck -c Release "-p:GameManagedDir=<game>/Cities2_Data/Managed" -- --road-partition
./tools/MovablePlanUvCheck/Test-GpuChannels.ps1 -ProjectPath <Unity-test-project> -LogPath artifacts/uv-atlas-channel-final.log
./tools/MovablePlanUvCheck/Test-GpuVtSources.ps1 -ProjectPath <Unity-test-project> -FixturesPath artifacts/uv-vt-fixtures -LogPath artifacts/uv-vt-source-final.log
```

The channel test uses the production BC7/mip/readback code and serialized Texture
payload reader. It checks mip-zero detail under quality limits 0 and 2, as well as
every lower mip of POT and rectangular NPOT pages. The VT test reads original
`.VTTexture` and `.VTSurface` fixtures and checks exact GPU pixel equality across
those quality settings. Its synthetic rectangular 4-by-2 tile fixture separately
checks tile order, 8-pixel border removal and interleaved owned/external layers.
The VT harness currently references the native PipelinePlugin at the documented
local Steam installation path in `VtSourceCheck.Editor.cs`.

These tests change quality settings only in their disposable Unity editor process.
The runtime implementation neither changes game quality nor modifies shared assets.
Editor copies use `DestroyImmediate` for cleanup; sampling code is unchanged.
Neither test proves complete generated-bridge serialization or game acceptance.

## Native UV compression and road attribute precision

The full layout run also sends every final-map UV probe through the installed
PipelinePlugin's Meshopt vertex-attribute encoder, ZSTD level 3, and both decoders.
It compares the resulting bytes exactly. The JSON `nativeAttributeCodec` section
records the block, vector and byte counts. This tests the native attribute codecs,
not an AssetDatabase registration, complete Geometry file or cold load.

The focused road-encoder regression runs the production `MovableRoadUv.Encode`:

```powershell
dotnet run --project tools/MovablePlanUvCheck -c Release "-p:GameManagedDir=<game>/Cities2_Data/Managed" -- --road-uv "<game>/Cities2_Data/Managed"
```

It covers Float32x2 and Float32x4, nonzero Z or W on a single vertex, a UV triangle
that collapses under the former Float16 conversion, and finite values above the
Float16 range. Both layouts pass the native compression round trip. The generated
road models use the object rendering path. `GeometryAsset.CreateMeshes` retains
their attribute descriptors and bytes; the network-only `MeshUV0.Unpack` has a
different format restriction and is not evidence for Float32x4 object support.
