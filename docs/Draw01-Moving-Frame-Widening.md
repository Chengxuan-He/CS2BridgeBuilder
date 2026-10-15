# Draw01 moving-frame widening data

## Measured correspondence

Draw01MovingWideningData records transverse displacement for retained moving geometry in main
mesh 0 at all three LODs. The 88 center-spanning high/middle member pairs were inspected from
actual native X/Z projections: 44 on each leaf, including lower/upper cross members, hinge
members, the central longitudinal member and diagonal braces. Side-only assemblies translate
rigidly. The highest-detail member determines the crossing classification and its span.

Some simplified middle diagonal endpoints are shortened by up to 0.093689 m. Their coefficient
uses their original high-detail member's span; independently dividing by a shorter middle span
would make LODs deform differently. Repeated braces are paired by their reviewed orientation,
location and native geometry, not classified at runtime. Exact source OBJ hashes accompany each
table, and the complete 88-pair inventory remains in the native inspection record.

| LOD | Original moving vertices covered | Distinct displacement coefficients |
| --- | --- | --- |
| 0 | 19536 | 13 |
| 1 | 12248 | 14 |
| 2 | 542 | 3 |

Far geometry aggregates the same members into envelopes. Its side stations use rigid +/-1
coefficients and its measured center stations use 0. The 70 tiny native negative-X centerline
residues remain centered by explicit identity, never by their sign. Middle tiny coordinates
remain their measured proportional coefficients; no blanket near-zero snapping is introduced.

## Application

Apply receives the half-width increment and a complete original main-mesh position array. It
changes X only for recorded moving vertices. It skips zero-coefficient operations to preserve
native center residues and signed zero. No Y/Z, bone, material, topology, UV or other attribute
is changed. The caller must first bind the original source identity and clone its positions.

This is only the moving portion of structural widening. Fixed bridgeheads, control buildings,
other meshes, pillars, footings and attachments require their corresponding plans. After all
original vertices have their complete widening, CreateMainMeshPlan constructs the recorded
road-boundary vertices from those widened source positions. Do not widen appended vertices a
second time or use this moving-only helper as a complete Draw01 plan.

Complete generator routing and native human near/far, width, motion and shipping acceptance
remain outstanding. This source increment has no final generator caller and changes no existing
bridge output by itself.
