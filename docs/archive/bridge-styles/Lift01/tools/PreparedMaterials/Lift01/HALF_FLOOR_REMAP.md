# Lift01 complete half-floor remap candidate

## Current continuous-unit migration

Regular near/middle domains now load the one offline complete half-floor unit
`7f727eeef2ad7c28df48e6200815357a780ac5636f83f82cd8d0ba46180e5f43`.
Source atlas affine coordinates are converted once to continuous unit coordinates;
there are no sourceAtlasRepeat flags or new period cuts. Existing physical panel,
motion and finite-terminal domains remain. Terminals retain original Surface and
absolute atlas UV. `Types_Lift01_ContinuousUnit` stores measured unit bounds and
source-to-unit scale/offset; near/middle sampling uses one shared payload.

The candidate resource closure is 5,169,930 bytes, not a complete owner budget.
`PrepareHalfFloorSourceGrid.py --grid-mip 3` and `BakeHalfFloorSourceGrid.ps1`
produce it outside the game with sourceGradientSampling and full original mips.
`SourceGridCheck.Editor.cs` reads persisted native headers/dependencies and measures
all 33 channel/mip combinations while retaining native sampler settings. The report
is `half-floor-source-grid/Lift01-complete-half-floor-source-grid-native-check.json`.
Mask finite-U endpoint error reaches 0.16127 at mip 5; mask interior error reaches
0.08022 at mip 6. Base-color mip 9 interior error reaches 0.02502. No increased
acceptance threshold or quality pass is asserted. Far baked track contamination
remains unresolved. These source changes require a new frozen build for geometry
validation and are not approved for deployment.

All candidate06 and earlier statements below describe historical seam-based builds.


## Candidate06 current status

The per-LOD callback is now connected in source and candidate06
(`CCFB8A4395A802B9E901C89CAFF123BF5CF06629369E4F3437F94DD0E6FFB925`).
Production near/middle patches now use projected receiver tangents. Actual-DLL
checks pass 4,608 domain cases, 192 wrap cases (3,840 triangles), and 960 sloped
receiver cases (57,600 vertices). Reports are `candidate06-domain-check.json`,
`candidate06-wrap-check.json` and `candidate06-receiver-frame-check.json` in
`artifacts/material-remap/Lift01`. These harnesses invoke constructors/wrap
directly; `roadPreparationInvokedByHarness=false` describes their scope.

The far constructor contains five finite domains for four original charts. The
negative moving chart requires one measured continuous gradient fold: no common
affine fits its Float16 intervals, while both local maps reproduce original bits.
The callback is not ready for deployment: all four far charts contain baked
rails/sleepers, including in normal and mask channels. Original mip inspection
shows those features merge with nearby texels in coarse mips. A same-atlas clean
Ballast donor has not yet been qualified for all channels and mips. No new far
texture, full-span near-pattern bake or semantic acceptance is claimed.

The following candidate04/05 sections and offline trials are historical evidence;
references there to inactive callbacks or constant production tangents describe
those earlier builds, not candidate06.


## Current direction: original-atlas road wrap seams

The approved road-only wrap-seam path supersedes the regular half-floor extraction
trials below. `Lift01HalfFloorAppearance.Create(lod, native, pose, halfDelta)` now
uses the original Surface for every near/middle domain. Genuine regular V repeats
use `sourceAtlasRepeat`; finite terminals use absolute `sourceUv`. U never wraps.
There is no prepared texture dependency in this constructor. The callback remains
inactive pending shared wrap verification and a complete far-LOD solution.

`PrepareOriginalWrapMetadata.py` records 60 total near/middle appearance domains,
all 576 original face checks, actual native tangent handedness, and 38 unique
internal longitudinal reset planes. Period-cell endpoints must retain their own
0/1 side of a reset; applying a raw fraction to shared seam vertices is invalid.
Only these evidenced road repeats may introduce wrap seams. Existing physical
panel boundaries and finite terminal boundaries remain distinct from repeat cuts.

The earlier candidate04 DLL verification below predates the original-atlas wrap
constructor and is evidence for the measured partitions and terminal projections,
not proof that the later shared wrap implementation has passed. The prepared-unit
and finite-warp resources below remain research candidates outside production.

Candidate05 (`736FBD7781054C3CD547BD43CC9FFBF7A92F19D8F82B27EC40934D1E28EE7272`)
has now passed the actual-DLL original-atlas checks in private Unity:

- 4,608 domain cases retain original U/V, uploaded tangent convention, material
  identity and bone ownership. Maximum U error is 4.18e-7; V phase error 1.79e-6.
- 192 wrap cases cover all 24 regular near/middle domains over four widths and
  two poses. The resulting 3,840 triangles retain area, required minimum period
  cells, selected vertex attributes, original Surface, source occurrence identity,
  and bone ownership. Maximum relative area error is 7.94e-6.
- 960 additional tilted-receiver cases cover five cross-slopes, two longitudinal
  slopes, two widths and two poses. For these measured original lateral tangent
  domains, `projectTangent` preserves handedness and the receiver normal while
  matching the geometric lateral direction. Across 57,600 output vertices,
  orthogonality error is at most 4.14e-8 and tangent error 1.20e-7. Constant source
  tangents instead reach a 0.28735 dot-product error against tilted normals.

These are explicit mode-comparison tests, not a silent runtime mode change.
The production constructor still supplies its measured constant tangent. The
common API currently restricts `projectTangent` to original-atlas repeat patches;
finite terminals need the corresponding explicit receiver-frame handling before
the same conclusion can be applied there. The callback remains inactive and the
far negative finite mapping remains unresolved. No game-render or independent
saved-bridge acceptance is claimed by these numeric checks.

The exact native receiver graph 118602 -> 121091 -> 121077 uses Surface
`a3522ead5e61e02569f0884fb4607eec`. Its base and worldspace layers are the
same supported Ballast appearance; TrackBed describes geometry, not a separate
material type. `AuditAppearanceSupport.py` verifies this graph and references
the exact shared material layer evidence. Other appearance types are not authorized.

`PrepareHalfFloorInputs.py` measures a complete 3 m half-width and one original
4 m longitudinal panel from original NPZ data. Near faces 259058–259061 preserve
the 2 m plus 1 m panel mapping breakpoint; middle faces 165669–165670 retain their
single 3 m affine panel. Neither unit is the old inner-only 2 m crop. U is finite;
V repeats. Width doubling reserves finite-U guards. The resulting candidate sizes
are 1296 x 860 and 1292 x 860, each with 11 mips.

`BakeHalfFloor.ps1` runs exclusively in the Lift01 private Unity environment.
Its private copy of the shared producer permits finite-U composite panels and
includes the final panel at the right guard endpoint. These two adaptations must
be reconciled with the shared producer before considering this a delivery pipeline.

Candidates and evidence are in `artifacts/material-remap/Lift01/half-floor`.
The native header, full dependency graph, all 66 channel/mip pixel-center and
RepeatV instrumentation checks passed. These checks do **not** qualify the assets:
finite-U endpoint error reaches 0.019287 in base color; the source-mip footprint,
panel normal-frame interpretation, and isolated-unit versus original-atlas coarse
mip behavior require the shared producer correction and fresh measurements.
The approximately 0.353 original-atlas boundary discrepancy is recorded, not
accepted by increasing a threshold. No candidate has been published to assets.

`BuildFarAppearance.py` evaluates four real finite LOD2 appearance domains.
It solves for a common affine strictly inside each original Float16 UV rounding
interval and checks exact re-encoding. The two fixed domains and positive moving
half pass. The negative moving trapezoid has no feasible common U affine; a plain
least-squares approximation is not accepted. The report records this as
`readyForRuntime=false`. No per-original-triangle clipping domains are introduced.
That domain needs a finite parameterized mapping supported by the common pipeline.

No runtime road mapping was enabled by this candidate work. There is no new
user-facing text, no localization change, no deployment, and no game-generated
bridge acceptance claim.

## Finite far continuation

`AuditFloorFrames.py` decodes all 584 original floor faces through the installed
native octahedral normal/tangent codec. The far negative domain uses tangent +X
and handedness -1. Its tiny geometric normal slope is **not** authored into the
normal texture: the receiver retains its own geometric normal and the normal
texture keeps the measured local tangent convention (identity transform).

`PrepareFarFiniteMap.py` recovers all ten original BC7 mips of all three far
channels from VTSurface `945c11faccb3838b5e4514358482af2e`, the original MidMips
carrier, and the original tail through `ExtractVtSources.Decoder`. Cached-mid-mip
copies match byte-for-byte. It also measures a finite trapezoid parameterization.
Two source face affine maps are used only by the offline sampling recipe; no
runtime original-face partition or span-dependent pixel count is introduced.

`StageFarFiniteProducer.py` stages a private extension of the shared /3 producer.
`BakeFarFinite.ps1` produced an unqualified 36 x 428, nine-mip native Clamp
candidate. Its content bounds are read from the producer report. There is one
complete original negative moving chart and one shared candidate for all uses;
the candidate is not generated per receiver, width, or bridge instance.

The original prototype's positive, negative, and fixed finite chart coordinates
are distinct. This work has not established a general equivalence/deduplication
rule among those atlas charts; no additional per-use material variants were added.

`FiniteCandidateCheck.Editor.cs/.compute` measures all 27 channel/mip levels using
the actual original GPU sampler, including original anisotropy. Persisted pixel
centers differ by at most 0.002930, but a 257 x 257 filtered finite-domain grid
differs by as much as 0.276783 in Mask and 0.164599 in Normal at mip zero. These
are **failed appearance-equivalence measurements**, not delivery acceptance.
The candidate remains outside `assets`, and runtime wiring remains disabled.

The independent CPU checker uses original face barycentrics and decoded BC7
blocks. It is useful for mapping diagnostics but ordinary CPU trilinear sampling
does not reproduce the original anisotropic GPU sampler. A specific Mask mip-3
probe differs by 0.03848 under native anisotropy, and by 0.00239 with anisotropy
disabled for the diagnostic only. The candidate and acceptance measurements
retain native anisotropy; sampler changes must not be used to manufacture a pass.

All finite-far evidence is under `artifacts/material-remap/Lift01/far-finite`.
`native-sampling-v2.json` is the current unqualified sampling report; earlier
`report.json`/`pixel-comparison.json` refer to the superseded slope-transform trial.
The remaining issue is filtered sampling equivalence after finite reparameterization,
not missing original mip data or unknown source tangent handedness.

## Source-grid physical domains and original finite terminals

`BuildHalfFloorDomains.py` now produces the independent measured resource
`Types_Lift01_HalfFloorDomains.bin`. `Lift01HalfFloorAppearance.Create` constructs
40 near and 20 middle appearance domains while retaining the original physical
bone partition. Near adds only the existing 2 m / 1 m lateral panel boundary;
middle retains its one 3 m half-panel. The regular domains all share one complete
source-grid half-floor asset; their different source U gradients never produce
additional textures.

The original terminal V projections are not all four metres. Three existing
terminal types have lengths 3.751953125, 4.216796875, and 3.9921875 m. Their absolute
V affine projections are measured from original faces and bind the original
Surface through `sourceUv`, preserving the entire original atlas, sampler, and
mip neighbourhood. They are not forced through the regular RepeatV unit. All
576 original near/middle floor faces pass the source U and V checks; regular V
is compared modulo its genuine period and terminal V is compared absolutely.

`InspectHalfFloor.py` exports the original full 646 x 860 albedo region for review.
It contains grey granular ballast and tonal variation, with no embedded painted
road markings. Every near/middle floor face, including the finite terminals,
falls within that exact region. Direct terminal reuse therefore does not retain
an old road-paint layer. This is a specific source-domain observation, not a rule
to remove other visible dark or light surface content.

`CheckCandidateDomains.Editor.cs` invokes the actual frozen candidate DLL
`BBFD1529F590BB8C42CA9E24255175E079BB0EB02507E018C7B1CFAC0667D81F` through reflection
inside private Unity. It verifies 4,608 cases: 576 original face centres, four
width deltas, and two poses. Original bone assignment, coverage, explicit native
tangents, terminal original-Surface identity, and shared regular-unit identity
pass; maximum U error is 1.73e-6, V error 1.91e-6, and tangent error zero.
This check uses placeholder Surface objects only for identity routing: no native
material loading, generated bridge, or appearance acceptance is implied.

The `Lift01RoadPreparation` callback has **not** enabled this constructor. The
regular asset is still unqualified and the far negative mapping remains open.
`PrepareHalfFloorSourceGrid.py --grid-mip 3` prepares the single /3 recipe and
budget estimate: 1304 x 860, eleven mips, approximately 4,493,904 raw BC7 bytes
for three channels. Descriptor/control/owner dependencies and native compression
are excluded, so package and owner size compliance remain unproven.


## Continuous production-chain verification

Frozen DLL `5B13D82BC56A5E458737046EF8B233DD4296CF8FE011E1A94566E921C260523F`
was exercised using `ContinuousProductionCheck.Editor.cs`: actual per-LOD appearance,
physical and appearance TryBind, Compose, physical rebind, MovableRoadModel.TryBuild,
and ToUnityMesh. The harness uses a synthetic full-span receiver and isolated native
descriptor lookup, not the selected-network type dispatch or a game generation request.
Nine far cases passed (three widths and three poses, including pitch/roll): direct
finite UV, original Surface identity, source geometry/color/normal/UV.zw, packed native
attributes and original bones survived actual upload. Far baked track contamination
remains a separate unresolved semantic defect.

All eighteen near/middle cases revealed a singular lateral partition frame: its
third row was changed to X while its first row remained X. Point classification had
not caught this; TryBind correctly rejected it. The source now sets the first row
to negative Z, making the frame invertible without changing the measured split
plane or domain membership. A new frozen DLL is required to verify the repair.
The mixed pass/failure report is `continuous-production/report.json` and must not
be described as a whole-suite pass.


The continuous production-chain repair is verified in frozen DLL
`475775B73D964A1D97B3223F5DC8A539EE4CDBB82CBD10EA00A6791622E8B38F`.
All original 27 cases pass without changing harness tolerances: three widths,
three poses (including pitch/roll), and all three LODs. Appearance-bound,
Compose-output and native-rebound triangle counts remain respectively 154, 94
and 28 for near, middle and far. No period cuts are added. Native upload checks
retain position, normal, color, UV.zw, packed attributes and physical bones;
finite patches retain their original Surface and direct finite UV projection.
The report and raw captures are under `continuous-production06/`.

The regular unit explicitly declares its fully authored finite sampling guard
as zero to one, distinct from its content bounds; this does not add an epsilon
or change content UVs. Tests use actual production Compose and mesh upload with
an isolated native descriptor lookup and synthetic receiver, on the prototype
patch Input(false) path. They do not prove selected-network classification,
whole RoadPreparation, GPU sampling equivalence, cold loading or game visuals.
Coarse mip quality and far baked rails/sleepers remain unresolved independently.


## Bounded far track-removal experiment

`ProbeFarLocalRepair.py` performs a same-atlas local composition, not a near-road
pattern bake. It replaces only the inspected moving-negative central track footprint
(x 59 through 65, y 250 through 419; 1,190 pixels) from original same-row columns
57 and 67. All three PBR channels and alpha share the same mask and donor coordinates.
The donor mask RGB channels are asserted zero. The original 512-square dimensions
and every pixel outside the affected footprint remain unchanged. Mip-zero removal
works for this bounded interior; source end bands are explicitly excluded.

The experiment transfers a box-filtered local delta into each original authored mip,
keeping unrelated texels exactly unchanged. This model fails: mask mip 1 becomes
negative (-0.08627), and the original versus box mip contribution residual reaches
0.20764 at mask mip 5. Color alpha can exceed one (1.00098). No clamping, undocumented
tolerance, native encoding or production publication was performed. These results
identify the missing information: the original filtering/encoding contribution of
the baked track footprint is not recoverable by assuming box filtering of decoded
BC7 mip zero. A justified local mip reconstruction is still required, as are clean
source samples for the endpoint bands and the other three charts. The exact report
and unmodified-range floating point candidates are retained under
`far-finite/local-repair-probe/`. This experiment does not claim recovery of unknown
hidden source texture content or qualify the whole far material.


## Native far local-composite candidate

`ProbeFarFourChartRepair.py` extends the coverage-weighted composition to four
verified interior footprints (3,297 mip-zero pixels). Original same-longitudinal
Ballast donors feed all PBR channels; normals decode native R*A/G coordinates,
blend as vectors and normalize before encoding. All 30 channel/mip candidates
remain within representable bounds without clamping, and zero-coverage values
are unchanged. Endpoint bands remain unrepaired; this is a new local composite,
not a lossless recovery of hidden original appearance.

`EncodeFarComposite.Editor.cs` serializes the entire 512x512, ten-level candidate
as native RGBA8 with lossless ZSTD. It does not recompress the atlas into BC7.
New road Surface CID is `8f7af97c14318d9d96fba87146c92a4b`, prepared key
`f8a79fc74113d8d969bf8a17649ca2b4f7e48ef82e1fd5ded7f48d0150db9dc7`.
The complete candidate closure is 1,188,860 bytes, separate from a generated
owner budget. The unchanged shared control retains its original CID. Original
far structure resources are untouched; only road appearance loads this derivative.
`Lift01FarAppearance.Prepared` retains all original finite UV domains and matrices.

The candidate passed nine actual far consumer cases through Compose, physical
rebind and native Mesh upload using the frozen 06 DLL and the new Surface header.
See `four-chart-convex-probe/consumer/report.json` and `native-manifest.json`.
This validates geometry and binding, not GPU texture quality or full selected-road
dispatch. Endpoint repair, deep-mip visual acceptance and independent cold loading
remain incomplete. The production Create wrapper requires the next frozen DLL test.


Frozen 10 (`17DC9EE1B631EBE203D730707F127AEC786E54D428A20B3163990D1EA1A029D2`)
was tested through the production FarAppearance.Create wrapper. Cold invocation
followed Initialize, RepeatUnit and file/dependency preflight, then failed in native
AssetDatabase.RegisterDatabase because the isolated Unity global database lacks
full game initialization. This is not a successful cold registration.
The harness subsequently seeds the prepared Surface cache with the actual native
candidate header and explicitly records that limitation. Real Create warm-key
lookup then passed all nine consumer/native upload cases. See
`four-chart-convex-probe/production10/production-lookup-scope.txt` and `report.json`.
This does not establish full database registration, independent cold loading,
selected-network dispatch, endpoint repair or material quality acceptance.


The shared double-precision clip-carry regression passes all 27 cases on frozen
11 (`B235BBACECCD5B2763998028B20FF92643E1889850E38CA8910B901BC47BBC9A`).
Near/middle/far retain 154/94/28 triangles through appearance binding, Compose
and physical rebind; every measured rebind area change is zero. Original harness
tolerances and prepared texture bytes are unchanged. The far path executes real
Create and RepeatUnit with an explicitly populated native-header warm cache;
no repeated cold database initialization attempt or full-DB claim is made.
See `continuous-production11/report.json` and `production-lookup-scope.txt`.
Selected-network classification, cold loading, endpoint completeness and filtering
quality remain separate unresolved checks.


## Terminal semantic audit: preserve legitimate metal

The former description of all high-metal terminal donor rows as track contamination
was too broad. `AuditFarTerminalRoles.py` maps each flagged far texel center through
its original UV triangle into physical XZ and intersects the original near and middle
geometry. Both LODs agree on the listed hits; `terminal-role-audit.json` retains the
actual face ordinals, coordinates and authored group bounds. A center-ray miss is
not proof that a thin rail is absent from the pixel/filter footprint.

| Far chart / flagged row | Physical Z at texel center | Evidence and disposition |
| --- | --- | --- |
| Moving negative 229 | Outside far UV chart | Preserve atlas/filter neighbor; not a road-center removal target. |
| Moving negative 230 | -79.8977 | Full-width authored end plate crossing:640, also longitudinal rails in the same region. Preserve plate; rail-only residual unresolved. |
| Moving negative 232 | -79.1457 | Original floor crossing:487 and moving rail span. Metal on side donor is not itself a rail classification; rail footprint still needs specific separation. |
| Moving positive 4 | 79.9918 | Full-width end plate crossing:493. Preserve plate; moving rail span also reaches this end. |
| Moving positive 6 | 79.2403 | Floor crossing:487 and moving rail span. Do not erase the row based on mask R. |
| Fixed negative 380 | -79.8379 | Pixel-center lies on moving floor, outside the fixed generated-road interval. Preserve boundary neighborhood. |
| Fixed negative 382 | -80.5936 | Side donor intersects non-rail side:2210 at x=2.112, y=10.1851; center is floor crossing:13. Preserve the side component; fixed rails remain separately identified. |
| Fixed negative 421 | -95.3295 | Side donor intersects non-rail side:2186; preserve it. Fixed rail span remains. |
| Fixed negative 422 | -95.7074 | Side:2186 and actual central sleeper crossing:54 coexist. Preserve side geometry appearance; sleeper footprint remains to be separated. |
| Fixed negative 423 | -96.0852 | Beyond near/middle and generated road extent; preserve filtering neighborhood. |
| Fixed positive 348 | 96.1797 | Beyond near/middle and generated road extent; preserve filtering neighborhood. |
| Fixed positive 350 | 95.4283 | Floor crossing:3 plus original fixed rail side:1815. Rail replacement still applies locally; not all metal is rail. |
| Fixed positive 388 | 81.1527 | Actual sleeper crossing:52, fastener side:1966 and rail side:1815. Residual track appearance is real. |
| Fixed positive 390 | 80.4014 | Actual sleeper crossing:53, fasteners side:1969/1974 and rail side:1815. Residual track appearance is real. |
| Fixed positive 391 | 80.0257 | Full-width end plate crossing:30 and fixed rail side:1815 coexist. Preserve plate; no whole-row Ballast overwrite. |

The measured rails occupy x=+/-[0.6812438965,0.8509368896], not the proposed
outer Ballast donor coordinates around +/-1.6 to 2.1 m. Fixed rails span
z=80.017578125..96.005859375 and its negative counterpart; moving rails span
-80..80. Sleepers have x=-1.3500061035..1.3500061035. The full-width end plates
extend beyond +/-3.39 m and are separate authored components. Thus a metallic
outer donor does not demonstrate an unwanted rail pixel, while its presence also
does not prove all central track contamination has been removed.

No new texture edits result from this audit. The outstanding endpoint requirement
is now narrower: separate actual rail/sleeper/fastener footprint from co-located
legitimate end plates and side components, with corresponding clean *same-role*
source samples (steel plate where appropriate, not necessarily Ballast). Source
centers outside generated coverage are preserved, not counted as a missing repair.
The current 3,297-pixel interior composite does not close the real residual track
footprints listed above; it must not be declared a complete far quality pass.


## Interior role safety and superseded-resource retirement

`AuditFarInteriorSafety.py` examines the entire far texel footprint against the
near/middle original triangles in the deck contributor band above the measured
9.800048828125 road datum, excluding triangles wholly above 10.4 m. It is a
conservative overlap check, not proof about overhead shadow baking. Authored
rail/fastener and sleeper memberships come from the existing reviewed extraction.
No non-track deck component overlap was found in the 3,297 receiver/donor samples,
but 329 receiver pixels lack track overlap in at least one LOD. They must not be
replaced merely because they lie inside a rectangular track strip.

The composite is narrowed to the remaining 2,968 supported pixels. All 329 rejected
mip-zero pixels are restored byte-exactly for color, normal and mask, verified in
`role-safe-convex-probe/restored-pixel-verification.json`. New local-composite key is
`aa572971f7048c8097d4756077cccd5bce76d15388d43755a5d8e80bf76fa8bf`, Surface CID
`aa7592177f40c808794d570677ccdcb5`, complete native candidate closure 1,192,689 bytes.
The production Create wrapper now selects this key. Nine actual Prepared/Compose/
rebind/native-upload cases pass with the new descriptor and frozen 11 consumer.
A new frozen wrapper lookup test remains necessary.

The old 3,297-pixel development Surface and its three unique texture payloads/CID
sidecars are retired from assets after a reference check; the unchanged shared
control is retained. Eight retired files remain preserved in the original artifact
output, and `retired-prior-resource-files.json` records exact removed paths. No
prototype/built-in or installed generated-owner file was changed. The old key is
not a usable production entry. Terminal mixed-role gaps, actual GPU appearance,
filtering quality and complete native database cold loading remain unfinished.


## Final candidate13 regression and terminal handoff

Frozen DLL `DF500825F27FE4042EEFA8EF6EA19030860A60AF19DDCA210B26632D22EFFA59`
passes all 27 original cases. The far production Create wrapper uses only the new
`aa572971...` prepared key: the harness seeds precisely that one far cache entry,
asserts the retired `f8a79fc7...` asset entry is absent, and verifies the actual
output Surface reference. Near/middle/far remain 154/94/28 triangles throughout
Compose and native rebind/upload, with zero rebind area change. No tolerance or
texture change was made for this regression. Reports are in
`continuous-production13/`; explicit warm-cache/isolated-database scope remains.

Smallest unresolved terminal counterexample: moving-negative row 230 maps to
z=-79.8977 inside the moving road. The original near and middle contain both
full-width end plate crossing:640 (z=-80.0078125..-79.86328125) and moving rails
side:6270/6271 (x=+/-[0.6812438965,0.8509368896], z=-80..80). The apparent outer
metal donor is therefore not demonstrably polluted: it may be the legitimate
end plate. A full row of Ballast would erase the plate, while retaining the old
central pixels leaves original track appearance. Existing center intersections
and metal-channel statistics cannot identify their contributions within the
roughly 0.38 m longitudinal far texel/filter footprint.

To extend the local repair safely, the missing evidence is an original-far donor
whose complete texel/filter support has the same plate/floor contributor mixture
as the target but excludes rail/sleeper/fastener appearance, with corresponding
color, alpha, normal and mask sampling and frame alignment. Alternatively, an
explicit measured local composite separating those contributors is needed. The
same unresolved mixture occurs at fixed-positive row 391 with crossing:30 and
side:1815. Fixed-negative row 422 and fixed-positive rows 388/390 additionally
have actual sleeper evidence. No verified same-role donor is claimed for these
mixed targets. Their original pixels remain untouched; they are not silently
classified as clean Ballast or erased because metal is nonzero.

No further blind donor experiments or rebakes are scheduled from this evidence.
Current resource graph and diagnostics are retained without deployment. Endpoint
semantics, GPU visual/filtering quality, actual selected-network dispatch, full
native database registration/cold loading, and complete owner/package acceptance
remain explicit outstanding work. The geometry-chain PASS does not close them.
