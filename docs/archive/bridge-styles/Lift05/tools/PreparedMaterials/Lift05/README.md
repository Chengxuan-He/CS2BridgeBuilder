# Lift05 prepared road materials

Read [the latest handoff](LATEST-HANDOFF.md) before using the historical preparation
records below. The formal payload is 84 files / 912,445 bytes; complete generated-owner
material bytes remain unmeasured. The integrated exact source-chart recovery adds no
pixels and still requires actual runtime coverage confirmation.

## Inputs and extraction

`ExportRecipes.py` reads the existing `Types_Lift05_Lift05RoadAppearance_cs.bin`
BBN1 resource without modifying it, validates its checksum and typed lengths, and
extracts the fixed and moving repeat rectangles selected by `Lift05RoadAppearance`.
Half and side projections do not change their pixels: eight appearance regions use
two distinct rectangle recipes. Neither recipe has transverse bands. Both retain
the existing cutout/double-sided native conversion setting. Only the fixed recipe
is currently reachable by prototype material conversion and belongs in `units` for
baking and packaging. Both declared recipes remain in the evidence record.

The original `LiftBridge05 Mesh` material in the native audit is CID
`78fda4223742d922d34b5cc2331eb65f`. Its descriptor SHA256 is
`40c466ebabd798b8bca9674788deefa14c10490f9f011f5377c8e48535bb984e`.
The exporter resolves the original bytes from game/DLC archives and checks the
template against the structural source inventory. It never reads another generated
bridge's resources. Source texture dimensions are obtained by the shared native
decoder during offline preparation, not assumed from the atlas coordinates.

Run from the repository root, using the actual Python executable and evidence paths:

```powershell
python tools/PreparedMaterials/Lift05/ExportRecipes.py . <Cities2_Data> <all-movable-audit.json> artifacts/uv-density/structure-seam-sources.json artifacts/prepared-lift05/repeat-recipes.json
```

The output uses the shared `BridgeBuilder/RoadRepeat/1` recipe schema accepted by
`tools/Prepare-Repeats.ps1`. It records source, runtime-code and BBN1 digests. This
JSON is authoring evidence, not a shipped runtime dependency. Runtime continues to
consume its existing embedded numerical arrays and the prepared native resources.

## Scope and integration

The exporter records declared prototype patches, not arbitrary selected-road
materials, and filters the bake inventory using the verified call chain.
`MovableRoadPreparation` uses the near native material for all road LODs;
original compatible selected-road/track materials remain original. Lift05 is not a
`PrototypeRoad` style in that shared method. Its moving original-surface branch
therefore keeps selected-source appearance, while the fixed transition uses the
prototype patch. Do not broaden prototype replacement to the moving selected road
merely because an unused declared patch has an offline recipe. The check confirms
fixed bone 0/connection 0 and moving bone 2/connection 100 in all three native LODs,
appearance regions 0–3 versus 4–7, moving-surface tagging and original-material
selection. It records call-chain source digests and fails if these assumptions
change. The moving recipe is evidence only and must not be baked or packaged.

Shared runtime lookup and native repeat authoring belong to the coordinating agent.
Use a separate Lift05 Unity project with its own Library/Temp, output and log files.
Equivalent existing products must be reused before any GPU bake; an isolated
project does not authorize rebaking identical resources. Copy validated payloads
into `assets/Materials/Lift05/Repeat/<key>/` with their original prepared CIDs and
complete dependencies. Do not package authoring JSON, logs or empty directories.

After preparation, validate the native Surface/Texture readers, all channel mips,
unchanged packaged hashes and source-density dimensions. Main-agent integration
must verify the prepared key route and absence of runtime rebaking. File and native
reader checks do not establish human near/far, moving-pose or cold-load acceptance.

## Localization

This offline exporter and its evidence introduce no user-visible runtime text or
locale keys. Existing in-game localization remains unchanged.

## Prepared resource and verification evidence

The reachable fixed unit is packaged at
`assets/Materials/Lift05/Repeat/3b59afd3aa0ad2d963d6c6c7ba95cb1d0e33ccd1eec3f4b19e1a324a77551222/Repeat.Surface`.
Its prepared Surface CID is `745ad87530583d799c80237225cd1a94`.
The four channel textures and their CID sidecars reside in Lift05's own `Shared/`
directory. The material graph contains ten files totaling 186,234 bytes;
this is the repeat graph's source-file subtotal, not a total mod-package or generated
bridge footprint. No empty material directories were retained.

The decoded native source is 4096 x 4096. The measured rectangle spans 112 x 625
source pixels; the prepared unit is 112 x 628 after upward BC7 block alignment.
The output retains transverse source density and increases longitudinal sampling
density by approximately 0.48%; it does not downsample or fit a full bridge span.

The initial native reader check passed one Surface, four textures and 377,152 complete mip
bytes uploaded unchanged. It also checked template 14, non-VT sampling, four native
channel bindings, color spaces, `_TANGENTSPACE_OCTO`, `_Snow = 0`, source-density
dimensions and style-local dependency resolution. A second authoring run created
zero products and reused all five existing products; the native reader check passed
again. Source-to-package file hashes were compared for all ten files.

Evidence is under `artifacts/prepared-lift05/`: the exact inventory, initial bake
report/log, native-reader log, reuse report/log and durable product store. Runtime
lookup integration, full generated-bridge tests and human visual/cold-load checks
are separate coordinating-agent responsibilities and are not claimed by these checks.

## Transparent control audit

`Test-RepeatControlAudit.ps1 -Output <new-absolute-candidate-directory>` runs
`RepeatControlAudit.Editor.cs` and `ControlMipRead.shader` in the dedicated Lift05
Unity project. It uses the packaged inventory and native game archives read-only,
checks actual source channels and all control mip pixels, and writes only candidate
Surface descriptors and evidence. It never bakes or publishes texture resources.
The tool computes the material recipe key with the existing authoring implementation,
normalizes JsonUtility empty bands to null, and rejects an unmatched packaged unit.

The audit under `artifacts/prepared-materials/Lift05/repeat-control-final/` passed
source, full-mip and native descriptor readback checks, including a known nonzero
pixel test of the readback path. Its complete repeat unit references the existing
`Road/ZeroControl.Texture` through an identical `Shared/<CID>.Texture` copy: the
source has no authored control and both old/new controls are transparent zero.
The descriptor changes only its control reference; base color, normal and mask
bindings remain byte-identical.

`IntegrateRepeatControl.py` publishes this verified candidate only after checking
all evidence hashes, the exact 16-byte descriptor difference, dependency CIDs and
backup copies. It scans every Lift05 Surface before deleting an obsolete control.
The integration checked all 30 packaged Surfaces, backed up four original files,
removed the old 74-byte texture/sidecar pair and added a 54-byte Shared zero pair.
The Lift05 source material directory therefore decreases by 20 bytes overall.
The machine report is
`artifacts/prepared-materials/Lift05/repeat-zero-control-integration.json`;
backups contain original files only under
`artifacts/backups/Lift05/repeat-zero-control-integration/`.
The independent environment, build output and game installation are untouched by
this asset integration. No user-visible mod text or localization behavior changes.

