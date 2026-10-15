# Draw01 fixed inner approach-wall fragments

## Measured profile

High/middle inner approach walls end at Y=9.800048828125, along X=+/-3.4000244140625.
Far faces 119/282/447/606 instead rise to the simplified sidewalk top at their outer end.
Draw01FixedInnerFragmentData replaces each with one retained triangle, appending a vertex
on its recorded original vertical edge at the high-detail wall-top elevation. The other two
original corners remain unchanged. Both bridge ends and both transverse sides are recorded.

All four edge fractions are 0.9680927991867065. The resulting vertices are at
X=+/-3.4000244140625, Y=9.800048828125, Z=+/-78. Original Y/Z coordinates are not rewritten.
Native normal/tangent/bone bytes agree at every contributing edge; UVs interpolate in their
original Float16 channel. Native winding, bone 0 and material slot 0 are retained.

The adjoining lower faces 118/281/446/605 already end at the retained elevation and remain
unchanged. Separate curb triangles above this level belong to Draw01FixedRoadData's whole-face
removal set. These side walls are higher than the center front support at Y=9.5; do not reuse
its ceiling for these different authored surfaces. Outer walls/railings remain separate.

## Assembly and status

Append Vertices to the plan's edge array; give Faces its absolute first vertex index. Combine
with the other measured fragment tables and whole-face masks in the complete structure plan.
Near/middle LODs return empty arrays because their separate road geometry uses whole-face masks.

These data do not finish the sidewalk front cap, complete structural widening or selected-road
partition. There is still no final Draw01 generator caller. No bridge output is changed solely
by introducing this unused data table. Human native-cycle and near/far acceptance remains open.
