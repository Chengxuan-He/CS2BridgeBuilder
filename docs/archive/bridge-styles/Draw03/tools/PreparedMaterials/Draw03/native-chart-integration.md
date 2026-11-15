# Draw03 native source chart integration

`Draw03UvCharts.For(renderName, meshIndex, domain)` is the production bridge-specific
entry point. `domain` is the unchanged index in `MovableUvDomains.Items`; it is not a
triangle number or the old prepared texture key. Each returned chart is shared by
all recipe domains that use it. The arrays load only when that mesh is requested.

The common implementation supplies
`MovableUvChart.Read(int[] integers, double[] coordinates, int offset)`. At `offset`
is the loop count, followed by `(coordinateOffset, pointCount)` for each loop.
Coordinates are interleaved X/Y doubles, implicitly closed without a repeated final
point. The native Float32 coordinates are promoted exactly. All loops use even/odd
containment. Two-point loops retain authored rank-one UV intervals; they must not be
treated as missing or constant UVs.

## Construction

`PrepareNativeCharts.py` extracts source mesh chart membership and physical boundary
edges. A join requires matching position and UV at both ends of a reversed edge,
within the same original index-connected component. It cannot merge touching but
disconnected parts, unrelated atlas islands or different material streams.
`GenerateUvCharts.py --publish` selects only charts referenced by the existing recipe
faces, writes `assets/HardcodedNumbers/Draw03/NativeUvCharts_*.bin`, and generates the
production `Draw03UvCharts.cs`. It leaves the old UV-domain arrays and material assets
unchanged. Exact collinear intermediate boundary coordinates are omitted; source
shape and phase do not change. Source frames remain distinct from source charts.

There are 35 mesh streams, 224,342 existing recipe domains and 76,531 referenced
chart instances. Their BBN1 arrays total 9,024,536 bytes before whole-package SDK
compression. This is an array measurement, not the mod package size. Original
Surface identities and their full native Texture/VT/MidMips graphs are retained;
there is no new texture payload, texture decode, resampling or encoding in this
bridge-specific change.

Generated literal metadata must pass the normal `HardcodedDataPack` step during
main-agent integration, like the existing generated catalogues. Do not replace
these arrays with runtime prototype mesh inspection.

## Common caller

The common retile path must request charts with the original recipe-domain index
before assigning constructed-fragment domains. The fragment still belongs to its
recorded source face's chart. Keep its physical density frame and exact original
source phase; a numerically conditioned repeat coordinate cannot be silently used
as an absolute original-atlas UV. Complete receiver-triangle containment, including
edges and holes, is necessary before using an original Surface directly. Point
containment at three corners alone is insufficient for a nonconvex chart.

Do not infer Repeat support from a rectangular shape. The standalone
`RectangleSeams.py` reference predates the finalized common interface and is not
called by the production catalogue. It is not a fallback for uncovered receivers.
No triangle-fold behavior has been added to native Surfaces.

## Measured receiver coverage

`ReceiverCheck` links the existing numerical projection code and invokes actual
Draw03 geometry plans through the current production DLL. The retained fixture is
`artifacts/uv-density/layout-plans.bin`. It exercises 35 inputs across width deltas
0, 2 and 8 with the symmetric default-sidewalk fixture: 105 scenarios, 716,037
density-checked triangles, zero density failures. It does not cover every selected
road layout or certify native rendering.

The changed receivers are retained in
`artifacts/prepared-materials/Draw03/native-charts/receivers.json.receivers.jsonl.gz`.
`CheckChartCoverage.py` tests those actual receiver coordinates against the same
recorded native loops, including edge crossings and wholly enclosed holes. With
an explicit 1e-12 UV arithmetic tolerance, 19,729 of 202,088 changed receivers fit
one complete native chart; 182,359 do not. These are receiver counts, not reusable
texture counts. The exact render/mesh/delta/domain/face breakdown and representative
UVs are in `receiver-coverage.json`. At delta zero, twelve constructed main-mesh
receivers also extend beyond their chart; their source phase/fragment construction
requires separate attention.

Consequently, direct-chart reuse alone is incomplete. Most stretched receivers
need an additional verified native source-region representation or bounded seam
handling. Widening the chart to an arbitrary bounding rectangle, dropping local
phase, sampling neighboring atlas parts, or restoring per-sampler texture baking
would not satisfy the material contract.

The main agent owns the common `MovableUvChart` reader, its caller and the remaining
sampling solution. Main assembly compilation, native filtered sampling at every
mip, generated native asset cold loading, independent persistence, and the official
all-style SDK package size remain integration acceptance items. No installed files,
formal material payloads, existing bridges or independent environment were deleted.
No user-facing text or localization keys changed.
