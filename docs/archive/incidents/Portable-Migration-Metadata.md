# Portable migration metadata preservation

## Observed failure

The owner's UUID is `b1f019d4e-faea-4a80-b98f-e71531d89df1`. The observed sequence is two successive migrations, not evidence of an unlimited loop:

- The 00:05 local-time backup contains the complete player name, selected-road identity, `GOViaduct` style and creation timestamp. Its native Unlockable has `m_IgnoreDependencies = false`.
- The 00:09 backup shows the completed unlock migration, but the UIObject name has become `UIObject`. The original metadata was lost during the earlier root clone.
- Comparing that backup with the next saved root shows only UIObject.name changed to fallback metadata. Both dependency saves report zero written files. No icon, pack or geometry difference explains the second migration.

Native `PrefabBase.Clone` invokes `AddComponentFrom` for each component. `AddComponent` initializes its name from the component type, and `AddComponentFrom` uses JsonUtility field copying. The observed serialized output confirms the metadata-bearing name was not retained. Native source evidence is in `artifacts/portable-metadata/native/PrefabBase.cs` (Clone and AddComponentFrom/AddComponent).

## Repair

`BridgePortableMigration` now assigns persisted metadata on every cloned root, regardless of whether unlocks, icon, pack, cleanup or metadata itself caused the migration. Complete roots that require no migration remain untouched. No additional runtime validation or backup-search framework was introduced.

The affected owner's current Prefab and CID were backed up under `backups/portable-metadata/owner/<UUID>/`. Only its existing UIObject.name scalar was restored from the complete 00:05 backup. All other root bytes and its CID are unchanged; no geometry, dependency, unlock or locale files were changed. The exact source path, hashes and recovered fields are recorded in `artifacts/portable-metadata/owner-repair.json`.

## Verification

- `tools/PortableMigrationCheck` compiles the actual production migration method and metadata parser. Its native API doubles model the evidenced component-name reset. All 39 assertions pass: unlock/icon/pack/cleanup clone preservation, missing-metadata completion, reload followed by a no-write second run, stable CID and original-file backups. This is an offline native-boundary fixture, not a Unity Player execution.
- `tools/NativeLocaleCheck` exercises actual missing-only description logic across all twelve locales, including no writes after complete data, one affected locale write for missing fields, and an unchanged subsequent pass. No evidence implicates repeated description generation. Its obsolete internal Unknown-mapping rejection assertion was aligned with the trusted-execution policy; unavailable localization-manager handling remains tested.
- `tools/AssetMetadataCheck` passes metadata round trips, file-failure preservation and partial-field canonicalization, including a read-only round trip of the repaired actual root. Its deferred-removal service double was brought up to date.
- All twelve localization tables pass. Production Build.ps1 passes, including prepared material resource verification. Existing nullable/non-exhaustive-switch warnings remain.
- With Cities2 absent, the build was installed in the active `160320_19` package. Original installed files were backed up under `backups/portable-metadata/installed/160320_19`. Version remains `26.11.12a`. Deployment hashes and package-resolution evidence are in `artifacts/portable-metadata/deployment.json`.

No game was launched or stopped. The next player-run cold load and a second startup without another migration remain live acceptance checks. No healthy bridge was removed.
