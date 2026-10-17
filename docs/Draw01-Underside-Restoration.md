# Draw01 moving-span underside restoration

## Diagnosis and native evidence

The native main mesh has downward-facing road slab faces and separate downward-facing
cutout-grating faces. Removing the complete road slabs while replacing only the selected
road's visible surface removed those lower faces as well. The retained central stringer
is not a substitute for the road slab and cannot close the resulting lateral gaps.

Native game 1.6.2f1 mesh 0 was inspected at all three LODs:

| LOD | Restored solid underside triangles | Restored sidewalk underside triangles | Treatment |
| --- | --- | --- | --- |
| 0 | 176 | 160 | Restore the original lower faces of both leaves |
| 1 | 152 | 160 | Restore corresponding original lower faces of both leaves |
| 2 | 0 | 0 | Preserve the already-retained 44-face merged structural underbody and its existing widening |

LOD0 source OBJ SHA-256: `97215ED0B33145FB53737999961C3DA0E6859E433A89566BB1F7C2C04EE7F9E1`.
LOD1 source identity is recorded beside its emitted membership in `Draw01UndersideData.g.cs`.
The reviewed near/middle bottom vertices are not shared with retained structural faces.
Restoring and moving these vertices therefore does not distort the adjacent steel frame.
The far mesh combines road and structure; replacing it with a higher-detail slab is not authorized.

## Generation rule

Resolve lateral spans from the evaluated selected-road geometry and its root-section provenance.
Only `Sidewalk` series sections contribute sidewalk spans. Determine the left and right
sidewalk spans independently. The solid underside covers the non-sidewalk interval between
them; if a side has no sidewalk, the solid extends to that road edge and that grating is omitted.
Curbs and shoulders must not create a grating sidewalk.

Fit the measured original underside vertices laterally to those spans. Keep the original
triangle winding, UVs, material reference, bone weights, Y and Z coordinates. The native
opaque underside and cutout-grating atlas regions remain distinct. No new material,
texture bake, independent object, depth correction or double-sided shader is introduced.
Original upper road faces remain removed. Restore the measured lower faces and the carriageway
slab side/end closures; do not restore duplicate top surfaces.
The existing selected-road surface, fixed platform adaptation and native barrier rules remain.

## Verification and limits

The native face sets, normals, atlas coordinates and all-LOD cross-sections were inspected.
Membership checks confirmed that restored vertices do not belong to retained frame triangles.
Preview and saved generation share the same structural assembly and persistence path.
Compilation and installation checks do not establish visual acceptance. Human verification
must cover underside views in closed/open poses, no sidewalks, one sidewalk, unequal widths,
and near/far transitions. No new in-game bridge was created by the agent.

## Carriageway thickness correction (2026-10-17)

The previous lower-face-only selection left the carriageway slab open at its edges.
The native carriageway spans Y = 9.5 to 9.800049 metres, a thickness of approximately
0.300049 metres. Inspection of the saved `b05ed8ace-a267-4b65-a192-54975a866a9d`
mechanism confirmed that all 176 vertical closure triangles were absent at each of LOD0
and LOD1. The data now restores 184 additional boundary triangles per level: 176 vertical
walls plus 8 sloping closure triangles. Total restored solid faces are 360 at LOD0 and
336 at LOD1, including the previously restored bottoms.

The closure vertices use the same lateral interval as their slab bottom. Their original
Y/Z, UVs, winding, material and bone data remain unchanged. Exact source membership is
emitted offline; the runtime does not discover faces by normals or height thresholds.
Existing sidewalk-bottom selection is unchanged. The far LOD retains its native merged
envelope and previously retained end-cap fragments; this correction does not substitute
near geometry for it or establish acceptance of the separately reported far-view appearance.

The restored vertices have no membership in retained steel-frame triangles. The change
adds indices to existing structural meshes, without creating Prefabs, materials or textures.
Preview and saved bridges use the same plan; registration and loading formats are unchanged.
Rebuild and deployment are loadability checks. A newly generated bridge still requires
human inspection of exposed slab edges and all LOD transitions.
