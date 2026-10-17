# Divided Road Median Role Fix

## Reported failure

At 2026-10-17 00:23:21, Medium Road Divided / Draw01 preview failed before render assembly:

```text
No measured road-surface role for 'Median Piece 2', geometry '61ff20f7140db6ddb6fe95db7533d170', mesh 0, submesh 0, face 48.
```

The failure originates in Draw01RoadPartitions.ForRoad. The UI message is only the outer failure notification; the camera framing change is not the failing stage.

## Native evidence

Direct inspection of installed resources.assets identifies Median Piece 2 as a surface-layer NetDividerPiece with BlockTraffic and BlockCrosswalk enabled. Its authored width is 2 metres. Its raised divider and lower margins occupy the central carriageway assembly, not the independently partitioned outside pedestrian strip. The material is the ordinary native Road surface.

The native geometry was decoded from Content/Game/Blob.cok, entry `Median2_e3b52de5912024844ae963ad2da94ac6.Geometry`:

- Geometry CID: `61ff20f7140db6ddb6fe95db7533d170`.
- SHA-256: `7377a7dddebfeb6113303793279e42be9ae31b01278912641b970760b0047a93`.
- One mesh; 252 vertices; 672 indices; 224 triangles.
- Raised profile spans approximately X=-0.6 to +0.6, with lower margins reaching X=-1 and +1. These measurements are offline evidence, not runtime classification thresholds.
- Object producer rule 0 references Street Light Double Placeholder at the centreline.

Median Intersection Piece 2 has geometry CID `dac11ed9cbc727bc8dcb1c6982d81ca4`, already covered by the existing 63-triangle intersection mapping. No duplicate case is needed.

## Change and compatibility

MovableNativeRoadRoles now explicitly maps the median's faces 0 through 223 to Carriageway and its exact rule-0 double-light producer to the same role. Unknown geometry and unmatched producer identities still fail explicitly. Runtime classification uses the recorded CID/producer identity; it does not infer roles from height, material, bounds or a partial name.

The existing carriageway partition supplies fixed regions, leaf bones and expansion gaps. Existing streetlight handling retains the selected model, lateral placement and orientation while using the previously approved native longitudinal mounting stations. The same road mapping is used for all prepared road LODs and both preview and formal generation. No material, texture, shader, geometry-widening rule or persistence layout changed in this fix.

The shared role table is consumed only by Draw01RoadPartitions in the current source tree. Other styles are unaffected by this addition.

## Verification and limits

Release build and deployment succeeded. All 12 installed payload files matched the build outputs. The previous 18 installed files were backed up and verified under `artifacts/backups/median-divider-role-20261017-002756`.

No synthetic visual tests or game automation were run. The reported missing mapping is addressed in code; a human must retry Medium Road Divided / Draw01 to verify subsequent generation, preview and animation. This does not close the earlier material-reuse or sidewalk appearance issues.

Existing generated bridges were not removed. The earlier UUID cleanup operation was rejected by execution policy; this turn did not attempt to bypass that rejection. Style-scoped cleanup therefore remains outstanding.
