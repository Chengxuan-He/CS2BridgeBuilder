# Draw03 native layer and atlas sampling investigation

## Result and scope

No verified native shader-only representation replaces the Draw03 cross-band bake
without actual appearance-domain partitioning while retaining original channels,
motion and source sampling density. This result follows inspection of the game's 40
material templates and their 26 shader interfaces, selected native DXBC programs,
and the material/VT/overlay loading code. It is not a proof about every possible
shader technique or future game version. No template was substituted in production,
no source texture was modified, and no additional baking was performed.

The reproducible inventory is `InspectNativeLayers.py`; its measured output is
`artifacts/prepared-materials/Draw03/native-layer-capabilities.json`. It records
every template, shader texture/property interface, animation keyword and the
SHA-256 of the inspected metadata and supporting extracted native sources.

## Candidates inspected

| Native family | Observed interface | Consequence for Draw03 |
| --- | --- | --- |
| Default, including cutout/double-sided/hashed variants (templates 0, 1, 13, 14, 35, 36; shader 571) | One base/normal/mask/control/emissive family and native procedural animation. `_UV0Scale` and `_UV0Offset`. | Supports prepared independent repeat units, but no arbitrary band selector or independent lane/gap phase maps. |
| NetCompositionMeshLit (17, 33; shader 581) | A base family plus worldspace albedo/normal/mask, `_WorldspaceUVScale`, and composition vertex-sync data. No `_GPU_ANIMATION_*` or `colossal_BoneParameters` interface. | Its second family is sampled in world XZ and mixed with the base using the native mask blue channel. It does not expose the moving local frame and per-band phases required here. |
| Curved (16, 30, 31, 34; shader 570) | One base family and network geometry keywords; no native object animation interface. | Does not add the missing layer selector and cannot simply replace the moving object's material. |
| Decal/curved decal/deterioration/overlay (18, 19, 20, 21, 32) | Texture-area controls and specialized native decal/network paths, without the object's procedural animation interface. | Not an equivalent material on the existing skinned receiver. Child projectors also violate the measured motion requirement; wear geometry is not the marking-footprint exception. |
| CharacterSkin (6, 9; shader 554) | `_OverlayAtlas`, `colossal_OverlayParameters`, normal/micro-normal, roughness/cavity/transmission and character controls. Supports animation. | Has real native overlay metadata, but not independent road base/normal/mask/control layer families or a verified local-repeat sampler. See below. |
| Windows (2; shader 550) | Interior atlas/randomization and window visibility, alongside one base/normal/mask family. Supports animation. | Interior rendering is not a second full road-material family; no arbitrary lateral band/phase interface. |
| Other character, glass, vegetation, water, pipeline, grass and fallback templates | Fully enumerated in the JSON inventory. Some support animation, others use vegetation/network deformation. | No additional interface was found combining the required full material channels, local-repeat band selection and native road motion. These were interface checks, not in-game equivalence tests. |

`DefaultPVTStack` and `ExtendedPVTStack` do not supply two interchangeable road
layers. The material library declares the first as base/normal/mask/control
channels and the second as emissive. Snow/rust/window/character special-purpose
inputs must not be repurposed without proving the complete original appearance,
including weather, masks and normals. Merely finding several texture properties
does not establish that proof.

## Native evidence

The extracted research directory contains the following relevant files:

- `shader-programs/581-133.asm`: the network fragment program samples its base
  texture at receiver UV, samples its worldspace family at world XZ multiplied
  by a scalar, and blends with the base mask's blue component. It does not use
  an independently specified affine frame for each lane.
- `Game/Game.Rendering/BatchDataHelpers.cs`: `m_CompositionSync0..3` are populated
  from `m_SyncVertexOffsetsLeft/Right` and node sync vertex targets. They are
  network vertex synchronization data, not spare material-layer descriptors.
- `Game/Game.Rendering/RenderingUtils.cs`, `GetOverlayAtlasElements`: native
  overlay records come from `CharacterProperties.m_Overlays`.
- `Game/Game.Rendering/OverlayAtlasElement.cs`: the persistent rectangle pair
  yields a scale and translation. `TransformUV` clamps to the source rectangle
  before applying the affine transform; no repeat-period field is present.
- `Game/Game.Rendering/BatchMeshSystem.cs`, `AddOverlayData` and
  `SetOverlayParameters`: overlay records are uploaded into a native graphics
  buffer and the per-material values encode allocation start and stride.
  They are not arbitrary persistent band vectors in a Surface descriptor.

The character overlay mechanism is therefore a real capability, not an ignored
candidate. However, adopting it would require proving a different channel model,
character component/render behavior, boundary repeat sampling and all mip/weather
semantics. Its exposed rectangle mapper by itself does not solve Draw03. No such
equivalence is established by this investigation.

## Can an original atlas island repeat without a copied island texture?

For the compatible non-VT Default path, the inspected `571-2701.asm` applies one
affine UV mapping and immediately samples the texture family. Hardware Repeat
wraps the complete bound texture. A scale/offset alone therefore wraps the whole
atlas, not an island. Required sampling is
`islandOrigin + islandExtent * frac(localUV)`; that additional local-wrap stage
was not found in this path. Repeating UV only at vertices is not equivalent on
triangles that cross a period.

Native VT does expose atlas transforms, but these refer to the registered whole
texture's allocation in the global VT atlas:

- `VTDatabase.GetTextureParamBlock` builds its transform from
  `GetAtlassedRect(indexInStack)` and its info from allocation dimensions and
  address mode.
- `SurfaceAsset` obtains those blocks from native streaming registrations and
  calls `BindMaterial`; `SyncToUnityMaterial` also reapplies the registered blocks.
- `SurfaceAssetVTHeader` persists source/preprocessed texture IDs and stack sizes,
  not arbitrary rectangular or triangular subtexture views.

Thus `DefaultPVTStack_atlasParams0/1` are not a verified persistent public shortcut
for selecting an island of an existing original atlas. Overwriting them after
loading would introduce a runtime binding dependency, and must not be mistaken
for an independently saved material. A triangle-shaped local-repeat view is also
absent from the inspected interfaces. There has been no GPU experiment claiming
to prove that unsupported manipulation correct.

## Implementation consequence

The native Default material can sample an immutable lane/gap unit after an actual
appearance-domain partition has established which unit and phase each fragment
owns. `Draw03RoadWearDomains` implements this permitted partition with the common
clipper; [integration evidence](Integration.md) records its scope and native checks.
It does not subdivide at texture periods, add a custom shader or discard wear.

The missing shader operation is per-fragment selection of multiple immutable units
inside one unchanged triangle. None of the inspected compatible interfaces exposes
it. This limitation does not prohibit the contract's actual appearance partitions.
Likewise, the audit does not establish that one texture per arbitrary atlas island
is required. Deduplicate equivalent inputs and retain compatible original references.

## Verification and localization

The inspection script completed successfully against the installed assets and
reported 40 templates / 26 shaders. The report is an interface and source audit,
not rendering acceptance. The report/tool introduces no game UI strings and does
not change runtime localization, generated asset names or language resources.

## Existing material partitions and skinned paint

The production path was also checked for alternatives that avoid changing the
native shader:

- `MovableRoadModel.TryBuild` assigns all three indices of each input triangle to
  `slots[geometry.Material]`, then creates one triangle-list submesh per slot.
  `MovableSurfaceAtlas.Compose` likewise emits one material index and one affine
  UV map for each output triangle. Additional material slots or native LOD groups
  do not provide per-fragment material selection inside that triangle.
- `Draw03RoadPartitions.Carriageway` describes the longitudinal fixed sections,
  leaves and opening gaps. `Draw03RoadAppearance.Regions` adds measured longitudinal
  appearance zones and left/right domains. It receives a pose and sidewalk flag,
  not the selected road's lane centers. Existing cuts therefore cannot guarantee
  alignment with every road-dependent lateral wear-band boundary. Individual faces
  that happen to fit already benefit from `Draw03RoadWearUnits.TryReduce`.
- `Draw03ObjectAssembly` calls `AttachMovablePaint` with selected-road decal
  batches. `TowerFactory.MovablePaint` creates polygons from those real marking
  footprints, partitions the new polygons at physical motion seams, and binds the
  original skeleton. It assigns every vertex a single white or yellow sample UV,
  material index zero, and one prepared paint Surface. This is an additional
  geometry path, not a spare arbitrary-texture layer on the road material.

Reusing the paint builder unchanged cannot render the lane unit: its UVs sample
one color point. Extending it to render wear requires new wear polygons, lane/gap
material slots and density-preserving UVs. Repeating a lane unit over a duplicate
whole-road triangle would create unwanted lanes outside the intended band; the
Default shader has no independent per-band clipping interface. Cutting only the
new overlay polygons still adds appearance geometry and does not become an
existing marking footprint by using the paint class.

An overlay also has unresolved correctness costs. The existing 0.001 m clearance
is specifically authorized for paint. Putting a wear layer at the same clearance
can collide with paint; putting it on the receiver risks depth fighting. Creating
one rectangle per lane is insufficient to follow arbitrary nonplanar road geometry
and unsupported native surface masks. A road-wide replacement layer would require
both lane and gap domains: gaps restart their measured repeat phase independently,
so one global gap underlay is not generally equivalent. No overlay implementation
or visual equivalence is claimed.

## Actual appearance-domain implementation

For the pure asphalt cross-band case, the implemented direct solution is a
material-domain split at each actual band boundary, followed by selection of the
already prepared lane or gap unit. It reuses the existing appearance-domain
clipping machinery and the reducer's affine scale/phase formulas:

1. Construct the selected layout's lane/gap intervals from the existing measured
   Draw03 wear recipe; retain narrow-lane clipping and gap phase exactly.
2. Split crossing appearance triangles only at boundaries where the source unit
   or its phase changes. Keep physical bone ownership and original vertex-attribute
   interpolation. Do not cut at every texture repeat or the gap's 32-pixel period.
3. Associate each fragment with its known band, select the full prepared native
   unit, and derive its affine UV from that band's measured crop and phase.
   A known band assignment is needed at inclusive shared edges rather than trying
   to infer ownership from the conservative current reducer.
4. Keep native mixed-surface masks separate. This change alone does not establish
   a complete solution for every mixed nonperiodic receiver.

This makes repeat storage independent of road width and avoids a custom shader or
additional coplanar surface. Movable-contract section 20 expressly retains actual
appearance-domain partitioning; its ban applies to paint-outline and texture-period
cuts. A lane/gap source-unit or phase boundary is an appearance boundary, not a
32-pixel repetition boundary. No stricter universal user/contract ban was found.
The narrower no-new-cut investigation constraint was a task instruction, not that
contract rule. Using an added skinned wear layer would need additional clearance,
occlusion and geometry validation; it is not the implemented approach.

The source-linked Unity check validates production partitioning, affine phases and
existing prepared keys; generated-road rendering and mixed-surface composite
acceptance remain separate. See Integration.md for evidence and limitations.
