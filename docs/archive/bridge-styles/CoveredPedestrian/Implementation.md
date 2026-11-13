# Covered Pedestrian Bridge implementation evidence

## Source and scope

The fixed `CoveredPedestrian` style reconstructs the native `Covered Pedestrian Bridge`
pathway construction. `BXP Covered Pedestrian Bridge Revised` shares its structural roof;
its alternate lighting is not a second structural family. This is distinct from the
wooden covered bridge. Native source resources remain prerequisites, but the source
network prefab is not used to reconstruct construction parameters at runtime.

The native geometry is `PedestrianBridgeCovered_821820996088a0941bca40147a7b74a1.Geometry`,
CID `fb756834d6da50c0bce6692b1bfafb2e`, SHA256
`675f8f57d39bbe1b161c81004bdb1f28465031c96e4b0e3ca0b388c365cde69f`.
The original material CID is `5d2454994f1ec785bae61f9314cd0975`.
The source has one mesh, 4,008 vertices and 3,560 triangles; the native piece has no
external LOD references. No replacement LOD or texture is fabricated.

## Recorded construction

The source road width is 3 m; nominal overhead piece width is 7 m and length is 64 m.
Actual decoded structural bounds are X [-3.26116943359375,3.26116943359375],
Y [0.10001754760742188,3.1500244140625], Z [-32,32] m. Nominal composition width
and physical mesh width are deliberately recorded separately.

The bridge has segment length 32 m, zero hanging, 5 m water elevation, no curved or
minimal-length construction, Any water flow, Elevated build style, and no fixed segments.
The overhead piece is median, Edge-only, Top layer, with disabled texture tiling.
The generic native pedestrian pillar placeholder remains a native reference, placed at
a node at (0,-3,0), identity rotation, elevated-only, without top or center anchoring.
Its existing native replacement graph is preserved. No new pillar scaling is introduced.

## Geometry recipe

Offline measurement identifies 36 welded components: nine inclined post frames,
seven transverse roof bows, three longitudinal purlins, sixteen side longitudinal ties,
and the roof shell. Complete side posts translate rigidly. Their crossbeam endpoint
vertices receive the corresponding endpoint displacement. The flat central roof crown
spans the measured +/-2.48333740234375 m stations; its independently curved eaves
translate rigidly. Side ties and outer purlins translate; the center-crossing purlin
uses its own measured span under the default crossing-member rule.

Immutable per-vertex displacement and derivative tables are BBN1 embedded resources.
The shared packed XYZ executor applies them. Native topology and source material/UV
channels remain; the shared factory applies recorded frame derivatives. Original atlas
coordinates are retained rather than baking structural textures; stretching changes
physical density on stretched domains. Native shaded appearance remains unverified.

## Reproduction and checks

Run `tools/GeometryMetaprogram/CoveredPedestrian.py` with the installed `Cities2_Data`
directory and an independent output directory. It verifies the exact source hash,
extracts temporarily, emits both numerical resources and JSON evidence, and deletes
temporary geometry on completion or failure. Compare emitted binaries with
`assets/HardcodedNumbers/CoveredPedestrian/`.

The source-level check passes widths with extra spans 0, +2, +10 and -2 m. It verifies
identity at zero, unchanged Y/Z bits, constant inclined-post displacement per side,
source topology counts and zero coefficient mismatches at every coincident seam.
An initial UV-face-based shell partition produced 80 seam mismatches and was replaced
by the authored crown/eave classification before delivery.

## Pending native acceptance

Compilation and the offline checks do not establish native appearance, placement,
frame/UV quality, LOD behavior or cold loading. Human samples must verify the roof and
center purlin at original and wider widths, asymmetric/no-sidewalk roads, preview and
both creation actions, near/far rendering and independent cold loading. The contract's
actual generated-versus-prototype width audit remains pending until such a sample exists.
Localization and shared build/deployment evidence are owned by the integration task.
