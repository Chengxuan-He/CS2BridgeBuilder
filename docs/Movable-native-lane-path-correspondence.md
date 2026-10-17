# Native lane path identity and receiver correspondence

## Verified native behavior

Inspected the local decompiled game sources on 2026-10-16. This record follows the
composition lane-to-piece map added to the private closed-road compiler.

| Path | Native evidence | Mapping consequence |
| --- | --- | --- |
| Ordinary edge lane | LaneSystem.CreateEdgeLane constructs start, middle and end PathNodes from the owner, composition lane index and segment index | The index must be interpreted together with the owner and segment, not as a visual batch index |
| PathNode packing | GetLaneIndex returns the entire low 16 bits; the byte-lane constructor packs the segment into bits 8–15 | A raw GetLaneIndex value is not generally an index into NetCompositionLane |
| Node connection selection | LaneSystem matches a node composition lane by flags, type and native geometry, then overwrites its m_Index with the existing edge lane middle node's low byte | Node endpoint lane IDs do not prove the original node-composition array index; the matching step must be retained or recovered through the native path |
| Secondary lane | SecondaryLaneSystem.CreateSecondaryLane allocates three fresh ushort indices and marks each PathNode secondary | These IDs are not parent composition-lane IDs and must never index LaneOccurrences |
| Secondary parent relationship | The placement overload receives left/right lane entities, but the final creation overload writes PrefabRef, Lane, Curve, Owner, Elevation and optional hanging/temporary state without those parent references | The inspected output path does not retain a direct left/right parent lane link for later lookup |

## Implications for the current implementation

MovableNetworkLanes.LanePieces remains a map from initialized composition-lane indices
to composition pieces. CompileRoad offsets those piece indices into its global occurrence
array. Neither operation makes this array a map from arbitrary PathNode or visual-batch IDs.

Do not mask an arbitrary secondary node ID to eight bits and look it up as a composition
lane. Its numeric value may coincide with an existing index while referring to a different
lane. Likewise, SourceLane preserves original lane-component semantics but is not unique
where several pieces use the same lane prefab.

For projected decals, MovableDecalReceivers already samples the actual split receiver
triangles and retains their bone, occurrence and source-triangle identity. Continue this
receiver-derived path instead of replacing it with endpoint-index inference. This only
establishes attachment provenance; unresolved material and distance-fade fidelity remains.

For non-decal lane geometry, direct composition mapping requires native primary-connection
provenance, and secondary geometry requires its construction/receiver correspondence.
Do not silently assign all secondary geometry to the carriageway, drop it, or move it to
an unrelated bone. This investigation rules out an apparently simple index lookup; it does
not claim that the remaining correspondence algorithm has been implemented.

## Evidence locations

- Game.Pathfind/PathNode.cs: byte/ushort constructors, GetLaneIndex, IsSecondary.
- Game.Net/LaneSystem.cs: CreateEdgeLane around lines 4127–4151; node connection matching
  and index replacement around lines 5270–5343.
- Game.Net/SecondaryLaneSystem.cs: left/right placement overload at line 1026; final
  creation overload at line 1321, fresh IDs at lines 1347–1349 and component writes afterward.

No runtime code, installed files or saved bridges were changed for this investigation.
All ten final movable generator routes and human acceptance remain unfinished.
