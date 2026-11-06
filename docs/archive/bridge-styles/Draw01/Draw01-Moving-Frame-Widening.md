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

The narrow central member (high first faces 3763/11657) has measured half-span
0.40000152587890625 m. Applying the mandated own-span mapping with an equal or larger
negative half-width increment collapses or reverses it. Actual native inspection at a full
width reduction of 4 m exposed this condition. Apply throws for a nonpositive resulting
member span, consistently across LODs, instead of emitting inverted surfaces with unchanged
packed normals. It does not clamp the selected width or change the mandated part mapping.
This limits supported narrower-road combinations and must not be described as unrestricted
road-width support. A different treatment would require resolving the project's part rules,
not silently changing the transformation in this data table.

Apply receives the half-width increment and a complete original main-mesh position array. It
changes X only for recorded moving vertices. It skips zero-coefficient operations to preserve
native center residues and signed zero. No Y/Z, bone, material, topology, UV or other attribute
is changed. The caller must first bind the original source identity and clone its positions.

This is only the moving portion of structural widening. CreateMainMeshPlan now copies the
source positions and invokes this mapping and the fixed-frame mapping before constructing
the recorded road-boundary vertices. Separate glass, pillars, footings and attachments require
their corresponding operations. Do not widen appended vertices a second time or use this
moving-only helper as a complete Draw01 plan.

Complete generator routing and native human near/far, width, motion and shipping acceptance
remain outstanding. This source increment has no final generator caller and changes no existing
bridge output by itself.
