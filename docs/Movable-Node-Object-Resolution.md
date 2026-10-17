# Movable node object resolution

## Implemented path

MovableNodeObjects.Resolve connects node candidate placement, native streetlight arbitration,
traffic-sign needs, concrete prefab selection and utility-lane node arbitration. It follows the
installed game 1.6.2f1 SecondaryObjectSystem.CreateSecondaryNodeObjects ordering:

1. Traverse native EdgeIterator contributions, including middle connections.
2. For non-middle contributions, read the actual node composition, edge/node geometry and network
   data; compute the native approach direction and contribution masks.
3. Resolve immediate visual objects, deduplicate utility object anchors, aggregate streetlights and
   collect sign candidates in their original order.
4. Collect connected-edge utility endpoints and target lane records.
5. Select streetlights by the native spacing/priority algorithm.
6. Apply the native isolated-node utility fallback only when there were no non-middle contributions,
   then accumulate node utility lanes and calculate sign needs from the node lanes.
7. Resolve requested signs in collected order, followed by eligible utility node objects.

Signal-capable rules remain in the candidate set because they affect closest-sign assignment. Their
concrete selection consumes the native randomization groups, but they do not become mechanism
children: their preserved network producers create and update the live signal instances.

The result contains closed-frame ObjectSubObjectInfo placements plus native HasStreetLights and
AlwaysLit results. It is not a saved bridge. Final assembly must preserve relevant lighting state
and apply the measured mechanism binding to the moving visual placements.

## Utility nodes

MovableNodeUtilities implements the three native aggregation blocks for connected-edge endpoints,
isolated nodes and node lanes. It preserves utility-type intersection, endpoint matching, native
object-bounds priority, first/equal-priority behavior, vertical/unsure flags, lane count and elevation.
The final native predicate omits an ordinary two-way joint and emits eligible other or vertical
connections. All lookup entities belong to the prepared lane world.

Native utility emission supplies a world transform separately from its local elevation/cache
transform. Candidate now has an explicit-object-transform constructor for this path. Ordinary
candidate construction still uses LocalToWorld. Composing the utility elevation into its supplied
world transform would incorrectly move the object a second time.

## Caller requirements and persistence

The caller supplies a complete native lane world, including connected edges, owner/endpoint geometry,
node and edge compositions, primary lanes, signal groups and utility prefab data. Two resolvers map
prepared composition entities to original object rules and prepared utility prefab entities to
permitted original ObjectPrefabs. No prepared-world entity may become a persisted prefab reference.
Supply all pre-suppression rules, including signals. Do not change world structure while the helpers
hold component lookups or native iterators.

The current straight-edge preview staging does not supply this full context. No final movable
generator calls this node resolver yet. Prepared-context construction, final mechanism assembly,
lane/marking conversion, lifecycle state and human acceptance remain unfinished. The copied node
arbitration does not itself prove that utility wires or every custom attachment can operate through
the full bridge motion. Existing-instance replacement/lifecycle handling is not implemented here.
Native secondary-object emission also writes instance Elevation/Lowered/OnGround flags. These are
distinct from the returned transform and must be reconciled with native child instantiation before
claiming equivalent persisted attachments; ObjectSubObjectInfo alone does not store those flags.
The subsequent [attachment state audit](Movable-Attachment-State.md) also establishes that the
native road streetlight update does not recurse into mechanism children, while removing network
lamp producers clears IsLit. The returned lighting booleans do not resolve that persistence gap.

## Verification and deployment

The edge, isolated-node and node-lane utility blocks were compared to the local decompiled native
source after explicit collection and parameter renames; all three remain present unchanged in their
decision arithmetic. The orchestration was reviewed against the same native method, including its
unusual right-side end tangent, middle connections, selection order and final utility predicate.
This is source comparison, not a visual-generation unit test or in-game acceptance.

Release builds completed without warnings. Deployment backed up and verified 18 installed original
files and verified all 12 copied payloads. Final backup:
`artifacts/backups/movable-node-object-resolution-20261016-063341`.
The preceding utility-only increment is at
`artifacts/backups/movable-node-utilities-20261016-063247`.
No final routed generation path calls these additions, so existing bridge assets were not changed
or removed. No engine code, shader, original native prefab or material was modified.
