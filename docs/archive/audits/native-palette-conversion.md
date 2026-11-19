# Native instance-palette conversion evidence

## Scope and identity

This record preserves the superseded GO runtime-baking experiment and its precompiled-shader
replacement evidence from 2026-11-18, not native game visual acceptance or a universal rule for other bridges. The governing behavior is maintained in
[native consumers and instance palettes](../../specs/material-spec.md#native-material-consumers-and-instance-palette-preservation).
The source Default to native NetCompositionMeshLit template-17 conversion is within the
non-Network native-template exception; that template is not a substitute for arbitrary protocols.

The local raw evidence is in `work/go-color-integration-20261118/` (development evidence,
not a release dependency). The candidate DLL SHA-256 recorded by `rgba8-verification.json` is
`0ff03e45d19e8d2748c3bd68b05fc7811eec40239e7af25c04a0df21009f05db`.

| Evidence artifact | SHA-256 |
| --- | --- |
| `current-source-shader.json` | `d07d2bda08a5aa49e2d2f8b41a1a786cb16cb13ef593e109aa04df3dceb57ba6` |
| `current-network-shader.json` | `6804ad98a135a2e40a5fa417ff0a3f0abbe6c1390979c38c554c178ed29dd66f` |
| `source-vt-fragment.asm` | `cbe1bd7370bea5e6b3d7481ee5558533052a1fb50cf0f0d5a82a31bbbc08a432` |
| `rgba8-verification.json` | `7e909e94d162494fb92ecb9c15545b4dc5e6a390d72e1b2dfeda8145b9ae3015` |

## Inspected mismatch and superseded candidate

The inspected `BH/SG_DefaultShader` consumes native `colossal_ColorMask` instance inputs;
the inspected `BH/NetCompositionMeshLitShader` does not consume that palette. The extraction
locators are respectively path IDs 567 and 577, not stable shader identities. Persisting Surface
`_ColorMask` properties therefore did not establish equivalent target rendering. The supporting
records include `source-blob-color-bindings.json`, `current-network-evidence.json`, fragment
parameter blobs and the captured native color producers (`ColorSet.cs`, `MeshColorSystem.cs`
and `BatchDataHelpers.cs`).

The experiment's `NativePalette.cs` and `NativePaletteTextures.cs` under
`src/BridgeBuilder/Bridges/Common/Geometry/`
apply the source fixed-palette computation to original mip samples after native linear-color
conversion. Palette alpha affects smoothness and coating, not metallic or surface transparency.
Only BaseColor and Mask change; BaseColor alpha, Mask metallic/R and B, NormalMap, ControlMask
and EmissiveColorMap are preserved. Output uses native Texture v5 RGBA8, sRGB BaseColor and
linear UNorm Mask, with native lossless ZSTD file compression instead of another BC7 encoding.
Original mip dimensions are retained without resampling. The complete inspected ordinary-view
shader clamps albedo; the receipt records negative intermediate RGB but no RGB above one for
these actual inputs. This observation does not establish another source's range or eliminate
weather/infoview and filtering checks. The rejected BC7 and RGBAHalf experiments are not the
candidate format.

## Recorded checks and costs

The receipt reports 34 mip rows and 24,117,247 pixels across the three source LODs. Maximum
linear RGB error is `0.00317895412`, reported mean linear RGB error is `0.0014816224575042725`,
and maximum smoothness error is `0.001960814`. Recorded alpha, metallic, Mask B and coating
errors are zero. These are texel checks, not a rendered-image equivalence result.

| LOD | BaseColor source / output bytes | Mask source / output bytes | Processing ms |
| --- | --- | --- | --- |
| 0 | 22,369,666 / 32,260,569 | 22,369,666 / 21,133,082 | 10,038 |
| 1 | 1,398,146 / 2,224,910 | 1,398,146 / 1,632,711 | 728 |
| 2 | 349,570 / 477,050 | 349,570 / 343,587 | 183 |

The six files total 48,234,764 source bytes and 58,071,909 output bytes: an increase of
9,837,145 bytes. These are affected-channel file totals, not complete package or owner sizes.
The receipt estimates approximately 138 MiB additional GPU-resident data for the two converted
channels versus BC7. No game frame-time improvement or acceptable performance is established.

Actual native TextureAsset Save/Unload/Load is recorded as pixel-exact for six files and 68 mips.
The receipt also records unchanged Nanpu and Lupu empty/default-white palette cases. Those cases
do not establish that every empty or unknown palette has the same native semantics.

## Supersession and acceptance limits

The user withdrew runtime material baking and selected a mod-owned precompiled shader route.
The texture-baking implementation and its RGBA8 outputs in this record are historical measurements, not
current generation instructions or approval to retain a runtime fallback. Native descriptor/template
and appearance-preserving resource conversion remain permitted. The replacement shader was planned
at this documentation boundary; this record does not establish its implementation or acceptance.

## Recorded acceptance limits

Isolated Unity D3D11 and native texture round trips are not native game rendering or saved-bridge
cold-load acceptance. Filtering transformed texels need not equal applying the nonlinear source
computation after filtering; image differences at mask edges and mip transitions remain subject
to source/generated-owner visual comparison. Existing saved owners were not changed; the candidate
affects new generation. Native game visual acceptance was not run in this receipt. Publication
remained paused at this evidence boundary. The record grants no migration or deletion authority.

## Implemented precompiled shader replacement

The replacement is implemented as `assets/MaterialPrograms/NetworkPalette.bundle`, 681,515 bytes,
SHA-256 `ceed3055ead86a39f09f6e1cc905b37e57a5caaf9e12ba9009fc43f38c495967`.
Its adjacent manifest records the offline producers and original native shader/container hashes.
The stable logical family is `BridgeBuilder/NetworkPalette`, distinct from the existing decal and
layered families; queue specialization uses the common stable family/queue identity. Native
Surface vectors `_BridgeBuilderPalette0/1/2` store the authored colors after one native
`Color.linear` conversion, including alpha. Source texture sampling precedes palette evaluation;
the runtime pixel-baking path is not the selected implementation. GO's nonidentity palette requires
this support; inspected Nanpu/Lupu default-white inputs retain the native path. These findings
are source-specific, not a blanket assignment of the shader to all non-Network bridges.

Local evidence under `work/network-palette-20261118/` records:

- `fragment-gpu-gamma.txt` and `fragment-gpu-linear.txt`: isolated GPU GBuffer and Forward
  white-palette comparisons report zero error. Twenty-one constant native-reference cases report
  maximum error `2.980232E-08`; the filtered ControlMask-then-palette nonlinear case reports
  `5.960464E-08`. Both result files have SHA-256
  `c2c15f22e9d597713d474cc2ef79ef0a30aa5f1119fa394cb5834dda46902a40`.
- `surface-native.txt`: actual native Surface SaveData/UnloadProperties/LoadProperties retains
  the stable NetworkPalette queue 2225 and three linear RGBA vectors byte-exactly, with no owner
  program payload. SHA-256
  `78441c45b05a9ad1e2f11b4e5177d763b672e0f9a6cc59abba72fa71b365198c`.

These results establish the reported isolated GPU comparisons and native Surface round trip,
not a full saved-bridge cold start, complete scene/weather/infoview equivalence, native gameplay
or human appearance acceptance. Final build/deployment and their candidate binding are separate
integration evidence. The superseded RGBA8 size and timing measurements above do not measure
this shader implementation. No publication or existing-owner migration is implied.
