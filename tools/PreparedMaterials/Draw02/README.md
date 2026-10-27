# Draw02 prepared repeating road materials

`ExportRecipes.py` reads the actual Draw02 BBN1 array, the measured native prefab
audit and original game Surface bytes. It writes four distinct pixel recipes and
five descriptors using `BridgeBuilder/RoadRepeat/1`. No new runtime numerical
constants, geometry, material slots or language strings are introduced.

The moving carriageway uses the original three transverse bands and exact binary32
division. Its opaque and cutout descriptors must reference identical textures.
The opaque descriptor is required by homogeneous selected-road asphalt promotion;
ordinary prototype-patch routing uses the cutout descriptor. Moving sidewalk,
fixed road and fixed sidewalk use three further cutout units. The four measured
projections have sixteen half/side aliases; receiver orientation and size do not
change a prepared unit's pixels.

`VerifyRecipes.py` compares both physical UV axes with eleven identified native
DrawBridge02 faces. The maximum relative two-dimensional texel metric discrepancy
is 0.000080217764, from the source's half-precision strip coordinate asymmetry.
The check records that discrepancy and does not modify it. It also checks the
actual Draw02 sampling route through the shared composer and recipe-key parity.
This source check is not verification of output texture pixels or game appearance.

## Authoring and verification

Run the exporter with repository, game-data directory, native audit, structural
source inventory and output JSON paths. Run the verifier with repository, native
mesh NPZ, that inventory and report paths. Use the shared `tools/Prepare-Repeats.ps1`
with an independent Unity project, output, log and writable product store.

The authored output root contains `Draw02/Repeat/<key>/Repeat.Surface` and its
dependencies. Validate it with the native Repeat reader before copying files into
`assets/Materials/Draw02/`. Do not publish an empty Repeat directory. Preserve
existing Paint, Cable, Lane and Road payloads.

Source evidence is stored in `artifacts/prepared-materials/Draw02/repeat-inventory.json`
and `source-density.json`. Actual preparation and native-reader evidence are in
`artifacts/agent-material-env/Draw02/reports/` and `logs/`. The isolated Unity
2022.3.62f2 execution passed five Surface descriptors and sixteen native textures,
including the shared opaque/cutout texture identities and all 8,670,272 decoded mip
bytes uploaded unchanged. The four unit sizes are 312 by 828, 600 by 828, 604 by 752
and 548 by 752 pixels, measured against 4096 by 4096 source channels. A second
execution created zero products and reused all twenty-five requested products.

The forty-two validated files in `assets/Materials/Draw02/Repeat/` and `Shared/`
total 3,757,870 bytes, including identity sidecars. Every copied file was checked
against its authored SHA256; no empty material directory remains. Existing
Paint/Cable/Road/Lanes also passed the game's native readers: twenty-six lane
descriptors and 22,369,632 uploaded mip bytes, recorded in
`artifacts/prepared-materials/Draw02/existing-native.log`.

The isolated baker normalizes JsonUtility's empty representation of null bands
before hashing and excludes the Surface descriptor digest from per-channel pixel
keys, where decoded channel content already identifies the source. These changes
belong to the coordinating agent's shared integration. `Prepare.ps1` expresses the
shared-tool workflow; the isolated environment's `Run.ps1` records the tested copy.
Neither wrapper publishes artifacts or touches another style's files.

## Structural material preparation

The coordinating agent supplied fifteen missing structural units in the isolated
Draw02 inventory. Their bake and native-reader checks passed. The resulting 130
files total 3,319,929 bytes and are retained outside the disposable environment at
`artifacts/prepared-materials/Draw02/structures/Structure/<key>/`. Each retained
file matches its authored SHA256. They have not been published to the mod assets;
the coordinating agent owns final integration and any shared payload consolidation.

`structure-inventory.json`, `structure-bake.json`, `structure-bake.log` and
`structure-native.log` beside that output preserve evidence. The native check read
all fifteen structural descriptors and the existing twenty-six lane descriptors;
37,057,616 total mip bytes uploaded unchanged. It did not create a game scene.

The shared baker and runtime integration are maintained by the coordinating agent.
Full bridge near/far, motion and cold-load human acceptance remain separate from
these offline preparation and native format checks.
