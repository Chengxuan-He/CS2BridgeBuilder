# Draw02 motion-domain review

## Source and scope

The native `DrawBridge02 Mesh.Geometry` from game 1.6.2f1 has SHA-256
`1f0ff1a2c0a8318e6537bd347ae064d1bb55bfa48f3cf3c2375601adcd05dd18`.
Its mesh 0 contains 44,370 vertices and 24,664 triangles. Source face ordinals below
are zero-based. Coordinates are restored to binary32 from the native extraction,
not decimal display values used as approximate thresholds.

This review supplies selected-road motion partitions only. It does not classify
carried structural parts, select faces for deletion, or establish material ownership.
The complete Draw02 generator is not yet routed.

## Recorded domains

| Surface boundary | Positive Z coordinate, metres | Source face evidence |
| --- | ---: | --- |
| Fixed carriageway begins | 78.7421875 | 10733 |
| Fixed sidewalk begins | 78.7421875 | 10819 |
| Moving carriageway ends | 78.69921875 | 2544 |
| Moving sidewalk ends | 78.69921875 | 2526 |
| Moving carriageway centre end | 0.030005931854248047 | 4434; negative counterpart 15392 |
| Moving sidewalk centre envelope | 0.030022144317626953 | Near-mesh top edges at Y=10 |

The sidewalk centre edge has three measured Z stations:
0.030005931854248047, 0.0300140380859375 and 0.030022144317626953.
The partition uses the largest as a planar gap envelope. This removes at most
0.000016212463378906 m of additional selected-road extent; it does not move any
prototype structural vertex. Negative-Z boundaries mirror these values.

The fixed/moving opening is 0.04296875 m at each hinge. These boundaries are
independent of the mechanism's 90 m node offset and the sampling approach's 144 m
length. Neither of those lengths may replace a physical motion seam.

## Skeleton correspondence

| Region | Bone array index | Native connection ID |
| --- | ---: | ---: |
| Fixed approaches | 0 | Fixed frame |
| Positive-Z leaf | 1 | 101 |
| Negative-Z leaf | 2 | 100 |

Draw01 uses the opposite leaf array ordering. Copying its partition tree would
bind Draw02's converted road and paint to the wrong leaf. The new Draw02 tree
preserves the recorded order without changing the native skeleton or bind poses.
All generated LODs must use these physical domains. Native LOD2's raised visual
shell is not an alternative traffic datum or bone mapping.

## Remaining integration

The near roadway atlas includes grating panels and narrow solid strips, rather
than Draw01's asphalt layout. At Z=40..44 m, faces 3680/3682 address grating UVs
U=0.5234375..0.5791015625 and V=0.27392578125..0.475830078125;
faces 3650/3684 address the separate solid-strip island. Sidewalk and fixed-cap
islands must also remain distinct. Do not copy Draw01's projection constants.

Original support inspection confirms the pillar and pillar-base have hollow
rectangular walls. Their far meshes weld and simplify these walls and returns.
The whole support must not be widened as one bounding-box scale. Per-part
correspondence and complete road/structure separation remain necessary before
publishing the full generator. No new bridge creation or in-game visual acceptance
is claimed by this motion-data increment.
