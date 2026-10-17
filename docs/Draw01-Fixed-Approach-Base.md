# Draw01 fixed approach base repair

## Cause

The generator installed the native ordinary pillar at its recorded network offset
`(0, -4, 0)` / `(0, -4, 3)`, but retained the selected road's shallow ordinary Bottom
layer. The missing structure is the deck-adjacent network base, not the pillar footing.
Native geometry supplies a steel underside reaching approximately -4 m beneath the deck.

## Implemented integration

- Derive the prototype's five Bottom-piece families, retaining all thirteen native geometry
  levels, materials, UVs, components, native costs and unchanged Y/Z coordinates.
- Compose the two inner strips and two outer strips independently of selected traffic and
  sidewalk sections. The zero-width intersection piece retains its recorded conditions.
  Outer strips are structural base geometry, not fabricated pedestrian surfaces.
- Use the ordinary bridge structural overlay route. Replace an existing structural overlay;
  concatenating two overlays would pack them along the same native width cursor.
- Hide selected-road Bottom layers only on elevated fixed branches. Keep ground branches,
  source surface/lane/upgrade behavior and the mechanism's fully hidden moving-node branch.
- Include ordinary run/node pieces and half-length connection edges. Do not install this
  fixed base in the central fixed-size moving node. Preserve native pillar placement.
- Create owned NetPiece roots through the existing render-graph writer, with their native
  network fields and remapped owned LODs. Section occurrences reuse the same generated pieces.
  New roots and geometry use the existing registration/persistence/rollback plan. No materials,
  textures, shaders or independent marking objects are created.

The fixed base uses the general contract's coordinate-displacement rule. Inner seam vertex
membership was recorded from decoded native geometry; runtime does not classify mesh parts.
Existing preview composition includes this structural overlay. Fresh in-game visual acceptance
is still required, including narrower/wider roads and asymmetric road layouts.

## Native geometry inspected

Source: installed game 1.6.2f1, `Content/BridgesAndPorts/Blob.cok`; actual compressed Geometry
payloads decoded offline. Vertex and triangle counts below are source measurements, not
measurements of a newly saved generated bridge.

| Native geometry | Vertices | Triangles | SHA-256 |
| --- | ---: | ---: | --- |
| DrawBridge01Bottom300cm Mesh | 980 | 646 | `cb80148557c410d9213bd28b3a6f18418f7e15916ebd7c8c3f3ff271a0f02fc5` |
| DrawBridge01Bottom300cm_LOD1 Mesh | 692 | 454 | `2580c29fb87c66609b2e54d992ee7687dcf177ea224c1d97bda05c06a1b70e1b` |
| DrawBridge01Bottom300cm_LOD2 Mesh | 188 | 136 | `3d2dbe7018fe08621ba8e2b61f4e7bb69a1081dbcea0254ba50f99671190d4e4` |
| DrawBridge01BottomNode300cm Mesh | 40 | 32 | `4ff24779d839f27aaf01925d0689990364d19003813c5cee2bfc3bd404f306db` |
| DrawBridge01BottomNode300cm_LOD1 Mesh | 40 | 32 | `4ff24779d839f27aaf01925d0689990364d19003813c5cee2bfc3bd404f306db` |
| DrawBridge01BottomNode300cm_LOD2 Mesh | 36 | 32 | `637880cc7b6c87721426a0a366ec834f2b7aaf6adddf9715f786a1eff0e53140` |
| DrawBridge01BottomEdge350cm Mesh | 1232 | 798 | `46399aa7cf9f19c573325102ebf4b5f30b57bd0a4aae09b579f1a7ae0896f394` |
| DrawBridge01BottomEdge350cm_LOD1 Mesh | 942 | 604 | `cbeb0b62273394ff0fb6e5625a2b6635ad2a9ad377de5700de294db3578f6951` |
| DrawBridge01BottomEdge350cm_LOD2 Mesh | 304 | 240 | `94650a3f1f611a224535ec3a49dfd5ce22127aa4ba280677e137d647b0da1e97` |
| DrawBridge01BottomEdgeNode350cm Mesh | 584 | 460 | `84d6693fff030a021e2b224f9b897d484cac58179db7ba723cb74b114e8e040d` |
| DrawBridge01BottomEdgeNode350cm_LOD1 Mesh | 464 | 364 | `0905aad42e2be9813c94e6b1ab2a4aa02932324945e7af0dd28662ccfbd7b242` |
| DrawBridge01BottomEdgeNode350cm_LOD2 Mesh | 188 | 164 | `67bfc5c8151699dd5b08b5b6e9575bbb061767acff00ac53ae068aed094e96ca` |
| DrawBridge01BottomIntersectionMiddle200cm Mesh | 62 | 63 | `3eb9bd582436b3d40d4263d4ae1205d84a5a5c375a16e8bc24ae1b2cacca852e` |

## Validation and remaining acceptance

- Release compilation succeeded; all 12 installed payloads match build output.
- Installation backup contains 18 original files only:
  `artifacts/backups/draw01-fixed-base-overlay-20261017-122847`.
- Style commits: `5968ded`, `0c645ac` on `bridge/draw-01`; integrated into the `dev`
  working tree alongside the shared NetPiece-aware writer change. Unrelated edits remain intact.
- Source review traced native overhead composition, preview composition, owned graph adoption,
  LOD remapping and normal persistence. No visual unit test is claimed as acceptance.
- Existing bridge assets were not rewritten or removed. The previously rejected cleanup action
  was not bypassed. Create a fresh Draw01 sample to evaluate the changed generator.
- Pending: near/far views of ordinary ends and connecting spans, base-to-pillar contact,
  opening clearance, supported road widths/asymmetry, and cold loading of the new saved graph.
  Compilation and decoded prototype geometry do not establish those results.

The movable generation contract now explicitly requires all non-moving parts to obey the
[general bridge generation contract](agent-contract/BRIDGE_GENERATION_CONTRACT.md).
