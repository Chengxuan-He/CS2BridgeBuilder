# Art Deco Viaduct implementation evidence

## Source and grouping

The six-lane and four-track Bridge Expansion Pack viaducts are one structural style. The recorded construction uses `BXP Art Deco Viaduct - 6 Lanes` from installed pack 92245 version 36. Its native road width is 25 m; the decoded pylon bounds are -16.7001953125 to +16.7001953125 m. These are source measurements, not a completed generated-bridge width audit.

The custom `BXP Art Deco Pylon` contains one `ArtDecoPylon Mesh` render and no separate LOD references. Its geometry CID `e2418d88bc0ec23a8ca39af6f6f66dcc` resolves to base-game `Blob.cok` entry `GoldenGatePylon01_32dfda9da5ebfa54bb651a18bb4e33fa.Geometry`. It has 1,610 vertices and 1,022 triangles. The original serialized RenderPrefab surface CID was not resolved from current archive sidecars; runtime uses the resource anchor's actual native Surface rather than inventing a substitute or freezing that unresolved ID.

The native source RenderPrefab and pylon are resource inputs, not a network donor. Derived geometry and object references are owner-local. The original object components and material identity remain unchanged, including native terraforming and content requirements. The original bridge network is not needed by generation. Bridge Expansion Pack resources and the original native content remain prerequisites.

## Recorded construction

The road prototype uses 60 m spans, zero hanging, 10 m water elevation, curves enabled, no fixed segments, elevation range 0–200 m and XP reward 3. The pylon is placed at `(0,7,3)` at nodes. The four bridge-specific native lights retain their original positions, quaternions, node placement and elevated-only condition; their X placement follows the same half-width increment.

The source custom sidewalk piece is ordinary paving with traffic props and street lamps, including `LightpolePark02-Gray`. The selected elevated road retains its own pavement, sidewalk layout, lanes, markings and street lamps. No prototype sidewalk surface is overlaid or fabricated. Separate bridge-owned node lights remain part of the structure.

The native AND unlock requirement and all measured native dependency identities are resolved separately from the original network and passed to the common persisted unlock snapshot path.

## Geometry and frame evidence

`tools/GeometryMetaprogram/ArtDecoViaduct.py` consumes an offline decode of the exact geometry. The source's joined side piers and stepped ends translate rigidly. Its transverse profile bands retain their own measured endpoint reach and centre-axis vertices through the original triangles. The producer emits vertex-ordered displacement and direction-Jacobian data; runtime does not inspect source topology or infer membership. Original indices, source channels, materials and mesh count remain intact.

Each incident triangle's affine displacement derivative was solved offline. Some vertices share distinct derivatives, but comparison using their actual native octahedral normal and tangent channels found equivalent normalized directions at half-width changes -2, +5 and +20 m. The largest absolute difference was 1.0477729794899915e-14 in the double-precision inspection calculation. The frozen source-face representative therefore does not introduce an arbitrary shading seam in these cases. This does not establish equivalence for every signed/degenerate width choice.

Identity at zero width change, unchanged Y/Z, and complete outer-shaft translation passed at half-width changes 0, -2, +5 and +20 m. The native source has no separate LOD chain to widen. The factory preserves its authored placement bounds separately from recomputed indexed submesh bounds.

Raw tokens, source mesh decode, original packed N/T channels, native encoding reference values and inspection images are retained in `artifacts/four-new-styles/ArtDecoViaduct/`. They are diagnostic evidence, not packaged resources.

## Remaining acceptance

Native preview, creation, lighting, arbitrary signed-width appearance, absent/asymmetric sidewalks, game near/far inspection and mod-enabled cold loading remain unverified. The complete generated-owner footprint and real generated bridge width allowance cannot be reported until a human creates a sample. Compilation and offline geometry checks do not establish these results.
