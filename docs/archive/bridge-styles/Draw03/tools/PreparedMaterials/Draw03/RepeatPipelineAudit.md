# Draw03 repeat pipeline output-equivalence audit

## Decision

The following exact `PrepareRepeat.cs` source hashes are output-equivalent for
the reviewed conversion. The main agent may add the old hash to the current
entry's `equivalent` list in the shared pipeline table:

| Role | SHA-256 |
| --- | --- |
| Draw03 baked snapshot | `A24D12A7E68C3044053618F98C7B5DFB6AFC11F6A5C4AEB9C2DF479B63D41DA0` |
| Reviewed current implementation | `53A6F2297B243BE8D7910D75612BEFC33147883652993F03A747FA24827AFC9D` |

This decision concerns output-affecting behavior, not identical storage lookup
behavior. New constant-control aliases and `ReuseOnly` intentionally change how
existing products are found and whether a missing product may be produced.
They do not alter pixels or native payload encoding. Do not generalize this
approval to an unreviewed source hash or discard the other pipeline components.

## Snapshot provenance

`artifacts/agent-material-env/Draw03/reports/bake.json` contains the old hash as
the third component of its twelve-component pipeline fingerprint. Both the
environment's `baker/tools/GpuBake/PrepareRepeat.Editor.cs` and compiled project
source `unity/Assets/Editor/PrepareRepeat.cs` match it exactly.

All ten saved Editor source files were hashed against their corresponding report
entries. They match. Current source files were compared after applying the same
wrapper transformations: namespace braces, immediate destruction in the offline
Editor, and retained CPU-readable source textures. Ten of the twelve current
pipeline components are byte-identical to the recorded pipeline. Only these differ:

| Component | Recorded SHA-256 | Reviewed current SHA-256 |
| --- | --- | --- |
| PrepareRepeat.cs | `A24D12A7E68C3044053618F98C7B5DFB6AFC11F6A5C4AEB9C2DF479B63D41DA0` | `53A6F2297B243BE8D7910D75612BEFC33147883652993F03A747FA24827AFC9D` |
| PreparedBakeStore.cs | `FBF2826233C5BD63E5DF69AC09EA5031D94AAFE1A947D0BCBED8EB2B2D1300A6` | `FDF593BF3F5163D6CA0C8EE59C6AD59985D57506C72AC6E8AFBCCBD018A52ED0` |

The other exact matches include PreparePaint, PrepareStructure, both source
decoders, the texture footprint/key helpers, MovableAtlasChannel, BridgeBake.compute
and the installed native `PipelinePlugin.dll`. The store pair is already present
in the shared equivalence table. Its original `Get`, digest and CID computation
are retained; `GetEquivalent` adds receipt-checked one-hop lookup and refuses
conflicting equivalent bytes.

The recorded execution suffix is Unity `2022.3.62f2` / `Direct3D 11.0 [level 11.1]`.
This audit does not authorize treating a different GPU/runtime suffix or changed
source material as the same recipe.

## Output-affecting comparison

| Concern | Checked behavior |
| --- | --- |
| Source content | CID lookup, digest validation, native/VT decoding and texture keys unchanged. |
| Unit identity | `Key(Unit,bool)` is byte-identical, including floats, null-band sentinel, cutout and source digest placement. The comment-only change around null normalization leaves its statement identical. |
| Dimensions | Original source maxima, measured crop extents, band equations and four-pixel rounding unchanged. No downsampling, crop change or new dimension cap. |
| Sampling | `Render` is byte-identical. Same chart frame, pixel-center convention, original/cross-band flags, affine maps, source texture bindings and dispatch dimensions. |
| Pixels and normals | Same `SurfaceAtlas` compute source and normal-frame conversion. Color-space handling and all four output channels unchanged. |
| Mips | Same MovableAtlasChannel and AtlasMip implementation: complete mip chain; color filtering in linear space; unpacked/renormalized normal filtering; unchanged mask/control filtering. |
| Encoding | Same BC7 encoder, native texture serializer and installed lossless compression codec. No format or compression setting changed. |
| Sampling metadata | Same Repeat wrap, Bilinear filter, anisotropy 8, native dimensions and mip count. |
| Surface | `Surface` is byte-identical: same template 0/14, keyword sets, `_Snow`, channel order and serialized texture bindings. |
| Identity and persistence | Same payload-content CID calculation and output writer; unchanged material identity when the same texture payloads are reused. |

Byte-level source checks independently confirmed these blocks are unchanged:

- Key serialization block SHA-256:
  `90f768df5555a062f032675fcef8d577af46c20a69a2ca20083b22cdb381dfe3`.
- Render, Surface, Bytes and Write block SHA-256:
  `46f8233555a73a49f7c09e6d2b74fa7fb268be496db95caf89870c362813eb47`.

## Constant-control alias and reuse-only behavior

The new branch applies only when the decoded source lacks `_ControlMask` and the
current channel is control. Both versions bind the same Unity opaque black texture
with source key `Unity/blackTexture/opaque/v1`. Sampling that constant is independent
of crop and band phase. The unchanged mip/BC7 pipeline preserves that constant for
the same output dimensions. The replacement key retains recipe, pipeline, source
texture identity, width, height and channel, while replacing the irrelevant crop
key with the explicit opaque-black recipe. It does not alias existing nonconstant
controls or textures with different dimensions/channels.

The legacy key is still calculated exactly as before. `GetEquivalent` checks
payload SHA-256 receipts, compares any existing canonical/legacy bytes, and returns
the original payload and content-derived CID. A one-hop alias stores a key and its
verified digest, not resampled pixels. It rejects missing or changed targets.

`ReuseOnly` throws before calling a missing product's producer. It does not install
a fallback, resize a texture or permit an unverified product. It still decodes
source metadata/textures as the current tool requires; it is not a promise that
all source loading has been eliminated.

## Existing product checks

All 23 existing Draw03 product receipts were validated without writing any product:
7 Surface files and 16 Texture files. The native texture headers contain:

| Width x height | Mips | Texture count |
| --- | --- | --- |
| 968 x 164 | 10 | 4 |
| 968 x 376 | 10 | 4 |
| 296 x 376 | 9 | 4 |
| 296 x 32 | 9 | 4 |

Each family has one format 108 color texture and three format 109 data textures
(BC7 sRGB and UNorm respectively), version 5, depth 1, Texture2D dimension 2,
filter 1, wrap 0 and anisotropy 8. The previous native validation additionally
read and uploaded their full mip bytes unchanged. This audit did not repeat a
bake or claim a new GPU/rendering comparison; it establishes conversion equivalence
by exact unchanged production code and verified existing payloads.

Only this Draw03 audit document is modified by this audit. Shared pipeline tables,
runtime code, products and the isolated environment are unchanged. The text is
technical documentation; no game UI strings, localization resources or generated
display names changed.
