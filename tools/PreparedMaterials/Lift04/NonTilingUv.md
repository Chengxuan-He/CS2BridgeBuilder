# Lift04 prototype UV sampling classification

## 1. Native evidence

`BuildUvSampling.py` rereads the seven original Surface descriptors and their original texture
payloads from installed game/DLC archives and checks their SHA256 values against the native
binding inventory. The six structural Surface identities use native template 0 with identity UV0
scale/offset, UV0 mapping mask and identity base/normal/mask/VT-stack texture transforms.
No Surface UV override is present. The original glass Surface uses unchanged template 2 Windows
and shader 550. The shared native template evidence and exact geometry-hashed window records
establish local UV0 YZ mapping for its eight domains. Complete UV0 XYZW and COLOR are retained;
the Windows shader is not interpreted as an ordinary planar XY material.

Each native mesh NPZ hash is checked against the retained native chart inventory. Physical chart
occurrences are measured from edges sharing both original XYZ and UV0 coordinates. Every admitted
occurrence traverses the native atlas without crossing its sampling boundaries. Disconnected reuse
of an atlas patch is not treated as tiling along a part. Image motif periodicity and texture-global
wrap alone are not classification predicates. Existing known repeat regions and their overlaps are
conservatively excluded, using complete source coverage rather than only one repeat interval.

Full evidence, source/texture identities, exact physical face membership, bounds, original-domain
classification and resource hash are recorded in
`artifacts/prepared-materials/Lift04/non-tiling/classification.json`.

## 2. Coverage and runtime consumption

| Source domain classification, all LODs | Count |
| --- | ---: |
| Retain both authored axes | 43,700 |
| Retain authored U, density-repeat V | 4,618 |
| Existing density route | 686 |
| Total | 49,004 |

`Lift04UvSampling.Axes(render, mesh, domain)` reads the independent lazy BBN1 resource
`Lift04UvSampling.bin`, containing 49,004 flags across eleven mesh/material branches.
The 196,132-byte resource uses schema 2: bit 1 retains U and bit 2 retains V.
`For` remains compatible and returns `Axes == 3`. Mask 0 keeps the existing density route.
Domain 3 and domain 28 use mask 1. Shared code must combine authored/interpolated U and density V
in original atlas coordinates before eligibility and parent-unit normalization.

`profile-continuations.json` records exact native face membership for 28 beam-side domains and
four finite end fascias across detailed and middle LODs. The beam sides share exact longitudinal
XYZ edges and V endpoint phase with measured profile faces. Their U coordinates are unchanged
subsets of the existing wide-profile unit [1910,2176] in the original 4096 atlas; two exact source
rectangles extend coverage without altering the unit or any texture. The four fascias instead join
finite end-cap charts across their short edges and retain their complete authored traversal under
native Clamp. Low LOD source mappings remain fully retained.

The historical 249 unresolved output units partition into 153 existing mixed mappings, 84 measured
beam continuation mappings and 12 full-retained fascia mappings. This source classification is
distinct from successful shared runtime replay. Negative controls preserve density for 33 beam
domains and 609 floor domains.

Regenerate with the repository Python runtime:

```text
python tools/GeometryMetaprogram/Lift04ProfileContinuations.py --write
python tools/PreparedMaterials/Lift04/InstallProfileContinuations.py
python tools/PreparedMaterials/Lift04/BuildUvSampling.py --write
python tools/GeometryMetaprogram/Lift04MixedProfiles.py
```

`InstallStructuralProfiles.py` also reapplies the continuation recipe after regenerating its three
original profiles. The independent source audit retains 117 channel/mip/rectangle comparisons in
`artifacts/material-resume/Lift04/outputs/mixed-axis-source-evidence.json`. Native reports preserve
both actual-coordinate phase error (about 0.000198) and the zero-error dyadic sampler isolation.
The latter does not prove actual-coordinate seam equivalence. Original source-grain differences
and finite-edge differences remain explicit; physical membership and the existing accepted repeat
unit semantics justify the two continuation mappings, not a new pixel-error tolerance.

The producer supports current schema-3 source domains through the shared exact decoder. Resource
checksum/readback and exact integer comparison are checked during generation. No geometry, bones,
material identities, texture pixels or localization text are changed. Selected-road materials do
not acquire this permission. Shared routing/build/native retention verification are separate from
this style-specific producer. Human near/far, motion and independent persisted-owner acceptance
remain pending.
