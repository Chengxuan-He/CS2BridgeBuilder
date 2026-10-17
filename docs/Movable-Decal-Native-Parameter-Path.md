# Movable decal native parameter-path audit

## Inspected evidence

The following game 1.6.2f1 decompilations were inspected directly. This is a native code-path audit, not rendered acceptance.

- `Game\Game.Rendering\BatchDataSystem.cs`: SHA-256 `D25D35E92AE8AC7624AA3674EC2C74EFF4D8D73D12554BAB84D296CE2C0465DB`.
- `Game\Game.Rendering\LaneProperty.cs`: SHA-256 `58AE4D562EEBC93703CD12363AE5F8A576A54194707D70EAC91F059E4F89EC72`.
- `Game\Game.Rendering\ObjectProperty.cs`: SHA-256 `157818D5D353250D665AC3ECFB2104E7097FE009783132281D76C657553BE3EB`.
- `Game\Game.Prefabs\CurveProperties.cs`: SHA-256 `DF4450968A0DBFFD409A2FCBBD477DDD5415DBD4705929F639096342F4235885`.
- `MaterialLibrary.cs`: SHA-256 `ED70BB80EBB25CD5724F1A79693A94958915B8C0621A11AA223DEAB917EA8218`.

## Ordinary objects do not acquire lane curve parameters

`BatchDataSystem.UpdateBatchDataJob.Execute` dispatches Object, Net and Lane using an if/else-if chain. The Object branch calls UpdateObjectData; a child object does not additionally run UpdateLaneData. UpdateLaneData reads Game.Net.Curve and writes LaneProperty.CurveMatrix, CurveParams and CurveScale (around lines 1219-1237). ObjectProperty has no corresponding curve entries.

`CurveProperties` stores tiling, smoothing, length and curve behavior flags on a render prefab. It contains no serialized per-object Bezier matrix, curve parameter vector or scale vector. Adding this component to an ordinary bone-parented decal object does not supply the missing lane-instance buffers.

Consequence: retaining an unchanged CurvedDecal surface on a StaticObjectPrefab is not an established persistent conversion. The generator must not use that material swap as a substitute for supplying native batch inputs. This conclusion is scoped to the inspected ordinary-object path; it does not claim that every possible native entity arrangement has been exhausted.

## Material templates are separate from saved surface properties

MaterialLibrary.GetMaterialDescription, GetMaterial and GetMaterialHash search the existing m_Materials list. Unrecognized templates return null or -1. This class has no persisted custom-shader installation mechanism. A saved Surface holding floats and texture references is not evidence that a newly compiled shader/template would be registered during a cold start without Bridge Builder.

No runtime material-library registration or shader patch has been added. Such a callback would violate manager-independent saved bridges unless a separate native persistent loading path is established. The inspected class alone does not prove that no other native importer could provide such a path.

## Effect on current implementation

The receiver sampler and DefaultDecal resource writer are uncalled construction stages. They are not a completed representation: the previously measured camera-distance/endpoint-opacity difference still prevents claiming exact curved-decal equivalence. The ordinary-object shortcut is now specifically ruled out by batch dispatch and property ownership.

Next investigation must establish a native representation that supplies both moving attachment transforms and correct curve data, or a native persistently loadable rendering resource with the required formula. Do not silently drop markings, flatten them into pre-weather road albedo, or clear the road builder unprojected-decal failure merely because an intermediate resource can be saved.

## Scope and validation

No runtime code changed in this audit. No build, shutdown, deployment or bridge deletion was needed. Existing movable implementation remains unfinished; human visual, traffic and cold-start acceptance has not occurred.
## ObjectSubLanes do not inherit animated curve updates

Further native source inspection shows CreateObjectLane constructs world-space Bezier points
from Game.Objects.Transform (LaneSystem lines 4594-4597). Its owner query requires Updated or
Deleted, and parent-mesh values participate in elevation/terrain handling. RelativeObjectSystem
reads the procedural BoneHistory and writes InterpolatedTransform for rendering; it does not
rebuild these lane curves or add their owner to LaneSystem's Updated query. RelativeBoneSystem
resolves the attachment's bone indices, rather than updating Game.Net.Curve. Consequently, adding
ObjectSubLanes to a bone-parented child does not establish a native moving-curve path. This rules
out that specific shortcut; ordinary attached object geometry still follows native bones.

## Surface serialization does not supply a curve matrix

A further inspection of `Colossal.AssetPipeline.Surface` confirms that its property dictionaries
contain textures, floats, integers, vectors, colors and keywords, but no matrix dictionary.
`ToUnityMaterial` calls SetFloat, SetInt, SetColor and SetVector; it does not call SetMatrix.
This rules out the proposed direct Surface-matrix serialization shortcut in this implementation.
It does not establish that every unrelated game asset format lacks matrix support.

`BatchManagerSystem.InitializeInstanceProperties<T>` registers instance properties separately
for Object, Net and Lane mesh types. CurveMatrix is a LaneProperty of type float4x4 and is
registered as overridden in the batch, with the Lane mesh-type condition. Saving arbitrary
same-named vectors in an object Surface therefore does not establish the required lane matrix
input. No such unsupported material workaround has been implemented.

The user has been asked whether native DefaultDecal distance fading is an acceptable deliberate
difference. Until that choice is supplied, exact equivalence remains the requirement; no answer
is inferred from elapsed time. Independent geometry work continues.

## Serialized zero smoothing is not runtime zero

A candidate exact subset was checked: serialized `CurveProperties.m_SmoothingDistance = 0` would make the endpoint exponent one if the runtime value were also zero. The native initialization path disproves that shortcut. `MeshSystem` sets `MeshData.m_SmoothingDistance = 0.001f` for every initialized render at line 431, then accepts the component value only when it is greater at lines 750�C752. Thus serialized zero remains a positive runtime distance.

`ManagedBatchSystem` forwards the initialized MeshData value to the material property block (line 1029). `BatchDataSystem` supplies the same value to the loaded and unloaded transform builders (lines 1217�C1218). `MovableLaneVisuals.Batch` already copies the initialized MeshData field rather than rereading the serialized component. Do not change that path to use the apparently zero component value.

| Audited render | Serialized smoothing distance | Native initialized distance |
| --- | --- | --- |
| `Car Lane 3 Mesh` | 0.6000000238418579 | 0.6000000238418579 |
| `Highway Lane 4 Mesh` | 0.800000011920929 | 0.800000011920929 |
| `Crosswalk Line 2 Mesh` | 0.0 | 0.0010000000474974513 |
| `Subway Sleepers Lod Mesh` | 0.0 | 0.0010000000474974513 |
| `Train Rails Cover Mesh` | 0.05000000074505806 | 0.05000000074505806 |
| `Train Sleepers Lod Mesh` | 0.0 | 0.0010000000474974513 |
| `Tram Rails Base Mesh` | 0.10000000149011612 | 0.10000000149011612 |
| `Tram Rails Mesh` | 0.05000000074505806 | 0.05000000074505806 |

This table covers the eight decal renders present in the existing movable audit, not every installed road component. The exact runtime minimum binary32 value is 0.0010000000474974513 m. A small nonzero transition does not establish exact DefaultDecal equivalence or authorize omitting it. The general zero-smoothing branch in the projection helper is not proof that any audited native decal reaches that branch.

Source evidence:

- `Game/Game.Prefabs/MeshSystem.cs`: SHA-256 `95e132b8756b0f4345a7c962e039eaff6b00ab80bcfe6925948b30da53f746a7`.
- `Game/Game.Rendering/ManagedBatchSystem.cs`: SHA-256 `90b384639bef7928b7c4756122c0ad04356a5f7fb14ee2c754f3e40ca18f4676`.
- `Game/Game.Rendering/BatchDataSystem.cs`: SHA-256 `d25d35e92ae8ac7624aa3674ec2c74eff4d8d73d12554bab84d296ce2c0465db`.

No runtime code changed. The candidate shortcut was rejected before implementation; endpoint/camera-fade compatibility and the pending deliberate-difference decision remain unresolved. No decal batches were dropped and the existing explicit failure for unprojected batches remains.


## Accepted conversion decision (2026-10-16)

The user has now explicitly accepted native DefaultDecal distance fading. Earlier pending-decision statements above are historical and no longer block this conversion. The native curved-road fade formula is not claimed to remain identical. No custom shader is introduced.
