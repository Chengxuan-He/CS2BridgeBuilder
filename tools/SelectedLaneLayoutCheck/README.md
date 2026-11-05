# Selected native lane layout

`MovableNetworkContext.CompileRoad` snapshots longitudinal `SubLane` curves from the
existing elevated private context. `PrefabRef -> NetLaneData` supplies width and lane
flags. Node endpoint widths use the actual game's `BatchDataHelpers.BuildCurveScale`
and the nominal width. Master, hidden, deleted, virtual and utility lanes are not physical
longitudinal railing candidates. The existing `CarLaneCenters` behavior is unchanged.

`MovableSelectedRoadLayout.Lanes` exposes object-coordinate lane metadata:

- `Width` is the original prefab nominal width; `StartWidth`/`EndWidth` are evaluated
  native endpoint widths, ordered by increasing `StartZ`/`EndZ`.
- `Type` distinguishes car, bicycle-only, pedestrian, parking, track and other lanes.
  Flags may combine; `NativeFlags` retains the original bit field.
- `LeftAt(z)`, `RightAt(z)`, `CenterAt(z)` and `WidthAt(z)` evaluate a covered station.
  The retained Bezier parameter, rather than normalized endpoint distance, determines
  the interpolation between native endpoint widths. Reverse-direction lanes retain
  their original curve parameter and width ordering.
- `Center`, `Left`, `Right` summarize the native curve midpoint; they do not authorize
  extending a lane over the complete bridge. `ConstantCrossSection` identifies the
  constant case.
- `LaneStations` contains the sorted native longitudinal endpoints. Consumers split
  their applicability at these stations and intersect with their own measured physical
  part ranges. They must not extrapolate a centre sample over the entire leaf.
- `LaneBoundariesAt(z)` returns car-facing boundaries at that station. `CarCar` is true
  only when native car-lane edges meet without another lane crossing that boundary.
  Positive gaps remain two separate outer boundaries, including arbitrarily small gaps.
  `NonLaneGap` means no lane occupies that interval, not an invented median type.
  Parking, track, bicycle and pedestrian regions never become car-car adjacency.

This metadata does not reselect road branches, infer lanes from paint or mutate selected
road assets. It currently covers longitudinal node/edge lanes selected by the existing
straight-road sampling filter; transverse intersection connectors are not railing runs.
The native engine's endpoint-scale API is used directly. In-game ECS extraction and
the bridge consumer's exact railing/width interpolation remain part of integration
acceptance; controlled numeric tests alone are not a saved-road replay.

Run `dotnet run --project tools/SelectedLaneLayoutCheck` for source-linked interval,
gap, overlap, direction and non-uniform-Bezier parameter cases. No user-visible strings
are introduced; the twelve-language localization check remains applicable.

## Multiple measured supplements

The road `MovableRoadAssets.Level` constructors accept optional
`additionalStructures: IReadOnlyList<MovableRoadAssets.Supplement>` after the existing
`extraStructure` argument. Each supplement retains the existing `(Source, Mesh, Plan)`
contract. The collection is snapshotted; the legacy singular supplement is processed
first, then each additional item in order through the same raw-attribute build, retile,
owner validation and resource lifetime path. `MovableRenderAssembly` carries both
forms through its level reconstruction. No style geometry or original attribute is
fabricated by this interface.
