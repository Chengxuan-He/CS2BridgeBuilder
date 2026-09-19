# Bridge base construction price and prototype unlocks

## Units and formula

All base values use the game's currency per 8 m billing unit, not a fixed fee per bridge.

```
offset = prototypeBridgeBase - 3 * (prototypeUpperRoadBase + prototypeLowerRoadBase)
formulaBase = offset + 3 * (selectedUpperRoadBase + selectedLowerRoadBase)
elevatedFloor = selectedUpperElevatedBase + selectedLowerElevatedBase
generatedBase = max(formulaBase, elevatedFloor)
```

A single-deck bridge has no lower-road term. Main/auxiliary ownership is not a synonym for
upper/lower; swapping ownership roles does not change this sum. Each selected prototype variant
is priced separately, including the two decks of a double-deck prototype. The offset is signed,
never clamped. The final total is raised to the elevated-road floor (including when the formula
is negative). Invalid inputs and totals outside the native `int` range reject creation before saving.

Prototype offsets retain the verified section/piece calculation: Elevated composition for the
prototype bridge, default composition without overhead for its road decks. Selected road prices
instead come from initialized `PlaceableNetData.m_DefaultConstructionCost`, the scalar used by
the native UI, not a reconstruction from serialized Road Builder pieces. The elevated floor is
calculated from initialized native section/piece entities using `GetCompositionPieces` and
`CalculatePlaceableData`. Height surcharges, upkeep and separately placed object charges remain
native and are not part of this base formula. The floor concerns base price, not a fixed-height quote.

The recorded Golden Gate example is 148 per 8 m for the bridge and 48 for its road deck, giving
an offset of 4 per 8 m. Another road costing 60 per 8 m gives 184 per 8 m before elevation and
separate object charges.

## Persistent implementation and ownership

Pricing prefabs are prohibited, including cost-only pieces/sections and renamed equivalents.
`BridgeConstructionCost.m_BaseConstructionCost` is a scalar component on the existing bridge
prefab, with no referenced asset or dependency. The main bridge owns the entire base price;
its privately owned auxiliary deck stores zero, preventing a second charge for the lower deck.
Source roads, tracks, sections, pieces and prototype prefabs are not modified for pricing.

After native initialization `BridgePriceSystem` replaces `PlaceableNetData`'s default price;
after native composition calculation it replaces `PlaceableNetComposition`'s construction base.
The first controls the displayed price, the second actual placement. Both are assignments, not
additions, so repeated updates cannot accumulate a fee. Height and upkeep fields remain untouched.
The scalar is serialized with the bridge and restored on reload. This requires BridgeBuilder
to be loaded, but does not introduce a pricing prefab or a dependency on another mod.

Old generated assets and pricing dependencies must be ownership-scoped, backed up and removed
before deploying this change; they are not safely migrated by merely changing the displayed price.
Cleanup must preserve user saves, original roads and other mods' assets/caches.

## Unlock policy (latest user requirement)

Every style inherits its selected prototype's unlock behavior, not the selected road's behavior.
The generated main and auxiliary networks receive `Unlockable` with
`m_IgnoreDependencies = true`, `m_RequireAll = [prototype]`, and an empty `m_RequireAny`.
The native dependency graph thereby retains the prototype's AND/OR, manual, milestone,
development and indirect requirements without flattening them or hardcoding current unlock state.
A prototype without any unlock requirements leaves the generated bridge unrestricted.

For Grand Bridge, this reaches the prototype's `GrandBridgeNode` requirement naturally. There
is no Grand-only exception: the user superseded that earlier request with prototype unlocking
for all bridges. DLC/mod availability checks remain unchanged.

Both Create-and-build and Manage/Build check the native locked state in game mode before
activating the tool. A locked bridge may be created, but not built; localized messages explain
that the original bridge must first be unlocked. Editor placement remains unrestricted by this
game-mode-only guard.

Publication runs in PrefabUpdate while the native UnlockSystem runs in MainLoop. Before either
build action, `BridgeUnlockPolicy.TryPrepareBuild` checks the original prototype entity rather
than trusting the generated bridge's initial Locked marker. Only when that prototype is unlocked
does it disable the stale marker on the generated main/auxiliary networks and emit the native
Unlock notifications. All affected networks are resolved before mutation; an unavailable entity
is a build-readiness failure, not a false "prototype locked" dialog. No native system is updated
recursively, no prototype is unlocked by the mod, and the persistent dependency rule is unchanged.

## Validation

`tools/EconomyCheck` tests the production arithmetic without constructing any geometry. Cases
cover single/double decks, negative offsets, ownership-role symmetry, elevated floors and
overflow. Blue suspension regression: offset 36/8m plus 3 times 12/8m is 72/8m, or 9000/km,
for a 1500/km road (unless its elevated floor is higher). Build checks are not evidence of
in-game billing or unlock behavior. Human validation still needs a new single-deck bridge, a
new double-deck bridge and locked/unlocked prototype cases, including saving and reloading.
