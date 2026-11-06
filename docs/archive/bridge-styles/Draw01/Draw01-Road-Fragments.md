# Draw01 mixed road and structure fragments

## Source and construction

`Draw01RoadFragmentData` records four partial replacements in far main mesh 0: faces
24, 193, 351 and 516. The original triangles span the structural end and the old road end.
Retain the lower triangle, ending at the measured underbody boundary Y=9.419189453125.
The new boundary vertex lies at fraction 0.840514600276947 along each recorded low-to-high
edge. The original vertices remain unchanged; the new vertex is appended.

Near and middle LODs have separate road slabs, removed by Draw01MovingRoadData. They do
not require these coarse-only fragments. This follows the same high-detail road/structure
distinction at every LOD rather than treating far welding as a new structural identity.

## Attribute preservation

Native channel inspection confirmed identical normal, tangent and bone bytes on each
clipped edge. Copy those bytes to the constructed vertex. Interpolate only the new UV
coordinates in the original Float16 layout. Face 193 has a different packed normal at
its untouched side corner; retain it exactly. Do not flatten or re-encode original data.

## Assembly

Append MovingVertices to the construction plan's added vertices. Supply their first
absolute output vertex index to MovingFaces. Pass both arrays to MovableStructureModel.Plan
alongside original source positions/indices, widened positions and whole-face removal masks.
Plan marks the mixed source faces replaced and emits fragments in their original material slot.
Its complete Positions array includes new boundary vertices for the caller's motion-bound check.

The common builder preserves source vertex count plus appended vertices, original channel
formats, untouched raw values, source material slot order and the fragment's recorded winding.
It does not infer a cut plane or classify source geometry at runtime. New vertices whose
non-UV endpoint data differ require an explicit supported conversion instead of silent blending.

## Status

The fragment capability and Draw01 data are implemented. The complete Draw01 structural
widening/road assembly and final generator caller remain unfinished. Fixed-approach mixed
surfaces still require their own reviewed fragment data. No final in-game acceptance is claimed.
