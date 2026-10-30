# Lift05 native UV sampling policy

`BuildUvSampling.py` emits `Lift05UvSampling.cs`, the checksum-validated
`Lift05UvSampling.bin`, and `artifacts/prepared-materials/Lift05/non-tiling/classification.json`.
All 47,941 domains in eleven native mesh/material streams are recorded:
47,151 atlas domains and 136 source-local window domains retain authored UVs;
149 retain only finite V while preserving U repeat density; 505 preserve density or prototype-road handling. Unknown mappings return zero axes.

The classification concerns native UV mapping, not whether painted pixels look
periodic. Each original Surface payload and CID is verified against its native
archive. Current template 0 uses UV0 with unit scale, zero offset and no mapping
override. Physical-position/native-UV adjacency records each part occurrence,
its source faces and domain membership. Disconnected atlas reuse does not imply
UV tiling. Constant and line mappings qualify under the same verified semantics;
2,557 admitted near/LOD1 structural domains have degenerate source UV triangles.

The native VT registration path uses `VTAtlassingInfo`'s Clamp default:
`ReserveTextureRect` constructs it, `GetTextureParamBlock` carries its address
mode, and `SurfaceAsset` binds the block. This establishes finite edge sampling
for 83 LOD1 pillar domains whose coordinates exceed the atlas; their UVs must
not be normalized or altered. Hashes of the inspected native code and current
template verification are retained in the evidence. Glass retains its original
Surface `24cf998eda84a65dcd7334a7b0b02b90`, template 2 and shader 550. Its 68 near
and 68 LOD1 domains are independently verified source-local UV0 YZ mappings.
The shared window reports retain exact domain IDs, complete UV0 XYZW, COLOR,
indices and original geometry hashes. The emitter checks those hashes, source
indices/UVs and local YZ bounds before admitting those exact domains. The shared
retention path must preserve and interpolate all UV components and COLOR;
an XY-only retention implementation does not satisfy this window policy.

The main 4096-square atlas and separate 512-square far atlas retain their native
Surface, texture and VT identities. Native source-part evidence separately identifies finite upper tower caps and terminal panels even when their UVs reuse a repeating region. Genuine central repeats and prototype-road domains retain density.
Selected-road and track appearance does not inherit this policy.

LOD2 domain 83 now qualifies from its own native atlas mapping. Its zero-delta
construction still changes geometry: the shared retention path must interpolate
original source UVs using construction weights, rather than bypassing the change
as an identity transform. This also applies to degenerate original UVs.

The BBN is 191,880 bytes, SHA256
`de5be78bb2485f740940998e71b7a8ad04380b658387f0f22cf4322008303e7c`.
The emitter verifies exact native position, face and UV agreement with the source
fixture, verifies BBN checksum/readback, and records full classifications and
source identities. Original material pixels, geometry, bones and user-visible text remain unchanged. The separately measured native profile unit is prepared once as described below.
Localization behavior is preserved. Shared routing/build, runtime interpolation,
complete owner bytes, native appearance and human acceptance remain separate gates.

## Complete native profile unit (2026-10-28)

Sixteen cap parts per near/LOD1, 32 terminal parts per near/LOD1, and nine central
LOD1 profile parts are identified by native mesh hashes and exact face seeds in
`Lift05FiniteParts.py`. Their full native charts, adjacency and source positions
are recorded by `BuildUvSampling.py`. Caps add 148 full-retention domains and
terminal panels add 84; 18 profile domains retain V only. `For` remains compatible
with full retention (`Axes == 3`). These decisions do not infer finite mapping
from a failed repeat lookup or a bounding-box intersection.

Native LOD1 faces 9892/9893 form the complete 540 by 20 texel, four-metre panel.
Adjacent faces 9434/9435 share physical seam vertices and reset U by exactly one
period. V is a finite authored profile. The once-prepared 540 by 40 guarded native
payload retains source mip provenance and effective finite-V sampling. Both old
near coverage (unchanged 540 by 12 texels) and complete profile coverage map to
one binding, `17115167afb412e4209c884e903af746b5605b80be08014fe5c0118d6c4c6982`.
Old near V maps into payload [.45,.75]; complete profile V maps into [.25,.75].
The old strip payload and descriptor were backed up and removed after verifying
no other descriptor references its three textures. Soffit and road units remain.

Evidence is under `artifacts/material-resume/Lift05/profile-unit`. Native readback
passes all 30 channel/mip levels, source pixel quantization, U seam periodicity,
and old-near subrange equivalence. Original-atlas boundary differences remain
recorded for visual observation. Material package bytes increase by 4,162
(912,445 to 916,607); measured profile metadata adds 96 bytes. Static tests verify
all 47,941 source domains and all 18 mixed-axis IDs. The old capture counterfactual
resolves 678 of 725 cases; the last 47 require fresh shared runtime measurement.
No deployment or in-game construction was performed. Human acceptance is pending.


### Native narrow-profile completion

Fresh shared capture found 41 rejected mappings. Exact native inspection exposed
omitted terminal triangles and narrow central profiles, separate from the shared
floating-point inverse-map issue. The exhaustive native inventory adds 80 near
and 40 LOD1 narrow-profile parts, and eight near plus six LOD1 terminal parts.
Selection is pinned by native hashes and face seeds; complete chart single-cover,
physical endpoints, four-metre U span/phase, and finite V are reverified. These
are not scenario-specific domain exceptions. Full retention now covers 47,287
domains; 149 use U density with finite V, and 505 remain density/road handling.
`TestUvSampling.py` keeps the soffit density negative control and verifies that
the original near unit retains U density. Two deterministic regenerations give
the SHA above. The shared precision correction and fresh capture remain separate.
