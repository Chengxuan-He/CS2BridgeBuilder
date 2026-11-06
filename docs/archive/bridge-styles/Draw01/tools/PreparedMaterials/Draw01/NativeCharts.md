# Draw01 native source-chart integration

## Implemented files

`src/BridgeBuilder/Bridges/Types/Draw01/Draw01UvCharts.cs` implements
`For(string renderName, int meshIndex, int domain)` for the existing
`MovableUvDomains.For(...).Items` indices. Its lazily loaded numerical resource is
`assets/HardcodedNumbers/Draw01/Draw01UvCharts.bin`. It calls the shared
`MovableUvChart.Read(I32, F64, offset)` interface. Repeated chart references share
one chart instance; repeated domain queries return the same reference array.

`BuildUvCharts.py` generates both files from the original native NPZ geometry and
the current embedded UV-domain resources. Each source chart follows exact common
edges in **both original position and original UV**. Atlas overlap, matching sampler
keys and unrelated triangles do not establish chart connectivity. Every domain
references only charts containing its actual original faces. Identical final chart
boundaries share their numerical representation without changing this association.

Polygon-union intersection coordinates remain Float64. If a rounded intersection
would exclude an original face, that face retains its exact triangle boundary;
the producer never expands a chart with a tolerance. These numerical triangle
boundaries are sampling metadata, not separate texture payloads.

The resource contains 25 render/mesh bindings, 22,939 domains and 1,092 distinct
referenced charts with 1,131 loops and 5,540 boundary points. The BBN1 resource is
288,788 bytes. All 22,587 domains with two-dimensional source coverage are mapped;
352 zero-UV-area domains have no two-dimensional chart. No texture or Surface was
created, baked, decoded, copied or replaced by this producer.

## Shared integration

Pass the **original parent domain index** to `Draw01UvCharts.For`. Constructed
fragment units have appended mapping IDs, so the caller must retain their parent
`di` instead of passing the fragment's `unit.Id` as a recipe index.

The chart describes continuous valid source coverage; it does not declare its
boundary repeatable. A receiving triangle may retain the original native Surface
only when the actual triangle is wholly within one of its associated charts and
the shared density/precision checks pass. This keeps the native Surface, texture
bindings, normal interpretation and complete mip graph unchanged. Do not normalize
UVs to the chart bounds or replace source coordinates with vertex-only modulo.

Draw01's source inventory and original dependency identities are already recorded
by `ExportSourceSampling.py` in
`artifacts/prepared-materials/Draw01/source-sharing/sampling-map.json`: 13 native
Surface identities, eight distinct descriptor contents and 12 unique built-in
texture resources. The primary source `97291ab7f7938fc35cc670b15f2bca36` retains its
original VT texture graph. Built-in direct references add no packaged pixel file.
Per-owner non-built-in copies remain the shared persistence layer's responsibility.

## Verification

`CheckUvCharts.py` verifies all 31,719 two-dimensional original faces are covered
by their own domain's charts. The remaining 352 recorded faces have zero UV area.
All 58,568 existing receiver observations matched original domain sampling data.
Because those fixtures omitted domain IDs, the replay conservatively requires
**every** possible matching original domain to cover each receiver triangle.
It also excludes `RepeatOnly` observations from direct-source acceptance.

This conservative replay finds 4,261 covered receiver observations, including
1,036 for which the old triangle/rectangle-only `CanUseSource` was false. It leaves
54,307 observations unresolved. The result is a containment check, not a substitute
for the shared Float32 metric checks or native-rendering acceptance.

The isolated `ChartCheck` project compiled with zero warnings and zero errors.
It tested the actual generated loader and production `HardcodedNumbers` reader,
using a stand-in for the parent's shared `Read` interface. All 22,939 domains and
22,608 chart references matched the source report. All boundary doubles retained
their exact bits, and 1,092 unique instances served the repeated references.
The stand-in does not certify the parent's polygon-containment implementation.

Evidence is saved outside the private environment in
`artifacts/prepared-materials/Draw01/native-domain-charts/`:
`domain-charts.json`, `receiver-check.json` and `loader-check.json`.

## Replay

The producer requires NumPy and Shapely. The current private environment has
Shapely under `artifacts/agent-material-env/Draw01/python-deps`; no environment
files are packaged with the mod.

```powershell
python tools/PreparedMaterials/Draw01/BuildUvCharts.py
python tools/PreparedMaterials/Draw01/CheckUvCharts.py
dotnet build tools/PreparedMaterials/Draw01/ChartCheck/ChartCheck.csproj -p:BaseOutputPath=C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/agent-material-env/Draw01/chart-check/bin/ -p:BaseIntermediateOutputPath=C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/agent-material-env/Draw01/chart-check/obj/
dotnet artifacts/agent-material-env/Draw01/chart-check/bin/Debug/net8.0/ChartCheck.dll artifacts/prepared-materials/Draw01/native-domain-charts/domain-charts.json artifacts/prepared-materials/Draw01/native-domain-charts/loader-check.json
```

## Remaining acceptance

The parent owns shared source-Surface selection, fragment-domain propagation,
native containment and density checks, and the remaining outside-chart cases.
Original-source mip and boundary sampling must be checked on the integrated native
path. Preview, creation, regeneration, export, cold loading and independent bridge
deletion/uninstall tests remain integration requirements. The complete official SDK
package must still be measured against the 55,000,000-byte gate.

This change does not claim complete Draw01 texture consolidation or a measured
package reduction. Existing Structure/Shared resources remain unchanged while
production still references them. No deployment, publication, environment cleanup
or game UI operation was performed. No UI strings changed; localization behavior
is preserved, and the new technical diagnostics remain unlocalized identifiers.
