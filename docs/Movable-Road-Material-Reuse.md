# Movable Bridge Existing Material Implementation

## Material selection

Preview and formal creation now reference existing materials. The generation path performs no
moving-road or marking texture baking, material cloning, template rewriting, mip generation or
material serialization. This replaces the earlier conditional reuse policy.

| Content | Existing material selected | Generated data |
| --- | --- | --- |
| Native structure | Original prototype materials at every LOD | Width-adjusted structure geometry |
| Object-compatible road surfaces | Original source material and UVs | Moving geometry and skinning |
| Network carriageway | Prototype asphalt atlas region | Selected-road geometry and remapped UVs |
| Network moving walkways | Prototype metal-grating atlas region | Selected-road geometry and remapped UVs |
| Fixed end walkways | Prototype opaque paving atlas region | Selected-road geometry and remapped UVs |
| Curved lane geometry | Measured native object counterpart with matching texture references; otherwise prototype material | Evaluated lane geometry and skinning |
| Lane lines | Existing parking decal material, white/yellow paint regions | Approximate stroke coverage and bone attachments |
| Other paint decals | Existing parking decal stripe presentation | Segmented native object decals |
| Deterioration, pavement transitions and rail-bed blending | Appearance supplied by existing bridge road surface | No separately reconstructed blend overlay |

Matching texture references does not prove identical coefficients. The selected existing material
supplies its own coefficients. Unmatched curved lane surfaces receive the prototype material with
their evaluated UVs; exact rail/fence appearance is not established. Decorative wear and transition
blending differ from the ordinary road. Native traffic lanes remain independent of these visuals.

## Native asset evidence

The existing `NoParkingStripingDecal01` SurfaceAsset CID is
`4b5500b1b5c270890d99149f7f62e51a`, template 18. Its original atlas is 2048 by 2048.
White paint uses pixel rectangle `(13.5, 1600.5)` to `(15.5, 1620.5)`; yellow paint uses
`(390.5, 30.5)` to `(410.5, 40.5)`. Original BC7 pixels and alpha were inspected offline.
No copied atlas is exported.

Forty-six connected rectangles, measured from the native LaneLine atlas at 512 by 128 inspection
resolution, approximate line/dash coverage. They are geometry masks, not new textures. Runtime
intersects them with source decal UV intervals and longitudinal repetition, then clips actual
receivers by stroke and native bone domain. Existing bounded chord subdivision handles curves.
Fine wear, triangular paint silhouettes, endpoint fading and other pattern details can differ.
Other paint types currently use a stripe approximation rather than their original full pattern.

Native LOD0 faces 1512/1513 provide the current prototype's asphalt UV rectangle; 1819/1820
provide grating. Their measured Float16 UV bounds are embedded in the generator. Material regions
use measured curb positions, without a section-Prefab replacement table. Geometry is still
extracted from the selected road, including components absent from the prototype. Visual acceptance
of every component is not established by that extraction.

Ninety-one native curved-to-object material pairs were found by inspecting texture-reference sets.
The resolver uses original CIDs and does not search generated bridges for donors. Unknown or
external curved inputs use the prototype object material.

## Persistence and performance

Built-in materials remain references. Unchanged external dependencies retain the byte-identical
per-owner snapshot rules. Generated geometry, Prefabs, UVs, skinning and placements are persisted.
Existing materials are not added to the owned newly-created asset list. Preview uses the same
selection without saving formal bridge assets.

Unused appearance-atlas, pixel-evaluator, texture-writer and material-descriptor writer code has
been removed. The compute loader remains for GPU preview skin bounds. The old embedded bundle
still contains unused appearance kernels; no generation caller dispatches them. No new rendering
shader was introduced. Geometry extraction, clipping and native asset serialization remain costs.

Removing baking removes the old atlas/upload/save stage. It does not prove a particular total
speedup or comparable total Prefab size without newly measured output.

## Verification

- Offline checks verified all 91 native material pairs against source texture references and
  object templates, and verified nonzero alpha across both selected paint patches.
- Release build passed; all 12 installed payload files match the build output.
- All 18 original installation files were backed up and verified in
  `artifacts/backups/existing-materials-only-20261017-011607`.
- Draw01 branch commit `b122412` was integrated by exact patch into the dev workspace.
- Road render callers, lane conversion, markings, preview capture and persistence allocation were
  reviewed. Structural-only callers retain original materials.
- Human in-game visuals, opening animation, near/far rendering, cold loading, timings and total
  dependency size remain unverified. Old saved bridges retain their previous generated resources.
- Earlier affected-asset cleanup was rejected by automatic execution policy (`blocked by policy`).
  This change has not removed or migrated those assets. Evaluate newly created bridge assets.

## Fixed platforms and marking batches

The fixed-platform partition and grouped marking persistence now follow
[the measured repair record](Movable-Fixed-Platforms-And-Generation-Cost.md). Fixed bridge ends no
longer select the grating patch. Matching paint volumes share a Prefab per bone/render-settings group.
