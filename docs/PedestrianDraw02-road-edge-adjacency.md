# PedestrianDraw02 road edge adjacency

## Scope and method

This inspection follows the complete 276-face road-top membership at each of the two main LODs. It reads original binary32 positions and native indices, then compares exact XYZ edge coordinates. The 116 selected road boundary edges are checked against all non-road faces in mesh 0. This is offline adjacency evidence, not a runtime semantic classifier or a removal plan.

## Exact edge neighbours

| LOD | Boundary edges | One exact non-road neighbour | No exact neighbour | More than one exact neighbour |
| --- | --- | --- | --- | --- |
| 0 | 116 | 96 | 20 | 0 |
| 1 | 116 | 96 | 20 | 0 |

Each LOD has 96 directly adjacent non-road triangles. Of these, 36 extend above the nominal Y=10 road datum to Y=10.10009765625; 60 extend downwards. These are geometric observations, not sufficient proof that every face belongs to a curb or removable road closure. Of the downward neighbours, 44 lie in the reviewed road atlas rectangle and 16 use other atlas locations. Material or atlas membership alone must not decide deletion.

## Different edge subdivision

The twenty unmatched edges all run along Z at fixed X/Y. An exact collinear interval check against all non-road triangle edges completely covers sixteen of them. These sixteen are at X=+4 or X=-4 on the fixed platforms. Their lack of a one-to-one edge neighbour is caused by different subdivision; they must not be treated as open holes.

The four remaining edges lie beside the moving-leaf tips at X=+3.68646240234375 or X=-3.68646240234375. Exact collinear non-road edges cover only part of them. The uncovered intervals are listed below. This does not prove a visible hole: faces touching through an interior, overlapping geometry, bevels and other mesh entries have not been ruled out. No filler geometry is justified by this check alone.

| LOD | Road triangle | X | Uncovered Z interval |
| --- | --- | --- | --- |
| 0 | 8548 | 3.68646240234375 | 0.06464576721191406 to 0.17243194580078125 |
| 0 | 11377 | -3.68646240234375 | 0.06465721130371094 to 0.17244338989257812 |
| 0 | 23580 | -3.68646240234375 | -0.17242813110351562 to -0.06464385986328125 |
| 0 | 26409 | 3.68646240234375 | -0.1724395751953125 to -0.06465530395507812 |
| 1 | 6120 | 3.68646240234375 | 0.029992103576660156 to 0.17243194580078125 |
| 1 | 7207 | -3.68646240234375 | 0.030002117156982422 to 0.17244338989257812 |
| 1 | 15080 | -3.68646240234375 | -0.17242813110351562 to -0.029988765716552734 |
| 1 | 16204 | 3.68646240234375 | -0.1724395751953125 to -0.029998779296875 |

## Consequences for replacement

Top-road removal must preserve or explicitly replace the adjoining non-road boundary. A generic operation that deletes every same-atlas face would remove authored end geometry without proving its role, while a rule that fills every unmatched edge would create geometry over sixteen native subdivision boundaries. Keep authored side structure unchanged until its part and closure ownership are measured.

Source hashes and top membership are recorded in [complete road membership](PedestrianDraw02-complete-road-membership.md). Full per-edge neighbours and exact interval matches remain in the intermediate extraction. No runtime source, deployed payload, saved bridge or generator route changed in this pass.

## Moving-tip face-interior intersection

The remaining intervals were checked against native non-road triangles in the exact constant-X side plane. Intersections with Y=10 use rational arithmetic on original binary32 coordinates, without tolerances or snapping. Much of each interval lies inside an authored vertical triangle, rather than on one of its edges. Thus the previous edge-only uncovered interval is not a side-wall removal boundary.

| LOD | Road triangle | Uncovered Z start | Uncovered Z end | Uncovered length (m) |
| --- | --- | --- | --- | --- |
| 0 | 8548 | 0.06464576721191406 | 0.06466102250979541 | 1.5255297881353912e-05 |
| 0 | 11377 | 0.06465721130371094 | 0.06467055925295948 | 1.3347949248541412e-05 |
| 0 | 23580 | -0.06465720781252979 | -0.06464385986328125 | 1.3347949248541412e-05 |
| 0 | 26409 | -0.06466865190432666 | -0.06465530395507812 | 1.3347949248541412e-05 |
| 1 | 6120 | 0.029992103576660156 | 0.06466102250979541 | 0.03466891893313526 |
| 1 | 7207 | 0.030002117156982422 | 0.06467055925295948 | 0.03466844209597706 |
| 1 | 15080 | -0.06465720781252979 | -0.029988765716552734 | 0.03466844209597706 |
| 1 | 16204 | -0.06466865190432666 | -0.029998779296875 | 0.03466987260745167 |

Near LOD residuals are approximately 13¨C15 micrometres. LOD1 residuals are approximately 0.03467 m. These are native mesh-0 contact differences; they are not generated-road defects and do not authorize welding, moving source vertices or adding filler. Other mesh entries were not evaluated by this plane-contact check. Full rational endpoints and intersected triangle IDs are retained in the measurement.

The current `MovableRoadGeometry.Partition` API supports arbitrary seam transforms, so a nonrectangular measured domain can be expressed through a binary plane partition. Its presence does not establish the style-specific partition, widening correspondence or final generator integration.
