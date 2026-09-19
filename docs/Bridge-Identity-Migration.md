# Permanent bridge identities

Permanent BridgeBuilder prefabs use `b{uuid}` exclusively; temporary previews keep
`tmp{uuid}`. Display names remain independent. There is no runtime `r{uuid}` alias
or legacy registry acceptance. Road Builder source-road identifiers are unchanged.

## One-time local migration (2026-09-19)

The user authorized renaming existing bridges and removing compatibility, then
authorized backing up and migrating the affected saves. One registered bridge was
found: `rf7b81751-b516-46a1-8395-70b35eed8096` (GoldenGate). It is now
`bf7b81751-b516-46a1-8395-70b35eed8096`.

`tools/MigrateBridgeIdentity.py` is an explicit offline maintenance tool, not a
runtime migration hook. It defaults to dry-run, requires the exact old identity,
checks ownership and destination collisions, and refuses to run with Cities2 open.
Before applying, it backs up every affected file and stages validated output with
a before/after SHA-256 manifest. It handles Windows extended-length asset paths.

The completed migration covers:

- 19 prefab files, renamed with their folders and internal generated names.
- 10 geometry files, renamed only; geometry bytes are identical.
- 29 asset CID sidecars, renamed only; every asset ID is unchanged.
- Registry and export-state rows; registration label and source-road ID unchanged.
- One affected save and its unchanged CID sidecar (62 files in the manifest).

The installed game's `PrefabID` compares type, name AND hash. Preserving asset
GUIDs alone is insufficient. `ReadSystem` stores raw metadata followed by ZStd
buffers with uncompressed/compressed size headers. The affected local save
`Saves/76561199197854251/19-九月-15-39-50.cok` contains exactly one old name in its
prefab identity records. Its decompressed data changes by exactly one byte, `r`
to `b`; all GUIDs and simulation data remain identical. Unaffected compressed
buffers are copied byte-for-byte. The ZIP update preserves raw filenames, flags,
timestamps and other entries, updating only sizes, offsets and CRCs as necessary.
All entries pass CRC verification, and all decompressed buffers are compared with
their original content before writing.

Twenty local saves were inspected. Eighteen other readable saves have no old-name
reference. `玉兰.cok` has a pre-existing CRC error and predates this bridge
(2025-08-12 versus 2026-09-19); it was left untouched. No other saves, source roads
or mod caches were altered.

Backup root:
`C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/bridge-r-to-b-migration-20260919-final`
contains `original`, `staged`, `manifest.json`, and the `COMPLETE` marker.
Restore, if needed, with the game closed: restore every manifest source from
`original`, remove only the corresponding renamed destinations, and reinstall the
previous DLL that accepts the old prefix. Do not restore just the registry or just
the save because those identities must remain consistent.

Offline byte/CRC validation and Release compilation do not constitute an in-game
load test. A human must restart and load the migrated save to confirm gameplay.
