# Draw02 authored non-tiling mapping

## 1. Implemented scope

`EmitUvSampling.py` emits `Draw02UvSampling.For(render, mesh, domain)` and its lazy
`Draw02_UvSampling` BBN1 resource. All 45 prototype render/material slots and all
38,210 domains are audited. The final classification enables 32,995 domains and
retains density behavior for 5,215 domains:

| Mapping | Domains | Result |
| --- | ---: | --- |
| Verified object UV0 single traversal | 30,211 | Retain authored source mapping |
| Verified negative-V network atlas traversal | 2,758 | Retain authored source mapping |
| Verified local window YZW/COLOR | 26 | Retain all original channels |
| Positive-V native network length mapping | 4,871 | Preserve density path |
| Conservative overlap with genuine RepeatRegions | 336 | Preserve repeat path |
| Window repeated-pane traversal | 8 | Preserve density path |

The existing 36 independently measured braces remain included (2,414 domains).
Far main LOD and fixed pillar/bottom/side/node mappings are audited separately;
near membership is not projected onto a different LOD. There are no network domains
mixing negative and nonnegative V. The three genuine RepeatRegions remain unchanged.

## 2. Source and native sampling evidence

The generator freshly reads the installed BridgesAndPorts `Blob.cok`. Its 11 unique
Draw02 Surface hashes match all 45 existing source-map slots. Exact descriptor,
texture/VT bindings, original mesh hashes, current schema-3 recipe hashes,
per-slot counts and sampling evidence are recorded in
`artifacts/prepared-materials/Draw02/brace-semantic/non-tiling-classification.json`.
Current `resources.assets` template identity evidence comes from
`artifacts/prepared-materials/shared-nontiling/native-semantics/`.

Templates 0 and 1 use shader 571's UV0 mapping with unit scale and zero offset;
Draw02 descriptors have no alternate UV-transform overrides. Complete native
UV-domain traversal is inside one integer tile. The direct shader sampling path,
actual prototype coordinates and unchanged descriptor jointly establish the mapping;
finite chart bounds, global wrap, visual repetition and failed destination mapping
are not independently proof. Texture motifs and disconnected repeated parts
do not imply UV tiling within a part.

Template 17 shader 581 distinguishes signed V in the vertex stage: nonnegative V
is multiplied by network mapping length, while negative V uses `frac(V)`, U unchanged.
Only complete negative-V domains within one integer tile are enabled. Positive-V
domains remain on the density path even when stored coordinates fit inside 0..1.
`MovableCompositionDeformation.cs` reproduces the signed operation; structural Build
retains/interpolates original attribute channels. Raw negative V and its fraction
sample identically through the actual Repeat textures.

Network near textures are 4096 square with 13 mips; far textures are 512 square with
10 mips. All six original textures have Trilinear filtering, Repeat addressing and
anisotropy 16. Near mask CID `2ae97c5ed8cb4ef6561cba57795d0966` shares the complete
13-mip zero-blue verification in Draw01's `nativeNetworkEvidence`; its hashes are
rechecked against the current archive. Far mask CID
`d3f52f8478b74e116965f66785d0b966` is independently decoded through all ten mips and
has zero RGB throughout. Thus shader 581's mask-blue worldspace blend contributes
nothing for either source. Existing object descriptor conversions preserve all three
native channel CIDs, samplers and mips. Both converted descriptors are checked against
their source bindings; blindly replacing the native shader is not required.

Window slot 1 has original template 2/shader 550, no property overrides and only
`_TANGENTSPACE_OCTO`. Shared full-channel native audits identify 13 local domains per
near/mid LOD, including exact YZW and COLOR. The generator verifies indices and complete
UV0 against the native input before enabling them. Eight repeated-pane domains remain
excluded. Original complete channels and Surface must be retained.

## 3. Verification and remaining acceptance

Regeneration uses the common schema-3 decoder and validates complete membership,
resource lengths, descriptor identities, signed traversal, texture CID sidecars,
all-mip zero worldspace contribution, RepeatRegion separation and exact window inputs.
Known invalid domain indexes throw; unknown render/slot combinations return false.
Runtime does not read original prototype files.

No new materials, textures, geometry, topology or bones are created by this change.
The classifier is 153,092 bytes of numerical metadata. No user-visible strings or
localization paths change. Physical density may change only for enabled authored
mappings; these changes are not reported as density-preserving passes.

Shared build and native packed-UV replay, owner dependency closure, complete package
size, and human near/far/motion/cold-load acceptance remain separate gates.
