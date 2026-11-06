# Fixed Platform Appearance and Movable Generation Cost

## Observed sample

The user confirmed the bridge preview is correct, but reported slow generation and missing end
platform tops. Inspected the persisted `Large Road Divided / Draw01` sample
`be0359a68-19df-426e-a991-3f23ea1e0d74`, its three mechanism Geometry files, native geometry,
original atlas alpha, generation log and export report. This report does not claim that a new
sample has already passed visual acceptance.

## Platform fault

The generated end-walkway geometry exists. At each LOD, 36 inspected outer fixed-top vertices
at `y ~= 10`, `abs(x) > 12.5`, `abs(z) >= 78.150390625` use UVs between
`(0.32470703125, 0.6611328125)` and `(0.326416015625, 0.74365234375)`: inside the native grating
patch. Those coordinate filters were offline inspection only, not runtime part classifiers.

The material partition introduced in the previous increment considered transverse position only.
It therefore applied the moving walkway's transparent grating to the fixed end platforms too.
The original grating patch contains alpha values 0 through 255. This explains the apparent missing
platform top without requiring missing structural triangles or a depth correction.

Native LOD0 faces 4865/4866 identify the fixed paving patch with UV bounds
`(0.486083984375, 0.01238250732421875)` to `(0.67529296875, 0.2734375)`.
The inspected interior of that patch has alpha 254 through 255. Native fixed sidewalk top geometry
starts at `z = +/-78.150390625`; the generator already records that motion seam.

The material partition now uses those measured longitudinal boundaries: fixed platforms select
opaque paving, moving walkways select grating, and carriageway selects asphalt. All three generated
road LODs use the same highest-detail material-region definition. Existing native materials are
reused. Selected-road geometry, structural widening and original Z coordinates are retained.

## Measured costs before this change

From the 2026-10-17 01:26–01:27 game log (Asia/Shanghai):

| Recorded stage | Measurement |
| --- | --- |
| Small-road preview markings | 49 ms; 66 objects |
| Large divided-road preview markings | 194 ms; 192 objects |
| Large divided-road formal markings | 344 ms; 192 objects |
| External dependency persistence | 91 ms; one copy, two written files |
| Formal prefab publication report | 523 saved/registered Prefabs |
| First observed preview render | 11 frames; about 1.54 s |
| Second observed preview render | 11 frames; about 1.33 s |

Texture baking and generated material counts were zero. The previous approximately 17.5-second
texture upload/save stage is absent. Existing logs do not time the entire sampling/generation path,
so these figures cannot establish the largest remaining stage or the full click-to-completion time.
The first compute-loader log also follows marking completion by about 1.7 seconds; that interval
contains uninstrumented assembly work and cannot be attributed wholly to GPU initialization.

## Implemented reduction

Previously every projected stroke created an object Prefab, render Prefab and geometry payload.
Now independently clipped stroke volumes are combined by native bone, paint color, renderer
priority and infoview setting. Each group creates one object/render/geometry set. No geometry
connects the empty spaces between strokes. Different bones and different render settings remain
separate. Native materials, clipping coverage and motion ownership are preserved; this requires
no texture baking or material generation.

All 192 marking renders in the inspected saved sample use white paint, priority zero and no
infoview color. Their attachments use the fixed domain and the two native moving connections.
Those settings imply three groups under the new implementation, instead of 192. Expected saving:
378 fewer object/render Prefabs and 189 fewer geometry assets for the same marking coverage.
This is a grouping calculation from the existing sample, not a measurement of new output.

Clipping remains CPU work and native geometry allocation, saving and registration remain ordered
through their existing thread-affine paths. This increment does not claim a completed GPU migration
of geometric clipping. Reducing redundant objects addresses the observed allocation/persistence
fan-out without attempting unsafe parallel Unity API calls.

Added timings for private-road sampling, road/material/structural mechanism assembly, remaining
supports/objects/network assembly, marking clipping versus allocation/save, prefab serialization,
and elapsed time through native publication. The next real creation can identify the remaining
largest stage without inferring it from a single marking timer.

## Verification and deployment

- Original and persisted mechanism attribute streams were decoded at all three LODs.
- Original fixed-platform UVs and atlas alpha were inspected directly.
- Shared marking callers, bone attachments, preview capture, owned-asset saving and native
  publication were reviewed. Structural-only render callers remain unchanged.
- Draw01 branch commit: `766c673`, integrated into the dev workspace by exact patch.
- Release build passed; all 12 installed payloads matched build hashes.
- Final installation backup: `artifacts/backups/fixed-platform-and-marking-timing-20261017-014119`;
  18 original files verified, no added backup documentation.
- No visual-generation unit tests or game automation were run.
- Human near/far inspection of a newly created bridge, opening animation, cold load, new timings
  and full dependency-size comparison remain pending. Preview correctness reported by the user
  applies to the previous build, not proof of the new combined decal rendering.
- Affected generation style is Draw01; no other style's created bridges were selected for removal.
  Previous affected-asset removal was blocked by automatic approval (`blocked by policy`); this
  increment did not bypass that restriction. Old saved assets remain unchanged.
