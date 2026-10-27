# Lift05 material continuation handoff

## Current result

The fresh polar-frame capture supersedes all earlier classification below. The common
`RetileRegionCheck` evaluated all 23,795 Units against DLL SHA-256
`98A4B97B3E62F53C6AFB83A5EED9658B5DD365DF1E4675CE849E46FA9A8427AF`,
with zero captured direct-decision mismatches: **8,199 original + 1,045 repeat +
14,551 unresolved**. Evidence is `actual-unit-check/polar-report.json` and its compressed
per-Unit classifications. Input SHA-256 is
`8695920CDE9043950ECC712F5886187A58607BFB39BD10C528D6FAC25DC6BED6`.

All 23,795 Unit identities, source-face memberships and scenario parameters match the
prior corpus. Transitions are 2,447 unresolved to original; 785 original to unresolved;
428 repeat to original; 1,045 retained repeat; 5,324 retained original; and 13,766
retained unresolved. Net unresolved reduction is 1,662. These are mapping outcomes,
not evidence that all previous accepted mappings were correct. The 785 losses comprise
408 near and 371 LOD1 chart-containment failures, plus six LOD2 conditioned-phase cases.
All six far cases are domain 83 at every sampled delta, including zero. Full old/new
metadata is retained in `polar-far-losses.json` for the common frame investigation;
zero width delta alone does not establish unchanged fragment geometry.

The focused `FragmentGeometryCheck` now resolves this far case using the real current
plan and fixture. Domain 83 is source face 85, fragment 0, parent anchors 103/99/98,
fragment corners 99/2648/2649 and projection pivot 2. At delta zero corner 99 is unchanged;
2648 moves from `[-4,10,66.13523]` to `[-4,9.800049,66.16014]`, and 2649 moves from
`[-4,10,62.5]` to `[-4,9.800049,62.58008]`. The width-only stage has no face-85 fragment.
`Lift05FarRoadBoundary` creates it and assigns the measured Y/Z boundary coordinates.
This is real pre-existing construction deformation, so an identity or rigid-transform
shortcut is invalid. `far-domain83-geometry.json` retains all six actual geometry cases,
construction weights, source UVs, parent anchors, source/target projection bases, polar
rotation, sampling basis and recomputed exact projection matching the capture.

The two earlier LOD1 domain 14940 boundary cases at deltas -1.25/-0.25 remain unresolved
with `ContainsTriangle`. Neither tolerance nor source boundaries were changed.
`polar-comparison.json` records transitions, loss groups and these exact cases.
The remaining 14,551 split into 13,691 outside registered region coverage and 860 with
no matching source region. Native output, owner closure and visual gates remain open.
No runtime source, BBN or prepared material changed during this reclassification.

The follow-up exact-phase probe isolated all 1,389 unresolved conditioned Units.
Restoring their unconditioned coordinates, exact points and final Float32 source UVs
as `MovableParentUv.Select` does produces **1,389 outside-chart / gradient-valid**
cases, with no missing exact metadata, coordinate-key conflict or covered/gradient-invalid
case. Evidence is `polar-conditioned-report.json`. The missing capability for this
class is semantic source-pattern continuation beyond authored support, not numeric
precision. Native local-coordinate affine descriptors cannot supply absent pattern
content. No permitted native extension is established for these source islands;
per-receiver descriptors, chart expansion and tolerance changes are not substitutes.

`BuildUvCharts.domains` now calls the common `MovableUvDomainFormat.decode_arrays`
after the existing checksum-validated BBN reader. Its plain Python domain membership
output is unchanged. Both schema -2 and -3 were exercised across all 11 streams and
47,941 domains, with identical integer arrays, Float32 bounds bits and ordered face
membership (`domain-codec-check.json`). No formal BBN resources were rewritten.

## Earlier pre-polar classification

The earlier 623-resource build's actual Retile observer was classified with
the unchanged production `Unit` and `RegionMapping` implementations. All 23,795
reconstructed `CanUseSource` decisions match the observer exactly. Current results are
**6,109 original source reuses + 1,473 prepared-unit matches + 16,213 unresolved Units**.
Prepared matches comprise 743 deck-soffit and 730 crossbeam uses. No remaining Unit
has a registered region containing its source domain but a rejected region mapping.
These are finite actual-plan decisions before material construction, not saved-owner
or visual acceptance. Run `ActualUnitCheck` with repository root and report destination;
its project requires `GameManagedDir` pointing at the installed game Managed directory.

The classifier preserves `RepeatOnly`, chart loops, `ExactCoordinates`, `ExactSourceUvs`,
`ExactPoints`, points and source/domain identity from
`artifacts/prepared-materials/shared-current/receivers/Lift05.units.ndjson.gz`.
Its report is `artifacts/prepared-materials/Lift05/actual-unit-check/report.json`.
The common runner and coherent-build identities are documented in `../SharedCurrent.md`.

Replacing only the current Unit's chart table with the pre-recovery table gives 6,034
original reuses: recovery enables 77 and rejects 2 previously accepted Units, a net gain
of 75. This is a chart-table counterfactual on current Units, not a full old-Retile run.
Both losses are LOD1 domain 14940 at deltas -1.25/-0.25. The measured original triangle
is fully covered; Float32 atlas interpolation puts tiny mapped boundary areas outside
the exact chart. The old no-chart simplex fallback admitted them. Do not expand chart
boundaries or relax precision to regain these two cases. Full records are retained in
`lostOriginalCases` for native coordinate-representation investigation.

The 16,213 unresolved Units comprise 14,298 outside their authored charts (including
40 glass uses), 1,399 conditioned-phase cases and 516 without a nondegenerate chart.
All 516 no-chart cases have zero-area original UV triangles. Although 104 map inside
their original exact source line, none pass the unchanged production gradient check.
Adding line charts alone therefore provides no safe gain. These results supersede the
older receiver-only counts below for current production classification.

The two existing native-validated semantic units remain the only integrated prepared
structure units. `assets/Materials/Lift05/` still contains 84 files, 912,445 bytes.
No material pixels, Surface identities, mips, channel bindings or geometry were changed
by this continuation. This directory subtotal is **not** the complete generated-owner
material closure. No complete saved owner was generated; its material bytes and the
10,000,000-byte acceptance result remain unavailable.

An offline source-topology improvement is integrated in `BuildUvCharts.py` and
`assets/HardcodedNumbers/Lift05/Lift05UvCharts.bin`. When simple boundary walking rejects
overlapping UV edges, the extractor now unions only the triangles of that same measured
physical component. It records the exact resulting outer loops, holes and disconnected
islands. It does not expand bounds, infer periodicity, produce new texture pixels, or
perform prototype Boolean operations at runtime. The runtime loader is unchanged.

The source fixture is `artifacts/uv-density/layout-plans.bin`, SHA-256
`cc253167a7916408d6471aebeb08d797c9821610ff843fecf582373c333811e2`.
The new BBN is 1,935,488 bytes, SHA-256
`5e6e9c312c9080d736c10eda19e2a4c98b3738f0e7618060e403062ac414f101`.
It retains 11 streams and 47,941 domains, with 16,333 charts and 16,612 loops.
The prior BBN was 1,847,592 bytes. This data increase is not a material payload increase.

## Verification and precise limits

`AuditTopologyRecovery.py` independently runs both extraction modes. Every previously
valid chart record remains identical. Reconstructing the emitted loops by even/odd
polygon coverage equals the original component's triangle union, retaining holes and
detached islands. Recovery adds 473 components (293 near, 180 LOD1) and covers 4,855
previously unmapped source faces. All 11 streams retain their source position, UV,
index and source-face-count hashes. The BBN trailing SHA-256 was verified. The regenerated
`Lift05UvCharts.cs` is byte-identical to its prior version.

The older actual-plan receiver binary remains SHA-256
`29d73da30b7034a50c7de512ac0359714479c6e0fb07046e025173cb184d9b15`.
Across six recorded width deltas it contains 23,795 receivers: 4,027 direct source reuses,
19,768 requiring another representation. Existing units provide 1,349 additional
prepared matches, leaving **18,419 unresolved by that recorded production mapping**.
The 1,541 mapped-unit total includes receivers already reusable, so it must not be
subtracted from 19,768.

The earlier isolated chart audit contained 77 additional non-direct, non-repeat receiver samples
(6 near, 71 LOD1); 1,079 inspected non-direct/non-repeat samples remain outside their
recovered charts. That older audit alone did not establish production success. The
current actual-Unit classification above now records the 77 gains and 2 precise losses;
use its 16,213 unresolved total rather than subtracting candidates from older totals.

`ExportRemainingCases.py` writes 59 representative cases covering streams, categories
and sampled width deltas. Each includes original material identity/native metadata,
source faces/positions/UVs, complete original chart loops and actual mapped receiver UVs.
Its source hashes bind the fixture, receiver binary, chart catalogue and classification.
This is diagnostic input, not a repeat-unit or baking recipe. `GroupStructureUnits.py`
was rerun after chart integration so classification and case evidence use the current
chart catalogue. Historical installed-unit coverage remains unchanged.

Localization review: no user-visible text, resource display name or localization caller
changed. Technical identifiers remain unchanged. The common agent owns compilation,
global BBN packaging, localization checks and coherent deployment; none was attempted
independently here. Backups contain only original files under
`artifacts/backups/lift05-topology-recovery-20261026/`.

## Remaining implementation

1. Continue from the classified current observer, not older receiver-only estimates.
   The 359 earlier chart-contained candidates must not be counted again: the current
   production classifier already applies real phase metadata, chart membership and
   gradient checks. A next precision improvement requires verified native local-coordinate
   descriptor semantics, especially the conditioned and degenerate cases. There is no
   safe further gain from simply supplying line charts or enlarging chart boundaries.
2. Resolve the remaining nonperiodic authored details, conditioned mixed islands,
   degenerate/unbound charts, native Template2 glass and LOD2 packed-atlas cases. A
   bounding box or whole-atlas Repeat is not a valid substitute. Preserve source density,
   orientation, phase, normals, alpha and every required mip. Existing semantic units
   must not be substituted for unrelated details or far-LOD materials.
3. Generate and inspect a complete owner through the authorized common workflow, including
   selected-road material dependencies. Measure the full owner closure and complete bridge
   graph separately from prepared files. Verify preview, save, regeneration, export,
   cold load and independent lifetime before claiming readiness for human acceptance.
4. Reuse the private environment `artifacts/agent-material-env/Lift05/` and existing native
   products for future necessary baking. No baker or Unity process was started in this
   continuation, and no environment or installed resource was deleted.

Evidence: `artifacts/prepared-materials/Lift05/topology-recovery/audit.json`,
`domain-charts/report.json`, `structure-groups/installed.json`, and
`remaining-cases/cases.json` beneath the same Lift05 artifact directory. Human near/far,
motion, filtered appearance and gameplay acceptance remain pending.
