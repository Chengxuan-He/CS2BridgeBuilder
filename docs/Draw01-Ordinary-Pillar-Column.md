# Draw01 ordinary-pillar column assembly

The ordinary network pillar is distinct from the crossing mechanism's stacked column.
Native `DrawBridge01NetPillar` references a top, a centred cylindrical column and a footing.
The cap, centred column and footing render graphs and their ordinary-pillar object assembly are
implemented. The complete bridge generator remains unfinished and has not called this assembly.

## Measured column

Original game 1.6.2f1 geometry is inspected at all three LODs. Each is one centred column;
road-width changes do not stretch its cross-section or translate it away from the centre.
All source positions, topology, raw vertex attributes, material references and authored bounds
are preserved. Private geometry and RenderPrefabs still belong to each generated bridge.

| Source render | Vertices | Triangles | Geometry SHA-256 |
| --- | --- | --- | --- |
| `DrawBridge01NetPillar Mesh` | 160 | 128 | `caa2df728cc54a96efcf44d8788fa318630d47ccc1e3174780c7698c1cf55f97` |
| `DrawBridge01NetPillar_LOD1 Mesh` | 148 | 76 | `8099388bb1f41ba4cba3caf9c8154a4e78c5b72a743bc4e618b0cd80f07fb37a` |
| `DrawBridge01NetPillar_LOD2 Mesh` | 210 | 100 | `0f5cb3cb39da2ec4882f422459fb53542f5b5be00270d7c8051aa535d5bcc84f` |

## Implementation and integration

`Draw01NetPillarAssembly.CreateColumn` requires the native root and both LODs, excludes
foreign bridge-owned source paths before geometry reads, and verifies the measured layouts.
It submits three structural-only levels to `TowerFactory.CreateMovableRenderGraph`. That path
allocates independent owned geometry, preserves native components/materials, rewrites LOD
references, and handles rollback through the existing persistence machinery. No synthetic bone,
new material, texture or shader is introduced. Temporary source meshes are released in `finally`.

The final generator has not yet called this assembly. Mechanism footing, complete
selected-road/marking assembly and generator routing remain outstanding.
This addition changes no existing routed generator output and therefore removes no existing bridges.
Build/deployment establishes compilation and installed-file equality, not in-game acceptance.

`Draw01NetPillarAssembly.Create` now invokes all three render builders in dependency order and
passes their private roots to `Draw01PillarData.Create`. The object retains the recorded native
mesh order, zero offsets, identity rotations, PillarObject type, anchor offset, vertical offset
range and content prerequisite. It does not duplicate stack/base/LOD components already copied
by the render graph writer. A failed render builder raises the ordinary operation error before
the object is created; successfully allocated earlier graphs remain in `factory.Created` so the
enclosing preview transaction can adopt and dispose even partial results. The existing save plan
collects these same owned nodes for dependency persistence. Full permanent-generation failure
cleanup and cold-load behavior still require verification with the final generator.

Native inspection outputs: `Draw01-native-ordinary-pillar.png` and
`work/bridge-research/draw01-netpillar-inventory.json` in the chat workspace.

## Ordinary footing

`Draw01NetPillarAssembly.CreateFooting` constructs all three private footing render levels using
`Draw01NetFootingData`. The native footing is a centre-crossing ring with an outer shoulder and
an open inner boundary. It is not the ordinary column or the structure immediately below the
road deck. Source indices, materials and all non-position attributes are retained.

The measured outer half-span is 3.1842041015625 m through Y = 2.001708984375 m and tapers to
2.68353271484375 m at Y = 2.5 m. Offline generation computes each vertex's X displacement
coefficient against this height-dependent authored span. The far model retains that same
profile; its simplified vertices do not redefine the part. Runtime consumes immutable
coefficients and performs no height-based classification or source-span inference.

| LOD | Vertices | Triangles | Geometry SHA-256 |
| --- | --- | --- | --- |
| 0 | 240 | 192 | `876bd56d36c7ac23ba4027c3ababa1093bf794d8fc38c604481571906bc6dc15` |
| 1 | 163 | 115 | `97105c01835bca84d73215eeda6c1c8d8c64ed6e51f7a546b301434e009d024b` |
| 2 | 134 | 100 | `043cc0d4337172c4d8726283212f7936690a8ac8826b5fa4ebe14df499236f9c` |

Authored Y/Z bounds remain unchanged. X culling bounds include the native envelope and every
transformed vertex, so simplified far vertices outside the detailed profile remain covered.
The geometry writer calculates submesh bounds from actual output positions. Native stack/base
components and the remapped LOD graph are carried through the shared render-asset path.

Native topology and 0/+8 m offline width examples were inspected in
`Draw01-native-ordinary-footing-detail.png` and `Draw01-ordinary-footing-width-mapping.png`.
These are untextured geometry inspections, not in-game acceptance. Column/footing contact,
near/far appearance and cold-load behavior still require verification on the complete bridge.
The retained emitter is `work/bridge-research/emit_draw01_netfooting.py` in the chat workspace.

## Ordinary cap

`Draw01NetPillarAssembly.CreateTop` now constructs the three native cap render levels using
`Draw01NetTopData`. The cap is a centre-crossing headstock, separate from the cylindrical column
and from the road deck. Its own outer section supplies the width at each vertex height.
The offline emitter intersects the highest-detail mesh edges with horizontal planes and computes
`X / halfSpan(Y)` coefficients. It emits those immutable coefficients for every source vertex;
runtime does not inspect height bands or infer part membership. Simplified LOD vertices use the
same highest-detail envelope, clamped at its vertical ends for small proxy excursions.

The authored cap occupies Y = -8 through -4 m. Its half-span grows from 2.2508544921875 m
at the bottom to 7.4832763671875 m at Y = -5 m, then remains constant through the top.
Intermediate spans are measured from intersected edges, not inferred from vertices sharing a
height. The cylindrical column remains unchanged by this cap transformation.

| LOD | Vertices | Triangles | Geometry SHA-256 |
| --- | --- | --- | --- |
| 0 | 174 | 116 | `38bf4491b8c33fb363ba9cb107cea177b9cad7a84a8616cb823164726332fe0a` |
| 1 | 139 | 81 | `144bf77d12fc3e578298345d172fd3b7a765c22ae2b647fef12236a08b9538bb` |
| 2 | 160 | 100 | `1b4ccdd7db1a3c14cec4c089ebb457279fe73c09d650c17e2c4489ed3072cac8` |

Source indices, raw non-position attributes and material references are preserved. The shared
render graph writer retains native stack components and remaps the private LOD references.
X bounds include all transformed vertices; authored Y/Z bounds remain unchanged. No shader,
material or texture is added by the cap conversion.

Retained evidence: `work/bridge-research/emit_draw01_nettop.py`,
`work/bridge-research/draw01-nettop-width-evidence.json`, and
`Draw01-ordinary-cap-width-mapping.png` in the chat workspace. Original and +8 m full-width
offline geometry were inspected. Complete stacked-pillar contact, textured appearance,
LOD transitions and cold-load behavior have not yet been verified in game.
