# Native150 prefab retirement ordering

Historical diagnostic and local deployment record, 10 November 2026. The current deletion contract remains the authoritative specification; this report is not an additional contract.

## Observed failure

The native149 user session loaded 16,063 prefabs, then failed in `PrefabUpdate -> PrefabInitializeSystem` at 00:48:42.841. `PrefabSystem.GetPrefab(PrefabData)` threw `ArgumentOutOfRangeException`. Mesh, network, object and infoview initialization subsequently failed. Road Builder's first recorded missing section was `Tiled Side0` at 00:48:44.634; missing lane groups and `Ground Cable Section1` followed. Bridge Builder's ordinary eight-owner inspection did not begin until 00:48:50.420. This was not an isolated missing Ground Cable asset or a Road Builder dictionary defect.

Native149's separate tombstone worker could run before that inspection and before the first native prefab initialization pass. It was scheduled before `PrefabSystem`, and accepted `WorldReady` plus completed asset loading as sufficient readiness.

The installed game's assembly establishes the unsafe sequence:

1. `GameManager.Initialize` sets `WorldReady` before waiting four frames and raising `onWorldReady`.
2. `PrefabSystem.RemovePrefab` adds `Deleted`, reindexes the remaining list and writes `PrefabData.m_Index = -1000000000` on the removed entity. It does not remove `Created`.
3. `PrefabInitializeSystem` queries `Created` plus `PrefabData`, without excluding `Deleted`, and reads the indexed prefab.

Thus early tombstone retirement can invalidate an entity still selected by the native initializer. The observed stack matches this failure mechanism. The logs do not independently record the exact invalid index or every retired entity; the sentinel and scheduling evidence come from the actual native implementation and the Bridge Builder call path.

## Repair

The deletion contract was updated before code. `BridgePrefabRetirement` now requires a menu, game or editor context, ordinary loading/inspection/save readiness, an empty native `m_UpdateMap`, and no prefab entities awaiting `Created` or `Updated` processing. Native `CleanUpSystem` removes these initialization/update tags; the gate has no fixed frame delay.

The same gate covers the owner worker, confirmed bulk map removal, direct single-bridge map removal and its pending continuation. Confirmation and durable intent remain available while initialization is pending. The native unregistration loop checks readiness again. A technical log records the owner and context when initialized prefabs are actually unregistered.

No Road Builder code, game assets, material conversion or player bridge payloads were changed. No premature index exception is caught and hidden as a successful initialization.

## Validation and deployment

- Primary and frozen Release builds passed. Existing nullable warnings in `MovableSurfaceAtlas` remain unrelated.
- Fifty production-source-linked deletion checks passed, including `WorldReady` in the boot context, pending `Created`/`Updated`, queued prefab replacement, bulk map ordering, sentinel-index reproduction and release after initialization. Native ECS/UI lifecycle objects in this executable are test doubles; this is not a game-run acceptance test.
- `tools/DeletionIntentCheck/VerifyNativeOrdering.ps1` checked the actual installed `Game.dll` implementation. SHA256: `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`.
- Self-check lifecycle, map instance removal and all twelve language checks passed. `git diff --check` passed.
- Native150 was installed with Cities2 absent. All 964 installed manifest files were verified; only DLL/PDB changed, with their native149 originals backed up.
- Installed DLL SHA256: `4CD157A091AEF4FD8C357EA3F2421028FCB4EC90B4AF18DF5188CE8C7DAF0635`.
- Official SDK ZIP: 77,172,799 bytes; SHA256 `9845E258DCF1D6849B6F28FA8D84A5CBF6D64270C78F3B7C6529B1451E6FD532`. No publication or GitHub push occurred.

Evidence, frozen source, logs and receipts are in `artifacts/material-remap/shared/native150`; the deployment receipt is `deployment-native150.json`. Original affected source files are backed up in `artifacts/backups/before-native150-prefab-readiness`.

## Tombstone fixture and remaining acceptance

The authorized fixture `b420d211e-bc19-4b25-b669-843046dd4aff` remains in ImportedData with its zero-byte `.bridgebuilderdeleted` marker. All 246 original payload/sidecar hashes still match the installation manifest. It was not reinstalled, deleted or rewritten during this repair.

The previous session crashed during `World.DisposeAllWorlds`, with a native `EntityComponentStore.Dispose -> BufferHeader.Destroy -> UnsafeUtility.FreeTracked` stack. It did not reach the global asset-database disposal callback that gates physical cleanup. This explains why physical deletion was not observed; the exact native allocator failure has not been independently reproduced or proved to have a single cause.

The repaired startup ordering, Road Builder catalogue initialization, silent fixture retirement and subsequent normal-shutdown physical cleanup require a fresh user-run session. The agent did not start or stop the game, and does not claim those live acceptance checks passed.
