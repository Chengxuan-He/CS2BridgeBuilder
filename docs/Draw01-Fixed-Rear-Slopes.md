# Draw01 fixed rear control-building slopes

## Native retained profile

High-detail rear faces 4897/4898 end at Y=9.199951171875. Face 3757 preserves the slope
from (X=6.2816162109375, Y=9.199951171875) to (X=6.5, Y=10), meeting control-building
support faces 3755/3756. Building wall faces 4641/4642 continue upward at X>=6.5.
The negative end has the corresponding slope at face 11651. Both buildings are on positive X;
transverse mirroring would discard this asymmetric profile.

Draw01FixedRearSlopeData applies that profile to far faces 317/646 in their own native rear
Z planes, preserving the coarse outer corner beside the control building. Four measured edge
vertices and four interior barycentric vertices form eight retained triangles. The native
triangle winding is retained at both ends. The interior turns reproduce their measured X/Y
coordinates using Float32 arithmetic. Original vertices are not lowered or moved in Z.

Native Geometry decoding verifies identical normal, tangent and bone bytes on every contributing
corner. Bone is 0; material slot is 0. Newly constructed UVs interpolate the original Float16
channel. No new material, shader or texture is introduced by these fragments.

## Assembly

Combine Edges with the other edge tables and Interior with the interior tables. Plan appends
all edges before all interior vertices. Pass the absolute starting indices of these two tables
to Faces; do not assume an interior point immediately follows this table's edge points when
other edge tables are present. Near/middle LODs return empty arrays.

Together with Draw01FixedRearFragmentData, these records cover all twelve rear structural cap
triangles at both ends: 24 edge vertices, 4 interior vertices and 22 retained triangles.
Road-only rear top strips remain in Draw01FixedRoadData's whole-face removals.

Front caps, inner returns, the complete structural widening/road partition and the final
Draw01 generator caller remain unfinished. These are construction inputs, not complete bridge
support. Actual source-fragment inspection does not replace human in-game acceptance.
