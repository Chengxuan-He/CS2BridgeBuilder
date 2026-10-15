# Draw01 control-building part mapping

Reviewed against the actual native main mesh projections at all three LODs. The positive-X
control buildings at both fixed ends include their platforms, rails, stairs and support feet.
They do not reach X = 0 and must move rigidly by the positive half-width displacement.
No building dimension, window spacing or column thickness may scale with the road.

| LOD | Main-mesh faces | Independent glass faces |
| --- | ---: | ---: |
| 0 | 2176 | 64 |
| 1 | 1006 | 64 |
| 2 | 62 | No separate glass mesh |

The generated source records exact native face ordinals and OBJ hashes. It contains no coordinate
threshold or runtime topology search. Main-mesh source counts are 15940, 9535 and 656 triangles.
The LOD2 buildings consist of the two 31-face native connectivity sets; the near/middle mappings
retain the same logical building despite their additional independent trim geometry.

These are retained structural faces, not road-removal faces. Use this mapping within a complete
per-mesh plan; it is not permission to transform only the building or leave the other bridge
parts unresolved. The selected road, adjoining central base, moving leaves, other railings,
lighting placements and all remaining structure still need their own complete plans. The glass
must receive the same displacement as the opaque building. No generator calls this data yet.

Source extraction and visual review are offline evidence. Generated near/far geometry and human
in-game acceptance remain required. This data-only addition changes no existing generated output.
