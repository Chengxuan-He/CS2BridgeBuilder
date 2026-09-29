# Automatic cleanup native crash, 2026-09-29

## Evidence

The local BridgeBuilder.log records confirmation of missing bridge
`b378a0ebe-7228-4acc-9e7b-6aad393eb6ef` at 21:06:08.113, a deletion plan of
432 entities at 21:06:08.129, and completion at 21:06:08.208. No prefab asset
was actually removed by finalization (0 removed, 1 skipped). Player.log ends
with native crash reporting and an empty managed stack. There is no crash dump
establishing the exact faulting native instruction. This is not evidence that
an asset-file deletion or the previous player's HDRP exception caused this crash.

## Confirmed lifecycle defect

Decompiled installed Game.dll: Game.Common.SystemOrder schedules MainLoop as
ToolSystem, LoadGameSystem, ModificationSystem, PreRenderSystem, UIUpdateSystem,
RenderingSystem, then PrepareCleanUpSystem. The latter snapshots Deleted entities;
CleanUpSystem subsequently destroys them. The former BridgeMissingAssetSystem
registration in UIUpdate could therefore mark entities AFTER lane/topology and
pre-render maintenance yet BEFORE destruction. CompleteAllTrackedJobs waits for
jobs; it does not rerun the skipped native lifecycle stages.

BridgeMissingAssetSystem now runs in PostTool before
Game.Objects.SubElementDeleteSystem. ToolSystem calls PostTool every update.
The native modification, reference and rendering stages now see Deleted before
the end-of-loop cleanup snapshot. No recursive native system Update is invoked.

The native LaneSystem.DeleteLanes excludes SecondaryLane; SubObjectSystem excludes
Game.Objects.Secondary. Our recursive owner/sub-element traversal now respects
both exclusions instead of preempting native secondary-reference management.
Selection of an entity in the deletion plan is cleared before deletion as it is
in the existing manual-delete path.

## Validation and limits

The entity-lifetime regression harness includes secondary lane/object preservation
and a source-level check on the cleanup phase registration, as well as existing
shared-junction, stable-evidence and deletion-completion cases. It is not an ECS
player or native rendering test. Compile/deployment checks cannot certify that
the reported crash is eliminated. Reload a COPY of the affected save with one
active BridgeBuilder installation, let automatic cleanup finish, then exercise
manual deletion and camera movement. Retain the original save and new logs.

The 21:04 startup also registered local and PDX UI payloads simultaneously.
Local test deployment consolidates them to the active 160320_4 cache, retaining
backups outside game discovery. No remote release is changed. A PDX repair or
redownload may overwrite this local test payload.
