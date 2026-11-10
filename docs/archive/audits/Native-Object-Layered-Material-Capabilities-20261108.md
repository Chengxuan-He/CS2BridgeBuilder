# Native object layered-material capability audit — 2026-11-08

This is read-only evidence for the native136 UV-density report, not a repair,
implementation requirement, or game visual acceptance. No production code, assets,
installed files, validation policy, or player bridge was changed.

## Exhaustive metadata scope

All **40 MaterialLibrary template entries / 26 distinct shaders** in the currently
installed `Cities2_Data/resources.assets` were inventoried. Their complete retained
shader metadata was compared with the current asset and matched. This covers the
registered native library available in that asset, not unregistered shaders, arbitrary
third-party bundles, every engine render path, or all possible future template IDs.
The input asset is 221,795,376 bytes. Texture declarations, coordinate properties,
keyword declarations, passes and compiled D3D vertex variants were inspected.
A declared keyword is not proof that a compiled usable variant exists.

| Shader path ID | Native templates | Compiled procedural-bone D3D vertex variant | Assessment |
| --- | --- | --- | --- |
| 549 | 21: CurvedOverlay | False | Curve/overlay buffers; one MainTex; not a second PBR/mask evaluator. |
| 550 | 2: Windows | True | Window/interior parallax/lighting, not independent complete road PBR blend. |
| 553 | 4: VegLeaves | False | Vegetation plus snow/wind protocol; not arbitrary second PBR. |
| 554 | 6: CharacterBody, 9: CharacterHead | False | Character overlay atlas/records and skin/micro-normal logic; not complete road PBR mix. |
| 559 | 7: CharacterDeformable, 38: CharacterDeformableCutout | False | One PBR family plus alpha mask; shape/body protocol. |
| 560 | 5: VegImpostor | False | Impostor normal/depth and vegetation protocol. |
| 561 | 27: CharacterGenericTransparent | False | One PBR family plus character transparency. |
| 566 | 39: Placeholder | False | Placeholder; no source PBR layer inputs. |
| 567 | 24: Base | False | One PBR family plus weather; no arbitrary second PBR/mask. |
| 568 | 8: CharacterEye | False | Specialized eye textures/refraction; no complete two-road-PBR graph. |
| 570 | 16: Curved, 30: CurvedTransparent, 31: CurvedCutout, 34: CurvedHashedCutout | False | One PBR family, network curve buffers required. |
| 571 | 0: Default, 1: DefaultCutout, 13: DefaultDoubleSided, 14: DefaultCutoutDoubleSided, 35: DefaultHashedCutout, 36: DefaultHashedCutoutDoubleSided | True | One base PBR family plus weather snow. Snow is not source Mask.B world PBR. |
| 572 | 20: CurvedDecal, 32: CurvedDecalColorOnly | False | One projected PBR family; curve/decal buffer protocol. |
| 575 | 23: CurvedPipeline | False | Pipeline visualization; no complete PBR texture family. |
| 576 | 11: DefaultGlassDoubleSided, 15: DefaultGlass | True | One glass PBR family; not original two-layer road blend. |
| 578 | 19: CurvedDecalDeterioration | False | One deterioration decal PBR family and curve protocol. |
| 580 | 12: Grass | False | Terrain/grass/weather sampling; not independent road layers. |
| 581 | 17: NetCompositionMeshLit, 33: NetCompositionMeshLitCutout | False | Exact source two-PBR world blend, but network composition vertex protocol, not bone object. |
| 583 | 3: VegRoot | False | Vegetation root/weather protocol; no arbitrary second PBR. |
| 584 | 26: CharacterGeneric | False | One PBR family plus character attributes. |
| 586 | 22: DefaultPipeline | False | Pipeline visualization; no complete PBR texture family. |
| 587 | 18: DefaultDecal | False | One projected PBR family; DBuffer mesh/projector writes, no independent second mask UV. |
| 588 | 25: HairApproximateDyed | False | Hair alpha/depth/masks and character protocol. |
| 593 | 28: WaterGeneric, 29: WaterStream | False | Water normal waves; no two complete PBR texture families. |
| 594 | 10: CharacterMouth | False | Mouth PBR/subsurface and character protocol. |
| 596 | 37: CharacterEyelashes | False | Eyelash base/alpha with character protocol. |

## Actual-program investigation beyond property names

The candidate paths were inspected as real extracted D3D DXBC, not only property
lists. This included vertex and fragment programs for Windows (550), CharacterBody
(554), NetComposition (581), CurvedOverlay (549), DefaultDecal Mesh and Projector
passes (587), and Default (571), including a procedural vertex variant and a
non-low-quality weather fragment. The selected variants are recorded with blob
indices/keywords; this is not an execution or disassembly of every permutation.

### Default weather/snow is not the missing generic layer

Default has additional snow textures, so describing it as having only three texture
slots would be wrong. Its inspected non-low-quality fragment computes snow addressing
from world position using the literal 0.03 rate; its snow amount derives from weather,
instance inputs, orientation and nonlinear shaping. It blends snow-specific normals
and scalar properties. It does not expose the source's independently configurable
WorldspaceUVScale and complete world PBR/mask tuple controlled directly by base
MaskMap.B. Changing UV0 cannot change that hardcoded snow coordinate path. Reusing
snow as the road layer would alter weather, blend and PBR semantics, not convert the
reported source by changing UVs.

### Character/window extra textures are specialized

CharacterBody's real fragment reads structured overlay records (stride 32), a texture
array/atlas, alpha/skin and micro-normal data. Those are not arbitrary pairs of complete
road PBR layers with the required independent mask coordinates. Its shader metadata
declares procedural animation, but no compiled GBuffer D3D vertex variant with that
keyword was found; the selected actual variant uses shape animation. Windows has an
actual procedural variant, but its extra layer implements interior/parallax/emissive
window behavior, not the required road albedo+normal+mask blend. Extra textures or
UV inputs alone do not establish equivalent output.

### MeshDecal/projector/overlay does not add a second sample to the same primitive

The actual DefaultDecal DBufferMesh fragment samples base color, normal and mask with
the same projected coordinate (`r4.xz` in the retained DXBC). It obtains receiver
position from scene depth and native instance/projection transforms. The corresponding
vertex input has position/tangent/UV/instance ID, with no procedural bone-index input.
The shader has DBufferProjector and DBufferMesh passes, rather than a second layered
road-PBR operation within the object's original opaque pass. Its property set lacks an
independent source blend-mask sampler/coordinate family. CurvedOverlay uses one MainTex
and curve/height inputs, not the required PBR pair.

Adding an extra decal draw would require its own receiver coverage, transforms and
blending semantics. It is not evidence for the requested same-primitive, no-extra-layer
solution. No new decal/overlay prototype or coplanar geometry was created. Existing
native object parenting possibilities are not globally disproved by this audit, but
they do not add the absent second sampling/mask operation to these inspected shaders.

### Original network shader has the right pixel formula, not the object protocol

NetComposition's actual fragment performs the required base UV + world-XZ PBR blend.
Its actual vertex program consumes composition matrices/selectors and synchronization;
its metadata/compiled D3D variants do not expose procedural object-bone animation.
Native BatchDataHelpers.CompositionParameters has composition matrices and sync fields,
not the procedural bone parameter protocol. BatchDataSystem selects native Object,
Net and Lane writers separately. Persisting UV2 or same-named material vectors does
not supply those network buffers or update them as a leaf moves. No verified static
persistent adapter was found that supplies both protocols without synchronization.
This rules out blind binding to the existing object layout, not every hypothetical
native graph/import mechanism outside the inspected paths.

## Completed final-coordinate audit for the actual sample

Source `40dd255c63eb8f65f96c5333d7d28afb` is template17, non-VT, with six PBR texture
references and no float/vector overrides. Current template17 resolves world scale to
0.4000000059604645. Generated Surface `ab6ef836caf87d597e45f8adc60b25a7` is template13,
non-VT, with original base PBR identities plus zero control and no vector overrides.
Current template13 has UV0Scale=(1,1) and UV0Offset=(0,0). Both current template objects
were compared against retained complete metadata and match. The actual Default
fragment computes `uploadedUV * UV0Scale + UV0Offset` before PBR sampling.
Consequently no hidden target-default scale or offset was found: final base sampling
retains uploaded UV. The three saved road LODs have approximately 0.125/0.03125 UV/m,
or 128 texels/m for the retained 1024x4096 base image. The independent world image's
409.6 texels/m is a different layer, not a correction factor for that base UV.

The source-to-final same-point composition input snapshot is unavailable. Original
TiledLane geometry and the native formula corroborate the coordinate rate; this is
not a proof of every vertex/occurrence/node mapping. No multiplier was changed.

## Current unresolved requirement intersection

The demonstrated source needs two independent coordinate/sample fields and a blend
mask; the current receiving object protocol evaluates one retained base PBR family.
The task simultaneously retains: only coordinate changes, unchanged textures/layers,
no baking, no approximate domain selection, no custom shader, no mod-dependent runtime
synchronization, and independently persistent moving assets. No inspected native
object representation satisfies that combination. UV changes can change coordinates
of an existing sample, but cannot add another texture sample or its blend operation.
The smallest missing capability is a persistable moving receiver whose shader already
performs the source's two-field PBR/mask formula. None was established here.

## Evidence locations and limits

`work/uv137/all-shader-capabilities.json` contains the complete 40/26 inventory.
`capability-programs/` holds current extracted DXBC, disassembly, variant metadata;
`source-output-descriptors.json` records complete source/output properties;
`current-template-check.json` records current native template hashes. Earlier
`fragment.asm`, `target-fragment.asm`, saved Geometry decoding and source texture
samples remain separate evidence. The display-only UV multiplier image is a hypothetical
albedo diagnostic, not a native rendering test, authorized change, or recommendation.

No game/launcher was started or stopped. No build/deployment was performed. The UV-density
bug remains unresolved; this audit must not be presented as a shipped fix.
