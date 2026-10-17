# Movable node sign and signal needs

## Implemented calculation

`MovableNodeSignNeeds` is a disabled-by-default system in the prepared temporary lane world.
It reads native node/edge lanes, curves, PathNodes, lane flags and signal groups. It neither
creates a city network nor patches the game's systems.

The calculation follows game 1.6.2f1 `SecondaryObjectSystem.CreateSecondaryNodeObjects`:

1. Reset node-local candidates, target-lane records, direction masks and read-only lookups.
2. For each contribution, record road direction sectors and initial sign exclusion masks.
   Keep the native condition that MoveableBridge signs are excluded unless the edge intersects
   waterways, the edge has FixedNodeSize and the node does not.
3. Add sign candidates with their actual object transforms and forward directions. Retain their
   contribution boundaries so endpoint-specific bridge-sign requests affect the proper candidates.
4. Collect connected-edge target lanes, preserving native endpoint/owner checks and slave-lane
   flag combination. Duplicate target keys retain the native diagnostic behavior.
5. Evaluate node vehicle and pedestrian lanes against candidate position/direction. Preserve
   turning, yield/stop, one-way, motorway, speed, parking and bridge-sign masks, vehicle-side
   lane counts, vehicle signal masks and left/right pedestrian crossing masks.
6. Apply the initial removal masks after lane processing. Expose the native final demand predicate;
   candidate presence alone must not instantiate an unrequested sign or signal.

Master vehicle lanes, secondary lanes and utility lanes follow their native exclusion paths.
The caller supplies actual connected-edge order and the native approach direction; empty synthetic
lane buffers cannot establish equivalent node behavior. Lists/dictionaries replace temporary native
containers, but target lookup and candidate ordering are retained. The native node-lane arithmetic
sequence was compared after explicit container/identifier substitutions and whitespace removal.

## Integration status and limits

The final generator still does not call this calculation. The prepared connected-node world,
full object dispatch, utility objects and final native attachment persistence remain unfinished.
The node-sign system complements the already implemented node placement and streetlight stages;
it does not make the existing edge-only ResolvePlacements method valid for node owners.

Native secondary-object creation also writes vehicle/crossing masks to placed TrafficLight ECS
components. Computing those masks is not proof that saving an ObjectSubObjectInfo alone preserves
them after cold start or correctly animates a signal attached to a moving object. That persistence
and native initialization path must be verified before signal support is claimed. No mask is
silently dropped and no custom runtime controller has been introduced in this increment.

Direct execution of the entire native secondary-object job remains dependent on a complete private
node/prefab world, command buffers and output initialization; it is not currently a shortcut around
those missing inputs. This increment implements the inspected read-only needs calculation only.

## Build and deployment

Final Release compilation passed without warnings. The previously reported unassigned
TrafficSignNeeds fields now have their node-specific producers; no warning suppression was added.
A missing System import in the initial build was corrected before successful deployment.
The final backup is `artifacts/backups/movable-node-sign-filter-20261016-062013`:
18 original installed files and 12 installed payload files were verified. No existing routed
bridge output changed and no existing bridges were removed. Source comparison and compilation
are not in-game traffic, animation, shipping or cold-start acceptance.
