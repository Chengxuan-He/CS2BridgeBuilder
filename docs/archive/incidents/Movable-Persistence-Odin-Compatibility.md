# Movable bridge persistence: native Odin value compatibility

## Failure and repair

On 2026-10-17 at 11:00, startup retired bridge `b70b2ccc3-3f9a-4370-ab3f-7945995b99ba` after layout migration could not parse its Mechanism RenderPrefab. The original serialized file is valid native Odin text. ProceduralAnimationProperties stores Unity Vector3 and Quaternion fields as typed objects containing unnamed numeric entries, such as `{ "$type": 9, 0, 6.62, 81.3 }`. The read-only text index incorrectly required every object entry to be a named JSON-style property. Consequently it rejected the animation skeleton before reading the complete Prefab, and startup scheduled removal of a valid saved bridge.

BridgeSerializedReferences now indexes unnamed entries inside typed objects as ordered values while preserving named fields, object/type IDs and external CID tokens. Quoted unnamed entries and nested values are supported as well. Malformed/truncated documents, duplicate named fields and duplicate object IDs remain failures. No error is suppressed and layout migration is not bypassed.

The native writer remains the persistence authority. It still registers the complete owned graph before saving explicit Prefabs as native text, writes Geometry separately, references existing built-in materials, and preserves unchanged external dependency snapshots. No animation metadata is flattened, stripped or converted into a custom format. The compatibility repair applies to startup layout, reference recovery, legacy-name migration and metadata readers sharing the index. It changes no generated geometry and requires no bridge cleanup.

## Verification and recovery

- Replayed all 71 real backed-up Prefabs, including all mechanism LODs and the unchanged external dependency snapshot, through the repaired parser.
- Reconstructed the complete saved graph in a temporary test directory and ran the real layout migration: success, zero relocations, all payloads and CID sidecars byte-identical.
- Layout regression checks passed, including typed anonymous values, truncated input rejection, idempotence, collision protection, missing sidecars and rollback after a locked-file failure.
- AssetMetadataCheck, LegacyNameCheck and DependencyCopyCheck passed. LegacyNameCheck emitted existing unassigned-field compiler warnings in its fixtures.
- Release build passed; all 12 deployed payload files matched the build. Installation backup: `artifacts/backups/movable-persistence-odin-20261017-110541/`.

Restored the user's original bridge from `C:/Users/admin/AppData/LocalLow/Colossal Order/BridgeBuilder Backups/20261017-030016-9011934` after checking for destination conflicts. Recovery copied only original files and retained all UUIDs/CIDs. All 202 restored files matched their backup SHA-256 hashes: 71 Prefabs, 18 Geometry files, 12 locale files and 101 CID sidecars. The backup remains intact. No asset was reconstructed or reserialized during recovery.

The user has confirmed current bridge appearance is normal. This increment does not change it. Actual game cold-start loading of the restored bridge remains to be confirmed after restart; filesystem and parser verification are not a substitute for that game check.
