# Lift01 native material delivery

Production inputs are rebuilt by `PrepareRebuild.py` from the real game COK archives and Lift01's three source-render declarations: 16 original Surface CIDs and 23 render/slot uses. The shared `tools/Rebuild-NativeMaterials.ps1` consumes this source manifest and independently extracted native template defaults. It never reads the previous per-triangle material library.

## Native network-to-object conversion

Three network Surface inputs require template 33 to object template 0 conversion. The producer explicitly writes 137 applicable source-template float defaults, retaining original texture CIDs, integer/vector/color overrides and `_TANGENTSPACE_OCTO`. It does not decode or bake images.

| Original Surface CID | Output descriptor CID |
| --- | --- |
| `368eb4e7fa7b81f6e28ee321c76dd9f2` | `2d94a491d55ac415c29591597618b5ef` |
| `879f0a8882e3069d3d8b890a5ec15140` | `2d94a491d55ac415c29591597618b5ef` |
| `0a9990f567fc079703b199f1fcb0c53c` | `ec895036ef4c8b94de29cb702fdd6120` |

Each descriptor is 3,253 bytes at `NativeSources/<CID>/Object.Surface`, with a CID sidecar. Two distinct descriptors and sidecars total 6,570 bytes. Six original built-in textures remain direct references, preserving all 69 mip levels. Fully compatible materials retain original native Surface references.

`Lift01NativeMaterials.Resolve(original, load)` contains this CID mapping. The style-local callback must load and retain the descriptor above. It requires original atlas UVs and complete receiver containment; normalized baked-page UVs are not interchangeable. External descriptors must be copied with unchanged CIDs per generated owner; built-in texture references remain native.

`PrepareNativeObjects.py` is only a diagnostic template-byte probe. Its template-only descriptors omit native defaults and must not be used for production or copied into the material package.

## Fresh output and verification

`artifacts/prepared-materials/Lift01/rebuild-native-v1/output/Lift01/` contains 64 fresh output files totaling 21,648 bytes. This includes source-level descriptors, native VT Paint and the common Cable/Road/Lanes/zero-control resources supplied to the shared producer. It contains no per-triangle structure texture variants. Lift01's producer re-encoded no textures.

`VerifyRebuild.py` verifies every file hash and sidecar, original source digests/templates, retained texture CIDs and all 137 defaults as exact serialized Float32 values. Evidence is `graph-verification.json`. The public native descriptor validator ran in Lift01's independent Unity project and consumed all 31 descriptors successfully, including 27 VT descriptors. Evidence is `native-reader.log`. This is native format validation, not renderer acceptance.

The shared producer verifies outgoing descriptor dependencies against the local graph or game COK registry. Original VT mip rendering and independent saved-owner closure still require native saved-asset checks. The output is not in `assets/Materials`; the main agent coordinates publication after integration.

## Sampling and incomplete branches

`NATIVE_UV_CHARTS.md` describes the complete 23-stream runtime chart table and actual receiver evidence. Original textures retain Clamp U/V, trilinear filtering and anisotropy 16. Chart bounds do not establish periodicity, and whole-image Repeat cannot substitute for an atlas region.

The one measured road repeat input preserves a 432 by 860 source region, at 216 and 215 texels per metre respectively, with eight physical sampling aliases. Its configuration is regenerated from BBN data. The new repeat material and native sampling implementation are not included in this output. Paint rendering, out-of-chart receivers, rank-one UVs, preview/create/regeneration/export, cold load, uninstall independence and aggregate official SDK package size remain incomplete. The old generated structure library is not an authorized fallback.

No user-facing strings changed. Technical diagnostics retain existing localized failure handling. No build, deployment or environment cleanup was performed here.
