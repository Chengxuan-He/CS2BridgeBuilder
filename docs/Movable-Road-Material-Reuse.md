# Movable bridge material reuse and conversion

## Material selection

Follow the [material generation contract](agent-contract/MATERIAL_GENERATION_CONTRACT.md)
and [movable surface selection](agent-contract/MOVABLE_BRIDGES_CONTRACT.md#243-material-selection-order).
Choose by actual surface type and physical motion domain, not by the containing section's name.
All asphalt, including empty lanes and parking areas, is road surface. Unsupported moving surface
types and rails/sleepers preserve the selected source appearance; stationary edge abutments keep
their separate rules. Never substitute a visually similar prototype material for missing conversion.

| Content | Required appearance and representation |
| --- | --- |
| Compatible original object material | Direct original reference and compatible UVs; no new material or texture |
| Carried prototype structure | Original material semantics; measured repeat/remapping when width deformation changes density |
| Repeating road or grating | Original compatible texture/material, or an offline-prepared repeat unit at native density |
| Fixed platforms and caps | Separate measured appearance domains, preserved structure and per-side sidewalk layout |
| Rails and sleepers | Selected track's original appearance and UVs; measured native object conversion only when necessary |
| Overhead wires | Selected native cable appearance and measured object conversion; no invented transparent fallback |
| Road paint | Separate single-sided skinned paint on original road bones, 0.001 m upper-normal clearance |
| Variable mixed surfaces | Native composition of required layers; repeating base cannot become a full-span bake |

Full reuse creates no new descriptor, identity or duplicate texture. Constant derived appearances
must be prepared before runtime and packaged by style. Preview/create/regenerate/export copy
finished native bytes and bind them without rebaking or recompression. No first-use or prior-bridge
result cache satisfies that requirement. Preserve both directional texel densities and all channels,
mips, normal encoding and filtering semantics.

## Native cable source evidence

Four measured train/tram cable inputs reference Surface CID
106cc56e9f15a135bb6ea7b99d675c2d, a textureless CurvedTransparent descriptor. Its measured
opaque float32 RGBA is (0.018867909908294678, 0.018867909908294678,
0.018867909908294678, 1); template defaults include metallic 0, smoothness 0.5 and snow 0.
The measured Default object conversion uses a neutral 1x1 control map and
_TANGENTSPACE_OCTO, preserving appearance while removing network curve deformation.
This does not authorize generic conversion of transparent or textured surfaces.

Preserve exact source/version and parameters in the prepared recipe; one appearance can be shared
across LODs within an owner. A matching CID does not authorize borrowing another bridge's files.
MovableCableMaterialCheck.py checks the original 83-byte asset hash and parameter bits. These
checks do not establish complete shader equivalence, motion, wind sway or cold-load acceptance.

## Native road source evidence

The measured native Road material 1d7dad9f64c9fe55a809f1080c39fb92 supplies these worldspace inputs:

| Object property | Native texture CID | Native texture |
| --- | --- | --- |
| _BaseColorMap | b00176216b0ce36579bce6fc675cfb77 | RoadEUWorldspace_BaseColor |
| _NormalMap | 9f446790360787756b0e5909db12e200 | RoadEUWorldspace_Normal |
| _MaskMap | 375b871b66aaf675dacef141eb2a9810 | RoadEUWorldspace_MaskMap |

The inspected 1024x1024 texture and 0.4 UV cycles/metre yield 409.6 texels/metre.
These values describe that source, not all roads or bridge prototypes. Read actual overrides,
worldspace masks and channel scales when identifying a recipe. Matching texture references alone
does not establish equal material parameters, blending or appearance.

Native LOD0 faces 1512/1513 and 1819/1820 in the inspected prototype identify asphalt and grating
regions. Such face identities and Float16 UV bounds belong to exact prototype data, not universal
selection rules. Extract verified atlas interiors offline, excluding paint and pale gutters, and
preserve their physical scale. Continuous Repeat UVs must not be reduced to fractional values at
vertices. Do not cut receiver triangles at texture periods or expand a repeat unit across the span.

An inspection found 91 curved/object candidates by texture-reference sets. Those candidates are
evidence for further comparison, not permission to substitute materials without matching template
defaults, overrides, stack order, channels and sampling. Missing required conversion fails explicitly.

## Native paint source evidence

NoParkingStripingDecal01 has Surface CID 4b5500b1b5c270890d99149f7f62e51a, template 18,
and a measured 2048x2048 original atlas. White source pixels occupy rectangle
(13.5, 1600.5) to (15.5, 1620.5); yellow occupies (390.5, 30.5) to (410.5, 40.5).
Keep these measured coordinates in source data. Resolve actual VT backing during offline
preparation rather than serializing an unavailable authoring Texture CID.

Prepare the required native skinned object appearance and texture at source resolution offline.
Polygon footprints supply the marking outline; bind them to the road's original bones with
single-sided rendering. No child decal projectors, per-dash Prefabs, receiver cuts, coplanar
full-road paint overlays or structural receiver-only material copies are permitted.
The measured 46-rectangle LaneLine approximation is not proof of exact complex symbols,
endpoint fading or all native decal behavior. Those require separate implementation and acceptance.

## Persistence and implementation status

Built-in original materials remain references. Unchanged external dependencies and prepared
resources retain byte-identical, original-CID snapshots under each owner. Copy all required
texture/VT/mip dependencies, preserve source files and never read a deleted bridge's resource.
Preview resources belong to the private preview lifetime; failed generation cleans only its own
partial output. Persisted assets must operate without manager callbacks after a cold start.

The required offline migration is not complete. [Prepared materials](Prepared-Materials.md)
records packaging, actual resource availability and remaining runtime binding/baking work.
This document specifies the target behavior and source evidence; it does not claim that every
current material path already avoids runtime baking or that all styles passed visual acceptance.

Verify original identity for full reuse, exact packaged bytes, per-axis output density, every LOD,
paint visibility, source isolation and independent persistence. Measure native decoding, composition,
readback, texture/VT writing and publication separately on fresh comparable requests. A source audit,
successful build or smaller texture page does not establish visual parity or a game speedup.
