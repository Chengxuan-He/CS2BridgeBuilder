# Movable lane deformation check

The track regression also links the production `BridgePreviewLanes.Lane`
snapshot and real game `BatchDataHelpers`. It checks node/edge/elevation/fallback
selection, native shared-endpoint factors, node scale and unprojected rail and
sleeper heights for ordinary and tiled geometry. The height fixture is synthetic;
it proves preservation of the supplied native datum, not visual acceptance.

Run against the installed game's managed math library:

```powershell
dotnet run --project tools/MovableLaneDeformationCheck -c Release "-p:GameManagedDir=<game>/Cities2_Data/Managed"
```

This executable links the production deformation evaluator and uses actual
Unity Vector/Matrix/Mathf types. Only the input Batch and Vertex containers are
substituted; it does not construct a game world or invoke native rendering.

The cable fixture follows native LaneSystem's equal-height anchor construction:
control-point sag is horizontal chord length times hanging ratio times 4/3.
An independent analytic parabola checks 17 vertices for each combination of
three lengths, three hanging ratios, two directions and both values of the
HangingSwaying flag (612 vertices). It checks endpoint positions, single
application of sag, tangent/normal normalization and orthogonality, handedness,
dilated UV representation and continued rejection of decal volumes.

The pre-repair evaluator fails this fixture with NotSupportedException. The
fixture exercises the neutral-wind closed pose, not time-varying wind. It does
not prove cable attachment placement, material conversion, bone assignment,
LOD selection, persistent loading or game visual acceptance.
