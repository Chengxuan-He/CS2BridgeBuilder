# Native marking resource reuse

## Current implementation

All extracted moving-road decal batches now use native object decals with original texture references.
The previous GPU marking-page bake is no longer called. MovableDecalAssets and MovableDecalReceivers,
which created baked marking surfaces/textures, were removed. The road-surface appearance bake remains
separate and unchanged. Unsupported or damaged external material data still fails explicitly;
there is no hidden pixel-baking fallback.

The user explicitly approved curve subdivision approximation and endpoint-fade differences on
2026-10-16. This supersedes the previous affine-only implementation described here. The approval
is recorded in bridge generation contract section 5 and the movable bridge contract; pre-edit baseline is
`dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

## Conversion

1. Read the native lane batch curve, width scale, source texture area and existing receiver bone IDs.
2. Subdivide the curve by de Casteljau subdivision until control-point distance from the chord and
   width variation are at most one unit of the existing road sampling density (normally 1/32 m).
   This is a geometric approximation criterion, not a claim of a screen-space error bound.
3. Use each segment's endpoints for longitudinal texture phase and its midpoint for width. Native
   DefaultDecal handles projection and fading; the original curved endpoint smoothing is not baked.
4. Clip the already bone-bound road triangles against each segment's projection volume. Extrude the
   resulting receiver footprints into projector meshes, grouped by existing bone ID. A projector
   never spans receivers assigned to different bones; no hinge is inferred from geometry.
5. Save the generated projector geometry and object/render Prefabs. Use the full segment's bounds
   and texture area for UV reconstruction, including when a receiver footprint ends at a bone seam.
6. Attach each object through the existing native bone-connection mapping. Preview and saved bridges
   use the same result. Markings are independent of structural LOD replacement.

## Assets and native evidence

The installed material library identifies CurvedDecalDeterioration, CurvedDecal and
CurvedDecalColorOnly as templates 19, 20 and 32. The fresh-road deterioration input is zero, matching
the former evaluated path. Each source surface receives one native DefaultDecal description per
bridge transaction, preserving source texture references, color, channel selection, opacity,
metallic and smoothness parameters. No marking pixel data is read back, resampled, compressed or
saved. No new marking TextureAsset or custom shader is created.

Native LaneProperty carries CurveMatrix/CurveParams/CurveScale. ObjectProperty has no equivalent
curve matrix; BatchDataSystem.UpdateLaneData supplies it from live lane entities. DefaultDecal's
native DBufferMesh instead reconstructs coordinates using object transforms, MeshSize and TextureArea.
Segmented projection is the accepted conversion between these native representations.

Built-in textures remain references. External original texture dependencies follow the existing
byte-identical, same-CID snapshot policy. Generated surface descriptions change the rendering
representation, not the referenced original images. Preview property leases retain direct references
until the temporary preview is disposed.

## Verification and remaining acceptance

Source call tracing confirms that MovableDecalGraph now calls only MovableNativeDecals and reports
`baked marking textures=0`. It receives neither a texture writer nor a baking page size. The previous
curved decal snapshot/bake routines have no production caller. They are not used as fallback.

Release compilation passed. Eighteen installation originals were backed up, and all twelve deployed
payload files matched the build. Installation backup:
`artifacts/backups/all-native-markings-20261016-222736`.

Draw01 is the currently routed affected style. Its two stale export-state rows were already backed
up and removed during the preceding increment; no corresponding owned assets remained in either
active asset root then. No new Draw01 state entry existed for this deployment; no unrelated bridge
was removed. The prior state backup is
`artifacts/backups/native-marking-state-20261016-221651/export-state.tsv`.

The former 192-page sample has not been regenerated. New object counts, generation time, complete
asset size and visual fidelity are unmeasured. Human checks must cover curved/tapered markings,
segment joins, seams during opening, near/far views, preview rendering and cold loading. Possible
projection seams or overlapping receiver footprints require actual visual inspection. Compilation
and source tracing do not establish visual acceptance or completion of the other reported bridge
mesh/preview problems.
