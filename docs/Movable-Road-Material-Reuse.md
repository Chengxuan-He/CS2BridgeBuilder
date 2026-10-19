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


## Native tiled carriageway descriptor (2026-10-17)

The approved descriptor exception supersedes the earlier absolute no-descriptor statement for
carriageway asphalt only. The current Draw01 route creates one native `Default` surface in the
bridge owner's namespace, shared by all three LODs. It uses the native `Road` material
`1d7dad9f64c9fe55a809f1080c39fb92` worldspace texture layer:

| Object property | Existing texture CID | Native texture |
| --- | --- | --- |
| `_BaseColorMap` | `b00176216b0ce36579bce6fc675cfb77` | RoadEUWorldspace_BaseColor |
| `_NormalMap` | `9f446790360787756b0e5909db12e200` | RoadEUWorldspace_Normal |
| `_MaskMap` | `375b871b66aaf675dacef141eb2a9810` | RoadEUWorldspace_MaskMap |

All three payloads were resolved in the installed native packages. This is the native EU Road
appearance, not an implementation of arbitrary selected-road worldspace blending, wear or tint.
The source worldspace UV scale (template default 0.4 cycles/metre unless overridden) is encoded
in closed object-local UVs. The skin carries those coordinates during opening; no world-position
sampling is needed. Carriageway domains 0, 5, 7 and 12 use whole-texture continuous sampling.
The superseded far asphalt atlas projection is removed. Fixed caps, grating and paint retain
their prior materials and atlas behavior; their existing geometric partitions still remain.

The helper saves with `saveTextures: false` and `vt: false`: no texture saving, compression, VT
production or pixel baking. Formal descriptors use the owner-prefixed BridgeBuilder resource
path; existing dependency traversal reads their texture CIDs and stops at built-in resources.
Preview descriptors use PreviewGeometry.NativeAssets and are deleted with that database. A
failed generation deletes the newly allocated descriptor. Existing assets are not migrated.

Release compilation passed. Actual new triangle counts, appearance at all distances, native
opening and cold loading with the mod disabled require newly created in-game samples. Compilation
and the reference audit do not establish these results.

## Approved GPU composition from existing textures (2026-10-17)

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.


## Native asphalt density approval and implementation (2026-10-18)

The user authorizes non-VT repeating asphalt and separate paint sampling, prioritizing
native road density over the previous combined-VT requirement. The inspected road uses
1024 by 1024 source textures and 0.4 UV units per metre (409.6 texels per metre).

The asphalt descriptor retains the native albedo, normal and mask texture references.
Continuous projected UVs do not take fractional values at vertices. Asphalt is excluded
from the paint chart rasterization; the chart remains transparent outside paint. The
cutout batch and opaque asphalt batch share receiver positions, triangulation and bone
ownership, with no depth offset, road cuts or separate marking objects. This adds receiver
triangles for the second material pass; it does not claim a zero-triangle increase.
Two composed pages leave room for the repeating asphalt batch at each LOD within the
native group limit. The repeating descriptor is now retained as a saved dependency.

Build validation is not visual acceptance. Coplanar marking depth behavior, native cold
loading, near/far appearance and the generated face counts require a new human-created
sample. This change preserves native asphalt density; it does not establish native marking
texel density or complete appearance parity.
