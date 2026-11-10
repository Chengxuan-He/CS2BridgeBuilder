# Generation-only road GPU construction

`RoadClip.compute` performs double-precision seam clipping and rigid bone assignment.
`RoadEncode.compute` first evaluates material coordinate mappings and the measured pose on resident input, then consumes bound geometry directly to encode native object attributes. Quaternion evaluation preserves the native Mono expression order through double intermediates and float stores.
The normal runtime path keeps bound geometry on the GPU and reads native attribute streams plus group metadata; explicit replay diagnostics and reference verification may additionally read bound triangles. GPU validation, stable material grouping, bounds and UV2/UV4 selection precede publication. Transfer counts and bytes distinguish these paths. Native publication
and saved Geometry files have no compute-shader dependency. Request-local LOD reuse remains
outside this operation. CPU fallback is explicit and reconstructs the complete result.

Build the embedded `assets/roadgpu` with `BuildBundle.ps1`, using Unity 2022.3 LTS. The game
observed in this task is 2022.3.71f1; the available authoring editor is 2022.3.62f2.
The double kernel intentionally retains `enable_d3d11_debug_symbols`: optimized FXC failed
on the double polygon implementation. Do not remove it without recompiling and replaying
real captures. The runtime checks D3D11/SM5 and executes a double-arithmetic probe before use.
A resource fingerprint prevents source edits from silently leaving a stale embedded bundle.

Run the editor checks in an isolated project, never by starting the game:

- `RoadGpu136.Run`: `-gameManaged`, `-modAssembly`, `-replay` (BRR1), `-report`.
  It verifies the CPU capture oracle, actual production GPU output and all native packed channels.
  Timings include validation, transfer, synchronization, both readbacks and cleanup. Native
  Model allocation is separate. Save/load testing reports a host interop block explicitly
  if the same failure occurs with the unchanged CPU encoder control.
- `RoadGpu136Cases.Run`: seam/gap/empty inputs, UV2/UV4, occurrence and face precedence,
  complete CPU fallback, invalid source rejection, material aliases and ordered grouping.
- `Road134Check.Run` in `tools/RoadLodReplay`: request-local cache ownership and native publication.

BRR1's output hash describes the producing backend, not a promise that GPU-interpolated
normals/tangents/UVs equal a CPU replay bit for bit. These checks use the recorded native134
CPU captures. Position, provenance, material and bone equality are tested separately from
small accepted floating attribute/quantization differences. Never label a CPU fallback as
GPU acceptance. Offline checks do not establish game appearance or cold-load acceptance.

- `RoadGpu155.Run`: same BRR1 arguments; `-frame` tests GPU mapping/frame from original source records, `-shared` supplies controlled adjacent Surface aliases, and `-capture` verifies the optional full-bound diagnostic readback. It compares six native attribute byte streams, indices and bounds with the existing GPU/CPU publication path. Timings exclude asset save and source-material creation; Surface aliases are test identities, not captured asset identities.
- `RoadGpu155Cases.Run`: adds nonidentity pose and planar mapping, resident output/capture checks and compact UV dimension tests to the seam, empty, gap, invalid data and CPU fallback cases.
