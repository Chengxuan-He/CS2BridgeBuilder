# Lift05 native UV chart integration

## Runtime binding

`src/BridgeBuilder/Bridges/Types/Lift05/Lift05UvCharts.cs` implements
`MovableUvChart[] For(string renderName, int meshIndex, int domain)`. The domain is
exactly the index in `MovableUvDomains.For(renderName, meshIndex).Items`.
`assets/HardcodedNumbers/Lift05/Lift05UvCharts.bin` is embedded by the existing
project resource glob. An explicit static holder loads it only on the first known
Lift05 chart request; domains share the same instantiated chart objects.
Unknown render/mesh inputs return an empty array. A known domain with no reliable
native rings also returns an empty array, preserving the common caller's existing
valid output. No texture, material, geometry or normal asset is created here.

`BuildUvCharts.py` generates both files from `artifacts/uv-density/layout-plans.bin`
and the existing Lift05 `UvDomain_*.bin` resources. Connectivity requires a shared
edge with exactly matching physical positions and UVs. Texture aliases at different
physical parts never connect those parts. Each domain references only charts that
contain its own recorded original faces. Non-manifold edges, ambiguous boundaries,
self-intersecting rings, failed ring validation and unavailable reliable loops have
no chart binding; no replacement triangle charts are fabricated.

The binding covers all 11 source render/material inputs and all 47,941 domain
indices. There are 15,860 retained charts with 16,088 loops; 473 unreliable native
parts are skipped. Empty bindings are an intentional capability boundary, not a
claim that the entire bridge now uses a single original-material representation.

## Binary and common interface

The BBN1 resource is 1,847,592 bytes, SHA-256
`1c8153f74bd67474f6184d1475444c764e8410857269729900ba85a0a747e845`.
Only I32 and F64 arrays are populated. I32 begins with schema 1, stream count,
chart count, stream offsets and chart offsets. A stream contains domain count and
one count-prefixed chart-reference list per original domain. A chart begins with
loop count, followed by `(F64 scalar offset, point count)` for each loop. F64 stores
interleaved x/y values; loops close implicitly and have no repeated final point.
The common `MovableUvChart.Read(I32, F64, offset)` applies the even/odd rule for
outer boundaries and holes.

The coordinating agent owns the common rendering entry. It may retain the original
Surface when the actual complete receiver lies inside a referenced reliable chart.
This binding does not infer Repeat, change source phase, cut geometry, clamp UVs or
substitute bounding boxes. Unsupported receivers remain explicit integration gaps;
the retired per-sampler material library must not be restored as a fallback.
The source Surface keeps its complete original Texture/VTTexture/VTSurface/MidMips
graph, original channel references and normal interpretation.

## Receiver verification

`ReceiverCheck/ReceiverCheck.csproj` is an isolated copy of the production-plan UV
check with Lift05-only receiver export. It invokes actual compiled geometry plans,
links current UV frames/parent projection, and records the original domain alongside
complete changed receiver triangles. It does not build or deploy the main mod.
`CheckReceiverCoverage.py` verifies the BBN data and measures complete triangle
containment using only those domain-specific chart references.

Evidence resides in `artifacts/prepared-materials/Lift05/domain-charts/`:
`report.json`, `charts.json`, `receivers.json`, `receivers.json.receivers.bin` and
`receiver-coverage.json`. All 66 cases passed density, footprint and native attribute
codec checks: 11 inputs at half-width deltas -1.25, -0.25, 0, 2, 8 and 24.
Among 23,795 changed receiver units, 5,566 are strictly covered by complete native
charts; 1,658 are additional original-material candidates beyond the old unit-domain
check. 14,558 leave their chart, 1,900 retain conditioned repeat phases, and 1,771
have no reliable chart. These cases must not be silently rebound to unrelated texels.
Strict geometric coverage is a capability measurement, not native filtered rendering
or full generated-bridge acceptance.

## Source evidence and outstanding acceptance

`ExportNativeBindings.py` and `Lift05NativeSourceCatalog.g.cs` retain seven original
built-in Surface identities. `ExtractNativeCharts.py` records material-wide UV
coverage for source evidence only; the runtime binding deliberately uses the stricter
physical-part/domain topology above. Source-wide union data must not replace runtime
part ownership. The standalone proposed identity adapter remains outside the runtime
source tree; simply rejecting old mappings is not an implementation of consolidation.

The source-level rebuild uses `PrepareNativeInputs.py` followed by the shared
`tools/Rebuild-NativeMaterials.ps1` entry. Its `sources` array contains only the
seven Lift05 prototype Surfaces; the original Paint evidence is recorded separately
as `paintSource` because the shared producer prepares its native VT descriptor.
Putting Paint in the reusable-original root list would incorrectly require its
absent original authoring Texture payloads.

The independently rebuilt output is retained under
`artifacts/prepared-materials/Lift05/rebuild-native-v1/output/Lift05`, with the input
manifest, native-reader log, producer report and `AuditNativeOutput.py` report beside
it. It contains 60 files totaling 15,078 bytes: 29 Surface descriptors, one shared
transparent-zero Texture and their CID files. All seven Lift05 prototype materials
remain direct built-in references. The common Cable, Road, Lanes and zero-control
payloads are byte-identical ownership copies from the fresh common source-level
output. No old structure/repeat baked outputs are reused and no image is re-encoded.

The installed game reader completely consumed all 29 descriptors, including 27 VT
descriptors. The independent audit confirms file hashes, no duplicate local payload
identity, no empty directories and zero unresolved active Surface references. The
Paint descriptor references the original full VT color payload rather than the
absent original plain Texture identity. These checks establish binary validity and
source-resource presence, not native filtered rendering acceptance. This source-level rebuild establishes native descriptors and source-chart evidence.
The structural semantic units and their separately recorded boundary observations
are described in `StructureUnits.md`.

The fixed-road repeat unit is integrated separately by `InstallRepeatUnit.py` and
`Lift05RepeatUnits.cs`; see `RepeatUnits.md` for the genuine-source measurements,
full VT/MidMips recovery, native tests and retained visual observations. Its road
Surface preserves template 14 double-sided alpha-test and Snow=0 semantics. The two measured structural units are integrated by `Lift05RepeatRegions.cs`;
`StructureUnits.md` records their full inventory, native validation and unresolved
classes. The combined formal style directory contains 84 files and 912,445 bytes;
no old per-triangle structure payloads have been restored.

Actual common-interface compilation, filtered native mip/normal/boundary checks,
preview/save/regeneration/export, cold loading, independent deletion and mod-uninstall
checks remain coordinating integration work. Measure the complete official SDK
package against 50,000,000 bytes after validated resource retirement; the source
chart resource size is not a complete package-size result.

No user-visible mod strings or localization behavior change.
