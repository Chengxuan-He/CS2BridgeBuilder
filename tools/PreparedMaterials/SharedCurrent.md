# Current shared movable-material integration

## Current acceptance and identities

This October 26 non-tiling integration is installed for manual inspection. Overall
material acceptance is **incomplete**: the tested scenarios retain 41,343 changed
density units without a compliant source/repeat binding, and no human-created owner
from this build has a saved material-footprint report. Owner bytes remain
**UNMEASURED**, not a pass or a prepared-directory estimate. No game generation or
construction API was invoked; visual, motion, near/far and cold-load checks remain
human acceptance work.

This document supersedes the older FF92682D integration and first non-tiling
239987EC candidate. Their evidence remains historical. Current runtime SHA-256:
`37C06B50CFC544217AAD92AC8955006F2B2DAD598DDBD0F2DC16F2CB3F157E53`;
MVID `dfdbf4b0-8a2f-4c42-8d43-a9e89f5c9b7a`. Build verifies 633 typed-array resources
and copies 440 native payloads. The 210 UV-domain resources remain exact schema-3
BBN1 recipes; no geometry, bounds or ordered source-face data was changed.

The official postprocessed SDK package is
`artifacts/shared-nontiling-package2/content.zip`: **47,591,758 bytes**, below the
50,000,000-byte gate by 2,408,242 bytes. All 893 entries were byte-verified. ZIP SHA:
`C9C05CDAD3FD86F0A2B13C07ADED77175F1CA6ADBD6AE16BF76BF212CB050D29`.
Postprocessed DLL SHA:
`CF0A43973CBE7ADFA43E54DB48ED3D88CF1F83386A1EC567DD17D568553B128C`.
Official postprocessing and SDK packaging are local; nothing was published.

## Source-UV rule and actual results

`MovableUvSampling` dispatches exact style/render/mesh/domain flags measured offline.
Only verified finite prototype mappings bypass the density frame. The source path
retains the corrected/interpolated UV produced by `MovableStructureModel.TryBuild`;
it does not use failure of `CanUseSource` as permission to stretch. True repeats,
selected-road/grating routes and unproven mappings retain their existing rules.
The final static data permits 1,788,924 of 1,922,488 original domains. This is source
classification, not a count of changed output units or an acceptance percentage.

All **1,368 actual plans** match the complete earlier plan fingerprints exactly.
The final observer is
`artifacts/prepared-materials/shared-nontiling/actual-final2/observer` (immutable copy),
SHA `6763DAB74EE7C1F628BC58B3C8A965ABC8DC321FED70D293B2FDB08030F95140`.
The input fixture SHA remains
`8ECEE8AD85056D8898DA6B4B4B9D2C59DD13206E2F7430F018A4A54A9A6E226A`.
It invokes real plan methods and the extracted production Retile body before material
selection. Fixture UVs are Float32-expanded; this is not a rendered game capture.

The old 2,148,569 changed units partition into **2,062,670 source-retained units**
and **85,899 still density-mapped units**. The latter have unchanged numerical
mapping and exact face provenance against the old capture. The comparison excludes
reassigned unit IDs and independently evolved chart/eligibility data. There are zero
numeric lineage mismatches. The 21,183,715 retained scenario-domain occurrences in
`observer.json` are a different measure and must not be subtracted from changed units.

Actual production source/region dispatch for the 85,899 density units gives:

| Style | Changed density units | Original chart | Prepared repeat | Remaining |
| --- | ---: | ---: | ---: | ---: |
| Draw01 | 7,699 | 1,237 | 5,697 | 765 |
| Draw02 | 1,020 | 373 | 65 | 582 |
| Draw03 | 36,571 | 13,805 | 0 | 22,766 |
| Lift01 | 27,488 | 11,304 | 0 | 16,184 |
| Lift02 | 2,975 | 1,190 | 1,785 | 0 |
| Lift03 | 0 | 0 | 0 | 0 |
| Lift04 | 7,216 | 2,816 | 4,151 | 249 |
| Lift05 | 2,810 | 1,040 | 1,045 | 725 |
| PedestrianDraw01 | 0 | 0 | 0 | 0 |
| PedestrianDraw02 | 120 | 48 | 0 | 72 |
| Total | 85,899 | 31,813 | 12,743 | 41,343 |

The PedestrianDraw02 domain-specific correction changes 475 source flags, removing
160 changed density units: 96 previously remaining and 64 previously original-chart
eligible. It does not create an allowlist keyed to the tested dimensions. Zero-unit
styles have explicit empty density captures; their retained scenarios are not dropped.

## Remaining semantics

- Draw01: 702 positive-V network units require genuine length-density handling;
  63 LOD1 structural units cross the recorded repeat coverage.
- Draw02: all 582 remaining units are positive-V native network mappings.
- Draw03: all 22,766 remaining units are 6,524 excluded native network domains.
  Existing template 17/33-to-0 conversion has not established equivalence for a
  nonzero worldspace mask layer. A 17-texel heuristic is not accepted as proof of
  all filter/global-LOD-bias support. A concrete future route is the original
  native network context in `Draw03ApproachAssembly`/`CreateMovableRenderGraph`
  (`NetPiecePrefab`), with layout, binding and shader behavior verified end to end;
  no global descriptor bypass was introduced.
- Lift01: all 16,184 remaining units are template-33 positive-V network mappings;
  no verified non-tiling source domain was omitted.
- Lift04: 153 units require mixed-axis behavior (retain finite U while preserving
  V density); the conservative whole-domain boolean does not implement that case.
  Another 96 units in 32 domains cross recorded repeat coverage.
- Lift05: 725 units in 153 domains overlap but extend beyond the measured repeating
  unit coverage (519 crossbeam and 206 soffit cases).
- PedestrianDraw02: 72 units in 24 domains cross the genuine-repeat boundary.

Unknown static domains that do not change in these scenarios are tracked separately;
they are not counted as changed-output failures. No unresolved domain is reported as
verified merely because it did not appear in this finite scenario suite.

## Native evidence and bounded verification

`native-final2.log` passes 108 actual `TryBuild -> Retile.Append -> ToUnityMesh`
integration fixtures, 762 emitted vertices, three LOD routes, Float16/Float32,
corrected/interpolated source UV0.xyzw, exact raw COLOR and bones, geometry/topology,
mixed source/density faces and per-face density gradients. Retained fixtures keep
one identical native Surface and allocate no texture pixels. These controlled
fixtures test the native upload path; they are not all-prototype game rendering.

The installed game reader consumes all 341 packaged Surface descriptors (270 VT).
The source-linked saved material-graph check passes ten styles and 169 identities
without missing dependencies or orphan textures. All 880 packaged native payload
and CID files match the previous coherent installation byte-for-byte. No source
image, texture dimension or mip was changed to fit the package limit.

Current template/shader evidence is under `shared-nontiling/native-semantics`:
Default 0/1 uses the original UV0 affine transform; native packed and dynamic VT
allocation defaults to Clamp. Network shader581 has signed-V vertex semantics and a
possible worldspace layer. Draw01/02 retained negative-V mappings additionally have
all-mip zero-mask and native Repeat equivalence evidence. Windows template2/shader550
uses UV0.YZ for the local interior box, W for orientation and COLOR for selection.
`windows/summary.json` and the exact per-slot records preserve the source evidence;
style generators independently verify template identity before accepting the IDs.

## Reproduction, installation and manual gates

`work/replay-nontiling.ps1 -Revision final2` records the coherent build, extracted
observer, real plans, per-style region dispatch and input hashes. Its exact reports
are under `artifacts/prepared-materials/shared-nontiling/actual-final2`;
`changed-unit-transitions.json` and `plan-parity.json` provide the regression checks.
`tools/Test-SourceUvRetention.ps1` runs the native integration fixtures in an isolated
Unity project. The GPU/native test project is temporary; Unity has exited. Automatic
approval review rejected both the bounded batch cleanup and the explicit literal
Unity-directory deletion with only `blocked by policy`, so those temporary paths
remain intact. No alternative tool was used to bypass that rejection.
Immutable observer binaries, generated sources, raw reports and logs are retained
outside that environment. Pre-existing agent environments are not removed.

The final installation uses `tools/Install.ps1 -SkipBuild`, with Cities2 absent,
an original-only backup and all 892 installed files checked against the build.
`shared-nontiling/deployment-final2.json` records the exact deployment. Existing
bridges were not modified. `existing-owners-final2.json` records six old owner
directories/3,644 files and no saved current-build footprint report. The actual
10,000,000-byte owner gate therefore remains UNMEASURED. A human-created current
sample and its saved dependency closure are required for that measurement, followed
by manual near/far, motion and cold-load inspection.

No player-facing strings were added. Existing localization tests passed in
`shared-nontiling/localization-tests.log`; the optional COLOR decoder also passes
the UV-density self-test. These results do not close the remaining material,
owner-footprint or human visual acceptance gates.
