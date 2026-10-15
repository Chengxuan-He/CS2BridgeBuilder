# Draw01 ordinary-pillar column assembly

The ordinary network pillar is distinct from the crossing mechanism's stacked column.
Native `DrawBridge01NetPillar` references a top, a centred cylindrical column and a footing.
This increment implements only the centred column render graph, not the complete pillar or generator.

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

The final generator has not yet called this assembly. Ordinary-pillar top/footing conversion,
mechanism footing, complete selected-road/marking assembly and generator routing remain outstanding.
This addition changes no existing routed generator output and therefore removes no existing bridges.
Build/deployment establishes compilation and installed-file equality, not in-game acceptance.

Native inspection outputs: `Draw01-native-ordinary-pillar.png` and
`work/bridge-research/draw01-netpillar-inventory.json` in the chat workspace.
