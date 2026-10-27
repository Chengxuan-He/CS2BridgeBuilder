# Lift01 periodic-page reuse evidence

## Verified source inputs

The immutable Lift01 mechanism placement records position `(0, 77.87999725341797, 0)`
and identity rotation. Its closed-world transform is a translation by `(0, 10, 0)`.
`MovableRoadPreparation` shares the same `SourceFrame` across its three LOD inputs,
sets `PrototypeRoad=false` for Lift01, and passes an empty paint-region array to
the production composer. These facts make the proposed exact original-road key
applicable without changing the selected road or its material.

The actual native `LiftBridge01Net` mesh archives were inspected independently:

| Native LOD | Horizontal upward faces with both UV gradients exactly independent of Y |
| --- | ---: |
| 0 | 1,728 |
| 1 | 1,088 |
| 2 | 192 |

There are 556 exact Float32 projection groups shared by at least two native LODs.
None of those groups differs in Y height. Moreover, the native Net LODs have
different source Surface identities. Consequently these source candidates do
**not** prove that any complete production page key matches across LODs, or that
discarding unused Y changes the page count in an actual Lift01 generation.

The numerical source scan and input hashes are preserved in
`artifacts/prepared-materials/Lift01/periodic-source-candidates.json`. They are
prototype source measurements, not a captured selected-road composition.

## Actual selected-road evidence boundary

The current `export-state.tsv` contains no Lift01 generated bridge. The available
game log only records Lift01 catalogue discovery, without its generated periodic
chart values. The available Lift01 chart audits concern prototype structural UV
plans. None contains `MovableStraightSample.CompileLod` output or the production
`Compose` chart/page records for a selected road. They cannot be substituted for
that input.

`CompileLod` evaluates native network geometry from `MovableNetworkContext`, and
`RoadToObject` is read from its generated mechanism entity. A standalone Unity
editor with native file readers does not provide that game's initialized prefab
system, terrain/theme/network state, material library and source asset databases.
The existing `SurfaceReuseCheck` invokes production `Compose` only for empty or
compatible-object bypass inputs. Its success would not validate periodic network
composition.

The independent environment can test the pure `Chart.NativePeriodicKey` by
reflection after a matching production assembly exists. That validates equality
semantics, not the existence of matching live Lift01 pages. Full production
composition requires a real selected-road capture or an initialized native game
environment. No fabricated input is labelled as a real generation here.

## Conservative key assessment and next evidence

The key retains source identity, full source frame, chart axes, dimensions,
density, complete patch projections and the actual sampling origin including
guardband. Removing an origin component only when both patch gradients and both
worldspace coordinate rows have an exact zero coefficient is conservative.
It does not require phase wrapping or quantization. An actual draw must retain
its original origin; only the identity key may normalize the unused coordinate.

To measure its actual benefit, capture the pre-sharing periodic charts from one
real Lift01 invocation: selected road identity, LOD, source CID, `RoadSource`,
frame, axes, bounds, dimensions, density, complete patch, actual sampling origin
and resolved source values. Compare full old/new key groups within that invocation.
Source changes, phase differences and different dimensions must remain distinct.
No predicted percentage or saved page count follows from the existing evidence.

No public code, geometry, material, scene, baked resource or localization text was
changed during this read-only audit.
