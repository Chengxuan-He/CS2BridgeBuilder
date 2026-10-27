# Lift04 native UV chart data

## Source and ownership

`tools/GeometryMetaprogram/Lift04NativeUvCharts.py` extracts actual UV edge
connectivity from all nine measured native render LODs (eleven mesh streams).
It writes `native-charts.json` and a standard BBN1 `native-charts.bin` candidate
under `artifacts/prepared-materials/Lift04/shared-sampling/`. Neither file contains
pixels. Original Surface and texture bindings are in `native-bindings.json`.
The candidate remains outside the production embedded-resource set until the
coordinating agent connects and validates its common native sampling consumer.

The source maps contain 63,303 original faces, 4,164 chart instances, 1,853 proven
rectangular charts, and 364 degenerate UV faces. All 525 previous sampler keys
map to actual source triangles and charts. Across all their source occurrences,
284 keys have at least one rectangular-chart candidate; 241 have only nonrectangular
candidates. Five previously triangular samplers have a real rectangular-chart
candidate. These are candidate counts, not permission to select a different part's
chart. Use the exact render name, mesh index and original face membership.

## Binary layout

The standard BBN1 header, typed-array counts and SHA-256 footer follow the existing
hardcoded-number format. Only I32 and F32 arrays are populated. I32 begins with
schema `-41` and mesh count. Each mesh then contains:

1. UV point count, original face count and chart count.
2. One chart index per original face; `-1` denotes a separately retained degenerate
   UV face. This is measured membership, never runtime part classification.
3. For each chart: exact-rectangle flag, single-boundary-loop flag, member-face
   count, unique source-triangle count and boundary-edge count; then original face
   indices, three UV point indices per source triangle, and two UV point indices
   per directed boundary edge.

F32 contains each mesh's exact source UV point pairs in mesh order. Source vertices
were native Float16/Float32 values; serialization retains their binary32 values.
No quantization, image conversion, triangle generation or source-geometry change
is performed. The JSON file gives the matching ordered render names, mesh indices,
source Surface CIDs, source NPZ hashes and original-sampler aliases.

## Native consumer requirements

`Lift04UvChartBindings.py` emits the production `Lift04UvCharts.cs` adapter and
`assets/HardcodedNumbers/Lift04/Lift04UvCharts.bin`. `For(renderName, meshIndex,
domain)` uses the exact index in `MovableUvDomains.For(...).Items`. The bridge
resource loads on first valid bridge lookup; each domain stores references to
shared `MovableUvChart` instances. No source matching occurs at runtime.

The adapter resource is 303,784 bytes: 915 reliable single-loop chart instances,
6,408 accepted domain bindings and 42,596 conservative empty bindings. Global UV
nonmanifold edges, self intersections, inconsistent enclosed area and unreliable
loops return no chart. The shared source-level implementation must handle those
receivers explicitly; removed prepared libraries are not a fallback. These counts are
binding availability, not claims that all resized receiver triangles fit a chart.

Its I32 schema is `1, chartCount, chartOffsets..., meshCount`, followed by each
mesh's domain count and each domain's reference count/chart indices. Each chart
record is `loopCount` followed by `(F64 offset, pointCount)` per loop. F64 stores
alternating exact x/y coordinates with an implicit closing edge. This adapter
currently emits only reliable single loops; the common reader may support holes
by its even-odd rule. The earlier `-41` binary remains an offline inventory only.

The common consumer needs the exact render/mesh/face chart lookup and original
native Surface binding. Preserve source texels, the original full mip chain and
the existing physical geometry/bones. For a density-preserving mapping that remains
inside a recorded chart, source UVs may directly address that original material.
The current integration accepts only whole receiver triangles contained by the
real chart. It does not infer repetition or split geometry. Do not tile an
arbitrary chart bounding box or reuse the generated triangle fold as the reference
appearance.

Nonrectangular boundaries, overlapping/ambiguous chart candidates, singular UVs,
source filtering at region boundaries and any large split count require explicit
handling by the common implementation. This data neither supplies a custom shader
nor proves those cases supported. No per-sampler texture fallback is introduced.
Native full-mip, normal, preview/persistence and independent cold-load verification
remain required after integration. No UI or localization behavior is changed.
