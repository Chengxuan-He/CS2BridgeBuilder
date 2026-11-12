# Native164 packed network tangent-frame repair

## Status and scope

Native164 repairs an evidenced generated layered-shader input mismatch. It is
installed for manual preview and city acceptance. It does not claim that the
complete game screenshot has already been visually accepted. No game was launched
or closed by the agent, and no saved bridge was modified.

The custom implementation remains the native shader plus the required current-bone
binding. This repair leaves all native fragment computations, texture payloads,
material properties, UVs, pass states and geometry unchanged.

## Actual player capture

The single consumed capture is
`ModsData/BridgeBuilder/PreviewMaterialCaptures/28a537e9a7f0499b8e2fe0bf26084c16`,
request `tmp6f05404b-33e8-40b5-8534-5f0344b25734`. Its bindings and game log identify
a Draw01 railway preview: 85 draws, 47 meshes and 22 materials. The arm file's
description of a planned eight-lane-road test is not the actual selected input.
No separate road preview was captured by this one-shot request.

The linear shaded image contains 16,642 pixels with non-finite RGB, concentrated
on the mechanism's railway roadbed and its ends. All 16,642 have positive coverage
in the independent coverage image. Thus those black pixels are covered geometry
whose invalid shaded values are converted to zero by preview pixel processing;
they are not proof of missing triangles. Other coverage holes are not automatically
assigned this cause.

The generated TrainSubwayTrack material has all six original native textures
bound. Its declared values match the fixed approach's original material apart
from the added bone parameter, receiver-mask binding and negligible infoview-color
rounding. The captured roadbed mesh has 504 vertices with finite positions/UVs,
`Normal:SNorm16x2`, `Tangent:Float32x1` and valid mounting bones. The source and
generated materials are therefore available for a direct same-source comparison.

## Root cause and measured repair

The native network vertex shader decodes packed tangent-frame input independently
of `_TANGENTSPACE_OCTO`. Its inspected non-DOTS GBuffer and Forward variants have
identical DXBC under the relevant keyword combinations. The generated authoring
tool incorrectly interpreted the keyword as permission to select an unpacked
`float4` tangent adapter.

For example, a captured tangent contains raw bits `0x00004000`. Those bits encode
a direction; interpreted as an ordinary float they are a tiny denormal. The wrong
adapter normalized that input into non-finite tangent components. A diagnostic
fragment observing the actual generated vertex output reproduced this with the
captured mesh: normals were `(0,1,0)`, positions and UVs were finite, but 391,920
tangent RGB components were non-finite across 130,640 rendered pixels. The normal
alone was not a sufficient test.

Using the native packed decoding in every generated vertex variant restored finite
signed unit tangents without changing the observed positions, UVs or normals.
With the original native fragment program restored, all four captured GBuffer
outputs became finite. Before the repair, the normal output was invalidly encoded
as zero and the lighting output contained 391,920 non-finite components. The
base-color output is byte-identical before and after the change:

`D1F430B5093A908BBB87209E5BD4DE62873CF285D4A137062296201009F83836`

This is an input-protocol repair, not a texture replacement, brightness adjustment,
NaN clamp in the shader, or removal of a material layer.

## Implementation and regression coverage

- `tools/MaterialShaders/BuildNativeLayered.py` now authors packed frame decoding
  for every DOTS/non-DOTS native network vertex variant. The unused unpacked
  program generation was removed.
- `assets/MaterialPrograms/WorldLayeredPbr.bbs1` and its manifest were regenerated.
  Native pixel programs were retained.
- `tools/MaterialShaders/VerifyNativeLayeredBindings.py` verifies all 66 native
  fragment variants byte-for-byte, all six pass states, and packed consistency
  across all 72 generated vertex variants.
- `tools/MaterialShaders/Material164PackedReplay.Editor.cs` replays real native
  mesh/texture inputs in an isolated Unity host. The actual captured railway input
  and the previously captured saved-road Original input pass closed, half-open and
  fully opened pose checks with the packed keyword both enabled and disabled.
  Non-DOTS tests include a nonzero world translation. Every native GBuffer channel
  is finite, and changing that keyword does not change the output hashes.
- A real `BatchRendererGroup` submission of the railway input passes the same six
  pose/keyword cases using the host's native instance metadata. This is not a
  complete Cities: Skylines II scene or motion-vector acceptance test.
- The bone-decal adapter was audited separately. Its producer explicitly supplies
  a four-component tangent and projection-frame channels; it does not select the
  erroneous network packed/unpacked branch. Its program was not changed.

All pass variants were compiled and their records checked. Actual full-game
shadow/depth/motion-vector images were not newly captured. The road replay is a
real saved Original road input, not a capture of the user's latest eight-lane-road
preview. Neither limitation is hidden by the successful railway evidence.

The complete frozen-source build, existing repository tests, twelve-language
localization checks and `git diff --check` passed. No runtime UI strings changed.
The design and material contract were updated before the authoring implementation.

Detailed diagnostics and regression artifacts are under `work/material163` and
`work/material164`. The production source snapshot and deployment receipt are under
`artifacts/material-remap/shared/native164`.

## Deployment and remaining acceptance

The game was absent before installation. The previous DLL/PDB were backed up;
964 installed files were verified, with exactly those two files changed and no
added or removed files. Installed DLL SHA-256:

`5C96074B33084598BCD671B477A50802AA7586424782E940C2E7170E170FF23C`

The official SDK ZIP is 77,898,616 bytes. No publication or GitHub push was performed.

New previews and newly created bridges use the repaired program. Existing saved
owners retain their own persisted old program and are not silently rewritten.
Manual verification must cover the reported railway and road previews and, when
desired, newly created city bridges at near/far views and during movement. The
mod-disabled custom-program loading limitation remains explicitly deferred by
the user's authorization; this repair does not change that scope.
