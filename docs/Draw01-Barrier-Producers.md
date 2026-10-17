# Draw01 barrier producer adaptation

## Native evidence

Game 1.6.2f1 `Sidewalk Piece 3.5 - NoTram` (resources path ID 120789)
uses `BasculeBridgeBarrier01 Placeholder` (78485) when `LevelCrossing` is set.
The placeholder has both `TrafficSignObject` and `TrafficLightObject`.
`Highway Shoulder 2` (120781) instead uses `Traffic Sign Placeholder` (116136)
in its crossing rule. Retaining that rule unchanged on a generated bridge permits
ordinary traffic lights where a boom barrier is required. Sidewalk classification
must not be changed to solve this signal-producer difference.

## Implementation

`Draw01BarrierRules` adapts only the converted `Node + FixedNodeSize` section graph.
For ordinary signal placeholder rules that can run at a level crossing, the crossing
branch references the existing native barrier placeholder. A rule that also applies
outside crossings is split into mutually exclusive crossing/non-crossing branches.
Rules excluding crossings, existing native barrier producers, ordinary connection
segments, and other styles retain their existing behavior.

All original placement fields and additional conditions are preserved, including
position, longitudinal placement, rotation, curve rotation, inversion, and spacing.
No sidewalk is added to provide a barrier mount. Left/right occurrence transforms
continue to be applied by the native network system.

Only changed section/piece paths are copied; existing private moving-node copies
are reused. New copies enter the same generated-prefab ownership and persistence
collection. The barrier remains a built-in asset reference, with no copied material
or mesh. Native `SecondaryObjectSystem` creates the node-owned signal and its group
masks; no mechanism child, runtime controller, or custom signal state is introduced.

## Verification boundary

Native serialized rules and native signal ownership code were inspected. Build and
deployment verify compilation and installation only. Human acceptance must check
no-sidewalk, single-sidewalk and unequal-sidewalk roads, both bridge ends and both
traffic directions, and confirm barrier motion during a ship crossing after reload.
