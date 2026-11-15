# Independent prototype-resource and preview-light changes

These are two separate issues. This change does not attribute missing prototype
materials to the preview material code, and does not alter that code.

## Prototype resource lifetime

On dev, TowerFactory still called RenderPrefab.ObtainMeshes/ReleaseMeshes in
section profiling, tower opening measurement, generation and LOD profiling.
The installed game's GeometryAsset.ObtainMeshes uses shared m_Data/m_Loading.
Its ReleaseMeshes destroys meshes and disposes those buffers when the Unity mesh
instance counter reaches zero, without consulting mRequestCounts. The city
asynchronous renderer uses RequestDataAsync and its separate mRequestCounts.
Balanced synchronous mesh access can therefore invalidate a live prototype's
streaming data. This is a concrete common-path hazard, not proof that every
reported missing material has the same cause.

Restore the independent descriptor/buffer reader previously present in 453c162
(removed with the published-baseline restoration). Every TowerFactory read,
including separately authored LODs, now uses private buffers and destroys only
the resulting private Unity meshes. Native conversion preserves vertex layouts,
normals, UVs, submeshes and bounds; geometry transformations are unchanged.

Preserve surface-slot order. The old null filtering shifted all subsequent
surfaces and the mesh-count comparison ignored multiple submeshes. Validate
against the total submesh count before writing generated geometry and reject
missing/misaligned slots without changing the source.

The catalogue's read-only material audit traverses installed donors, sections,
pieces, objects, reverse placeholder replacements and LODs, resolving material
slots and checking surface streams without loading/unloading shared materials
or textures. It logs coverage and unreadable references; VT texture dummies are
not incorrectly reported as missing physical texture files. It cannot prove
rendered appearance or GPU residency. The existing asset-anatomy dump is dated
September 19 and is not evidence that today's prototype materials are healthy.

## Preview key

The previous key was still Point at (50, -35) against a camera facing (35.264, 45).
It is now Directional at (45, 35), illuminating the visible front/top, at the
same 4000 lux centre illuminance. All eight studio lights are directional and
shadowless; only the key contributes specular. Cast shadow maps are disabled
on the preview camera to remove hard projected shadows and avoid requesting
HDRP's city-sun cascade atlas. Normal-based shading remains. Fixed environment,
exposure, alpha and supersampling remain unchanged. No global light setting,
prototype material, city camera or authored bridge light is modified.

## Verification limits

Release compilation and whitespace checks pass. Source inspection confirms no
TowerFactory shared ObtainMeshes/ReleaseMeshes calls remain. No visual unit test
or automated game control was performed. Restart the game to discard already
invalidated in-memory renderer state. Human acceptance must check original
bridges near/far before and after generating bridges, compare daytime/nighttime
previews, and retain the next Prototype material audit log summary. Missing
source pack files, if the audit reports any, require separate identification;
do not recolour materials, replace originals with generic surfaces or delete
game/mod source assets to conceal failures.

## Deployment

Cities2.exe was stopped and verified absent. The installed mod and owned generated
assets were backed up under
`C:\Users\admin\Downloads\BridgeBuilder-prototype-resources-20260930`.
Required ownership-scoped cleanup removed 10 imported directories, 10 generated
geometry files and 2 state files, preserving 9 RoadPrefabExporter dependency
directories. The following dry run found zero targets. No save file was changed.
Twelve installed payload files match Release output hashes in active `160320_4`;
no duplicate local installation was created. DLL SHA-256:
`D4B7CC1811A16F97783A8B16A2BE529A1E78C62A5337097FBE9A30A7AC717BAE`.
No GitHub push or Paradox Mods publication was performed.
