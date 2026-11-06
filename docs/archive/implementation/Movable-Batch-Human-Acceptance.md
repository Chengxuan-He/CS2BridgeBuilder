# Movable bridge batch: human acceptance handoff

## Delivery boundary

On 2026-10-20 the user clarified that actual in-game batch generation is part of
human acceptance, not a prerequisite for entering it. The updated task completes
when every bridge enters that stage. This handoff does not claim visual acceptance,
successful native generation, cold loading or measured generation performance.

The nine additional styles below have generator routes, recorded source inputs,
render and network assembly paths, and localized catalogue entries. They now enter
human acceptance together. Draw01 retains its earlier visual acceptance and should
receive a shared-pipeline regression check.

| Style | Stage | Particular acceptance focus |
| --- | --- | --- |
| Draw01 | Previously accepted; regression | Paint motion, underside and grating |
| Draw02 | Human acceptance | Both leaves, fixed sidewalks and all LODs |
| Draw03 | Human acceptance | Detailed joints and far-level shoulders |
| Lift01 | Human acceptance | Far tower/core and end-lip supplements |
| Lift02 | Human acceptance | Conditional sections, track replacement and mechanism |
| Lift03 | Human acceptance | Independent upper/lower decks and far support supplement |
| Lift04 | Human acceptance | Moving frames, fixed bodies and converted road |
| Lift05 | Human acceptance | Moving shell, counterweights and far road boundary |
| PedestrianDraw01 | Human acceptance | Ropes, deck partitions and far boundary |
| PedestrianDraw02 | Human acceptance | Joined supports, two mechanism LODs and column LODs |

## Verified delivery

The final Release build succeeds. The built and installed BridgeBuilder.dll both
have SHA-256:

`5F281C32A49DD6C652B74DE5CCD562C8B8E4375B3BE540A71A5FA212B4526948`

Installation: the existing `pdx_mods/160320_17` cache directory. No replacement was
necessary because the installed binary already matches this build.

The delivery rerun passes RecordedVariantCheck (88 assertions),
Lift01SectionRolesCheck, Lift02SectionRolesCheck, Lift02StructureCheck,
Lift03StructureCheck, MovableSidewalkCheck (36 Draw02 and 24 Draw03 cases),
MovableMaterialSlotsCheck, MovableLaneDeformationCheck (612 vertices), and all
twelve languages in LocalizationCheck. Structure checks require the retained
research fixture directory; the lane deformation check requires GameManagedDir.
Initial invocations without those arguments were corrected and rerun successfully.

Earlier source-resource and structural checks, with their precise limitations,
are recorded in [implementation evidence](Movable-Bridges-Implementation.md).
They cover 167 reconstructed render descriptors, 302 direct native resources,
74 referenced native VT assets and 135 actual Surface payloads. Offline tests
do not establish engine rendering or persisted output completeness.

## Human acceptance procedure

For each style, create a fresh bridge through the existing UI and record the
selected road, upgrades, direction and layer configuration. Preserve healthy
existing bridges. Use ordinary measured width mapping; special narrow-road
adaptation is outside this batch. Non-movable bridges are unchanged by this handoff.

1. Check preview and actual creation separately. Record failures with the export
   report and selected style; verify the output is saved under its owner directory.
2. Inspect near, middle and far views in closed, opening, fully open and closing
   states. Observe running motion as well as pause, both sides and the underside.
3. Check road texture density, material slots, grating transparency, structural
   continuity, paint visibility and absence of backside paint. Pay particular
   attention to the style-specific items in the table.
4. Check sidewalks, shoulders and attachments against the selected road. Existing
   offline coverage is incomplete for Draw03 far joints and carried structures in
   the lift/pedestrian families; these remain explicit acceptance risks.
5. Exercise traffic stopping/restarting, shipping clearance, signals, lights,
   upgrades and Lift03's separate networks where applicable.
6. Save, restart and verify independent loading with required native/DLC resources
   available and the manager absent. Check generated references, VT and mid-mip
   resources, localized names, unlocks, icons and native fees.
7. Record cold/warm creation time, generated file sizes and complete dependency
   graph size, plus actual triangle/material counts per LOD. No performance or
   size-parity claim is established by the build.

Report each style as accepted or requiring repair after these observations.
Entering this stage is not a claim that the above checks have passed.

## Change scope and localization

This handoff adds documentation only. It changes no geometry, runtime code,
installed binary, saved bridge, migration behavior or non-movable bridge.
There are no new runtime strings; the twelve-language check passes.
