# Draw03 Repeat control reuse audit

## Result

All seven currently packaged Draw03 Repeat descriptors are eligible to reference
the existing style-local `Road/ZeroControl.Texture`. Their four distinct control
textures are transparent zero at every mip. The real measured prototype has no
authored control channel. This conclusion was checked against Draw03's actual
source files and GPU-decoded payloads; it does not inherit another style's hashes.

The descriptor-only candidates are retained under
`artifacts/prepared-materials/Draw03/repeat-zero-control-candidates/Repeat/`.
The corresponding `audit.json` records every old/new Surface CID, SHA256, byte
offset, before/after texture binding and native disk-readback result. The Unity
log is `artifacts/prepared-materials/Draw03/repeat-zero-control-final.log`, exit 0.
The audited candidates are integrated into `assets/Materials/Draw03/Repeat/`.
No shared code was edited and no texture was baked, encoded or rewritten.

## Actual native source

The inventory's source is native DrawBridge03 Surface
`792a855e9d0c5c34d2c41d48b062139d`, SHA256
`fb159828e8d73c22dc96cdf7e82c74b36a53483d0126124a6eacbd0fda4a7e46`.
The tool reopens the installed native Game/BridgesAndPorts archives and resolves
that exact CID. Its control-stack CID is all zero; there is no backing VTSurface
CID. The existing native source decoder exposes only `_BaseColorMap`, `_NormalMap`
and `_MaskMap`, despite the template inventory describing an optional control slot.
Thus this is an absent authored input, not an authored nonzero control being discarded.

Only complete unbanded Draw03 repeat-unit descriptors are eligible. An authored
control or an unaccounted inventory entry fails the audit before candidate creation.

## Complete mip verification

The installed native TextureAsset reader loads every original control header.
Native ZSTD decoding preserves the encoded mip bytes; uploading them is checked
against the full original byte array. An offline explicit-LOD shader samples each
mip into an RGBAFloat render target at that mip's own dimensions. All four float
components must be exactly zero at every pixel. A known-white positive control
must read back four ones before zero results are accepted. No low-resolution byte
quantization, mip regeneration or approximate alpha test is used.

| Resource | Dimensions | Mips | Decoded pixels | Native file bytes |
| --- | --- | --- | --- | --- |
| Between-lane control | 296 x 32 | 9 | 12,632 | 40 |
| Lane control | 296 x 376 | 9 | 148,353 | 53 |
| Clean-asphalt control | 968 x 164 | 10 | 211,573 | 53 |
| Grating control | 968 x 376 | 10 | 485,198 | 75 |
| Existing Road transparent zero | 1 x 1 | 1 | 1 | 22 |

All 39 mips / 857,757 pixels passed. The four original control formats are
`RGBA_BC7_UNorm`; the existing zero uses `R8G8B8A8_UNorm`. All retain linear data
semantics. A constant zero has no spatial resolution or filtering differences.
The replacement was independently measured as CID
`729893d593df5d52ba911bc1e7eab77b`, SHA256
`d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4`.

## Descriptor integrity and identity

Each candidate clones the original Surface bytes and replaces only the 16-byte
`_ControlMask` CID field. Prefix and suffix comparisons prove that all other
serialized data is unchanged, including template, keywords, UV parameters and
other channel references. Base color, normal and mask CIDs and payload hashes
remain exactly the originals. The native Surface reader consumes every candidate
from disk and confirms its new binding.

The new CID follows the existing `PreparedBakeStore` rule:

```text
SHA256(UTF8("BridgeBuilder/RoadRepeat/1/" + SHA256(candidateBytes)))[0:32]
```

Repeat recipe directory keys do not change. The audit emits only seven Surface
files and seven CID sidecars, not copies of unchanged textures. Integration retains
the original other-channel dependencies and the existing style-local Road zero,
and copies its exact payload/CID into `Shared/729893d593df5d52ba911bc1e7eab77b.Texture`.

## Benefit and limits

The four obsolete control payloads total 221 bytes because their constant BC7
blocks already compress well. Their four CID files add 128 bytes. No non-Repeat
Surface in the current Draw03 style references them, so publishing all candidates
permits removing 349 bytes and eight files from the complete style package. The
Shared copy of the existing 22-byte Road zero plus its 32-byte CID adds 54 bytes;
descriptor lengths are unchanged. The completed integration therefore saves
295 bytes net: 112 to 106 files, 3,651,427 to 3,651,132 bytes for the complete
current Draw03 material directory. These are style-material sizes, not whole-mod
or generated-bridge sizes.
This is principally a reduction in texture resources, not a meaningful download
size reduction. No measured generation-time improvement is claimed.

A generated bridge's saving depends on which units it uses and whether its graph
already contains the Road zero. Do not apply the complete-style 349-byte figure to
every individual bridge. Existing exported bridges are unchanged by these candidates.

The audit tool and shader are offline verification inputs, not mod resources or
custom runtime shaders. No user-facing text, display name or localization key changed.

## Integration verification

`IntegrateRepeatControl.py` checks every candidate against the native audit, original
Surface hash/CID, the exact 16-byte control-only difference and the existing
`PreparedBakeStore` identity rule before writing. It backs up the 22 original files
under `artifacts/backups/Draw03/repeat-zero-control-integration/`; that tree contains
original Surface/Texture/CID files only, with no reports or extra metadata.

All 36 style Surface files were scanned for the four obsolete native control CIDs
before and after descriptor publication. Their zero reference counts authorize
removing only the eight identified Shared files. Unrelated package files remain
byte-identical. `repeat-zero-control-integration.json` records original/new hashes,
identities, backup scope, complete-Surface reference checks and exact size changes.

An independent packaged dependency check verifies all 28 bindings from seven
descriptors resolve to 13 unique style-local textures with the exact hashes of the
native-verified candidates. Its report is `repeat-zero-control-dependencies.json`
in the Draw03 artifacts directory. Installed files and generated bridges were not
modified; main-agent build/package integration remains separate.

## Reproduction

Copy `RepeatControlAudit.Editor.cs` into the independent Draw03 Unity project's
`Assets/Editor`, and `ControlMipRead.shader` into its `Assets` directory. Keep its
existing source-reader helpers. Execute `Draw03RepeatControlAudit.Run` using
`-batchmode -force-d3d11 -quit` with `-gameData`, `-styleRoot`, `-repeatInventory`,
`-candidateOutput` and `-logFile`. The output directory must be new; the tool does
not overwrite previous candidates. This runs source decoding and validation only.
