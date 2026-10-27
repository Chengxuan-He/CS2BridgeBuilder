# Draw02 shared-source migration handoff

## Completed bounded work

Created a source-texture identity map independent of all per-use sampling keys.
The current Draw02 production inventory has 953 structural sampling records
(763 triangular and 190 rectangular), 14 original material CIDs and 14 native
backing CIDs. Comparing actual backing content plus stack/layer and channel
semantics reduces these to 22 distinct source-channel identities. Each record
retains its exact A/B/C/bounds Float32 bits, source version, fold/repeat behavior
and normal interpretation.

All original sources are installed built-in game assets. Their fourteen backing
files total 111,847,422 bytes in the game archives; this is not a mod package size.
Compatible original references require no new packaged texture copies. The map
does not replace the still-required current generated repeat resources without
a supported native per-use sampler implementation.

No production assets were changed or deleted. No source texels were rebaked,
resampled into a new payload, encoded, or placed into a container of independent
copies. New material/texture payload count: zero. Proven package saving: zero.
The ten-style 50,000,000-byte official SDK ZIP gate remains a main-agent
measurement and has not been satisfied by this partial migration.

## Representative verification

The representative uses actual main Draw02 source
`8a28ab8d9526e258efbcb81c3bbc936b`, SHA-256
`e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597`, and
sampling key `2383b25d3a91b6f24bda62dcbd91b93b687b0e2e4003cf43adcde823db94dcba`.
Its original native VTSurface provides four 4096 x 4096 channels.

The private Unity probe decoded original native mip zero and sampled those
same textures through direct affine UV mapping and the existing repeat/fold
semantics. For four interior coordinates across four channels, all 16 results
were exactly equal. Outside the first triangular cell, 12 of 16 results differed;
maximum channel error was 0.8352941274642944. This is a direct native-texel
sampling mismatch, not a comparison of separately compressed baked outputs.
Ordinary affine UV replacement therefore fails this representative's general
periodic/folded sampling contract. No permutation/shear optimization was tested.

The GPU probe passed its explicitly limited assertions on Unity 2022.3.62f2,
D3D11, exit 0. It does not demonstrate a supported native material configuration
for arbitrary atlas-subregion repeats or triangular folds. It uses a temporary
offline diagnostic compute shader only, never a runtime shader.

The source decoder explicitly reconstructs mip zero only. It cannot establish
preservation of the original full native mip graph in a new ordinary Texture.
Full source-graph references must retain native VTSurface/VTTexture/MidMips and
their original identities, or use a separately verified full-mip conversion.
Direct native SurfaceAsset.ReadHeader was also attempted; its VT reference read
requires live game AssetDatabase services and failed in isolated Unity before
loading. That evidence is preserved, rather than treating a manually parsed
header as a successful native cold load.

## Shared dependencies and exact remaining work

The main agent has received these interface requirements:

- Keep shared source-payload identity separate from existing sampler keys;
  identify original channel content, color space, normal representation and mips.
- Return an original/native shared Surface plus per-use UV or supported native
  sampler settings. A sampler key must not mandate a separate baked Texture.
- Provide native atlas-subregion repeat and triangular fold handling, including
  reflected normal behavior and boundary/mip filtering. Ordinary affine UV alone
  is insufficient for the measured representative.
- Preserve complete original native VT graphs; current mip-zero decode is not
  a full-mip conversion API.
- Validate cold-loading the original VT material reference in the game's
  initialized native asset database.

No shared API was invented or public file modified. Migration of all 953
production records remains pending those native capability/interface decisions.
The map intentionally marks the full native dependency closure and production
replacement as unverified. Existing valid resources remain in place. This
bounded investigation is stopped; no further optimization experiment is queued.

## Files and evidence

Tools in this directory:

- `ExportSharedSourceMap.py`: reads actual source bytes and emits separate
  payload/channel identities and exact sampler records.
- `SharedSourceProbe.Editor.cs`, `SharedSourceProbe.compute`,
  `Test-SharedSource.ps1`: the private representative capability probe.

Evidence root: `artifacts/prepared-materials/Draw02/shared-source-migration/`.
It contains `source-sampler-map.json`, `representative.json`,
`representative-native.json`, `representative-evidence.json`, the successful
GPU log, and `representative-native-header-unavailable.log` for the native
database limitation. All deliverables are outside the private environment.
There are no runtime UI or localization changes.
