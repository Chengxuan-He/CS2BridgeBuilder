# Title recovery and save cleanup separation

## Implemented locally

- Keep legacy unlock migration and the registration guard at the earliest startup callback, before native network initialization.
- Run owned prefab disk validation and retirement in `BridgeStartupAssetSystem` after MainMenu loading completes. Confirm current damage on separate engine frames; retain files on access/inspection failures. Retry repaired, previously quarantined roots through normal registration.
- Retire exact audited files to non-importable recovery copies without removing native prefab indices, geometry, or materials. Preserve same-session retired UUIDs because later saves can still resolve their cached objects.
- Restrict `BridgeMissingAssetSystem` to placed-network cleanup after save loading. It no longer repairs, registers or deletes prefab files. Recognize both obsolete saved identities and same-session retired identities.
- A blocked shared junction produces an actionable message and ends this load's cleanup attempt without retrying. Saving is never intercepted. Foreign network deletion and arbitrary prefab substitution remain prohibited. This supersedes the earlier five-second retry behavior.

## Remaining player-save blocker

The captured local run reports a shared junction at `Entity(1171648:1)` with no initialized surviving road/track prefab to receive ownership. The same run reports missing `Divided Highway 6-lane`, other divided highways, and `Double Train Track Dual Electric`. This is not evidence authorizing deletion of those foreign networks. The exact adjacent dependency still requires in-game verification; the missing bridge's complete removal is not claimed.

Restore the player's missing network dependencies, restart to the title screen, and then load the test copy. Verify completion in BridgeBuilder.log before saving over any city. The original downloaded save was not edited.

## Verification and deployment

- Release build succeeded.
- 36 instance-removal/scheduling checks, 15 disk-audit checks, 49 reference-validation checks, and 29 unlock snapshot/recovery checks passed. These are offline tests with native stubs and source assertions, not in-game acceptance.
- All 12 deployed payload files matched their sources. DLL SHA-256: `D288EBB4F09A93F20FACF8981B987BA55B457CB9C79712E53BECC0D7076C33BF`.
- Existing local mod, logs, generated assets and fixture remnants were backed up under `C:\Users\admin\Downloads\BridgeBuilder-title-recovery-20261002` before the mandatory generated-asset cleanup. No test fixtures were reinstalled.
- No GitHub push or Paradox Mods publication was performed. Title-screen and player-save runtime acceptance remain pending.
