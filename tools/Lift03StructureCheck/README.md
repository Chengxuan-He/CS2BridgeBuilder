# Lift03 structural composition check

Run from the repository root with the reviewed research directory:

```text
python tools/Lift03StructureCheck/ExportInputs.py RESEARCH_DIRECTORY
dotnet run --project tools/Lift03StructureCheck -c Release -- RESEARCH_DIRECTORY
```

The exporter requires NumPy, `movable-skin/LiftBridge03 Mesh.Geometry`,
`movable-skin/LiftBridge03_LOD1 Mesh.Geometry`, and the corresponding
`lift03-main-{0,1}-0.npz` native-decoding caches. Source hashes are checked.
The caches must come from the reviewed native decoder; verifying a source hash
does not independently prove the cache was decoded correctly.

The harness links production geometry classes and extracts the current production
structural Plan implementation. It rejects a stale Plan snapshot. A lightweight
Vector3 substitute supplies arithmetic only; Unity rendering is not exercised.

Fourteen upper/lower width pairs cover reversed width order, independent widening,
one narrowed deck, and signed centre crossings. Checks cover detailed fragment
positions, interpolation recipes, source immutability, unchanged indices, finite
positions, disjoint road removals and far-supplement face selection. The 2,962
far-base vertex expectations use measured component membership, independently
of the production base-mapping helper.

These checks do not establish visual correctness under extreme narrowing, native
material behavior, full far-shell clearance, selected-sidewalk compatibility,
resource persistence, or game acceptance. They do not modify installed assets.

Pass `--export-detailed` after the research directory to export the zero-delta
production structure as `lift03-result-{0,1}.bin`. Each file begins with two
little-endian int32 values (vertex count, triangle count), followed by float32 XYZ
vertices and int32 `(A, B, C, originalSourceFace)` records. These diagnostic meshes
include retained source triangles and replacement fragments, excluding removed
faces. They contain no material or skinning data and are not importable game assets.

The independent catenary fixtures cover 13,832 / 4,921 native source faces.
Every tested detailed composition must remove all of them and must not restore
them through structural fragments. These checks replace the obsolete ten-foot
width test after the complete assemblies were identified as native catenary.
