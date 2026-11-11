# Native163 preview material investigation

## Status

Native163 is a targeted diagnostic build, not a verified material repair. It
preserves native162 shader packages, generation, geometry and rendering behavior.
The reported new preview remains unresolved pending its actual render capture.
No saved bridge was modified. No game or launcher was started or stopped.

## Reported request and established evidence

The captured user screenshot shows Draw01 (Curved-Soffit Girder Bascule Bridge)
with Eight-Lane Divided Road, an open mechanism, pale roadway and a dark central
longitudinal region. This is a newly generated preview, not an old saved owner.
The corresponding request in BridgeBuilder.log is
`tmpa26af602-5477-4765-9d3c-6b3f058ea8ac`, preview revision 13. Generation and render
completed successfully; the render finished at 19:33:08 on 2026-11-11. The game
later logged disposal at 19:34:38. No matching shader/cbuffer exception appears
in the retained Player.log.

The selected elevated road includes Median Piece 2 and Elevated Bottom Piece 2.
The actual median source Geometry is `61ff20f7140db6ddb6fe95db7533d170`, with
252 vertices and 224 triangles, transverse extent [-1, 1]. Its Surface is
`1d7dad9f64c9fe55a809f1080c39fb92`. Source counts and shared Original routing do
not prove the final preview's per-face coverage or material binding.

Native162 tested actual BatchRendererGroup GBuffer/DBuffer submission. The preview
instead uses ordinary MeshRenderers with private materials, property blocks,
open-pose bones and a stage translated to (0, -1000, 0). It captures HDRP's shaded
camera result and separately redraws Forward coverage to construct PNG alpha.
Those are materially different submission and output paths.

A bounded non-DOTS Forward probe reused the previously captured native162
Original geometry and original six BC7 textures. It produced 21,648 covered
pixels, each with alpha 1, matching that sample's GBuffer coverage. This excludes
an unconditional zero-alpha failure in that tested variant. Its RGB contained
NaNs because the minimal isolated host does not provide the complete HDRP
lighting inputs. It is not a successful appearance test, not the new Draw01
preview, and not evidence that the reported defect is fixed.

## Diagnostic implementation

The maintained generated-material design was updated before code. A one-shot
request file in the game's own ModsData directory now arms capture of the next
preview containing a generated material program. A fixed catalogue preview does
not consume the request. Consuming the request moves it into a new diagnostic
directory; no saved-owner file is accessed or changed.

After the first completed, warmed preview readback, capture records:

- Linear HDR shaded pixels as a float EXR and separate Forward coverage as PNG.
- Exact raw mesh vertex streams, attribute layouts/offsets/strides, all submesh
  indices and topology in BPM1 binary files, deduplicated per mesh.
- Every declared material property, keyword, pass state, queue, texture identity,
  dimensions, format and sampling metadata; per-draw declared property overrides.
- Surface CIDs, template IDs, VT stack identities, private-material IDs, packing
  mode and native-lane context.
- Draw and camera transforms, projection, bounds, stage origin, current bone
  ranges and the actual open-pose skin matrices.

The operation reuses existing completed image readbacks. It does not issue a
replacement draw, adjust shader state, regenerate textures or modify sources.
Diagnostic exceptions are logged and do not fail normal preview completion.
The capture has additional one-time mesh/file IO cost and must not be used as an
unqualified preview-performance measurement. It is not enabled for every request.

## Verification

The actual capture source was linked into an isolated Unity Editor test host;
only the namespace syntax was adapted to that host's older C# parser. Real Unity
mesh and texture APIs verified two-submesh index roundtripping, raw stream output,
shared mesh/material deduplication, HDR and coverage encoding, nonzero transform
and bone recording, preservation of an armed request for an empty preview, and
no second write after completion. Stub draw-list inputs in this test do not prove
the game's full preview. The production draw-list/Surface integration separately
passed the full game-referenced build.

Main-tree and frozen builds passed, along with the existing test suite,
twelve-language localization checks and `git diff --check`. No new user-visible
text was added; diagnostic files/logs are English. The four existing nullable
warnings in movable generation remain unrelated to this diagnostic change.

## Installation and next observation

The installed DLL SHA256 is
`061115D55517EC8B4AF9053E861D0E27C3F9D6CBFC413A709E0F114CEB7316C2`.
The deployment verified 964 installed files, changed only DLL/PDB and backed up
both originals. There were no added or removed installation files. The official
SDK package is 77,906,495 bytes, above the existing publication budget; nothing
was published or pushed.

A single capture request was armed while Cities2 was absent. The user must
manually open the creation panel and select the same Draw01/eight-lane combination,
then allow preview completion. Creating or deleting a bridge is unnecessary.
The data will be under
`ModsData/BridgeBuilder/PreviewMaterialCaptures/<capture-id>/`.
The next investigation must compare the shaded and coverage images and map the
actual captured median indices/material/pose before changing rendering behavior.

Evidence is retained under `work/material163/`, including the pre-install game
logs, probe source/logs, capture test, builds and localization output. The frozen
candidate, original installation backup and deployment receipt are under
`artifacts/material-remap/shared/native163/`.
