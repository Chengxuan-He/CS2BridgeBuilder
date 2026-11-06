# Draw01 sidewalk composition repair

## Cause and implementation

The road conversion previously treated any Piece with a PedestrianLane as a sidewalk.
The generator now uses the original section occurrence supplied by native composition:
only the Sidewalk section family qualifies. Curbs no longer acquire grating merely because
their lane definitions allow pedestrians. This classification changes appearance and motion
partition selection, not native lane definitions or traffic permissions.

Fixed mechanism platforms previously retained the prototype sidewalk slabs at their original
width. Their measured road faces are now replaced by the selected road geometry, including
the fixed portions of actual sidewalks. The existing measured material domains supply opaque
paving at fixed ends and grating on moving sidewalks. The source composition supplies independent
left/right widths and positions; no sidewalk strip is fabricated when its section is absent.

## Structural scope

The removal plan uses the existing explicit PlatformFaces and carriageway lists for each LOD.
Native source OBJ hashes match the recorded extraction. Their union matches the original reviewed
fixed-road membership: 124, 102 and 36 triangles for LOD0, LOD1 and LOD2 respectively.
Platform subsets contain 92, 70 and 28 faces. None intersects the recorded control-building set.
Measured fragments retain the structural portions of merged far-LOD faces rather than restoring
whole sidewalk platforms. Supporting walls, control buildings, outer safety structure and Y/Z
coordinates remain governed by their original structural plans.

The same PrepareRenderLevels and mechanism plan serve preview and persisted generation at all
three LODs. No material, texture, independent marking object or additional Prefab is introduced.
The change is limited to Draw01; other style generators are unchanged.

## Verification limits

Source geometry membership and caller paths were inspected. Compilation and deployment are recorded
in the delivery report. Human verification of newly generated no-sidewalk, one-sidewalk and unequal-
width sidewalk roads remains required, at near and far distances and in open/closed poses. Existing
screenshots show the previous implementation and cannot validate this revision.
