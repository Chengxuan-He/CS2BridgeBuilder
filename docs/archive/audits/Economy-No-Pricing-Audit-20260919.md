# Bridge price correction — 2026-09-19

Baseline: dev `8c90b34a617b272d08ed3b27b2d24c7230f79458`.
Rollback ref: `rollback/economy-no-pricing-20260919`.

## Findings and correction

The previous implementation reconstructed selected-road costs from serialized section/piece
objects, then generated cost-only section/piece dependencies. Its exported charge was consistent
with that reconstruction, but this did not establish agreement with the initialized road price
shown by the game. In particular, the reported 1500/km road must enter the formula as 12/8m,
not 36/8m. Checking a charge asset alone was insufficient validation.

Local game assembly inspection confirms:

- `PrefabUISystem.PlaceableNetCostBinder` reads `PlaceableNetData.m_DefaultConstructionCost`
  and multiplies by 125 for currency/km. Auxiliary network costs are added separately.
- `NetUtils.GetConstructionCost` reads `PlaceableNetComposition.m_ConstructionCost` for
  actual construction; changing the UI scalar alone would not fix billing.
- `NetInitializeSystem` calculates the default price during PrefabUpdate;
  `NetCompositionSystem` calculates placement composition prices during Modification4,
  before the native cost system in Modification5.

The replacement reads the initialized selected-road scalar, retains verified prototype offsets,
and enforces the sum of selected decks' elevated construction bases as the floor. It stores the
result on the existing bridge as `BridgeConstructionCost`, without any asset references.
The main bridge carries the complete base, auxiliaries zero. `BridgePriceSystem` assigns this
value after each native producer, for both UI and placement. It never adds a second charge.
Prototype/source piece prices, elevation surcharges and upkeep are not rewritten.

The exact reason the former reconstructed Road Builder input differed from the user's live
price has not been reproduced in game. The replacement removes that input path; compilation
and arithmetic tests are not a substitute for live price, placement and reload validation.

## Cleanup and checks

- Removed 152 owned ImportedData directories, including 106 `_Pricing_` directories,
  the owned BridgeBuilder geometry directory, and two state/registry files.
- Backup: `C:\Users\admin\Documents\Codex\2026-09-01\qi\outputs\economy-no-pricing-backup-20260919`.
- Subsequent ownership scan: no remaining cleanup targets; no `_Pricing_` directories remain
  in ImportedData. Nine `RBExportDep_*` directories remain preserved.
- Release compilation passed. Pure arithmetic tests passed for 1500/km → 9000/km,
  elevated floor, single/double decks, signed offsets and overflow guards.
- Project contract and both AGENTS entry points prohibit all pricing prefabs, including
  renamed cost-only assets. Runtime source no longer creates pricing pieces or sections.

## User acceptance still required

**Acceptance failed on 2026-09-19.** The newly generated
`baca241f5-1a89-4b37-a9f7-4a222c034c64` still cost 18000/km. Its actual export report records
native selected price 36/8m, elevated floor 120/8m, offset 36/8m, final 144/8m. Installed and
built DLL hashes match (`8DFCDA0E36452AF4A456D70D76BB1BC6CD45FE44F19F102267D0E5364CC965B7`).
The arithmetic regression did not validate the live input. The discrepancy with the user's
1500/km road price remains unresolved; the user explicitly requested skipping further pricing
repair in the follow-up turn. Do not describe this pricing change as accepted or verified.

The same follow-up repairs the separate stale-lock activation path. Game IL shows UnlockSystem
runs in MainLoop whereas bridge publication/activation can occur inside PrefabUpdate. Activation
now resolves the actual prototype lock and synchronizes only generated decks when it is unlocked.
This compiles but still requires paused-game Create-and-build and Manage/Build acceptance,
plus a truly locked prototype negative case. Cleanup backed up seven imported directories,
eight geometry files and two state files to `outputs/unlock-race-backup-20260919` in the workspace.

Recreate blue suspension from the 1500/km road: base display should be 9000/km unless the
selected road's elevated base is higher. Check actual placement separately from height/object
surcharges. Repeat with a high-cost elevated road and a double-deck bridge; reload the save
and confirm the scalar is restored without new pricing dependencies. No game was launched
or operated for this audit, and no pre-release merge is claimed.
