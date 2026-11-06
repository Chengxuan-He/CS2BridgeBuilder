# Native bridge pricing and prototype unlocks

This document describes the current implementation. Contract sections 16 and 19 govern native
pricing and independent persistence. Earlier pricing formulas and runtime price overrides are
obsolete; dated audit reports remain historical evidence, not implementation instructions.

## Native pricing

Preserve the selected networks' and bridge prototypes' native `PlaceableNetPiece` and
`PlaceableObject` construction, elevation and upkeep fees. The game selects composition pieces
and calculates charges for network length, elevation, auxiliary networks and placed objects.
There is no Bridge Builder multiplier, signed offset, elevated-price floor or fixed bridge total.

Do not zero auxiliary-network fees or create pricing-only copies. Private geometry-derived assets
retain their source fees. Neither native UI prices nor runtime composition prices are overridden
by a Bridge Builder pricing system.

`BridgeConstructionCost` remains readable for legacy conversion only. New generation and native
conversion remove this custom component before saving. Previously baked native fees are retained;
do not guess their original values or remove referenced dependencies. Restoring historical fees
requires authoritative source data and a separately verified migration.

## Native unlocking and persistence

The selected bridge prototype supplies the unlock policy. `BridgeUnlockSnapshot` captures its
requirements, and `BridgeNativeUnlock` persists the nested AND/OR expression using native
`Unlockable` components and bridge-owned native group assets. Generated main and owned auxiliary
networks receive those conditions. Do not flatten them into a current unlocked flag or require the
Bridge Builder assembly to evaluate them after a cold start.

Create-and-build and Manage/Build check readiness before activating the construction tool in game
mode. `BridgeUnlockPolicy.TryPrepareBuild` evaluates the persisted native rules and synchronizes
the generated networks' locked state. Missing or unavailable requirements are a readiness failure;
they must not silently unlock the bridge. Editor placement is outside this game-mode guard.

The optional manager setting to remove development restrictions operates while the mod is loaded;
the persisted native requirements remain the independent asset behavior. Required game/DLC content
and the documented double-deck rail-seam limitation remain separate compatibility concerns.

## Migration and deployment

Legacy conversion preserves bridge/deck UUID, CID and geometry, completes missing native payloads,
and uses the shared file layout. Successful conversion is idempotent. Failed migration follows
contract section 19's rollback and backup/removal rules; persistent changes require restart.

Deployment follows contract section 13. Preserve healthy bridges; recover only specifically
incompatible style/UUID-owned assets. A generation-code change alone does not require removal. Do not use an old
pricing implementation or a mod version change alone as a reason to clear assets.

## Verification

Relevant existing checks include `tools/CheckNativePricing.mjs`, `tools/UnlockSnapshotCheck`,
`tools/UnlockRepairCheck`, `tools/NativeLocaleCheck` and `tools/SelfCheckLifecycleCheck`.
These checks do not establish in-game billing or unlock behavior. Human acceptance covers
single- and double-deck placement, locked/unlocked requirements, save/load, and a cold start with
Bridge Builder disabled while required native content remains installed.
