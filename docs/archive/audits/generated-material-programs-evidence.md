# generated material programs evidence

[Behavior specification](../../specs/material-spec.md) · [Interface contract](../../specs/material-spec.md)

Retained inspections, implementation stages and limitations. These records describe their stated inputs and versions; they do not establish current implementation or acceptance. Historical recipes are not competing current requirements.

<a id="native-layered-program-authoring"></a>

## Native layered program authoring

Offline tools accept variable-length compiler outputs and rebuild native program
arrays, shader record tables, literal-LZ4 segments and serialized-file/UnityFS
lengths and offsets with alignment. No compiler output receives a fixed reserved slot.
Native pixel programs and pass/reflection data are preserved unless the particular
adapter requires an explicitly authored change. Diagnostic fragments are not production
shaders. The supported authoring seed uses UnityFS 8, serialized-file 22 with type trees
and literal-LZ4 shader data; this describes the inspected container, not every game variant.
Offline parsing and native loading are separate checks and do not establish in-game drawing.

<a id="verification-boundaries"></a>

## Verification boundaries

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

<a id="ordinary-utility-visuals-and-functional-carriers"></a>

## Ordinary utility visuals and functional carriers

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
