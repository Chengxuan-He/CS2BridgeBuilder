# Lift03 exact source union recovery

## Source and decision

Normal `tools/GeometryMetaprogram/Lift03RuntimeUvCharts.py` regeneration now runs
`StageUnionCharts.py` followed by `IntegrateUnionCharts.py --apply`. NumPy and Shapely
must be installed in the selected Python interpreter or supplied through `PYTHONPATH`.
The tools resolve the repository from their own locations and require no Unity project.

The stage uses original native chart JSON and BBN under
`artifacts/prepared-materials/Lift03/native-charts/`. It considers every domain whose
entire original chart set was rejected by the original boundary parser. Physical
source-chart membership remains unchanged. Admission depends only on exact source
union evidence, never on receiver scenarios or gained classifications.

The 176 examined source charts yielded 63 verified unions. Their 65 holes remain
explicit rings. Twelve derived boundary coordinates are exactly representable
source-edge intersections: Fraction arithmetic verifies source-edge incidence and
complete boundary coverage. Shapely verifies original-triangle coverage and zero
round-trip symmetric difference. Rounded intersections and unproved boundaries
remain rejected. All required charts must be proved before a domain is recovered.

## Independent loss diagnosis

Run `tools/PreparedMaterials/Lift03/ProveUnionLosses.py` to replay the 47 lost-direct
units retained in `exact-union-chart-candidate/lost-units.json`. It restores JSON
coordinates to binary32, uses the production SourceUv arithmetic when no exact
source UV is recorded, and tests final vertices against original native triangles
with rational cross products. It uses neither candidate loops nor Shapely nor an
epsilon. Every one of the 47 units has a vertex strictly outside its original
source union. The report records a separating edge for every source triangle.
These are old empty-chart false positives, not evidence of missing authored area.

## Integration and compatibility

Integration appends 53 deduplicated chart sets and redirects 36,057 previously empty
domain entries. Original chart coordinates and all other integer entries remain
unchanged. `CheckRuntimeCharts.py` checks the original boundary records and exact
recovered loops separately. The generated C# reader remains byte-identical;
its offset-based schema and all production consumers remain unchanged. No texture,
descriptor, geometry, bone, shader, localization or native dependency identity changes.

Two complete standard regenerations produced the same BBN SHA256:
`e0d82afcf62554c0f105992cda66b761b7a7b8451ad1e6b760ca610bb0298d7f`.
The resource grows from 3,197,824 to 3,228,836 bytes (+31,012 raw bytes).
Isolated Deflate-9 grows from 157,923 to 163,978 bytes (+6,055); this estimate does
not replace complete official SDK package measurement.

Validation: 606,619 domains, all 3,642 original accepted charts, 36,057 recovered
domains, exact ordering and no cross-stream references pass. Native source validation
passes all 5,954 sampler bindings and preserves original UV bits/triangles. The
source hashes and detailed proofs are retained in the candidate evidence directory.

## Remaining verification

The pre-integration capture classified 313,633 units: direct reuse changes from
133,741 to 138,258 (4,564 gains and 47 legitimate losses; net +4,517). These are
staged classifier results only. The shared integration owner must build, measure the
complete SDK package, rerun actual Retile because selection occurs before emission,
and report current-build results. This increment does not establish complete material
coverage, a generated owner's 10,000,000-byte budget, native rendering, cold loading
or human acceptance. Human near/far and motion checks remain separate.
