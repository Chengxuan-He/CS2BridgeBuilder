# Lift05 native UV sampling policy

`BuildUvSampling.py` emits `Lift05UvSampling.cs`, the checksum-validated
`Lift05UvSampling.bin`, and `artifacts/prepared-materials/Lift05/non-tiling/classification.json`.
All 47,941 domains in eleven native mesh/material streams are recorded:
46,905 atlas domains and 136 source-local window domains retain authored UVs;
900 preserve existing repeat or prototype-road overlap handling. Unknown mappings
return false.

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
Surface, texture and VT identities. Both known structural repeat regions and
prototype road regions exclude every touching domain before retention is enabled.
Selected-road and track appearance does not inherit this policy.

LOD2 domain 83 now qualifies from its own native atlas mapping. Its zero-delta
construction still changes geometry: the shared retention path must interpolate
original source UVs using construction weights, rather than bypassing the change
as an identity transform. This also applies to degenerate original UVs.

The BBN is 191,880 bytes, SHA256
`115609cf2d0217437caeaf6df5df69c4492ba3ebcb5a87f936fa079cb5401417`.
The emitter verifies exact native position, face and UV agreement with the source
fixture, verifies BBN checksum/readback, and records full classifications and
source identities. No material pixels, geometry, bones or user-visible text change.
Localization behavior is preserved. Shared routing/build, runtime interpolation,
complete owner bytes, native appearance and human acceptance remain separate gates.
