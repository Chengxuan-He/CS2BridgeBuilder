# Lift01 authored UV retention

## Classification and implementation

`BuildUvSampling.py` generates `Lift01UvSampling.cs` and the lazy embedded
`Lift01UvSampling.bin` resource. `For(render, mesh, domain)` returns the measured
classification for all 23 exact native streams, including stationary support parts
and all three LODs. Unknown streams return false; an invalid domain in a known
stream raises an internal error. No runtime topology or sampler inference occurs.

The 647,781 domains contain 560,924 verified finite authored atlas mappings, 80
verified local window mappings, 288 domains intersecting the genuine repeating
floor region, and 86,489 positive-V network mappings. The first two groups retain
authored UVs under the non-tiling exception. The floor and network groups remain
on the density-preserving path. The original
floor region is `(0.57421875, 0.260009765625, 0.6796875, 0.469970703125)`; its
independent repeat evidence remains `repeat-units-v2/prototype-evidence.json`.

## Evidence

The machine-readable record is
`artifacts/prepared-materials/Lift01/non-tiling/classification.json`. It identifies
each Surface CID and digest, texture/VT references, source archive entry, native
template, actual overrides, mesh/slot, every LOD, domain-resource hash, authored
position/UV/index hashes and excluded domain indices. Object atlases retain U/V;
windows retain their complete UV0 channel, with local Y/Z and direction W, plus
COLOR. The source fixture and part-scoped chart inventory are hashed.

Native template evidence is
`artifacts/prepared-materials/shared-nontiling/native-semantics/`. Current templates
0 and 1 select UV0 with unit source texture scale and zero offset. Actual
Lift01 Surface records introduce no UV/worldspace/repeat overrides; their only
property override is `_Snow=0` on the main cutout source and its far atlas.
Original finite physical-position/UV-connected islands provide mapping evidence,
rather than texture wrap flags or unsuccessful Retile results. Reusing an island
on another disconnected authored part does not imply tiling along a part.
Template 2 retains the original Surface and shader550. The shared full-channel
window audit identifies every domain in both Lift01 window slots as a finite local
Y/Z mapping. The generator verifies its exact domain IDs, Geometry hash, UV0 XY
against the source fixture and original indices. Full-channel interpolation and
native sampling checks belong to the shared verification.

Template 33 and 17 both use shader581. Their vertex programs 106, 107, 108 and 109
(ordinary, alpha test, octahedral tangent and both keywords) are byte-identical:
`c23313b0e5a3082ecfeaaa1d35c505e34bf38da7933f83004dd6466098425f8b`.
The signed-V instructions multiply nonnegative V by evaluated network length;
negative V uses its fractional part. All three Lift01 network streams have
strictly positive authored V. Their 65,019 / 20,578 / 892 domains therefore remain
false. Original texture scale/offset alone does not bypass this vertex mapping.
The existing native network-to-object descriptor conversion remains required;
its worldspace-layer equivalence is not established by a retention flag.

The 33,292 domains referencing conservative chart fragments still have exact
authored source triangles and physical-part membership. Their polygon-union
representation limits extrapolated coverage; it does not invalidate unchanged
native UV sampling. Likewise, the 204 source constant/line domains retain their
actual authored coordinates; no positive-area chart is invented for them. The
source-UV retention path does not require a density-preserving inverse mapping.

## Verification and limits

Two complete regeneration runs produced identical resource SHA-256
`c50729500c1b3290239b85eb2356f250ee3cb5e5e033fc0299c435aa8f16c550`.
The resource is 2,591,288 bytes, and its decoded I32 payload equals the generated
flags exactly. Every stream's positions, UVs and indices match the retained native
chart fixture. Source descriptors were read from their original archives and
matched the authoritative source-map digests. No descriptor verification/bake,
pixel edit, geometry transform, topology change or bone change was introduced.

The shared agent owns routing, build and runtime UV-retention checks. Native
near/far appearance, motion, cold loading and complete owner dependency bytes
remain separate pending acceptance. Physical texel density can change on the
verified non-tiling mappings as geometry stretches; this is expected and must not
be reported as a density-preserving pass. Native texture resolution, channels,
mips, original-compatible identity and independent persistence remain required.
No user-visible text changed; internal errors use existing localized operation
failure handling. The unproven steel crop remains unregistered and was not baked.
