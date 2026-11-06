# Lift04 prepared road material

## Measured input

`ExportRecipes.py` reads the existing Lift04 appearance BBN1 resource, verifies its
checksum, and extracts its exact Float32 sampling rectangle. It resolves the real
`LiftBridge04 Mesh` material slot zero through the native anatomy audit and reads
the matching descriptor from the game/DLC packages. No generated bridge asset is
an input. The checked-in `recipes.json` records the source hashes and complete
repeat recipe for the shared offline authoring tool.

The four fixed/moving projection aliases share one pixel recipe:

- Source CID: `6b973ac2133fa9bd8d897bc9eff3363f`.
- Source template: native Default (`0`).
- Rectangle: `(0.4697265625, 0.8251953125)` to
  `(0.62548828125, 0.9814453125)`.
- No bands; native cutout/double-sided converted repeat treatment.

Receiver U/V projection, original road bones and numerical resources are unchanged.
Moving selected-road surfaces retain the shared surface-type selection rules.
This prepared source is for the existing prototype repeat conversion path, not a
replacement for compatible original materials or selected-track materials.

## Reproduction

Run from the repository root with Python, the native game data directory, the full
native anatomy audit, and the verified structural source-channel inventory:

```powershell
python tools/PreparedMaterials/Lift04/ExportRecipes.py . $gameData $nativeAudit artifacts/uv-density/structure-seam-sources.json tools/PreparedMaterials/Lift04/recipes.json
python tools/PreparedMaterials/Lift04/TestRecipes.py
python tools/PreparedMaterials/Lift04/VerifyCoverage.py . $gameData "artifacts/uv-density/native/LiftBridge04 Mesh.npz" artifacts/uv-density/repeats-Lift04-coverage.json
./tools/Prepare-Repeats.ps1 -ProjectPath $lift04UnityProject -GameData $gameData -Inventory tools/PreparedMaterials/Lift04/recipes.json -Output artifacts/prepared-repeats-Lift04 -Report artifacts/uv-density/repeats-Lift04.json -LogPath artifacts/uv-density/repeats-Lift04.log -ProductStore artifacts/prepared-repeat-products-Lift04
./tools/Test-PreparedNative.ps1 -ProjectPath $lift04UnityProject -GameManaged $gameManaged -Structures artifacts/prepared-repeats-Lift04 -LogPath artifacts/uv-density/repeats-Lift04-native.log
```

Use an independent Unity project, Library, Temp and log. Pass existing equivalent
finished product stores as `-ReadOnlyProductStores` instead of rebaking known
equivalent resources. Publish verified payloads only to
`assets/Materials/Lift04/Repeat/<key>/`, preserving native bytes and CID sidecars.
The shared runtime repeat lookup is integrated by the coordinating agent.

## Verification state

The extractor and its checks passed: exact Float32 rectangle, four aliases, source
descriptor/native slot agreement, immutable numerical input, and rejection of
truncated or checksum-damaged BBN1 files. No user-visible text changed; localization
behavior is unchanged.

The native-source density check also passed for all four appearance aliases and the
eight measured source triangles cited by the production appearance code. All three
sampled source channels are 4096 x 4096 with 13 native mips. The measured rectangle
uses 638 x 640 source pixels; BC7 alignment requires a 640 x 640 prepared unit,
with no density reduction. Fixed domains measure 160 transverse and 159.5
longitudinal pixels/metre before block alignment; moving-domain projections retain
their independently measured 3.806640625 m period. The production route exposes
three road LODs and only the cutout repeat variant for this prototype rectangle.
The evidence is `artifacts/uv-density/repeats-Lift04-coverage.json`; it proves source
projection and recipe coverage, not all structural UVs.

Offline Unity baking and native reader validation passed for the single reachable
repeat recipe. The published unit is 640 x 640 with four native texture channels
and complete mip chains. Native validation uploaded all 2,184,960 decoded mip
bytes unchanged and verified source-density dimensions without creating a game
scene. The initial bake's five payloads and CID sidecars occupied 632,351 bytes;
the integrated control reuse below removes its redundant control mip chain.

- Recipe key: `28dc86a8c640b80ac3c0ad59d935ab79c4fdf2ba1919b36f1bd6df07da896505`.
- Current Surface CID: `ff672a61fc295a255437d7b55418fabb`.
- Package payloads: `assets/Materials/Lift04/Repeat/<key>/` and
  `assets/Materials/Lift04/Shared/`.
- Bake report and log: `artifacts/uv-density/repeats-Lift04.json` and
  `artifacts/uv-density/repeats-Lift04.log`.
- Native reader evidence: `artifacts/uv-density/repeats-Lift04-native.log`.
- Retained offline products: `artifacts/prepared-repeat-products-Lift04/`.

Published payloads and the retained product store were checked byte-for-byte
against the validated output. Reuse this product store on subsequent authoring
runs; do not rebake identical resources. Recipe serialization must normalize an
empty bands array to null. The material lookup key includes the source digest;
only the separate offline texture product key excludes it.

Shared runtime integration, structural UV preparation, deployment and in-game
visual acceptance remain separate responsibilities. These results verify the
prepared road repeat, not every structural material or a complete generated bridge.

## Missing structural units

The coordinating agent's missing-only structural inventory contains 21 Lift04
units. All 21 were prepared in an isolated Unity project, producing 84 native
products with source-sized repeating textures and complete channel/mip data.
Native readers accepted all 21 structural descriptors and the existing Lift04
paint input (26 native VT lane descriptors), uploading 31,530,864 mip bytes
unchanged without creating a game scene.

The retained delivery is `artifacts/prepared-materials/Lift04/structures/`:

- `output/`: 168 payload/sidecar files totalling 3,579,349 bytes.
- `products/`: reusable offline products; retain these instead of rebaking.
- `inventory.json` and `report.json`: exact requested/output key coverage.
- `bake.log` and `native.log`: preparation and native validation evidence.

Every preserved output and product matches its validated source hash. These
structural units are staged for coordinating-agent integration, not published
under `assets/Materials/Lift04/`. This check covers the supplied missing-only
inventory; full structural coverage and in-game appearance remain separate checks.

## Repeat control reuse audit

`Test-RepeatControlAudit.ps1 -Output <new-directory>` runs the dedicated
`RepeatControlAudit.Editor.cs` and mip sampling shader in the Lift04 Unity
environment. It reads the real prototype descriptor and VT inputs, validates the
inventory source digest, uses native SurfaceAsset/TextureAsset readers, samples
every control mip on the GPU, and writes only descriptor candidates. It does not
bake textures or modify packaged assets.

The verified result is in
`artifacts/prepared-materials/Lift04/repeat-control-audit-verified/`.
The prototype `6b973ac2133fa9bd8d897bc9eff3363f` has no authored control channel:
its control stack CID is zero and its decoded channels are BaseColor, Normal and
Mask. The complete repeat unit's existing BC7 control is transparent zero at all
10 mips (640 x 640 through 1 x 1), as is the local Road zero texture.

The candidate replaces control CID `7661c8ba2aa7c241197a7b600e420440` with
`729893d593df5d52ba911bc1e7eab77b`. Its Surface CID becomes
`ff672a61fc295a255437d7b55418fabb` using the existing PreparedBakeStore identity
rule. Only the 16-byte control binding changes; all other descriptor bytes and
BaseColor/Normal/Mask references and hashes are unchanged. Native readback of the
persisted candidate passed.

The candidate is integrated into `assets/Materials/Lift04/`. A byte-identical copy
of the Road zero and its original CID sidecar is present in `Shared/`. All 30
packaged Lift04 Surface files were scanned before and after replacement; the old
control had only the replaced Repeat reference and is now unreferenced. Its
payload and sidecar were removed only after that check. Four modified/removed
original files were backed up, without metadata in the backup directory.

`artifacts/prepared-materials/Lift04/repeat-control-integration.json` records the
backup location, exact changed/added/removed files, source and candidate hashes,
16-byte binding-only difference, unchanged other channels, complete local Repeat
dependencies and absence of remaining old control references. The published
Surface is byte-identical to the candidate validated by the native reader.

The removed control and sidecar occupied 118 bytes; the required additional
Shared zero copy occupies 54 bytes, so the style's source package files shrink by
64 bytes. The redundant native mip payload was 546,240 bytes versus 4 bytes for
the shared zero, a 546,236-byte reduction in standalone texture payload. This is
not a measured GPU allocation or final publication archive saving. No textures
were baked, no build/install output was modified, and no UI text or locale keys
changed. `audit.json` and `benefit.json` retain the pre-integration audit evidence.

## Shared source textures and independent sampling records

`ExportSharedSampling.py` separates each original texture's identity from its
structural sampling records. It reads and verifies the real game/DLC descriptors
and complete native VTTexture bytes. It does not bake or copy built-in resources
into the mod package. Reproduce the inventory from the repository root:

```powershell
python tools/PreparedMaterials/Lift04/ExportSharedSampling.py --output artifacts/prepared-materials/Lift04/shared-sampling/map.json
./tools/PreparedMaterials/Lift04/Test-SharedSampling.ps1
```

The measured Lift04 graph contains 525 structural sampler records: 293 rectangular
region repeats and 232 triangular reflection repeats. Six Surface CIDs resolve to
two distinct descriptor contents and six distinct source texture identities. The
six built-in native VTTexture payloads total 28,981,608 bytes; compatible original
bindings require no packaged copies. At measurement time the current style folder
has 1,583 texture files totalling 69,207,758 bytes, including nonstructural resources.
These are uncompressed source-file totals, not official SDK package measurements.

The map retains every sampling key and region/triangle coordinate separately from
the texture identity. Its identity covers original payload content, semantic
channels, dimensions, format and mip count. This is authoring data, not a claim
that the old per-sampler prepared files have been replaced or may be deleted.

`SharedSamplingAudit.Editor.cs` uses the isolated Lift04 Unity project and actual
source textures. Four representatives cover both descriptor contents and both
region types. Direct affine atlas UVs with full-texture Clamp, Repeat or Mirror
were compared against the existing local region modulo/triangle reflection
mapping. All 36 channel/wrap cases differ: 1,230 of 2,268 sampled values differ,
with maximum component error 0.5402304. The current source decoder exposes mip
zero only, so this diagnostic makes no lower-mip equivalence claim. It writes no
texture, encodes no pixels and changes no packaged asset.

This result excludes only that simple affine/full-texture-wrap substitution; it
does not prove that every native representation is impossible. A shared native
implementation must preserve the real prototype's chart patterns, density and
normal orientation without per-sampler image duplication, custom runtime shaders
or excessive subdivision. Generated triangular folds are not a prototype oracle.
The coordinating agent owns that shared implementation.
The source-level rebuild must resolve unsupported cases explicitly; removed
per-sampler libraries must not be restored as a fallback. No runtime UI text or
localization key changes in these authoring tools.

The exporter also writes `native-bindings.json`, covering all nine original render
bindings and the original glass material omitted from the structural-sampler list.
That complete native graph has seven Surface CIDs and ten texture resources:
six VTTextures plus four ordinary glass textures, totalling 37,457,736 source-file
bytes. All are built-in references and create zero packaged copies when compatible.
See [native binding integration](NativeBindingIntegration.md) for the limited shared
code handoff and its unresolved density-extension boundary. No claim is made that
the old generated triangle-fold pattern is the prototype's authoritative behavior.

## Original chart extraction

The Lift04-specific extractor and verification use the measured native meshes:

```powershell
python tools/GeometryMetaprogram/Lift04NativeUvCharts.py --output artifacts/prepared-materials/Lift04/shared-sampling/native-charts.json
python tools/PreparedMaterials/Lift04/TestNativeCharts.py
```

All 525 sampling records map to original triangles in eleven mesh streams across
nine LOD renders. The binary chart candidate is 877,316 bytes, with 63,303 source
faces and 4,164 chart instances. Verification checks exact binary32 UV round trips,
complete face membership, source hashes, all sampler mappings, and rejects an
incomplete rectangular region. It produces no textures. Read
[chart schema](NativeChartSchema.md) before integrating the common consumer; this
authoring result does not establish full-mip rendering or package acceptance.

## Runtime domain bindings

The source-only adapter uses the existing runtime domain indices without changing
geometry or sampling density:

```powershell
python tools/GeometryMetaprogram/Lift04UvChartBindings.py
python tools/PreparedMaterials/Lift04/TestUvChartBindings.py
python tools/PreparedMaterials/Lift04/PrepareReceiverCheck.py
```

The first command generates `Lift04UvCharts.cs` and the bridge-local BBN resource;
the second checks exact source loops, original domain numbering, shared instances
and resource integrity. The last creates an isolated instrumented copy of the
production-plan audit in `artifacts/agent-material-env/Lift04/receiver-check`.
Run that project's `Lift04ReceiverCheck.csproj` with the same arguments documented
by `tools/MovablePlanUvCheck/README.md`, using the Lift04 report destination
`artifacts/prepared-materials/Lift04/shared-sampling/receiver-plans.json`, then run:

```powershell
python tools/PreparedMaterials/Lift04/CheckReceiverCharts.py
```

The actual eleven-mesh, six-width plan run checks density, original reference,
packing, footprint and native UV codec behavior. Chart-only checks are separate
from the production whole-domain and genuine-region coverage below; triangle
counts must not be confused with material counts. The offline predicate uses exact
domain indices and no tolerance; it does not substitute for testing the common
native reader, full-mip appearance or saved assets. No runtime UI or locale keys
are added.

`PrepareNativeReceiverCheck.py` creates a second isolated project that directly
links the production `MovableUvChart`, `MovableStructurePacking.Unit`, source-UV
mapping and Float32 gradient guard. `NativeReceiverCheck.cs` reads the same actual
receiver fixture. This verifies the shared chart predicate and data reader; it is
not the final whole-domain region selector, full-mip game rendering or
persisted-asset independence.

## Fresh source-level rebuild inputs

`PrepareSourceRebuild.py` rereads original game archives and verifies complete
native payload hashes without reading any generated material or product store.
It writes source configuration and original descriptor inputs only under
`artifacts/agent-material-env/Lift04/source-rebuild/config`. Compatible built-in
textures are not copied into the mod. The shared producer receives a new `out`
destination and must never use the removed per-sampler library as a fallback.
Prototype references cover seven original Surfaces and ten original textures;
the one measured repeat region has four appearance aliases. Paint, Cable, Road
and Lane descriptor conversions use the shared source-level producer interface.
Source input verification is not a completed material bake or game acceptance.

The source-level producer is invoked with `native-sources.json`, a new private
output root, and the authorized fresh common descriptor directory. The rebuilt
payload retained outside disposable environments is
`artifacts/prepared-materials/Lift04/rebuild-native/output/Lift04`: 60 files totaling
15,078 bytes (29 Surface files, one 22-byte zero-control Texture and 30 CID files).
Seven compatible prototype Surfaces remain original built-in references and do
not create packaged files. Texture payloads were not decoded or re-encoded.

The installed game's native reader consumed all 29 descriptors, including 27 VT
descriptors. `VerifyNativeRebuild.py` checks output hashes, unique local CIDs,
source-to-output descriptor mappings and all local/built-in outgoing dependencies,
then retains the payload and `graph.json` outside the disposable environment.
`source-output.json` and `native-validation.log` keep producer and native-reader
evidence. No formal `assets/Materials` files are written by this Lift04 step.

This is a valid descriptor subset, not full Lift04 acceptance: out-of-chart
structural receivers still need the common native mapping implementation.
Removed crop textures are not restored. Native reader
success does not establish renderer behavior, complete-mip appearance, cold-load
independence or the complete SDK package size.

## Genuine repeat unit input

`PrepareRepeatUnitsV2.py` writes `repeat-units-v2.json` from the native road faces
and checked hardcoded projection values. Exactly one original rectangle is shared
by four complete quadrilateral source uses (eight faces). Its base extent is
638 by 640 source texels. The two fixed uses are 4 by 4 metres, at 159.5 by 160
pixels per metre; the two moving uses are 3.806640625 by 4 metres, at
167.60184710107748 by 160 pixels per metre. These per-use physical mappings are
separate from the shared pixel unit. Block alignment must not stretch 638 texels
into 640 or rescale the original pattern.

All three original channels have thirteen mips. The evidence retains original
Surface/VTTexture CIDs and hashes, every mip's exact region coordinates, source
faces, boundary edges, projection gradients, native mip-tail hash and complete
tile-table extents. This proves source provenance and the complete serialized
payload extent; it does not prove that a mip-zero-only decoder preserves lower
mips. The source unit corresponds to the existing Repeat key and exactly one
Structure key. Material cutout/other descriptor differences remain separate;
the other 524 structural sampling keys are not newly invented repeat units.

Complete original VTTexture bytes and source descriptor are staged only under
`artifacts/agent-material-env/Lift04/repeat-units-v2/inputs`, with a
`native-inputs.json` handoff beside them. They are temporary authoring input, not
packaged builtin copies. The shared producer must decode all native VT levels;
its native Surface template conversion can retain the original descriptor's
parameters while binding the measured pixel unit. Do not silently regenerate
missing native middle mips from level zero.
The evidence copy outside the disposable environment is
`artifacts/prepared-materials/Lift04/rebuild-native/repeat-unit-evidence.json`.

`FindRepeatAuthoringSources.py` searches every installed Content subdirectory's
Blob and VT archives for the original Texture identities in the three VTTexture
headers. None exists in the 38 archives and 47,134 CID entries checked. The report
is `artifacts/prepared-materials/Lift04/rebuild-native/authoring-source-search.json`.
The ordinary Texture-only producer therefore requires the shared
`tools/PrepareRepeatUnits/ExtractVtSources.py` adapter before consuming these inputs.

`PrepareVtDependencies.py` records the exact original source entries, hashes,
tail/table offsets and native middle-mip cache in `repeat-vt-dependencies.json`
alongside that report. The original Surface CID is present at byte 11,547 in
`Content/BridgesAndPorts/MidMips.cok` entry `TS512_MB3.MidMips`. This cache is an
installed game authoring dependency, not a 283 MB mod payload. Byte occurrence
alone does not establish decoded mip pixels. The shared adapter separately parsed
the native record and joined its 256/128/64 middle levels with the <=32 tail and
larger tile levels; the extraction report preserves the per-mip block hashes.

The shared adapter recovered all thirteen authored mips for each of the three
channels without re-encoding the original BC7 blocks. Its recovered first middle
mip matched the independent copy in the native tile byte for byte. The independent
Unity producer created one 638 by 640 unit with three channels, ten complete output
mips, a native descriptor and a shared zero control: ten files, 736,916 bytes.
Original fixed/moving density mappings remain separate metadata.

`RepeatUnitCheck.Editor.cs` and `RepeatUnitCheck.compute` read the saved native
textures and descriptor, verify complete dependency closure and compare all thirty
channel/mip levels against the original atlas. Pixel-center errors stay within
RGBA8 quantization tolerance, and repeat self-comparison is zero. Original atlas
boundary differences are not zero: maximum BaseColor 0.008301, Mask 0.035294 and
packed Normal 0.429419. These are manual visual observations under the accepted
existing-atlas pattern-difference policy, not a strict-equality production gate.
A periodic unit compared only with itself does not verify the original boundary.

`SaveRepeatUnitCandidate.py` retains candidate outputs, exact per-mip measurements,
source extraction hashes and logs under
`artifacts/prepared-materials/Lift04/rebuild-native/repeat-candidate`. Full original
atlases remain temporary authoring inputs and are not packaged. The known native
descriptor subset plus the original descriptor candidate totals 68 files and
751,940 bytes after sharing the identical zero-control file.

`InstallRepeatUnits.py` installs these exact verified texture bytes in
`assets/Materials/Lift04/Shared`, without another pixel bake. It preserves the
structural Default descriptor (template 0) and creates a separate native
DefaultCutoutDoubleSided road descriptor (template 14), retaining the road path's
snow and cutout behavior. Both descriptors reference the same three texture CIDs
and zero control. `Types/Lift04/Lift04RepeatUnits.cs` maps only the measured road
Repeat key and the single proven normalized structural rectangle key. It does not
map any barycentric triangle or the other 524 structural keys to this rectangle.

The installed Lift04 prepared subset is 78 files and 800,902 bytes, including the
independent road descriptor and the shared beam strip described below.
`repeat-integration.json` records exact hashes and aliases. The installed game
reader consumed all 32 descriptors (27 VT), including both repeat templates;
`artifacts/prepared-materials/Lift04/beam-repeat/native-installed.log` records that check. Common
code owns runtime copying, lifecycle and the remaining native
structural path. The measured atlas-edge differences remain visible manual
acceptance items. Out-of-chart receivers and game/owner lifecycle acceptance
remain open; this is not a declaration that all Lift04 generation paths are done.
No user-visible strings or localization keys change; UI localization is preserved.

## Genuine region routing and receiver coverage

The Lift04 partial registers two genuine regions, only when the original
Surface SHA-256 matches the measured source. Bounds are loaded directly from
`Types_Lift04_Lift04RoadAppearance_cs` F32 entries 0 through 3 for the floor and
`Types_Lift04_BeamRepeatRegions` F64 entries 0 through 7 for the beam. Both region
bindings use structural template-0 descriptors, never the template-14 road descriptor.
Other atlas charts and barycentric triangles are not declared new repeating units.

`PrepareBeamRepeatUnit.py` records the original beam rectangle at pixels
(2092, 2076) through (2138, 3342) in the 4096-square source atlas. Its 46-by-1266
coverage remains unchanged. The authorized real 404-pixel pattern is prepared
once as a 46-by-404 BaseColor/Mask/Normal unit with nine complete mip levels.
The original U/V axes remain unchanged: U spans the beam thickness, V spans its
length. The native source-density ratios remain exactly one on both axes.
The region constructor receives unit bounds first and original coverage bounds
second, so widening repeats the pattern instead of squeezing the original chart.
No old normalized 46-by-1266 recipe is directly aliased to this shorter unit.

`InstallBeamRepeatUnit.py` installs the verified native bytes without rebaking,
shares the existing zero-control file and stores measured bounds in a demand-loaded
128-byte BBN resource. The three-channel payload and descriptor add eight files
and 48,694 bytes. The original source atlases are not packaged. All 27 channel/mip
checks pass native-header, dependency and pixel-center validation; the saved
original-atlas boundary differences remain human visual acceptance observations.
Evidence and outputs are retained in `artifacts/prepared-materials/Lift04/beam-repeat`.

`PrepareReceiverCheck.py` records actual production-plan receivers, including
original constructed-face coordinates and exact phase. `PrepareRegionReceiverCheck.py`
source-links `MovableRepeatRegions`, the Lift04 partial, the original numerical
resource and production Float32 gradient guards. `RegionReceiverCheck.cs` groups
ordinary faces by their complete domain and keeps constructed fragments separate,
matching the runtime selector. Incorrect source hashes register no region.

The eleven-mesh, six-width run passed geometry/density checks. Of 33,470 changed
receiver triangles, 5,626 use the original material, and 1,720 use genuine
regions across 1,220 mapping units. The floor covers 1,560 receivers and the beam
160; main LOD0 and LOD1 each contribute 860 region receivers. The original failing
LOD0 mesh0 face4/domain2 receiver at width delta2 explicitly selects the beam unit.
The remaining 26,124 comprise 24,138 outside the measured regions and
1,986 with a different source Surface hash; no eligible-region receiver failed
the Float32 or degenerate-gradient guard. These are changed-receiver coverage
counts, not the percentage of all bridge faces or game acceptance.

`artifacts/prepared-materials/Lift04/beam-repeat/receiver-coverage.json`
contains per-render counts, while `region-receiver-plans.json` records the plan
checks. Region registration shares the saved texture payloads across all receivers.
Remaining receivers belong to the common native-material work; this registration
does not fabricate new units or restore old crop textures to hide that gap.

## Complete structural inventory

`AuditStructureInventory.py` groups all 49,004 original domains across eleven
meshes and material branches into thirteen dispositions. The shared
`AuditStructureUnits.py` verifies complete assignment and evidence hashes in
`artifacts/prepared-materials/Lift04/structure-inventory`. Receiver evidence covers
six finite widths only. A domain with no changed receiver in those widths is not
proof of validity at every future width. Mixed-domain group totals include
already valid receivers; `remaining-constraints.json` separately counts the exact
22,626 unresolved receivers and identifies original faces and UV charts.

One finite profile inspection verifies the existing 404-pixel longitudinal grain
in four original beam profiles. `PrepareStructuralProfileUnits.py` batches two
shared units: a 266-by-404 complete beam cross-section and a 16-by-404 outer flange.
The narrower and longer overlapping profiles share the former unit; they are not
separate pixel bakes. Original Normal and Mask cross-section details remain in
these units. All 54 channel/mip checks pass native decoding and source pixel-center
comparisons. Candidate output and measurements are retained under
`structure-inventory/profile-candidate`; these add 297,919 bytes after sharing the
zero-control file.

Those unpadded candidates are retained evidence, not runtime bindings. The current
`repeatAxes="V"` products use guarded native textures of 532-by-404 (wide beam)
and 32-by-404 (outer flange), with the original profile in the central half of U.
`InstallStructuralProfiles.py` installs those byte-identical products and measured
BBN bounds. Wide and long beam coverage rectangles remain separate, sharing one
payload. The flange's encoded normal differs by up to 213 across U edges; U
extrapolation is rejected rather than repeated.

All 57 channel/mip checks pass native headers, complete dependency references,
source pixel-center quantization tolerance and repeated-V seam tests. Both density
ratios are one. A repeat run reused all six textures and produced none. The original
atlas boundary differences are recorded for human observation, not asserted zero.
The retained evidence is `structure-inventory/guarded-profiles`. Added resources
total 323,658 bytes; the formal Lift04 directory contains 94 files and 1,124,560
bytes. This is a prepared-library size, not a complete generated owner's closure.

Production-source receiver replay covers 33,470 changed triangles: 5,626 retain
their native source and 5,218 use genuine units (floor 1,560; prior beam 160; wide
beam 3,450; flange 48). Of the remaining 22,626, 5,560 fail the nonperiodic U
constraint, 15,080 lie outside measured regions and 1,986 have another source
Surface hash. No Float32/degenerate rejection remains in this run. The original
beam counterexample still passes. No geometry, original bones or LOD data changed.
Full generation, complete owner closure and persisted cold-load checks remain
open. Keep the prior beam unit: it still supplies 160 receivers. See
`CurrentHandoff.md` for the current continuation boundary.
