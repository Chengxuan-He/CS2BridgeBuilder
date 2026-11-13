# GO create-and-build readiness

## Observed failure

On 2026-11-13, `BridgeBuilder.log` recorded successful persistence and native network
publication for `b1f019d4e-faea-4a80-b98f-e71531d89df1` at 21:38:27.737 (`ready=True`),
followed by `ActivateNotReady` at 21:38:28.093. The owner used GOViaduct with the selected
Road Builder network `r10f81fc0-7603-440d-ad80-7912c2c6e72a-76561199197854251`.
Eight dependency copies had completed successfully. This was a placement-activation failure
after commit, not evidence that the saved geometry should be removed.

The saved root contains an active native `Unlockable`, `m_IgnoreDependencies=false`,
one explicit RequireAll reference and no RequireAny references. The current native PrefabsMap
maps `6b53d6c4984e3bc4097b7707ef687186` to path ID 121938, `FeaturePrefab RoadsHighways`.
It is a valid native highways requirement. No unlock requirement was invented or replaced.

## Native execution and defect

The installed Game.dll scheduler runs `PrefabSystem` (including its nested PrefabUpdate),
then the remaining main-loop systems and `UnlockSystem`. The former Bridge Builder
publication callback ran at the tail of PrefabUpdate, before that unlock pass.
`PrefabInitializeSystem` initializes and late-initializes Created prefabs; it does not clear
their Created marker. `CleanUpSystem` removes that marker later.

For carried gates with implicit dependencies, the old availability check called the portable
unlock snapshot routine. That routine intentionally refuses Created sources and unsupported
recursive/manual network graphs. These are snapshot constraints, not native current-state
unlock rules. Thus an initialized fresh network could pass publication validation and immediately
fail the snapshot-based availability test. The source-linked regression reproduces this path;
the original log did not capture the complete live dependency buffer or progression bits.

Native `UnlockSystem.CheckUnlockRequirementsJob` reads the initialized `UnlockRequirement`
buffer and each requirement entity's enabled `Locked` bit. Its main-loop update repeatedly
propagates unlocks until no additional groups unlock. It does not recursively snapshot the graph
or classify a locked self/cyclic requirement as unavailable. It runs outside simulation ticks,
so pausing simulation does not require an artificial timeout or forced system update.

## Implementation

- Publish completion in MainLoop immediately after native `UnlockSystem`. Retain the existing
  live entity, network archetype, section and menu-registration checks and replacement handling.
- Evaluate carried implicit gates from their initialized native buffer with native AND/OR
  current-state semantics. Missing buffers/entities remain diagnosed readiness failures.
  Self/cyclic locked requirements remain locked. Explicit owned group evaluation is unchanged.
- Preserve snapshot readiness checks for persistence. No forced unlock, retry timeout,
  generated-asset rewrite, fee change or GO geometry change is involved.

## Verification and delivery

`UnlockSnapshotCheck` passed 67 cases, including all eight carried AND/OR combinations with
a Created root, retained snapshot rejection, locked self/cycles, unavailable entity/buffer
and initialized empty-buffer behavior. `PublicationCheck` passed dependency-before-root,
one-time completion/menu membership, replacement and failure cases and checks the new scheduler
registration. These tests use source-linked native API stand-ins, not a running game.
Both JavaScript and C# localization checks passed all twelve languages; no user-facing text changed.
The production build passed with four pre-existing nullable warnings.

The active playset and the last loaded UI module identify installed package `160320_19`.
With the game absent, originals were backed up and DLL/PDB/MJS outputs copied and hash-verified.
Version remains `26.11.12a`. All 142 files in the reported saved owner were hash-unchanged.
No bridge was deleted, migrated or regenerated. User acceptance remains pending: manually launch
the game, use Manage/Build on the existing owner, and test Create-and-build on another request.
An actually locked highways feature must still produce the normal locked response.

Evidence: [deployment receipt](../../../artifacts/activate-not-ready/deployment.json),
[saved owner hashes](../../../artifacts/activate-not-ready/saved-owner-hashes.json),
[build log](../../../artifacts/activate-not-ready/build.log) and
[native decompilations](../../../artifacts/activate-not-ready/native/).
