# Selected-road object composition and moving attachment binding

## Implemented path

`MovableClosedRoad.TryCompile` now extracts native object-placement rules alongside road
triangles and lane visuals. `MovableRoadObjects.Extract` imports the selected composition's
NetPieceObject buffers into a disposable world, preserving all requirement/spacing fields.
Only the referenced objects' StreetLightData, LaneDirectionData, TrafficSignData and
UtilityObjectData are needed by this composition stage; those are copied read-only.

The installed game's
`NetCompositionSystem.InitializeCompositionJob.AddCompositionObjects` performs condition
filtering, section inversion, lane-direction/sign flipping, streetlight grouping, spacing
overrides and avoidance spacing. Its result is returned as original ObjectPrefab references
plus complete NetCompositionObject records. Temporary entity IDs are cleared before return.
No entity is registered in the city and no native source object is mutated.

Both extraction paths now retain the original NetPiecePrefab and composition-local occurrence
for each surviving object rule. A private trace runs native AddCompositionObjects one piece at a
time against its accumulating output. Existing entries carry temporary identities in Position.y,
which that native merge routine does not read when comparing existing entries. Prefab identity,
lateral position, light layers, spacing and flags remain unchanged. After restoring the original
longitudinal values, every output field and output order must equal the primary native result.
Only verified occurrence indices escape the trace; the temporary entity is destroyed in finally.
The trace does not replace native filtering or change actual placement data.

Placement candidates retain the complete source rule through node streetlight arbitration and
placeholder selection. MovableNodeObjects.Result.Sources aligns with Placements and refers to
the winning original producer even when a concrete selected object has a different prefab.
Lane-derived and utility-derived candidates have no composition rule and remain explicitly null;
null does not imply a carriageway role. Occurrence indices are local to their source composition,
not indices into the combined road or lane render arrays. Runtime trace execution and mixed-role
attachment mapping remain unverified; source-piece identity alone does not prove every attachment
on a mixed sidewalk/road piece has the same motion partition.

`MovableRoadObjects.Bind` takes resolved closed-pose object placements, the measured
road-to-object pose, an explicit partition for each placement and the final owned mechanism.
The partition list must align with the placement list; it is not a road-piece or lane-batch index.
It assigns each anchor to its supplied measured seam tree, transforms position/rotation into object
space and writes native connection IDs into ObjectSubObjectInfo.m_ParentMesh. The moving
road must be merged into the mechanism's first render graph. Fixed-root attachments retain
parent 0. Prefab references, group indices and probabilities are retained.

`TowerFactory.AttachMovableRoadObjects` invokes binding after root creation, appends the returned
children without removing structural attachments or generated markings, and adopts all lamp
copies into the factory's owned graph even when a later binding operation fails. Native lighting
effects therefore remain on the final root, not a temporary staging object. The style must still
supply justified per-placement partitions; this API does not infer attachment roles.

Bone array indices and connection IDs remain distinct. Invalid connection IDs and ambiguous
bone mappings throw errors. Coplanar anchors use the positive region, matching triangle
clipping. Native RenderingUtils.FindBoneIndex performs inverse-bind conversion; applying it
again while generating placements would transform the attachment twice.

## Native evidence

Inspected game 1.6.2f1 sources:

- NetCompositionSystem.cs, AddCompositionObjects, line 398: native rule resolution.
- SecondaryObjectSystem.cs, edge-object generation around line 2529: spacing instantiation,
  random curve offsets, theme/replacement selection and traffic-sign requirements occur later.
- RenderingUtils.cs, FindBoneIndex, line 210: connection lookup and inverse-bind conversion.

The composition stage does not claim to instantiate all objects. In particular, native
spacing, probability, theme/placeholder selection and traffic-sign needs must still be
resolved before calling Bind. Long curved rail/guard geometry follows the lane-geometry
path rather than rigid point-attachment binding. The extraction result must not be mistaken
for already-created child placements or used to bypass these native decisions.

## Validation and remaining integration

Shared implementation was built on dev and deployed. Each of the two deployments verified
18 backed-up original files and 12 installed payload files. No generated bridge was removed:
MovableClosedRoad has no final generator caller yet. Existing fixed-bridge paths are unchanged.
The new extraction call is connected to closed-road compilation; Bind still awaits the final
mechanism assembly caller and native placement resolution.

Compilation establishes API compatibility only. Native reflection invocation, object placement,
near/far motion, night lights, shipping and cold-load persistence still need runtime acceptance.
No game control, construction request or visual-generation unit test was performed.


## Native candidate placement increment (2026-10-16)

MovableRoadObjectPlacement now implements the candidate-placement portion of game 1.6.2f1
SecondaryObjectSystem.CreateSecondaryEdgeObjects. It consumes actual edge geometry, endpoint
composition/geometry, selected-network geometry data and composed object rules. It preserves
minimum length, even spacing, endpoint contributions, avoidance-spacing parity, randomized curve
offset, native curve-length evaluation, tangent blending, local offsets and the native in-loop
probability update. It does not infer endpoint contributions from a bridge's fixed node offset.

The resolver callback receives parent/local/object transforms and shares the same random stream
by reference. This matters because native placeholder selection consumes random values between
placements. Generating all coordinates first with an independent random stream would change later
selection and placement. The callback must resolve native requirements, theme, replacements,
traffic-sign/light needs and probability before producing bound ObjectSubObjectInfo entries.

The placement code was built and deployed on dev. The original-file backup contains 18 files;
12 installed payload files were hash-verified. No existing bridge output changed because the
final generator does not yet call this stage. It is not complete attachment generation: the
resolver callback, generated lane context, native anchoring/elevation and final assembly remain
required. No synthetic visual tests or game operation were performed.


## Native object selection and placement resolution (2026-10-16)

MovableRoadObjectSelection implements the native secondary-object selection stage using read-only
prefab data. It preserves default-theme requirements, AND-of-OR requirement groups, SelectOnly and
IgnoreExplicit semantics, randomized group indices, cached randomization groups, weighted ties,
placement probability and native traffic-sign/light/lane-direction scoring. No name-based choice
or prototype-light substitution is used.

TryResolve consumes a placement candidate and explicit traffic needs, resolves a concrete native
ObjectPrefab, applies the selected orientation flip, native top/center/ground anchoring and native
rotation symmetry. Its ObjectSubObjectInfo is ready for the existing road-frame/bone binding stage.
The saved placement has probability 100 because selection has already applied probability; applying
the original probability again would produce a second independent omission. Resource references are
retained rather than copied into new materials or textures.

Selection was derived from game 1.6.2f1 SecondaryObjectSystem.CheckRequirements and the two
CreateSecondaryObject overloads. The inspected decompiled source SHA-256 is
`c3196ba86b10b5d43518a607b684cc6afacbf1c3b927bd9bc5e200d3031fe21e` after UTF-8 text reading.
The native requirement set for this path contains the default theme only. The shared selector
retains this rule rather than inventing additional requirements.

Both successful increments were built and deployed with 18 original backup files and 12 deployed
payload hashes verified. One missing namespace compile error was corrected before deployment;
the failed build was not installed. CS0649 warnings remain on the traffic-needs fields because
their actual native-lane context producer is not yet connected. They are not evidence that empty
traffic needs are valid: callers must provide measured/generated native traffic context.

The placement visitor, selector and binder still require final orchestration, lane-derived traffic
needs and any applicable per-edge replacement selection. Tree/entity state, full attachment lifecycle
and human motion/night/cold-load acceptance remain unverified. This increment does not create a
finished movable generator or remove any existing bridge. No engine shader or simulation system was
modified; the generation-time selection code emits native prefab placements.


## Edge traffic needs and object orchestration (2026-10-16)

MovableRoadTrafficNeeds now calculates native edge-object traffic needs from an explicitly
prepared lane world. It reads closest eligible vehicle/pedestrian lanes, master/slave links,
merge/turn/forward flags, incoming/outgoing lane aggregation, speed transitions, public transport
restrictions, roundabouts and bicycle restrictions. Connected-edge traversal and district rules
use the native EdgeIterator and AreaUtils paths. It writes no network or simulation data.

MovableRoadObjects.ResolvePlacements connects native spacing candidates to this calculation and
the prefab selector, returning resolved ObjectSubObjectInfo placements in the closed-road frame.
The lane world and prefab source world remain distinct: prefab component-presence flags are read
in the source world, while all lane lookups use the prepared world's owner/endpoint entities.
No source-world entity is substituted for a remapped lane-world entity.

The caller must prepare actual endpoint compositions, edge/node geometry and native node/edge
lanes. Empty fabricated endpoint buffers cannot establish the absence of incoming traffic. The
current BridgePreviewLanes staging path does not yet provide that complete endpoint context,
so the final closed-road/generator caller remains unfinished. Per-edge SubReplacement selection
and its tree-age state also remain outstanding; this entry point does not establish their support.

The shared code was built and deployed twice, with 18 original backup files and 12 installed
payload hashes verified per successful deployment. The remaining nine CS0649 warnings concern
traffic-needs fields not assigned by the copied edge-object path (including crossing/vehicle
masks and secondary speed/sign fields). Node-object signal generation is a separate native path;
this edge calculator does not claim to implement it. No warnings were hidden with pragmas or
invented nonzero values. Existing bridge output and cleanup scope are unchanged.

Human visual, traffic, shipping and cold-load acceptance remain pending. These callable stages
are not a completed movable bridge generator.

## Stationary object suppression in moving network nodes (2026-10-16)

Native `NetCompositionSystem.InitializeCompositionJob.AddCompositionObjects` reads each selected
piece's `NetPieceObject` buffer. `NetCompositionHelpers.TestObjectFlags` tests composition and
section requirements, not hidden mesh layers. Consequently, hiding all road layers does not by
itself suppress stationary lamps or signs.

`MovableSectionConversion` now uses `MovableSectionObjects` for the measured branch requiring
`Node` and `FixedNodeSize` with all layers hidden (-1). This is an explicit moving-node branch,
not a rule for arbitrary layer masks. Ordinary and bottom-only connecting branches retain their
original object behavior.

The pass retains rules whose object has `TrafficLightObject`, including signal-capable placeholders.
Streetlight rules now point to private empty native lighting placeholders, preserving IsLit/AlwaysLit
without stationary lamp emission; see [native lighting representation](Movable-Native-Lighting.md).
It removes the other object rules from the private copy. It removes `NetPieceObjects` only when no
rules remain, reuses unchanged pieces, and copies only parent sections whose references change.
Signal-rule entries retain their original requirements, transforms, ordering and object references.
Other piece components, including
lanes and native pricing, remain intact. Geometry, surface and object assets retain original
references. Source graphs are not mutated; shared references within one conversion are reused.
Malformed null/cyclic external subsection graphs fail conversion with cleanup of owned allocations.

The common conversion output and all ten network adapters now return `PrefabBase[]` for owned
nodes, rather than a sections-only array. This shared API adjustment ensures changed pieces are
not omitted from the eventual save plan. No per-style measured mapping values were changed.
The final generator assembly must pass the entire array into the existing owned-prefab writer
and preview lifecycle. Those generator callers are still absent; the API is not evidence of a
successful end-to-end saved bridge. The existing tower writer accepts arbitrary owned Prefabs.

The selected-road object extraction occurs before suppression, so its rules remain available for
moving object placement and bone binding. `ResolvePlacements` excludes retained signal rules from
the bake; `TryResolve` rejects an attempt to bake one as an internal routing error. Native network
creation must resolve those rules and assign their instance masks, preserving direct ownership for
traffic-light updates. `TrafficSignObject` alone does not select retention: lane and path marking
placeholders also carry it. This pass does not suppress lane-generated markings;
that requires the separate persistent moving-marking path. No engine code or shader was replaced.

Compilation and deployment are verified; game-cycle and cold-start acceptance remain pending.
No existing bridge output reaches this new conversion path yet, so no existing bridge was removed.

## Exact source rule indices (2026-10-16)

Each extracted `MovableRoadObjects.Rule` now carries `SourceRuleIndex` in addition to
`SourcePiece` and `SourceOccurrence`. The index identifies the original initialized
`NetPieceObject` entry, not its surviving position in the composition's object array.
Native `NetInitializeSystem` fills that buffer in `NetPieceObjects.m_PieceObjects`
order. A repeated placeholder reference therefore remains distinguishable by its
actual producer rule.

The private provenance replay presents one original rule at a time to the installed
`NetCompositionSystem.InitializeCompositionJob.AddCompositionObjects`. It keeps the
original object entity, piece offsets and flags, and accumulated object output. For
inverted pieces it visits source rules in reverse, matching the native inner loop;
the native method still applies all position, rotation and requirement handling.
The temporary piece proxy changes only the entity from which that method reads the
one-rule buffer. No original piece or object prefab is changed.

After each invocation the existing private longitudinal tag records the source
occurrence and rule index. Native grouping never reads that tagged longitudinal field
on existing entries. The replay restores the field and compares all output fields
and order against the original whole-composition result before returning provenance.
Both extraction entry points retain the indices, and selected node placements carry
them through their existing nullable `SourceRule` values. Utility/lane-only candidates
still have no composition-rule origin; no identity is invented for them.

Measured first-input rules explain why this distinction matters:

| Piece | Rule indices | Native objects and eligibility |
| --- | --- | --- |
| Car Drive Piece 3 | 0, 1, 2 | RoadArrow, LaneMarkings and PathMarkings placeholders; all require Edge |
| Sidewalk Piece 3.5 - NoTram | 0 | Traffic Sign Placeholder; excludes LevelCrossing |
| Sidewalk Piece 3.5 - NoTram | 1 | BasculeBridgeBarrier01 Placeholder; requires LevelCrossing |
| Sidewalk Piece 3.5 - NoTram | 2 | Street Light Single Placeholder; excludes Tunnel and LevelCrossing |
| Sidewalk Piece 3.5 - NoTram | 3 | The same street-light placeholder; requires Tunnel and either transition flag |
| Sidewalk Piece 3.5 - NoTram | 4 | Tunnel Light Placeholder; requires Tunnel, excludes both transition flags |
| Sidewalk Piece 3.5 - NoTram | 5 | Sidewalk Prop Placeholder; excludes Tunnel, Elevated, Raised and Lowered |

Do not infer that every object in the mixed sidewalk piece belongs to its raised
pavement or that every listed rule is active in the sampled elevated node. The native
requirements and subsequent signal/object selection remain authoritative. This
increment supplies exact producer evidence; the final per-placement motion partition
and generator caller remain unfinished.

Release build and deployment succeeded. Backup:
`artifacts/backups/movable-object-rule-indices-20261016-110633`; 18 original files and
12 installed payload files were hash-verified. Existing routed bridge output is
unchanged and no existing bridge was removed. Native in-game replay and attachment
motion have not yet been exercised; compilation is not that acceptance.


## Measured attachment partitions and native utility ownership (2026-10-16)

`MovableNativeRoadRoles.TryGetAttachment` now maps the recorded original piece name,
source rule index, original placeholder name and geometry CID to a surface role.
These are exact measured producer identities, not substring matching or classification
of a selected concrete lamp/sign by its display name. The current entries cover
Car Drive Piece 3 rules 0–2 as carriageway and Sidewalk Piece 3.5 - NoTram rules
0, 2 and 3 as sidewalk. Native eligibility and selection still precede this lookup.
Unknown producers do not receive a default bone or disappear silently.

Offline projection onto the original Sidewalk3.5 triangles confirms the sign anchor
at X = -0.75, Z = 0.5 on face 111, and the lamp anchor at X = -0.75, Z = 0 on faces
31/111. These are raised sidewalk faces. The tunnel-lamp anchor at X = 1.5, Z = 0
instead reaches carriageway faces 95/127. Its rule requires Tunnel and is not added
to the elevated attachment table. Ground-only prop rule 5 is likewise not admitted
by this increment. Shared geometry does not make every object's rule equivalent.

`Draw01RoadPartitions.ForObjects` converts the selected node objects' retained sources
into measured carriageway/sidewalk partition trees. `Draw01ObjectAssembly.Create`
now accepts the live `Draw01ClosedRoad` context, resolves its objects once, derives
those partitions, and binds the resulting placements to the final mechanism root
using that same context's road-to-object transform. This closes the callable
assembly stage; the final generator route remains absent. Other native/custom
producers still need measurements rather than an inferred fallback.

A separate ownership correction concerns utility-lane node objects. Native
`SecondaryObjectSystem` collects `UtilityLaneData.m_NodeObjectPrefab` from sublanes
and creates the eligible utility nodes after traffic-sign processing. That producer
is independent of `NetPieceObjects` and hidden road mesh layers. The moving section
conversion retains utility lanes and their node-object references (its separate
track-end suppression does not change utility-node producers). Consequently,
`MovableNodeObjects` now preserves utility arbitration and selection/group consumption
but does not append a second utility node to the moving mechanism. The saved network
remains responsible for creating it. This change neither removes utility connectivity
nor assigns a guessed moving-surface role to a pipe/cable junction.

Evidence: local native `Game.Objects/SecondaryObjectSystem.cs` lines 1575–1707,
1910–2030 and 2363–2391; `NetInitializeSystem.cs` object-rule initialization; the
retained `all-movable-audit.json` producer records; and decoded geometry
`49670fd1cafa11667cbd47d1c380fd04`. Native utility nodes still require in-game and
cold-load validation along with the complete bridge.

All three increments built and deployed successfully, each verifying 18 original
backup files and 12 installed payload files:

- `artifacts/backups/movable-measured-object-roles-20261016-110905`
- `artifacts/backups/draw01-object-partitions-20261016-111003`
- `artifacts/backups/movable-native-utility-ownership-20261016-111246`

Style integration was committed on `bridge/draw-01` as `71ebc3a` and cherry-picked
to `dev` as `f0e177f`. Shared work remains on `dev`. No existing routed style calls
these unfinished movable generation stages, so no existing bridge was removed.
This is not a completed movable generator, visual acceptance or proof of shipping
compatibility.
