# Prepared material rebuild checkpoints

## Recorded source-level rebuild

The retained source-level rebuild used the installed game's original source
archives. The removed per-sampler material library is not a production fallback. A successful
code build without the complete rebuilt material graph is not a deliverable bridge package.
The producer described in this stage emits compatible native references or converted descriptors;
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

The recorded source-level library contains 618 files (180,345 bytes), including 299 Surface
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
the 55,000,000-byte complete Paradox Mods package gate.

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
integration is a diagnostic measurement, even when its byte length is below 55,000,000.
