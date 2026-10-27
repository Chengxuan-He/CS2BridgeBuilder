# Draw03 prepared repeat units

`PrepareRecipes.py` reads the existing Draw03 numerical resources without changing them,
checks their checksums, resolves the real native source Surface, and emits the inventory
accepted by `tools/Prepare-Repeats.ps1`. It prepares clean asphalt, transparent grating,
one native lane-wear period, and the unpainted strip between lane-wear periods.
Opaque and cutout variants share identical texture recipes; only the Surface differs.

The lane-wear layout still depends on selected lane centers and widths. The atomic lane
and gap units are immutable sources, not a precomputed complete road layout. Their
presence does not mean arbitrary selected-road band composition has been converted to
an offline constant. Never rescale a unit to fit a changed lane width.

```powershell
python tools/PreparedMaterials/Draw03/PrepareRecipes.py . <Cities2_Data> <material-library-resolved.json> <inventory.json>
```

Use a dedicated Unity project and output directory when running the shared repeat baker.
Publish only validated native payloads to `assets/Materials/Draw03/Repeat/<key>/`.
Keep authoring inventories, reports and logs outside `assets/Materials`.
The runtime material lookup uses the immutable source digest, exact crop coordinates,
cutout mode and band recipe; receiver UV origins and dimensions are not material keys.

The source is native DrawBridge03 Surface `792a855e9d0c5c34d2c41d48b062139d`.
The measured recipes originate in `Draw03RoadAppearance` and `Draw03RoadWear`; their
canonical numeric values remain in the existing BBN1 arrays. This tool changes no UI
text, localization keys, geometry, bone ownership or selected-road layout.

## Native output verification

`Test-Repeats.ps1` runs `RepeatNativeCheck.Editor.cs` in the dedicated authoring
project after `tools/Prepare-Repeats.ps1`. Pass `-ProjectPath`, `-GameManaged`,
`-Inventory`, `-Output`, `-Report` and `-LogPath`; `Output` is the root containing
the style directory. The verifier uses the installed game's Surface and Texture
readers, checks the four native channels, opaque/cutout templates, source-sized
dimensions, color spaces, complete mip chains, GPU upload bytes and style-local
CID references. Opaque/cutout variants with identical sampling must share texture
identities. This verifies native serialization and uploads, not game appearance.

The checker compiled against Unity 2022.3.62f2 with zero warnings or errors, then
passed against the finished Draw03 output using the installed native readers and GPU:
7 Surface descriptors, 16 unique native textures, 3 equivalent descriptor variants,
and 3,447,552 complete mip bytes uploaded unchanged. The initially verified graph has
46 payload/CID files totaling 1,714,254 bytes. Its constant-control descriptors now
share the verified Road transparent zero through Shared, removing four redundant
control textures; the integrated repeat graph has 40 files totaling 1,713,959 bytes.
See [control reuse and integration evidence](RepeatControlAudit.md). This measures
the repeat graph, not the complete
mod or a generated bridge. Source-sized units are 968 by 164, 968 by 376, 296 by 376,
and 296 by 32 pixels. The compressed native files preserve all mip payload bytes.

The prepared graph is under `assets/Materials/Draw03/Repeat/` and its dependencies
under `Shared/`. Publication compared every destination file against its validated
source by SHA256, preserving existing style resources. Reports are retained outside
the package as `artifacts/prepared-materials/Draw03/repeat-bake-report.json`,
`repeat-bake.log`, and `repeat-native.log`.

The isolated baker normalizes JsonUtility's empty representation of a null `bands`
array back to null before computing the runtime key. Texture-equivalence keys omit
the containing Surface digest; the authoritative source-texture key, exact sampling,
pipeline, channel and dimensions still participate. These shared-tool changes are
reported to the main agent for integration; this directory does not own those tools.

## Variable lane-wear reuse boundary

The native full lane unit is 296 by 376 source pixels; the empty-area unit is
296 by 32 source pixels. The recorded transverse density is approximately
120.985275 source pixels per metre. Lane centers, clipping and empty-area spans
remain selected-road inputs, not offline constants.

An existing receiver triangle wholly inside one band can reuse its complete
immutable unit without cuts. For a lane band `[min,max]` contained in the native
unit `[fullMin,fullMax]`, its local band coordinate `q` becomes:

```text
unitV = (min - fullMin) / (fullMax - fullMin)
      + q * (max - min) / (fullMax - fullMin)
```

For the empty-area band, retain `unitV = q * repeatY`. Keep the measured
longitudinal projection. Narrower lane bands use a subrange of the full texture;
they do not need a different crop or material. The common composer needs a
per-existing-triangle patch override before chart aggregation and prepared-key
lookup. `Draw03RoadWearUnits.TryReduce(Patch, Vector3, Vector3, Vector3, out Patch)`
implements the Draw03-specific conversion. The main agent has integrated its caller
in the common face loop before chart grouping, preserving native mask projections.
It accepts only the measured Draw03 crop family and triangles strictly inside one
band and one global repeat period. It preserves the longitudinal map, source Surface
and repeat behavior. Boundary, crossing and unrecognized triangles return false and
must retain their current material path; callers must not discard them.

Crossing faces are handled by `Draw03RoadWearDomains`: it expands real lane/gap
appearance domains before the existing clipper and assigns each fragment's full
prepared unit with a band-local affine map. This is permitted appearance-domain
partitioning, not subdivision at every texture repeat. No new texture is needed.
`Draw03RoadPreparation` collects actual bounds across all evaluated LODs, including
far-LOD geometry outside the LOD0 span, and uses those same invocation-local inputs
for preparation. Outer gap domains retain their continuous phase through boundary
roundoff instead of restarting the complete layout. See [integration and evidence](Integration.md).

The unpartitioned dynamic-band formula would make its height grow with road width,
storing repeated gap pixels. That reference calculation is retained in the numerical
report to detect reintroduction of such a path. It does not describe the new pure
asphalt domain path. Mixed native masks still require separate integration review.
The [native layer investigation](NativeLayers.md) records the installed 40-template,
26-shader interface audit and the measured limits of network, character-overlay and
VT atlas mappings. No general shader-only selector inside an unchanged triangle was
established; the implemented appearance-domain path uses the existing Default sampler.
Neither deleting wear nor stretching the 32-pixel gap to fill a median is equivalent.

`VerifyWearUnits.py` verifies the algebra on measured inputs. `WearCheck` links the
actual production `Draw03RoadWear`, embedded-array reader and `MovableRepeatKey`:

```powershell
dotnet run --project tools/PreparedMaterials/Draw03/WearCheck -c Release "-p:GameManagedDir=$env:CSII_MANAGEDPATH" -- artifacts/prepared-materials/Draw03/repeat-inventory.json
```

The production check passed seven key comparisons and 7,196 float32 sample
comparisons over four arithmetic layouts, with a worst difference of
0.0001220703125 source pixels. It also verifies unchanged sidewalk and no-lane
branches. These layouts establish numerical equivalence of the band-local map;
they do not establish real road mesh coverage or visual acceptance.

The source-linked reducer check additionally covers 84 triangles using a three-axis
projection and positive/negative repeat origins, 3,780 barycentric samples, and 24
rejected cross-band/period inputs. Worst source sampling difference is 0.0028076171875
pixels under float32 arithmetic. It does not prove coverage of generated road meshes.

The native geometry check uses `DomainNativeCheck.Editor.cs` and the source-linked
`WearCheck` assembly built for `netstandard2.1` with `OutputType=Library`. It must run
inside Unity because production matrix validation calls a native Unity API. The
Editor runner accepts `-gameManaged`, `-repeatInventory`, `-checkAssembly` and
`-checkReport`, and writes no texture assets. Eighteen 2/3/6-lane, 20/40/80-metre and
reflected-frame scenarios passed: 1,074 fragments, 22,449 interior samples and 840
boundary-phase probes. Maximum source-pixel errors were 0.00048828125 and
0.00024602280518593034 respectively. Five sub-0.000001 m² float slivers were excluded
from bone/sample assertions; the full scope and limitations are in Integration.md.

## Missing structural units

The main agent supplied a 68-unit missing-only structural inventory. All units were
prepared in the independent Draw03 environment, then validated with installed native
Surface and Texture readers. The retained candidate graph is
`artifacts/prepared-materials/Draw03/structures/Structure/`: 552 payload/CID files,
3,714,514 bytes, copied with SHA256 verification. It is intentionally not published
to `assets/Materials`. The accompanying inventory, bake report and native log are
`structures-inventory.json`, `structures-report.json`, `structures-bake.log` and
`structures-native.log` in the parent artifact directory.

Native validation read 68 structural descriptors and 26 existing Draw03 lane
descriptors, plus existing paint/cable/road assets, and uploaded 34,012,080 full mip
bytes unchanged. That combined byte count includes existing assets and is not the
new structural package size. Main-agent integration and human visual acceptance remain.
