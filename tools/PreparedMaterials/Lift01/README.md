# Lift01 prepared material authoring

`ExportRecipes.py` extracts the constant road repeat recipe from the checked-in
`Lift01RoadAppearance` BBN1 arrays. It validates the checksum, the runtime patch
construction and all eight fixed/moving, half and side aliases. The aliases have
different receiver UV frames but identical source pixels; they require one repeat
resource, not eight baked textures.

The source is surface slot zero of the measured `LiftBridge01 Mesh` native audit.
The extractor checks that slot against `Lift01SourceRenders`, reads its actual
descriptor bytes from the game/DLC COK archives, and verifies its template against
the structural source inventory. Generated bridges are never authoring sources.

```powershell
python tools/PreparedMaterials/Lift01/ExportRecipes.py . `
  '<Cities2_Data>' '<all-movable-audit.json>' `
  artifacts/uv-density/structure-seam-sources.json `
  artifacts/prepared-materials/Lift01/repeat-recipes.json
```

Pass the resulting inventory to an isolated copy of the repeat authoring tool.
Use a Lift01-only Unity project, output, writable product store and log.
Finished resources belong to `assets/Materials/Lift01/Repeat/<key>/`; do not
create that directory before actual payloads exist. Preserve source Float32 bounds,
channel semantics and density. The shared RoadRepeat/1 key intentionally excludes
receiver geometry, road width, region, style and destination. Equivalent existing
products must be copied with unchanged bytes and CIDs instead of rebaked.

`VerifyDensity.py <repository> <Cities2_Data> <inventory> <report>` reads the actual
native VT headers without Unity, checks the recipe key, and compares both physical
gradients across all eight projection aliases. The measured Lift01 source has three
4096 x 4096 color/normal/mask channels and no authored control channel (the shared
producer supplies its constant neutral control). Its extracted 432 x 860 repeat unit preserves exactly 216
texels/metre transversely and 215 texels/metre longitudinally; block alignment adds
no rounding for this unit. The resulting native payload additionally passes the
source-density dimensions, template, channel format and complete mip upload checks.
With `--native-mesh '<LiftBridge01 Mesh.npz>'`, the checker also solves the actual
X/Z-to-UV Jacobian of measured native faces 11144 and 11145 and requires exact
agreement with the immutable physical density. The wrapper enables this comparison;
the report records the mesh hash and both face Jacobians.

`Prepare.ps1` runs extraction, density validation, offline baking and native
reader verification in order. Use `-BakerTools` to select a dedicated environment's
`baker/tools` directory. Its optional `-Publish` switch copies only verified
native payloads and CID sidecars into the Lift01 package directory. Existing differing
files cause failure before publication. The script never builds or deploys the mod.

The inventory includes `bandsNull: true`: Unity JsonUtility turns a JSON null array
into an empty array, but the sampler key distinguishes null (`-1`) from empty (`0`).
The baker must restore explicitly marked null bands before calculating either key.
The pixel-product key also excludes the descriptor digest; descriptor identities
must not force identical sampled texture inputs to be baked again.

## Runtime scope

`Lift01RoadPreparation` sends the same eight patches to the shared composer for all
three native road LODs. All patches request repeat and use a cutout, double-sided
native object material; there are no band recipes. The source atlas rectangle is
not a standalone repeat texture, so the constant crop requires offline preparation.
The shared composer binds the prepared key before texture decoding and GPU
composition, preserving the patch's measured U/V frame for each receiver.

Lift01's moving span preserves original selected-road surface types through the
shared native conversion path (`PrototypeRoad` is false). The prepared floor unit
does not authorize replacing selected rails, sleepers or unsupported surface types.
Those retain source appearance, with complete external snapshots per generated owner.

Mechanism structure, glass, far-tower supplement, columns, footings, network pillars
and fixed approaches flow through the common structural material pipeline. Their
original native slots remain direct references where compatible. Density-preserving
structural conversions use a separate measured structural inventory; this road
inventory does not claim to cover those conversions.

## Verification boundary

The extractor checks real source identities, bytes and immutable recipe values.
Native reader/mip validation and packaged-runtime lookup must additionally pass
before this style is ready for human acceptance. Visual repetition, near/far motion,
preview and cold loading require human game verification. No user-visible strings
are changed by this authoring tool; localization behavior is unchanged.

The existing Lift01 Paint, Cable, Road and 26 lane descriptors passed the real native
SurfaceAsset/TextureAsset reader check, including 22,369,632 decoded mip bytes uploaded
unchanged, in `artifacts/prepared-materials/Lift01/existing-native-restored.log`.
The packaged Repeat payload separately passes the native reader: one Surface,
four native textures, 1,985,664 mip bytes uploaded unchanged, and the original
432 x 860 repeat dimensions. A second execution created zero products and reused
all five products. Its complete payload and CID sidecars total 1,046,713 bytes.
Evidence is in `artifacts/prepared-materials/Lift01/repeat-bake.json`,
`repeat-native.log`, `repeat-package.json` and `repeat-density.json`.
The retained `repeat-recipes.json` beside this tool records its exact source recipe.
These checks do not create a game scene or establish visual acceptance. The shared
runtime binding is present in source; deployment and game acceptance remain separate.

The 29 missing Lift01 structural units have also passed the native reader check.
Their 232 payload/identity files total 7,620,509 bytes and remain outside the package
at `artifacts/prepared-materials/Lift01/structures/output` for coordinated integration.
The accompanying inventory, report, native log, hash manifest and reusable product
store are preserved beside the output. Native validation includes 29 structural
descriptors and 36,285,840 uploaded mip bytes together with the existing Lift01
paint/cable/road/lane assets. Repeating preparation creates zero products and reuses
116 products. These staged resources have not been published to `assets/Materials`.
