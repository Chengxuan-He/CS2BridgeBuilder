# Bridge file cleanup regression

The 22:47–22:49 game log removed the empty-section test (04), but did not reject
12 or 14–17. The old post-load validator inspected network sections/archetypes;
it did not inspect persisted geometry references or files hidden by duplicate
prefab names/CIDs. 13 is a healthy control and must not be deleted.

## Change

`BridgeDiskAudit` indexes registered bridge UUID directories in ImportedData.
It checks root/deck identity against the UUID filename, duplicate prefab CIDs,
duplicate root/deck names, and unavailable serialized geometry references.
Geometry can resolve from the generated geometry directory or the game's asset
database (including shared/DLC/mod assets). Display names are not identity.
Read failures are inconclusive: they do not authorize deletion.

The existing post-load cleanup reconfirms evidence on separate engine frames,
collects both root and carried decks and waits for native network retirement.
The file path also captures collision losers absent from PrefabSystem. Conflicting
bridge UUID groups are retired together, not by an ambiguous AssetData/CID handle.
Surviving writable prefab references block retirement before topology is changed.

After native deletion, exact inspected prefab files and sidecars are hash-checked
and moved to ModsData/BridgeBuilder/RemovedBridgeFiles with `.bbremoved` suffixes.
Partial moves attempt rollback. Registry/export entries and menus are then retired.
Live prefab objects, native indices, geometry and materials are not destroyed or
unloaded. Geometry files remain available for the running session. Recovery copies
are not active prefabs and are not automatically restored.

This is not arbitrary mesh-corruption detection. The new geometry check covers a
missing referenced asset, not malformed vertex buffers or every possible render fault.

## Verification

- Release compiled against installed game assemblies.
- 14 disk checks passed: synthetic identity/geometry/ownership/recovery cases plus
  read-only checks on the preserved real laboratory's Healthy and Corrupt graphs.
- All seven healthy fixture graphs passed. Corrupt 12, 14, 15, 16, 17 were rejected;
  13 was retained. Empty-section 04 is handled by the existing network validator.
- Existing 19 network-validation and 23 instance-removal/confirmation checks passed.
- No native game run is claimed by these checks.

## Local deployment and next acceptance

The game was stopped. Installation/log/asset backups are in
`C:\Users\admin\Downloads\BridgeBuilder-disk-cleanup-fix-20260929`.
The required cleanup removed 102 imported directories, 124 geometry files and two
state files. Fourteen laboratory side-section files and 43 older bridge-owned
side-section pairs missed by the standard cleanup were moved to that backup.
No UUID prefab files remained before test restoration; shared export dependencies
were retained. No save file was edited.

Twelve payload files were installed into the existing PDX 160320_4 installation
and hash-verified. BridgeBuilder.dll SHA-256:
`9119F092B53E04E46413292CD8C065F679082159186C299057D7A97599126491`.
No GitHub or Paradox Mods publication was performed.

The same safe laboratory cases 04,12,13,14,15,16,17 were restored after deployment
(376 files, 40 private section/network references verified). The original lab ZIP
remains unchanged. Restoration record:
`C:\Users\admin\Downloads\BridgeBuilder-CorruptionLab-20260929\History\20260929-231305-5475-NonNullFaults`.

Human acceptance: start the game and load only the test save. Expect 04,12,14–17
to be retired and 13 retained. Check `Confirmed invalid bridge`,
`Retire invalid bridge files`, and `Missing bridge cleanup completed` in the log,
with no new NetInitializeSystem critical or rendering failure. Network retirement
must also be observed on a test map containing placed faulty bridge instances.
Do not overwrite an important save while validating this cleanup.
