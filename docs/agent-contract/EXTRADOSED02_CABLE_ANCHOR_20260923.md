# A-pylon double-deck cable anchor repair

Branch: `bridge/extradosed-02`; baseline `a48168ebc4dce07b3e139d9f66fdc846942f10c0`.

The native `ExtradosedBridge02PillarCablesNet` geometry and both named LODs were
extracted from BridgesAndPorts/Blob.cok, without changing game assets.
The real generated sample is `b4242a749-eb11-4b81-8e80-e1480a3d78bc`, with a
36 m selected root road and 17 m widening recorded in last-export-report.txt.
Its three geometry files survive in the gray-upper deployment rollback backup.

The native full-detail mesh has 18 tall cable assemblies reaching across x=0.
The general height-sliced profile incorrectly translates their small nonzero
attachment vertices by 8.5 m, tearing the attachment into opposite sheets.
This is not a road-width or tower-placement correction.

The offline metaprogram records exact cable vertices and their own full-detail
span. Runtime applies x' = x + (x / native reach) * half-width delta, centred on
the original x=0 tower axis. It does not move the axis to either tower leg.
Non-cable geometry retains its existing transform. Native y/z, indices,
materials and vertex channel layouts remain unchanged. LOD1 inherits the 18
assemblies; LOD2 splits them into 36 front/back representations and inherits
the corresponding full-detail span rather than reclassifying them.

| Detail | Cable vertices | Native attachment max abs(x) | Old generated max abs(x) | New mapping max abs(x) |
| --- | ---: | ---: | ---: | ---: |
| Full | 5940 | 0.32180023 | 8.8218 | 0.57069606 |
| LOD1 | 3315 | 0.31853485 | 8.818535 | 0.5648832 |
| LOD2 | 648 | 0.33307648 | 8.8330765 | 0.59070534 |

The attachment measurement includes cable surface thickness, not a displaced
centre: the transform fixes x=0 exactly. These are native/sample measurements
and an offline mapping inspection, not visual acceptance. A newly generated
bridge still requires human near/far inspection in game. No visual unit tests
were run.
