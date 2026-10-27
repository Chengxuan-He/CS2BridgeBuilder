# PedestrianDraw02 material preparation

Use `PrepareSourceInputs.py` to prepare source-level native inputs from the game
archives, independently of all previous generated material libraries:

```powershell
python tools/PreparedMaterials/PedestrianDraw02/PrepareSourceInputs.py . GAME_DATA artifacts/agent-material-env/PedestrianDraw02/source-input
```

`GAME_DATA` is the installed `Cities2_Data` folder. The manifest records seven
render inputs, five original Surface identities, eight pixel payloads and shared
MidMips provenance. The extracted 13 native files total 42,758,491 bytes; these are
read-only authoring inputs, not a proposal to package built-in textures. Compatible
built-in references create no new material or pixels. A required template conversion
must retain the original shared texture graph and be prepared offline.

The public source-level preparation entry point is `tools/Rebuild-NativeMaterials.ps1`.
The verified independent result is under
`artifacts/prepared-materials/PedestrianDraw02/rebuild-native-v1/`: 60 files,
15,078 bytes, 29 Surface descriptors and one copied 22-byte constant Texture,
with their CID sidecars. Five original prototype Surfaces remain direct built-in
references. There is no texture re-encoding. The installed native reader consumed
all 29 descriptors (27 VT); all output hashes and 92 active Surface dependency
edges resolve, with no empty directories. See `report.json`, `dependency-check.json`
and `native-descriptors.log` there. The verified library and fixed-road repeat
unit are in formal `assets/Materials/PedestrianDraw02`: 70 files, 1,052,640 bytes,
including the subsequently verified fixed-road cutout descriptor and its CID.
The exact source-density unit has a production Repeat alias; atlas-edge pattern
differences remain human inspection items under the accepted exception. No game
installation was performed. See the `published-native-v2` closure report for scope.

Do not call the old per-repeat or per-triangle bakers, restore old generated material libraries, or
convert each sampler into a separate image. Native sampling, all mip levels,
normal semantics and per-owner persistence require common integration verification.

`ExportRecipes.py` and `VerifyRecipes.py` identify one reachable fixed prototype
road patch and the unused moving patch. These are measured sampling evidence,
not independent pixel bake jobs. This style retains the selected moving-surface
appearance (`PrototypeRoad` is false). `ExtractNativeCharts.py` and
`GenerateUvCharts.py` produce reliable source-chart boundaries and the formal lazy
BBN chart adapter. See [source mapping](SourceSampling.md) for coverage and limits.

`PedestrianDraw02NativeSampling.cs` is an offline arithmetic experiment only.
It adds zero eligible production cases and is not compiled into the mod.

`ExportRepeatUnitEvidence.py` rechecks the actual fixed-road source faces and
records their measured rectangle/density, all 2,232 old request identities and
native VT/full-mip evidence gaps. Its output is review evidence under
`artifacts/prepared-materials/PedestrianDraw02/repeat-units-v2/`; no candidate is
approved for baking without genuine pattern/boundary/full-mip verification.

No user-visible text changes; localization remains unchanged. No complete native
cold-load or visual acceptance is claimed by these authoring checks.

`AuditCurrentSources.py` revalidates all 13 extracted native inputs against their
original archives, the current seven-render/nine-stream source bindings, and every
formal material file against `published-native-v2/closure.json`. It writes
`artifacts/prepared-materials/PedestrianDraw02/current-source-audit.json` without
baking or modifying source assets. The 2,231 historical structural requests and
legacy `RunRegionCoverage.py` counts do not represent current production failures:
the underlying `MovablePlanUvCheck` drops exact coordinates and original domain
provenance, and does not supply Charts before parent UV selection. Use the common
production Retile observer for new coverage decisions. See
`artifacts/prepared-materials/PedestrianDraw02/CurrentHandoff.md` for remaining work.

Normal `GenerateUvCharts.py <repository>` regeneration now also runs
`StageUnionCharts.py`, its `--exact-intersections` mode, and
`IntegrateUnionCharts.py --apply --exact-intersections`. The source-union step
requires Shapely 2.2.0 (plus the existing NumPy dependency); the current isolated
installation is `artifacts/agent-material-env/PedestrianDraw02/python/`. Install
Shapely there with `python -m pip install --no-deps --target
artifacts/agent-material-env/PedestrianDraw02/python shapely==2.2.0`, or provide it
in the authoring Python environment. This is an offline tool dependency only.
The stage admits all independently verified source unions, never a subset selected
by receiver success. It preserves holes and requires original UV coordinates or
exactly representable intersections derived from the original UV edges. Rational
checks verify complete boundary coverage by positive-area original source edges;
rounded intersections are rejected without tolerance. Current integration appends
37 charts and fills 1,333 previously empty domain entries, preserving every original
valid chart and the earlier strict-union shapes. See
`exact-union-chart-candidate/integration.json` for exact byte and proof hashes.
