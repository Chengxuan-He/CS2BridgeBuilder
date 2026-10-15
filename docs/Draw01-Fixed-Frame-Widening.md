# Draw01 fixed-frame widening

## Native membership and application

`Draw01FixedWideningData.Apply` records the fixed main-mesh vertices of both ends and the
separate control-building glass. It must run once on copied original positions. Do not also
translate the control-building face set: that set shares 28 original vertices with the remaining
fixed frame at LOD0 and 24 at LOD1. Separate passes would translate those vertices twice.

| LOD | Fixed main-mesh vertices | Fixed source triangles | Separate glass vertices |
| --- | ---: | ---: | ---: |
| 0 | 6560 | 3660 | 128 |
| 1 | 3516 | 1863 | 128 |
| 2 | 454 | 256 | None |

The main-mesh records include original fixed-road corners needed to construct mixed-face
replacement vertices. Whole road faces are subsequently removed by `Draw01RoadReplacement`;
their inclusion here does not preserve the prototype road surface. Construct all appended
fragment vertices after original-vertex widening, not before it.

## Reviewed mapping

The fixed frame is not one stretchable connected component. Its large welded group contains
the rear approach wall, side supports and positive-X control building. The separate front
support also has a lower transverse lip. Native high-detail center stations remain at X=0;
the transverse endpoints move with their adjoining side structures. The inspected front
stations include +/-3.20001220703125 and the lower lip's +/-3.24139404296875 m. The rear
central wall meets the rigid side portions at +/-4 m. Road-adjacent cap corners follow the
base coordinate mapping, including the +/-3.4000244140625 m front shoulders. These are
distinct authored parts, not a single span taken from the whole mesh's outer bounds.

In these recorded vertices the resulting displacement coefficients are -1, 0 and +1.
The implementation stores exact vertex membership, rather than inferring a transform from
position or connectivity at runtime. The control buildings and all their glass translate
by the positive half-width increment; their dimensions remain unchanged. The simplified far
frame retains outer/center stations rather than the high-detail wall subdivisions. It receives
the corresponding side displacement and unchanged center, not a new classification from its
merged triangles. No Y/Z, skinning, UV, material or index data changes in this operation.

Native OBJ identities are recorded in the generated source. The offline inventory script is
`work/bridge-research/draw01_fixed_widening_inventory.py` in the research workspace. Its JSON
records every fixed vertex, the remaining/crossing faces and shared building vertices.
Native X/Y and X/Z projections were inspected at all LODs, followed by the actual fixed mesh
at original width and +8 m full width. This is offline geometry inspection, not runtime or
in-game acceptance. The inspection image retains original road faces to expose their shared
corners; the final assembly must remove them and use the selected road.

## Integration boundary

`Draw01RoadReplacement.CreateMainMeshPlan` applies both moving and fixed mappings to a copy
of the source positions before constructing fragments. Do not widen that result again.
Do not use the fixed mapping on moving vertices, pillars or footings. A complete generator still needs
source binding, the other structural render prefabs, selected-road conversion, attachment
placement, ownership transfer and the final route. This helper alone publishes no bridge.
The moving-frame central-member narrowing limit remains applicable to the combined structure.
Near/far, movement, traffic, shipping and cold-start acceptance remain outstanding.
