# Draw01 attachment placement at motion gaps

## Current policy (2026-10-22)

The selected-road pole at the measured central opening gap is omitted, not moved.
This supersedes the previous nearest-streetlight-station workaround. That workaround
moved a TrainPowerPole01 from Z=0 to one of Draw01's lamp stations while the native
wire curves retained their original layout. A tie was resolved toward the first
station, also introducing longitudinal asymmetry. Lamp stations are not wire anchors.

Draw01RoadLights now relocates only actual streetlights through MovableRoadLights.
All other selected-road placements retain their original position and rotation.
MovableRoadObjects.Bind omits an overhead utility support only when its original
anchor lies in the supplied measured gap. UtilityObject's Catenary/LowVoltageLine
semantics identify supports, not prefab names. The common rule applies to movable
styles, using each caller's own partitions. Other unsupported furniture still fails
explicitly. Both sides use the same predicate; there is no nearest-station tie-break,
side-specific displacement, invented support or change to unrelated fixed bridges.

Native wire geometry and the retained support coordinates remain unchanged by this
repair. Do not shift wires toward the obsolete displaced poles. Electrical wires
must meet existing supports at their authored hanging points; preserving the original
layout removes the known displacement defect but is not a substitute for checking
those connections in game. Any remaining mismatch requires evidence from the native
wire curves and ObjectSubLanes anchors, not guessed heights or lamp positions.

## Verification and compatibility

Draw01AttachmentCheck covers the original central gap at three transverse positions
and three width deltas, supported native stations and their mirrored positions.
No geometry, native bones, LODs, material batches or persistence identities change.
New previews and newly created bridges use the updated attachment filter; already
saved bridges retain their serialized placements and are not rewritten or deleted.
Localization review: no user-visible strings are added or changed. Technical error
messages remain in the existing generation diagnostic path.

Manual acceptance remains: regenerate Double Train Track + Draw01, inspect the
opening seam and symmetric pole layout, check remaining wire/support contacts in
the closed pose and during motion, then repeat with tram tracks and cold loading.

## Moving-span end supports (2026-10-22)

Baseline: dev/aca314f1c6efea45716145d02046f82cad65227f, preserving existing local changes.
The selected track's overhead support rows now supply additional supports at both
outer ends of the moving span. This supplements original placements; it does not
relocate a support out of an opening gap. The common implementation intersects each
row with the style's recorded road partitions, selects the first and last moving
interval and uses equal inward clearance from the rotated native object bounds.
The full support, including its crossarm, stays inside the moving interval.

Preserve the selected native prefab, transverse position, elevation, rotation and
selection metadata. Do not invent a pole for a track without selected overhead
supports or mirror a one-sided track layout across its width. Duplicate endpoint
placements are suppressed. Separate elevations retain separate rows. Bind added
supports through the same original road-bone connection IDs as existing attachments.
There are no extra supports at the central opening gap. Existing gap supports remain
omitted instead of moved. This applies to previews and newly saved movable bridges;
existing persisted bridges require regeneration to acquire the new placements.

Draw01AttachmentCheck now also checks two symmetric, supported moving-span endpoints
across three width deltas and transverse rows, and excludes fixed-only domains.
Wire curves are not changed by this endpoint-placement addition. In-game inspection
must verify the actual wire/hanging-point contact and opening animation, including
railway and tram inputs and the two decks of Lift03. Compilation is not visual acceptance.
Localization review: no UI text, localization keys or generated display names change.
