# Draw01 section role evidence

Read directly from installed `resources.assets` on 2026-10-16. Path IDs are extraction identifiers only; they must not become runtime asset IDs.

## Source and scope

Root: `DrawBridge01` (path ID 118584). All 21 root entries are included below. This inventories native definitions, not the result of executing composition jobs.

| Index | Section | All | Any | None | Hidden | Half length | Invert | Median |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | DrawBridge01 Side 0.5 | [] | [] | [80] | 0 | 0 | 1 | 0 |
| 1 | DrawBridge01 Side 0.5 | [0, 80] | [] | [] | -1 | 0 | 1 | 0 |
| 2 | DrawBridge01 Side 0.5 | [80] | [] | [0] | 0 | 1 | 1 | 0 |
| 3 | DrawBridge01 Sidewalk 3.5 | [] | [] | [80] | 0 | 0 | 1 | 0 |
| 4 | DrawBridge01 Sidewalk 3.5 | [0, 80] | [] | [] | -1 | 0 | 1 | 0 |
| 5 | DrawBridge01 Sidewalk 3.5 | [80] | [] | [0] | 0 | 1 | 1 | 0 |
| 6 | DrawBridge01 Car Drive Section 4 | [] | [] | [80] | 0 | 0 | 1 | 0 |
| 7 | DrawBridge01 Car Drive Section 4 | [0, 80] | [] | [] | -1 | 0 | 1 | 0 |
| 8 | DrawBridge01 Car Drive Section 4 | [80] | [] | [0] | 0 | 1 | 1 | 0 |
| 9 | DrawBridge01 Median 0 | [] | [] | [80] | 0 | 0 | 0 | 1 |
| 10 | DrawBridge01 Median 0 | [0, 80] | [] | [] | -1 | 0 | 0 | 1 |
| 11 | DrawBridge01 Median 0 | [80] | [] | [0] | 0 | 1 | 0 | 1 |
| 12 | DrawBridge01 Car Drive Section 4 | [] | [] | [80] | 0 | 0 | 0 | 0 |
| 13 | DrawBridge01 Car Drive Section 4 | [0, 80] | [] | [] | -1 | 0 | 0 | 0 |
| 14 | DrawBridge01 Car Drive Section 4 | [80] | [] | [0] | 0 | 1 | 0 | 0 |
| 15 | DrawBridge01 Sidewalk 3.5 | [] | [] | [80] | 0 | 0 | 0 | 0 |
| 16 | DrawBridge01 Sidewalk 3.5 | [0, 80] | [] | [] | -1 | 0 | 0 | 0 |
| 17 | DrawBridge01 Sidewalk 3.5 | [80] | [] | [0] | 0 | 1 | 0 | 0 |
| 18 | DrawBridge01 Side 0.5 | [] | [] | [80] | 0 | 0 | 0 | 0 |
| 19 | DrawBridge01 Side 0.5 | [0, 80] | [] | [] | -1 | 0 | 0 | 0 |
| 20 | DrawBridge01 Side 0.5 | [80] | [] | [0] | 0 | 1 | 0 | 0 |

Requirement values in this direct extraction are native numeric enum values, not inferred enum names.

## Unique root section pieces

| Section | Piece | Layer | Declared width | Vertex count | Index count | Lane definitions |
| --- | --- | --- | --- | --- | --- | --- |
| DrawBridge01 Side 0.5 | DrawBridge01Side50cm Mesh | 3 | 0.5 | 932 | 2112 | None |
| DrawBridge01 Side 0.5 | DrawBridge01SideNode50cm Mesh | 3 | 0.5 | 324 | 912 | None |
| DrawBridge01 Sidewalk 3.5 | Sidewalk Piece 3.5 - NoTram | 0 | 3.5 | 144 | 384 | Boarding Lane 0 (ParkingLane slots={'x': 0.0, 'y': 0.0}); Pedestrian Lane 3 (PedestrianLane) |
| DrawBridge01 Sidewalk 3.5 | DrawBridge01BottomEdge350cm Mesh | 1 | 3.5 | 1232 | 2394 | None |
| DrawBridge01 Sidewalk 3.5 | DrawBridge01BottomEdgeNode350cm Mesh | 1 | 3.5 | 584 | 1380 | None |
| DrawBridge01 Car Drive Section 4 | Car Drive Piece 3 | 0 | 3.0 | 36 | 96 | Car Drive Lane 3 (CarLane) |
| DrawBridge01 Car Drive Section 4 | Car Drive Piece 3 | 0 | 3.0 | 36 | 96 | Car Drive Lane 3 (CarLane) |
| DrawBridge01 Car Drive Section 4 | Car Middle Piece 3 | 0 | 3.0 | 53 | 189 | Car Drive Lane 3 (CarLane) |
| DrawBridge01 Car Drive Section 4 | DrawBridge01Bottom300cm Mesh | 1 | 3.0 | 980 | 1938 | None |
| DrawBridge01 Car Drive Section 4 | DrawBridge01BottomNode300cm Mesh | 1 | 3.0 | 40 | 96 | None |
| DrawBridge01 Median 0 | Median Intersection Piece 0 | 0 | 0.0 | 53 | 189 | None |
| DrawBridge01 Median 0 | DrawBridge01BottomIntersectionMiddle200cm Mesh | 1 | 0.0 | 62 | 189 | None |

## Mapping consequences

`Sidewalk Piece 3.5 - NoTram` contains both `Boarding Lane 0` and `Pedestrian Lane 3`. The former has a `ParkingLane` component with slot size (0, 0). Native `NetInitializeSystem` lines 1582–1624 adds `LaneFlags.Parking`, then `LaneFlags.Virtual` when slot width is zero. Therefore the presence of `ParkingLane` alone is not evidence of physical parking spaces. A blanket rejection of pieces containing parking metadata would reject the native sidewalk itself. This does not establish support for actual parking or stops on a moving leaf.

`Median Intersection Piece 0` has declared width 0 but 53 vertices and 189 indices, with bounds spanning X=-1 to X=1. It has no `NetPieceLanes`. Zero allocation width and absent lanes must not discard its geometry or assign a road role by default. Its conditional selection still belongs to the native composition path.

`Car Drive Piece 3`, `Sidewalk Piece 3.5 - NoTram` and `Median Intersection Piece 0` reference the same surface GUID fields: (-103098415, 1441766470, -2145415030, 700421056). Surface identity cannot distinguish their motion partitions.

`DrawBridge01 Car Drive Section 4` uses the 3-metre `Car Drive Piece 3`; its name is not the allocated width. The sidewalk includes a curb profile spanning road and sidewalk heights. Neither the name nor a single lane type proves that every triangle in a piece belongs to one measured motion region.

Layer 1 bottom pieces and layer 3 side pieces appear in the same sections as road surfaces but represent separate native structural behavior. The mapping must preserve the separation between generated selected-road surfaces and carried/replaced archetype structure. Do not bind every visible piece to the carriageway solely because it lacks pedestrian lanes.

Existing `MovableRoadAssets.Level.RoadPartitions` selects one tree per piece occurrence. Mixed-profile pieces require an explicitly measured construction partition or triangle-level provenance before this array can be populated reliably. Lane visual batch indices are a separate index domain; material equality is not a correspondence.

## Compiled-triangle partition transport

The shared road assembly now also accepts `RoadTrianglePartitions`, keyed by compiled piece
occurrence and its pre-atlas `SourceTriangle` ordinal. The map must cover every evaluated
triangle submitted to binding. Missing entries raise an explicit internal error rather than
falling back to the carriageway tree. A supplied triangle map takes precedence over the
coarser occurrence map. Each value remains a complete measured partition tree, so a mixed
triangle may be split by that tree instead of being assigned to one bone as a whole.

The current ordinal is assigned across all material slots of each compiled occurrence by
MovableStraightRoadEvaluation. It is not an original GeometryAsset triangle index. Native
composition can change tessellation; an authored source-face table must not be indexed by
this ordinal without establishing the native composition correspondence. Occurrence offsets
are retained when combining node contributions. Atlas subdivision, frame transformation and
seam clipping retain both identifiers. This identifies the exact compiled snapshot and avoids
material-slot collisions; it does not itself produce the semantic mapping.

Draw01RenderAssembly forwards the map when adding its structural replacement plans. Lane
visual partitions remain separate and must be supplied explicitly when either road mapping
is present. Native object-decal projection uses the resulting bone-bound road receivers.
The first four measured source geometries now populate this map as described below. Remaining
source geometries, lane visuals and attachments still need their own measured correspondence.

## Measured source-face roles

Direct native geometry decoding establishes the following source roles. These are geometry
identities, not prefab-name heuristics; section wrappers that share an inspected geometry can
use the same record. Runtime consumes source-face ordinals recovered through native composition.

| Native geometry | CID | Source faces | Role |
| --- | --- | --- | --- |
| CarDriveLane3 | `65b7e0cbd9c16168c9f6c306369b88f6` | 0–31 | Carriageway |
| RoadIntersectionMiddle3 | `92eb25687f4a6fcd79cf82a761ee2e23` | 0–62 | Carriageway |
| RoadIntersectionMiddle2 | `dac11ed9cbc727bc8dcb1c6982d81ca4` | 0–62 | Carriageway, including the zero-width median occurrence |
| Sidewalk3.5 | `49670fd1cafa11667cbd47d1c380fd04` | 80–95 and 112–127 | Lower road strip: carriageway |
| Sidewalk3.5 | same as above | 0–79 and 96–111 | Raised pavement, curb top and vertical curb: sidewalk |

All four geometries have one mesh and one submesh. The sidewalk's top/curb/road bands are
distinct native triangles, so this assignment does not require cutting source faces or changing
the source asset. Longitudinal motion seams are still applied after native road evaluation.
The source's tiny height deviations are retained; no runtime geometric threshold is used.

MovableNativeRoadRoles holds the shared measured records. Draw01RoadPartitions.ForRoad converts
the compiled snapshot's source identities into the appropriate recorded longitudinal trees.
Unknown geometry or face indices produce an explicit error with source identity; no carriageway
default, material matching, zero-width omission or lane-type guess is used. The table currently
covers only these four geometries, not all native or custom road pieces. It does not establish
complete-road support, a final generator route or in-game acceptance.

Evidence is retained in `work/bridge-research/inspect_native_road_roles.py`,
`native-road-source-role-candidates.json`, the decoded source files and
`Native-road-source-role-inspection.png` in the chat workspace. Geometry SHA-256 values are
embedded alongside the runtime records. The source-face classification has been inspected
offline; operation of the native provenance pass still needs in-game validation.

## Verification boundary

The fresh extraction establishes the listed prefab fields and native initialization behavior. It does not prove receiver-level triangle masks, a working generator, animation, appearance, persistence or shipping acceptance. No runtime generation code or saved bridges were changed by this audit. All ten final generator routes remain unfinished.

## Source fingerprints

- `resources.assets`: SHA-256 `95242b9385d43bdaae38d797c939e080f8a9768d7f42bd4dc81d5ae74fb106a6`.
- `NetInitializeSystem.cs`: SHA-256 `021cfcd09bf86d48a5f8b294096e56f495a50c83fae3c064f341e60f5f6e490e`.
