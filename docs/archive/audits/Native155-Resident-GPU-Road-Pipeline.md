# Native155: resident GPU road and lane generation

## Delivered behavior

The production road and lane assembly consumers now upload original evaluated road records once. The GPU applies material planar-coordinate mappings and the measured road-to-object pose, validates both original and evaluated attributes, clips in double precision, assigns rigid bones, reduces bounds, selects UV2/UV4 layouts, encodes and verifies native attributes, and writes stable per-material streams. Intermediate bound triangles remain GPU-resident unless an explicit replay capture is armed.

Quaternion rotation preserves Unity/Mono float-product stores and double expression intermediates. A nonidentity rotation/translation test exposed 6–12 ULP position errors in an initial float-only implementation; the final implementation passes that exact comparison. No geometry tolerance was introduced to accept it.

CPU responsibilities remain native Surface object identity, partition-reference flattening, ordered allocation/group prefixes, native buffer ownership/publication, strict cross-LOD equality, and asset serialization/IO. The existing complete CPU fallback still logs its reason and applies the full material mapping and pose before binding. GPU validation was moved, not removed. No saved bridge depends on the compute bundle at load time.

## Real capture and measurement boundary

- Real captured BXP four-track/Draw01 input: 380,576 triangles; output: 382,732 triangles / 1,148,196 vertices.
- Device: NVIDIA GeForce RTX 5060, Direct3D 11; isolated Unity 2022.3.62f2 native test host. The game was not started.
- Seven paired rounds; the first is warmup. Controlled adjacent Surface aliases produce 88 output groups. BRR1 does not store Surface object identities: these aliases test grouping correctness, not the actual saved bridge's material grouping. A separate 168-independent-slot stress run also passed.
- The reference entry retains the previous bound+packed readback and CPU grouping/model encoder in the same candidate assembly. Both entries use the candidate's shared clip kernel, including its input checks. This is an implementation-path comparison, not a separately launched native154 binary benchmark.
- The old measured interval starts after CPU input mapping/InFrame. The new interval includes GPU mapping/InFrame. Both include clipping, encoding, validation, transfers, grouping and native Model allocation; neither includes source material conversion or downstream native asset save. Thus this is **not total bridge generation time**.

| Warm median | Reference path | Resident GPU path |
|---|---:|---:|
| Measured chain | 1655.074 ms | 137.838 ms |

The measured interval falls by 91.67%. New native model allocation/copy plus reflection-call overhead is approximately 10.598 ms of that interval.

### New phase medians

| Recorded phase | Milliseconds |
|---|---:|
| `prepareMs` | 24.065 |
| `uploadMs` | 16.401 |
| `countDispatchWaitReadMs` | 26.321 |
| `prefixAllocateUploadMs` | 1.008 |
| `emitDispatchWaitReadMs` | 21.279 |
| `groupingUploadMs` | 14.547 |
| `summaryDispatchWaitReadMs` | 4.947 |
| `layoutEncodeVerifyWaitReadMs` | 18.093 |

These are independent phase medians, not an additive timing partition after taking medians. These are CPU wall-clock intervals. Blocking readback intervals include GPU execution, queue waiting and transfer; they are **not isolated GPU kernel timings**. Upload includes API submission and resident-coordinate dispatch. Grouping is linear in input/output triangle counts, with canonical Surface identity preparation over the small slot table; it does not rescan all triangles once per material.

### Transfers

| Per request, excluding initial 4-byte capability probe | Reference | New |
|---|---:|---:|
| Bound + packed / final native stream bytes | 139,314,448 | 55,113,408 |
| Clipping counts/error readback bytes | 4,566,912 | 4,566,912 |
| Extra bounds/UV/error metadata readback bytes | 0 | 5,632 |
| Total readback bytes | 143,881,360 | 59,685,952 |
| Blocking readback API calls | 5 | 6 |

New uploads total 97,450,684 bytes: 94,384,604 bytes of source/partition/count/prefix/mapping input and 3,066,080 bytes of grouping/summary/layout data. The additional small metadata readback increases call count by one while total readback bytes fall by 58.52%. Normal creation reads no full bound array. Explicit capture adds one 88,793,824-byte bound readback and is reported separately. Direct blittable source upload removes the former byte-array/uint-array CPU staging copies. Resident pose evaluation temporarily adds a second 86,771,328-byte input-sized GPU buffer for this capture; lower transfer volume is not a claim of lower peak GPU memory. Request-owned compute buffers are released on success and failure.

## Correctness and remaining acceptance

- Actual input: six native attribute streams (position, normal, tangent, complete UV4, color and bone) and ordered indices exactly match the reference. Bounds contain all output vertices within the tested float bound precision. No material or triangle is discarded to obtain the timing.
- Actual diagnostic capture: full bound records match the old GPU output; invalid vertex input returns failure with diagnostic and no prepared output.
- Sixteen actual GPU cases cover seams, gaps, empty input, UV2/UV4, occurrence/face precedence, complete CPU fallback, malformed attributes/material/bone, canonical grouping, and nonidentity pose with planar mapping both disabled and enabled.
- Main-tree and frozen builds, source/bundle fingerprints, 907 embedded parameter resources, 181 AoS programs, 351 Surface descriptors, 103 Texture ownership checks, localization and shared UI regression passed. No user-visible text was added.
- Native model attributes and indices were inspected using the game's managed API in an isolated Unity host. This task did not start Cities2 and does not claim manual in-game appearance, full save/cold-reload acceptance, or an end-to-end whole-bridge speedup. Native serialization code was not changed.

## Deployment

Native155 was installed after confirming Cities2 was absent. Only DLL and PDB changed; both originals were backed up, all 964 installed manifest files were verified, and no player bridge files were modified.

- Installed DLL SHA256: `DE71FCCC0BF11F05155C3207E0602A1E5C15213E01FF2800DE2570CF666BE17E`.
- Official SDK ZIP: 77,182,814 bytes; the existing 55 MB publication limit remains exceeded. No publication or GitHub push occurred.
- Receipt: `artifacts/material-remap/shared/native155/deployment-native155.json`.
- Final trace: `work/gpu155/candidate-lane.txt` and `candidate-lane.log`.
- Regression traces: `work/gpu155/cases-final.log` and `capture-lane.txt`.
- Reproducible probes: `tools/RoadGpu/RoadGpu155.Editor.cs` and `RoadGpu155Cases.Editor.cs`.
