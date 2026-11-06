# Draw01 moving road replacement data

## Scope

`Draw01MovingRoadData` records the complete moving carriageway/sidewalk slab face sets
for native main mesh 0 at all three LODs. Both longitudinal leaves are included.
This is input to structural road replacement, not a complete generator or admission result.
Fixed approaches, widening, skeleton binding and final assembly remain separate work.

## Reviewed membership

| LOD | Source triangles | Removed road triangles | Treatment |
| --- | --- | --- | --- |
| 0 | 15940 | 1888 | Six independent carriageway/sidewalk slabs, including raised profiles and closures |
| 1 | 9535 | 1472 | Corresponding six slabs after native simplification |
| 2 | 656 | 72 | Flattened road tops and closures; shared underbody retained |

Near and middle views were inspected with the complete slab face sets removed from the
actual native geometry. Longitudinal members, transverse members, railing posts and hinges
remain. Far geometry merges these structures: its 44 lower-envelope triangles remain as
the structural underbody. Removing the entire 116-face height-band candidate would also
remove this retained representation. The 84 reviewed lateral-envelope faces are disjoint
from the 72-face far road-removal set.

The emitted source contains explicit face ordinals and source OBJ hashes. Runtime must not
repeat the offline component or height-band selection. Source triangle counts identify the
expected streams; the complete structure plan must still validate source positions and indices.
Preserve original channels and surface bindings on retained structure.

## Integration and acceptance

This data has no final generator caller yet. It replaces the moving-road top-only selection
when assembling the future complete Draw01 structure plan; do not union old top lists blindly
with other removals, and do not use it for fixed end platforms. Existing bridges are unaffected.
Compilation establishes source compatibility only. Generated selected-road insertion, all-LOD
seams, movement and human in-game near/far acceptance remain unverified.
