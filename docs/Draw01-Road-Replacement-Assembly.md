# Draw01 road replacement assembly

## Sidewalk front caps

Draw01SidewalkFrontFragmentData records far faces 105/106/268/269 and 433/434/592/593.
The retained sidewalk support ends at Y=9.800048828125. The high-detail outer lip at
X=+/-6.5 reaches Y=10, continuing toward its outer corner. The far representation retains
its own outer corner (Y=10.047119140625) and uses the measured inner lip point to separate
that structural lip from the old sidewalk top. This preserves the native coarse exterior
instead of imposing one flat cut across the lip. The recorded profile is constructed offline;
no height, lateral-band or slope classifier runs in game.

Sixteen edge vertices and eight interior vertices create twenty retained triangles. Both ends
and both sides retain native winding. All contributing packed normal/tangent/bone values are
identical within their source triangles; new UVs interpolate the original Float16 values.
Original vertices and material slot 0 remain unchanged. Bone remains 0.

## One main-mesh plan

Draw01RoadReplacement.CreateMainMeshPlan combines the two whole-road removal tables and all
six fragment tables in one MovableStructureModel.Plan for main mesh 0. It takes source
positions/indices and separately computed widened positions. The caller must resolve the
recorded native source identity and supply the complete measured structural widening plan;
matching array counts alone does not establish source identity or validate widening.

All edge tables are appended first, then rear-slope and sidewalk interior vertices. Absolute
fragment indices are calculated from these offsets, so multiple fragment tables cannot collide
with one another's appended vertices. The common builder checks the original position/index
streams, original-triangle membership, channel compatibility and emitted bounds before output.

| LOD | Source triangles | Whole removals | Replaced source triangles | Retained fragment triangles | Output triangles | Output vertices |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 15940 | 2012 | 0 | 0 | 13928 | 29840 |
| 1 | 9535 | 1574 | 0 | 0 | 7961 | 18772 |
| 2 | 656 | 108 | 36 | 62 | 574 | 1194 |

Far output appends 64 edge and 12 interior vertices. Original unused vertices remain in the
buffer by design, preserving native channel numbering; only emitted indices determine bounds.
Near/middle surfaces are separate and use the complete road-solid removal masks.

## Remaining integration

This assembles the measured road-removal inputs, not the final bridge. Complete structural
widening, selected-road geometry/material/marking construction, motion bounds, object assembly,
owned-resource transfer and final generator routing remain required. Other main-mesh parts,
glass, pillars and footings are not transformed by this main-mesh-0 helper. It has no final
generator caller yet. Native human near/far, motion, traffic, shipping and cold-start acceptance
remain mandatory. No existing bridge is invalidated by an unused construction helper.
