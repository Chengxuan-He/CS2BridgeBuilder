# Movable bridge fixed render graphs

## Shared construction path

`MovableRoadAssets.Level` now has a structure-only constructor accepting a source render,
prepared bounds and one measured structural plan per source mesh. It represents fixed
supports without inventing selected-road triangles, surfaces, a partition or procedural bones.
The existing road constructor explicitly marks its level as containing a generated road.

Both forms use the same graph builder. The builder checks source ownership, complete native
LOD reachability, surface-slot alignment, measured source streams and bounds. It carries
native components, preserves structural surface references, replaces derived LOD references,
computes render counts/area and writes owned geometry through the existing asset database.
Preview uses the existing disposable geometry capture. Failure uses the existing per-call
rollback list; it cannot leave a successful subset of LODs published.

Structure-only levels skip road appearance baking, partitioning and lane conversion. They
still require actual source meshes and aligned native surfaces. An empty loaded structure is
an external-input failure, not an empty successful render. Existing geometry destinations
are rejected before creating either kind of model. No original material or texture is cloned.

## Skeleton handling

A structure without ProceduralAnimationProperties uses zero bones and receives no skeleton
component. If the original render has a skeleton, it is validated and retained. Existing
BlendIndices are validated against the actual bone count by MovableStructureModel; accepting
unskinned structures does not permit invalid skinned vertices or create a synthetic bone.
Generated road levels still require their valid native skeleton and measured partition.

Static authored bounds are not a procedural motion envelope. The original Draw01 column's
declared near/middle maximum Z is 92.40020751953125, while its decoded geometry reaches
92.400390625. Its far declared maximum Z is 92.41539764404297, while decoded geometry
reaches 92.416015625. Strict containment against native authored bounds would reject those
unchanged source meshes. The builder therefore applies structural motion-envelope containment
only to renders with a procedural skeleton. Static supports retain authored stacking bounds
with the measured X adjustment; the structural model independently computes accurate submesh
bounds from emitted geometry. This introduces no epsilon, coordinate normalization or guessed
bound correction. Position finiteness, source-stream identity and unchanged Y/Z checks remain.

## Persistence ownership and call site

`TowerFactory.CreateMovableRenderGraph` replaces the former unused `CreateMovableRoad` name.
It adds every prepared render to the same `_created` list as other bridge-owned prefabs. The
normal generation writer remains responsible for prefab persistence and external dependency
snapshots. Generated geometry uses the existing owner-prefixed names and private identities.
The caller must provide source identity, complete structural plans and correct bounds; this
shared builder cannot infer authored part membership or identify a bridge style by geometry.

Repository search found no external caller of the old method, so no active fixed bridge route
changes. The current shared `MovableRenderAssembly` calls `CreateMovableRenderGraph`; the earlier
uncalled-stage statement no longer describes current routing. Construction capability and
active callers do not establish all-style material or game acceptance.

## Verification and remaining work

Release compilation passed. Compatibility review traced road-level conversion, structure-only
conversion, raw-channel export, surface ordering, LOD remapping, preview capture, owned-node
collection, geometry save and failure cleanup. No visual-generation unit tests were executed.
Runtime creation, saved-asset reload and human near/far inspection remain unverified.

The current generators must be validated against the complete structural, supported-type road
composition, marking, attachment, network and persistence requirements. The tests recorded here
do not establish current native traffic/shipping/cold-start acceptance.
