# PedestrianDraw02 exact top boundary loops

## Extraction and coverage

Boundary edges were extracted from the exact-position welded edges of the previously inspected Y=10 upward candidates. All six bone/LOD groups have boundary vertex degree two and no edge incident to more than two candidate triangles. These are boundaries of the selected candidate faces, not a classification of excluded sloped faces or all authored structure.

| LOD | Bone | Boundary edges | Closed loop lengths, excluding repeated endpoint |
| --- | --- | --- | --- |
| 0 | 0 | 68 | 8, 26, 26, 8 |
| 0 | 1 | 32 | 32 |
| 0 | 2 | 32 | 32 |
| 1 | 0 | 62 | 5, 26, 26, 5 |
| 1 | 1 | 35 | 35 |
| 1 | 2 | 33 | 33 |

Near LOD has one 32-edge loop per moving leaf. Fixed candidates form four loops: two 26-edge platform outlines and two separate eight-edge islands. Far candidates have different boundaries because the exact-horizontal filter excludes some simplified faces. These far-filter boundaries must not be copied as physical road holes.

## Near moving leaf outlines

All coordinates below have Y=10. Each list closes back to its first point. Decimal values preserve the decoded source positions; no tolerance-based simplification or symmetry replacement was applied.

### Bone 1

| Vertex order | X | Z |
| --- | --- | --- |
| 0 | -3.68646240234375 | 0.030002117156982422 |
| 1 | -3.68646240234375 | 2.2608642578125 |
| 2 | -3.68646240234375 | 9.05517578125 |
| 3 | -3.68646240234375 | 15.849609375 |
| 4 | -3.68646240234375 | 22.64404296875 |
| 5 | -3.68646240234375 | 29.4384765625 |
| 6 | -3.68646240234375 | 36.2333984375 |
| 7 | -3.68646240234375 | 43.02734375 |
| 8 | -3.68646240234375 | 49.822265625 |
| 9 | -3.68646240234375 | 56.6162109375 |
| 10 | -3.68646240234375 | 59.3623046875 |
| 11 | -3.68646240234375 | 64.470703125 |
| 12 | -1.9168701171875 | 64.470703125 |
| 13 | -1.9168701171875 | 59.3564453125 |
| 14 | -1.00000761449337e-06 | 59.3564453125 |
| 15 | 1.9168701171875 | 59.3564453125 |
| 16 | 1.9168701171875 | 64.470703125 |
| 17 | 3.68646240234375 | 64.470703125 |
| 18 | 3.68646240234375 | 59.3623046875 |
| 19 | 3.68646240234375 | 56.6162109375 |
| 20 | 3.68646240234375 | 49.822265625 |
| 21 | 3.68646240234375 | 43.02734375 |
| 22 | 3.68646240234375 | 36.232421875 |
| 23 | 3.68646240234375 | 29.4384765625 |
| 24 | 3.68646240234375 | 22.64404296875 |
| 25 | 3.68646240234375 | 15.849609375 |
| 26 | 3.68646240234375 | 9.05517578125 |
| 27 | 3.68646240234375 | 2.2608642578125 |
| 28 | 3.68646240234375 | 0.029992103576660156 |
| 29 | 1.9114990234375 | 0.02999401092529297 |
| 30 | 0.0 | 0.0300140380859375 |
| 31 | -1.9114990234375 | 0.030004024505615234 |

### Bone 2

| Vertex order | X | Z |
| --- | --- | --- |
| 0 | -3.68646240234375 | -64.470703125 |
| 1 | -3.68646240234375 | -59.3623046875 |
| 2 | -3.68646240234375 | -56.6162109375 |
| 3 | -3.68646240234375 | -49.822265625 |
| 4 | -3.68646240234375 | -43.02734375 |
| 5 | -3.68646240234375 | -36.232421875 |
| 6 | -3.68646240234375 | -29.4384765625 |
| 7 | -3.68646240234375 | -22.64404296875 |
| 8 | -3.68646240234375 | -15.849609375 |
| 9 | -3.68646240234375 | -9.05517578125 |
| 10 | -3.68646240234375 | -2.2608642578125 |
| 11 | -3.68646240234375 | -0.029988765716552734 |
| 12 | -1.9114990234375 | -0.029990196228027344 |
| 13 | 0.0 | -0.03001117706298828 |
| 14 | 1.9114990234375 | -0.030001163482666016 |
| 15 | 3.68646240234375 | -0.029998779296875 |
| 16 | 3.68646240234375 | -2.2608642578125 |
| 17 | 3.68646240234375 | -9.05517578125 |
| 18 | 3.68646240234375 | -15.849609375 |
| 19 | 3.68646240234375 | -22.64404296875 |
| 20 | 3.68646240234375 | -29.4384765625 |
| 21 | 3.68646240234375 | -36.2333984375 |
| 22 | 3.68646240234375 | -43.02734375 |
| 23 | 3.68646240234375 | -49.822265625 |
| 24 | 3.68646240234375 | -56.6162109375 |
| 25 | 3.68646240234375 | -59.3623046875 |
| 26 | 3.68646240234375 | -64.470703125 |
| 27 | 1.9168701171875 | -64.470703125 |
| 28 | 1.9168701171875 | -59.3564453125 |
| 29 | -4.00003045797348e-06 | -59.3564453125 |
| 30 | -1.9168701171875 | -59.3564453125 |
| 31 | -1.9168701171875 | -64.470703125 |

## Integration boundary

The closed loops provide more information than extrema: they retain the central gap, notched hinge end, and small diagonal transitions. They do not supply authored-part widening masks. A runtime construction partition must be based on reviewed loop/part correspondence and the same width mapping as the retained structure. Do not silently scale the entire outline or extend every transverse station by the same rule.

No runtime partition, removal mask, generator or installed asset changed. Source hashes are recorded in Pedestrian-native-surface-candidates.md. Intermediate extraction is reproducible with work/bridge-research/pedestrian02_top_boundaries.py and retains numeric loops in pedestrian02-top-boundary-loops.json.
