# Draw01 road repeat preparation

`ExportRecipes.py` extracts the constant pixel recipes used by
`Draw01ClosedRoad.PrepareRenderLevels` from its actual C# expressions and
checksum-validated BBN arrays. It resolves the near render's Surface CID from the
native audit and verifies the descriptor against the installed game/DLC archives.
It never reads a generated bridge or changes geometry, motion, UV projection or
the numerical resource.

## Recipe coverage

| Appearance regions | Pixel recipe | Object material |
| --- | --- | --- |
| 0, 5, 7, 12 | Asphalt interior | Default |
| 1, 4, 8, 11 | Metal grating | DefaultCutoutDoubleSided |
| 2, 9 | Stationary sidewalk | DefaultCutoutDoubleSided |
| 3, 6, 10, 13 | Stationary carriageway cap | DefaultCutoutDoubleSided |

The 14 regions share four distinct pixel recipes. Mirrored projection axes and
width-dependent origins remain receiver UV data; they must not create extra
baked copies. All three road LODs use the same measured near-atlas regions and
the prepared units' full mip chains. Actual selected-road material selection,
original-compatible surfaces and variable native composites remain separate
runtime responsibilities.

## Authoring

Run from the repository root with the installed game data, complete native audit
and source-channel inventory as arguments:

```powershell
python tools/PreparedMaterials/Draw01/ExportRecipes.py . <Cities2_Data> <all-movable-audit.json> <structure-sources.json> artifacts/prepared-repeat-draw01/inventory.json
```

The output uses `BridgeBuilder/RoadRepeat/1`. It records exact binary32 bounds,
source CID, descriptor digest, channel bindings and source-file hashes. Runtime
keys use the exact `MovableRepeatKey` BinaryWriter protocol and are independently
checked by the shared offline repeat baker. Recipe parsing
fails on changed source aliases or material-slot assignments instead of silently
authoring stale resources.

`VerifyRecipes.py` compares all five measured projection frames against ten
actual native mesh faces in both UV directions. It also checks the current
projection at six narrowing/widening deltas: only phase may change, while the
source rectangles and both UV derivatives remain identical. It checks the current
moving-road classifier route: its carriageway appearance uses regions
0, 5, 7 and 12 because the physical moving spans end before the fixed-cap
boundaries. It must not invent additional opaque grating/cap variants merely
because the shared classifier can produce them for another style.

```powershell
python tools/PreparedMaterials/Draw01/VerifyRecipes.py . "artifacts/uv-density/native/DrawBridge01 Mesh.npz" artifacts/prepared-repeat-draw01/inventory.json artifacts/prepared-repeat-draw01/source-density.json
```

Use a dedicated Draw01 Unity project, output directory, product store and log.
Resolve existing equivalent immutable products before baking. Finished resources
belong below `assets/Materials/Draw01/Repeat/<key>/`; copy them with their original
CIDs after native-file, dependency and density checks. Do not put inventory,
logs, receipts or this documentation into the material payload directory.

`Prepare.ps1` runs extraction, source-density verification, offline baking and
the installed game's native Surface/Texture readers in sequence. Supply an
independent Unity project and a valid local Unity license. It writes only the
Draw01 authoring output and never installs, builds or publishes the mod. A failed
bake or native check stops the sequence; no output is advertised as ready.

Native reader and pixel checks establish authoring compatibility, not in-game
appearance. Human verification still covers both leaves, fixed caps, grating,
all LODs, motion and cold loading independently of Bridge Builder.

## Prepared output verification

The verified units use a 4096-by-4096 native source and retain its sampled density:
asphalt 644-by-732, grating 692-by-916, stationary sidewalk 776-by-1072 and
stationary carriageway cap 832-by-1072 pixels. Their four native channels have
complete mip chains. Native readers validated four Surface descriptors and
16 unique textures, uploading 15,117,952 mip bytes unchanged.

The isolated preparation environment is `artifacts/agent-material-env/Draw01`.
Its `reports/bake.json`, `logs/native.log`, `reports/reuse.json` and
`reports/package-copy.json` record generation, native verification, complete
second-run reuse and byte-identical publication respectively. The verified
payload adds 40 files totaling 6,969,805 bytes under this style's `Repeat` and
`Shared` directories. These counts describe the prepared subset; they are not
the complete mod package or a generated bridge's total asset footprint.

The isolated baker normalizes JsonUtility's empty representation of a null band
array before computing runtime keys. Pixel product keys exclude descriptor
digests because channel content and sampling determine pixels. The coordinator
owns integration of these shared baker corrections. Runtime lookup, deployment
and human appearance acceptance are separate pending integration steps.

Localization review: this authoring tool creates only technical identifiers and
offline evidence. It adds no runtime user-visible strings.
