# Draw02 native material rebuild

## Deliverable

The retained fresh output is `artifacts/prepared-materials/Draw02/rebuild-native-v2/output/Draw02/`.
It contains **64 files / 21,648 bytes**: 31 Surface descriptors and their CID sidecars,
one 22-byte zero-control Texture and its CID sidecar. Cable and Road share the single
zero-control payload. No original texture was decoded or re-encoded, and no old
prepared Structure or Repeat library was copied. Original built-in texture and VT
resources remain direct references, preserving their stored mip chains.

The output comprises Cable (1 descriptor), Road (1), Lanes (26), NativeSources (2)
and Paint (1). Eight compatible structural source Surfaces remain original built-in
references and therefore add no files. Six structural source descriptors convert
from native template 17 to template 0 and deduplicate to two descriptors. Paint uses
a new native VT descriptor referencing the original standalone VTTexture; it does
not use the former mip-zero extraction and mip regeneration path.

The Draw02-only fresh common subset remains available for byte-identical reuse at
`artifacts/prepared-materials/Draw02/rebuild-native-v1/output/Draw02/` (Cable, Road,
Lanes, Shared; 58 files / 14,580 bytes). Reusing it does not invoke a texture baker.

## Source identity mapping

| Original Surface CID | Operation | Result Surface CID |
| --- | --- | --- |
| `436d1828c36d1336cf115c85def2c62c` | builtin-reference | `436d1828c36d1336cf115c85def2c62c` |
| `55697a0c744a3cfa77056b9ff0f93646` | native-descriptor-conversion | `e7e3a33d1499167c8c385ebfb70af78b` |
| `5d98bd98700c73926bb2b3ff69c784e2` | native-descriptor-conversion | `1d6c92d008a952b1db4e1cc6cf7a1160` |
| `8a28ab8d9526e258efbcb81c3bbc936b` | builtin-reference | `8a28ab8d9526e258efbcb81c3bbc936b` |
| `8f9df730508323eaed9b30749f399ffc` | builtin-reference | `8f9df730508323eaed9b30749f399ffc` |
| `9b0ef24ab46511040acf280f549a3a73` | native-descriptor-conversion | `e7e3a33d1499167c8c385ebfb70af78b` |
| `9f31e9f71c97263c984303131decbfb0` | native-descriptor-conversion | `1d6c92d008a952b1db4e1cc6cf7a1160` |
| `a3743232fac2497c278d590f55ae827d` | builtin-reference | `a3743232fac2497c278d590f55ae827d` |
| `a4e1a84902d9065eeb75df9b18c9a746` | builtin-reference | `a4e1a84902d9065eeb75df9b18c9a746` |
| `af48c4b16775963b7d025f9f672722f1` | native-descriptor-conversion | `1d6c92d008a952b1db4e1cc6cf7a1160` |
| `d04355d2a678a0a396fe017b043e4c02` | builtin-reference | `d04355d2a678a0a396fe017b043e4c02` |
| `dee7bfe689bf6752861f0f6f29a6c8e2` | native-descriptor-conversion | `1d6c92d008a952b1db4e1cc6cf7a1160` |
| `eb151a8b50ea389b26d394f3c78290fb` | builtin-reference | `eb151a8b50ea389b26d394f3c78290fb` |
| `f1e512a5deebd43bbbe4bcf7b84a97a6` | builtin-reference | `f1e512a5deebd43bbbe4bcf7b84a97a6` |
| `4b5500b1b5c270890d99149f7f62e51a` | native-vt-paint-descriptor | `d765f1348645cc68318b5ac29c61abde` |

## Verification

`Test-NativeRebuild.ps1` used the independent Draw02 Unity project and the installed
game's actual SurfaceAsset/TextureAsset readers. Unity exited with code zero.
Both structural conversions and native VT Paint parsed completely, including native
template, VT header, property maps and tangent encoding. All 26 native VT lane
descriptors, Cable and Road passed. The sole shared control's complete four-byte mip
payload uploaded unchanged. No GPU image baking or encoding was run.

`NativeClosureCheck` parsed every output Surface with the production dependency
reader. Every reference resolves locally or to a verified installed-game archive
CID. It recorded all 31 descriptor roots and dependencies: zero orphan payloads,
zero duplicate physical payloads per CID. The output has no empty directories.
All 64 retained files were compared byte-for-byte with the private producer output.
Evidence is in `rebuild-native-v2/producer.json`, `manifest.json`, `sources.json`,
`native-reader.log`, `native-reader.json` and `retained-copy.json` under the retained
Draw02 artifact directory. Template and original source checks are producer evidence;
unchanged built-in payload references are not a claim of pixel render validation.

## Runtime resolver

`Draw02NativeMaterials.Resolve(SurfaceAsset original, Func<string, SurfaceAsset> load)`
provides the shared production caller with the same interface as Draw01 and Lift01.
Only the producer's six `native-descriptor-conversion` sources are mapped to its two
fresh object descriptor CIDs. Original built-in sources must load as template 17;
converted descriptors must load as template 0, be non-VT, and have the exact expected
CID. The eight compatible source identities return the same Surface object without
calling the prepared loader. Paint is not included in this structural conversion.

`NativeMaterialsCheck` source-links the actual resolver and compares its routing
against the full producer report and retained CID sidecars. It passes all six
conversions, eight retained sources and rejection checks for invalid source/template,
wrong output CID/template and unexpected VT output. This check uses a SurfaceAsset
control-flow seam; native descriptor validity remains established separately by the
installed-game readers. Shared dispatch wiring and the full mod build are owned by
the shared agent. No public code was edited for this bridge-specific entry point.
Technical exception diagnostics use the existing bridge-operation error handling;
no new localized UI labels or notices were introduced.

## Remaining shared implementation and acceptance

This delivery establishes descriptor preparation and native graph validity. It does
not establish complete Draw02 appearance. Paint's persisted native rendering must
still be checked in game, including channels, blend behavior and mip selection.
Structural receivers outside measured original charts and road local-region/band
repetition still need the shared verified native sampling implementation. They must
not fall back to the old per-sampler image library. The existing 953 sampler mapping
and measured per-part/per-LOD chart data remain the source-to-use inventory.

Preview, saved creation, regeneration, export, cold loading, independently owned
external dependency snapshots and uninstall independence still require integrated
acceptance. The complete official SDK package must also be measured against its
55,000,000-byte gate. No final game visual acceptance or whole-style completion is
claimed from successful native header parsing.

Only Draw02-private authoring/test/report files and private outputs were changed.
No official assets, installed files or user bridges were modified. No background
material writer remains, and the independent environment is retained for its owner.
There are no UI changes; existing localization behavior is unchanged.
