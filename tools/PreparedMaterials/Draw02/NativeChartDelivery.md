# Draw02 native UV chart delivery

## Implemented source data and interface

`Draw02UvCharts.For(renderName, meshIndex, domain)` implements the agreed public
interface. `domain` is the original index in `MovableUvDomains.Items`. The method
returns only charts reached by that domain's original faces. Unknown render/mesh
keys return an empty array; an invalid domain for a known mesh is an internal error.
`Draw02_NativeUvCharts.bin` contains checksum-protected BBN1 I32/F64 arrays, loaded
only when Draw02 is requested. Lists and identical chart boundaries are shared.

The source extractor reads the actual native NPZ meshes and exact existing domain
resources. It joins faces only across a unique, oppositely oriented physical edge
with identical endpoint UVs and a matching physical plane. Atlas-coordinate matches
alone never merge separate parts. Polygon unions retain holes. Where numerical
union intersections fail to contain an exact authored face, its original triangle
is retained as an additional chart; boundaries are never expanded by an epsilon.
No rectangle implies repetition, and the extractor creates no new material or pixel
payload. Compatible source Surface and complete native VT/mip references remain the
shared resource representation.

## Evidence

The implementation contains 45 mesh keys, 38,210 domain mappings and 3,166 shared
charts in 414,460 bytes. The binary SHA-256 is
`2f366835f21b6f9438fbd7c0deef44df614033e5c585553316472210daa7d96a`.

- All 51,794 nondegenerate original face references are fully contained by their
  corresponding chart. The 230 original UV-degenerate faces have no area chart.
- All 38,038 domain-to-chart references decode exactly through the I32/F64 schema.
- The C# schema check compiles the actual generated class and production BBN reader;
  all 38,210 domain calls pass, preserve exact F64 coordinates, and reuse loaded arrays.
  Its `MovableUvChart` reader is an API/schema stand-in, not native rendering evidence.
- Actual generated plans for 45 meshes at width deltas 0, 8 and 24 retain their UV
  density checks. Among 15,020 changed triangles at deltas 8 and 24, 182 complete
  Float32 atlas triangles fit inside a corresponding real chart. Coverage tests use
  complete polygons including their interiors and edges, not just corners or bounds.
  This is a measured subset, not a claim that all Draw02 repeated surfaces now work.

Retained evidence is under
`artifacts/prepared-materials/Draw02/shared-source-migration/`:
`native-charts.json`, `native-chart-coverage.json`, `chart-plans.json`,
`chart-plans.json.seams.json`, and `chart-plans.log`.

## Reproduction

The extractor and coverage check require NumPy and Shapely 2.2.0. This task's
Shapely installation is isolated in the Draw02 agent environment.

```powershell
python tools/PreparedMaterials/Draw02/ExportNativeCharts.py . --python-lib artifacts/agent-material-env/Draw02/python
python tools/PreparedMaterials/Draw02/CheckNativeCharts.py . artifacts/agent-material-env/Draw02/python
dotnet run --project tools/PreparedMaterials/Draw02/NativeChartSchemaCheck -c Release -- artifacts/prepared-materials/Draw02/shared-source-migration/native-charts.json
```

`SeamPlanCheck` is a Draw02-only copy of the production plan audit with actual domain
indices and receiver coordinates exported. Its optional finite seam implementation
remains authoring evidence only. Blindly splitting the legacy triangular repetition
would create 108,516 triangles from 7,421 changed inputs at delta 8, and 267,826 from
7,599 at delta 24, with up to 800 pieces per face. It is not enabled or proposed as
a full-bridge fallback. The public chart interface supersedes that approach.

## Remaining integration and acceptance

The main agent owns `MovableUvChart.Read`, public style dispatch, complete-triangle
containment, and the production native material path. Receivers outside every real
chart remain unresolved; they must not be forced into an original atlas or silently
replaced by per-sampler baking. Native filtered boundaries, complete mip appearance,
normal interpretation, saved/cold-loaded graphs and in-game acceptance remain to be
verified by that integrated path. No old production resource has been deleted,
no texture has been rebaked, and no deployment has occurred.

There is no UI or localized string change. Diagnostics are developer-only.
