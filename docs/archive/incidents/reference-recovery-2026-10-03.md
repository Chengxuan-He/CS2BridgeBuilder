# Lane-reference startup recovery

Scope: local dev changes only; no remote push or Paradox Mods publication.

## Native evidence and behavior

Game.dll NetInitializeSystem directly calls GetEntity on SecondaryLane left/right/crossing entries,
AuxiliaryLanes entries and NetLaneGeometryPrefab mesh entries. These were absent from the registration
validator. SecondaryLane arrays may be null natively; AuxiliaryLanes arrays may not. Although the
NetInitialize mesh loop accepts a null array, NetLaneGeometryPrefab.GetDependencies does not.

The validator now covers these paths recursively, and the early registration guard covers owned
NetLanePrefab assets as well as owned networks, sections and pieces. It does not intercept unrelated
mods' assets or modify shared/native assets.

At the title boundary, before re-registration, recovery reads the original owned Odin document and
uses its exact component/field/array index and CID or UnityGUID. It resolves only already-loaded
instances; it never forces recursive asset loading or substitutes a similarly named road. Descriptor
and array copies prevent mutation of shared source entries. Registered prefab graphs are not edited.

Outcomes:

- Exact target loaded: restore the reference, validate and register normally.
- Known asset not loaded, unavailable mod/DLC, unsupported serialization, ambiguous identity or IO
  failure: retain/quarantine, report and require dependencies/restart. An unresolved CID alone is not
  proof that its asset is permanently gone. Deferred bridge IDs cannot authorize save-network cleanup.
- Proven serialized null or a known user-asset file definitely absent: remove invalid secondary/
  auxiliary lane or lane-mesh entries where the native empty-list form is valid. Preserve original
  files, applying recovery again on startup. Do not persist a guessed replacement or rewrite Odin IDs.
- Unrecoverable essential road section, piece, primary lane, bridge object or carried deck: reject the
  incomplete graph and use the existing scoped file-retirement/placed-network deletion pipeline.
  Do not silently create a partial bridge by dropping its essential structure.

Deferred edits do not compact arrays: later recovery must still address the original serialized
index. Whole-bridge file retirement now produces a localized title notification (12 languages),
distinct from the existing placed-network deletion notification after city loading. Save operations
remain unrestricted; city cleanup remains one attempt per load.

## Verification and limits

79 reference/recovery/latch checks passed, including a real old Odin document, late-resolved CID,
UnityGUID, secondary lane nulls, descriptor copying, shared/registered retention, definite missing
user files, essential-section rejection and deferred-index stability. Instance-removal source/stub
regressions and 12-language UI checks passed. Release compilation succeeded against installed Game.dll.

These are offline/nonvisual checks, not proof that this player's incident is fixed. No new player log
or failing asset bundle was supplied. Real old-save startup, repaired lane connectivity, retirement
notifications and save/restart behavior require human game acceptance using a copy of a save and its
matching ImportedData assets. Do not overwrite the original save during testing.

## Local test deployment

Official ModPostProcessor completed successfully. With Cities2 stopped, 12 payload files were
installed into the existing subscribed `pdx_mods/160320_6` cache and each SHA-256 was checked against
the postprocessed staging copy. Subscription metadata was preserved; only one installed
BridgeBuilder.dll was found. This is a local test override, not an online release or version bump.

Installed DLL SHA-256: `7AE48BF86D7B51A4415950ECEFA9C4E99237607B0F7132BCA84AD7576688D9EE`.

Published payload backup: `C:/Users/admin/Downloads/BridgeBuilder-reference-recovery-20261003/Published-26.10.2-before`.
The required generated-asset cleanup reported zero removed directories/files/icons/state and
preserved nine export dependencies. No save was modified and the game was not launched.
