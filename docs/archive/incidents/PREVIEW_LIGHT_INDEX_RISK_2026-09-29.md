# Preview mixed-light indexing risk

Pre-change checkpoint `4b3a1290c0cdec486b74a575652b57ccef753d96` was pushed to
`origin/dev` before this change. This is not a Paradox Mods release.

## Evidence and limits

The player's available September 28 report contains 482 repeated
`HDRenderPipeline.PreprocessVisibleLights` IndexOutOfRangeException entries.
It loads Bridge Builder 26.9.28 on Microsoft Store game build
`1.6.2f1 (768.21d1) [6300.27778]`. The second supplied ZIP has the same SHA-256;
neither is a new report from after the player replaced the bridges.

Local Steam game build is `1.6.2f1 (767.21d1) [6300.26419]`. Its HDRP MVID is
`6c16b39f-81e6-4aab-9ae3-bcc6a0e602ac`, unlike the player's
`35b7c672-977c-44f8-a336-961637edc89e`. Local IL offsets must not be presented as
the exact offending instruction in the player's binary.

Inspection of the installed HDRP reveals a concrete ordering hazard:

- `HDRPDotsInputs.FillVisibleLights` appends ECS lights after Unity lights.
- `HDGpuLightsBuilder.PackLightSortKey` sorts GPU light type before source index;
  Point precedes ProjectorBox.
- `BuildVisibleLightEntitiesForDots` gives ECS lights a data index offset by
  `s_NumUnityLightData`.
- `PreprocessVisibleLights` takes the first
  `min(sortedLightCounts, s_NumUnityLights)` sorted entries and indexes
  `HDLightRenderDatabase.hdAdditionalLightData` without checking their source.

The old preview used one Point key and seven ProjectorBox fills. A city ECS Point
can therefore sort into that prefix ahead of the fills; its data index belongs
to a different data set. Disabling fill shadows does not prevent this lookup.
Scene and light-layer isolation does not change this mixed-list sorting rule.

## Initial September 29 mitigation (superseded below)

All eight private preview lights now use Point, placing Unity preview points
before appended ECS points of the same category/type/volume in the sort key.
This removes the ProjectorBox ordering hazard introduced by the preview; it is
not a general engine fix for every invalid light index, rejected light, other
mod's light type, or camera lifecycle issue.

Key direction, distance, illuminance and shadows are unchanged. Fills retain
their directions and centre illuminance, using the same distant-point setup as
the key (`candela = lux * distance squared`). Only the key has shadows/specular;
fill range and fade cover the entire preview. Point fills approximate parallel
box illumination, so appearance must be checked in game. No scene bridge lights,
materials, mesh generation, automatic cleanup or global renderer state is changed.
The existing four-sample supersampling and stable light lifecycle are retained.

Start/finish/cancellation logs include preview identity, frame and sample count;
start also records the actual loaded HDRP MVID. Readback failures retain their
exception type/message. No per-frame logging or global exception suppression is
introduced.

## Required acceptance

Build and deployment checks cannot reproduce this player's failure. A human must
open, switch and close creation/management previews repeatedly on an existing
city with street lights, in daytime and at night. Check thin cables, diffuse fill,
key shadows and the unchanged city view. Correlate preview log markers with any
new `PreprocessVisibleLights`, shadow or render-pipeline errors. A fresh player
log and, if offsets are needed, their matching HDRP assembly are still necessary
to confirm the reported exception's root cause.

## Local verification and deployment

Release compilation and `git diff --check` passed. No visual unit tests were run.
The compiled stage uses Point lights exclusively and no longer configures a box
spot shape. In-game acceptance remains pending; the agent did not open the game.

`Cities2.exe` was stopped and verified absent. The active PDX cache installation
and generated assets were backed up under
`C:\Users\admin\Downloads\BridgeBuilder-preview-light-index-20260929`.
Ownership-scoped cleanup removed 18 imported directories, 20 geometry files and
2 state files, preserving 9 RoadPrefabExporter dependency directories. The cleanup
dry run then found no remaining targets. No save file or corruption-lab backup
was modified.

Twelve payload files were deployed to the active `160320_4` cache installation
and verified against build-output SHA-256 hashes. Installed BridgeBuilder.dll:
`53B4EAB0E47DB0930864CC9F3F150AE7B5B1D24E8D6FF3993D5EDD175C4258EB`.
No duplicate local Mods/BridgeBuilder installation was created. The pre-change
checkpoint is on GitHub dev; this follow-up fix is local and has not been
published to Paradox Mods.

## September 30: directional fill and isolated environment

The requested follow-up replaces all seven diffuse fills with shadowless
Directional lights, using Lux directly. Their illumination no longer varies with
distance across the bridge. The Point key retains its position, illuminance,
specular response and punctual shadows. Directional fills request no sun cascade
shadows. Directional sorts before Point in the inspected HDRP; ProjectorBox fills
remain absent. This is still a narrow mitigation, not proof of the player's cause.

The private camera now explicitly disables sky reflections, reflection/planar
probes, probe volumes, SSGI, ray tracing and volumetrics (in addition to the existing
disabled exposure control, SSR and atmospheric effects). Its volume anchor is
explicitly its own transform. The distant local volume overrides VisualEnvironment
to no sky/clouds and Dynamic ambient mode, avoiding the static baked sky path.
IndirectLightingController diffuse and reflection multipliers are zero. Constant
white directional fills supply a neutral diffuse environment; fixed EV100 conversion
is retained. No city volume profile, sun or global RenderSettings is modified.

Release compilation and diff whitespace validation passed. Existing supersampling
and transparent background handling are unchanged. Human acceptance must regenerate
the same bridge preview by reopening the panel in daytime and at night, check
matching illumination and unchanged city lighting, and check logs for HDRP errors.
No in-game visual verification has been performed by the agent.

Local deployment: Cities2.exe was stopped and verified absent; the previous
installation and generated state were backed up to
`C:\Users\admin\Downloads\BridgeBuilder-preview-directional-20260930`.
Ownership-scoped cleanup removed one export-state file, no imported bridge or
geometry files, and preserved nine RoadPrefabExporter dependency directories.
No save files were modified. All twelve payload hashes match the Release output
in the active `160320_4` cache; no duplicate local installation was created.
Installed BridgeBuilder.dll SHA-256:
`491405848CEFC9A0E1D73B8783E55DA606A4A197F01C2544EBD8D1BA6462730A`.
No GitHub push or Paradox Mods publication was performed for this follow-up.
