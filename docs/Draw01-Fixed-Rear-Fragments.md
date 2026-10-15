# Draw01 fixed rear cap fragments

## Native scope

Draw01FixedRearFragmentData records partial replacements for far main-mesh faces
316/319/320/321/322 and 644/645/648/649/650. These are the central and negative-X rear
caps at both ends. High-detail retained rear faces establish Y=9.199951171875 as their
upper structural boundary; the far envelope merges road-facing material above it.

Twenty explicitly measured edge vertices and fourteen retained triangles replace the ten
source triangles. Original vertices remain unchanged. Native decoding confirmed identical
normal, tangent and bone bytes on every clipped edge; UVs are interpolated in Float16.
The source Geometry hash is embedded in the generated class. Bone remains 0, material slot 0.

## Composition with other records

Append Vertices after any preceding edge-vertex table and pass its absolute first output
index to Faces. Combine these fragments with the moving end fragments, other measured
fixed fragments and the whole-face road masks in one complete MovableStructureModel.Plan.
The builder preserves original material ordering and calculates bounds from emitted indices.
Near and middle LODs return empty arrays because their road surfaces are already separate.

Positive-X rear caps 317/646 are deliberately outside this table. Their retained control-
building-side slope is supplied by [Draw01FixedRearSlopeData](Draw01-Fixed-Rear-Slopes.md),
including its measured interior construction points. Combine both tables for the rear caps.
Front caps, inner returns and complete structural widening still need their measured plans.
This table is a partial construction input, never a standalone completed bridge or permission
to publish an incomplete structure. No final generator currently calls it.
