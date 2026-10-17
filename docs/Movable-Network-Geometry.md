# Movable network geometry context

## Purpose

Selected-road conversion needs native endpoint and intersection geometry before node lanes,
markings and network attachments can be resolved. The existing straight-edge preview lane builder
does not provide this context. `MovableNetworkGeometry` runs the installed game's geometry jobs
synchronously inside a prepared private ECS world. It does not create a city bridge, replace native
geometry algorithms, or add a persisted mod component.

## Native sequence and field audit

The source evidence is the installed 1.6.2f1 `Game.Net.GeometrySystem.OnUpdate` and its nested jobs.
The following order retains every dependency, including both node-geometry iterations.

| Stage | Native job | Audited fields |
| --- | --- | --- |
| 1 | AllocateBuffersJob | 3 |
| 2 | InitializeNodeGeometryJob | 17 |
| 3 | CalculateEdgeGeometryJob | 28 |
| 4 | FlattenNodeGeometryJob | 11 |
| 5 | FinishEdgeGeometryJob | 9 |
| 6 | CalculateNodeGeometryJob, iteration 0 | 19 |
| 7 | CalculateNodeGeometryJob, iteration 1 | 19 |
| 8 | CalculateIntersectionGeometryJob | 15 |
| 9 | CopyNodeGeometryJob | 4 |
| 10 | UpdateNodeGeometryJob | 14 |

Component and buffer lookups and chunk handles are bound to the private world, respecting native
ReadOnly attributes. The remaining fields are the edge list, intersection buffer, height map,
terrain data/bounds, loaded flag and iteration index. Unknown fields fail explicitly. The private
native intersection type is allocated through a compiled generic NativeList factory; no layout,
field offsets or allocator-constructor ABI are copied. Chunk masks use the installed Execute
signature. All temporary containers are disposed, including when native invocation fails.

## Caller obligations

The caller must prepare all nodes, edges, connections, curves, geometry components, composition
entities and referenced prefab data in the same private world. Queries include all EdgeGeometry
and NodeGeometry entities, matching the native full-geometry query. The loaded flag controls the
native initialization behavior; it does not select a partial query in this helper.

The caller supplies initialized TerrainHeightData and must complete its writer before invocation.
The helper borrows those arrays without disposing them and completes private-world tracked jobs
before synchronous geometry calculation. It must never receive an uninitialized terrain value or
be used to modify the default game world. Flat terrain and endpoint geometry are not silently
substituted. Source-world terrain access and its dependencies must be established by the caller.

No entity identities, temporary geometry components, terrain arrays or job buffers are persisted.
The eventual conversion consumes the calculated context and persists only the owned native assets
specified by the asset-persistence contract.

## Validation and remaining integration

The field inventory above was compared with all nine native job declarations. Release compilation
passed without warnings. Deployment verified 18 backup originals and 12 installed payloads in
`artifacts/backups/movable-native-network-geometry-20261016-064946`.

This helper has no final generator caller yet. A complete prepared-world builder, integrated lane
generation, selected-road conversion and all ten final generator routes remain required. Native
runtime invocation and human in-game geometry, motion, traffic, shipping and cold-start acceptance
are not established by compilation. No existing bridge output changes and no bridges were removed.

## Primary node and edge lanes

`MovableNetworkLanes.CreatePrimary` calls the installed LaneSystem.UpdateLanesJob chunk method for
prepared, updated network nodes and edges. It then runs the native reference-maintenance sequence:

| Native job | Audited fields | Responsibility |
| --- | --- | --- |
| LaneSystem.UpdateLanesJob | 96 | Native primary node and edge lanes |
| UpdateLaneReferencesJob | 12 | Owner SubLane references and native path-method flags |
| FillNodeMapJob | 2 | Map skipped connections to surviving path nodes |
| FixSkippedLanesJob | 11 | Repair lane references using that map |
| UpdateLaneIndicesJob | 14 | Native ordering and master/slave lane indices |

All five declarations were audited against the installed native source. The lane-generation
overload is selected by its exact chunk/int signature, avoiding the separate int/list overload.
Each stage plays its command buffer into the private world before the next stage binds new handles.
Chunk arrays do not survive structural playback. Native ReadOnly annotations control lookup and
handle access. Unrecognized job fields fail explicitly instead of receiving default data.

The caller supplies initialized terrain, a private-world theme entity, traffic handedness,
RandomSeed and remapped BuildingConfigurationData. Prepared owners require Updated, SubLane and
the full native node/edge geometry, prefab, composition, connectivity and random-seed components.
This method handles live node/edge owners, not object-owner lanes or editor construction. All
referenced lane prefab entities and archetypes must already belong to the private world. Created
and Deleted primary lanes are processed by native reference maintenance; secondary lanes are
excluded from that reference query as in the original system.

This is a primary-lane stage, not a complete rendered-lane pipeline. The separate overlap and
secondary stages below must follow in the final context builder before marking extraction.
No temporary lane identity or component is saved in bridge prefab assets.

Release compilation passed without warnings and deployment verified 18 backup originals and
12 payloads at `artifacts/backups/movable-native-primary-lanes-20261016-065155`. The helper is not yet
called by a final generator; runtime invocation and in-game acceptance remain unverified. No
existing bridge generation output changed and no bridges were removed.

## Overlaps and secondary lanes

`CalculateOverlaps` evaluates the entire prepared private network using native full/loaded queries
and `m_UpdateAll = true`. This retains the complete native overlap algorithm; the incremental
AddNonUpdatedEdgesJob is unnecessary for this full-context mode. It runs these native jobs:

| Native job | Audited direct fields |
| --- | --- |
| UpdateLaneOverlapsJob | 19 |
| ApplyExtraOverlapsJob | 3 |
| SortLaneOverlapsJob | 4 |
| CollectLaneDirectionsJob | 13 |
| UpdateLaneFlagsJob | 21 |

The audit counts only direct job fields, excluding fields of nested scratch types. Native source
and target map value types and the extra-overlap queue value type are resolved from the installed
system. Compiled generic factories allocate their containers; parallel writers and subsequent
readers share those containers. Allocation failures and job exceptions dispose every successfully
allocated container. Map capacity follows the native primary-lane query count.

`CreateSecondary` invokes SecondaryLaneSystem.UpdateLanesJob (42 audited fields), then
SecondaryLaneReferencesSystem.UpdateLaneReferencesJob (12 fields), retaining native path methods.
The reference query includes Created/Deleted secondary lanes. The generation query selects
updated live network nodes and edges, consistent with this helper's network-only scope.

The installed SystemOrder places LaneReferencesSystem before LaneOverlapSystem, then connection
and traffic-light initialization and SecondaryObjectSystem before SecondaryLaneSystem. Secondary
references follow in Modification5. Consequently the new methods remain separate: the context
builder must supply the intervening initialization rather than assume that primary lanes,
overlaps and secondary lanes alone reproduce every native state. These methods have no final
generator caller yet. They do not publish assets or run city simulation.

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-secondary-lanes-20261016-065329`. No existing bridge output changes
and no bridges were removed. Runtime invocation, complete context construction, final generation
and human acceptance remain outstanding.

## Signal initialization and calculation entry point

`CalculateNetwork` now connects the private geometry, primary lanes/reference maintenance,
full-context overlaps, native signal initialization and secondary lanes/reference maintenance in
that order. It requires an already prepared network-only world. It is not a topology builder and
does not import road prefabs, create city entities or publish a bridge.

`InitializeSignals` runs the installed TrafficLightInitializationSystem.InitializeTrafficLightsJob
using its native TrafficLights/Updated query. All 25 fields were audited; only native ECS lookups
and handles are required. It writes TrafficLights and LaneSignal while reading ordinary car and
pedestrian lane data. SecondaryLaneSystem does not consume LaneSignal, but native secondary-object
signal placement does. The initialized context therefore supports subsequent attachment resolution
without treating lane IDs alone as signal-group information.

Native movable-bridge detection starts from a level-crossing TrafficLights owner. It searches
direct SubObject entries carrying PointOfInterest and a PrefabRef whose prefab has MoveableBridgeData.
For a subnode owned by an edge, it chooses the nearest edge endpoint and searches that endpoint's
SubObject buffer. The native job assigns the MoveableBridge/IsSubNode flags and groups from the
mechanism lift offsets and connected lane path nodes. Waterway lane handling and physical signal
flags remain native. The context builder must reproduce these relationships before invoking the
calculation entry point; the helper does not fabricate a mechanism when that graph is missing.

The prepared world must contain no building/area owners or their connection work. Those city
connection systems are not run by this network-only calculation. Mechanism objects needed for
signal identification must already exist. Ordinary secondary-object selection occurs separately
against the calculated network; that resolver reads the resulting lanes and signals rather than
mutating them. This restriction must be checked when constructing the context, not generalized to
arbitrary city entities.

Release compilation passed without warnings; 18 originals and 12 payloads were verified at
`artifacts/backups/movable-native-network-sequence-20261016-065457`. No final generator calls
CalculateNetwork yet, so no existing bridge output changed and no bridges were removed. Complete
topology/import construction, runtime execution, persistence and human acceptance remain required.

## Private prefab import

`MovableNetworkPrefabs` imports the existing selected-road prefab graph into a caller-owned private
world. Its explicit list now covers 59 component/buffer record types used by native geometry, lanes,
section composition, signals and object selection. Native instance components are not copied from
the city. The importer retains original PrefabBase references for later visual/object extraction.

Each source entity must resolve to a valid original prefab. Before reading its component payload,
the importer applies the existing path-based foreign-bridge exclusion. This applies recursively,
including placeholder alternatives, requirements, utility node objects, lane alternatives and
section/piece dependencies. There is no same-CID exception permitting another bridge's private data.

One private entity is cached per source before recursively importing its references, preserving
cycles and identity. Direct Entity fields are rewritten to private entities. Direct EntityArchetype
fields are rebuilt from the same component types in the private world, including all primary,
master and slave lane archetypes. Buffer elements are fully remapped before obtaining the target
buffer, because recursive imports can perform structural changes. PrefabData enabled state is
preserved. Missing or rejected external sources fail the operation; the caller must discard a
failed temporary context rather than reuse its partly imported graph.

The installed record declarations were audited for reference placement: Entity and EntityArchetype
fields are direct fields in the copied records. Remaining nested fields contain geometry, numeric,
color or flag data; they are copied intact. The remapper is intended for those audited records and
remapped configuration records, not arbitrary city components with embedded entity references.

This import changes no prefab names, CIDs, files, materials, fees or shared source archetypes. It is
temporary ECS preparation, not dependency snapshotting or persistence. Private entity IDs and
archetypes are never saved into generated bridge assets.

Release compilation passed without warnings; 18 originals and 12 payloads were verified at
`artifacts/backups/movable-network-prefab-import-20261016-065648`. Complete topology/composition
construction and the final generator caller remain outstanding. No existing generator uses this
importer yet, and no existing bridges were removed. Runtime execution and in-game acceptance are
still unverified.

## Native node and edge composition construction

`CreateComposition` creates a private composition using the imported road's actual node or edge
composition archetype, selected by CompositionFlags.General.Node. It sets PrefabRef and the
requested flags, obtains pieces with native GetCompositionPieces, and adds TerrainComposition
when selected pieces contain NetTerrainData. This follows CompositionSelectSystem.CreateComposition
without forcing edge/elevated flags or replacing node data with the edge composition.

The resulting NetGeometryComposition cache is created locally on the imported prefab; city
composition-cache entities are never imported. Each returned composition is a private-world
entity and must be assigned to the matching edge/start-node/end-node slot by the topology builder.

CalculateNetwork now initializes pending compositions before geometry. The installed
NetCompositionSystem.InitializeCompositionJob has 37 audited fields and populates native lane,
carriageway, object, area and crosswalk buffers, native composition dimensions/LOD and applicable
road, track, pathway, waterway, taxiway and terrain attributes. Its input importer was extended by
11 records, including RoadData, NetCrosswalkData, MeshData and NetPieceArea. No new shader, material,
fee formula or persisted resource is introduced. Archetype entity creation selects the installed
EntityArchetype overload explicitly to avoid the unavailable Mono ReadOnlySpan overload.

The native job appends several output buffers. After successful initialization, its private
Created markers are removed so later calculations do not append duplicate objects or crosswalks.
On failure the temporary context must be discarded. No live-world lifecycle marker is changed.

Release compilation passed without warnings and deployment verified 18 originals and 12 payloads
at `artifacts/backups/movable-native-composition-context-20261016-065850`. Native declarations and
additional direct entity fields were audited. Complete network topology, final generator calls,
runtime invocation and human acceptance remain outstanding; no existing bridges were removed.

## Private network topology and required components

The calculation system now exposes creation methods for nodes, edges and mechanisms. Node and edge
entities use the imported NetData archetypes, retaining the native component sets. The caller supplies
measured Node poses, seed values, optional native Elevation and explicit edge curves. Curve length
uses native MathUtils.Length. Edges receive separate edge/start-node/end-node composition references
and update both endpoint ConnectedEdge buffers. Middle connections update ConnectedNode and the
node's ConnectedEdge buffer. Native equality semantics prevent duplicate connectivity entries.

SetNetworkOwner establishes the initial Owner/SubNet relationship for a newly prepared child; it is
not a live reparenting operation. CreateMechanism uses imported ObjectData.m_Archetype and supplied
Transform, PointOfInterest and seed values, and records Owner/SubObject membership. The native
MoveableBridge component supplies PointOfInterest in that archetype. ObjectData is the 59th imported
record, with its archetype remapped into the private world. These methods construct temporary
entities only; they neither place bridges in a city nor derive mechanism positions from geometry.

CalculateNetwork now runs native NetComponentsSystem.CheckNodeComponentsJob after geometry and
before primary lanes. Its native node/edge query uses Updated unless loaded is requested. The job
derives TrafficLights, level-crossing, roundabout and gate component changes from the connected
compositions and geometry. Its command buffer plays into the private world before later jobs bind
fresh handles. Signal component presence is therefore not guessed by the topology methods.

The caller still must assemble the actual measured bridge arrangement, select valid composition
states, provide all ownership relationships and initialize any other required native instance state.
These primitive construction methods are not evidence that all ten arrangements are assembled or
that an arbitrary incomplete graph is usable. Entity identifiers remain local to the disposable
world and are not persisted in assets.

Release compilation passed without warnings; 18 originals and 12 payloads were verified at
`artifacts/backups/movable-native-network-topology-20261016-070105`. No final generator invokes the
new construction methods yet. Full arrangement integration and human runtime acceptance remain
outstanding, and no existing bridges were removed.

## Consuming the calculated context

MovableLaneVisuals now accepts a private world, an explicit ordered set of node/edge owners, the
prefab importer, traffic handedness and a common closed-pose origin. It reads each owner's native
SubLane buffer, deduplicates lane entities and excludes deleted, hidden and master lanes. Original
lane prefabs come from the importer's provenance map. Curves, optional EdgeLane parameters and
render rejection flags come from the calculated private entities, including native overlap results.

The renderer-state predicate is shared with the existing preview lane path without changing that
path's behavior. Non-geometry traffic lanes remain valid network lanes but do not produce visual
batches. A non-geometry lane carrying SubMesh render data fails explicitly instead of silently
dropping that data. Original lane and render prefabs pass source-isolation checks before payload
reads. Native material references, tiling, curve parameters and decal classification flow through
the existing visual converter. The origin is a translation in the common closed-pose frame, not an
implicit rotation or a guessed mechanism transform.

MovableRoadObjects.Extract also accepts the initialized private composition and its importer. It
reads NetCompositionObject directly, resolves original ObjectPrefab references and clears private
entity references through the existing Rule representation. Both edge and node resolvers can now
consume those rules without recomposing objects in a second world.

The old straight-edge extraction overload remains for existing callers. The new overloads connect
the prepared context to existing visual and object consumers, but a final bridge generator still
must assemble the measured arrangement and invoke them. They do not prove that unprojected decals,
surface conversion, structural replacement or persistence are finished.

Release builds passed without warnings. The final deployment verified 18 originals and 12 payloads
at `artifacts/backups/movable-context-object-rules-20261016-070314`; the lane-visual integration was
also deployed at `artifacts/backups/movable-context-lane-visuals-20261016-070254`. Existing generation
routes do not use the new context overloads, and no bridges were removed. Runtime invocation and
human visual acceptance remain unverified.

## Unregistered generated mechanisms

Generated mechanisms do not yet have an entity in the loaded game's PrefabSystem when the generator
assembles them. Importing such an object through the loaded-prefab path would either fail or tempt
an incorrect dependency on a live DLC prototype. `StageMechanism` therefore accepts the assembled
StaticObjectPrefab directly, together with its generated ObjectGeometryData and PlaceableObjectData.

The method preflights its dependency graph for foreign bridge paths before reading archetype
metadata. Native dependency enumeration includes null optional references; those are not traversed.
Cycles are visited once. The root and all actual referenced prefabs still pass source-isolation checks.
It collects the object's active native components and uses their GetArchetypeComponents methods,
then adds the native Created/Updated markers and builds that archetype in the private world.

The temporary prefab entity receives ObjectData, the caller's complete geometry/placement records,
NetObjectData and MoveableBridgeData. The installed NetObject.Initialize and MoveableBridge.Initialize
populate the latter records from the generated author's component fields. This retains native
composition requirements, lift offsets and movement time without looking up an archetype prefab.
Original-prefab provenance is recorded for the private entity. CreateMechanism can then instantiate
it and establish the native Owner/SubObject/PointOfInterest path used by signal initialization.

This is the metadata subset consumed by private network calculation, not general object-prefab
registration or full rendering initialization. The caller must provide geometry and placement
records derived from the generated asset, including native fee fields; defaults or another bridge's
records are not substitutes. The method does not calculate those records, initialize every object
component, save resources, register city assets or run a runtime movement controller.

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-unregistered-mechanism-context-20261016-070538`. Final generator integration
and runtime acceptance remain outstanding. No existing bridge output changed or bridges were removed.

## Installed object and mesh initialization evidence (2026-10-16)

The SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`
identifies `all-movable-audit.json`, not the game assembly. The extraction script hashes that JSON
as `source_sha256`. The installed `Cities2_Data/Managed/Game.dll` instead has SHA-256
`AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`.
These different hashes are not evidence of a game update.

Both `Game.Prefabs.ObjectInitializeSystem` and `Game.Prefabs.MeshSystem` were freshly decompiled
from that installed DLL using `ilspycmd --disable-updatecheck`. Their complete source matches the
existing research source after normalizing line endings and terminal whitespace. The update-check
banner from an earlier command was tool output, not C# source. This comparison verifies these two
classes only; it is not an assembly-wide compatibility claim.

### Required initialization order and isolation

1. The generated object's active component initializers provide native placement flags, fees,
   movement settings and standing-leg parameters. Calling `StandingObject.Initialize` alone does
   not derive the object's complete geometry.
2. Render-prefab metadata must exist before native object geometry initialization. `MeshSystem`
   derives bounds, counts, LOD/shadow biases, stack/decal/base flags and related records from the
   generated render graph. `ObjectInitializeSystem` reads those records when computing physical
   bounds, rendering LOD, layers, mesh flags and object placement behavior.
3. Object initialization writes back to the render entities, including LOD entities. All such
   entities must belong to the disposable calculation world. Calling the city system on shared
   render entities would violate source isolation even if the generated root itself were private.
4. The native object's `PrefabData.m_Index` must resolve in the same world's `PrefabSystem`.
   The current network importer copies source indices for its explicit metadata consumers and
   cannot be passed unchanged to native object initialization in another world.

`MeshSystem.OnCreate` also creates `BatchManagerSystem` and `ManagedBatchSystem`; its update builds
material indices and batch-related data. It is not a standalone bounds calculator. A metadata-only
implementation must account for the fields consumed by object initialization without accidentally
starting a second live rendering pipeline. In particular, copying source `MeshData` unchanged is
incorrect for widened or replaced render meshes: the native biases depend on the generated bounds
and surface area.

The generated road render path already sets `RenderPrefab.bounds`, `meshCount`, `indexCount` and
`surfaceArea` in `MovableRoadAssets`. Native metadata reads those properties directly; lack of a
persistent GeometryAsset during preview does not by itself mean the values are unavailable.
Preview mesh ownership must still remain with `PreviewGeometry`.

This inspection resolves the suspected source-version mismatch and identifies the concrete inputs
for replacing the external geometry/placement arguments to `StageMechanism`. That replacement and
its final generator caller are not implemented by this documentation change. No runtime code,
installed payload or existing bridge was modified during this inspection.

## Generated render metadata staging

`MovableNetworkPrefabs.StageRender` now stages generated RenderPrefab metadata in the existing
private network world. It checks source ownership before reading each prefab, memoizes identity
before traversing LOD references, and exposes original-prefab provenance through `Original`.
LOD entities are staged recursively and ordered by descending index count with stable equal-count
ordering, matching the native valid-reference path. Missing or foreign LOD references fail rather
than producing an incomplete generated graph. Dispose the temporary context after a staging failure.

The scalar projection follows the inspected `MeshSystem.OnUpdate`:

- Bounds, submesh/index counts, smoothing distance and initial shadow bias come from the generated
  render prefab and native initialization constants.
- Explicit LOD biases and the native small-mesh, surface-area and shape adjustments retain their
  original operation order. These native rendering thresholds do not classify structural parts.
- Underground and waterway layers, impostor/decal/stack/skeleton flags, curve tiling and length
  overrides, base flags and default-mesh LOD limits retain their native meanings.
- Active component schemas determine whether the native LOD buffer exists; a disabled LodProperties
  component must not activate that branch merely because the component can be found by type.

Recursive staging finishes before acquiring the parent's DynamicBuffer, avoiding invalid buffer
handles after structural changes. No city render entity, shared prefab, material or shader is
modified. The staged data is disposable ECS metadata, not a new persistent asset. GPU batches,
material indices, procedural animation buffers and object-specific initialization remain separate.
The implementation does not start MeshSystem or its rendering-system dependencies.

The source audit covers the scalar MeshData assignments and valid LOD ordering in the installed
native implementation. Release compilation passed and deployment verified 18 original files and
12 installed payloads at `artifacts/backups/movable-render-metadata-20261016-071524`.

This is a prerequisite for the generated mechanism's native geometry initialization. StageRender
currently has no generator caller; StageMechanism still accepts externally supplied geometry and
placement records. Native prefab-index registration, object initialization and the final style
routes remain to be connected. No existing route changed, no existing bridges were removed, and
runtime or human visual acceptance is not claimed.

## Generated mechanism geometry connected to staging

`StageMechanism` now accepts only the assembled StaticObjectPrefab. The caller-supplied
ObjectGeometryData and PlaceableObjectData arguments have been removed. The existing graph
preflight checks ownership before dependency payload reads, and the required MoveableBridge and
NetObject components must be active. All ten recorded B&P mechanisms use the static pillar/net/
standing-object metadata path inspected for this change.

The method stages each render graph and initializes the applicable native metadata components in
their authored component order. Pillar anchor offsets, standing legs, network node offsets,
composition flags, movement parameters and any PlaceableObject fees are obtained by invoking their
native Initialize methods. Additional native placement components retain their own initialization
semantics. The code does not construct a custom price or copy metadata from a live DLC mechanism.

Render staging now registers render prefabs in the disposable world's native PrefabSystem, which
allows native geometry initialization to resolve render and LOD entities in that same world.
Ownership preflight precedes AddPrefab, whose availability/unlock checks may traverse dependencies.
This registration does not register the object in the city or publish a saved asset.

The adapter invokes the installed ObjectInitializeSystem's ObjectGeometryPrefab InitializePrefab
overload, rather than duplicating its mesh-state, bounding-box, pivot, standing-leg and LOD formulas.
All geometry and mesh writes target private entities. The static movable-pillar collision flags
follow the native OnUpdate branch immediately after that overload. SubMesh records and calculated
stack/locale metadata are retained in the temporary entity. This connects StageRender to a consumer.

Do not call the full ObjectInitializeSystem update on this mixed network context. Imported city
PrefabData indices are intentionally not a local registry index; only the render graphs used by
this direct native routine are registered locally. The adapter does not initialize the whole
prefab graph, child-object requirement masks, renderer batches or persisted asset registration.
Those are outside this temporary network calculation. The audited network GeometrySystem consumes
placement flags/offsets and geometry bounds for the fixed node; lane calculation also consumes
geometry flags. A final generator must still instantiate the staged mechanism into that topology.

Release compilation passed and deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-mechanism-geometry-20261016-071803`. The former external geometry
arguments have no remaining callers. StageMechanism itself still has no final generator caller.
Runtime invocation, selected-road conversion integration and per-style human acceptance remain
unverified; this change is not completed support for the ten styles. Existing routes and bridges
were retained.

## Complete temporary network orchestration

`MovableNetworkContext` now owns one disposable world and connects the previously separate
mechanism staging, topology, composition, geometry, lane and extraction helpers. Its constructor
accepts explicit measured node, edge, mechanism, middle-connection and parent relationships.
There is no hardcoded number of nodes, three-branch section rule, bridge-style switch or inferred
hinge. Each edge supplies its own edge/start-node/end-node composition flags. The caller remains
responsible for applying the selected road's and archetype's actual state rules.

Construction performs the following sequence:

1. Import selected source network prefabs and instantiate the supplied nodes.
2. Build each edge's three requested native composition entities and create its endpoint links.
3. Apply explicitly supplied middle connections and auxiliary-network owner relationships.
4. Stage each generated mechanism, derive its geometry/placement metadata and instantiate it under
   the specified network owner with the supplied transform, point of interest and random seed.
5. Import the source theme and remap every Entity field in BuildingConfigurationData through the
   same ownership-checked prefab importer. City entity IDs must not be passed as private identities.
6. Run the existing native composition, geometry, component, primary-lane, overlap, signal and
   secondary-lane sequence with a complete freshly assembled context.

The caller owns TerrainHeightData and must complete its producer before this synchronous operation.
The context does not create fake terrain or add a road to the city. Construction failure disposes
the temporary world and propagates the exception. Successful callers must dispose the context after
consuming its geometry and extracting outputs.

LaneVisuals extracts the requested ordered set of node/edge owners from that same calculated world.
ObjectRules uses its initialized native composition. Pieces returns original permitted piece prefab
references and scalar composition data, clearing each temporary piece Entity before returning it.
The piece labels record order within the supplied composition; they are not recovered source-section
paths or an archetype role classifier. Native owner entities remain context-local and cannot be saved
or used after disposal. Owner-level object resolution and base-mesh evaluation can consume the live
context before it is disposed.

The entry point now calls StageMechanism, CalculateNetwork and the prepared-context extraction
methods. Final bridge generators still do not construct this context: supplying all per-style
measured arrangements, compiling the visible surfaces and publishing the saved graph remain work.
The original straight-road compiler is unchanged and must not stand in for node geometry.

Release compilation and both deployments passed. The final configuration-reference deployment is
`artifacts/backups/movable-context-configuration-20261016-072220`, with 18 originals and 12 installed
payloads verified. The preceding context deployment is
`artifacts/backups/movable-network-context-20261016-072137`. No existing route changed and no existing
bridges were removed. No game operation or visual-generation unit test was performed; runtime
invocation and the full per-style acceptance matrix remain unverified.

## Native composition vertex deformation and context surface compilation

The temporary network can now compile an edge, its start-node contribution or its end-node
contribution into explicit selected-road triangles. `CompileSurface` reads the actual calculated
EdgeGeometry or StartNodeGeometry/EndNodeGeometry and calls the installed
BatchDataHelpers.CalculateEdgeParameters or CalculateNodeParameters. It does not derive node
geometry from a straight span or assume all nodes share one deformation matrix.

Each visible composition occurrence is packed through the existing native mesh-generation helper.
The helper's straight preview bounds and material-property matrices are not consumed by this path;
its packed mesh is evaluated using the actual network parameters. Material slots retain original
SurfaceAsset references for the measured templates 17/33. Outputs contain explicit vertices and
no private Entity references. The caller must choose visible closed-road compositions before
applying the moving-span hiding conditions. Empty invisible contributions remain empty; they do
not prove that a complete moving surface has been generated.

The new CPU evaluator follows the inspected non-terrain path of
`shader-programs/581-106.asm` (NetCompositionMeshLit vertex program):

- Color R/G select the two curves from eight composition matrices.
- Color A's low three bits choose consecutive synchronization groups; its high bit forces the
  endpoint width interpolation. Native epsilon-ordered breakpoints map packed position X to the
  synchronized transverse coordinate.
- Cubic Bezier position and derivative evaluation use packed position Z. The transverse coordinate
  controls geometry, while UV Z controls mapping-distance interpolation. The native endpoint skew
  correction is retained rather than treating UV Z as the geometry coordinate.
- Normals and tangents use the derivative-derived across/up/forward frame, with the native
  zero-derivative fallback directions. The composition transform and requested common origin
  produce explicit object-space output.
- UV Y retains the native distance-rate versus negative non-tiling convention; the intermediate
  UV Z selector is cleared while UV W is retained for surface appearance conversion.

Source correspondence: the shader's selector branches precede the synchronization block beginning
with `mul r0.w, v6.w, l(31.875000)`; cubic evaluation, skew-corrected mapping and tangent-frame
construction follow it. BatchDataHelpers.BuildEdgeMatrix stores the four control points and mapping
distances as columns. CalculateNodeParameters supplies native source/target synchronization vectors
from the composition and edge-node geometry. This is geometry conversion, not a modified runtime
shader or a custom engine rendering loop.

Color B selects terrain sampling in this vertex program. That branch still fails explicitly; the
implementation does not flatten it or infer its GPU texture sample from an unrelated height value.
Unknown matrix selectors and unsupported surface templates likewise fail through normal errors.
The old straight-edge evaluator remains unchanged for its existing caller. Its shared mesh decoder
now also accepts native CompositionParameters for the new context path.

Source inspection and Release compilation passed. Deployment verified 18 originals and 12 payloads
at `artifacts/backups/movable-context-surface-20261016-072602`; the preceding evaluator deployment
is `artifacts/backups/movable-composition-deformation-20261016-072456`. No visual-generation unit
tests or game control were performed. Source correspondence and compilation do not establish
in-game visual equivalence. Final bridge-style callers, terrain-dependent cases, full material/
marking conversion and the acceptance matrix remain outstanding. Existing routes and bridge assets
were unchanged.

## Closed-road assembly and render-level handoff

`MovableNetworkContext.CompileRoad` now combines an explicit ordered list of edge/start-node/
end-node surface contributions in one common closed-pose frame. It offsets both material indices
and occurrence indices while preserving each triangle's source-triangle identity. Composition
records, object-rule arrays and numeric occurrence starts remain parallel to the supplied
contributions. Diagnostic path strings are not used to recover membership or classify geometry.
The next occurrence start, or total occurrence count for the final contribution, defines each range.

The method extracts the requested native lane owners once, using the existing entity deduplication,
and compiles non-decal lane geometry against the complete combined receiver surface. This prevents
an edge/node seam from truncating receiver lookup to whichever contribution happened to compile
first. Curved-decal batches remain explicit in LaneGeometry rather than being discarded or emitted
as opaque projector boxes. A collection with no visible triangles fails normally.

MovableClosedRoad.Result now holds multiple composition records and per-contribution object rules;
the existing straight-road entry creates a single contribution with occurrence start zero. A new
MovableRoadAssets.Level constructor accepts the compiled result directly and carries its receiver
triangles, source surfaces and lane geometry into the existing appearance, partition, skinning and
owned-asset pipeline. The existing unprojected-decal failure is unchanged. This handoff does not
claim the unresolved moving DBuffer representation has been completed.

The final Release build and deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-road-contribution-ranges-20261016-072904`. The initial assembly/handoff
build is `artifacts/backups/movable-closed-road-context-20261016-072844`. All updated result
construction sites compile; no active fixed-bridge route uses these helpers and no existing bridges
were removed. Full per-style generator calls, remaining appearance conversion and human acceptance
are still required.

## Attachment consumers

The prepared `MovableNetworkContext` now exposes `EdgeObjects` and `NodeObjects`. Pass the same coordinate origin as surface/lane compilation and an original-world theme entity. Each operation obtains the owner's native secondary-object random seed and calls the existing native-derived placement resolver. The node operation preserves its streetlight-result flags. Resolve placements before disposing the context, then bind the returned original-prefab placements to the measured mechanism partition with `MovableRoadObjects.Bind`. These are construction-time operations; no private ECS entity is serialized. Final per-style callers are still required.

### Edge lane-derived attachments

`EdgeObjects` now includes native track endpoint objects and arbitrated utility junction objects, in addition to composition object rules. All three stages share one native selection stream; retained signal candidates consume their original selection/group operations without producing duplicate mechanism children. Track transforms use actual lane curves and native surface-height offsets. Utility membership and priorities come from native lane data, not bridge-name or coordinate heuristics. This completes another input stage; callers must still bind the resulting placements and suppress duplicate stationary visual producers where required.

## Native mechanism placement and road frame (2026-10-16)

The private network context now accepts the prepared mechanism's original NetSubObjectInfo,
its owner's world transform and a native traffic-signal state. It stages the mechanism first,
then applies its local placement and the native aligned Standalone-pillar top anchor using
initialized ObjectGeometryData and PlaceableObjectData. Standing-object and stack-derived
bounds are therefore included; no universal 10 m correction or raw-mesh maximum is used.
All ten audited B&P mechanisms use this Standalone placement branch.

The animation target is obtained from the game's TrafficLightSystem.UpdateMoveableBridge after
anchoring. The context retains the created mechanism entities and exposes RoadToMechanism,
which maps CompileRoad's origin-relative world coordinates into the anchored mechanism's local
frame using the inverse position and rotation. The resulting Pose can be supplied directly to
MovableRoadAssets.Level. The calculation is private construction-time data; it adds no runtime
controller, shader or persisted manager dependency.

Call-chain review found no external MechanismInput constructors or final movable generator
callers. Its sole CreateMechanism call was updated with the new input contract. Fixed generators,
existing saved assets, persistence and UI behavior are unaffected; no bridges were removed.
The final generators still need to supply measured node/edge arrangements, surface contributions,
partitions and complete render assemblies before this path can be accepted in game.

Release builds passed after both increments. The latest deployment backed up 18 original files
under `artifacts/backups/movable-anchored-road-frame-20261016-080848` and verified all 12 installed
payloads against the build output. The preceding placement increment used
`artifacts/backups/movable-native-mechanism-placement-20261016-080822`. Build and hash checks do
not establish actual road alignment, animation or cold-load acceptance.

## Native composition-state selection (2026-10-16)

Private network construction now runs the installed CompositionSelectSystem's
SelectCompositionJob and CreateCompositionJob after connections, ownership and mechanism
SubObjects have been created, before NetCompositionSystem initialization and geometry calculation.
It no longer requires callers to invent final edge/start-node/end-node flags. EdgeInput carries
an optional native Upgraded component instead. The old manual CreateComposition method was
removed after its sole caller was replaced.

The prefab importer now includes NetGeometryEdgeState, NetGeometryNodeState and FixedNetElement
buffers. Each imported geometry prefab receives an empty private NetGeometryComposition cache;
city composition entities are never copied. Native selection applies source state rules,
handedness, elevation, connected-edge node matching and mechanism NetObject flags, including
FixedNodeSize. Native creation shares matching compositions and handles obsolete upgrade flags.
The reflection runner now supplies either an EntityCommandBuffer or its ParallelWriter according
to the actual native job field type. Selection and creation receive fresh handles, and command
playback finishes before composition initialization.

The inspected installed source is Game.Net/CompositionSelectSystem.cs: SelectCompositionJob,
GetEdgeFlags, GetEdgeStates, GetNodeStates, GetNodeFlags and CreateCompositionJob. Imported state
records and FixedNetElement contain no additional entity references requiring bespoke remapping.
The change is construction-time orchestration in a disposable world; it does not patch the engine
or introduce a saved runtime dependency.

Release build and deployment passed. Latest backup:
`artifacts/backups/movable-remove-manual-composition-20261016-081050`; 18 original files backed up
and 12 installed payloads verified. The preceding selection increment used
`artifacts/backups/movable-native-composition-selection-20261016-081024`. No final movable route
yet calls the context, so no existing generated bridge is invalidated or removed. Execution with
real selected roads and full bridge acceptance remain unverified until the final generators are
connected; compilation is not evidence of runtime reflection or geometry success.

## Draw01 closed-node sampling connection (2026-10-16)

Draw01ClosedRoad now constructs and owns a disposable MovableNetworkContext. It uses two straight
144 m sampling approaches around the central node; 144 m is the recorded native segment pitch,
not an assumed moving surface length. The staged mechanism supplies its native 90 m fixed-node
offset. Source-road state selection, node/edge geometry, lanes, crossings and secondary lanes run
through the existing native private-world pipeline after the mechanism is attached.

The compiled surface contains edge 0's EndNode and edge 1's StartNode contributions. Approach edge
surfaces are excluded from the object input. Lane visuals are extracted for the central node, and
Objects() resolves its native secondary objects while the private context remains alive. The
sample stores RoadToObject from the actually anchored mechanism transform. Construction failure
disposes the private world; normal ownership ends through IDisposable. Source theme, terrain,
elevation, upgrades, handedness and random seed are explicit caller inputs. The caller must supply
the visible selected-road source before moving-network layer suppression.

The closed target uses native signal group 1 and Ongoing state, matching Draw01's first lift-offset
entry. This is sampling metadata, not a custom animation controller or city construction request.
No shipping lane is fabricated: navigation compatibility still depends on the final generated
network and native crossing behavior in the real city.

The implementation was committed on bridge/draw-01 as 2be8775 and integrated into dev as dfc5acc.
Release build and all 12 deployed-file hashes passed. Backup:
`artifacts/backups/draw01-closed-node-road-20261016-081219` (18 original installed files).
No existing bridge was removed because the final Draw01 route is still absent. The sampler has
not yet been executed in game. Measured road partitioning, decals, remaining support geometry,
final assembly/persistence and human acceptance are still required; this is not completed Draw01
or all-movable-bridge support.
