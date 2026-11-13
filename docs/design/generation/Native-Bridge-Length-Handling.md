# Native bridge longitudinal handling reference

## 1. Scope and reproducible evidence

This reference supports the [non-Network virtual prototype contract](../../agent-contract/NON_NETWORK_BRIDGES_CONTRACT.md).
It records installed native metadata and its native consumers, not visual or traffic acceptance and
not a change to any generator. The audit was performed on 2026-11-13 with game **1.6.4f1
(1097.158a) [6360.18886] Windows Steamworks**, identified by the local `SceneFlow.log`.
Unity resource version is **2022.3.71f1**. Third-party Bridge Expansion Pack and GO assets are not
native sources and are excluded. Other game releases and uninstalled future content are not covered.

The scan uses the actual `Game.Prefabs.Bridge` component type, not English bridge names or the
Bridge Builder catalogue. It reads all three installed Unity `.assets` containers and all official
`Content/**/*.cok` directory inventories. `resources.assets` contains all **75** Bridge components;
`globalgamemanagers.assets` and `sharedassets0.assets` contain none. Header scans have no errors.
The official packages also contain **2,044** Odin Prefabs: their exact serialized root types contain
no Network root, and their bytes contain no Bridge component or segment-length fields. The audit
also checks `.cpatch` entries for those fields. Thus DLC bridge roots are included through the
resource file, rather than assumed absent because their DLC packages have no Prefab entries.

Of the 75 roots, **44 are bridge network variants: 40 primary roots and 4 auxiliary decks**.
The remaining **15 piers, 15 quays and 1 dam** use the same Bridge component and are retained in
the machine-readable inventory, separately classified. The bridge roots comprise **20 Game,
23 BridgesAndPorts and 1 SanFranciscoSet** content prerequisites. These are serialized source
requirements, not proof of the current account's entitlement or menu visibility. All referenced
structural geometry in the audited graph is locally present: **1,018 of 1,018 distinct nonzero
Geometry CIDs** resolve to installed official package entries, with file sizes and CRCs recorded.
SanFranciscoSet geometry is present despite having no separately named content directory.

Counts are network roots, **not distinct visual styles**. The four ordinary suspension lane widths,
the two ordinary truss-arch road variants and the pedestrian/bicycle variants remain separate source
rows. Identical-style variants must still be grouped for Bridge Builder style selection; different
road widths or transport categories do not create new styles. Auxiliary decks are not separate
bridge styles. Different native names such as ordinary Truss Arch and TrussArchBridge02 must not be
merged by a filename resemblance.

The physical metadata graph contains **4,251 records**, including 445 NetPieces, 657 additional
RenderPrefabs, 192 LOD components, 116 piece-object components and all 10 MoveableBridge components.
The extractor follows source section/subsection alternatives, overhead/underground sections,
object meshes, child objects, LODs and recorded placeholder replacement candidates. It records
movement/matching, divider, stacking, pillar and animation metadata. UI/unlock/theme back-links,
texture pixels and vertex-buffer decoding are outside this longitudinal metadata audit. Reached
alternatives are not all simultaneously selected; conditions remain in the raw graph. Every root's
piece/object/LOD references and geometry CIDs are retained. Meshless carriers remain in the graph.

Evidence and reproduction (run scripts from the repository root with their explicit local Python
dependencies available; no Unity or game startup is involved):

- [Extractor](../../../artifacts/native-bridge-lengths/extract.py) and [summarizer](../../../artifacts/native-bridge-lengths/summarize.py).
- [Coverage and source hashes](../../../artifacts/native-bridge-lengths/coverage.json): installed files, all official package inventories, version, current PrefabsMap and geometry coverage.
- [75-root CSV](../../../artifacts/native-bridge-lengths/inventory.csv) and [JSON](../../../artifacts/native-bridge-lengths/inventory.json): exact GUID/path ID, content prerequisite, length fields, fixed sequences/states, support placements and per-root graph membership.
- [Source graph](../../../artifacts/native-bridge-lengths/graph.json): source-byte SHA-256, current UnityGUID, serialized fields, LOD links, geometry/material CIDs and package provenance.
- [Extra official Prefab inventory](../../../artifacts/native-bridge-lengths/package-prefabs.json) and [count summary](../../../artifacts/native-bridge-lengths/summary.json).

The resource-file SHA-256 is `3840f65ca2b0bfae1bcb4e38369e24851dca670b656f3a7bcf6c6c33369e10e6`; the installed `Game.dll` SHA-256 is `f076df4b70aa0f21a80345e03a052ca2273bc6754f41dab7e63d837837a43745`.
The GUID map was read anew from the same resource file; no historical map substitutes for current
identities. Geometry payload existence/CRC and metadata are verified; this is not a vertex-shape,
material-rendering or active-content registration test.

## 2. Complete bridge-root inventory

Among the 44 bridge roots, **25 use ordinary edge composition**, **9 use fixed sequences**, and
**10 use movable crossing objects with approaches**. These are primary layout categories, not
mutually exclusive mesh techniques: every category can additionally use node pieces, object
placement, LODs and ordinary network normalization. All nine bridge fixed sequences have positive
lengths and count `(1,1)` per entry. Their initialized element ranges are therefore `0.6L..1.4L`;
none is an equal-min/max locked element. All ten movable roots have empty `m_FixedSegments`.

The table shows native `m_SegmentLength`, not guaranteed realized length. Overhead values are
`NetPiecePrefab.m_Length`, not road-span measurements. The JSON retains exact section conditions,
all root/base pieces, widths, node offsets and full support records. External-connection helper
objects are omitted from the compact placement column only, and retained in the JSON. `none`
means no overhead contribution or relevant root placement, not no structure. Auxiliaries inherit
their relationship from the parent's recorded `AuxiliaryNets`, not a guessed second road.

| Native root (exact name) | Content / role | Segment (m) | Length handling | Overhead piece reference lengths (m) | Root object placements |
| --- | --- | ---: | --- | --- | --- |
| `ExtradosedBridge01 Train Track` | Game / auxiliary | 128 | ordinary composition | none | none |
| `ExtradosedBridge01` | Game / primary | 128 | ordinary composition | 128 | middle |
| `TrussArchBridge01` | Game / primary | 256 | sequence 128 + 128 | 128 | middle |
| `Arc Bike Bridge` | Game / primary | 32 | ordinary composition | 64 | node |
| `Cable Stayed Bike Bridge` | Game / primary | 64 | ordinary composition | 64 | middle |
| `Covered Bike Bridge` | Game / primary | 32 | ordinary composition | 64 | node |
| `DrawBridge01` | BridgesAndPorts / primary | 144 | mechanism + approaches | none | non-crossing ends/node, non-crossing middle, crossing node |
| `DrawBridge02` | BridgesAndPorts / primary | 144 | mechanism + approaches | none | non-crossing ends/node, non-crossing middle, crossing node |
| `DrawBridge03` | BridgesAndPorts / primary | 152 | mechanism + approaches | none | non-crossing ends/node, non-crossing middle, crossing node |
| `ExtradosedBridge02 Above Road` | BridgesAndPorts / auxiliary | 256 | ordinary composition | none | none |
| `ExtradosedBridge02` | BridgesAndPorts / primary | 256 | ordinary composition | 256 | middle |
| `LiftBridge01` | BridgesAndPorts / primary | 168 | mechanism + approaches | 64 | non-crossing ends/node, non-crossing middle, crossing node |
| `LiftBridge02` | BridgesAndPorts / primary | 168 | mechanism + approaches | none | non-crossing ends/node, non-crossing middle, crossing node |
| `LiftBridge03 Train Track` | BridgesAndPorts / auxiliary | 200 | ordinary composition | none | none |
| `LiftBridge03` | BridgesAndPorts / primary | 200 | mechanism + approaches | 64 | non-crossing ends/node, non-crossing middle, crossing node |
| `SuspensionBridge01` | BridgesAndPorts / primary | 0 | sequence 88 + 76 + 76 + 88 | 88, 76 | NodeAfterFixedSegment[0], EdgeStartOrNodeFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `SuspensionBridge02 Above Road` | BridgesAndPorts / auxiliary | 80 | ordinary composition | none | none |
| `SuspensionBridge02` | BridgesAndPorts / primary | 0 | sequence 80 + 80 + 80 + 80 | 80 | NodeAfterFixedSegment[0], EdgeStartOrNodeFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `TrussArchBridge02` | BridgesAndPorts / primary | 256 | sequence 128 + 128 | 128 | middle |
| `ExtradosedBridge03` | BridgesAndPorts / primary | 320 | ordinary composition | 224 | middle |
| `ExtradosedBridge04` | BridgesAndPorts / primary | 320 | ordinary composition | 224 | middle |
| `LiftBridge04` | BridgesAndPorts / primary | 168 | mechanism + approaches | none | NotWaterwayCrossingNode, non-crossing middle, crossing node |
| `LiftBridge05` | BridgesAndPorts / primary | 168 | mechanism + approaches | none | NotWaterwayCrossingNode, non-crossing middle, crossing node |
| `PedestrianBridgeCoveredWood01` | BridgesAndPorts / primary | 64 | ordinary composition | 64 | node |
| `PedestrianDrawBridge01` | BridgesAndPorts / primary | 144 | mechanism + approaches | none | NotWaterwayCrossingNode, non-crossing middle, crossing node |
| `PedestrianDrawBridge02` | BridgesAndPorts / primary | 144 | mechanism + approaches | none | NotWaterwayCrossingNode, non-crossing middle, crossing node |
| `SuspensionBridge03` | BridgesAndPorts / primary | 0 | sequence 168 + 320 + 320 + 168 | 168, 320 | NodeAfterFixedSegment[0], EdgeStartOrNodeFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `SuspensionBridge04` | BridgesAndPorts / primary | 0 | sequence 168 + 320 + 320 + 168 | 168, 320 | NodeAfterFixedSegment[0], EdgeStartOrNodeFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `TrussArchBridge03` | BridgesAndPorts / primary | 256 | sequence 128 + 128 | 128 | middle |
| `Arc Pedestrian Bridge` | Game / primary | 32 | ordinary composition | 64 | node |
| `Cable Stayed Pedestrian Bridge` | Game / primary | 64 | ordinary composition | 64 | middle |
| `Covered Pedestrian Bridge` | Game / primary | 32 | ordinary composition | 64 | node |
| `Cable-stayed Bridge - XL Road Divided - 8 Lanes` | Game / primary | 128 | ordinary composition | 128 | middle |
| `Extradosed Bridge - Large Road Divided - 6 Lanes` | Game / primary | 128 | ordinary composition | 128 | middle |
| `Golden Gate Bridge` | SanFranciscoSet / primary | 0 | sequence 350 + 640 + 640 + 350 | 350, 350, 640, 640 | EdgeStartOrNodeFixedSegment[0], NodeAfterFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `Grand Bridge` | Game / primary | 0 | sequence 130 + 170 + 170 + 130 | 130, 130, 168, 168 | EdgeStartOrNodeFixedSegment[0], NodeAfterFixedSegment[0], NodeBeforeFixedSegment[3], EdgeEndOrNodeFixedSegment[3] |
| `Suspension Bridge - Highway Oneway - 2 Lanes` | Game / primary | 256 | ordinary composition | 256 | middle |
| `Suspension Bridge - Highway Oneway - 3 lanes` | Game / primary | 256 | ordinary composition | 256 | middle |
| `Suspension Bridge - Highway Oneway - 4 Lanes` | Game / primary | 256 | ordinary composition | 256 | middle |
| `Suspension Bridge - Highway Oneway - 5 Lanes` | Game / primary | 256 | ordinary composition | 256 | middle |
| `Tied Arch Bridge - 4 lanes` | Game / primary | 128 | ordinary composition | 128 | ends/node |
| `Truss Arch Bridge - Highway Twoway - 2 Lanes` | Game / primary | 128 | ordinary composition | 128 | ends/node |
| `Truss Arch Bridge - Small Road - 2 Lanes` | Game / primary | 128 | ordinary composition | 128 | ends/node |
| `Wooden Covered Bridge - 2 lanes` | Game / primary | 64 | ordinary composition | 66 | ends/node |

## 3. How the native rules change length

### Ordinary composition and the arch distinctions

[NetInitializeSystem](../../../artifacts/native-bridge-lengths/native/NetInitializeSystem.cs)
copies each piece's reference length/node offsets, requirements and flags. For a positive bridge
segment length it normally sets the edge range to `0.6S..1.4S`; `m_AllowMinimalLength` leaves the
existing minimum rather than forcing `0.6S`. A zero segment length retains existing defaults and
does not mean a zero-length bridge. Curvature and hanging remain separate native fields.

[BatchMeshHelpers.GenerateCompositionMesh](../../../artifacts/native-bridge-lengths/native/Game.Rendering.BatchMeshHelpers.cs)
normalizes longitudinal mesh positions using half the selected composition piece's reference
length. Section inversion, flipping, offsets, half-length selection and node composition affect
that mapping. The renderer then uses the actual edge/node curve frame. A piece length is therefore
an authoring denominator, not a rigid instance length. It must not be substituted by the root's
segment length: Arc/Covered pedestrian and bicycle roots use segment 32 with piece length 64;
Wooden Covered uses segment 64 with piece 66; Extradosed03/04 use segment 320 with piece 224.

The native arch references are materially different:

- Ordinary `Truss Arch Bridge - Highway Twoway - 2 Lanes` and `...Small Road - 2 Lanes` use
  128 m edge-only overhead pieces, no fixed sequence, and `EdgeEndsOrNode` pillar placement.
- `Tied Arch Bridge - 4 lanes` likewise uses a 128 m overhead piece with end/node supports.
- Arc pedestrian/bicycle roots use a 64 m reference piece, preferred segment 32 and node pillars.
- `TrussArchBridge01`, `02` and `03` use two 128 m fixed-sequence elements with `Front`/`Back`
  states. Each selects the corresponding overhead piece; the Back contribution flips it. Their
  primary pillars are `EdgeMiddle`, not the ordinary arch's end/node placement. All three retain
  the same ranged-element semantics, despite one being a base-game rail root and two being DLC.

Consequently "copy the native arch behavior" requires identifying which actual arch and complete
piece/support graph is meant. It never means globally scaling piers with the spanning mesh.

### Fixed sequences and native validation

[CourseSplitSystem.TryAddCoursePhase2](../../../artifacts/native-bridge-lengths/native/Game.Tools.CourseSplitSystem.cs)
allocates lengths and counts for the entire sequence. Zero count bounds mean unbounded native
limits `(0,10000)` in that allocation, not automatically zero instances. For positive available
length it distributes excess through ranged elements. For a course shorter than the combined
minimum it also distributes the deficit proportionally to minimum contributions. States and
fixed indices then determine section and object placement.

The 31 other Bridge-component roots expose an important additional pattern: the dam has lengths
`64, 0, 32, 0, 64`, with count `(1,1)` for positive entries and `(1,0)` for the zero-length entries.
In `NetInitializeSystem`, the presence of an entry of length `<=0.1` causes positive entries to
initialize with equal minimum/maximum lengths; the zero entries use the network's default range.
The dam is not a conversion donor merely because it provides this example. The 15 piers and 15
quays remain separate domains; their exact zero/default or 10000 segment settings are in the CSV.
No bridge-root fixed sequence in this installed version uses this mixed zero-length arrangement.

[ValidationHelpers](../../../artifacts/native-bridge-lengths/native/Game.Net.ValidationHelpers.cs)
uses the initialized range, but relaxes an equal-min/max fixed element's minimum to `0.6L`.
HalfLength composition further multiplies the minimum by `0.1`; the maximum is multiplied by `1.1`.
It compares the actual start/end edge center curves' XZ lengths and emits native ShortDistance or
LongDistance errors. This is not the same quantity as the untouched source mesh extent or a clear
opening measurement. Thus fixed segments, even with flexible approaches, do not guarantee an
immutable total/span, and a custom epsilon range would be a policy-changing workaround.

### Nodes, rigid supports and object repetition

[GeometrySystem](../../../artifacts/native-bridge-lengths/native/Game.Net.GeometrySystem.cs)
uses the maximum of edge and node composition offsets, together with node shape, intersections,
minimum offsets and slope/width terms. For example, `LimitHeightDelta` includes
`max(edge.NodeOffset,node.NodeOffset) + abs(slope)*edge.Width*0.25`. Do not treat NodeOffset as a
constant length subtracted from both ends in all configurations. Deforming structure and rigid
endpoint objects must use the same realized native geometry.

`NetDividerPiece.m_PreserveShape` contributes PreserveShape and DisableTiling. In node mesh
normalization the native PreserveShape branch uses composition width to retain that local shape;
ordinary node/half-length paths have different clipping and normalization. Median/divider pieces
in selected roads can carry this flag. Its presence somewhere in a bridge's graph does not mean
the entire bridge span is shape-preserving. `MovePieceVertices` and `MatchPieceVertices` are also
part of the native junction representation and must not be omitted to simplify conversion.

Root `NetSubObjects` explicitly distinguishes nodes, edge ends/middles, course endpoints,
fixed-index boundaries and waterway/non-waterway placement. Each source row retains position,
rotation, spacing, anchors and flags; placeholders retain their actual candidate and stacking
rules. Native vertical ground-reaching stacks are not evidence that a pier may be stretched along
the bridge. Distinct endpoint halves are distinct parts even if their contacts coincide.

[NetCompositionSystem.AddCompositionObjects](../../../artifacts/native-bridge-lengths/native/Game.Prefabs.NetCompositionSystem.cs)
selects and composes `NetPieceObjects`, respecting section requirements, offsets, inversion and
cross-sectional deduplication. Spacing X is a deduplication distance; Z is the longitudinal interval,
not a command to repeat in a 2D grid. [SecondaryObjectSystem.CreateSecondaryEdgeObjects](../../../artifacts/native-bridge-lengths/native/Game.Objects.SecondaryObjectSystem.cs)
uses the actual composed curve length, phase range, minimum length, probability and orientation.
Without the avoidance-spacing branch, ordinary count is `floor(L/s+0.5)` with its small-count
fallback and step `L/count`; EvenSpacing adjusts endpoint contributions, subtracts one count and
uses `L/(count+1)` with a half-step phase. Endpoint contributions are bounded by `s/3`, not by a
fixed source NodeOffset. Avoid-spacing, flags and node object arbitration remain native behavior.
For example, an ordinary 64 m curve with interval 14 and phase zero gives five candidate stations
at 0, 12.8, 25.6, 38.4 and 51.2 m before offsets, not a 14 m constant grid. This numerical example
illustrates the consumer; it is not evidence of a native 64/14 bridge source.

### All ten movable mechanisms

DrawBridge01/02/03, LiftBridge01/02/03/04/05 and PedestrianDrawBridge01/02 each place a real
mechanism at `WaterwayCrossingNode`, with separately selected fixed approaches and non-crossing
supports. The inventory preserves the reachable MoveableBridge component, procedural bones,
LOD graph and approach-piece metadata. Their empty fixed sequences disprove the assumption that
all moving bridges require FixedSegments. They require their native motion and traffic behavior
under the [movable contract](../../agent-contract/MOVABLE_BRIDGES_CONTRACT.md); the static virtual
prototype workflow does not authorize inventing a length-scaled mechanism or a new animation path.

## 4. Applying this evidence and verification limits

Use the normative [virtual prototype selection and acceptance rules](../../agent-contract/NON_NETWORK_BRIDGES_CONTRACT.md#3-describe-the-virtual-bridge-prototype).
Record the source's total extent, clear/bearing span and reference piece length separately. Select
native longitudinal roles offline and store only ordinary immutable recipe data. Do not create or
save an intermediate Network asset or add a runtime virtual-prototype pass. The selected final
road/rail/pedestrian network supplies composition and traffic semantics without a road-kind gate.

Native length deformation and Bridge Builder's transverse authored-part map are separate stages.
This reference does not change X-width rules, tower depth/height constraints, rigid pier sections,
all-LOD face coverage, source-material projection, per-side sidewalks, fees, unlocks or persistence.
Tests must distinguish source-width identity from longitudinal native deformation and inspect
both sides of each span/rigid-support interface on reference, short and long courses. Actual
node/edge geometry, repeated spans, fixed-state boundaries and applicable slopes/intersections
belong in the check; a source arithmetic ratio alone is not end-to-end evidence.

This documentation-only audit performed no runtime edit, build, deployment, game startup or bridge
creation. No new runtime strings were added and all twelve existing language tables are unchanged.
Source metadata and algorithms establish the listed handling, not live near/far appearance, traffic,
motion or cold-load success. Account entitlement, uninstalled content and future releases remain
outside the installed-file coverage claim. Native decompiles are retained with this evidence so the
method and field semantics can be rechecked without relying on names or this prose alone.
