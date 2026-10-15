# Draw01 render assembly

## Stacked column call chain

`Draw01RenderAssembly.CreateColumn` connects the measured column transform to the shared
render graph builder and TowerFactory's owned-prefab collection. Given the native highest
render and half-width increment, it resolves the two recorded LOD references in native order,
preflights their exact source names and excludes other bridges' ImportedData sources before
opening any geometry. It does not substitute the ordinary DrawBridge01NetPillar.

The method privately reads each source geometry, creates its recorded per-LOD column plan,
and releases the temporary Unity meshes even if preparation fails. Bounds preserve original
Y/Z and expand/contract X by the supplied half-width increment. They retain authored stacking
reach rather than deriving vertical bounds from a partial visible surface.

All three prepared levels enter CreateMovableRenderGraph together. That builder preserves
the original structural surfaces and components, rewrites private LOD references, validates
geometry/bounds, captures previews or saves owned GeometryAssets, and adds all prepared renders
to the normal prefab writer's ownership list. It creates no material, texture or fake skeleton.
Failure does not publish a partial LOD graph or modify the native source renders.

## Scope and verification

This connects the stacked column's measured data to resource construction. It is not the final
Draw01 bridge generator. The caller still supplies the generation owner/name and resolves the
native mechanism's column root. The full mechanism, footing, ordinary supports, selected road,
markings, attachments and final generation route remain separate required integration work.

Source geometry names, mesh/slot counts and LOD count follow the installed native evidence.
The shared builder separately checks actual source position/index streams against the prepared
plan. This does not establish compatibility with a future game version that changes geometry;
source hashes in the recorded column metadata remain the versioned evidence baseline.

Build validation and source-level ownership/disposal/caller review do not prove runtime or
in-game success. Human near/far, movement, shipping and independent cold-start acceptance remain
outstanding. No existing generator calls this assembly yet, so no existing bridge output changes.
