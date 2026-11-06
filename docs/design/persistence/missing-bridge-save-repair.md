# Missing bridge save repair

At the city loading-complete callback, `BridgeMissingAssetSystem` checks placed
network references against the game's obsolete prefab IDs. It does not use the local bridge
registry as proof of absence. A live prefab is always preserved.

Scope: exact `b{uuid}`, `b{uuid}_Lower`, `b{uuid}_Upper` network prefabs only. Road Builder `r`/`t`
IDs, temporary previews, arbitrary suffixes and unrelated missing assets are excluded.
Obsolete static objects bearing an exact missing bridge ID are included even if owner links are lost.
The old `r{uuid}` bridge naming scheme is not reintroduced.

Removal uses `BridgeInstanceRemoval` and native `Deleted` processing, not immediate entity
destruction. Shared junctions survive; a missing junction prefab is replaced from an adjacent
surviving valid network when possible. Recursive Owner/SubNet/SubLane/SubObject descendants are
explicitly collected, marked Deleted and tracked. Applied/Created/Updated tags are removed from
deletion targets as in native SubElementDeleteSystem. Shared composition/prefab caches are excluded.
UI updates collect late descendants and verify every tracked entity is gone. Exceptions and a
30-second incomplete cleanup produce CRITICAL diagnostics. A single
localized dialog is shown after all tracked entities cease to exist. State is reset
before loading another city. No save file is overwritten: the user must save the repaired city.

## Manual acceptance (use disposable save copies)

- Load a save with a missing single-deck bridge: remove its segments and isolated endpoints;
  display one dialog. Check road connections, lanes, vehicles and orphan bridge objects.
- Repeat with a double-deck bridge, including a missing auxiliary `_Lower` or `_Upper` prefab.
- Connect the missing bridge to ordinary roads: preserve the roads and shared junction; confirm
  native junction rebuilding is correct.
- Keep a valid bridge prefab but remove its registry row: do not remove the bridge.
- Include an unrelated missing mod network: do not remove it.
- Load paused; verify deletion completes and the dialog appears (also check after unpausing).
- Load a second city without quitting; no counts or entity handles from the first city may leak.
- Save under a new name and reload: removed networks stay absent; no repeated removal dialog.

Compilation does not establish these runtime outcomes. In-game acceptance is still required.
