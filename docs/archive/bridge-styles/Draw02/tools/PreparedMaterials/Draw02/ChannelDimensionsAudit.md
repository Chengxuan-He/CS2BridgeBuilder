# Draw02 structural channel dimensions

## Result

All actual texture channels within each of Draw02's fourteen source materials have
the same original width and height. Independent per-channel allocation therefore
saves **zero pixels, zero native mip bytes and zero serialized texture bytes** for
the current 953 structural sampler units. No baker change, Unity probe or rebake
is warranted for this bridge.

This conclusion compares channels within a material. Different Draw02 materials
use different resolutions, and the existing baker already allocates each unit
from its own source material rather than the largest texture across the bridge.

## Native input evidence

The active inventory is
`artifacts/uv-density/structure-tessellated-sources.json`. Native backing-file
evidence is in
`artifacts/prepared-materials/Draw02/unimodular-native-inputs.json`, read directly
from the installed game's Game/BridgesAndPorts content archives through
`tools/PreparedStructureFootprint.py`. These are original Surface, VTSurface,
VTTexture and Texture headers, not dimensions inferred from the generated output.

| Original Surface CID | Current units | Actual channels | Size of every channel |
| --- | ---: | --- | --- |
| 436d1828c36d1336cf115c85def2c62c | 38 | BaseColor, Normal, Mask | 512x512 |
| 55697a0c744a3cfa77056b9ff0f93646 | 1 | BaseColor, Normal, Mask | 512x512 |
| 5d98bd98700c73926bb2b3ff69c784e2 | 1 | BaseColor, Normal, Mask | 4096x4096 |
| 8a28ab8d9526e258efbcb81c3bbc936b | 568 | BaseColor, Normal, Mask, Control | 4096x4096 |
| 8f9df730508323eaed9b30749f399ffc | 100 | BaseColor, Normal, Mask | 512x512 |
| 9b0ef24ab46511040acf280f549a3a73 | 35 | BaseColor, Normal, Mask | 512x512 |
| 9f31e9f71c97263c984303131decbfb0 | 7 | BaseColor, Normal, Mask | 4096x4096 |
| a3743232fac2497c278d590f55ae827d | 2 | BaseColor, Normal, Mask | 4096x4096 |
| a4e1a84902d9065eeb75df9b18c9a746 | 23 | BaseColor, Normal, Mask | 4096x4096 |
| af48c4b16775963b7d025f9f672722f1 | 3 | BaseColor, Normal, Mask | 4096x4096 |
| d04355d2a678a0a396fe017b043e4c02 | 72 | BaseColor, Normal, Mask | 512x512 |
| dee7bfe689bf6752861f0f6f29a6c8e2 | 10 | BaseColor, Normal, Mask | 4096x4096 |
| eb151a8b50ea389b26d394f3c78290fb | 86 | BaseColor, Normal, Mask | 512x512 |
| f1e512a5deebd43bbbe4bcf7b84a97a6 | 7 | BaseColor, Normal, Mask | 4096x4096 |

There are 43 material/channel bindings before deduplicating shared source backing
files. Source metadata can describe additional optional layers, but absent native
layers are not allocated by MaterialSource. In particular, the main bridge's
authored Control channel is a real 4096x4096 input; it is not a tiny fallback texture.

## Decoder and allocation trace

`PrepareStructure.MaterialSource` resolves active VTSurface layers first, resolves
remaining nonzero VTTexture references from the native Surface header, then loads
additional direct Texture bindings. It does not invent textures for absent native
layers. The measured dimensions correspond to exactly those inputs.

`MovableVtTextureSource.ReadSurface` reads stack width and height at native header
offset `26 + stack*40`, selects active layer flags, reconstructs full-resolution
mip-zero blocks and uploads at those dimensions. `ReadTexture` likewise uses the
individual VTTexture header's width and height. Both call Upload with
IgnoreMipmapLimit, so a quality-biased display texture is not substituted.

`MovableTextureSource.Read` uses the direct Texture header's width, height and mip
count, validates payload sizes, and uploads with IgnoreMipmapLimit. It does not
resize a channel. The offline authoring copy only keeps CPU bytes readable for
fingerprinting; it does not alter these dimensions.

The current PrepareStructure code calls MovableStructurePacking.Pack once per
unit with all actual channel dimensions. Cell.Map is identity, and SampleX/Y
depend on its W/H. Since every channel dimension vector in that call is identical,
the componentwise maximum used by MovableTextureFootprint is the same as any
individual channel's density. Running the same calculation separately would yield
the same BC7-aligned W/H, sample centers, complete mip chain and encoder inputs.
Thus no small channel is being enlarged to another channel's resolution here.

## Equivalence and scope limits

For this bridge the proposed per-channel-size implementation would be a no-op if
implemented correctly, preserving density and bytes while adding complexity.
The zero-byte result follows from identical source dimensions and the unchanged
deterministic allocation/encoding path; no new assets were generated to measure it.

For another bridge with genuinely different channel dimensions, retaining the
same normalized lookup does not alone establish identical filtered appearance.
Different texel-center grids, BC7 block alignment, mip counts, implicit LOD
selection, source sampler behavior and normal-map filtering would need separate
checks. This audit establishes neither safety nor benefit for those other inputs.
It also does not treat downsampling an authored constant-looking channel as
equivalent to allocating it at its original resolution.
