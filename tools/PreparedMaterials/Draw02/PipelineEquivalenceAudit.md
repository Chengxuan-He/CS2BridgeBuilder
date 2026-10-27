# Draw02 repeat pipeline equivalence audit

## Decision

The verified Draw02 repeat products may be reused by the reviewed current repeat
baker without changing serialized material or texture output. The reviewed
`PrepareRepeat.cs` mapping is:

| Role | SHA256 |
| --- | --- |
| Verified Draw02 snapshot | `9BC8EEC1D55BA146A040E1FEF430B23DC095D6F1B537682684BF731C0969537C` |
| Reviewed current source | `53A6F2297B243BE8D7910D75612BEFC33147883652993F03A747FA24827AFC9D` |

This is a source-equivalence audit, not a fresh execution of the current tool. No
baking or public source modification was performed. Add the old hash to the exact
current hash's reviewed equivalence list before supplying the Draw02 prior report
to `ReusePipelineReport`. The list inspected during this audit did not yet contain
the Draw02 old hash. Do not extend this decision to any other source hash.

## Provenance and component checks

The prior report is
`artifacts/agent-material-env/Draw02/reports/bake.json`; its inventory digest equals
the SHA256 of `artifacts/prepared-materials/Draw02/repeat-inventory.json`:
`43d4f5535424cf4ec1ab4fdd6589fd4c0fd1d37f08e3c628f79b5f0d8f564c6e`.
Both the isolated baker source and the actual Unity editor copy of PrepareRepeat
match the old hash above. All ten editor source snapshots match their respective
component positions in the report's twelve-part pipeline fingerprint.

The current sources were compared using the same namespace, DestroyImmediate and
readable-texture substitutions performed by `Prepare-Repeats.ps1`. Eight of the
ten editor source components remain byte-identical. The compute shader and native
codec also match the report exactly. Only PrepareRepeat and PreparedBakeStore
changed.

| Unchanged component | Report and current SHA256 |
| --- | --- |
| PreparePaint | `35B5A60B1303C8D3913D599943B29D940FEF3E4F989442E4E002AC0D0BEE9D59` |
| PrepareStructure | `FF4761076F7809189660C8172CCF950C2A9001939E906C2F3970A414CAAC7E28` |
| MovableStructurePacking | `98876B38230F60CA79EA7671ACEBE420520258450A7B76B8BF82F1266FB71EE8` |
| MovableStructureKey | `EA036AC9DAE2803FFD171724C3C370B001EFB373DEFA565CD798A0D89565608E` |
| MovableTextureFootprint | `58B0E7F7704B5500988DB3C6701BA853388B7E9A6990F5DB38D8236D6738B96E` |
| MovableAtlasChannel | `FFEBEE2429F8E7242E7FAF5870567D813935F7407207A1AF61E18B3ED3896876` |
| MovableVtTextureSource | `6B189FFABE310D98ECE652892EAB2B602E8F740A5F0E10E711E7951D1DEFE680` |
| MovableTextureSource | `5BDE6967098B75297A0FEF14710F9922ADB0B5EEB30F7F2F63C6C5944617BB08` |
| BridgeBake.compute | `0502DA78915F28030A10B2C8B3604055E378F40486F0C5666C06B3CF38C57CDB` |
| PipelinePlugin.dll | `A5393B364AEACFF6D34F596E02FE230801846AE5C984177D4D16A86ED51E1A84` |

PreparedBakeStore changed from
`FBF2826233C5BD63E5DF69AC09EA5031D94AAFE1A947D0BCBED8EB2B2D1300A6`
to `FDF593BF3F5163D6CA0C8EE59C6AD59985D57506C72AC6E8AFBCCBD018A52ED0`.
Its existing Get, key serialization and content-derived CID calculation are
unchanged. The changes add nullable annotations and a checksum-verified one-hop
alias resolver; they do not transform a returned payload.

## Output-sensitive review

| Aspect | Observed comparison and consequence |
| --- | --- |
| Source decoding | PrepareStructure.MaterialSource and both source texture readers are identical. The native Surface digest and decoded channel fingerprints are still verified. |
| Recipe interpretation | Key is text-identical. Both versions already normalize JsonUtility's empty bands to null. The current version adds explanatory comments only at that point. Rectangle, bands, orientation, cutout and row semantics are unchanged. |
| Dimensions | The same crop extents, source channel maxima, band requirements and four-pixel ceiling alignment are used. Draw02 remains 312x828, 600x828, 604x752 and 548x752 from 4096x4096 source channels. |
| Pixels and sampling | Render is text-identical. The same SurfaceAtlas kernel, half-pixel positions, band split rounding, PatchArea, repeatY, source sampler and channel binding are used. No UV or color arithmetic changes. |
| Missing control input | Both versions bind Texture2D.blackTexture with the same opaque-black source identifier. The current version records whether the channel was missing so only this fallback receives a canonical constant-product key. Authored control textures keep the original sampler key. |
| Mip generation | MovableAtlasChannel and AtlasMip shader bytes are identical. The full chain and channel-specific color/normal filtering remain unchanged. Constant opaque black remains constant through the control-channel average. |
| Encoding | CompressAtlas, BC7 layout, base-color sRGB versus linear channels, native Texture serializer and codec DLL are identical. No recompression is performed when an existing product is selected. |
| Texture sampling metadata | The same Repeat wrapping, bilinear filtering and anisotropy 8 are retained. The constant key names these existing properties; it does not change them. |
| Surface serialization | Surface is text-identical: native template 0 or 14, Snow override, channel order, texture CIDs and keyword sets are unchanged. |
| Asset identity | CID remains derived from recipe and payload digest, independent of product lookup key. An alias returns the same bytes and therefore the same CID. |

The constant fallback key still includes pipeline, source-channel fingerprint,
output width, output height and channel index. It removes only the varying crop
sampler from an already constant input. SurfaceAtlas writes AtlasControl by direct
sampling; it does not blend that channel with road color or cutout alpha. Therefore
two missing-control recipes with the same dimensions produce identical control
pixels, mip chains and serialized texture bytes regardless of the crop region.
This statement does not apply to authored control masks.

## Storage and execution changes

ReuseOnly stops before calling a producer if no existing product is available; it
cannot silently rebake. For constant control, GetEquivalent first checks current
keys and validated aliases, then the legacy key. It verifies payload receipts,
rejects missing or corrupt pairs, and rejects conflicting supposedly equivalent
payloads. Aliases have one validated key target and payload digest; they do not
recursively follow paths. A successful match returns original payload bytes.

The wrapper computes actual component hashes. ReusePipelineReport accepts a prior
component only when it is identical or explicitly listed for the exact current
component hash. The Unity version and graphics-device version are still appended
by the editor; a differing environment remains a different product key and will
fail under ReuseOnly rather than produce a replacement.

Consequently the reviewed changes affect storage lookup, integrity checks,
metadata and failure behavior only. They preserve the output for valid Draw02
inputs and compatible recorded environment. Existing native verification remains
the evidence for the old products: five Surfaces, sixteen textures, identical
opaque/cutout texture CIDs, and 8,670,272 mip bytes uploaded unchanged. This audit
does not claim a new game appearance test or a new current-pipeline execution.
