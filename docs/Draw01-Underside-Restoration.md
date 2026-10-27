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

## Bottom atlas-coordinate correction (2026-10-23)

The user's close-up disproves the earlier claim that the underside appearance was fixed.
The far supplement repairs missing coverage, but does not repair the original UV error.
Both detailed levels have a solid underside on the negative-Z leaf mapped into the native
asphalt/yellow-line atlas island. The opposite leaf maps into the unpainted underside island.
The defect was preserved by copying all structural UVs unchanged. It is not explained solely
by different LODs or missing triangles.

The correction replaces UV0 on 176 measured bottom vertices in LOD0 and 158 in LOD1.
Offline correspondence rotates the source X/Z by 180 degrees onto the opposite leaf's
actual bottom triangles and interpolates their native UV0. This also handles the differing
middle-LOD triangulation. Runtime data is a checksum-validated embedded typed array;
no prototype lookup, geometric classifier or texture sampling occurs at runtime.
Only these bottom UVs change. Positions, indices, normals, bones, material references and
other channels stay intact. The far supplement applies the same LOD1 UV correction before
compaction. No replacement texture, material or additional object is introduced.

Validation uses the actual embedded arrays exported by `tools/Draw01UndersideCheck` and
`check_texture.py` against the native raw atlas (raw image rows already correspond to V;
they must not be flipped again). A triangular barycentric grid sampled 13,464 yellow pixels
at LOD0 and 7,584 at LOD1 before correction, and zero after correction. The geometry check
covers 9,600 downward-facing points across both leaves and three sidewalk layouts, with
zero uncovered points. These checks establish the measured mapping and coverage, not
in-game visual acceptance. Inspect newly generated previews/bridges in open and closed
poses at all LODs. Existing saved bridge meshes are not rewritten by installing the DLL.

Release compilation and material-slot regression checks passed. No user-facing strings
were added or changed; existing localization behavior is preserved. This scoped UV repair
supersedes the original-UV preservation requirement above for the measured bottom vertices
only. The historical far-LOD restriction below/above is superseded by the permitted middle-LOD
supplement documented in Draw01-Far-Underside.md.

## UV seam regression (2026-10-23, follow-up)

The preceding per-vertex opposite-leaf lookup was incorrect at UV seams. A position on a
shared geometric boundary can belong to several independent atlas islands. Choosing the
first containing triangle independently for each vertex caused one slab triangle to interpolate
between unrelated atlas regions. The yellow-pixel check did not detect this failure.

The new offline extractor (`tools/Draw01UndersideCheck/extract_island_uv.py`) groups bottom
triangles by shared vertex indices and matches complete panels. Each panel uses one measured
rectangular counterpart and its local bounds; all its vertices are evaluated within that same
UV island. The middle LOD has 45 source islands versus 44 opposite-leaf islands; incomplete
triangular donor islands are excluded, rather than extrapolating UVs outside them. Near LOD
has 44 islands on each leaf. The embedded resource still corrects 176/158 vertices, and the
far supplement consumes the corrected middle-LOD data. This replaces the earlier per-vertex
lookup, without changing positions, bones, topology or material identities.

This repairs cross-island interpolation. It does not establish native texel density after
arbitrary structural widening: lateral geometric scaling still scales the native panel texture.
A separate repeat-unit implementation would be required to guarantee width-independent bottom
texture density. Visual acceptance and the separately reported missing upper asphalt surface
remain open; neither is claimed resolved by this UV resource update.
