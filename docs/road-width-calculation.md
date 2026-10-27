# Road width calculation

## Selected elevated composition

Measure the same selected road state that generation and preview use: Edge + Elevated,
the road's native edge-state conditions and the requested upgrades. The native zero-flags
default width is a diagnostic comparison only, not the elevated road-width input.

Read initialized NetGeometrySection, NetSubSection, NetSectionPiece and NetPieceData.
Resolve alternatives with NetCompositionHelpers before measuring. Sum actual selected lateral
component slots; overlapping render layers in one slot contribute their maximum selected width
once. This is not a maximum across alternative variants. Include selected medians, carriageways
and sidewalks; measure outward side extensions separately from the road-surface allocation.
Use the same selected data for left/right sidewalk presence, position and width.

Do not infer width from lane count, names, cached road totals or a universal sidewalk width.
Do not compress ground-mode geometry into the elevated width. Source RoadPrefab, TrackPrefab,
PathwayPrefab and available Road Builder entries use the same native-state measurement.
Unmeasurable external inputs fail the normal operation path; no old serialized-section sum or
native default total may silently replace the requested composition.

## Measured state distinction

The shipped Sidewalk With Parking 5 selects Sidewalk 3.5 for Elevated/Tunnel/Raised/Lowered
(requirements 11/12/13/14, checked against the native enum and serialized Prefab).
For the inspected Medium Road Divided, elevated surface width is
3.5 + 3 + 3 + 2 + 3 + 3 + 3.5 = 21 m; ground default is 24 m.
The inspected five-lane highway uses 2 + 5 * 4 + 2 = 24 m.
These are source examples, not hardcoded exceptions. Harbor and bicycle variants follow
their actual requirements; the parking sidewalk's bicycle-5 alternative excludes Elevated,
while its 3.5 m elevated alternative can itself select bicycle-3.5.

The retained factory log that reported Medium Road Divided as 24 m consistently passed that
value through composer and factory. It rules out corruption during parameter transfer, but
does not validate using ground state for an elevated bridge. A prototype width and selected
road width must describe matching boundaries and states before calculating their difference.

## Integration and verification

The relevant paths are NetWidth.cs, BridgeRoadWidthSystem.cs and DeckCatalog.cs. Keep their
selected-state inputs, surface/extension boundaries and sidewalk allocations consistent with
the actual native composition. This requirement does not authorize changing measured prototype
parameters or structural transforms to hide a road-width mismatch.

Report selected piece widths, offsets, flags, surface sum, side extensions and the native
default-width diagnostic separately. Verify zero, one and unequal-width sidewalks and different
carriageway/median alternatives. Compare the real prototype and a human-created bridge using
[the complete-span width audit](agent-contract/BRIDGE_GENERATION_CONTRACT.md#14-preserve-each-archetypes-width-allowance).
Compilation and the examples above do not establish all-road or visual acceptance.
