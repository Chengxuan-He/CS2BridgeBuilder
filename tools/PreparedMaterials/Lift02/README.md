# Lift02 source-level material preparation

Use `RebuildSourceInputs.py` to reconstruct the Lift02 native source inventory directly from original game archives and measured BBN data:

```powershell
python tools/PreparedMaterials/Lift02/RebuildSourceInputs.py
```

The default staging root is `artifacts/agent-material-env/Lift02/source-rebuild/`. Inputs and reports have separate subdirectories. Do not invoke the old per-triangle preparation inventory to populate the replacement material library. A source texture is shared across its sampling variants; compatible original Surfaces remain direct native references with their complete original mip dependency graph.

The inventory includes 18 native renders, 20 material uses and 13 original Surfaces, including the glass material. All source descriptor and physical backing hashes are measured from game/DLC archives. The original source texture footprint is not a mod package-size measurement. Do not copy built-in textures into the mod merely to reach a file-count or size estimate.

`ExportNativeCharts.py` produces `Lift02UvCharts.cs` and its Lift02 BBN resource from actual native indexed UV chart boundaries. `ChartBindingCheck` compiles the actual shared native chart implementation and checks original-Surface reuse against real generated receiver coordinates. `CheckNativeCharts.py` supplies an independent whole-triangle containment and texture-density check. All these tools keep sampling data separate from texture payloads.

`PrepareRecipes.py` and `VerifyRecipe.py` retain a historical stationary-end recipe. Source Surface `ea923855f34e05d7a517992fe064ddb7` has 4096 by 4096 VT channels. The original near/mid roadbed has a 4 m longitudinal U reset and a **finite** 0.42156982421875 m transverse V strip. The strip width is not a measured transverse repeat period. Do not use the historical recipe as authorization for transverse repetition or a generic asphalt appearance. Far LOD uses a separate finite atlas mapping.

`Lift02RoadAppearance.ForLod` explicitly gates both fixed and moving domains. No Asphalt, Sidewalk or WheelTrack support is evidenced. The verified TrainSubwayTrack input's base and worldspace layers jointly form Ballast on the TrackBed receiver role; the prototype supplies its own bed appearance. Near/mid reuse original Surface `ea923855f34e05d7a517992fe064ddb7` and its full original atlas. Continuous absolute atlas U retains the measured 4 m period and phase; the shared road-only wrap-seam path inserts only necessary reset seams. V traverses the original finite strip once per lateral domain. Far reuses Surface `006ce894a103c90606e67d95c32475ac` across three finite atlas domains without periodic resets. The central interval [-82, 82] m belongs to bone 1; fixed end intervals [-106, -82] m and [82, 106] m belong to bone 0. `BuildBedAppearance.py` measures these mappings. `AuditBedPhaseFrames.py` and `BedFrameCheck.Editor.cs` verify source phase and packed tangent frames. The shared wrap path consumes explicit native tangent overrides while interpolating the other attributes.

The `/2` and `/3` cropped-unit experiments remain diagnostic artifacts only. Neither is shipped or referenced by this route: original atlas border filtering must retain its neighboring texels rather than mix opposite crop edges. `AuditOriginalAtlasClosure.py` verifies all 13 original Surfaces and 19 physical backing assets directly against source archive hashes and CIDs. Original mip payloads remain byte-identical. New road material and texture payload bytes are zero; complete generated-owner and native cold-load checks remain separate acceptance gates.

`ExportUvSampling.py` validates all 20 original material streams and emits retained-axis masks (U=1, V=2) into the Lift02 BBN resource. Repeated finite islands on separate crossbeams retain both axes; every roadbed-domain source face proves U is longitudinal or constant, so its finite V axis is retained independently. Results are written to `artifacts/material-remap/Lift02/source-mapping/classification.json`. `SourceMappingCheck` replays the candidate assembly's actual geometry plans and shared axis dispatch against all original fixture streams at three widths. It does not replace final uploaded-UV, native-rendering or cold-load tests.

Common Road, Paint and transparent-control preparation belongs to the shared material producer and must occur only once for an identical constant resource. Each bridge style must have independent complete non-built-in ownership copies with unchanged CIDs. Do not use another generated bridge or the old large prepared library as a fallback. Only the coordinating agent's cleanup completion and producer interface notification authorize formal library integration.

All tools are offline implementation support. No user-visible text changes, geometry edits or native-acceptance claims are introduced. Original density, mip behavior, channel semantics, saved-asset independence and the complete official SDK package limit remain acceptance requirements.

`AtlasWrapCheck` reflects the actual integrated candidate's `Patch` constructor by parameter name and executes `MovableRoadAtlasWrap.Apply`. It checks two lateral receivers at three widths for source phase, finite V, footprint area, explicit native tangents, interpolated normals/colors/UV.zw and source provenance. Its receiver is deliberately a simple planar selected-road fixture: this isolates the wrap helper and does not claim a full `ForLod` callback, GPU upload, native material load or bone validation. Run only against a candidate containing the new helper; older DLLs correctly fail with a missing-type error.

```powershell
dotnet build tools/PreparedMaterials/Lift02/AtlasWrapCheck/AtlasWrapCheck.csproj -p:GameManagedDir='<Cities2_Data>/Managed' -o artifacts/material-remap/environments/Lift02/outputs/atlas-wrap-check
dotnet artifacts/material-remap/environments/Lift02/outputs/atlas-wrap-check/AtlasWrapCheck.dll '<candidate BridgeBuilder.dll>' '<Cities2_Data>/Managed' artifacts/material-remap/Lift02/source-mapping/atlas-wrap-check.json
```
