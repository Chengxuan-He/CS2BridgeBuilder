# Lift02 source-level material preparation

Use `RebuildSourceInputs.py` to reconstruct the Lift02 native source inventory directly from original game archives and measured BBN data:

```powershell
python tools/PreparedMaterials/Lift02/RebuildSourceInputs.py
```

The default staging root is `artifacts/agent-material-env/Lift02/source-rebuild/`. Inputs and reports have separate subdirectories. Do not invoke the old per-triangle preparation inventory to populate the replacement material library. A source texture is shared across its sampling variants; compatible original Surfaces remain direct native references with their complete original mip dependency graph.

The inventory includes 18 native renders, 20 material uses and 13 original Surfaces, including the glass material. All source descriptor and physical backing hashes are measured from game/DLC archives. The original source texture footprint is not a mod package-size measurement. Do not copy built-in textures into the mod merely to reach a file-count or size estimate.

`ExportNativeCharts.py` produces `Lift02UvCharts.cs` and its Lift02 BBN resource from actual native indexed UV chart boundaries. `ChartBindingCheck` compiles the actual shared native chart implementation and checks original-Surface reuse against real generated receiver coordinates. `CheckNativeCharts.py` supplies an independent whole-triangle containment and texture-density check. All these tools keep sampling data separate from texture payloads.

`PrepareRecipes.py` and `VerifyRecipe.py` retain the measured stationary-end local repeat evidence: source Surface `ea923855f34e05d7a517992fe064ddb7`, source faces 83248 and 83249, a 4 m longitudinal period and a 0.42156982421875 m transverse period. The original VT channels are 4096 by 4096. These measurements are input to the new native shared-source sampler; they do not authorize baking a separate region image for every sampling variant.

Lift02 moving-road preparation retains selected-road appearance (`PrototypeRoad = false`). The central interval [-82, 82] m belongs to bone 1; fixed end intervals [-106, -82] m and [82, 106] m belong to bone 0. Keep that behavior when integrating the replacement producer.

Common Road, Paint and transparent-control preparation belongs to the shared material producer and must occur only once for an identical constant resource. Each bridge style must have independent complete non-built-in ownership copies with unchanged CIDs. Do not use another generated bridge or the old large prepared library as a fallback. Only the coordinating agent's cleanup completion and producer interface notification authorize formal library integration.

All tools are offline implementation support. No user-visible text changes, geometry edits or native-acceptance claims are introduced. Original density, mip behavior, channel semantics, saved-asset independence and the complete official SDK package limit remain acceptance requirements.
