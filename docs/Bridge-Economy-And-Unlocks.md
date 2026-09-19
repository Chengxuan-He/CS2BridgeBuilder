# Bridge base construction price and prototype unlocks

## Units and formula

All base values use the game's currency per 8 m billing unit, not a fixed fee per bridge.

```
offset = prototypeBridgeBase - 3 * (prototypeUpperRoadBase + prototypeLowerRoadBase)
generatedBase = offset + 3 * (selectedUpperRoadBase + selectedLowerRoadBase)
```

A single-deck bridge has no lower-road term. Main/auxiliary ownership is not a synonym for
upper/lower; swapping ownership roles does not change this sum. Each selected prototype variant
is priced separately, including the two decks of a double-deck prototype. The offset is signed,
never clamped. A negative final total or a total outside the native `int` range rejects creation
before saving, rather than wrapping to an enormous unsigned construction price.

`BridgeEconomy` evaluates actual section/piece requirements with the native
`NetCompositionHelpers.GetCompositionPieces` path. As in `NetInitializeSystem`'s default-price
pass, bridge pieces use Elevated composition and road pieces use default composition. Road
baselines exclude bridge overhead sections. Underground utilities are included in both.
Only active `PlaceableNetPiece.m_ConstructionCost` contributions are summed; alternative pieces
are not all charged together. Height surcharges, upkeep and separately placed object charges
remain native and are not part of this base formula.

The recorded Golden Gate example is 148 per 8 m for the bridge and 48 for its road deck, giving
an offset of 4 per 8 m. Another road costing 60 per 8 m gives 184 per 8 m before elevation and
separate object charges.

## Persistent implementation and ownership

Permanent generation makes private copies of the section graph and of pieces with nonzero base
construction charges. Only their construction charges are zeroed; height/upkeep, geometry asset
references, materials, lanes, requirements and placement fields are preserved. Source roads,
tracks and prototype prefabs are not edited. An auxiliary that is not a private clone causes
creation to stop before applying the price/unlock policies.

One zero-width, zero-height, geometry-free native piece on the main bridge carries the complete
base charge. Auxiliary decks have zero base construction charge to prevent charging either deck
twice. All pricing dependencies are saved and published with the bridge, using native prefab
types only; no custom component is needed to deserialize the asset. Previews do not allocate
these nonvisual pricing copies.

Existing bridge assets are not rewritten or deleted by this change. Newly generated assets use
the new formula and unlock policy. No mod cache or user save is modified by installation.

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

## Validation

`tools/EconomyCheck` tests the production arithmetic without constructing any geometry. Cases
cover single/double decks, negative offsets, ownership-role symmetry, zero, negative totals and
overflow. Build and locale/source-policy checks are loadability/static checks, not evidence of
in-game billing or unlock behavior. Human validation still needs a new single-deck bridge, a
new double-deck bridge and locked/unlocked prototype cases, including saving and reloading.
