# Lift01 original UV chart integration

`BuildUvCharts.py` produces `Lift01UvCharts.cs` and
`assets/HardcodedNumbers/Lift01/Lift01UvCharts.bin` from the real native mesh fixture
`artifacts/uv-density/layout-plans.bin` and every Lift01 `UvDomain_*.bin` resource.
The generator requires NumPy and Shapely 2.1.2. Shapely is an offline metadata tool;
it is not shipped or called by the mod.

The output covers 23 render/mesh streams and all 647,781 domain indexes. The domain
argument is the unchanged index in `MovableUvDomains.For(...).Items`. It interns
6,788 immutable chart shapes and 6,691 domain chart sets. The uncompressed BBN is
3,427,812 bytes, SHA256
`85dba5dda99b03283750d12d99d59fb6812144e702371193648c50827cab6086`.
This is one source resource measurement, not an official SDK package measurement.

## Provenance and topology

Faces join only through the same physical position and original Float32 UV edge.
Matching atlas coordinates on different physical parts do not join those parts.
Within each connected part, the generator computes the two-dimensional union of
original UV triangles, including overlaps, and proves coverage of every source
triangle. It retains original triangle fragments if that exact proof fails.
It never intersects or rewrites physical geometry. Domain references retain their
own source-face membership even when identical immutable loop shapes are shared.

The 204 domains containing only rank-one UV faces have empty chart sets: 172 in
`LiftBridge01_LOD1 Mesh/0` and 32 in `LiftBridge01PillarBase_LOD1 Mesh/0`. They are
explicitly unsupported by this two-dimensional coverage representation. A zero-area
source mapping must not be replaced with an invented rectangular footprint.

## Public interface

`Lift01UvCharts.For(string renderName, int meshIndex, int domain)` returns the
shared `MovableUvChart[]` for that domain. The nested holder loads only Lift01's
BBN on first use. Unknown render/mesh streams return an empty array.

Each chart uses `MovableUvChart.Read(int[] I32, double[] F64, int offset)`:
`I32[offset]` is the loop count, followed by `(F64 offset, point count)` for each
loop. F64 coordinates alternate x/y. Loops are implicitly closed, with no repeated
last point; exterior and hole coverage uses even-odd fill. The generated BBN also
contains stream/domain and immutable chart-set lookup tables, decoded by the
bridge-specific class before calling the common reader.

Full receiver containment is required before direct source-atlas sampling.
Chart shape does not imply repeatability. The original Lift01 textures use Clamp;
whole-image Repeat, modulo phases and silent UV clamping are not valid substitutes.
Network Surface template conversion is described in `NATIVE_OBJECT_DELIVERY.md`.

## Actual receiver evidence

`PrepareReceiverProbe.py` snapshots the real production geometry assembly and UV
mapping code into the private Lift01 test project. It exercises 24 source/plan
inputs across half-width deltas -1.25, -0.25, 0, 2, 8 and 24: 144 cases. It evaluates
generated fragments as well as original faces, before the old phase conditioning.
The independent conservative boundary/hole oracle accepts 87,290 of 270,779 changed
receivers. It uses a 1e-12 boundary epsilon; it is not the public native implementation
or a native rendering acceptance test.

Residual source containment failures include rank-one faces and Float32 UVs
interpolated for generated fragments slightly outside the original polygon. For
example, Net face 47 has an outside area about 5.4e-14, moving face 12285 about
9.5e-14, and NetPillarTop LOD2 face 57 about 4.85e-10. These are not evidence that
the original native triangles were absent from the extracted coverage. The shared
pipeline must handle original-versus-interpolated coordinates explicitly; this
bridge change neither expands the region nor clamps the UV to conceal the issue.

Wide receivers outside their actual source charts remain unsupported by direct
binding alone. Prepared Structure resources remain intact. Native material appearance,
normal orientation, mip seams, preview/create/regenerate/export, independent cold
loading and the complete 50 MB SDK package still require integration validation.

Evidence is saved outside the private environment at
`artifacts/prepared-materials/Lift01/native-charts/`: `report.json`, `charts.json`,
`receivers.json` and `delivery.json`. `SummarizeUvCharts.py` updates the delivery
summary from completed checks without rerunning geometry or baking textures.
No UI text changed, and the existing localized failure handling remains in use.
