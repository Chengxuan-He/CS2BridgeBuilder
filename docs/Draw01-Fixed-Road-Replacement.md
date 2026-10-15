# Draw01 fixed road replacement data

## Native membership

`Draw01FixedRoadData.RemovalFaces` records the road faces on both fixed end structures,
all on bone 0 in main mesh 0. It complements `Draw01MovingRoadData.RemovalFaces`.

| LOD | Fixed-road faces | Moving-road faces | Combined road-removal faces |
| --- | --- | --- | --- |
| 0 | 124 | 1888 | 2012 |
| 1 | 102 | 1472 | 1574 |
| 2 | 36 | 72 | 108 |

The fixed set includes the carriageway and sidewalk tops, inner curbs, angled sidewalk
returns and discrete approach slab closures/undersides where the native mesh provides
them. It replaces the earlier top-only and fixed-inner-curb subsets; do not use those
partial lists as a complete fixed-road removal plan.

## Retained structures

All recorded control-building faces are disjoint from these removals. The positive-X
control buildings, their platforms, stairs and supports remain. Outer structural walls,
railings and coarse pier envelopes remain. In particular, far faces 105/117/268/276
extend down into the structural pier and are not deleted merely because they share an
edge with a road top. Far road curbs and discrete end returns are removed separately.

The actual native geometry was inspected in end and plan projections at all three LODs.
Offline height/lateral bounds were discovery tools only. Runtime consumes explicit face
ordinals bound to the source hashes in the emitted class; it must not rediscover membership.

## Integration status

This is removal metadata, not a finished Draw01 generator. The complete structure plan
must validate original positions/indices, apply the fixed and moving masks, preserve
retained raw channels/material references, widen authored structures and insert the
selected-road geometry with its measured partitions. Final caller and native in-game
acceptance remain outstanding. Existing generated bridges are unaffected by uncalled data.
