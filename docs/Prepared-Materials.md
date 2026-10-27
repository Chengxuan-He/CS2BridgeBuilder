# Prepared Bridge Materials

## Directory layout

Prepared material payloads belong in `assets/Materials/<BridgeStyleId>/`. Follow the
[material generation contract](agent-contract/MATERIAL_GENERATION_CONTRACT.md#5-per-bridge-material-directory-and-mod-packaging).

Each immediate subdirectory uses a stable `BridgeStyleDefinition` ID. Keep every style's
materials and non-built-in dependency payloads inside its own directory. Additional source
variants may use subdirectories within that style. Do not use localized names or generated
bridge UUIDs here.

Only styles with actual material/dependency payloads may have directories. Do not create or
retain empty directories or use `.gitkeep`, documentation or dummy files to keep them present.
Styles that reuse built-in original materials without prepared external payloads need no folder.

## Packaging and lifetime

Full reuse of an original material must create no new material. Keep built-in materials as
native references; only required external dependency files are copied byte-for-byte with their
existing CIDs. This directory does not require baking or cloning unchanged original materials.

The required package destination is `Materials/<BridgeStyleId>/` beside `BridgeBuilder.dll`.
Copy complete native payloads, mip/VT resources and CID sidecars unchanged, preserving relative
paths. Documentation and `.gitkeep` placeholders are not runtime payloads and must be excluded.
Build, installation and publication must retain only populated payload directories. Omit folders
left empty after filtering, including the packaged `Materials/` root if there are no payloads.

Treat installed sources as read-only. Runtime generation copies prepared resources into each
bridge's independent dependency snapshot; it must not rebake constant materials or use a
previously generated bridge as the source.

## Source-level rebuild

Generated material libraries are being rebuilt from the installed game's original source
archives. The removed per-sampler material library is not a production fallback. A successful
code build without the complete rebuilt material graph is not a deliverable bridge package.
The current source-level producer emits compatible native references or converted descriptors;
it does not yet complete cross-chart structural repetition or all road-repeat mappings.

Use `tools/Rebuild-NativeMaterials.ps1` in each style's independent output directory:

```powershell
./tools/Rebuild-NativeMaterials.ps1 -Style Draw01 `
  -Manifest <absolute-source-manifest.json> -Output <private-output-root> `
  -Report <private-report.json> -GameData <Cities2_Data> `
  -TemplateDefaults <native-template-json-directory> `
  -CommonMaterials <validated-fresh-common-style-directory>
```

`Output` is a root; the producer writes only `Output/<Style>/`. The source manifest is:

```json
{
  "style": "Draw01",
  "sources": [
    {
      "cid": "16b28f9bc6abc54fd73acbdfa5f14978",
      "sha256": "2d821cde8d3b99205eedeae22f74999f39f5ba60154c7514ae0a05fa84eb87c7",
      "template": 17
    }
  ]
}
```

Additional inventory fields are permitted. Sources are resolved anew by CID in the installed
Game and BridgesAndPorts archives, and their descriptor hash and template are checked. Already
compatible object Surfaces remain built-in references and create no files. Required native
object conversions preserve source texture identities, complete VT headers, and explicitly
persist common source scalar defaults. Network descriptors with worldspace bindings require
an explicit conversion recipe; they are not blindly relabeled as object materials.

Byte-identical converted descriptors are stored once at
`NativeSources/<descriptorCid>/Object.Surface`. The CID is derived from the complete converted
bytes, independently of source aliases or style. Each bridge-specific binding must use the
source-to-output mapping in the report. A changed recipe does not authorize retaining an old
hardcoded descriptor CID.

`CommonMaterials` accepts the validated fresh Cable, Road, Lanes and Shared directories only.
It copies their bytes and CIDs rather than baking them again. Old generated libraries must not
be passed to this option. Conflicting existing outputs fail before any writes; use a fresh
private output directory for a changed recipe. Formal source integration follows common
validation and coordinator scheduling.

The output report has schema `bridgebuilder.native-material-output/1`, `style`,
`completeStyle`, `roots`, `files`, `fileCount`, `bytes`, `texturePayloadsCreated`,
`nativeMipBytesReencoded`, and `remaining`. Each source root records `sourceCid`,
`sourceTemplate`, `targetTemplate`, `operation`, `cid`, `path` and `builtinDependencies`.
A fully reused source has a null path. File entries contain relative `path`, exact `bytes`,
and `sha256`. The Paint root identifies its native VT backing; sampling records remain
separate from texture identity. `completeStyle: false` is mandatory until production coverage,
rendering and independent saved-asset checks are complete.

`tools/Test-NativeMaterialDescriptors.ps1 -ProjectPath <private-Unity> -Materials <style-folder>
-GameManaged <Managed> -LogPath <log>` uses the installed game's Surface header, property-map
and Hash128 readers. It checks complete consumption and object-template compatibility.
The Draw01 source-level output passes 30 native descriptors, including 27 VT descriptors.
This validates file format, not shader rendering or in-game visual acceptance.

The integrated source library contains 618 files (180,345 bytes), including 299 Surface
descriptors and ten style-local copies of one constant-control Texture, each with a CID
sidecar. Built-in texture references are not packaged texture bytes. Native dependency closure
and source-linked independent-owner snapshot checks pass for all ten styles and 38 distinct
identities. This is a partial library, with no prepared Structure/Repeat fallback.

The standard `tools/Build.ps1` checks embedded numerical resources and compilation.
The production `MovableStructureRetile`/`MovableStructureTiles` path uses exact chart-contained
source UVs where density is preserved. Remaining receivers need a verified region-repeat
representation. A full atlas reference outside its actual chart, a bounding-box-only test,
vertex-only modulo, and an old per-triangle folded bake are not accepted replacements.
Neither a small incomplete private output nor a build without prepared resources establishes
the 50,000,000-byte complete Paradox Mods package gate.

## Genuine repeat-unit preparation

The material contract permits each evidenced genuine repeat unit to receive one necessary
offline conversion. All sampling uses share that result. Different genuine units in one
atlas may have distinct payloads; arbitrary charts, triangles and sampling variants are
not additional units. A native full-atlas Repeat sampler cannot express local repetition
`a + w * frac(x / P)` inside one atlas rectangle, so an independently extracted true unit
is used where native full-texture Repeat is the required representation. Road triangles
are not subdivided at texture periods.

`tools/Prepare-RepeatUnits.ps1` is the shared isolated Unity entry point:

```powershell
./tools/Prepare-RepeatUnits.ps1 -Style Lift01 -ProjectPath <private-unity-project> `
  -GameData <Cities2_Data> -Manifest <manifest.json> -Output <private-output-root> `
  -PreparedMaterials <prepared-Materials-root> -Report <report.json> -LogPath <unity.log>
```

The manifest uses `recipe: "BridgeBuilder/RepeatUnits/2"`, `style`, and `units`.
Each unit has the following fields:

- `id`: diagnostic semantic-unit name; not a pixel identity.
- `sourceCid`, `sourceDigest`: authoritative source Surface identity/version.
- `min`, `max`: measured two-component normalized source rectangle.
- `physicalSize`: measured two-component physical period for this evidence record.
  Different use frames remain in the bridge's existing mappings; they are not pixel keys.
- `evidence: { path, sha256 }`: reviewed prototype and periodic-boundary evidence.
- `descriptor: { path, sha256 }`: original native Default/Cutout Surface, VT or non-VT.
- `channels`: records `{ name, cid, sha256, path }` for complete authored native Texture
  payloads. BaseColor, Normal and Mask are required. Preserve authored Control and required
  Emissive channels. A verified transparent control may only replace an absent control;
  it must not replace an authored control map.
- `aliases`: equivalent existing road sampler keys, independently verified by the bridge
  agent. These are use records, never additional texture variants.

Paths are absolute or relative to the manifest. The producer hashes every source and evidence
file before use. Native Texture inputs must contain their complete authored mip pyramid;
an absent original authoring CID or a mip-zero VT extraction is not valid input. Search the
actual built-in Blob archives as well as VT archives before concluding an original Texture
is unavailable. For VT-only sources, first run:

```powershell
python tools/PrepareRepeatUnits/ExtractVtSources.py <manifest.json> <private-source-output> <Cities2_Data>
```

The resulting `manifest.json` is directly accepted by `Prepare-RepeatUnits.ps1`. Channel `kind`
is `Texture` (default), `VTTexture`, or `VTSurface`; combined surfaces specify `stack` and `layer`.
The adapter recovers original square BC7 blocks from native tiles, the native tail and the
appropriate game's `MidMips*.cok` entries. It checks the first intermediate mip against the
independent trilinear tile copy. It does not regenerate mips or recompress BC7. Its full-source
Texture outputs and source reports are private offline inputs, not packaged materials. In
combined VT files, the tail offset is relative to the end of the 498-byte header. All intermediate
levels are individually hashed in `extraction.json`; large original MidMips caches stay in the game.

The producer samples each output level from the corresponding original source mip, retaining
color-space interpretation, instead of generating all levels from mip zero. Its default is native
RGBA8 Texture serialization and lossless file compression. A channel may explicitly request
`"encoding":"BC7"` after a source-specific quality and size check. This is lossy native encoding,
not a resolution reduction. The single-period sampling grid is rounded upward to a four-texel
block boundary without changing the source region or physical period. Each authored mip is
encoded independently at Best quality; partial last blocks replicate edge texels outside the
logical mip extent. Those block-only texels are not added to the texture's native sampling extent.
This avoids the black partial-block edges observed with Unity's whole-chain compressor.
Preserve the healthy RGBA comparison, native-read every saved mip, and report encoded channel,
alpha-cutout and decoded normal errors before selecting an output. Do not blanket-convert channels
whose size or quality worsens. Sampler variants must still share the chosen unit payload.
Integral spans retain their dimensions, including 638 texels rather than 640. Fractional spans
use one semantic period on a grid rounded upward to the next whole texel. The source region and
physical period remain unchanged; the extra sampling point does not widen the pattern or round
its measured bounds. This necessary offline reconstruction has no lower base sampling density
than the source and avoids huge rational-period images. It is not a pixel-identical conversion:
filtering differences, including repeat seams, remain part of native and manual appearance checks.
Reports include original fractional spans, output dimensions and both density ratios. All output
levels sample their corresponding authored source mips, not a mip-zero-generated chain. Every
complete generated owner must meet its 10 MB material ceiling, and the complete deliverable must
meet the 50 MB package gate. Prepared-directory size is reported separately.
An evidenced composite unit may provide `bands`, each with `start`, `end`, `min`, `max` and
integer `repeatY`. Bands cover the horizontal unit interval continuously from zero to one;
their original proportions and source regions are retained. The output grid meets the highest
directional source sampling density of every band. Bands are part of the source semantic unit,
not receiver-dependent crops; they do not authorize arbitrary chart consolidation. Authored
Control data is sampled through the same mapping rather than replaced with transparent Control.

Pixels are keyed by source texture identity/content and the actual unit rectangle. Diagnostic
names, aliases, physical use frames, receiver geometry and descriptor settings are excluded.
Outputs are `Shared/<payloadCID>.Texture` plus CID sidecars. Source material fields and
keywords are preserved; VT binding is replaced with the extracted native Texture binding.
Template 1 remains DefaultCutout, not template 14 DefaultCutoutDoubleSided. The producer does
not infer a double-sided material requirement.

The optional `-ProductStore` retains verified conversion products outside the published output.
Identical source content, genuine region and conversion state reuse those bytes across aliases
and descriptor variants. A content mismatch fails verification rather than silently replacing
an existing product.

Each report's `key` is a **material binding key**, derived from the resulting Surface identity,
not a pure texture-unit identity. It selects `RepeatUnits/<key>/Repeat.Surface`. Opaque and
cutout descriptors may therefore have different binding keys while sharing identical texture
files. Do not rebake pixels for those descriptor differences.

`MovableRepeatUnits` contains per-style partial alias resolvers. Each bridge's own file
implements `static partial void <StyleId>(string samplerKey, ref string? unitKey)` and assigns
only validated aliases to the report binding key. `BridgePreparedMaterials.Repeat` uses that
mapping before the ordinary source path. Existing per-use UV frames remain in the actual
calling geometry path. A composite-band recipe cannot be aliased to one rectangle merely
because its main region matches.

`BridgePreparedMaterials.Structure` also resolves these aliases for structural rectangles with
the same measured unit mapping. A triangular source domain with an unrelated normalized mapping
must not be registered as a rectangular alias. Nonperiodic structural coverage remains separate.

For a genuine rectangular unit containing several original structural domains, implement
`static partial void <StyleId>Regions(string sourceDigest, List<Region> regions)` in that style.
Match the original Surface byte digest and add `new Region(bindingKey,minU,minV,maxU,maxV)`
using its measured hardcoded coordinates. Select the structural descriptor semantics, not a
road descriptor with unrelated cutout settings. `MovableStructureTiles` first retains compatible
original materials, then uses these regions only when a complete original domain lies inside
the evidenced coverage. Where the original domain contains several periods, use the overload
`Region(bindingKey,minU,minV,maxU,maxV,coverageMinU,coverageMinV,coverageMaxU,coverageMaxV)`.
The first rectangle is the genuine sampling period; the second is the independently measured
original same-material domain. It does not expand the physical period or admit neighboring atlas
parts merely because a generated triangle reaches them. It converts source UVs into continuous true-unit
coordinates and verifies the Float32 gradient. Constructed faces retain their unconditioned
double coordinates; an integer phase in an old triangular domain is not treated as a period
of a different rectangular unit. This adds no triangles and creates no textures at runtime.

The isolated producer fixture verifies distinct authored mip colors byte-for-byte, unchanged
full mip count, native Repeat settings and one conversion across different use aliases.
It is not a real-bridge rendering result. Real-unit outputs, alias equivalence, native graph
closure and complete generation remain to be verified per bridge. Structural regions lacking
an evidenced period still require a density-preserving native representation; no per-triangle
fold textures or deleted large prepared library may be restored as a fallback. No final
package-size or human-acceptance claim follows from a small partial library.

A concrete structural case is `LiftBridge04 Mesh`, mesh 0, LOD 0, face 4,
measured domain 2. The actual width plan with delta 2 changes the lateral endpoints from
approximately +/-5.1018066 m to +/-7.1018066 m. Its original Surface is
`6b973ac2133fa9bd8d897bc9eff3363f` (Default, VT). The original triangle fits the measured
46-by-1266-source-texel chart; the density-preserving generated triangle has a 1762.999-texel
edge, longer than that chart's 1266.835-texel diagonal. Translating or rotating the corrected
UVs inside this chart cannot fit that edge without reducing density. The exact original and
generated positions, UVs, loops and assembly identity are recorded in
`artifacts/shared-material-pipeline/structure-counterexample/receiver.json`.

The inspected compatible Default shader exposes affine UV scale/offset and whole-texture
sampling. Native VT transforms address the registered full source allocation, not a user-defined
subregion period; see `tools/PreparedMaterials/Draw03/NativeLayers.md` and its native metadata
inventory. A verified larger same-pattern domain or genuine period could make source reuse or
the new region route valid. Neither follows merely from this chart's rectangular shape. In their
absence, whole-atlas repetition samples unrelated regions, squeezing UVs violates the density
rule, and preparing this receiver's arbitrary crop would violate genuine-unit consolidation.
The merged structural LOD subdivision permission in bridge generation contract section 5 is
limited to representing authored parts' separate width transforms. This detailed single-part
face is not that case; movable contract section 24.6 also excludes physical texture-period cuts
for nonrectangular domains. Subdivision alone would not establish a repeating source pattern.
The original strip's BaseColor and Mask (including alpha) now provide strong evidence for a
404-texel repeating steel pattern. Normal detail is not byte-periodic; at the 404 seam its
G/A differences are within the original strip's adjacent-detail range. The source strip and
boundary measurements are recorded in the Lift04 inspection and
`artifacts/shared-material-pipeline/structure-counterexample/strip-boundary.json`.
This supports preparing and checking that evidenced unit, with the full 1266-texel source strip
as coverage, without pretending exact normal periodicity. The bridge's full-mip output and
production binding must pass before this case is counted as implemented; other uncovered
structural domains are not automatically covered by this one steel sample.

An original atlas island can have different samples at its two authored edges. Native Repeat
filters across the isolated unit's opposite edges, so exact equality to both original edge
samples is not a valid acceptance assumption. Preserve source density, channel interpretation,
pattern scale and the authored mip inputs; record edge differences for human inspection under
the accepted existing-atlas pattern differences. Lift01's measured normal endpoints differ by
0.619608 on its longitudinal period; this is distinct from missing normals, density loss or a
wrong channel binding. A periodic self-comparison alone does not establish original appearance.

The Paint path is independently connected to the new library. All ten production object
assemblies call `AttachMovablePaint`, which loads `Paint/SkinnedPaint.Surface` through
`BridgePreparedMaterials.Paint(style)` and passes that same surface to the render prefab and
preview capture. All ten files exist and have matching native VT base-layer bindings and
content identities. This path does not request deleted Structure/Repeat keys. The check is
recorded in `artifacts/shared-material-pipeline/paint-route-review.json`; it inspected actual
production sources and serialized assets, rather than executing a live game prefab.
The Paint descriptor has no explicit normal/mask/control VT layers; its GPU default-layer
appearance remains for human visual acceptance. No confirmed route or binding defect was
found, so this review did not change production code.

Reaching human acceptance is sufficient for this task's completion criterion: visual quality,
actual opening/closing and cold-load observations may be performed by the user. Complete
preview, creation and persistence must still be available first. Missing required prepared
material keys prevent that readiness; they are not deferred visual checks.

## Offline paint preparation

The source-level producer creates `Paint/SkinnedPaint.Surface` from the measured original
paint source `4b5500b1b5c270890d99149f7f62e51a`. It uses the native object template with the
original first-layer VTTexture backing, its original dimensions and complete authored mip
pyramid. It does not decode pixels, regenerate mips, or create a color texture. Original
nonexistent authoring Texture IDs remain header metadata and are not ordinary texture
file dependencies. The active native VTTexture CID is the dependency.

The producer requires an independently available original first-layer VTTexture. A source
that exists only as a combined VTSurface needs a verified layer conversion before use; do
not bind a missing original Texture CID to a non-VT descriptor. Native reader validation has
passed; actual native rendering and paint/control fallback behavior remain acceptance work.
The mip-zero decoder in `PreparePaint.Editor.cs` is an authoring diagnostic and must not be
used to populate the rebuilt deliverable.

## Offline cable preparation

Textureless curved overhead-wire materials require a native object-template conversion after
curve geometry has been evaluated. Prepare the native cable appearance as a Surface and a linear
zero-control Texture in each style's `Cable/` directory. The zero control prevents the object
template's city-palette tinting without changing the original wire color.

`tools/PrepareCable` reads the native cable Surface from the installed content archive and the
offline-extracted `CurvedTransparent` template in `tools/PrepareCable/NativeTemplate.json`. It retains the source color override and resolves
metallic, smoothness and snow from source overrides or the measured template defaults. Recipe
and source digests determine the finished identities. Runtime compares all of those resolved
properties exactly before reusing the prepared Surface, independently of the incoming source CID.
An equal-looking name is not a match. Prepared assets are never added to an owner's destruction list.

Sources with different color or scalar overrides still require conversion coverage; until that
migration is complete they retain their own descriptor and reuse the packaged zero-control
Texture. They must not silently inherit the prepared cable's color. This remaining descriptor
creation is not compliance with the constant-material preparation rule. Compatible native object
materials continue to return directly without loading or creating a prepared replacement.

The prepared cable Surface CID is `48419f3a3b00d6aab0d4ac878646c8b9`; each complete style-local
cable graph occupies 239 bytes including sidecars. The source Surface CID is
`106cc56e9f15a135bb6ea7b99d675c2d`, with SHA-256
`8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219`.
The preparer pins both that source and the extracted template and fails on changed evidence.

## Offline lane conversion

Native curve templates 16, 31 and 34 map to compatible object templates 0, 1 and 35.
Prepare their descriptors offline while retaining the exact VT header, original texture and
VT resource identities, property overrides and keywords. Resolve missing appearance scalars
from the measured source template. Do not decode, rebake or duplicate built-in texture payloads.

Store each source variant under `Lanes/<source-descriptor-SHA256>/` within every style.
Runtime selection must match the complete source bytes and the current resolved source-template
defaults; matching a CID or a resource name alone is insufficient. Compatible object materials
remain direct original references. Unknown custom sources retain their distinct appearance while
their conversion coverage is unfinished. This does not make runtime descriptor conversion compliant.

Prepared descriptors may reference built-in VT resources. Validate those references before native
loading and never resolve them through another generated bridge's resources. Persist only the
prepared descriptor into each bridge's dependency snapshot; built-in dependencies remain references.

`tools/PrepareLanes` performs the conversion from installed archives and the extracted template
evidence in `NativeTemplates.json`; `--check` verifies all packaged variants without writing.
The variant key uses the complete source descriptor digest. The output CID additionally includes
the template-evidence digest, recipe `BridgeBuilder/SkinnedLane/1` and output bytes.

## Offline worldspace asphalt conversion

The shared native Road source uses network worldspace template 17. Its object conversion binds
the same albedo, normal and mask textures in template 0, with a zero control mask and native
octahedral tangents. Prepare that constant descriptor and control mask in every style's `Road/`
directory. Preserve the source worldspace UV scale separately in geometry; do not bake a
bridge-sized asphalt texture. Match all three native texture identities before reusing the
prepared descriptor. A changed source must retain its own texture bindings.

The road appearance owner must allocate variable composition assets in the owning bridge's
database with owner-specific names. It must never use the prepared source's database or name
for writes. Only locally created assets enter relocation, cleanup or ownership-transfer lists;
prepared materials and built-in textures never enter those lists.

`tools/PrepareRoad` reads the shared native Road CID `1d7dad9f64c9fe55a809f1080c39fb92`;
source SHA-256 is `99f48ff7b6296957b5170c7fdf14ce65912c1de8eb81b15ba7c38b5189be1572`.
Recipe `BridgeBuilder/SkinnedWorldspaceAsphalt/1` produces Surface CID
`88df662f6bc8058d3ecf8f7a5d7f2239`. Each style-local graph occupies 267 bytes including
sidecars. Its original texture channels are neither baked nor copied. Different custom texture
bindings retain their own descriptor until their offline conversion coverage is complete.

## Implementation verification

Run the standard build and localization/generator checks on the integrated sources:

```powershell
./tools/Build.ps1
node tools/CheckRuntimeLocalization.mjs
node tools/CheckBridgeGeneratorStructure.mjs
dotnet run --project tools/MovableUvChartCheck -c Release
```

`MovableUvChartCheck` covers concavities, holes, boundary containment, malformed coordinates
and degenerate rings. `Test-SurfaceReuse.ps1` exercises the compiled composer and shared
structural selection. Source-contained UVs still need their Float32 gradient test; a valid
chart cannot compensate for lost sub-texel precision in stored output UVs.

`NativeMaterialGraphCheck` links the production Surface dependency reader and dependency-copy
engine. It resolves built-in identities against actual installed archives, rejects conflicting
CID bytes and duplicate local payloads, checks all local outgoing edges, then writes two test
owners and verifies byte-exact snapshots and independent deletion:

```powershell
dotnet run --project tools/NativeMaterialGraphCheck -c Release -- `
  assets/Materials <Cities2_Data> <outside-content-report.json>
```

Its roots are the rebuilt descriptor catalogue, including future selected-road lane variants.
It establishes dependency closure, not that every generator branch now has a replacement.
It must not be used to delete possible future-width resources based on a finite width test.

`Test-NativeMaterialDescriptors.ps1` verifies the rebuilt catalogue through the installed native
binary readers. Built-in VT dependencies keep their original identity and encoded mip bytes;
there is no mip-zero reconstruction in the producer. Actual native shader behavior, weather,
paint/control defaults, near/far seams and saved-bridge independence still require the relevant
rendering and game checks. Binary parsing alone cannot certify those behaviors.

The mip-zero paint producer and the old per-sampler Structure/Repeat producer/checkers describe
a different resource representation. Do not run them to fill missing entries in the rebuilt
library. New-format validation uses the source-level producer report and the tools above;
existing legacy-format fixtures may remain useful only for their explicitly scoped regression
checks. No successful legacy fixture authorizes restoring the removed material library.

Only a complete native material graph bound by every required preview/create/regenerate/export
branch can qualify for final package acceptance. An official SDK archive of the partial
integration is a diagnostic measurement, even when its byte length is below 50,000,000.
