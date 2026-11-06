# Network initialization / independent unlock recovery (2026-10-01)

## Confirmed incident

The supplied FileSystem log first fails to resolve
`CID:b98f0304f8b33d02f5b79adab26663a6` in the Unlockable component of
`b7dff25d3-cf93-4466-9c26-d5aa3be39f3e` and its lower deck. Both contain the old policy:
RequireAll = [external prototype], RequireAny = [], IgnoreDependencies = true.
The CID identifies Bridge Expansion Pack's Golden Gate Bridge Subway prototype.
Unlockable.LateInitialize then calls IsUnlockable(null), followed by
NetInitializeSystem.AddSections calling GetEntity(null). This occurs before the
Bridge Builder OnLoad registration hook is installed. Post-load removal cannot prevent it.

The save contains references, not the missing bridge prefab/geometry payload. Its older bridge UUID
differs from the failing UUID in this log. We have not loaded or repaired the player's save in-game.
The original logs and save remain unchanged.

## Independent unlock policy

New bridges capture the donor's initialized native UnlockRequirement graph, including indirect
requirements. AND/OR flags and nesting are preserved; current unlocked state is not copied.
Intermediate network nodes become value-only groups. Native manual/progression leaves retain only
their value PrefabID, resolved against registered prefabs at evaluation time. No serialized
PrefabBase, Entity, external network or prototype reference is retained in a completed snapshot.

The data is encoded in the name of a native ManualUnlockable component. This is deliberately a native
component: imported files can be deserialized before code-mod OnLoad without needing a custom unlock
formatter. ManualUnlockable contributes only a self-lock, avoiding DefaultLateInitialize's dependency
traversal. Root and auxiliary decks receive the same snapshot. BridgeUnlockSystem evaluates it after
registration and emits native Unlock events. The development-restriction setting remains a runtime
override, not a mutation of saved rules.

Missing progression assets, uninitialized buffers, unsupported flags, cycles and manually unlocked
network leaves fail closed. They do not silently become unlocked or authorize deletion. A manually
unlocked network whose only rule is an external event cannot be copied as an independent progression
rule by this implementation: generation is explicitly rejected, not tied back to that network.

## Existing files and startup protection

BridgeStartupRecovery invokes migration automatically at the earliest BridgeConstructionCost.OnEnable
callback and retries from IMod.OnLoad if an import stream prevented the first disk pass. Registration
protection is installed first. UnlockRepair remains an optional PRE-START recovery tool using the same implementation.
It targets exact b{UUID} root/_Upper/_Lower folders bearing BridgeConstructionCost, and checks serialized
identity against the filename. It rewrites only the historical one-prototype policy. The native manual
gate contains a deferred CID/UnityGUID as encoded string data, not an Odin external reference.
After dependencies are registered, the runtime captures the independent rule and atomically replaces
only that gate string on disk. It never calls PrefabAsset.Load to force a premature dependency load.

The migration preserves prefab UUID, sidecar CID, construction cost, sections, geometry and save files.
Odin object identities and later reused array type declarations are preserved. Shared array references
or unrecognized policy shapes are refused, not guessed. All candidate files are checked before writes;
each replacement has a byte-preserving .bbunlockbackup and is atomic. A later IO failure can leave an
already migrated prefix of the batch: report counts, keep backups, retry idempotently. No asset deletion
is part of migration.

The serialized BridgeConstructionCost.OnEnable callback also installs the registration validator before
IMod.OnLoad. Owned invalid networks/sections/pieces are kept out of AddPrefab/UpdatePrefab; shared/native
prefabs are untouched. Before registration, old policies are converted to value-only gates in memory.
Even if native deserialization already turned the external reference into null, the persisted exact
owned file supplies the migrated gate. No PrefabAsset.Load or guessed replacement road is used.

Successful disk and memory migration no longer mandates another restart. A file locked during the
early callback can be retried at OnLoad without retaining a spurious failure latch. Incomplete access,
unrecoverable rule identity or unknown policies still block automatic deletion; they are not evidence
of irreparable geometry. Confirmed structural damage which survives recovery and validation follows
the existing PostTool retirement path: retire placed networks and owned children, observe native
completion on a later frame, then retire backing files. Never delete a live prefab index to repair it.
Map preload still cannot clear a real native-batch failure. No in-process repair can rewind a batch
which already failed before these hooks ran: in that case retain all assets and restart after recovery.
The offline tool is an optional fallback, not a prerequisite for ordinary mod-start migration.
Early-callback timing and actual native deletion still require real game acceptance.

## Long paths and IO

BridgeFileAccess uses extended Windows paths at IO boundaries, and ordinary full paths for ownership.
Existence probes use GetAttributes rather than File.Exists, which masks access errors. Only definite
file/directory-not-found results mean absence. Access denied, sharing violations and other IO errors
make audits incomplete; incomplete audits cannot retire any files. Reads, hashes, enumeration and
retirement moves use the same long-path handling.

## Commands (game must be closed)

From the repository:

```powershell
dotnet run --project tools/UnlockRepair -c Release -- "C:\Users\admin\AppData\LocalLow\Colossal Order\Cities Skylines II" --check
dotnet run --project tools/UnlockRepair -c Release -- "C:\Users\admin\AppData\LocalLow\Colossal Order\Cities Skylines II" --apply
```

The framework-dependent standalone build requires .NET 8 Runtime. Do not point it at the game
installation, save file, or an unrelated directory; supply the user-data directory containing
ImportedData. The tool refuses to run while Cities2 is running. It never launches the game.

## Verification and limits

- Release build against installed game assemblies succeeds.
- UnlockRepairCheck: 35 checks, including both complete Odin dumps from the supplied player's log,
  repeated migration, type-table reuse, long paths and sharing-violation retention.
- UnlockSnapshotCheck: 27 checks for fake native AND/OR buffers, donor independence, null-reference
  gate recovery from disk, idempotence, unavailable identities and startup file-lock retry/retention.
- DiskAuditCheck: 15 checks including paths over 300 characters, IO failure and retirement refusal.
- NetworkValidationCheck: 49 checks including required references and cleanup/restart latches.
- These are nonvisual offline checks, not proof of game startup recovery or every native mod rule.
- Human acceptance: restart; load a COPY of a test save; verify no unlock-resolution or
  NetInitializeSystem CRITICAL, both decks' locks before/after progression and the override, then
  save/restart and verify completed snapshots no longer require the prototype.
- This update did not load/save a city, push to GitHub or publish to Paradox Mods.
