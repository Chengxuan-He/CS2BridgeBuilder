# Current shared movable-material integration

## Current requirements and remaining implementation

The [general bridge mapping rule](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode)
applies to fixed and movable bridges. The [material contract](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md)
requires original non-road materials; only prototype-supported road appearance types enter
composition/precomposition (offline material baking outside the game), and all other road types retain their original materials. Asphalt,
sidewalks and wheel-track/rut effects are independent per-prototype decisions. Source tiling
stays tiling and source stretching stays stretching; no full-road rasterization is permitted.

The retained later Draw01 log `artifacts/layer-decoupling/shared/logs/BridgeBuilder.before.log`
shows a preview path reaching a 16384-square VT page with 32 charts and 49,755 ms of VT writing.
This confirms that the performance/material representation work remains incomplete; the earlier
fixed-base repair does not establish compliance with the new scheme. The native repetition audit
supplies source evidence, not an independent-layer implementation or a new numeric tolerance.
Current generated-owner size and game acceptance remain separate verification work.

The build/deployment sections below record specific prior artifacts and checks. They are not
new builds or confirmation of the live installation after documentation edits. Do not deploy an
older hash merely because it appears in a stage-specific section headed "current".

## Latest retained Draw01 fixed-base identity repair

The subsequent Draw01 preview failed at `Fixed Base 0` with missing Structure key
`021869598a9e3a57c97e51c649ad25da40e8855bd0483f42583b2c91cb9dd757`.
Native `Identifier.ToString()` includes `[uri]` for registered assets. The shared
native-network gate compared that display string with an exact CID and incorrectly
entered object/atlas conversion. Earlier URI-less fixtures did not exercise this.
The repair uses `id.guid.ToString()` in the shared gate, existing-object material
lookup and Draw01/Draw02/Draw03/Lift01 descriptor lookup and validation. No predicate
was widened, no missing payload fabricated, and no geometry or material resource changed.
Native construction failures now retain their inner exception and graph context in logs;
localized user-facing failure handling is unchanged.

Frozen raw runtime: `A20527373519352F8A4C8238EF643D90CBFEAFBEB2987A07C6C10DC4B123F55A`.
Official SDK package: **52,486,998 bytes**, SHA-256
`B9CADFFC393440FDF965FEAA95DE8BBC9F0FCC8DFF44319F0134EF3080438254`.
The package has 937 verified entries; only DLL/PDB differ among the 936 installation
files. The other 934 files, including all 462 native material payloads, match the
preceding VT repair exactly. Both `Mods/BridgeBuilder` and
`.cache/Mods/pdx_mods/160320_17` are installed; all 936 payload hashes in each
location were independently verified. Only DLL/PDB were replaced. Original-only
backups are under `artifacts/backups/before-draw01-cid-fix-20261028-174410/`, in
`local-BridgeBuilder` and `pdx-160320_17`. Deployment evidence is
`artifacts/draw01-fixedbase-fix/shared/outputs/deployment-final.json` and
`install-verification.json`. The later preview log is summarized above; it does not certify game visual or cold-load acceptance.

Native URI-bearing tests reproduced the old Fixed Base 0 gate rejection, then passed
13 Draw01 streams / 78 Plan-Build-Append-upload cases / 34,512 vertices and
15 Draw03 streams / 90 cases / 153,462 vertices. They preserve original Surface,
raw UVs, color, bones, topology and native NetPiece/LOD component behavior; 168
rejection controls retain object, skinned, road, wrong-CID/slot and provenance gates.
The old complete game construction and its exact missing-key lookup were not rerun
in the isolated fixture: the saved game log establishes that failure; the native
regression reproduces its identity rejection and exercises the repaired downstream path.
Build and localization checks passed. Native game preview and cold-load acceptance
remain human checks; this does not claim the pending generated-owner size audit passed.

Evidence: `artifacts/draw01-fixedbase-fix/shared/outputs/native-regression.json`,
`package-verification.json`, `install-source-manifest.json`, `source-manifest.json`,
`package-final/measurement.json`, and the sibling `logs/` directory. Source fixtures
are under `artifacts/draw01-fixedbase-fix/Draw01/outputs/native-fixture` and the preserved
Draw03 native inputs. Temporary environments contain no unique deliverables.

## Previous VT loading regression and repair

The subsequent human game run invalidated the prior broad material-readiness claim.
`artifacts/vt-mid-mip-fix/SceneFlow-before.log` records 44 failures (22 distinct
PedestrianDraw02 Surface CIDs: 21 converted lanes and paint, seen in local and
cached installations). Native VT loading requests zero preprocessed mid levels
for the converted Surface identities, while their original VTTexture carriers
require three. The installed Surface bytes match the repository; this is not a
stale installation. Previous descriptor-header, geometry and upload checks did
not exercise this native Surface-CID-to-MidMips relationship.

The repair passed automated validation and is installed in both `Mods/BridgeBuilder` and
`.cache/Mods/pdx_mods/160320_17`. All 936 installed payload hashes in each location
match the frozen unprocessed build and verified material sources. Original-only
backups are under `artifacts/backups/before-vt-mid-mip-fix-20261028/` in
`local-BridgeBuilder` and `pdx-160320_17`. Deployment evidence is
`artifacts/vt-mid-mip-fix/shared/outputs/deployment-final.json` and
`install-manifest.json`. At that repair handoff, the game had not yet been relaunched for human verification.
New Surface identities now retain their implicit native MidMips dependencies.
Twenty-two compact caches are packaged once under `Materials/Shared/MidMips`;
owner persistence copies only referenced caches independently, with native filenames.
Runtime-created lane aliases obtain an exact native BC7 subset from a permitted
original cache. No mip level is discarded, exception suppressed, or source pixel changed.

Frozen unprocessed runtime SHA-256:
`306E50825C99BE4C5FE0DF0D8E74B417C55BE788E49245F5423433F7F4F66738`.
Official postprocessed package runtime SHA-256:
`581D5FDAFE23A591B78493F4AF15A1432906A6DF9CDA03C461A20B46237B2B35`.
The official SDK ZIP is **52,486,709 bytes**, below the approved **55,000,000-byte**
cap, SHA-256 `2F33456DF2B3A40E291F42D1A848AA462517642F0B857BDA524D1EB4B5263737`.
All 937 ZIP entry names and content hashes match the postprocessed source tree;
936 files are installation payloads (the additional ZIP entry is LICENSE.txt).
There are 462 native material payloads: the previous 440 plus 22 MidMips caches.
Evidence and exact installation sources are under
`artifacts/vt-mid-mip-fix/shared/outputs/package-final2`, `package-verification.json`,
`final-source-manifest.json`, and `install-source-manifest.json`.

Current verification:

- Native split-cache reservation and `Surface.LoadTexturesVT` passed 23 Surfaces,
  87 channel bindings, 22 missing-cache negative controls, four empty controls,
  and 55 asynchronous byte-exact mip-tail reads.
- Runtime alias cache output passed the same native path for 22 new Surface CIDs,
  84 channel bindings, 22 negative controls and 52 byte-exact mip-tail reads.
- Twenty-six actual native COK-backed `SubStream` cases proved seek support,
  native filename tagging and byte-exact production subset extraction.
- Source-linked preflight passed 22 required mappings, five zero/empty inputs and
  24 missing/truncated/header negative controls. Independent material dependency
  snapshots passed all ten styles and 191 identities.
- Frozen build passed with zero errors. Targeted native UV regression passed
  144 cases, 1,116 emitted vertices, all four axis masks, tangent/handedness,
  raw channels and finite-boundary controls. Geometry behavior was not changed
  by this repair; the previous 1,368-plan evidence is historical, not rerun here.

Logs are in `artifacts/vt-mid-mip-fix/shared/logs`; fixtures, binaries and manifests
are preserved outside temporary environments. These tests exercise native CPU
reservation/loading and actual asynchronous carrier reads, not rendered game
cold-load behavior. A fresh human game launch must confirm the reported 0-versus-3
error is gone. Actual current human-generated owner material size remains
**UNMEASURED** against the unchanged **10,000,000-byte** cap; independent snapshot
checks do not substitute for that measurement. Near/far appearance, motion and
save/reload remain manual checks. No game construction API was invoked.

## Previous manual acceptance handoff

The October 28 integration resolved the changed-output binding gaps measured by
its automated scenario suite and was installed for manual acceptance. The later
VT loading failure above was outside that suite; the current repair evidence supersedes its loading-readiness claim.
The user-approved completion milestone is entry into manual acceptance,
not completion of the human checks below. Both `Mods/BridgeBuilder` and `.cache/Mods/pdx_mods/160320_17` contain the final
runtime; all 892 deployed payload files in each location were hash-verified. The
game was closed during replacement. Original-only backups are under
`artifacts/backups/before-material-resume-20261028`. Deployment evidence is
`artifacts/material-resume/shared/outputs/deployment-final.json` and
`install-manifest.json`; user-generated assets were not modified.

No game generation or construction API was invoked. Actual human-created owner
material bytes remain **UNMEASURED** until a current generated bridge supplies a
saved footprint report. Near/far appearance, movement and save/reload remain human
checks. No current owner was fabricated to satisfy those gates.

Runtime SHA-256 for this recorded stage:
`6A2203E503A89738C80F17802C05E519813595E730D13C4A242998D6FC967851`.
The preserved DLL is `artifacts/material-resume/shared/outputs/actual-final/BridgeBuilder.dll`.
The coherent build verifies **634 typed-array resources** and copies **440 native
payloads**. The original 210 UV-domain recipes and all actual geometry plan
fingerprints are unchanged. The 2,206-file input manifest is
`artifacts/material-resume/shared/outputs/build-input-manifest.json`.

The official postprocessed SDK package is
`artifacts/material-resume/shared/outputs/package/content.zip`: **47,604,274 bytes**,
2,395,726 bytes below the 55,000,000-byte gate. ZIP SHA-256:
`B76445C479EB99296EFF030C5DDDD7C212174928B29A671B7EFAB361E1C4564D`.
The complete set of **893 entries** and every entry's bytes were verified.
Postprocessed DLL SHA-256:
`8AC6A2A781E2AE28C1D1F220FBACEBAC7A0BB0BD1CDC441E5DA265568FA789E5`.
Postprocessing and packaging were local; nothing was published.

## Implemented sampling rules

- Finite prototype mappings retain corrected source UVs and recorded interpolation.
  A failed density sampler never authorizes stretching.
- Exact Draw01/02/03/Lift01 render/mesh/CID identities can retain original network
  Surface templates 17/33 only within a fixed source NetPiece graph with no selected
  road or skeleton. Its plain RenderPrefab LOD children inherit that graph context.
  Native signed-V length evaluation, negative-V wrapping, worldspace layers and
  NetPieceTiling metadata stay in the native network path.
- Lift04 independently retains finite U while preserving repeating V density.
  Lift05 independently retains finite V while preserving repeating U density.
  Final tangents follow the combined UV Jacobian and preserve handedness and the
  authored tangent convention. Finite components survive parent-coordinate
  inversion exactly; genuine out-of-range samples still fail the finite guard.
- Repeat-boundary classifications use actual native physical source faces. Lift05's
  complete four-metre LOD1 profile supplies one shared guarded finite-V payload;
  the old narrower coverage maps into that same payload. No receiver-dependent
  crop, arbitrary expanded coverage or reduced texture resolution was introduced.

Static classification covers 1,922,488 domains: 1,789,201 retain both axes, 4,618
retain U, 149 retain V and 128,520 remain under density/native-context rules. These
are source-domain counts, not output coverage percentages. Native network contexts
are recorded separately from the finite flags.

## Actual evaluated results

`artifacts/material-resume/shared/outputs/actual-final` contains the current
observer, exact reports and hashes. All **1,368 actual plans** match the earlier
complete plan fingerprints: zero missing, added or changed fingerprints.

| Style | Changed density units | Original chart | Prepared repeat | Remaining |
| --- | ---: | ---: | ---: | ---: |
| Draw01 | 6,681 | 984 | 5,697 | 0 |
| Draw02 | 65 | 0 | 65 | 0 |
| Draw03 | 0 | 0 | 0 | 0 |
| Lift01 | 0 | 0 | 0 | 0 |
| Lift02 | 2,975 | 1,190 | 1,785 | 0 |
| Lift03 | 0 | 0 | 0 | 0 |
| Lift04 | 7,196 | 2,831 | 4,365 | 0 |
| Lift05 | 1,390 | 548 | 842 | 0 |
| PedestrianDraw01 | 0 | 0 | 0 | 0 |
| PedestrianDraw02 | 0 | 0 | 0 | 0 |
| Total | 18,307 | 5,553 | 12,754 | 0 |

The previous October 26 baseline had 85,899 changed density units and 41,343
unbound units. Exact source-face lineage now classifies 65,969 baseline units as
native network sampling, 1,623 as newly retained finite mappings, 6,441 as
independent-axis mappings and 11,866 as unchanged density mappings. All unchanged
density numeric fingerprints match. These categories sum to the same 85,899
baseline units; the full transition report retains per-style counts. The observer's
21,170,698 finite scenario-domain occurrences and 638,352 native network
scenario-domain occurrences are different units and must not be subtracted from
changed-unit counts.

Previously documented conservative Windows and Lift03 near-period classifications
remain conservative when not proven by source evidence. None creates an unbound
changed unit in this suite. Zero remaining in these measured scenarios is not a
claim that every possible configuration or unknown static domain has been proven.

## Native and dependency verification

- `native-axes-final-hashed.log`: 144 actual Build/Retile/ToUnityMesh cases, 1,116 emitted
  vertices, three LOD routes, all four axis masks, Float16/Float32, constructed
  vertices and corrected source UV0.xyzw. Exact positions, topology, bones and raw
  COLOR pass. Packed tangents, handedness and normals are checked against an
  independent UV-Jacobian calculation. Finite-boundary positive and negative
  controls pass; one original Surface remains unchanged with no new pixels.
- Draw03's `native-network-final.log`: 90 native network cases over 15 real source
  headers and three widths, 153,462 vertices and 90 negative controls on this same
  runtime. Exact model/Surface identity and original native UVs pass. Additional
  ZW/COLOR controls are synthetic and are reported as such. Object/road/skinned
  graph rejection and actual component-copy Tiling/LOD metadata checks pass.
- `native-descriptors-final.log`: the installed game reader consumes all 341
  packaged Surface descriptors, including 270 VT descriptors, completely.
- `native-material-graph.json`: ten styles and 169 distinct identities pass saved
  dependency closure and independent byte-identical owner snapshot checks, with no
  missing dependencies or orphan payloads. These temporary snapshots are not a
  human-generated bridge or an owner-footprint measurement.

Full TryCreate persistence requires game AssetDatabase resources and was not
represented as a standalone pass. Game cold-load/rendering acceptance remains
explicitly human. Original source textures, material semantics and all mip chains
remain the authority; the isolated CPU/native tests do not certify the displayed
image in the running game.

## Human acceptance matrix

For each row, create representative shorter, longer and wider variants through the
normal user workflow. Inspect near/mid/far LODs and transitions, closed/open motion,
underside/rails/windows, and save/reload. Include narrow and wide road selections
where supported. Verify the current saved owner footprint is at most 10,000,000
material bytes; prepared-directory sizes cannot replace that measurement.

| Style | Focus | Near/far + motion | Save/reload | Owner bytes |
| --- | --- | --- | --- | --- |
| Draw01 | Native approach mapping and LOD1 terminal faces | Pending | Pending | UNMEASURED |
| Draw02 | Positive-V network length and transverse mapping | Pending | Pending | UNMEASURED |
| Draw03 | Original network worldspace layer and approaches | Pending | Pending | UNMEASURED |
| Lift01 | NetPieceTiling and all approach LODs | Pending | Pending | UNMEASURED |
| Lift02 | Retained finite atlas, structural repeats and windows | Pending | Pending | UNMEASURED |
| Lift03 | Finite structures and conservative near-period domains | Pending | Pending | UNMEASURED |
| Lift04 | Finite U profiles, repeating V, fascia and normal direction | Pending | Pending | UNMEASURED |
| Lift05 | Shared complete profile, finite V, U seam and LOD transition | Pending | Pending | UNMEASURED |
| PedestrianDraw01 | Moving frame and conservative windows | Pending | Pending | UNMEASURED |
| PedestrianDraw02 | Native finite closure quads and windows | Pending | Pending | UNMEASURED |

Reproduce with `artifacts/material-resume/shared/Replay.ps1`,
`tools/Test-IndependentUvAxes.ps1`, the per-style native/source producers and
`artifacts/material-resume/shared/Package.ps1`. Source test tools and immutable
outputs are retained outside temporary environments. Localization review: no new
player-facing UI text or localization keys; new diagnostics are developer/test
messages only. The unrelated publishing description was not modified by shared work.

## Temporary environment cleanup

The coordinator's bounded cleanup attempt for seven newly created environments
(shared, Draw01, Draw02, Draw03, Lift01, Lift04 and PedestrianDraw02) was rejected
by automatic approval review before execution with only `blocked by policy`.
The rejected command included absolute-containment, reparse-point and live-process checks; independent process inventory confirmed no live tests.
No retry or alternative deletion method was used. Those new environments remain;
the new Lift05 environment also remains and was not separately deleted. These are
separate from the older environments retained after the previous cleanup rejection.
All required test sources, fixtures, immutable observers and evidence are preserved
outside the temporary environments, and no material-test process remains running.
This filesystem cleanup limitation does not change the installed runtime or the
user-defined manual-acceptance milestone.
