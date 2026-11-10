# Native149 deletion lifecycle implementation and isolated verification

This is an implementation audit, not a replacement for the deletion contract. Native149 was installed locally. A single explicitly authorized, tombstoned backup bridge was added for manual acceptance. No game or launcher was started, no existing player bridge was removed, and nothing was published or pushed.

## Implemented behavior

- Confirmation durably creates and closes the zero-byte `.bridgebuilderdeleted` marker before logical mutation. A map-only owner instead receives an outside record. Existing valid intent is idempotent; malformed intent never authorizes mutation.
- Management, road selection, catalogue rebuild and UUID allocation honor marker or outside record. Startup resumes only logical removal for marked owners and excludes them from ordinary inspection/migration and its counts, popups and restart requests.
- Map removal precedes prefab unregistration. Bulk removal retains the entire-batch map barrier. Native shared allocations and physical read paths remain alive during the session. Retained NetTool selection and road-menu group entries are cleared through native data before unregistration.
- Physical cleanup uses the internal late normal-shutdown observation described below. It first commits `ModsData/BridgeBuilder/DeletionPending/b{uuid}.pending`, then removes only the matched direct owner directory recursively. It clears the outside record only after verified directory absence. Partial failures retain intent. Directory links are rejected; there is no asset dependency gate or per-payload deletion.
- User-confirmed logical completion explicitly says file cleanup is pending safe shutdown, in all twelve languages. Startup handling remains silent. No external script, executable, service or task was introduced.

## Native shutdown evidence and guard

The installed player reports Unity **2022.3.71f1**. Inspection of the actual game assemblies establishes this order in `GameManager.TerminateGame`: committed Quitting state and save completion; mod disposal; world/native-runtime destruction; `TaskManager.CompleteAndClear` with a native timeout wrapper; global asset-database disposal; logging teardown; process quit.

Consequently, mod `OnDispose` only arms the observer. The observer runs after successful disposal of the **same global database instance captured during mod initialization**, not after arbitrary databases. It requires the actual captured `CompleteAndClear` task to have completed successfully, an empty native queue, completed remaining registered tasks, all ECS worlds disposed, and successful logical removal of each owner. A timeout wrapper returning alone cannot pass these checks. A normal world transition invalidates previous logical completion and rediscovers pending intent; committed shutdown preserves completion while destroying the world.

The installed `Cities2_Data/boot.config` includes `single-instance=`. The matching [Unity 2022.3 API documentation](https://docs.unity.cn/2022.3/Documentation/ScriptReference/PlayerSettings-forceSingleInstance.html) describes the native startup restriction and rejection of a concurrent same-player instance. The implementation conservatively requires the configuration to remain unchanged and checks for an already observable competing Cities2 process as an additional guard. Process enumeration is **not** the primary exclusion mechanism. Unsupported or changed configurations leave cleanup pending.

This is native code/configuration evidence plus isolated tests. The agent did not launch concurrent game instances, perform a real game shutdown, or claim live acceptance. A failed or skipped lifecycle condition leaves durable intent and logs the condition. Crash/kill/power loss can skip cleanup; a later enabled session excludes the owner and retries at a verified normal shutdown. Cleanup with Bridge Builder disabled is not promised.

## Verification

Frozen source: `artifacts/material-remap/shared/native149/source`, copied from native148 with the explicit overlay in `scope.json`.

- Frozen production build passed against the installed native assemblies. Two pre-existing nullable warnings remain in MovableSurfaceAtlas.
- 39 source-linked intent/shutdown checks passed, including confirmation/cancellation, marker durability/idempotence, map-only owners, whole-batch map barrier, locked-file partial deletion with outside intent retention, successful retry, malformed marker preservation, traversal refusal, world transition, global database identity mismatch, duplicate callbacks, native drain fault/incompletion, live worlds, missing single-instance protection, competing-instance denial and non-game-host refusal. Native UI/lifecycle dependencies in this executable are test doubles; it does not simulate a real engine teardown.
- Existing source-linked instance-removal tests passed, including ordinary-road/shared-junction preservation, editor handling and repeated removal.
- Self-check lifecycle tests passed, including the new marked-owner exclusion: no structural inspection, dependency migration, popup or repair count for that owner.
- All twelve localization suites passed. `git diff --check` passed.
- Official SDK package: **77,172,761 bytes**, 964 files. The existing 55 MB publication gate remains exceeded; this task only installed locally and did not publish.

Logs and receipts are in `artifacts/material-remap/shared/native149/`. No surrogate test result is presented as an in-game visual or shutdown result.

## Installed artifacts

DLL SHA-256: `BF59B3C304591258362942EF28291F4E08B57EBE993980D66D2F5B5AC7B470E1`.

All 964 installed files were verified. Only DLL/PDB changed; originals are in `before-native149-installed`. The game was absent both before installation and before adding the fixture.

## Authorized tombstoned bridge

Owner: `b420d211e-bc19-4b25-b669-843046dd4aff`.

Installed directory:
`C:/Users/admin/AppData/LocalLow/Colossal Order/Cities Skylines II/ImportedData/b420d211e-bc19-4b25-b669-843046dd4aff`.

Original backup:
`E:/CS2BridgeBuilder/artifacts/backups/before-restore-oct16-native102/ImportedData/b420d211e-bc19-4b25-b669-843046dd4aff`.

The owner was absent before installation. The fixture contains 246 unchanged native payload/sidecar files totaling **8,706,283 bytes**, plus the zero-byte marker. Only the obsolete `BridgeCacheEdges.txt` was omitted from the backup. Root Prefab CID is `891ade9d653aceb363c2c6ea68fc12d0`. No serialized owner or asset identity was rewritten. All copied payload hashes match their staged originals; the marker was present before the directory was installed. Other existing owner directories were preserved.

The root is a native RoadPrefab, local sidecar/payload pairing was checked, and no serialized BridgeBuilder runtime type was found. The backup has 44 Prefab references outside its 123 local CIDs. Archive-name matching did not resolve those references and is not proof they are missing: built-in Prefabs can reside in native serialized resources. A supplementary Unity type-tree read was stopped rather than treating incomplete tooling output as validation. Full current-game cold loading of this historical fixture is **not yet verified**. The marker prevents the mod from repairing it back into use; the purpose is to test silent logical exclusion and safe shutdown cleanup of the authorized fixture.

## Manual acceptance still required

With the user starting the game, the marked fixture should be unavailable in both management and road placement without a tombstone popup or self-check count. A normal user-controlled exit should then produce the verified-cleanup log and remove its directory and outside record. If native unregistration or a shutdown guard fails, the directory must remain excluded and pending with a diagnostic. Crash/restart and actual engine resource release remain manual acceptance boundaries, not completed claims.
