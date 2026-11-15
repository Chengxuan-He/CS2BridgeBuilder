# Incomplete bridge repair and the 2026-10-02 crash

## Evidence

Preserved input: `C:\Users\admin\Downloads\BridgeBuilder-crash-20261002-012221`
(Player.log, Logs, and installed-before). This run predates the prototype-source gating build.

- 01:06:23: title recovery retired all 12 damaged test identity groups. No
  `PrefabUpdate->NetInitializeSystem` CRITICAL appears in this run.
- 01:07:02: the loaded city contains obsolete root/lower networks for
  `b6927b0e6-ec5d-4564-b968-25068624da85`. Other missing assets include the
  Divided Highway 6-lane, 4-lane and 1+3 roads.
- Cleanup cannot safely transfer shared junction `Entity(982036:1)` to an
  initialized surviving connected network. It refuses the plan before Apply.
  There is no cleanup-scheduled/completed message. Repeated planning previously
  also counted unplaced retired fixtures and misleadingly logged their objects
  as being removed before applying anything.
- 01:08:12.857: autosave starts; 01:08:12.863: GameManager.Save starts.
  Player.log subsequently reports an UNKNOWN native crash with an empty managed
  stack, after an asset-unload message. A duplicate water-settings warning also
  appears; it does not establish the crash cause.

The logs establish temporal proximity to autosave, not the native instruction
that crashed. No crash dump was found. They do not prove that deleting bridge
entities caused this crash: the blocked plan had not been applied.

## Current policy: one attempt, no save restriction

The earlier bounded-retry/save-barrier implementation was withdrawn at the user's
request. BridgeSaveGuard and BridgeSaveRepairState were removed, including their
Harmony registrations and save-blocked/retry-stopped translations.

- Scan once per city load after deserialization, or at the first safe update for
  a load path without that callback.
- Submit the validated native deletion plan once at PostTool. Do not rescan,
  resubmit or extend the plan during subsequent frames.
- If blocked, stop immediately without retrying or deleting a foreign junction.
  A new load permits one new attempt. Later failure-registry revisions do not.
- After submission, observe native completion read-only; finish observation on
  success, exception or a 30-second timeout. Do not submit another deletion.
- Count retired identities only if actually used by placed networks.
- Automatic and manual saving are never intercepted by this cleanup feature,
  whether cleanup is pending, blocked, failed or complete.
- Blocked notices in all 12 languages only explain the invalid connecting
  network, restoring dependencies/reloading, and keeping the original save.
  The one-attempt and unrestricted-saving policy remains unchanged in code.

## Follow-up run: 01:53-01:57

Preserved input: `C:\Users\admin\Downloads\BridgeBuilder-autosave-investigation-20261002-020102`
(Player.log and Logs; build stamp `cb5f595f395a`).

- 01:55:42.529: one cleanup plan is blocked at shared junction
  `Entity(245706:1)` because no initialized surviving connected road/track can
  take it over. No blocked deletion plan was applied and no retry was recorded.
- 01:56:50.169: first autosave starts; 01:56:52.078: Saving completed.
- 01:57:43.126: bridge preview rendering completes successfully.
- 01:57:50.188: second autosave starts; 01:57:51.685: Saving completed.
  `02-十月-01-57-50.cok` exists (72,379,630 bytes, written at 01:57:51).
- UI.log continues through 01:57:56.082. Player.log subsequently contains an
  UNKNOWN native crash with an empty managed stack. No native dump was found.
  There is no new NetInitializeSystem CRITICAL in this run.

Read-only inspection of this installed Game.dll confirms that GameManager.Save
logs completion after writing the package and invoking its completion event,
but before disposing its temporary save database. The completion message is
therefore not proof that every finalization operation completed; subsequent UI
activity nevertheless rules out simply claiming the package write failed.

Duplicate water-settings warnings occur during both saves. Unused-asset unload
messages and a missing older water-settings asset also occur. These messages
do not identify the crashing native instruction. Other mods' pre/post-save
messages contain no exception in the inspected interval; they do not establish
mod culpability. Road Builder reports 2,287 invalid edges, which demonstrates
broader invalid network state, not ownership or causation by Road Builder.

Conclusion: missing dependencies explain why bridge cleanup stops, but this
run does not establish that they directly crash autosave. The crash is native,
after a successfully written autosave, with resource finalization/unloading or
subsequent simulation/rendering still unlocalized. No speculative deletion,
save interception, or rendering patch is justified by this evidence alone.

## Verification and remaining limits

Build and installed-file hash comparison are required. InstanceRemovalCheck
covers topology protection and checks single-load scan/submission boundaries,
terminal state and absence of save interception. Localization checks cover all
12 languages. These checks do not reproduce Unity's native crash.

Human acceptance:
1. Load a copy of the affected city: the shared-node blocker should be reported
   once, without further scans or deletion submissions during that session.
2. Confirm manual and automatic saves are not blocked by Bridge Builder.
3. Restore the missing dependencies and reload: one new cleanup attempt is allowed.
4. Load a healthy city: no automatic cleanup or save restrictions should appear.

The native crash instruction remains unidentified. No player save was edited.
