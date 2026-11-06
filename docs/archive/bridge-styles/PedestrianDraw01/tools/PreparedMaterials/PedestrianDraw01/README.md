# PedestrianDraw01 source-level material preparation

## Current continuous mapping candidate

Production `PedestrianDraw01RoadPreparation` now binds three offline half-width
units through `PedestrianDraw01PreparedRoadMapping` for near/middle LODs. The
original x=0 and physical appearance boundaries remain; no longitudinal period
cuts are generated. Each unit uses finite U with the producer's reported content
guard and continuous repeat V. The measured native tangent is explicitly retained.
Far LOD keeps its eight original finite charts and original Surface.

The candidate is packaged for integration testing, not deployment or acceptance.
`continuous-v5` contains the stable producer manifest, native resources and full
99-row GPU filtering results. Maximum channel error is 0.136126 and maximum
normal difference is 16.716 degrees. A private pre-BC7 export still measures
0.135363 and 16.560 degrees, respectively: reconstruction/filtering is the main
unresolved issue, not simply BC7 compression. No tolerance was relaxed.

The isolated continuous consumer ran 54 cases through the actual common
`TryBind` and `Compose`: 1,368 triangles total, 22¨C24 per near/middle fixture,
30 per far fixture, no reversed or zero-area faces, preserved receiver attributes
and no extra composition cuts. The initial mapping was fixture-supplied. A subsequent 54-case replay against
DLL `3186193D1D06E8ABE8A712A4AF6E6A2635056701945F4875EE556C69B034EA39`
consumed the production mapping class, embedded BBN and installed asset keys.
It passed actual TryBind/Compose and native Unity mesh upload/readback; complete
Prepare sample extraction/type classification and the persisted geometry writer
remain outside that fixture. See `continuous-v5/production05.json`. Game-world type dispatch,
motion and independent saved-owner cold loading remain unverified.

The three-unit native graph is 9,180,961 bytes. That is not the complete generated
owner graph or official package measurement. The previous 56-cell native-atlas
implementation and its candidate-06/07 reports are retained as historical evidence
under `artifacts/material-remap/PedestrianDraw01`; they are no longer production
code because pure period cuts are forbidden by the current contract.

Fixed source UV has additional quantized non-affine interpolation: the existing
canonical-unit approximation has not eliminated the previously measured maximum
1.39453125 U and 0.34448242 V source-texel residual. Exact-gradient and half-value
feasibility audits are retained separately; they do not justify receiver cuts or
claim pixel equivalence. `AuditPathwayLayers.py` establishes Pathway support for
the exact measured input Surface, not arbitrary pathway materials.

## Fresh source input and current rebuild

Use `PrepareSourceInputs.py` to prepare a new private authoring directory directly
from the installed native game archives. It refuses an existing destination and
does not read the generated material library or invoke the old per-sampler baker:

```powershell
python tools/PreparedMaterials/PedestrianDraw01/PrepareSourceInputs.py . $GameData artifacts/agent-material-env/PedestrianDraw01/source-rebuild
```

The source graph has eight original Surface identities and ten physical pixel
resources: six VTTexture channels and four ordinary glass/interior Texture
resources. Each source is stored once in this private input graph with its exact
CID, full serialized bytes and checksum. Complete native mip chains, normal
encoding and native settings remain intact. The 42,310,735 input bytes are offline
source inputs, not mod payload. All these sources are built-in; compatible output
must retain their original references and distribute zero replacement pixels.

`source-inputs.json` separates those sources from 1,453 sampling records, the two
fixed-road region records and the native chart bindings. The source graph does
not establish native repeat capability. Fixed-road regional repeat/cutout and
structural receiver regions beyond verified charts require the common native
mapping implementation before output material descriptors can be finalized.
No descriptor conversion has been established as necessary merely from different
sampling coordinates. Do not rebake source pixels to create one variant per
sampling record, and do not restore the old large material library as fallback.

The source-input preparation creates `sources.json` for the shared
`tools/Rebuild-NativeMaterials.ps1` entry point. Its separate `native-output/`
contains 60 files totaling 15,078 bytes: 29 Surface descriptors, one shared Texture
and 30 CID sidecars. All eight original source Surfaces remain built-in references;
the output includes the shared source-level paint, road, lane and cable descriptors.
No texture bytes are re-encoded by this step. The copied shared texture retains
its existing identity and bytes.

The private Unity native-reader check consumed all 29 descriptors, including 27
VT materials. The dependency audit resolved all 30 local CIDs and 58 outgoing
built-in identities, with no missing dependencies or empty folders. Results are
in `source-rebuild/native-rebuild-report.json`, `native-descriptor-check.log` and
`native-output-closure.json` under the private environment. These checks validate
the new descriptor batch, not every deformed receiver or regional repeat mapping.
Full style acceptance, actual rendering and the complete SDK package gate remain
separate. The authoritative material directory is maintained by the coordinator's
shared task during the rebuild.

## Measured fixed-road recipes

`ExportRepeatUnitsV2.py` prepares the current source-level unit manifest and
source-face evidence under `artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2/`.
It measures exactly two authored 4 m by 4 m fixed road units: native mesh-zero
faces 1035/1036 and 13243/13244. Their 4096-square source pixel bounds are
`[1572,388.5,2340,1157]` and `[1572,388.75,2340,1157]`, respectively. Source density
is 192 pixels/m horizontally and 192.125 or 192.0625 pixels/m longitudinally.
The fractional boundaries and phase are native measurements, not rounding errors.

The source Surface uses three real VTTexture carriers with 13 authored mip levels,
three mid mips and 85 tile records each. Searching all 19 installed built-in Blob
archives and 519 Texture CID sidecars found no standalone payload for any of its
three original authoring Texture CIDs. `manifest.json` records the actual VT
carriers; it does not substitute an old generated crop or claim an ordinary
Texture exists. The source template is native VT template 1. The shared
`ExtractVtSources.py` adapter reconstructs all 13 authored BC7 mip levels without
pixel conversion. The three middle levels come from
`BridgesAndPorts/MidMips.cok`, entry `TS512_MB3.MidMips`; the first recovered middle
level matches the independently stored trilinear tile bytes. Fractional starts,
exact multi-period extents and compatible VT source descriptors are supported by
the new unit baker.

The `native-sources/` evidence folder retains the original Surface and three
whole VTTexture files with original CIDs outside the temporary agent environment.
These are offline inputs, not candidate mod payloads. `native-vt-layout.json`
records SHA-256 and exact byte ranges for each header, 1,392-byte native tail
block, tile table and all 85 compressed tile records. Their ranges cover the
complete VTTexture file. There is no combined VTSurface reference, but full mip
reconstruction additionally requires the indexed external MidMips carrier above.
Serialized completeness is distinguished from full-mip decoder verification and
actual original-atlas boundary comparisons.

The finite trial under `repeat-units-v2/trial-v1/` contains two prepared units:
768 by 1537 pixels with two longitudinal periods, and 768 by 3073 pixels with
four. They retain the measured 768.5/768.25-pixel spans without rounding. Their
six generated channels, shared zero texture, two descriptors and CID sidecars
total 18 files and 12,720,506 bytes. Combined with the validated native descriptor
batch, the style has 76 unique files and 12,735,530 uncompressed bytes. A local
style-only ZIP estimate is 12,752,535 bytes; this is not the official SDK package.
Whole source atlases are offline inputs and excluded from these material counts.

`RepeatAtlasCheck.Editor.cs` and its offline compute shader compare the actual
candidate against the original atlas, not against another repeated candidate.
Across three channels, two units and all 13 source mip levels, 2,034 of 7,488
samples differ by more than 2/255, including 1,778 edge-neighborhood samples.
The maximum linear channel errors are 0.055664 for color, 0.133333 for mask and
0.109331 for encoded normals. Packed-normal channel comparison does not establish
angular normal equivalence. Edge and far-mip differences remain, and the style
exceeds the 10 MB target. The candidate is diagnostic evidence, not an accepted
replacement or a release-ready material library. No repeated bake is needed until
the shared boundary/mip representation changes. Persistent outputs, extraction
evidence, logs, hashes and a reusable `persistent-manifest.json` are saved outside
the temporary agent environment.

`legacy-uses.json` maps all 1,453 old Repeat/Structure use records to their actual
fixed-road unit or source-chart purpose. The two moving leaves continue to use
the selected-road route and are not new prototype repeat assets. Structural
charts are not promoted to repeat units solely because their outline is a
rectangle. The already validated native descriptor library remains separate.

`ExportRecipes.py` extracts the existing four appearance regions from the actual
`PedestrianDraw01RoadAppearance.cs` expressions and checksum-validated BBN arrays.
It checks the source material against the complete native audit and installed
game/DLC archives. It does not read generated bridges or alter geometry.

The fixed positive and negative regions have different native half-precision V
minima. They must remain separate recipes. The two moving leaves have identical
source pixel recipes, but `PrototypeRoad=false` routes those moving surfaces to
selected-road appearance. Their prototype recipe is retained in `sourceUnits` as
source evidence and is excluded from the two `units` that require preparation.
Do not enable a new prototype-material path to make that unused recipe reachable.
The two fixed units need cutout, double-sided repeat behavior. Preserve authored
channels and their native mip behavior through the shared source-level native
mapping; the old repeated-pixel products are not the new deliverable.

The native source is `15c63410efe103a46b109b0dd6aa246d`, selected from material slot
zero of `PedestrianDrawBridge01 Mesh`. The exporter checks its actual descriptor
digest rather than trusting its name. Sources, numeric arrays, and runtime source
hashes are included in the inventory evidence.

## Authoring and verification

Run from the repository root with the real native audit and source inventory:

```powershell
python tools/PreparedMaterials/PedestrianDraw01/ExportRecipes.py . $GameData $NativeAudit artifacts/uv-density/structure-seam-sources.json artifacts/prepared-repeat/PedestrianDraw01/recipes.json
python tools/PreparedMaterials/PedestrianDraw01/VerifyRecipes.py . 'artifacts/uv-density/native/PedestrianDrawBridge01 Mesh.npz' artifacts/prepared-repeat/PedestrianDraw01/recipes.json artifacts/prepared-repeat/PedestrianDraw01/source-verification.json
```

`VerifyRecipes.py` compares all eight recorded native triangles, their exact UV
bounds, physical extents, alias reuse, stable recipe keys, and current shared
moving-surface routing. This is source evidence, not a rendered-output or
cold-load acceptance claim.

Use `tools/Prepare-Repeats.ps1` with a dedicated Unity project, output, log, report,
and writable product store. Supply other immutable product stores read-only.
Keep `Library` and `Temp` private to this style's authoring process. Native-reader
verification must pass before copying the prepared `Repeat/` descriptors and
`Shared/` dependencies into `assets/Materials/PedestrianDraw01/`.

The two packaged fixed units retain 768 x 772 color, normal and mask textures,
extracted from the native 4096 x 4096 channels. Their source periods remain
4 m by 4 m; BC7 alignment increases the vertical sample count slightly instead
of lowering density. The real prototype has no authored control channel. Its
injected black fallback was verified to sample transparent zero, so both
packaged descriptors reuse this style's existing 1 x 1 native transparent-zero
control CID `729893d593df5d52ba911bc1e7eab77b` through `Shared/`.

The two descriptors differ from their original prepared versions only in the
16-byte control reference. All other fields and patterned textures are unchanged.
Native readers verified both integrated descriptors, seven unique textures and
4,751,140 mip bytes uploaded unchanged. The style's 31 Surface dependency graphs
are complete; no Surface refers to the retired full-page zero control.
Machine evidence is saved in
`artifacts/prepared-materials/PedestrianDraw01/repeat-control-integration.json`,
`repeat-control-graph.json` and `repeat-integrated-native.log`.
The original bake inventory and payload manifest remain authoring evidence,
not the current post-integration payload manifest. These checks establish
prepared-file correctness, not in-game rendering or cold loading.

Runtime integration belongs to the shared material loader and atlas composer.
The generated bridge must take its own byte-identical snapshot of these packaged
resources. The current shared preparation distinguishes selected-road moving
surfaces from fixed prototype regions; packaging a repeat recipe does not change
that surface-selection policy or require otherwise unused materials to be loaded.

No user-visible strings or localization keys are introduced by these authoring
tools. Existing localized style names and generation messages remain unchanged.

## Shared source textures and independent sampling records

`ExportSharedSampling.py` reads the installed game/DLC resource archives and
checks the exact native Surface digests against the complete structural inventory.
It exports source-channel identity separately from each recorded sampling mapping:

```powershell
python tools/PreparedMaterials/PedestrianDraw01/ExportSharedSampling.py . $GameData artifacts/prepared-materials/PedestrianDraw01/shared-sampling/source-map.json
tools/PreparedMaterials/PedestrianDraw01/Test-SharedSampling.ps1
```

The current mapping covers 1,451 structural units and two reachable fixed road
repeat units. Four native Surface descriptors reference six channel sources:
three 4096 x 4096 channels and three 512 x 512 channels. Each resolution has a
color, normal and mask source. Two Surface pairs share their respective original
VTTexture CIDs while retaining distinct object-template/material semantics.
There are 1,189 triangle-reflection sampling records and 264 rectangular records.
Texture keys exclude sampling coordinates; sampling records keep their original
keys and exact regions. Source provenance keys alone do not establish equality
of distinct decoded channel payloads or their mip chains.

The current packaged style still has 4,248 Texture files (77,468,687 bytes) and
11,460 total material files (77,870,990 bytes). These measured files have not been
replaced: exporting a shared-source inventory is not runtime consolidation.

The private Unity GPU probe reads the real native source VT channels and checks
six representative coordinates for every structural sampler. It rejects a naive
whole-atlas affine-UV replacement: 14,031 channel samples differ by more than
1/255 across 12 channel/material combinations. This is a coordinate-only mip-zero
counterexample. It does not test higher filtered mips, normal rotation parity,
actual generated receiver reachability, or an implemented native regional sampler.
The probe shader is an offline instrument and must never ship in the mod.

The common implementation must preserve the original prototype's chart regions,
texel density, tangent-space normals and filtered mip behavior using supported
native rendering. Old per-triangle reflection is not an authored requirement.
Merely binding the original atlas with an unchecked affine map is insufficient.
Public sampler/loader/baker changes
belong to the coordinator; this style's inventory is ready for that interface.
Do not remove existing resources, alias unequal CIDs or report the 55 MB package
gate passed before the replacement path and full package have been verified.

`ExportSharedSampling.py` also produces `native-bindings.json`, an executable
authoring result covering all nine native mechanism/column/footing render inputs
and every listed LOD. Their eight original Surface references resolve to six
VTTexture and four ordinary Texture resources. The six VT resources retain native
10-level (512-square) or 13-level (4096-square) chains with three native mid mips;
the tool checks complete tile-table extents and retains full serialized hashes.
The four ordinary Texture resources retain complete native mip byte extents.
These built-in resources require zero distributed replacement Surface or pixel
files. Existing `PedestrianDraw01SourceRenders` already binds these original
Surface CIDs directly before the shared deformation/retile stage.

This full source graph is the native implementation input, not a new runtime
sampling capability. No style-ID branch or unused resolver was added merely to
claim a code change: the remaining conversion is in `MovableStructureRetile` and
`MovableStructureTiles`, which are shared coordinator-owned code. The original
prototype is the appearance reference; the old generated triangle fold is not
itself an acceptance requirement. Any replacement must nevertheless demonstrate
valid atlas regions and native texel density after the actual width deformation.
Until that shared representation exists, no safe replacement patch or old-resource
deletion is claimed. The six acceptance areas in material contract section 6
remain incomplete for generated output.

## Native chart data and bounded seams

`ExportNativeCharts.py` reads the real prototype mesh positions and UV streams,
then joins source faces only across edges with identical positions and UVs. This
preserves physical islands before merging their identical atlas-region records.
The measured source face, rather than the old baked sampler key, is the lookup:

```powershell
python tools/PreparedMaterials/PedestrianDraw01/ExportNativeCharts.py . artifacts/prepared-materials/PedestrianDraw01/shared-sampling/native-charts.json
```

Nine renders and eleven mesh streams contain 46,064 source faces. Their 9,897
physical chart occurrences share 3,303 region records, of which 549 have strictly
rectangular boundaries. All 1,453 old sampling records map to measured regions;
the records touch 611 unique regions, including 175 rectangles. A total of 409
records map to more than one region across the meshes/LODs. Selecting the first
region for such a key is incorrect. The consumer needs `(render, mesh, sourceFace)`
or the measured domain's face list. `faceCharts` and `domainCharts` provide both.
Some real charts combine up to 130 old triangular sampling records. Region count
does not imply a new texture per region: the shared source-channel files remain
the six original native texture references.

The exporter also writes a BBN1 `native-charts.bin` numeric candidate and a
`native-chart-index.json` name/CID index. Its I32 stream starts with version,
chart count and mesh count, then rectangle flag/boundary-edge count per chart,
then face count and chart index per source face per mesh. Its F32 stream stores
each chart's bounds and ordered edge endpoints. The binary has the standard
BBN1 SHA-256 trailer. Candidates remain outside packaged assets until the shared
reader and generated output are verified. No unused production reader is added.

For unchanged source faces, preserve native UVs directly. For deformed faces,
first use the true chart's native density and check the generated footprint
against that chart. A rectangular outline alone does not prove an image is
seamlessly periodic. Periodic reuse requires an authored or verified repeat
unit; atlas adjacency is not permission to sample the neighboring material.
Where repetition is valid, retain continuous density-corrected coordinates and
use the native repeating material without cutting geometry at period boundaries.
Do not use the tiny old triangle domain as a repeat period or tessellate every
prototype triangle into a dense sampling grid. Nonrectangular and ambiguous
regions retain explicit boundaries and require the coordinator's common mapping;
this table does not establish that they contain a valid repeat unit.

Before replacing production resources, the common implementation must report
generated face-count growth, atlas containment and density for representative
widths and all LODs, followed by native mip/normal/seam checks and the remaining
preview/save/export/cold-load/uninstall acceptance cases. This authoring change
does not alter source geometry, UVs, pixels, mips, packaged resources or UI text.

## Runtime chart adapter

`BuildUvCharts.py` turns the measured regions into production
`PedestrianDraw01UvCharts.For(renderName, meshIndex, domain)` and
`assets/HardcodedNumbers/PedestrianDraw01/UvCharts_PedestrianDraw01.bin`.
The domain argument is the unchanged index in `MovableUvDomains.Items`. Only
charts belonging to that domain's original source faces are returned; no lookup
crosses meshes or LODs by matching a sampling key.

The single 602,508-byte BBN1 resource contains I32 domain/chart indices and F64
loop coordinates. It loads lazily on the first lookup and creates 3,063 shared
`MovableUvChart` instances. Loops use the common `Read(I32, F64, offset)` layout:
loop count followed by coordinate offset/point count pairs, with implicit closing
edges. Outer loops and holes use the even-odd rule. Repeated final points are not
stored. The C# adapter contains only source names and loading logic, not numeric
geometry literals.

The builder rejects empty boundaries, nonmanifold boundary vertices, self or
cross-loop intersections and regions whose even-odd area differs from the native
triangle area. If any chart in a domain is unreliable, the entire domain returns
an empty array so the existing generation path remains available. Of 41,690
domains, 36,148 have reliable chart sets and 5,542 use this conservative fallback.
The builder verifies every serialized domain binding against its original face
membership and reads the complete BBN1 back through the existing numeric reader.
A separate check of the initial chart library confirmed centroid containment
for its 39,739 nondegenerate source faces with reliable boundaries. The current
recovery additionally verifies exact non-overlap and area for all 16 recovered
physical occurrences.

This adapter does not infer repeat units, cut geometry or rewrite UVs. The common
consumer may directly reference the original Surface only when the actual
generated footprint is fully contained at the required density. Integration
compilation and generated-output rendering remain coordinator-owned checks;
only the verified native descriptors and measured repeat units described below
belong in the current packaged materials.

## Prepared single-cycle materials

`IntegrateSingleCycle.py` integrates the two fixed-road units produced by the
common `Prepare-RepeatUnits.ps1` tool. Their measured source spans are 768 by
768.5 and 768 by 768.25 texels. Both use a 768 by 769 sampling grid over the
unchanged source bounds and unchanged 4 by 4 metre physical period. The resulting
density ratios are `(1, 1.000650618)` and `(1, 1.000976245)`. Neither a larger
physical period nor a reduced source density is used. This removes the unnecessary
two- and four-period pixel copies without adding geometry cuts.

The six channel textures contain complete ten-level mip chains down to 1 by 1;
their source inputs retain all thirteen original atlas levels. Each output level
is sampled from the authored source mip through the common producer rather than
being regenerated from output mip zero. The integration check decodes every
output mip using the native codec and records its hash. Color, normal and mask
channels remain separate native textures with their original channel semantics.

Road aliases use native double-sided cutout template 14, zero road snow and the
existing packed-tangent keywords. Structural regions retain source template 1
and source parameters. These descriptor variants share exactly the same texture
CIDs. The two measured structural regions use the existing numerical resource;
no arbitrary chart or triangle is declared to be a new repeat unit. Other source
regions continue through the common native-source coverage path.

The formal `assets/Materials/PedestrianDraw01` directory contains 80 files:
33 Surface descriptors, seven Texture files (including the shared zero control)
and 40 CID sidecars, totaling 5,183,700 bytes. There are no duplicate physical
payload CIDs or empty folders. Full source atlases, intermediate candidates and
reports are retained only under `artifacts`, outside the mod package. The native
game reader consumed all 33 descriptors successfully, including 27 VT descriptors.

Durable evidence is under
`artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2/single-cycle-v1/`:
`report.json` records density and output sizes, `integration.json` records all
file/mip hashes and runtime aliases, and `native-reader.log` records native
descriptor validation. The stage's `integrated-output/PedestrianDraw01` copy
matches every formal file by SHA-256. The style-only ZIP is 5,201,752 bytes;
this is not the complete Paradox Mods deliverable measurement.

The finite original-atlas comparison covers 7,488 samples across all thirteen
source mip requests. It records 2,133 differences above 2/255, including 1,847
boundary samples; existing-atlas pattern differences remain a human observation
item rather than a strict equality gate. It does not establish normal-vector
angular equivalence, in-game visual acceptance, preview/save/export cold loading
or operation after uninstall. No UI text, geometry, bones or localization behavior
is changed by this integration. Common runtime build verification is coordinated
by the shared implementation agent.

## Complete original structure grouping

`AuditStructureGrouping.py` accounts for all 41,690 recorded original domains
across eleven mesh/material streams and all LODs. The inventory contains 46,056
member triangles; it does not claim to cover the eight native faces absent from
the measured domain inventory. Its manifest and evidence are under
`artifacts/prepared-materials/PedestrianDraw01/structure-units-v1/`.
The common `AuditStructureUnits.py` check passes with unresolved classes.

The existing two measured walking-surface units cover 112 original domains.
Their existing production `Regions` entries and prepared textures are reused;
no additional arbitrary chart or triangle crops are baked. Glass and side
columns account for another 230 domains. `OriginalReuseCheck` exercises their
actual compiled geometry producers and native Surface selector, and the
production UV frame, at six half-width deltas: -1.25, -0.25, 0, 2, 8 and 24 metres.
All 1,380 domain scenarios and 2,760 face scenarios retain the original UVs and
the exact source Surface object. The report records the tested DLL digest and
is included in the manifest evidence chain. This managed-path test does not
claim a game-rendering or cold-load acceptance result.

The remaining 41,348 domains require more specific evidence before extending
repeat coverage:

- 40,278 main-body domains span authored steel, mechanism, housing, rope and
  deck atlas islands. Each needs a measured semantic material period or a
  verified native receiver footprint under its actual part transform.
- 918 far-LOD domains use a separate 512-pixel atlas with merged details. Its
  source identity differs from the 4096-pixel road atlas; road units cannot
  replace it without changing the authored appearance.
- 152 foundation domains stretch with width and use another source material.
  They need foundation-specific pattern periods and true per-island coverage;
  the road units are not applicable.

These counts identify unresolved evidence, not proven runtime failure counts or
proof that all those surfaces are nonperiodic. No uncovered domain is silently
redirected to a different texture. The prepared directory remains 5,183,700
bytes; the full generated-bridge material dependency size and complete generation
coverage are not established by this inventory. No formal resource is identified
as unreferenced by this check, and none is deleted. No UI text or localization
behavior changes.

## Current continuation evidence

The current durable material handoff is
`artifacts/prepared-materials/PedestrianDraw01/HANDOFF.md`.
`RecoverNativeBoundaries.py` reconstructs filled boundaries after excluding
zero-area UV triangles, preserving all previous valid loops and verifying exact
non-overlap and physical occurrence identity. The production chart library now
serves 36,148 domains; 5,542 remain conservative. `RegionCoverage` evaluates the
compiled geometry plans with source-linked material mapping. Its finite checks
resolve 396 additional receiver observations but do not establish complete
material generation, native rendering or persisted-owner size acceptance.

