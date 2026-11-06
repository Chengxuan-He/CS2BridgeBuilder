# Movable overhead wire sampling repair

## Evidence and cause (2026-10-22)

The private network samples each moving deck as a long straight node. Native train
track AuxiliaryLanes (source components 36599, 178843/178844 as applicable) select
Edge, Intersection, DeadEnd or LevelCrossing, excluding an ordinary straight node.
LaneSystem.CreateNodeLane tests that selector on both adjacent node compositions.
Consequently the node-only visual extraction can contain rails but no train wires.
The extracted component 36599 records contact height 6 m, messenger height 7.2 m
and longitudinal spacing 50 m. No replacement dimensions are invented.

Train Contact Cable Lane (121044, UtilityLane 145707) is LowVoltageLine, while
Train Catenary Cable Lane and tram wires use Catenary. Testing Catenary alone
would still omit the actual train contact wire. Tram component 161851 already
allows straight nodes: contact height 5 m, longitudinal offset 2 m, spacing 40 m.
Its lateral support-wire rule remains native; this repair does not invent poles
or transverse supports. Lift03 and other movable styles share this same sampler.

## Implementation and compatibility

During private prefab import, only non-underground utility auxiliaries of track
lanes are considered. For Catenary or LowVoltageLine, a purely topological Any
selector containing Edge also accepts Node. All/None restrictions, mixed upgrade
selectors, position, spacing, flags and native prefab identity remain intact.
Already unrestricted tram wires remain unchanged. Ordinary utility networks,
third-rail tracks without overhead auxiliaries and non-movable generators are
not changed. The live source buffers are never modified.

The native lane jobs still generate curves and static sag. Existing conversion
retains native cable material, all LODs and original road-bone partitions. Preview
and persisted generation share the repair. Saved bridges are not rewritten;
regenerate affected bridges to obtain the added wire geometry. Persistence and
self-check formats are unchanged. There are no changed user-facing strings;
existing localization remains intact.

## Verification

MovableLaneDeformationCheck covers train contact and messenger topology, native
tram eligibility and height, exclusions, mixed upgrade isolation, rail/sleeper
height preservation and 612 cable deformation cases. Build and core checks are
required before deployment. In-game wire visibility, motion seams, near/far views
and cold-load behavior remain human acceptance checks, not established by these
offline checks.

## Native batch-budget regression (2026-10-22)

The next game run reached cable geometry assembly but rejected Double Train Track
on Draw01: 17 material batches across its three LODs exceeded the native limit of
16. Retain that limit. When necessary, separate the already-built lane models and
surfaces into a second complete native LOD graph, each group limited to 16. Append
that graph as an ObjectMeshInfo on the same mechanism, after authored meshes,
with matching transform, state requirements, StackProperties and procedural bones.
This follows the accepted skinned-paint object-mesh arrangement, not child-object
history-driven animation. No triangles, materials or LODs are removed or merged.
Factory ownership, preview capture, asset persistence and failure rollback include
both graphs. Existing saved bridges and non-movable generators remain unchanged.

MovableMaterialSlotsCheck covers the 17-batch regression, exact 16/32 boundaries,
individual-group overflow and missing lane LOD rejection. Release build and core
checks pass; in-game regeneration and animation acceptance remain pending. No UI
strings were introduced; technical render identifiers remain unlocalized.
