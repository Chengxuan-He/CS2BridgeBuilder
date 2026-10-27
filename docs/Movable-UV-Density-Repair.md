# Movable UV density repair status

## Scope

Draw01, Draw02, Draw03, Lift01–Lift05 and PedestrianDraw01–02; every component and LOD.
The local research inventory contains 97 native Geometry files across these ten styles.
An inventory is not a completed generated-output audit.

## Confirmed defects

- Shared road composition chooses the smaller of two directional source densities.
- Each non-repeating chart is fitted into a single page before packing.
- Packing subsequently reduces both axes again to meet a one-page target.
- Homogeneous-asphalt detection currently exempts only a subset of road charts.
- Structural generation ordinarily copies UV0 while applying width-dependent geometry.
- Draw01's repaired UV seams still do not compensate texture density for widened panels.

Latest available Draw01 generation log (2026-10-23 23:57:48): 7/40 asphalt domains tiled;
37 packed charts; page size 4096; packing scale 0.34867835; logged minimum native-density
scale 0.0415762626. The logging expression already multiplies the chart-fit and packing
scales: it must not be multiplied again. For the limiting chart this is approximately
4.16 percent of the requested linear density before integer pixel rounding.
This is evidence of a resolution defect, not an acceptable quality/performance tradeoff.

## Required implementation

1. Establish prototype/source texture dimensions and per-triangle texel metrics across
   all LODs, including sparse native channels and atlas seams.
2. Replace full-span mixed-surface bakes with bounded native-density appearance units,
   retaining surface masks and original non-asphalt layers.
3. Retile deformed structural panels within their measured islands; do not scale UVs
   into adjacent atlas content or stretch a fixed-resolution cropped tile across a wider panel.
4. Keep native batch/texture limits through valid grouping and bounded units, rather than
   silent quality reduction or dropping geometry.
5. Check actual generated Geometry/Surface/Texture resources, mip chains, persistence,
   previews and city rendering. Record each style/component/LOD result separately.

## Current result

### Triangle-level asphalt routing increment (2026-10-24)

The shared generator now classifies each selected-road triangle's actual UV footprint,
including the bilinear filter support, instead of a merged chart's enclosing rectangle.
Non-asphalt texels outside the receiver no longer force a pure asphalt triangle into
the reduced-density composite. A mixed triangle still follows the mixed-surface path;
the repair does not discard its curb, median, or other original material layers.

Classification honors each source mask's U/V sampler addressing (Repeat, Clamp, Mirror,
MirrorOnce), rather than always wrapping. Identical footprints share a dispatch within
the generation request. This is not a cross-request result cache. Pure triangles route
to the existing measured prototype repeat unit, with their original positions, bones,
and triangle count retained. Remaining chart bounds are recomputed from their actual
triangles. No texture-period geometry cuts or custom runtime shaders are introduced.

Validation: the production compute kernel passed GPU checks for an unrelated mixed
texel inside the bounding rectangle, a mixed texel inside the triangle, bilinear borders,
degenerate UV footprints, and differing Clamp/Repeat behavior. Existing surface blending
and 8/16-metre repeat-scale checks passed. Release build, repository tests, projection
orientation/binding tests and native material batch checks passed. No UI text changed;
existing localization behavior is retained.

This is a bounded routing correction, **not completion of the universal repair**.
Mixed-chart downscaling and widened structural UV density remain unresolved. All ten
styles still require generated-output density measurements and human rendering checks.
Existing serialized bridges are not rewritten by a DLL update and are preserved.

The increment was deployed with Cities2 absent, installed DLL/PDB originals backed up,
and source/installed SHA-256 hashes verified:

- DLL: `7A59833DE14AFF99498993629BDA5CBA5FD92C83C6686ED946DC510EEF6D2DAF`
- PDB: `FC71CF199FDD1ECBDBA66F212CB7032B5248D4BA3A5CE8614794782E297843BD`

No post-deployment game sample is available yet. In particular, the old `7/40` log
must not be presented as the new classifier's measured result. New logs report
`tiled triangles` and `unique footprints`, which are not chart counts.

### Independent repeat descriptors and native baseline audit (2026-10-24)

`SaveRepeatingAsphalt` previously rewrote the same input Surface for every distinct
asphalt page. Consequently, later units could replace the textures referenced by
earlier units, although those triangles retained their earlier UV projection.
`SaveRepeat` now creates one owner-local native descriptor per distinct repeat page,
for both asphalt and grating. The input descriptor remains immutable; the existing
repeat-page key still shares identical units across LODs. Created descriptors join
the normal owned-resource rollback, relocation and persistence lists.

`tools/GeometryMetaprogram/UvDensityAudit.py` decoded all 97 native Geometry files
in the ten-style research inventory, retaining position/index/UV channels and UV0
per-triangle Jacobians in `artifacts/uv-density/native`. These source metrics use
UV units/metre: source texture dimensions are not silently assumed. The comparison
mode uses two singular density ratios for uniquely matching UV triangles. Physical
rotation/translation does not change the ratios, and equal-area anisotropic stretch
is detected. Ambiguous or changed UV correspondences remain explicitly unverified.
Same-material texture dimensions cancel in this comparison; material replacements
and converted roads require separate provenance/dimension checks.

The current existing Draw01 bridge `bcfbaa93d-cd28-4f67-8d66-abfe6ac57c4a` (recorded
as Draw01 in `export-state.tsv`, created before these increments) gives the following
structural mesh-0 evidence. The report compares its existing Geometry files directly
with native prototypes; it does not regenerate or modify the saved bridge.

| LOD | Unique UV face matches | Density changed by more than 1% | Unmatched/ambiguous faces |
| --- | ---: | ---: | ---: |
| Near | 1,138 | 146 | 13,150 |
| Middle | 1,008 | 186 | 7,289 |
| Far | 512 | 180 | 62 |

One near example retains approximately `1.0, 0.279833` of the source directional
density. This confirms remaining structural stretch; it does not establish a total
defect count, validate unchanged materials, or certify any unmatched faces. Reports
are `artifacts/uv-density/draw01-saved*.json`. The previously mentioned deleted bridge
directory is absent and was not reused as a data source.

Release build and repository tests passed for the descriptor correction. Atlas-channel
ownership/mip/readback tests and numerical density regressions passed. No user-visible
text changed. Full mixed-surface and structural repair remains active and incomplete.

Deployed with Cities2 absent, original installed DLL/PDB backed up, and matching
compiled/installed hashes: DLL `FA8AA50B6C794FB99672AFA2EF392E650FA9ECA7243F10685DB88E60FE8C908C`;
PDB `F520396803618070965EFC5B9FD08DFECBD7C80469303BE4FDAEE383DAC09D44`.

### Prototype patch repetition and complete object LOD groups (2026-10-24)

The old non-native-road composition kernel already repeated prototype patch UVs
with `frac`, but the CPU only selected native-sized repeat textures for asphalt
and explicitly marked grating. Other valid prototype patch projections therefore
repeated inside a downscaled full-span bake. These now use the same measured crop
and continuous repeat projection directly. Original non-asphalt composites retain
their cutout/double-sided treatment when converted to repeat units. Explicit patch
source materials are preserved instead of being replaced with `input.Native`.
Mixed selected-road inputs and singular/inconsistently oriented projections remain
on their existing composition path; those outstanding cases are not certified.

`RenderGroups` now partitions object-material batches into complete native LOD
groups of at most 16 while retaining every model/Surface reference exactly once.
The established structure/track split is retained when it fits. Larger layouts use
balanced contiguous slot ranges; all groups copy the source skeleton, LOD links,
bounds and object-mesh transform. Additional groups append after authored meshes,
preserving authored attachment indices. No texture is reduced to fit this budget.
Native network pieces do not have this object-mesh attachment route and explicitly
reject additional groups; empty LOD payloads are also not fabricated. These cases
remain implementation limitations, not permission to degrade or omit geometry.

Checks passed: 16/32 batch boundaries and 34/79/192-batch grouping; exact slot
conservation; all 97 source files use one native submesh per model; GPU prototype
crop/repetition at 8 and 16 metres; existing GPU surface-mask checks; Release build
and repository tests. Generated native rendering/cold-load checks remain pending.
No UI strings changed; technical group names remain unlocalized identifiers.

For the saved Draw01 sample, using its unchanged Y/Z coordinates to disambiguate
repeated UVs increases the structural evidence (not generalizable to styles whose
Y/Z change with width):

| LOD | Unique matches | Changed density | Unmatched/ambiguous |
| --- | ---: | ---: | ---: |
| Near | 12,514 | 2,660 | 1,774 |
| Middle | 7,441 | 1,354 | 856 |
| Far | 512 | 180 | 62 |

The affected near/middle connected UV components reuse only 43/42 distinct source
UV rectangles (1,272/688 components). Far geometry has 50 affected components and
50 rectangles. This provides measured input for structural repeat-unit extraction;
it is not a completed repair. All source-face membership remains offline evidence
under `artifacts/uv-density/draw01-affected-islands-lod*.json`.

Deployed after verifying Cities2 was absent and backing up installed DLL/PDB originals.
Verified compiled/installed SHA-256: DLL `7CC957F6EE25CFAE4196740E78623418BE5DA5A88B970DE5CEA8C6F424EA61BA`;
PDB `DC2DC9A1FAA92F52970A0C3267BF2B8E304BD7CDB42E174A980A381A49B0E71C`.

### Structural rectangular-domain integration (2026-10-24)

`MovableUvRecipes.py` now extracts planar appearance domains from all 97 native
Geometry files (117 mesh streams), evaluates an XYZ affine density check, and
optionally packages rectangular domains as 117 checksum-validated typed-array
resources. Source face membership and anchors are identified offline. Existing
Draw01 underside UV corrections are applied before measuring those domains; they
must not be replaced by the original erroneous negative-leaf coordinates.

`MovableStructureRetile` is connected to object-render structural assembly and
supplemental underside geometry. It uses `MovableUvFrame` to preserve the measured
two-dimensional UV gradient metric after widening. Changed domains receive cropped
native repeating materials; rigid domains retain exact native Surface/VT references.
Only vertex records at recorded UV seams are duplicated. No physical triangle is
added, omitted or cut at texture periods. Other raw channels and original bone
identities are copied; normal/tangent frames follow the measured frame rotation.
UV0 output uses Float32 to avoid losing precision in larger repeated coordinates.

`MovableStructureTiles` copies the source shader template and all authored material
properties. Every native texture-stack channel is cropped separately at its own
pixel dimensions, including normal, mask, control and emissive layers where present.
Crop dimensions round upward to BC7 blocks rather than reducing source resolution.
Textures/materials belong to the current owner and participate in preview lifetime,
saved dependency discovery and failed-build rollback. No generated-result cache or
new runtime shader is introduced. The generation compute bundle includes `CropNative`.

Unequal near/far material counts can require additional far batches. The builder now
partitions existing triangle index streams without changing their triangles or raw
attributes; every additional native group still has a nonempty complete LOD chain
and at most 16 batches. Native network-piece attachment remains a separate unfinished
route and does not enter this object-only retile path.

Checks completed:

- Release build and repository tests, including existing localization checks.
- Native frame tests: rigid motion, widening, compression, reciprocal-axis stretch,
  XYZ shear, reflection, source UV residuals and degenerate rejection.
- GPU source-texel crop tests at independent channel sizes, alpha and sRGB handling;
  existing mixed-surface and triangle-mask tests also passed.
- All 117 native source streams use the supported packed normal/tangent encodings.
- Unequal native grouping cases `48/24/2`, `256/8/1`, `1024/16/4` and 16 LOD levels.
- Replay on the older saved Draw01 geometry corrects 2,480 near, 476 middle and 16
  far retained rectangular faces with directional density error below `1.1e-13`.
  They use 42, 21 and 8 source rectangles respectively. This is a mathematical replay,
  not a new in-game generation result. Removed/fragment faces are not included in those
  counts. Evidence: `artifacts/uv-density/draw01-rectangular-replay.json`.

Remaining implementation: mixed-road atlas downscaling, nonrectangular structural
domains, native network-piece grouping and complete generated-output density audits.
The whole task has **not** yet entered game-only verification. New game near/far,
movement and cold-load observations are also pending. No user-facing strings changed;
technical resource identifiers remain unlocalized and existing localized failures
remain in use.

Installed with Cities2 absent and original DLL/PDB-only backup at
`artifacts/backups/uv-structure-installed-20261024-020747`. Compiled and installed
SHA-256 values match: DLL `C6092A4E54C1E649041648307D25F74D0A85D5E2894A205407B46E99C88EEEE0`;
PDB `DD9C5E7EC5CBBD3B9123A7A1B4D7EA3E94AE42EEDF99355D24D05E5772FB256E`.
Embedded-resource decoding also passed for all 476 resources / 2,949,154 typed values.

### Mixed-road and remaining structural routes (2026-10-24)

This increment supersedes the previous implementation-status paragraph, but does not
record visual acceptance or completion of the whole task.

`MovableSurfaceAtlas` no longer fits a chart down to page capacity or retries packing
at `0.9^attempt`. The one-page restriction is removed. `MovableTextureFootprint`
measures pixel-space derivatives of all sampled color, normal, mask and control
channels, including the selected-road worldspace layer or its prototype replacement.
It includes the correlation of oblique UV axes rather than taking the smaller of two
directional densities. Native repeating patch dimensions also account for every
sampled channel, instead of using only the base-color dimensions.

Mixed road receivers use a bounded longitudinal repeating unit when all sampled
projections have a common period. Repeat and mirror samplers are considered explicitly;
clamped coordinates cannot be treated as periodic. The accumulated phase error over
the entire receiver must remain below 0.01 source pixels. One-pixel constant channels
and unbound control defaults do not impose a spurious period. Distinct native surface
layers and original phase are preserved. Only exactly equal source recipes share a
unit within the current generation; there is no generated-result cache.

The native `TextureAsset` writer stores one wrap mode, not independent U/V modes.
Periodic composites therefore retain source-sampled transverse guard bands and use
the full texture extent only along the periodic axis. Their geometry is not cut at
texture periods. Nonperiodic charts retain their full pixel footprint on additional
native pages. A nonperiodic chart larger than the GPU limit now fails explicitly;
providing bounded coverage for every such supported input remains implementation
work, rather than permission to downsample or report completion.

Nonrectangular structural regions now use existing source triangles as repeat units.
The second half of a parallelogram samples a 180-degree rotation of the same source
triangle; it never substitutes an adjacent atlas island. Normal XY directions rotate
with that half. Source physical frames also support rank-one UVs without an inverse
UV matrix. Constant UVs retain their source behavior. Per-channel dimensions bound
the largest oblique source-pixel footprint and round upward to compression blocks.
No physical triangle is added, removed or cut for repetition.

An additional literal-source inventory found 93 original Geometry files omitted by
the earlier 97-file dump. Their names and unique SHA-256 matches are recorded in
`artifacts/uv-density/native-additional/coverage.json`. The combined catalogue now
contains 190 Geometry files / 210 mesh streams, including approach and ordinary
network-pillar components. `Default_Base Mesh` is a native rendering reference used
by the recorded component templates, not an additional transformed mesh in this
inventory. This source coverage is not a generated-output acceptance report.

Network-piece structural models now enter the same retile path. Additional complete
LOD groups are attached as pieces of the same existing section after style-specific
widths are finalized. Dimensions, offsets, requirements, native render components and
LOD links are preserved. The original group alone retains `PlaceableNetPiece`, so
construction, elevation and upkeep costs are not duplicated. Native source review:
`NetCompositionHelpers.CalculateCompositionData` takes the maximum piece width within
a section; `CalculatePlaceableData` sums only pieces with the placeable component.
No new lateral section is introduced. Native network rendering still needs observation.

Validation completed:

- Release build and repository tests, including localization checks.
- 569 checksum-validated resources / 11,714,728 typed values decoded successfully.
- Source-frame replay on 2,122,916 nonconstant, nondegenerate native faces at three
  width factors: 6,368,748 transforms, no failures at the 1% tensor-error threshold.
  Maximum float-coordinate tensor error was approximately 0.263%; this is numerical
  evidence, not a claim of bit-identical raster output or an actual style-plan audit.
- GPU crop tests preserve alpha, source channels, sRGB behavior and triangular normal
  direction. Mixed-composite tests compare all four output channels at five 16-metre
  offsets, covering 80 metres, with and without prototype asphalt replacement.
- Material grouping checks retain exact per-LOD slot coverage, including unequal near/far
  counts, with no fabricated empty LODs. Common-period and oblique-footprint checks pass.

Outstanding: complete generated style-plan/output density and resource-size audits,
bounded handling of oversized nonperiodic receivers, and native near/far, movement and
cold-load observations. The whole task has not entered game-only verification. No UI
text was added; technical resource/log identifiers stay English and existing localized
operation errors are retained.

Deployed with Cities2 absent. Original DLL/PDB backup:
`artifacts/backups/uv-density-installed-20261024-024840`.
Build and installed SHA-256 values match:
DLL `4F86F95936F0158E4CE00668C30B8BAE6A84F3E4CDB3FE3E49113EC3423D0E1C`;
PDB `C10C45498B3CC4445A603FABB6F4544B560CEE50C7D0CE3C5640A991AEA54EBF`.
No generated bridge was deleted or modified as part of deployment.

### Recorded-fragment precision repair and production-plan replay (2026-10-24)

Actual geometry plans exposed defects that the earlier global affine replay did not
exercise. Lift01 approach fragments reused their parent face's UV frame even when
recorded child parts had different width transforms. Some very thin fragments also
lost their UV gradient when interpolated values were stored back as Float16.
PedestrianDraw01's far LOD exposed a separate absolute-UV-delta test that incorrectly
classified small but proportionally significant density changes as unchanged.

The structural retile path now gives each existing recorded fragment its own source
and target frame. Frame intermediates use double precision, and a cyclic choice of the
shortest source edge conditions the Float32 local triangle coordinates. This changes
only parameterization: the fragment's original index sequence, physical coordinates,
bones and source sampling triangle are retained. The change test also compares the
intrinsic physical frame dimensions; it does not rely solely on a fixed UV-distance
threshold. Constructed UV0 values are promoted from Float16 to Float32 before
interpolation, while original Float16 values are represented exactly. Repeated-index
triangles already present in the recorded Lift01 far plan have zero area at every
width; they retain their original indices and material without a nonexistent UV frame.

`ExportApproachUvCases.py` now exports all 190 native Geometry files / 210 mesh streams,
plus the Lift01 tower/end-lip and Lift03 support supplements. `MovablePlanUvCheck`
invokes the built production plan methods using reflection and evaluates the source
and generated UV gradient tensors independently. The Draw01 underside input uses an
explicit synthetic selected-road layout (12 m carriageway and two 3.5 m sidewalks,
widened with the test delta); it is not a game-generated road sample. Draw02/Draw03
selected-sidewalk and joint-layout permutations and arbitrary upper/lower deck width
combinations still require separate coverage. The Draw01 far underside supplement is
not separately exercised by this fixture.

Results against the current Release build:

- 212 plan inputs, with half-width deltas 0, 2 and 8: 636 combinations and 5,150,466
  nondegenerate face comparisons; no failures at the existing 1% tensor threshold.
- Maximum tensor discrepancy: 0.2611390168%. The threshold was not relaxed to hide
  the fragment failures. This is floating-point geometry/UV evidence, not pixel or
  visual equivalence and not a complete serialized-output test.
- Independent native-frame replay: 6,368,748 transforms, zero failures; maximum
  discrepancy 0.2625678953%.
- Release build, repository tests (including localization), and the 569-resource
  checksum/coverage check passed. No new user-visible strings were introduced;
  existing localized failure paths remain in use.

A resource-count experiment found that the existing per-unit retile path can request
thousands of distinct material units (for example, 7,407 in Draw03's detailed mesh
at delta 8 before considering actual native texture dimensions). This is unresolved
resource/size work, not an acceptable completed implementation. A finite-atlas
prototype passed GPU sampling tests, including negative repeat coordinates, guard
bands, normals and alpha, but failed the capacity investigation. In a deliberately
synthetic 4096-by-4096-channel scenario, some meshes exceeded two billion allocated
pixels. These are stress estimates, not measured game asset sizes. The finite-atlas
runtime changes were withdrawn and are **not deployed**. Their source and GPU evidence
are retained under `artifacts/uv-density/atlas-prototype`; the offline packing model
is `tools/MovablePlanUvCheck/ExperimentalPacking.cs`. It is not compiled into the mod.

Remaining implementation/verification work includes bounded structural material
storage and native-size measurements, oversized nonperiodic road receivers,
selected-layout permutations, complete generated Geometry/Surface/Texture inspection,
and native near/far, movement and cold-load observations. The whole task has not yet
reached game-only verification and is not complete. No existing bridge is deleted or
rewritten by this increment.

Deployment: Cities2 was absent. Original installed DLL/PDB files are backed up in
`artifacts/backups/uv-fragment-installed-20261024-034205`; installation evidence is
`artifacts/uv-fragment-install.json`. Build and installed SHA-256 values match:
DLL `E9EA2EB24BCF28DD91941FDDE711BC65849AD3AF273C1736ECCB7FE9CA51CE64`;
PDB `306A6BD98E532F63F4AF6AEC0C2901893F3F1BCD1109ACE83CB55DED6F3C00C9`.

### Full-resolution source decoding (2026-10-24)

Native implementation review found another independent loss of texture resolution.
`SurfaceAsset.Load(useVT: false)` defaults to the global texture mip bias. Moreover,
`TextureAsset.Load(0)` is insufficient once the shared asset has already loaded a
biased instance: native reference counting returns that existing instance without
reloading its original mip chain. Measuring output density from the returned Unity
texture dimensions can therefore permanently bake the current quality reduction.
This is a confirmed code path, not proof that it alone explains every screenshot.

`MovableTextureSource` now reads the original serialized Texture payload into a private
Texture2D. It preserves width, height, graphics format, the exact stored mip count,
sampler settings and mip bytes. It supports native versions 1–5, including the native
version-5 ZSTD wrapper through `CompressionUtilities`. No mip bias is applied, no mip
is regenerated, and no shared TextureAsset is loaded, unloaded or mutated by this
non-VT sampling route. Unsupported dimensions, malformed headers and truncated data
fail explicitly. This reader does not restore detail already absent from the original
serialized asset.

`MovableSurfaceReader` owns this private material/texture lease and copies authored
scalar, vector, color and keyword properties. Structural repeat tiles, moving-road
composites and native marking color extraction now share this same reader. Native VT
sources continue to decode their stored VTSurface/preprocessed VT data; provenance
Texture references are not substituted for VT stack data. Generated results remain
uncached. No runtime shader, UI text or localization key was added.

Validation:

- Release compilation and repository tests passed, including localization checks.
- `TextureSourceCheck.Editor.cs` ran the production decoder under Unity D3D11.
  All 1,280 highest-mip checkerboard pixels matched exactly across native versions
  1–5 and quality mip limits 0/2. The test also retained a partial four-level mip
  chain, format and dimensions, and rejected truncated inputs. This fixture covers
  uncompressed payloads; it does not claim separate GPU coverage of the native ZSTD
  codec or game-loaded Surface integration.
- Inspection of the actual Draw03 structural source found three 4096-by-4096 native
  channels, each with 13 stored mips and 22,369,662 serialized bytes. Thus the earlier
  4096-channel capacity warning cannot be dismissed as an unrealistically large
  source assumption for Draw03. Bounded material storage remains unresolved.
- Evidence: `artifacts/uv-source-fullres-build.log`, `uv-source-fullres-tests.log`,
  `uv-source-fullres-gpu.log`, and `uv-source-fullres-install.json`.

Deployed with Cities2 absent. The previous DLL/PDB originals are backed up in
`artifacts/backups/uv-source-fullres-installed-20261024-040308`. Build and installed
SHA-256 hashes match: DLL
`6681D58618E1EB805B6D48B53FD148420196AD5DF095753D863A6D5B9AC3A4D4`;
PDB `89218038DA1ED56A3F2CB5C19F7FE25BA8321C309E64E3D5B7698F99EA3A83F8`.
Existing bridge assets were not changed. Previous outstanding implementation and
output-audit items still apply; the task is not complete or at game-only verification.

### Measured rectangular units and wider production-plan coverage (2026-10-24)

The source-domain exporter previously split every nonrectangular or non-affine
connected component into independent triangle samplers. It also retained each
triangle's original first edge even when two long, almost parallel edges produced
an unnecessarily large rectangular texture. These issues increased storage without
adding source detail.

`MovableUvRecipes.py` now cyclically pivots triangular sampling frames onto the
shortest original physical edge, using double-precision offline measurements. It
also identifies complementary source UV triangles whose four corners and diagonal
cover a measured rectangle, including UV seams with duplicated vertex indices and
disconnected copies of the same sampled region. Such faces share that rectangle's sampler
bounds but retain independent physical frames and original face membership. This
does not merge target geometry, require matching part transforms, change winding,
cut triangles or reduce source-pixel density. A shared boundary side is explicitly
rejected: overlapping triangles can otherwise have the same summed area as their
bounding box while leaving a gap. Degenerate, skewed and nonrectangular patches are
not treated as rectangles.

Rectangular regions with more than two authored triangles can share the same sampler.
Offline identification deduplicates exact UV-triangle aliases, cancels oppositely directed
interior edges and verifies that the remaining edges partition all four sides of the measured
rectangle exactly once. Holes, ambiguous overlaps, nonrectangular boundaries and unresolved
T-junctions are rejected. This is analysis of existing UV edges, with no mesh Boolean operations,
welding or subdivision. Every physical frame and source-face membership remains separate.
Existing grouped frames use a larger proven sampler only when every member belongs to that
same complete rectangle. `MeasureUvRectangles.py` inventories candidates without modifying
embedded resources; `--repack` in the exporter applies the measured sampler records.

The complete 190-file / 210-stream inventory was regenerated. `--repack` can reuse
the measured domain NPZ records while rebuilding sampling anchors/bounds from the
original native mesh files. `TestMovableUvRecipes.py` verifies rectangular coverage,
translated and non-square charts, reversed winding, overlap rejection, degeneracy
and nonrectangular rejection, duplicated seam vertices, multiple disconnected copies
and separation of equal-sized regions at different atlas locations.

The UV-seam inventory comparison covers all 210 embedded sampling resources: 66 files
change, replacing 25,530 triangular sampler records with proven rectangle bounds.
Every physical frame anchor and source-face membership remains identical. Evidence is
`artifacts/uv-density/uv-seam-rectangle-metadata.json`. The complete production-plan run
is `artifacts/uv-density/uv-seam-rectangle-plans.json`: no density, original-reuse,
packing, pixel-footprint or parent-phase failures. Resource decoding remains bit-exact.

`MovablePlanUvCheck` now applies the production Draw02 curb-removal/sidewalk stages
and Draw03 joint-layout stage. It includes no sidewalks, each single-side case and
unequal left/right widths; independently sized Lift03 upper/lower decks; and the
Draw01 middle-source far-underside supplement. These are explicit offline layout
fixtures, not a claim that every selected-road Prefab has been generated in-game.

Validation against the installed resource revision:

- 213 plan inputs, 1,401 attempted width/layout combinations, including half-width
  deltas -1.25, -0.25, 0, 2, 8 and 24. Existing Draw02 linkage and PedestrianDraw01
  A-frame guards rejected 12 negative-width cases before producing geometry; these
  are reported separately and are not counted as UV passes. Their production rules
  were not changed by this work.
- The remaining 1,389 combinations completed 16,284,198 nondegenerate face checks,
  with zero failures at the unchanged 1% tensor threshold. Maximum discrepancy was
  0.2611390168%. Evidence: `artifacts/uv-density/extended-width-plans.json` and `.log`.
- In the identical delta-8, symmetric-layout, single 4096-by-4096-channel comparison,
  Draw03 near units fell from 7,373 to 5,558 and requested source-unit pixels from
  771,976,688 to 42,277,952. Middle units fell from 6,499 to 5,585 and pixels from
  895,203,520 to 60,905,984. These are uncompressed pixel-allocation estimates, not
  serialized asset-size measurements or complete bridge totals. Comparison files:
  `layout-plans.json` and `rectangle-plans.json` under `artifacts/uv-density`.
- Release build, repository tests/localization checks, rectangle regressions and
  the embedded-resource checks passed. No runtime UI strings were changed.

This is a substantial reduction, but thousands of remaining units still violate
the intended bounded-material design. The experimental finite-atlas route remains
offline only. Further work is required on bounded material grouping/storage,
oversized nonperiodic receivers, serialized outputs and native observations. UV
gradient checks do not establish cross-domain seam appearance or visual acceptance.

Deployment evidence: `artifacts/uv-rectangle-install.json`; previous DLL/PDB originals
are in `artifacts/backups/uv-rectangle-installed-20261024-042842`. Cities2 was absent
and source/installed hashes matched: DLL
`CA436DC69976398CE8E19139CB14B17DF306BA67C5ED4A778FA5C2BC61572A56`;
PDB `89218038DA1ED56A3F2CB5C19F7FE25BA8321C309E64E3D5B7698F99EA3A83F8`.
No existing generated bridge was removed or rewritten.

### Structural page integration and final-map checks (2026-10-24)

The finite structural atlas is now integrated into `MovableStructureRetile` and
`MovableStructureTiles`. This supersedes the earlier offline-only status above.
Each physical domain retains its measured UV frame. Identical source samplers
share the union of their required image footprints, with an explicit alias for
every physical domain. Material grouping preserves the physical index count,
original bones and unrelated vertex channels. It does not cut faces at periods.

`MovableStructurePacking` uses a source-pixel metric that includes the maximum
width and height of all sampled channels. Well-conditioned charts use an
orthogonal pixel frame; almost singular charts retain independent repeat axes
with a conservative density bound. Calculations use a local coordinate origin.
Finite charts have eight-pixel guards and block-aligned dimensions rounded upward.
Ordinary pages are bounded at 4096 pixels per axis; a wider chart does not enlarge
unrelated pages. Large, costly finite footprints use the original native-density
repeating representation instead. The storage decision never reduces resolution.
The new `CropNativeAtlas` kernel shares the existing native crop sampler, including
triangular image/normal rotation, with the original `CropNative` kernel.

The audit now checks the final page mapping separately from source-frame geometry:
unique and complete domain aliases, vertex containment inside guarded charts,
positive-semidefinite output-minus-source pixel metrics, and Float32 round trips
through the same page coordinates and sampling vectors used by the shader.
Round-trip error is measured in original repeating-unit pixels, with a 0.01-pixel
limit. Samples use actual mapped vertices and convex combinations within their
footprint. A bounding box in oblique repeat coordinates can include points outside
the packed physical chart and is not a valid coverage probe.

Validation of the production revision:

- `artifacts/uv-density/production-packing-plans.json`: all 213 inputs; 16,284,198
  geometry face checks and 2,616,212 final-map round-trip checks. Zero geometry,
  round-trip or pixel-footprint failures; maximum round-trip error 0.003715516
  source-unit pixels. Existing rejected negative-width geometry cases remain
  separately recorded, not counted as passing generated geometry.
- `uv-packing-production-gpu.log`: 22,208 channel samples passed using the actual
  production compute kernel, including one finite page and three repeating pages,
  two source resolutions, rectangles, triangles, thin oblique/rank-one UVs,
  negative repeats and RG/AG normal rotation. Sixteen exact-discontinuity probes
  are excluded and reported separately because CPU and GPU fused arithmetic may
  select opposite sides; this is not a seam-appearance acceptance test.
- `uv-packing-surface-gpu.log`: existing native crops, surface classification and
  periodic mixed-road composition regressions passed. The first attempt used a
  stale experimental editor harness; its failure is retained in
  `uv-packing-stale-harness.log`. The passing run used the repository harness.
- Release build, 569 embedded-resource checks, repository tests and localization
  checks passed. No UI text, translation keys or display names changed.

At the same delta-8 symmetric-layout 4096-channel estimate, Draw03 near material
units decrease from 5,558 to 64 pages, Lift01 near from 1,343 to 234, and Lift03 near
from 2,714 to 104. Their page allocations are respectively 102,725,856, 56,137,152
and 55,492,272 pixels per channel. These estimates are per source mesh, not whole
bridge serialized sizes. Finite packing trades additional image area for fewer
materials; it is not a claim that the whole generated asset graph is already
comparable in size to its original.

Deployed with Cities2 absent; original installed DLL/PDB files alone are backed up
under `artifacts/backups/uv-packing-installed-20261024-052157`. Source and installed
hashes match (`artifacts/uv-packing-install.json`): DLL
`AAD4DF86738349DC648DA2CF66E19A6136EA07D99E9ACB4146CB127274ABFC73`, PDB
`7B11866F6036EDA97E8FBB15DF18051BFFF6E4DBF06E5EF48D3C736CA3A94414`.
The rebuilt embedded GPU bundle has SHA-256
`28899342E1ACFCB58FCAE6DAAB76A74C727CAEE57D18DE04E8F919D9DA05012C`.
Healthy generated bridge assets were preserved.

Remaining work includes oversized nonperiodic road receivers, complete serialized
Geometry/Surface/Texture and asset-size audits, and native near/far, movement and
cold-load observations. Some wide pedestrian cases still require many repeating
materials. Numerical and synthetic GPU checks do not establish their game
appearance. The overall task is not complete or at game-only verification yet.

## Compressed source residency and bounded road charts (2026-10-24)

The previous uncompressed RGBA texture-reader test did not establish BC7 source
resolution. A new test through the production BC7 compression/readback pipeline
reproduced the missing detail: at global mip limit 2, a texture still reported its
original dimensions but sampled a lower mip. A one-pixel black/white pattern became
uniform gray. Correct UV derivatives and serialized width/height alone could not
prevent that loss. This is an additional confirmed cause of reduced generation
resolution, not in-game visual acceptance of the reported screenshots.

`MovableTextureSource` now creates private source textures with
`TextureCreationFlags.IgnoreMipmapLimit` before uploading their bytes. This applies
to all generation consumers of `MovableSurfaceReader`, including structural pages,
road composites and marking color extraction. Runtime game quality is unchanged.

The native `VTTextureAsset.Load` and `VTSurfaceDataHandler.FillDebugMaterial` paths
had the same BC7 upload problem. Both discard CPU data after uploading. A detached
clone cannot recover the original levels afterward: the editor reproduced the
non-readable-texture rejection when changing `ignoreMipmapLimit`. Consequently,
`MovableVtTextureSource` reads mip-zero blocks directly from original version-1
`.VTTexture` and `.VTSurface` streams, uses native ZSTD-with-marker decompression,
removes the native 8-pixel tile borders, and uploads private textures with the flag
already set. It preserves the stored graphics format and sampler conventions.
Original game materials and cached VT assets are neither modified nor unloaded.
The generation sampler explicitly reads mip zero; generated output mip chains are
still created by the existing production mip/BC7 pipeline. This does not rewrite
original resources or change generated assets to a custom runtime shader.

For nonperiodic roads, an oversized union of coplanar receivers is now partitioned
at existing face boundaries by `MovableChartPartition`. Each resulting chart retains
the source frame, phase and density. No triangle is cut, removed or duplicated.
A single original face that exceeds the GPU limit at its original density still
fails explicitly. The latest recorded game log did not demonstrate such a single
face; handling that case remains an implementation limitation, not a reported
observed failure.

Validation for this increment:

- `uv-road-partition-tests.log`: 1,003 cases, 32,005 faces and 96,015 phase probes;
  face ownership, capacity and density passed. Includes degenerate-axis charts,
  coincident LOD faces and explicit oversized-face rejection.
- `uv-atlas-channel-final.log`: 180,096 channel samples and 84 lower mips through
  production compression/readback and Texture payload reading. RGB, normal and
  mask data; POT/NPOT pages; quality limits 0/2. Hardware BC7 decoding retained
  mip-zero detail. The test payload header is written by the harness, so this is
  not a complete native AssetDatabase serialization test.
- `uv-vt-layout-and-quality-final.log`: 78 original VT files (74 from the recorded
  movable-source inventory plus four Draw01 fixtures), 154 channels and 10,092,544
  exact channel comparisons between quality limits 0/2. Native ZSTD decompression
  passed. A separate rectangular 4-by-2 synthetic tile fixture verifies tile order,
  border removal and interleaved external/owned layers against the original GPU
  image, rather than relying only on quality-invariant output.
- Release build, 569 embedded-resource checks and repository tests passed. There
  are no new UI strings or translation keys. Source diagnostics remain developer
  messages behind the existing localized operation-error handling.

Failed diagnostic attempts remain available: `uv-atlas-channel-gpu.log` initially
used a linear-byte expectation for a Gamma-project sRGB mip; the corrected expected
value is 188. `uv-atlas-channel-gpu-corrected.log` attempted standalone sub-block BC7
textures; the harness now pads standalone lower-mip probes to valid block sizes.
`uv-atlas-channel-gpu-verified.log` reproduces the actual quality-dependent mip-zero
loss, and `uv-vt-quality-detached-probe.log` shows why late clone repair is invalid.
The final passing checks use the corrected harness and changed source-reader code.

The final build was deployed with Cities2 absent and matching installed hashes;
see `artifacts/uv-bc7-install.json`. DLL SHA-256:
`E7CC26522AAC897845377CEC98B154C31A405829CD08518BF9B3B2FAF380F911`;
PDB SHA-256:
`20B03057E598BB53EB7F2BA7F652129DFFAF19F1F09EB4D55D79F828DA910A85`.
Original installed binaries are backed up under
`artifacts/backups/uv-bc7-installed-20261024-060147`. The embedded GPU bundle is
unchanged from the preceding structural-page increment. Healthy saved bridges
were preserved; previously baked low-resolution textures are not silently rebuilt.

Still outstanding: complete generated Geometry/Surface/Texture and total asset-size
audits, the oversized individual nonperiodic-face case, and native near/far, motion
and cold-load observations. The task is not yet at game-only verification. The
earlier full-resolution claim must be read together with this BC7 residency finding.

## Road attribute precision and native codec verification (2026-10-24)

One remaining precision regression was found in `MovableRoadModel`: a nonzero
third or fourth UV component on any vertex selected Float16x4 for the entire
material batch. Its atlas coordinates could therefore lose detail even after
correct-density packing. A regression triangle at (0.5, 0.5), (0.50001, 0.5),
(0.5, 0.50001) collapses in that representation.

The object-road encoder now retains Float32 for both two- and four-component UVs.
`MovableRoadUv.Encode` removes zero Z/W components only when every vertex has zero
Z/W; otherwise it preserves all four components without quantization. The upstream
finite-value validation remains. Normals, tangents, indices, bones and material
assignments are unchanged. Nine regression cases passed, including one nonzero
Z or W component and values beyond the Float16 range.

Native compatibility was checked in the installed implementation: object/lane
batch caching uses `ObjectMeshHelpers`; `GeometryAsset.CreateMeshes` reconstructs
the declared attribute format/dimension and copies its bytes into Unity mesh
buffers. The separate network `MeshUV0.Unpack` accepts Float32x2 but not Float32x4.
It must not be cited as support for this object-road layout. This check does not
claim an observed native scene render.

`artifacts/uv-density/native-codec-plans.json` adds actual native Meshopt vertex
compression followed by ZSTD level 3 and the reverse decode sequence to the full
layout audit. All 829 nonempty blocks, containing 2,616,212 final-map UV vectors
(20,929,696 raw bytes), round-tripped bit-for-bit. The compressed payload totals
13,279,639 bytes. No geometry, mapping or pixel-footprint failure was reported.
This validates the attribute codec, not the entire saved bridge resource graph.

The following coverage is from that report. Layout counts include attempted
negative-width cases; 12 existing geometry rejections are not UV passes. All ten
styles cover LOD 0, 1 and 2. Face counts cover nondegenerate evaluated geometry;
probe counts cover final packed coordinates.

| Style | Layout attempts | Face checks | Final UV probes |
| --- | ---: | ---: | ---: |
| Draw01 | 201 | 527,625 | 78,602 |
| Draw02 | 300 | 654,002 | 81,831 |
| Draw03 | 240 | 4,474,545 | 1,333,343 |
| Lift01 | 144 | 3,847,986 | 171,113 |
| Lift02 | 120 | 920,784 | 101,515 |
| Lift03 | 144 | 4,747,194 | 595,881 |
| Lift04 | 66 | 276,102 | 35,179 |
| Lift05 | 66 | 340,992 | 45,241 |
| PedestrianDraw01 | 66 | 185,884 | 42,422 |
| PedestrianDraw02 | 54 | 309,084 | 131,085 |

The Release build, 569 embedded-resource checks, repository/localization tests and
focused road UV regression passed. This change adds no user-visible strings.
Deployment was performed with Cities2 absent and verified source/installed hashes:

- DLL: `CB08B66E3FAA0210019336DC099662FF0E967C5FB81B668B0CEF827462F9AD35`.
- PDB: `45C213D9B28EFAF022D1B19B0C09F537BEDCC99326BBED80FFB7047A50EBA439`.
- Installation record: `artifacts/uv-road-float32-install.json`.
- Original installed DLL/PDB backup:
  `artifacts/backups/uv-road-float32-installed-20261024-062120`.

The GPU bundle is unchanged from the preceding increment. No saved bridge was
deleted or rewritten. Existing bridges with previously baked low-resolution
textures do not acquire new texture data merely by replacing the DLL.

### Native integration verification still required

Use newly generated previews and persisted samples from this build. Compare the
same material at the same physical scale against its original source, including
top, underside, side faces, approaches and every LOD. Exercise both leaves and
opened/closed states; include asymmetric sidewalks, median/parking asphalt,
railway, tram and independent Lift03 deck widths. Check material density separately
from lighting and screen resolution. Verify cold loads with the generator disabled.

The complete Geometry/Surface/Texture resource graph and physical/logical size
comparison require a newly persisted game sample. Bridge generation contract
section 24 specifically requires a human-created persisted bridge; codec byte
totals and per-mesh texture estimates cannot substitute for it. Large material
counts in wide pedestrian cases remain an integration concern. The explicit
GPU-capacity rejection for an oversized individual nonperiodic face also remains;
the recorded fixtures have not reproduced such a source face. Do not silently
reduce density if a new selection reaches that limit.

No game visual acceptance, complete native asset-graph acceptance or size compliance
is claimed by this increment. The earlier status entries are chronological
evidence; the coverage and limitations in this section describe this deployment.

## Draw01 fixed-base generation failure (2026-10-24)

The 13:01:43 local game log for preview
`tmp2da1c66b-7d45-43a8-9d61-0cf3d3cd44e2` reports the underlying failure before
the general fixed-base exception:

> Structural retile requires a native texture channel.

`MovableStructureTiles` enumerated only the material description's VT stacks.
Draw01's `DrawBridge01Bottom300cm` surface uses non-VT template 17, with an empty
stack list and three ordinary TextureAsset bindings: `_BaseColorMap`, `_MaskMap`
and `_NormalMap`. Consequently, valid original textures were excluded from the
retile input. This was a channel-discovery regression, not evidence that the
deleted bridge's textures were read again. Numerical UV tests did not exercise
this material-loading branch and therefore did not catch the failure.

`MovableSurfaceReader` now exposes the texture properties it resolves: authored
TextureAsset bindings for non-VT sources and native stack properties for VT
sources. `MovableStructureTiles` consumes that list. All channels still use the
private full-resolution reader and existing density-preserving packing. Empty
input remains an error, now identifying the source material, template and VT flag;
the failure is not suppressed or replaced with a fabricated material.

The recorded original-source inventory contains 93 non-VT surfaces with ordinary
texture references and empty VT stack lists (templates 2, 17 and 33). This shared
fix covers that representation across styles and LODs. See
`artifacts/uv-fixed-base-source-audit.json`; the count describes source inventory,
not 93 independently reproduced game failures.

Release compilation, all 569 embedded-resource checks and repository/localization
tests passed (`uv-fixed-base-build.log`, `uv-fixed-base-repo-tests.log`). No new
UI text was introduced; expanded diagnostics use existing localized error paths.
Original edited sources are backed up in
`artifacts/backups/uv-fixed-base-source-20261024-130548`.

The first deployment attempt stopped before replacing files because Cities2 was
still running (PID 9564). After the process exited, deployment completed at
13:09:26 local time with verified source/installed hashes. See
`artifacts/uv-fixed-base-install.json`; original DLL/PDB files are backed up under
`artifacts/backups/uv-fixed-base-installed-20261024-130926`.
DLL SHA-256: `1341E7BC163A5D496070A6A54F999FC565850963673D7197DC487A8BCFBFA1E7`.
PDB SHA-256: `7CAB14CAB5D2685C872FB6C32392826A0F994737730A3E8ED36E12AD9374C27E`.
No generated bridge assets were removed or rewritten. Repeating the failed preview
is still required; this entry does not claim a successful native preview or visual
acceptance.
