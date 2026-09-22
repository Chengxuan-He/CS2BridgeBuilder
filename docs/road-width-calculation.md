# Road width calculation correction

## Latest correction (2026-09-21)

### Elevated-state correction (supersedes the zero-flags choice below)

The measured input and rendered deck were in different states. Measurement used
ground/default flags while preview renders Edge + Elevated. The shipped
`Sidewalk With Parking 5` selects `Sidewalk 3.5` for Elevated/Tunnel/Raised/Lowered
(requirements 11/12/13/14, verified against the game's enum and serialized prefab).
Thus Medium Road Divided's elevated surface is 3.5+3+3+2+3+3+3.5=21 m,
although its ground default is 24 m. The five-lane highway uses 2+5*4+2=24 m.

Measurement now selects Edge + Elevated, evaluates the road's own edge-state
conditions as preview composition does, and sums selected component slots.
Sidewalk geometry and widths are never changed or widened. Harbor/bicycle variants
are resolved by their authored requirements, not a universal 3.5 m override or
a name-based exception. No claim is made that every bicycle variant retains width:
the inspected parking sidewalk's bicycle-5 subsection excludes Elevated, while its
3.5 m elevated subsection can itself select a bicycle-3.5 subsection.
Runtime confirmation of the corrected build remains pending.

The returned width is now an explicit sum of actual selected component widths,
read from initialized `NetPieceData`. Each selected lateral slot contributes once;
overlapping render layers in that slot share its maximum selected width. This is
not a maximum over alternative variants: native requirement selection happens first.
The same contributions feed sidewalk allocations. Native composition totals remain
diagnostics only and are not returned or used as fallback values.

Runtime evidence at 03:08:05 on 2026-09-21 confirms `Medium Road Divided` reached
the CableStayed factory as 24 m (composer measured/structure/towerArgument=24,
factory targetDeck=24). Its constituents are 5+3+3+2+3+3+5=24 m. The factory used
an authored road reference of 32 m and a -8 m transformation. This excludes a
24 m value becoming larger during parameter transfer, but does not establish that
the prototype datum or resulting structure is correct. The new explicit-sum build
still requires runtime verification; no visual acceptance is claimed.

Source asset measurement now uses zero composition flags, matching the inspected
`NetInitializeSystem.InitializeNetPrefabsJob` default-width calculation, rather
than inventing a placed Edge state and applying edge-state transitions. Native
per-piece selection remains in use; surface and extension boundaries stay separate.
Reports include `NetGeometryData.m_DefaultWidth` as an independent initialized
total for comparison with the selected surface-only sum. Four-Lane Divided Road
must measure 24 m per user acceptance data; this is not hardcoded into the reader.
Older sections below describe earlier iterations and their then-current validation gaps.

## Scope

Implemented on `dev`. `NetWidth.cs`, the source-road reader `BridgeRoadWidthSystem.cs`,
and the Road Builder width input in `DeckCatalog.cs` are the affected paths. Bridge generation, archetype parameters, geometry transforms,
and LOD generation are unchanged.

## Cause

The old road measurement summed serialized sections, skipped median sections in
`NetWidth.Of(NetGeometryPrefab)`, and took the maximum candidate piece width in each
section without evaluating its requirements. `RoadSurfaceOf` used a different sum.
Neither represented the actual selected composition. Width variants such as 3 m and
4 m carriageways cannot be resolved by choosing the largest candidate. The same
problem applies to sidewalk, median, and other section variants.

The inspected game `NetInitializeSystem.InitializeNetPrefabsJob.Execute` calls
`NetCompositionHelpers.GetCompositionPieces` with default composition flags, then
`CalculateCompositionData`, and stores `compositionData.m_Width`. Its `AddSections`
preserves medians; it does not discard them as non-width-bearing decoration.

## Correction

The previous disposable adapter used the zero-flags pricing composition, not an
actual edge composition. Tracks and pathways also retained the old maximum-variant
path. The new reader covers source RoadPrefab, TrackPrefab and PathwayPrefab inputs
(not bridge archetypes). It reads initialized native NetGeometrySection,
NetSubSection, NetSectionPiece and NetPieceData, starts with the Edge flag, applies
native edge states, selects pieces with GetCompositionPieces and packs them with
CalculateCompositionData. Each selected piece supplies its actual initialized width,
including carriageways, sidewalks and medians. No fixed lane width,
name-based width, cached road total, or maximum across alternative widths is used.
Road Builder catalog entries also use this measurement rather than configuration width.
When a breakdown is requested it includes each selected piece's width, offset and flags.
Unmeasurable source roads return zero instead of falling back to the old section sum.

## Verification and limitations

### Follow-up: boundary mismatch and remaining maximum-variant path

The first native-reader revision included terrain-blending side sections in the
road-surface total, although callers treat those as separate outward extensions.
It also left `RoadEdgesOf` using the maximum serialized candidate width. This
made the total and sidewalk boundaries inconsistent and allowed some callers to
count extensions twice. Both paths now use the same selected native piece data:
surface measurement excludes the existing `SectionNames.IsSide` classification,
extensions are measured separately, and sidewalk allocations use native selection
and packing for each active section, not all serialized alternatives. Bridge mesh
deformation and prototype parameters are unchanged.

This still measures the default Edge composition, not an arbitrary placed edge's
upgrades or node state. No real before/after sample currently verifies all native
roads. The existing section-name classification is retained, not replaced by an
unmeasured geometric heuristic. Build success does not close that validation gap.

- Release build passed on 2026-09-21.
- No visual-generation unit tests were executed.
- Game stopped; owned generated artifacts were backed up and cleaned per contract.
- Latest cleanup record: `C:\Users\admin\Downloads\BridgeBuilder-native-width-20260921`
  (no generated assets remained to remove).
- The reported Four-Lane Divided Road's before/after measured widths have not been
  captured in a real game run. No numerical result or visual acceptance is claimed.
- Human verification: select Four-Lane Divided Road, generate a fresh bridge, and
  check road/structure alignment in near and far views. Retain the export breakdown.
  Repeat with roads using different carriageway, sidewalk and median widths.
