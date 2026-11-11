# Native160 investigation: shared motion for movable components

> Historical native-only investigation. The requirements and outcome below describe
> the earlier stage before the user authorized generated shaders, exempted native
> light positioning, and temporarily deferred mod-disabled independent loading.
> They are not current implementation prohibitions. Current production work has
> since connected both generated material programs and all ten decal producers;
> complex ordinary attachments and game-scene acceptance remain incomplete.
> No partial native160 build has been deployed.

## Outcome and scope

The requested repair is **incomplete and not deployed**. Native159 remains installed, with its existing movement defect unresolved. After the initial read-only investigation, production source changes began: rigid bone encoding, common render supplements and selected-road static-visual preparation. These changes do not yet cover decals, effects, utility semantics or every authored attachment. No generated player bridge, installation or material asset was modified. Specifications preceded the source changes.

The current saved relative-child representation does not satisfy the new requirement that every moving component share the deck's native motion module and motion sample. No complete replacement meeting all retained requirements was found in the inspected native interfaces. Compatible static mesh encoding has since been implemented and tested separately; it does not resolve native decal/effect inputs. This is a bounded capability blocker, not a claim about every possible future engine implementation.

Required together: original retained decal artwork and continuous native blending; same-sample movement for the deck, decals, poles, lamps/effects, cables and authored moving attachments; no transform tracking, prediction or compensation; independent cold loading without Bridge Builder; no custom shader or mod-owned synchronization service. Fixed components must remain fixed.

## Native update-order evidence

Current installed `Game.dll` SHA256:
`F076DF4B70AA0F21A80345E03A052CA2273BC6754F41DAB7E63D837837A43745`.

Current DLL decompilations are retained in `work/decal160/native/`:

| Native method | Evidence |
| --- | --- |
| `SystemOrder.Initialize` rendering registrations | `ObjectInterpolateSystem` at line 659, `RelativeObjectSystem` 661, `EffectTransformSystem` 663, `ProceduralUploadSystem` 690. |
| `RelativeObjectSystem.GetRelativeTransform` | Lines 1014-1044 read `BoneHistory[relative.m_BoneIndex.y].m_Matrix`, apply attachment position/rotation, then the submesh transform. They do not evaluate the current procedural bone state. |
| `ProceduralUploadSystem` | Lines 333-369 calculate skin matrices, replace history and upload current matrices with motion blur; lines 394-407 do the corresponding current upload/history update without motion blur. History is refreshed after relative-object/effect processing. |

Thus a valid parent connection is insufficient for same-sample rendering: the child's object transform can use the previous available bone matrix while the skinned deck uses the later current upload. This is a real protocol mismatch, not proof that a single ordinary rendered-frame angular difference alone explains every pixel of the screenshot.

The inspected `EffectTransformSystem.GetEffectTransform` similarly reads BoneHistory (lines 270-305 in the retained native source). Moving lamp effects are affected, even when their source is relocated onto the mechanism root. Moving lamp geometry, selected-road poles/supports, retained decals and authored ObjectSubObjects all use the relative-child path. Existing fixed root/tower/control-room attachments do not acquire a movement mismatch merely by sharing this file format.

## Additional native hierarchy limitation

`RelativeObjectSystem` skips independent evaluation when an attachment's owner itself has Relative (lines 147-150). Recursive `UpdateTransforms` at lines 375-390 and 719-734 uses `Relative.ToTransform()` without evaluating each descendant's bone index. A nested relative hierarchy can therefore lose bone-driven movement rather than only lag.

This conditional failure is not established for the reported instance. `SubObjectSystem` creates normal network-owned mechanisms without necessarily adding Relative; its object-parent and nonzero bone-index branches determine the component. Saved Prefab fields do not prove the live ECS archetype. Do not present the nesting observation as the confirmed screenshot cause without instance evidence.

## Read-only saved-instance and numerical evidence

The existing Draw01 owner `b8a014179-776d-42f9-a415-ad477f0370cf` was inspected without mutation. Its mechanism has moving connections 100/101, hinge positions Z = -81.3/+81.3 m, type 33 and speeds -0.015/+0.015. Saved decal projection volumes have height approximately 1.0000002 m. Attachment parent values include 0, 100 and 101; the files do not simply omit all moving parent bindings.

`work/decal160/motion-evidence.py` reproduces the distance-dependent projection failure numerically using explicit synthetic angular differences and these measured volume heights. An 80 m radius with 0.5 degree sample difference produces 0.698 m receiver displacement in the old projector frame, outside a roughly 0.5 m half-height; the hinge remains coincident. These angle differences are test inputs, **not measured game frame differences or observed animation speeds**. Same-sample transforms eliminate this geometric difference by construction, but this numerical control does not implement a native same-sample binding.

## Alternatives inspected and why they are not a delivered repair

- `RelativeBoneSystem` resolves connection IDs to bone indices; it does not replace the subsequent history-based transform evaluation. Changing a correct ParentMesh ID cannot establish same-sample motion.
- `ObjectMeshInfo` persists mesh, fixed position/rotation and required state. It has no direct current procedural-bone object-transform input.
- DefaultDecal's inspected 16 compiled D3D vertex variants lack a procedural-bone variant. CurvedDecal's 24 inspected variants also lack that input and additionally require lane curve data. Adding weights or a keyword cannot create the missing native shader protocol.
- `ProceduralAnimationProperties` describes skeleton bones/clips, not a direct whole-StaticObject transform controller. Inspected AnimatedSystem root-motion handling processes animation data; it did not establish a persistent bridge-leaf entity-motion route.
- Moving an entire rigid group into another Relative child would make that group follow historical matrices. It is not an accepted way to relabel tracking as a shared direct-motion module.
- Enlarging decal volumes would conceal some displacement but retain incorrect motion and potentially project onto unintended receivers. No such change was made.
- A recurring mod patch that recomputes child transforms, refreshes history earlier or changes native system ordering conflicts with independent mod-disabled loading and the explicit prohibition on chasing. No such patch was added.

The renderer's actual live sample difference, culling/LOD state, and any alternative native whole-object bridge controller remain unverified. The audit does not claim full exhaustion of undocumented engine behavior. It does establish that the current persisted connection-based path and the inspected direct substitutions are insufficient.

## Specification updates and validation

Root/runtime AGENTS, movable contract section 19, material contract and Prepared-Materials now require the shared motion module/sample for all moving components and reject tracking/compensation. Earlier wording treating native relative attachment as sufficient was removed. Audit documents remain outside the normative contract graph.

`git diff --check` passed. Existing native159 source changes were preserved. No new user-visible UI text or localization keys were introduced. Native160 intermediate builds and isolated Unity/native model tests have run; direct UInt32 bone encoding and preservation of source position/UV/index channels passed. The connected production path still requires broader validation and is not a complete repair. No native160 deployment was made. The installed DLL hash remains:
`FA729FEF05231F8F6788A7C6ACFEEEF533E15C726ED8E9CE44E59CBD8F3D7CDF`.

## Subsequent implementation work (not installed)

The main source tree now contains an incomplete direct-mesh migration. All ten selected-road
mechanism generators call the common `TowerFactory.PrepareDirectRoadMeshes` before native
render assembly. Lift03 contributes both deck placement lists. The common preparation keeps
source render/LOD identities, transforms compatible static mesh positions and direction frames
into the mechanism's closed frame, and supplies `Supplement.RigidBone` to the existing model
assembly. The model encoder writes native UInt32 single-bone indices with implicit weight one.
The later attachment binder omits the duplicate relative visual only for a matching merged
placement. Far-LOD supplement forwarding and Lift03's combined level forwarding were updated.

This is not a completed permanent removal of the old path. Conditional or independently
animated meshes, nested object and utility semantics, authored object attachments, decal
projectors and light effects remain unconverted. In particular, a merged streetlight's effect
still goes through the existing native BoneHistory route. The old route has not been removed
for these unresolved components, and this work must not be deployed as native160 or described
as satisfying the no-history requirement. No geometry/material substitution, hidden omission,
tracking controller or new shader was introduced to disguise these gaps.

Validation completed:

- The main tree builds after all ten generator connections. Existing material and numerical
  resource guards pass; two pre-existing nullable warnings remain in MovableSurfaceAtlas.
- An isolated Unity host with the actual game assemblies passes `MovableStructureModel.TryBuild`
  and native `Model.ToUnityMesh`: original positions, UVs and indices remain unchanged; native
  UInt32 bone indices 0 and 2 are emitted with implicit weight one.
- The connected factory test was attempted with the original
  `StreetlightSingle02_Geom_df1e2ad48a46fc04d8cddc81a12ed118.Geometry` from the installed
  Game/Blob.cok. The independent host cannot complete the game's GeometryAsset IO: both
  SetData paths and later LoadSync attribute decompression fail in the host's
  NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr/Unity.Collections boundary. The real
  PipelinePlugin was loaded. This is a failed/unavailable connected test, not a successful
  generation test and not evidence that the production game has the same exception.
- `RigidBone160Check.Editor.cs` defaults to the runnable model test; its explicit `-connected`
  mode retains the connected diagnostic instead of hiding the failed attempt.

No complete source-to-prefab save, successive-pose rendering, actual ten-style generation or
mod-disabled cold-load acceptance is claimed for the new migration. No player asset or installed
DLL was changed. The fundamental remaining decal/effect limitation is the inspected native
consumer protocol, independently of the separate offline-host IO limitation.

## Current-resource re-audit and concrete mesh correction

The later check reads the currently installed resources, rather than assuming historical
Unity path IDs remain stable. `resources.assets` SHA-256 is
`3840F65CA2B0BFAE1BCB4E38369E24851DCA670B656F3A7BCF6C6C33369E10E6`.
Its current AssetDatabaseResources library contains 40 templates referencing 26 distinct
shaders; the whole asset contains 136 shader objects. The current Game.dll hash remains the
one recorded above. Historical path IDs such as 571/572/587 must not be used to identify
Default/CurvedDecal/DefaultDecal in this current asset.

| Current shader | Current path ID | D3D vertex programs | Procedural-bone variants | Finding |
| --- | ---: | ---: | ---: | --- |
| AreaDecalShader | 559 | 12 | 0 | Additional decal shader found outside the three previously inspected decal families. |
| CurvedDecalShader | 568 | 24 | 0 | Current templates 20 and 32; curve/decal inputs, no direct procedural-bone variant. |
| CurvedDecalDeteriorationShader | 574 | 16 | 0 | Current template 19; does not supply a direct skeleton route. |
| DefaultDecalShader | 583 | 16 | 0 | Current template 18; both DBuffer mesh/projector passes lack that variant. |
| SG_DefaultShader | 567 | 1296 | 432 | The native ordinary-object protocol does support procedural bones. No compiled transparent-surface vertex variant was found, despite the keyword declaration. |
| GlsShader | 572 | 228 | 76 | Forty variants combine procedural bones and transparency, but this is a glass/refraction surface, not the original DBuffer receiver blend. It is not an accepted equivalent decal replacement. |

All 68 current decal vertex programs were extracted as actual DXBC and successfully
disassembled, plus one current procedural Default control. The control includes the extra
raw-buffer/input access used by its procedural path; the decal programs do not contain a
corresponding compiled procedural variant. This is stronger than searching declared shader
keywords, but does not claim every unregistered third-party bundle or undocumented engine
renderer has been exhausted. Reproduction and per-program hashes are retained in
`work/decal160/current-shader-by-name.py`, `current-library.py`, `current-programs.py`,
`current-shader-protocols.json`, `current-material-library.json` and `current-programs.json`.

Fresh decompilation of the installed Game.dll establishes the material/instance link:
`ManagedBatchSystem` reads the output RenderPrefab's ProceduralAnimationProperties at line
936 and, at lines 1044-1053, writes BoneParameters, enables `_GPU_ANIMATION_PROCEDURAL` and
sets MotionVectors/Bones flags. Thus native instance flags do not require inventing an
animation property on each Surface. The original material must nevertheless have the usable
shader variant and compatible extra inputs. This evidence is not visual acceptance of every
possible attachment material. Original per-render EmissiveProperties/ColorProperties and
independent LOD bias also need explicit preservation when merging different source render
objects; the partial source migration does not establish that complete behavior yet.

The freshly decompiled EffectTransformSystem still obtains parented effect positions from
BoneHistory (lines 270-305). Its animation-index branch at lines 240-245 multiplies intensity;
it is not a current procedural matrix selector. EffectSource serializes the parent connection,
local offsets/rotation/scale, intensity and animation index, with no alternate current-GPU-bone
input in the inspected contract. Moving an emissive mesh is not equivalent to moving its
illuminating light effect.

A concrete source defect was corrected in this iteration: attachment preparation previously
collected multiple source submeshes while the supplement consumer accepted only one, causing
an avoidable whole-generation failure. It now emits one supplement per original material
slot. `MovableStructureModel.TryBuild` can select that exact source submesh's index stream
without making another intermediate Unity Mesh. The consumer resolves the matching flattened
Surface slot. The actual native Model/ToUnityMesh regression passes two source slots with
opposite winding, unchanged positions/UVs and one output material slot each. Main-tree build,
resource guards and the existing rigid bone tests also pass. This does not change the failed
complete Geometry IO host test or claim full persistence/rendering acceptance.

The remaining original-decal requirement is blocked by the inspected native consumer
capabilities: original DBuffer blend and artwork, direct same-sample mechanism bones, and
independent loading without a synchronization/custom-renderer dependency cannot presently all
be supplied by these native protocols. Effects have the separate CPU-history input limitation.
No glass proxy, hashed alpha, historical-transform fallback presented as a repair, hidden
attachment removal, or deployed replacement was used to bypass that boundary. The full repair
and permanent removal of every old moving-component path remain incomplete.
