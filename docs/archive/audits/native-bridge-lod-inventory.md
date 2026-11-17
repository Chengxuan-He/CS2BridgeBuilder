# Installed non-mod bridge LOD inventory

## Evidence scope

This is an offline installed-resource inspection, not game rendering or performance acceptance. The source inventory covers 30 styles: 11 base-game, 18 Bridges & Ports and 1 San Francisco Set. Seven mod styles are excluded. The inventory contains 44 main-component rows; variants and conditional pieces are alternatives, not additive whole-bridge totals.

The original delivered JSON is named `native-bridge-lod-counts-20261117.json`, SHA-256 `5799e03de67a26e9216852bd3e395c9cd0eec788e20942d66445dae7a048dc66`. Its paired CSV enumerates conditional pieces; these rows must not be summed as one bridge. Development summary: `work/native-bridge-lod-20261117/main-summary.md`.

Inspected `resources.assets`: 229502992 bytes, SHA-256 `3840f65ca2b0bfae1bcb4e38369e24851dca670b656f3a7bcf6c6c33369e10e6`. Game.dll SHA-256 `f076df4b70aa0f21a80345e03a052ca2273bc6754f41dab7e63d837837a43745`. The recorded method is UnityPy native typetree extraction without a game process; 2840 nodes were freshly read, with 0 differences from the index. The raw developer installation path is not a project path requirement.

## Counting and interpretation

Counts are source-native index counts expressed as triangles per unique mesh. Missing lower entries mean no independent authored mesh, not zero faces; native composition may reuse the existing mesh. Repeated spans, mirrored instances, stacked supports and road variants require actual placement multiplicities. Main structures may contain multiple authored members in one mesh. Mesh longitudinal bounds and network reference length are different quantities. These data do not establish a universal budget or a 5,000-triangle ceiling. See the [LOD specification](../../specs/lod-spec.md#per-level-triangle-disclosure-and-reference-scope).

## Main-component triangle counts

Each row is one inspected component; slash-separated values are its authored LOD sequence. No row claims a whole instantiated bridge total.

| Style | Component | Authored LOD triangles |
| --- | --- | ---: |
| ArcPedestrian | Pedestrian Arc Bridge Piece | 4960 |
| CoveredPedestrian | Pedestrian Covered Bridge Piece | 3560 |
| PedestrianDraw01 | PedestrianDrawBridge01 Mesh | 27727 / 16589 / 1068 |
| PedestrianDraw02 | PedestrianDrawBridge02 Mesh | 30076 / 18037 |
| Draw01 | DrawBridge01 Mesh | 16004 / 9599 / 656 |
| Draw02 | DrawBridge02 Mesh | 24698 / 14800 / 548 |
| Draw03 | DrawBridge03 Mesh | 127508 / 76472 / 448 |
| Lift01 | LiftBridge01Net Mesh | 71900 / 24540 / 1020 |
| Lift01 | LiftBridge01 Mesh | 380558 / 228334 / 776 |
| Lift02 | LiftBridge02 Mesh | 134086 / 80445 / 2600 |
| Lift03 | LiftBridge03Net Mesh | 56064 / 31916 / 5862 |
| Lift03 | LiftBridge03 Mesh | 383528 / 230116 / 852 |
| Lift04 | LiftBridge04 Mesh | 36718 / 22023 / 1736 |
| Lift05 | LiftBridge05 Mesh | 31134 / 18628 / 1824 |
| WoodenCovered | 2-Lane Wooden Covered Bridge Piece | 2776 |
| CoveredWood | PedestrianBridgeCoveredWood01Cover Mesh | 5294 / 3175 / 224 |
| Suspension01 | SuspensionBridge01MiddleNet Mesh | 92302 / 50186 / 2195 |
| Suspension01 | SuspensionBridge01EndNet Mesh | 100886 / 53970 / 2353 |
| Suspension02 | SuspensionBridge02Net Mesh | 191248 / 73552 / 2068 |
| GoldenGate | Golden Gate Bridge Middle Support | 21440 |
| GoldenGate | Golden Gate Bridge Middle Cables | 111496 |
| GoldenGate | Golden Gate Bridge End Support | 11792 |
| GoldenGate | Golden Gate Bridge End Cables | 61492 |
| SuspensionGolden | SuspensionBridge03SupportMiddle Mesh | 137332 / 82393 / 4294 |
| SuspensionGolden | SuspensionBridge03SupportEnd Mesh | 53034 / 31820 / 2270 |
| Suspension | 2-Lane Suspension Bridge Piece | 17068 |
| Suspension | 3-Lane Suspension Bridge Piece | 17068 |
| Suspension | 4-Lane Suspension Bridge Piece | 17068 |
| Suspension | 5-Lane Suspension Bridge Piece | 17068 |
| Extradosed01 | ExtradosedBridge01PillarCablesNet Mesh | 107348 / 63740 / 3544 |
| Extradosed02 | ExtradosedBridge02PillarCablesNet Mesh | 128824 / 73612 / 4306 |
| Extradosed03 | ExtradosedBridge03PillarCablesNet Mesh | 30908 / 21404 / 6022 |
| ExtradosedLarge | 6-Lane Extradosed Bridge Piece | 660 |
| CableStayed | Pedestrian Cable Stayed Bridge Piece | 1600 |
| CableStayed | 8-Lane Cable Stayed Bridge Piece | 3160 |
| TrussArch01 | TrussArchBridge01Net Mesh | 123132 / 74196 / 5002 |
| TrussArch02 | TrussArchBridge02Net Mesh | 161186 / 80678 / 4806 |
| TrussArch03 | TrussArchBridge03Net Mesh | 25138 / 15079 / 2236 |
| TrussArch | 2-Lane Truss Arch Bridge Piece | 5360 |
| TiedArch | 4-Lane Tied Arch Bridge Piece | 5656 |
| Grand | Grand Bridge Middle Support | 27897 |
| Grand | Grand Bridge Middle Cables | 104996 |
| Grand | Grand Bridge End Support | 21193 |
| Grand | Grand Bridge End Cables | 81372 |
