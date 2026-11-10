# Native152 silent deletion and shutdown collection fix

Historical verification record, 10 November 2026. The deletion contract was updated before implementation.

Successful single deletion, successful bulk deletion, empty batches and pending normal-exit cleanup now produce no popup or status announcement. The existing pre-deletion confirmation remains. Genuine failure reporting remains unchanged, and startup tombstone processing stays silent. The single-owner completion call still refreshes state through the existing success-suppressing runtime-status API. The unused pending-exit, bulk-success and empty-batch translation entries were removed in all twelve languages. Internal verification, markers, ledger, map-first ordering and physical cleanup timing are unchanged.

The latest native151 shutdown log provided a separate concrete failure: its final Unity quit event did execute, but `World.All.All(...)` threw `NotSupportedException` because Unity's `NoAllocReadOnlyCollection` deliberately rejects interface enumeration. This is fixed using the native typed `foreach` enumerator, preserving the same live-world predicate. Test doubles now reproduce the native collection's interface rejection instead of disguising it as a normal List.

Validation:

- Release and frozen builds passed; existing unrelated nullable warnings remain.
- Sixty-four source-linked lifecycle checks passed, including silent success/empty batches, retained confirmation, genuine confirmation failure, silent startup and shutdown gates.
- The actual installed Unity.Entities collection was tested under the game's Mono: interface enumeration rejects; its public typed enumerator returns all elements. The actual Unity event/Harmony global-unpatch regression also passes. No game or Unity Player was launched; this is not live shutdown acceptance.
- Self-check lifecycle and all twelve language checks passed. `git diff --check` passed.

Native152 was installed with Cities2 absent. All 964 installed manifest files were checked; only DLL/PDB changed and their originals were backed up. DLL SHA256:

`E2D5A4AEFFBD9D4EAA2700714C8B30583243569869DDF2C767BC4CA92FD45A82`

Receipts and logs are in `artifacts/material-remap/shared/native152`. The official SDK ZIP is 77,171,451 bytes, still above the pre-existing 55 MB publication ceiling. Nothing was published or pushed.

The authorized tombstoned fixture remains for a user-run normal-exit test; no player bridge was manually removed. Its native151 failure retained the marker and original files as designed. Full physical cleanup with native152 remains to be verified in a real normal shutdown through `Logs/BridgeBuilder.Deletion.Shutdown.log`.
