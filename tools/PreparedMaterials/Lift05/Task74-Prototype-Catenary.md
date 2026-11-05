# Lift05 prototype catenary removal

The user requires removal of this prototype overhead assembly for every selected road, because the selected road's overhead supply is produced by another existing flow. No conditional retention is added. Other overhead generation remains unchanged, and lift cables are retained.

## Root cause and source evidence

Installed73 owner `baa696b6a-60ee-488d-888e-3df2e0e5b612` was generated at width30.5, half-width delta11.25. Its original road has no overhead sections. The owner's existence was verified before reading; no deleted owner was read or recovered.

The contamination is original prototype geometry retained by `Lift05StructurePlan`, not a texture, extra native road object or newly produced catenary. Near source components825–902 and middle534–565 contain the prototype's eight cross-road suspension rows, two longitudinal contact wires, insulators/brackets and four end poles. Their Y range is9.800049–17.267578. They are separate from the lift's long vertical cables, counterweights, structural towers and overhead trusses.

| LOD | Source geometry SHA256 | Original triangle range | Removed faces |
|---|---|---|---|
|0|10a9c40e522c91debff4bc0251dbe93980cac6b92ff6b176c216e27ba52c05fb|29278–30749|1472|
|1|dc4b8d4ae9688952992b03d724d8549cdb79f52eb0877c7f6fe62d1246ce286e|17729–18243|515|
|2|Recorded in source-membership.json|No prototype catenary faces|0|

`artifacts/material-remap/Lift05/catenary74/source-membership.json` records all component bounds, faces, native identities and bones. Original source geometry hashes are checked before extraction; membership matches native decoded indices. All selected faces belong wholly to the recorded components. No runtime bounds/height/name heuristic or Boolean operation is introduced.

`saved-owner73-provenance.json` proves all1472near and515middle source faces are present in the actual saved owner, using exact Float32 Y/Z and sourceUV corner multisets including multiplicity. X is intentionally excluded because width mapping changes it. Generated Geometry hashes are recorded. The previous73 native Plan retained every one of these faces whole at deltas0/3/12, with no replacement fragments. Original far geometry contains no corresponding catenary assembly.

## Change and compatibility

- `Lift05Catenary.cs` consumes the112-byte embedded `Types_Lift05_Catenary.bin` and removes only the measured triangle intervals from the existing Plan.
- `Lift05StructurePlan.cs` invokes that removal after road replacement and before structural Retile.
- `tools/GeometryMetaprogram/Lift05Catenary.py` reproduces source evidence and the BBN resource. `CheckCatenaryOwner.py` reproduces the existing-owner provenance diagnostic; `CheckCatenary.py` compares actual before/after Plan rows.

Preview and formal creation share this same Plan/RenderAssembly path. No source geometry, source material, native selected-road sampling, track utilities, road objects, motion skeleton, asset persistence or existing saved owner is modified. Candidate73 pure-road resources and RoadProjection remain unchanged. Existing selected supply continues through `MovableCatenarySampling`, native lane geometry, `MovableRoadObjects` and its attachment binding. This change makes no new claim about every possible selected-road utility configuration.

Localization review: no user-visible strings changed. The new layout error is an internal resource/schema diagnostic, consistent with existing native layout errors.

## Verification and acceptance

Candidate74 DLL: `artifacts/material-remap/shared/outputs/build-native-74/BridgeBuilder.dll`, SHA256 `AEDC748A7A63498CDC44D7771CE0021F23FF7919607D601FA408232385CC480D`. Shared integration confirms candidate73 materials, UI, vendor and public resources retain their hashes.

- Offline source extraction and actual saved-owner provenance: PASS.
- `Test-SourceFaceCoverage.ps1 -RunName native74-face-coverage`: all15 native Retile/TryBuild/ToUnityMesh cases PASS (9main plus6glass), with final indices, positions and original packed attributes/bones checked. Main output triangle counts are29606near,17983middle and2036far. Glass remains136faces per detailed LOD.
- `CheckCatenary.py`: all expected catenary faces are absent at half-width deltas0/3/12, with zero remaining fragments. All147981 unrelated source-disposition rows match candidate73 exactly:88578near,53931middle,5472far. This is the explicit negative control for lift cables, towers, road replacement and other retained structure. Evidence is `catenary74/native-plan-comparison.json`, retained `actual-plan-disposition74.csv`, and `native74-face-coverage.log`.
- `Test-RoadContinuous.ps1 -RunName road74`: all27 actual ForLod/Compose/physical rebind/native mesh cases PASS across3LODs,3widths and3poses. Reports `artifacts/material-remap/Lift05/road74.json` and `.log`.
- Local source whitespace checks passed; no changed user-facing localization text. Shared integration performs full localization/build checks.

Native shared selected-supply tracing confirms `MovableNetworkPrefabs` only adapts eligible non-underground track auxiliary `UtilityLane` selectors, then `MovableCatenarySampling.ForNode` adds the Node topology alternative for Catenary/LowVoltageLine while retaining other flags and native dimensions. Selected placements still flow through `Lift05ObjectAssembly` to `MovableRoadObjects` and native-bound end supports. These paths were not modified. There is no real Lift05 track ECS sample in this verification, so this is not an end-to-end selected-track acceptance claim.

Game visual acceptance remains separate: regenerate a Lift05 ordinary-road sample and inspect near/middle/far and open/closed states; confirm the original cross-road overhead is absent, lift cables remain, and a selected overhead-equipped road still receives its independently generated supply. Existing saved bridges are not rewritten by the generator repair. Deployment belongs to root/shared coordination, not this worker.
