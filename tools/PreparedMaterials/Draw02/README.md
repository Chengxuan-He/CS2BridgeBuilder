# Draw02 material authoring and verification

## Current production route

`Draw02RoadSampling` starts with an unsupported (`None`) domain, then supplies measured
per-LOD moving carriageway, fixed carriageway and sidewalk appearance levels. Far LOD
uses its own measured finite mapping. Unsupported input types preserve their original
material. Physical motion partitions remain separate from appearance partitions.

Finite cap/ramp and other fully reusable domains bind their original Surface and atlas
UVs. Evidenced repeating domains use offline prepared materials with continuous UVs;
no new texture-period seams or complete-span texture baking is performed. Three existing
raw units serve moving carriageway, moving sidewalk and fixed sidewalk. One painted
fixed-carriageway unit is shared between finite short terminals and the complete 4 m
repeating domain, with different per-use V sampling. U stays finite.

## Offline preparation

Use only the assigned Draw02 private Unity project, baker, writable caches, output and
log directories. Never run two Unity processes against that project concurrently.

- `ExtractFixedCarMapping.py` emits the 64 measured cap/ramp/short/repeat mappings and
  their typed BBN data, preserving per-LOD source identities and native tangents.
- `ExportFixedCarPaintRecipe.py` records exact source channels and all 13 source-mip
  paint masks/donor rules. Control remains unchanged. This is local composition,
  not recovery of an unknown original surface beneath painted pixels.
- `BuildFixedCarPaintTrial.py` prepares paired unpainted/painted manifests for the
  reviewed shared producer. `CheckFixedCarPaintTrial.py` verifies mask isolation and
  unchanged Control using all output mip readbacks.
- `StageFixedCarPaintCandidate.py` stages the one painted unit and helper BBN data.
  `StageOfflineAtlasCandidate.py` stages the separate three raw units.

The fixed unit preserves the complete original 4 m cycle with a cyclic phase shift;
short finite sampling lies inside the same unit. Phase rearrangement and generated mips
are not claimed to preserve all source texels or filtering exactly.

## Verification and acceptance limits

`SourceCoverageProbe.Editor.cs` exercises the frozen production mappers on all measured
road source triangles, retaining the old fallback as a negative coverage detector.
Candidate 12 passed 38,448 source-face uses without legacy/unsupported leakage.
`FixedCarCandidateProbe.Editor.cs` exercises actual mapper binding, composition and native
model upload for all four fixed-carriageway roles. Candidate 12 completed 576 cases.
`AuditFixedCarQuantization.py <geometry-folder> 192` checks exact Float32 rounding cells,
topology and an independently derived absolute area bound for that four-role matrix.
The older two-role matrix uses 96. Historical relative-area diagnostics remain recorded.

Reports live under `artifacts/material-remap/Draw02/`. Automated mapping, native upload
and paint-isolation checks do not establish PBR/filtering quality, native registration,
independent cold load, human motion/near/far acceptance or the complete owner/package budgets.
Existing boundary and internal-mip quality counterexamples remain unresolved. These are
reviewable development resources, not accepted release assets.

## Retired road aliases

The five old `RoadRepeat` band-composition aliases are unreachable from the current route.
`Draw02RepeatUnits` deliberately returns no old alias. `RetireLegacyRoadAliases.py`
checks measured source coverage and every surviving Draw02 Surface reverse reference
before moving only unused originals. Eighteen files (792,348 bytes) were retained under
`artifacts/retired/material-remap/Draw02/legacy-road-aliases/`; twelve texture payloads
still referenced by structural Regions remain packaged. The retirement report lives
outside that backup at `artifacts/material-remap/Draw02/legacy-alias-retirement.json`.

`Prepare.ps1`, `ExportRecipes.py`, `IntegrateCompositeUnit.py` and
`RestoreRepeatDescriptorSemantics.py` require an explicit legacy-evidence flag. They
reproduce historical evidence only and must not restore retired production road bindings.
Older delivery notes and recipe checks describe their original captured candidates,
not the current generation route. `RepeatBindingsCheck` now verifies that all five
historical sampler keys remain unmapped.

## Structure and localization

Structural Regions retain their independent bindings and shared texture dependencies.
`IntegrateStructures.py` handles only the structural inventory. Do not remove a texture
because a road alias was retired: its surviving CID consumers govern retention.
No runtime UI strings or localization keys change in this material mapping work.
