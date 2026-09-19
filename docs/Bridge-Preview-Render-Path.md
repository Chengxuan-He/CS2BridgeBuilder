# Temporary bridge preview rendering

## Scope and evidence (2026-09-19)

The user reported a visible PNG containing disconnected, clipped white fragments.
The failure is in assembly/rendering, not in the UI image layout. The preview must
mock the game's rendering process rather than display the bridge's icon.

The shipped game assemblies were inspected locally, specifically:

- `NetCompositionHelpers.GetCompositionPieces/CalculateCompositionData` and
  `BatchMeshHelpers.GenerateCompositionMesh`: section packing, signed X/Z mapping,
  authored half selection, offsets, and packed vertex channels.
- `RenderPrefabRenderer.Instance`: actual MeshFilter/MeshRenderer objects, material
  property blocks, explicit VT stack binding and VT tile requests.
- `ThumbnailCustomPass`: Forward/ForwardOnly/SRPDefaultUnlit renderer lists and
  depth/stencil state, rather than isolated DrawMesh calls.
- HDRP `TryCull`: beginCameraRendering occurs before culling. EndCameraRendering
  occurs before final command submission; readback stays on a later engine frame.
- `TextureStreamingSystem.BindMaterial`: binds the procedural VT stack in addition
  to texture parameter blocks. Copying a SurfaceAsset material alone omits this.
- `NetCompositionHelpers.AddCompositionLanes`, `LaneSystem.CreateEdgeLane` and
  `SecondaryLaneSystem.UpdateLanes`: native lane placement, direction/group flags,
  adjacent-lane marking selection and theme requirements.
- `ManagedBatchSystem` and `BatchDataHelpers`: curved marking mesh shader
  variants, texture area, native curve matrices and the Roads decal receiver mask.

## Implemented path

1. For the creation tab, generate a `tmp` bridge recipe using the same generation
   entry point as formal creation, without registering or exporting it. For the
   management tab, borrow the already registered bridge prefab by exact UUID;
   do not regenerate its recipe or create a temporary prefab copy.
2. Compose ordinary elevated edge sections in an isolated ECS world, applying
   their edge-state requirements. Retain the resulting `NetCompositionData`.
   Decode source attributes using the game's `MeshVertex/MeshNormal/MeshTangent/
   MeshUV0.Unpack` methods, then invoke the installed game's
   `BatchMeshHelpers.GenerateBatchMeshJob.GenerateCompositionMesh` method in that
   private world. Its visibility, half-piece topology, winding, packed vertex
   colors, normals, tangents and texture coordinates remain native code.
   Do not substitute affine-scaled source NetPiece meshes for a composition.
   Supply the four `colossal_CompositionMatrix` properties using
   `BatchDataHelpers.CalculateEdgeParameters` for the same straight edge. The old
   preview omitted all four matrices required by network surface shaders.
3. Build the composition's lanes and secondary markings in a disposable ECS
   world, importing source lane prefab data read-only. Recreate archetypes and
   remap prefab references to that world before invoking the installed native
   jobs synchronously. Use native curve matrices and authored mesh/material
   variants, not guessed lane positions or a fixed set of painted stripes.
   Assemble the mesh parts and towers under one temporary bridge object in a
   separate Unity scene. Use MeshFilter/MeshRenderer and native property blocks,
   including the prefab's first color variation. Source materials stay unchanged.
   Network and object materials have separate private variants. The packed-normal
   shader keyword follows the actual mesh format; network shader variants are not
   forcibly changed to ordinary object geometry.
   Placeholder pillars select separate vertical/standalone, horizontal and base
   candidates using the native height/width scoring, not an alphabetical winner.
4. Bind and request the native virtual textures explicitly. Enable the preview
   camera's native opaque/decal passes and give composed surfaces the native
   Roads receiver mask. Capture HDRP's shaded camera buffer before postprocessing,
   including its DBuffer markings. A separate thumbnail renderer-list pass supplies
   geometry coverage only. Unity supplies per-object and camera-relative transforms.
5. Use an orthographic isometric camera and expanded edge/object bounds, not the
   normalized network vertex bounds. Copy shaded colour into an independent linear
   ARGBFloat target and coverage into a separately cleared RGBA target. Disable
   camera exposure and post-processing;
   read un-clipped HDR radiance, apply fixed EV100 11 and a photographic shoulder,
   and convert to sRGB once when encoding the RGBA PNG. Untouched pixels remain
   alpha zero; no color-keying or opaque gray background is used. Private
   4000/1000 lux key/fill lights are presentation settings, not changes to the
   bridge's in-game lighting behavior. Both are private box spots; the key casts
   orthographic punctual-atlas shadows onto the road/structure, the fill does not.
   No ground receiver is added to the transparent image.
   Rasterize at 3072 x 1536 and resolve 2 x 2 coverage into a 1536 x 768 PNG.
   The resolve averages premultiplied linear radiance, restores straight RGB,
   applies the fixed exposure/tone curve and preserves fractional alpha. It does
   not blur geometry, alter cable thickness or depend on temporal AA history.
6. Read the completed render texture after GPU submission. On selection changes,
   release the temporary renderers, lights, camera, scene, meshes, cloned
   materials and balanced VT leases. Formal creation builds fresh `b{uuid}`
   assets, never promotes a preview object.

The preview camera uses
[`Camera.scene`](https://docs.unity3d.com/cn/2022.1/ScriptReference/Camera-scene.html)
to restrict rendering to its scene. Its objects are additionally outside the city,
on a dedicated layer, and hidden before non-preview-camera culling. City camera
masks, city lights, saves, generated assets and the mod-cache structure are not
modified.

The construction context is a straight course over flat ground at 16 m, clamped
to the generated prefab's elevation range. This is not a city simulation or an
audit sample. No formal bridge width correction is made. Native edge mesh and
shader deformation, primary lanes and secondary markings are now used, but live
terrain-dependent alignment, auxiliary-lane expansion and node/intersection
topology are not executed by this isolated preview. Do not
describe it as a verified, fully identical replacement for the complete in-game
construction pipeline; those differences and visual verification remain open.

## Verification boundary

Release compilation and static source checks pass. No game was launched or
controlled and no synthetic visual tests were run. The user must still verify
the real preview, including GoldenGate with the reported RoadBuilder road, a
single-deck bridge and a double-deck bridge, plus repeated selection/close/reopen.
Check that the deck and structural pieces join, colors and exposure are visible,
the model is framed without clipping, and no preview geometry/lights appear in
the city. Compilation does not establish that these visual checks pass.

## Fixed-segment overlap and aliasing (2026-09-19)

The reported GoldenGate preview had extra, reversed cable runs and noisy edges.
The retained real prototype dump (`ModsData/BridgeBuilder/asset-anatomy.txt`,
Golden Gate Bridge, fixed segments at lines 67749 onward) specifies four spans:

| Span length | Prototype state | Previous preview state |
| --- | --- | --- |
| 350 m | Front | Front |
| 640 m | Opening, Front | Opening, Front |
| 640 m | Opening, Back | Opening, Front, Back |
| 350 m | Back | Opening, Front, Back |

The prototype's overhead sections independently require Front or Back; Back
selects the flipped section. Its end-cable pieces exclude Opening. Accumulating
states therefore selected both orientations on the last two spans and selected
middle cables on the last approach. This is a selection fault, not a reason to
change tower width, stretch meshes differently or shorten the bridge.

The installed game's `CompositionSelectSystem.GetEdgeFlags` starts with fresh
flags for each edge, applies that edge's `FixedNetElement.m_SetState`, and removes
its unset states from the elevation flags. `BridgePreviewScene` now follows that
per-edge lifetime, always starting from the same elevated-edge base flags.
Non-fixed bridges and auxiliary-deck placements retain their existing behavior.

The capture previously used a single 1024 x 512 raster with all AA disabled;
the custom HDR target is outside HDRP's postprocess AA. Supersampled coverage now
addresses the remaining thin-wire/stair-step aliasing without adding opaque or
dark borders to the transparent preview. The native mesh and index buffers are
unchanged. This repair is limited to the disposable preview, not formal assets.

Release compilation passed. No visual unit tests were run. The game was stopped
for installation and was not opened or controlled. Existing bridges, saves and
mod caches were retained. Human verification is still required for the actual
GoldenGate cable joins and fine-detail rendering after restarting the game.

## Grand assembly failure and colour shift (2026-09-19)

The user confirmed the Grand preview displays `PreviewAssemblyFailed`, not
`PreviewBuildFailed`. The temporary recipe therefore reached scene assembly.
The available log did not identify which return-false branch or component failed.
Do not claim a confirmed Grand-specific root cause from that generic message.

The real Grand dump includes pylon and pillar placeholders and their replacements,
with 18 and 76 mounted children respectively (lights, benches and bins). The object
walker incorrectly used “this object emitted a mesh” as its success condition:
an inactive-state-only child aborted its entire parent. Success now means the
active graph assembled without error. Inactive mesh states and meshless children
are legitimate; meshless containers are still traversed for their children.
An active mesh with unavailable geometry/materials remains an error, not a
reason to hide a failed structural part or substitute a different bridge.
This is a corrected code path, **not proof that it was the reported Grand trigger**.
Assembly failures now log the exact piece/object/child/material or placeholder,
segment and selected recipe. This is failure-only reporting, not a full audit or
per-frame diagnostic scan. Reproduce Grand once after installation to determine
whether another assembly failure remains.

The previous output transfer applied Reinhard independently to R, G and B. This
changes linear RGB ratios, especially in bright saturated metal. The output now
applies one peak-channel shoulder multiplier to all three components before the
single sRGB encoding; alpha remains untouched coverage. Source materials, VT
textures and colour palettes are not recoloured. Key/fill intensity is reduced
one stop, the key and bridge meshes now cast/receive shadows, and the diffuse
fill no longer adds a second sharp specular highlight. Those changes affect only
the isolated capture scene; city lighting and authored bridge effects are intact.

Release compilation is the available code/loadability check. No visual unit
tests or game control are performed. Grand restoration, natural material colour,
shadow quality and transparent-edge appearance still require human in-game
verification; the preview still has the construction limitations listed above.

## Directional shadow atlas conflict (2026-09-19)

The subsequent Player.log records repeated `Cascade Shadow atlasing has failed,
only one directional light can cast shadows at a time`. The preceding lighting
change enabled soft shadows on the preview key and ShadowMaps on its camera.
The installed HDRP's `HDShadowManager.LayoutShadowMaps` reports this exact error
when `m_CascadeAtlas.Layout(allowResize: false)` fails. A separate Unity scene,
camera culling mask or light layer is not a private directional shadow atlas.

The preview now submits no shadow-casting lights: both Light components use
`LightShadows.None`, HDAdditionalLightData explicitly disables shadows, the preview
camera disables ShadowMaps, and its temporary renderers neither cast nor receive
shadows. The game's sun, camera settings and HDRP asset are not modified. Key/fill
colour, intensity, material/VT bindings, exposure, transparent capture and bridge
geometry remain unchanged. This supersedes the shadow-enabling portion of the
previous entry, not its assembly or colour-transfer changes.

Trade-off: the isolated preview has no cast shadows. Do not claim an identical
in-game lighting match. Compile/static checks cannot confirm the in-game result;
the user must restart and exercise preview selection/close/reopen to confirm that
the atlas error no longer recurs and city shadows remain unchanged. Existing
bridges, saved games and mod caches are retained.

## Missing road markings (2026-09-19)

The screenshot shows Grand with a road surface but no lane paint. The old preview
only generated composition surfaces and placed object meshes: it never ran lane
composition or secondary-lane generation, and it explicitly disabled camera
decals. Merely adding white lines to the surface would not reproduce the chosen
road's authored lane direction, spacing, regional markings or track meshes.

The isolated lane adapter now runs the installed game's ordinary edge-lane and
secondary-lane methods. It reads source prefab data, default theme and traffic
handedness, but all entities, recreated archetypes, lookups and command buffers
belong to a private disposable world. API incompatibility reports a preview
assembly failure; it must never fall back to city archetype handles. Both decks
use the same path. Track-only/pedestrian roads are not given invented road stripes.

Lane draws retain native tiling, inversion, curve deformation, texture area,
decal priority and source colour data. Editor, intersection/track-crossing-only
and wrong-traffic-handedness meshes are not drawn on the straight preview edge.
Composed road materials receive the native `DecalLayers.Roads` mask (raw integer
bits as a float), which SurfaceAsset material copies alone do not supply.

HDRP's complete camera colour buffer is copied at BeforePostProcess, so DBuffer
markings affect the shaded road. The previous Forward-only redraw is retained
solely for transparent silhouette coverage. Fixed exposure, chromaticity-preserving
tone transfer, two-times spatial sampling and the no-shadow-atlas policy remain.
Camera/targets, lane meshes, private worlds and source-material leases are released
on preview replacement; formal bridge creation and saved assets are unchanged.

Release compilation and static API/source inspection are the available checks.
No visual tests or game control were performed. After restarting, verify the
reported Grand/RoadBuilder combination, a one-way or asymmetric marked road,
a double-deck bridge and a track-only selection. Check marking presence and
alignment, transparent edges, exposure, repeated switches and close/reopen.
This does not claim that full node topology or all auxiliary-lane visuals have
been implemented or that the screenshot result has been verified in-game.

## Independent preview shadows (2026-09-19)

The user requires cast shadows without using the game's sun. This supersedes the
temporary no-shadow policy above, but does not re-enable a shadow-casting
Directional light. The installed `HDAdditionalLightData.GetShadowMapType` sends
Directional lights to the cascade atlas and spot lights to the punctual atlas;
`ExtractSpotLightData` uses orthographic projection for `SpotLightShape.Box`.

The private key and fill are now `HDLightTypeAndShape.BoxSpot`. Their light-space
boxes enclose all eight corners of the assembled preview bounds with padding.
The source, width, height and range change only the presentation rig, not bridge
geometry. Range attenuation is disabled; neutral white 4000/1000 lux, fixed
exposure and the original light directions remain. Only the key casts shadows,
requesting a 2048 map (subject to the installed HDRP cap) updated each preview
frame. The fill contributes diffuse light without a second shadow/specular sun.
Temporary mesh renderers cast and receive native material shadows.

The preview camera enables ShadowMaps and continues to render its isolated scene,
outside the city, with matching private light/shadow layers. Neither preview
light is directional or references the city sun. No city light, renderer, camera,
HDRP asset or global shadow setting is changed. Preview lighting is hidden before
non-preview-camera culling and is disabled once capture completes.

The longer shadow distance needed by full fixed-span bridges is supplied by a
private LOCAL volume with a small collider around the far-away preview camera.
It is deliberately not a global volume: an unrelated camera can have an
Everything volume mask. The profile overrides only maxShadowDistance, and the
volume, collider, profile components and private lights are released with the
temporary preview. This uses HDRP's ordinary punctual shadow atlas, not a new
dedicated atlas, and never requests a second set of directional cascades.

Shadows darken the road and bridge surfaces; the PNG background remains
transparent. No opaque ground plane, recoloured material or painted shadow is
introduced. Release compilation and static inspection passed; no game was opened
or controlled and no synthetic visual tests were run. After restart, a human
must confirm tower/cable shadows on the deck, no cascade-atlas errors, unchanged
city sunlight/shadows, and cleanup after switching or closing previews.

## Shared lane-adapter failure and CRITICAL reporting (2026-09-19)

The 13:00:56 and 13:00:59 BridgeBuilder logs identify the same failure for
GoldenGate and SuspensionGolden: `BridgePreviewLanes.LaneSystem.BindJob` throws
`InvalidOperationException: Sequence contains more than one matching element`.
This aborts scene assembly before the independent shadow renderer starts.
The installed `SystemBase` hides the component/buffer lookup factories also
declared on `ComponentSystemBase`. Enumerating the inherited methods and using
`Single` by name and parameter count is therefore ambiguous on the game runtime.

The adapter now reflects four uniquely named, declared-only local generic
methods. Their actual ECS factory calls are compiler-resolved and remain bound
to the private preview world. Both primary and secondary lane jobs use this
path; road markings are not omitted to make assembly appear successful. The
installed primary job's 16-parameter CreateEdgeLane signature, LaneBuffer
constructor/disposal, GetCompositionData and secondary UpdateLanes signatures
were also checked. No geometry, dimensions or lighting were changed by this fix.

Every failed current preview request now reports once through `ILog.Critical`,
including invalid/unavailable selections, recipe construction, scene assembly,
rendering, empty images, readback and timeout. The message includes revision,
bridge style, upper/lower deck IDs and temporary prefab identity. Captured lane
and render exceptions are passed through with their stack traces. Component
warnings remain additional context, not the sole failure report. A cancelled or
superseded selection is not reported as a failed generation. A failure never
publishes PreviewReady and never creates a formal bridge asset.

Rendering callbacks schedule disposal on the owning system's next update; they
do not destroy a camera during its render callback. Synchronous build failures
release their temporary session immediately. Existing bridges, saves and mod
caches are untouched. Release compilation passed against the installed game
assemblies. This is not an in-game visual verification: after restart, reselect
the reported RoadBuilder road with GoldenGate/SuspensionGolden and confirm the
complete preview, markings, independent shadows and repeated switch/close cleanup.

## Logical lanes are not mesh lanes (2026-09-19)

The next real run reached lane drawing: at 13:17:28, GoldenGate revision 6 failed
in `BridgePreviewLaneMesh.Build` while accessing `lane.Prefab.m_Meshes`. The game
IL confirms that `PrefabSystem.TryGetPrefab<T>` returns true whenever the prefab
index is valid, even if its `as T` cast returns null. Requesting every generated
lane as NetLaneGeometryPrefab therefore put null entries in the draw list for
ordinary NetLanePrefab traffic lanes.

The output adapter now resolves a non-null NetLanePrefab and checks its actual
type. Logical lanes still participate in primary/secondary lane creation; only
their non-existent own visual is excluded afterward. This follows native
RequiredBatchesSystem.UpdateLaneBatches, which skips prefabs without a SubMesh
buffer. NetLaneGeometryPrefab supplies that buffer and NetLaneGeometryData;
ordinary NetLanePrefab does not. A non-geometry custom lane with non-empty
SubMesh data is explicitly unsupported rather than silently losing its mesh.

Missing source mappings, invalid prefab resolution and missing drawable meshes
or materials return failure with the affected lane/render identity. The mesh
consumer also rejects a null geometry prefab defensively; it does not turn it
into successful empty output. The existing per-request CRITICAL boundary remains
in force. No bridge dimensions, material colours, shadow settings or saved assets
were changed. Release compilation and installed-game API inspection passed;
the user's GoldenGate/RoadBuilder selection still requires a restarted in-game
preview check. Compilation alone does not establish that the preview is correct.

## 30 m course, native tower stacks and existing-bridge previews (2026-09-19)

The reported Suspension01 screenshot shows overlapping tower/base parts. The
previous object draw loop rendered every active ObjectMeshInfo at its authored
origin, without the StackData / Stack instance / tile placement used by the game.
It also used the raw visible mesh union for top anchors and placeholder scoring.
That is not ObjectInitializeSystem's object geometry metadata for a stacked tower.

All preview courses now use a main deck 30 m above a flat reference ground. The
private scene places that main deck at y=0 and ground at y=-30; it does not clamp
to PlaceableNet's road-tool elevation range. Auxiliary decks retain their exact
authored relative Y offsets, and use the same world-ground reference. This is
presentation placement only; formal assets, widths and player construction
elevation settings are not rewritten.

BridgePreviewObjectLayout reads initialized ObjectGeometryData, StackData and
PlaceableObjectData for registered objects. Unregistered tmp objects aggregate
authored RenderPrefab.bounds by the installed ObjectInitializeSystem rules,
including part transforms, First/Middle/Last overlaps, forbidden scaling,
inactive-state metadata and StandingObject leg bounds. Placeholder scoring uses
native ObjectUtils.GetSize rather than the raw mesh union's size. Anchoring and
flat-ground stack range follow AlignSystem.AlignHeight; a stacked Base is not
prematurely moved to ground before this calculation. Horizontal/vertical pairs
use the selected horizontal support range for their final flat-course spacing.

The draw loop directly invokes both installed BatchDataHelpers
CalculateStackSubMeshData overloads to determine tile counts, offsets and scales.
It emits the same source template with native instance transforms; it does not
edit vertices, prefab metadata or width corrections. Recursive objects inherit
the stored native Elevation for their initial stack, not the parent's final
world-height distance from terrain. ParentMesh is not interpreted as a tile
number. Bone-connected children and optional native terrain-adjustment behavior
of special negative ParentMesh codes remain validation boundaries; this change
does not claim a general simulation of every object/terrain interaction.

The management tab now sends PreviewExistingBridge with the formal prefab UUID.
This path resolves that loaded registration directly and bypasses road/style
catalogue lookup, TryBuildBridge and the tmp session constructor. Missing existing
assets fail with CRITICAL rather than being regenerated. Borrowed sessions own no
prefabs; only private renderer meshes/materials, buffers and temporary scenes are
released. Changing the view or closing it never destroys the borrowed bridge.
Formal creation remains a separate regeneration with a new b-prefixed UUID.
Permanent registration accepts only b-prefixed UUIDs. Existing r-prefixed bridge
assets and affected save references were migrated offline before removing the old
prefix compatibility; see Bridge-Identity-Migration.md. Source Road Builder IDs
are not bridge identities and are never renamed by this migration.

Successful images no longer display the fixed isometric-preview caption beneath
them. Loading/failure messages remain inside the image area; the per-request
CRITICAL diagnostic is unchanged.

Release compilation, JavaScript syntax and installed-game API inspection are
the checks available here, not visual acceptance. A human must restart and
verify Suspension01/Grand/GoldenGate bases, 30 m supports, double-deck separation,
and management previews after source-road changes, including repeated switching
and closing. No game was opened or controlled for visual testing. Existing bridge
assets, savegames and mod caches are preserved during installation.
