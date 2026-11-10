# Generation-only road GPU construction

`RoadClip.compute` performs double-precision seam clipping and rigid bone assignment.
`RoadEncode.compute` consumes that GPU buffer directly and encodes native object attributes.
The runtime reads back complete bound geometry and encoded attributes; native publication
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
