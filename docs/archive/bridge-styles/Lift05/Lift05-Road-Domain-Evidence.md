# Lift05 road-domain evidence (2026-10-20)

## Source and method

Decode the three native LiftBridge05 main Geometry files from game 1.6.2f1.
Check each SHA-256 against the retained native surface-candidate inventory and
compare the decoded topology with the bone-group OBJ extraction. Source files
remain unchanged. Inspection is offline; none of these coordinate predicates
may become a runtime classifier.

Reproduction scripts in the research workspace:
`lift05_road_domains.py` and `lift05_road_strips.py`.
The first produces `lift05-road-domain-review.json` and a three-LOD plan view,
`lift05-road-domain-review.png`, inspected during this increment. These are
native-source diagnostics, not generated bridge acceptance images.

## Longitudinal motion domains

Detailed fixed road sheets use bone 0, X from -4 to +4 m, Y =
9.800048828125 m, and absolute Z from 62.580078125 to 84 m.
Both near and middle contain 96 corresponding horizontal faces and projected
area 342.71875 square metres.

The central moving carriageway uses bone 2, X from -3 to +3 m, Y =
9.800048828125 m in the near source, and Z from -62.4609375 to
+62.4609375 m. Its projected area is 749.53125 square metres. The gap
between the moving end and detailed fixed road is 0.119140625 m on each end.
These are Lift05 measurements; Lift01 bone indices and seam constants are
inapplicable. Lateral shoulder and structural surfaces are separate domains,
not evidence that the carriageway is 10 m wide.

## Middle-LOD sloped faces

An exact horizontal-plane filter finds only 121 central road faces with area
712.1484359687383 square metres in LOD1. Seven more native upward road faces
complete the same projected carriageway area as LOD0 and LOD2:

`14720, 14721, 14722, 16639, 16640, 16657, 16658`.

They remain entirely on bone 2, within the same central X/Z envelope. Their
outer vertices at X=+3, Z=20 and X=-3, Z=-60 reach Y=9.7607421875 m;
the other road vertices retain Y=9.800048828125 m. Their projected area is
37.38281403126166 square metres. Thus the full middle carriageway contains
128 faces, like the near source, while the far carriageway contains four.
A horizontal-plane-only road-removal mask would leave these seven original
road faces behind. Future removal tables must include their exact face IDs
and preserve unrelated side/underside geometry.

## Far fixed-end incompatibility

The LOD2 fixed-end upward sheet is at Y=10 m, spans X +/-10.699951171875 m,
and reaches absolute Z=61.4501953125 through 84.25 m. Its twelve faces and
946.642822265625-square-metre projected area combine road and outer structural
regions. It is not equivalent to the detailed fixed road. Removing all twelve
faces would remove unrelated side regions; retaining the entire sheet would
place it above the replacement road datum by 0.199951171875 m. A measured
part/domain reconstruction is required before the Lift05 render assembler can
be considered complete.

The moving far sheet also includes outer structural strips extending to
X +/-5.042724609375 m. These must not be confused with the central X +/-3 m
carriageway or used as its width reference.

## Status

This increment establishes source geometry evidence and the next implementation
requirements. It does not add a generator route, road-removal table or material
conversion. Sidewalk identity, shoulder/rail replacement, native UV/material
ownership, far fixed-end reconstruction and full structure widening remain
unfinished. No game operation was performed. Human batch acceptance remains
pending. There are no changes to user-visible strings or localization behavior.

## Implemented immutable top masks and motion partition

`Lift05RoadTops.py` now re-decodes native binary32 positions and indices, verifies
source hashes and exact correspondence with the bone-group OBJ, and emits
`Lift05RoadTops.g.cs`. Moving carriageway removal contains 128/128/4 faces across
the three LODs, including all seven middle-LOD slopes. Detailed fixed removal
contains 96 faces per near/middle level. Every entry includes its source triangle
and vertex indices; runtime detects a mismatched input rather than rediscovering
surface membership. No vertices or receiver triangles are cut by these masks.
Side/underside faces remain; the far composite fixed sheet is deliberately handled
by a separate, still-pending reconstruction stage rather than a broad deletion.

`Lift05RoadPartitions` records the detailed fixed/moving/gap stations and original
bone indices 0/2. It is a carriageway motion partition, not a lateral sidewalk mask.
The forthcoming render/road assembler must consume both helpers; they are not a
complete Lift05 generator and do not yet change generated bridge output.

Validation: native topology, face ownership, positive projected area and measured
station checks pass for each emitted mask; Release compilation, 25 generator routes
and all 12 UI languages pass. No new user-visible text was introduced. Full structure,
appearance, far fixed-end reconstruction and generator integration remain required.
