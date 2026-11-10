# Native151 shutdown event compatibility

Historical implementation and deployment evidence, 10 November 2026. The deletion contract remains authoritative.

## Cause and repair

The user session exited successfully but left the marked fixture directory intact. `Modding.log` records Bridge Builder disposal at 09:39:08.905, SuperFastBuildingAndLeveling at 09:39:08.951 and Tree_Controller at 09:39:08.966. Both latter mods' installed `OnDispose` implementations call ownerless `Harmony.UnpatchAll()`. The shipped Harmony implementation removes every owner's patches when its argument is null. This removed Bridge Builder's late task/database postfixes before the native calls they observed. Native `ModManager` itself does not remove patches. `GameManager.State` is an ordinary enum and the shutdown code directly assigns `Quitting`; this was not a flags-comparison defect.

After updating `BRIDGE_DELETION.md`, the implementation replaced those Harmony hooks with a direct Unity `Application.quitting` event subscription. It remains subscribed through committed application shutdown and unsubscribes on ordinary mod unload. It does not change other mods, suppress their unpatch calls, delete earlier or launch an external executor.

Actual native ordering is: reject the initial quit request; enter `Quitting`; dispose mods; destroy worlds; complete the task queue/registry; dispose the global asset database; release logging; finally set `Terminated` and request Unity exit again. The callback additionally verifies the captured manager's terminal state, absence of the global database, emptiness of the original database, disposed worlds, completed queue, empty task registry, completed tasks captured at mod disposal, unchanged native single-instance configuration and no observed competing player. `TaskQueue.Complete()` only returns the current completion task: the callback does not invoke another drain or wait for work.

Because Colossal logging is already closed, technical admission, pending and per-owner result messages go to `Logs/BridgeBuilder.Deletion.Shutdown.log`. This log is never used as self-check or persistence input. Missing/failed lifecycle evidence retains durable intent and files. Confirmed marker/ledger semantics, map-first ordering, silent startup exclusion and exact-owner recursive deletion are unchanged.

## Verification

- Primary and frozen builds passed; existing unrelated nullable warnings remain.
- Sixty-one source-linked lifecycle checks passed, including pending/faulted tasks, captured tasks hidden by registry clearing, live database/worlds, nonterminal callbacks, non-shutdown unload, repeated events and durable cleanup behavior. Engine lifecycle objects in this suite are test doubles.
- `tools/ShutdownEventCheck/Run.ps1` ran with the actual installed game Mono, UnityEngine.CoreModule and the installed SuperFast Harmony assembly. It installed a control postfix, removed it with ownerless `UnpatchAll`, then invoked Unity's actual managed quit-event delivery. The event subscriber survived, ran once, and stopped receiving events after unsubscribe. No game or Unity Player process was launched. This verifies actual managed event/Harmony behavior, not a live engine shutdown.
- Native assembly checks verify teardown and final quit-admission ordering. Self-check lifecycle and all twelve language checks passed. No user-facing strings changed. `git diff --check` passed.
- An initial .NET 8 control-patch attempt encountered an incompatible older Harmony runtime-detour failure; the successful regression uses the game's Mono instead. The production implementation does not add that host or any new runtime dependency.

## Local deployment and remaining acceptance

Native151 was installed with Cities2 absent. All 964 manifest files were verified; only DLL/PDB changed and their originals were backed up. Installed DLL SHA256:

`38F18727B4ACD164E15339DE144AF4974B462E2D5E8835362056063148FF920A`

Receipts, logs and frozen source are under `artifacts/material-remap/shared/native151`. Official SDK ZIP size is 77,173,036 bytes, above the pre-existing 55 MB publication limit. No publication, GitHub push, game launch or game shutdown was performed.

The authorized fixture `b420d211e-bc19-4b25-b669-843046dd4aff` remains installed with its zero-byte marker. All 246 original payload/sidecar hashes are unchanged. It was not deleted or reinstalled by the agent.

A user-run normal exit is still required to verify the complete engine callback and physical fixture removal. The next session should retain silent catalogue exclusion; normal exit should record callback admission and either verified removal or a specific pending/error condition in the new shutdown log. Crash, forced termination and earlier throwing event handlers may prevent delivery; durable records retain the work. No live cleanup success is claimed by this report.
