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

| Render family / LOD | Retain authored UV0 | Density-controlled overlap | Unknown |
| --- | ---: | ---: | ---: |
| Moving structure LOD0, slot 0 | 24,749 | 2,633 | 0 |
| Moving structure LOD1, slot 0 | 15,088 | 2,675 | 0 |
| Moving structure LOD2 | 1,153 | 0 | 0 |
| Glass LOD0 and LOD1 | 8 | 0 | 0 |
| Pillar LOD0 / LOD1 / LOD2 | 504 / 452 / 120 | 0 | 0 |
| Pillar base LOD0 / LOD1 / LOD2 | 794 / 712 / 116 | 0 | 0 |
| Total | 43,696 | 5,308 | 0 |

`Lift04UvSampling.For(render, mesh, domain)` reads the independent lazy BBN1 resource
`Lift04UvSampling.bin`, containing 49,004 flags across eleven mesh/material branches.
The 196,132-byte resource uses schema 1. Only flag 1 authorizes full authored-UV retention.
Flags 0 retain the shared density route; they do not assert that every overlapping domain is itself
a repeat unit. Domain 3's confirmed mixed outer-flange mapping and domain 28's overlapping profile
remain flag 0. This boolean route does not implement mixed-axis retention.

Regenerate with the repository Python runtime:

```text
python tools/PreparedMaterials/Lift04/BuildUvSampling.py --write
```

The producer supports current schema-3 source domains through the shared exact decoder. Resource
checksum/readback and exact integer comparison are checked during generation. No geometry, bones,
material identities, texture pixels or localization text are changed. Selected-road materials do
not acquire this permission. Shared routing/build/native retention verification are separate from
this style-specific producer. Human near/far, motion and independent persisted-owner acceptance
remain pending.
