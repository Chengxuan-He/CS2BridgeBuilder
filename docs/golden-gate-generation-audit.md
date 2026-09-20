# Golden Gate generation audit — 2026-09-21

Branch: `bridge/golden-gate`; baseline: `578bb62192d2c9291a568dfbb081c2f62045b0f2`.

## Evidence

Prototype data: the real `asset-anatomy.txt` captured on 2026-09-19, retained in
`C:/Users/admin/Downloads/BridgeBuilder-GoldenGate-evidence-20260921/ModsData`.
Generated assets and geometry from the current game session were retained alongside it.
Single-deck sample: `b6af527c6-ef20-480a-b8bb-b10e6c987ddd` (XL Road Divided).
Double-deck sample: `bb9d07446-f5f4-4942-a178-f41b5c25bdc3` (asymmetric highway).

| Defect | Prototype | Generated path / correction |
| --- | --- | --- |
| Main cable deformation | End/middle cable pieces are separate from support pieces; clear spans 25.01709/24.69287 m; no separate LodProperties in captured prefabs | Shared section profile and railing fitting were applied to cables too. Exact cable identities now use signed rigid translation for every mesh, bypassing support railing remapping. |
| End anchorage excavates ground | GoldenGateBridgePylon01 has BuildingTerraformOverride, with DontRaise and DontLower true (anatomy lines 27991–28019) | Build applied only generic object role and mounted props. Preserve the complete authored terraform component. No artificial y offset added. |
| Missing tower foundation | GoldenGateBridgeBase01 replacement uses PillarObject type Base (anatomy around 28254); BXP base has the same role | Exported generated Base01 prefab had m_Type=2 (Standalone). Preserve the source pillar type, offset and vertical range after binding the new placeholder. |
| Double-deck tower not derived | BXP Train/Subway use BXP GoldenGateBridgePillar Placeholder - Custom Lighting, with two replacements including the base | The style table listed only the end anchorage. Record the actual BXP main tower too, using its 25 m prototype road datum. |

Game DLL inspection of PillarObject.Initialize confirms m_Type is copied directly into
PillarData; it is not a cosmetic label. BuildingTerraformOverride contributes
BuildingTerraformData to the prefab. Do not substitute a generic standalone role for a base.

This change does not perform a width-invariant adjustment and adds no audit delta at runtime.
Cable identity metadata comes from the captured real prototype, not generated bridge dimensions.
The source meshes and their materials stay bridge-owned / prototype-derived as before.

## Validation status

Compilation passed. No visual unit tests were run. Old generated assets were backed up and
removed by the ownership-scoped cleanup script. New in-game output is not yet available:
near/far inspection of newly created single and double Golden Gate bridges remains required.
Check cable roundness, anchorage terrain, and both tower foundations at low and high elevation.
Compilation alone is not visual acceptance.
