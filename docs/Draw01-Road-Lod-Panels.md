# Draw01 road LOD panels

## Native comparison

The inspected native far mesh contains 656 triangles in total. Its moving top surfaces use long
panels: for example faces 18, 20 and 22 cover the interval Z = 0.02999878 through 68.16016;
faces 16, 17, 19 and 21 continue to Z = 77.94141. Their UV islands cover long strips rather
than repeating the near road's four-metre atlas tiling. Other sides and leaves have separate
measured islands. This is evidence for continuous panel topology, not permission to substitute
the prototype's road layout for the selected road.

The generated far geometry retained every near appearance-tile and paint-clipping triangle.
The inspected saved Large Road Divided sample has 9,580 road triangles and 3,300 white paint
triangles, in addition to its structure. Copying these unchanged into each LOD defeats the native
reduction in complexity.

## Implemented reduction

For the middle and far Draw01 render levels, coalesce adjoining converted road triangles after
motion binding and paint inlay. Merge only opposite shared edges with identical vertex attributes,
bone and material. Require one plane, affine-compatible UVs and constant normal/tangent/colour
attributes across the resulting convex polygon. Remove redundant collinear boundary vertices and
triangulate that polygon. Never move a retained position, cross a material/UV/paint seam or motion
boundary, merge concave regions, or alter authored structural classification.

This preserves selected-road geometry and appearances instead of deleting every Nth triangle or
scaling an entire native road. Near detail is unchanged. The shared option defaults off; only
Draw01 opts in. Preview and persistence share the same compacted models and real index counts.
No new material, texture, Prefab, independent marking object or runtime rendering patch is added.

## Real-file inspection

The inspection ran the compaction code over the actual saved mesh positions, faces, bone indices,
UVs and raw lighting attribute equality tokens from bridge
`b944aeab6-493a-4f21-a77a-3c6f38c23994`. It used no synthetic mesh fixtures.

| Saved far batch | Before triangles | After triangles | Inspection elapsed |
| --- | ---: | ---: | ---: |
| Road | 9,580 | 9,187 | 48 ms |
| White paint | 3,300 | 1,381 | 16 ms |
| Combined | 12,880 | 10,568 | 64 ms |

Combined reduction is 2,312 triangles (17.95%). Summed area before/after is
4,802.424137310812 / 4,802.424137501735 for road and
191.41246604672756 / 191.412462856802 for paint, with differences at floating-point summation scale.
Packed saved attributes can differ from pre-export attributes, so these are saved-sample inspection
results, not measured new-generation timings or promised runtime counts.

Release compilation passed; 12 deployed payload files matched the build. No active Draw01 owners
existed at this deployment. New human-generated files and near/far visual acceptance remain pending.

## Remaining threshold mismatch

This reduction does not establish original LOD transition thresholds. Most road atlas repetition
seams cannot be removed without changing the texture mapping. The native far long-strip projection
must be adapted separately to arbitrary selected-road carriageways, sidewalks and paint without
sampling prototype markings or gutters. Existing near projection is deliberately retained here
until that far appearance mapping is verified. Native thresholds depend on actual index counts,
bounds and area; never copy prototype index counts into metadata for a different mesh.
