# Movable source-face and native LOD coverage audit

Use the authoritative [source-face coverage contract](../../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#source-face-coverage-and-prevention-of-missing-structure). This toolkit supplies reusable evidence entry points, not automatic prototype part classification.

## Saved owner inventory

Run `IndexSavedMovableOwners.py --game-data <Cities Skylines II user-data directory> --report <evidence.json>` with the existing project Python interpreter. It reads export history and checks actual current owner paths. It neither creates a bridge nor changes a saved owner. An absent owner is reported as missing evidence, never a pass; a history row alone does not establish a current owner or installed build.

Current seven-style inventory: `artifacts/material-remap/shared/seven-style-coverage54/saved-owner-index.json`. All seven styles currently lack a live saved-owner sample. Use each style's measured native source and final-plan/native-upload fixtures, and retain that exact limitation.

## Source and final-plan evidence

- `tools/GeometryMetaprogram/UvDensityAudit.py`: `NativeGeometry` decodes actual Geometry positions, triangle indices and UVs through the installed native PipelinePlugin. Its default result is not a complete raw attribute/bone dump. Do not report it as one.
- `tools/PreparedMaterials/Draw02/UndersideProbe.Editor.cs`: executed example for native packed-attribute input, final structural plan, TryBuild, ToUnityMesh readback and an isolated real Retile.Append path. Reuse the sequence with the correct style's immutable mesh/face data; do not reuse Draw02 dimensions, masks, source IDs or skeleton assumptions.
- `tools/PreparedMaterials/Draw01/StructureMembershipCheck.Editor.cs`: reflection example for reading measured face and widening memberships from a frozen production DLL. Membership alone does not prove final visibility.
- `artifacts/material-remap/shared/draw02-structure52/ExportAttributes.py`: actual saved-owner attribute extraction example; records native geometry bytes/hash, P/UV, packed N/T, color and indices with render bindings. The saved original Prefabs retain procedural skeletons and bind poses. This exporter is not a live animation capture.

For each style, retain source version/hash and exact mesh/LOD identity, original face IDs, final `RemoveTriangles`, replacement-fragment ownership, added-vertex provenance, physical role and prescribed transform. Classify every affected source face as retained, replaced or intentionally omitted with an explicit reason. Check duplicate whole-face/fragment coverage, winding, unchanged unrelated faces and the real final constructor/retile path. Preserve raw channels where required and distinguish exact Half-to-Float32 promotion from byte equality.

## Independent native LOD verification

`dotnet run --project tools/MovableMaterialSlotsCheck -- <frozen BridgeBuilder.dll> <planner-report.json>` checks the production planner against the saved Draw02 owner and the Draw03 regression. It includes all 243 combinations of Draw02's five independent renderers, original slot identity exactly once, all-LOD road anchors and 16 physical rail occurrences. This is not automatic acceptance of another style.

Use [NativeRenderGroupAudit](../../NativeRenderGroupAudit/README.md) to pass another style's actual ordered producer slots, boundaries and measured physical coverage IDs to the current or frozen planner. It reports incomplete post-balancing evidence rather than inventing labels for sliced meshes.

The current `MovableRoadAssets.Supplement` optional `semanticKey` must identify the same physical occurrence across every LOD, even if one LOD uses a measured supplement from another source level. Missing/duplicate keys cannot establish correspondence. A complete structural prefix that fits the native budget is kept together, including far supplements replacing near main-mesh parts. Larger structural prefixes still need explicit complete occurrence roles; audit the actual selected plan rather than assuming every graph received the Draw02 fix.

Read native LodProperties, Bounds, SurfaceArea, IndexCount and source grouping, not only same-numbered near/middle/far results. `artifacts/material-remap/shared/draw02-structure52/CheckRenderMetadata.py` and `ComputeLodThresholds.py` reproduce the actual owner threshold audit from saved native fields and the captured installed-game formulas. They are read-only evidence scripts; adapt owner paths/group count explicitly and re-check native version/stack direction before reuse. `lod-batch-absence.md` retains the failing viewLOD132 example. Test every part across independent group choices; batch-count compliance is insufficient.

## Evidence boundaries

Keep geometry removal, material/channel/alpha/mip behavior, renderer ownership/culling and native motion transforms as separate possible causes. A final-plan test, native upload, isolated Retile call, saved owner and running game each prove different portions of the chain. Do not fabricate missing GameDB or animation observations. Record no/one-sided/unequal sidewalk allocations and ordinary measured width changes without dedicated narrow-road adaptation. Leave saved owners intact and perform no automatic game generation.
