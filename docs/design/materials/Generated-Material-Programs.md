# Generated material program implementation

The material contract authorizes two generated programs: current-skeleton decals
and base-UV/world-coordinate layered PBR. This document describes their package
and registration machinery, not completed rendering acceptance.

## Shader definition and scope

**Custom shader = native shader + bone binding**

The generated program preserves the native material model: source PBR channels,
continuous alpha and blending, UV/world-coordinate sampling semantics, fragment
operations and pass states. Bone binding connects those native semantics to the
current movable mechanism skeleton, including decal projection frames. It does
not introduce a new appearance model or a per-frame mod-driven motion tracker.
Bridge Builder must remain enabled for startup program registration; native rendering consumes
the current bone data. The verification requirements below still apply to every
supported pass, variant and rendering path.

## Native layered program authoring

`tools/MaterialShaders/BuildNativeLayered.py` accepts extracted native object and
layered Shader type trees, a compatible Unity container, and an output directory.
UnityPy is an offline authoring dependency. The tool preserves the source layered
pixel programs and their pass states. It replaces the evaluated-geometry vertex
protocol for six native passes, with separate DOTS/non-DOTS variants. The selected
native network program consumes packed normals and tangents in every inspected
keyword variant, including variants without `_TANGENTSPACE_OCTO`. Preserve that
input protocol for the encoded road meshes; do not invent an unpacked interpretation
from the absence of that keyword. It retains the original UnityPerMaterial property offsets and
appends the current bone-range field; competing partial constant buffers are not
an interchangeable representation.

The extracted fallback-error Shader reference belongs to another assets file and
cannot be copied as a valid local reference. The generated container has no such
fallback dependency. Unsupported shader realization returns a registration error;
an error shader is not successful rendered output. Native source shaders and
shared game assets remain unchanged.

The BBS1 package contains the complete container plus each generated HLSL vertex
adapter and the exact locations of its seed programs. Creation and startup compile
those adapters with D3DCompile and rebuild the container using the complete output,
which may be shorter or longer than the seed. The runtime updates each native program
array, shader record table, literal-LZ4 segment, Shader field, serialized object/file,
and UnityFS block/directory size and offset with native alignment. Declared seed
locations remain integrity assertions, not reserved capacities. Native
pixel programs are deliberately retained. A diagnostic fragment or clip-space
probe is never a production shader.

Package version 1 describes vertex adapters with their fixed entry point. Version
2 additionally records an explicit entry point and vertex/pixel profile per
program, allowing the decal's current-pose projection to be compiled for both
stages. Only the supported D3D11 vertex/pixel profiles are admitted. This format
extension does not certify the generated decal's rendering equivalence.

Both existing package versions retain this rebuilding path; owner files need no migration.
The supported authored container is UnityFS 8 with uncompressed streaming blocks,
serialized-file 22 with type trees, and one literal-LZ4 shader segment. Unknown formats,
out-of-range/overlapping declarations and mismatched seed bytes fail explicitly. Unchanged
pass/reflection records and unrelated programs remain byte-identical. Compiler byte count
is not a compatibility verdict. The source-linked `ShaderContainerCheck`, independent
UnityPy parser and `VariableShaderLoadCheck.Editor.cs` cover shorter/longer compilation,
multiple occurrences, deterministic rebuilding and actual Unity bundle/shader loading.
These checks do not establish in-game drawing, motion or saved-city cold loading.

## Ownership and registration

`BridgeCustomShaderRegistry` owns immutable loaded programs for the lifetime of
their native consumers. A program-content digest and source-template identity
produce a stable positive template key. Existing entries, including invalid ones,
are not overwritten. A new template retains source material properties, keywords,
queue, instancing and lighting flags, VT stacks and mip policy. Failed publication
releases only unpublished resources.
Each program container also has a content-derived internal serialized-file name.
Changing only the public AssetBundle name is insufficient: Unity rejects loading
two bundles with the same internal CAB identity. Both generated programs and older
saved program versions must be able to coexist without unloading live consumers.

`MovableCustomShaderAsset` creates one native FileAsset per demanded program and
owner/request. Its versioned text envelope carries the source-template identity
and Base64-encoded BBS1 package. `BridgeCreationDependencies` includes these assets
as explicit roots because a Surface carries only a template integer. Preview uses
the private native asset database. The existing per-owner recursive export includes
the program files. Failure rollback and restoration must remain coupled to the
native asset lifetime; a deleted program asset cannot be reused from a request cache.

Decal draw priority is a persisted template variant, not merely a copied
`_DrawOrder` float. Native validation does not recognize a generated shader's name
as the built-in decal shader and therefore does not establish its queue. The
program envelope records the actually demanded queue variants alongside one
shared program payload. Registration includes the queue in template identity and
recreates those variants and the source-default queue variant before Surface loading.
The default remains restorable when the same package also gains explicit priorities.
Multiple projection regions do
not duplicate the program payload. Failed envelope updates fail the owner
transaction; they must not publish a Surface whose template cannot be restored.
Template identity derives from the persisted source package and configuration,
not compiler output bytes, which can change between compiler environments.
Restoration attempts independent owner packages even if current embedded program
registration fails, and accumulates diagnostics instead of skipping historical
owners. Updating an existing envelope must preserve its previous bytes on failure;
it must never delete a previously successful program asset as rollback.
When another saved bridge supplies an already-generated Surface, its registered
program and queue configuration are still required inputs. Copy that program into
the new owner's transaction, even when the Surface itself can remain unchanged.
Do not mistake a process-global template registration for owner-local persistence.

Startup restoration must precede Surface material realization. A previously
realized unknown-template Surface can hold the native shared fallback material;
registering a template later does not automatically replace that cached instance.
Never modify the shared fallback in place. Consumer activation remains incomplete
until both creation and restoration ordering are connected and tested.

The current native normal-startup order creates systems, runs mod `OnLoad`, awaits
asset loading, loads/adds Prefabs, and only then reaches `WorldReady`, when ordinary
MeshSystem updates run. Register the current program in `OnLoad` and repeat owner
package discovery in `OnGameLoadingComplete` before the state transition, covering
asset files that completed loading after `OnLoad`. ImportedData deserialization
alone does not mean a Unity Material has been realized. Another mod explicitly
loading a Surface earlier, or hot enabling this mod in an existing world, is not
proven by this normal-startup ordering; do not silently claim it refreshed existing
native batches. Self-check must not classify owners as corrupt merely because
program restoration failed.

## Verification boundaries

Every pass and variant must declare one consistent `UnityPerMaterial` layout in both
the common parameter table and every referenced serialized parameter record. Append
bone parameters after the complete source block, without overlapping source fields.
The second integer in a parameter record is not a fixed record-kind discriminator;
filtering records by a few observed values can leave DOTS variants with stale sizes.
Verification must include a real Unity `BatchRendererGroup` submission: an immediate
non-DOTS draw can succeed while the native batch rejects an inconsistent material block.
Preserve source fragment offsets and verify the resulting source-texture draw, not
only compilation or material property readback.
DOTS variants retain their native DOTS program headers and keyword identity.
When current-bone vertex varyings replace all fragment `UnityPerDraw` consumers,
remove that obsolete fragment declaration and binding from the DOTS parameter
record. Renaming a buffer containing built-in fields is not a valid substitute:
Unity enforces the built-in buffer protocol during native batch submission.

For a reported missing appearance, first map the saved RenderPrefab's ordered
Surface references to the actual Geometry submeshes and their receiver domains.
Separate Original, prepared prototype, and decal coverage: a road can use all
three in one render group. A complete dependency graph or a logged texture binding
does not prove the final pixel result. Compare the actual source coordinates,
material defaults/overrides, packed attributes, and native instance inputs in the
selected pixel variant. Do not add unused UV channels or change a serialized
receiver-mask default when the native batch supplies the correct instance value.
Validate unmatched curbs/medians separately from prototype carriageways, and
validate decal projection over both receiver types at every LOD.

Preview acceptance additionally requires the actual ordinary MeshRenderer path,
including its complete Surface defaults and overrides, property block, open-pose
bone buffer, nonzero stage/world transform, and selected non-DOTS variants. A
successful identity-transform GBuffer or BRG test is not evidence that this path
renders correctly. Compare the shaded camera image with the separately rendered
Forward coverage image before attributing a missing preview region to geometry or
material selection. Coverage must not erase a correctly shaded opaque receiver.

For an otherwise unreproduced preview defect, an opt-in
`ModsData/BridgeBuilder/PreviewMaterialCapture.request` arms one preview containing
a generated program. The request is consumed into a unique diagnostic directory,
not every preview. After the already submitted frame completes, record its linear
HDR shaded image, separate coverage image, raw mesh streams and indices, actual
private material properties/keywords, Surface/VT identities, property blocks,
camera/draw transforms and open-pose matrices. Diagnostic failure is isolated from
normal preview completion. The capture is not a generated-asset dependency or a
claim that rendering has been repaired; it must not modify saved owners or sources.

When testing an adapter, observe its actual interpolated normal and tangent as
well as final color: a packed tangent interpreted as a tiny ordinary floating-point
vector can normalize to a non-finite direction even when positions, UVs and the
normal appear correct. An albedo-only or hash comparison that accepts non-finite
lighting channels does not establish a valid rendered material.

The decal adapter transports the projection frame through the same current bone
matrix as its volume vertices. The fragment stage receives the resulting object
and inverse-object matrices; it retains the inspected native DBuffer depth
reconstruction, receiver clipping, texture sampling, PBR channel operations,
continuous alpha and distance fade. It does not use a previous-pose attachment or
replace source blending with a cutoff. The source projection frame is vertex data,
so independent volumes can share the mechanism's current bone range.

The mixed mechanism RenderPrefab must not acquire `DecalProperties`: that component
classifies the whole render resource. Its decal Surface instead supplies the
`DBufferMesh` pass, enabled pass state, projection properties and an opaque-range
queue. Native HDRP selects that pass through its renderer list. Do not add ordinary
ShadowCaster, DepthOnly or GBuffer writes to the decal volume. Actual mixed native
batch drawing remains a separate integration check.
Preview depth configuration identifies projection materials by their enabled
`DBufferMesh` pass as well as native decal components. Mixed mechanism meshes
have no whole-render decal component; their projection Surface must still retain
its source depth/write state during initial material creation and refresh.
Capture the original lane mesh's native distance grade with its source snapshot.
The mixed render does not receive the decal-only native material-property block;
its Surface must carry the captured `CalculateDistanceFactor` value. That grade is
not the bridge's zero-based LOD array index. Include the factor in material grouping
so sources with different distance semantics cannot accidentally share a Surface.

The isolated native GBuffer probe exercises the original layered pixel program,
six synthetic texture inputs, mask endpoints, and a changing current bone matrix.
The registration probe uses the real MaterialLibrary type but a synthetic source
template. These tests do not prove a game scene, native DOTS batching, complete
source materials, decal projection, saved-owner restoration or all attachment
conditions. Those are separate required checks before delivery. A separate
non-DOTS decal comparison now exercises original versus current-bone projection
at closed and rotated poses, including nonuniform source artwork and soft alpha.
The measured GPU inverse-matrix sampling differences must be reported rather than
described as bitwise equality. The direct native FileAsset save/read test likewise
does not by itself prove a game's complete saved-owner restoration.

Production conversion probes now additionally load captured TramRails,
TramRailsBase, LaneLine and BusLane descriptors through the current-bone adapter,
and a captured TiledRoad descriptor through the layered adapter. They verify
retained native texture identities and loaded material parameters, including
continuous-alpha state, draw priority and base/world coordinate scales. The
isolated native database substitutes CID allocation only for the Editor ABI.
These checks narrow the material-input gap; they still do not establish a live
mixed native batch, complete VT coverage or all ordinary attachment semantics.

## Ordinary utility visuals and functional carriers

Simple static utility visuals can enter the mechanism's skinned model assembly
while a private native utility carrier retains functional placement. Do not empty
the object's mesh list: native object initialization derives its bounds from those
entries. A metadata-only render preserves each source bound, surface area, local
transform and relevant components, but has no geometry, material slots or indices.
It must not retain an LOD reference to visible source geometry. Sources with native
`BaseProperties` require their separately generated foundation visual to be handled
first; a zero-submesh render with that component would still generate a base batch.

Ordinary nested visuals require recursive native selection and accumulated local transforms,
with one shared plan for merged visuals and retained native branches. Conditional visibility,
actual independent animation, instance color and emissive attachments deliberately retain their
existing native attachment path under movable contract section 19. Derived utility-network
rendering still requires its functional native consumers. Native utility topology and native light
positioning are not replaced by a per-frame Bridge Builder synchronizer.

The native-type carrier probe verifies utility position, bounds and surface area,
zero visible slots, removed visible LOD references, and an unchanged source object.
Unity JSON cloning did not preserve the native bounds and utility `float3` fields
in that probe; the producer explicitly copies them. This does not prove derived
utility-node behavior in a city. The request placement plan recursively accumulates deterministic
nested local transforms. Source LODs/material slots enter the mechanism, while metadata-only
native hierarchies retain child ordering, parent connections, conditional branches, probability
and group records. Probabilistic/placeholder branches are not baked into a once-per-prefab choice.
Implicit group identity shared with a retained native conditional context remains native;
unrelated ordinary leaf visuals remain mergeable. Carrier reuse includes the recursive plan,
not only source identity.
Conditional state, instance color/emissive data and actual independent animation are explicit
retained-native exceptions, not claims of direct skeleton migration. Verification of recursion
must distinguish completed production wiring from untested hierarchy combinations.
