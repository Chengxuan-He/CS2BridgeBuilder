# Lift05 structural semantic units

## Inventory and implementation

`GroupStructureUnits.py` groups all 47,941 original domains in 11 render/material
streams across every available LOD exactly once. The measured receiver inventory
contains 23,795 units at half-width deltas -1.25, -0.25, 0, 2, 8 and 24. This finite
sample is not proof of every future road width. Domains with no changed receiver
retain their original native Surface/UV mapping; their zero count is explicit.

The shared `AuditStructureUnits.py` verifies inventory completeness, disjoint
membership and evidence hashes. Its result is a bookkeeping check, not visual
acceptance. Durable inputs, output and reports are in
`artifacts/prepared-materials/Lift05/structure-groups/`.

Two semantic units come from complete original rectangular surfaces, without an
arbitrary crop, receiver-dependent texture or triangle-fold sampling:

| Unit | Original source | Source pixels | Physical period |
| --- | --- | --- | --- |
| Upper crossbeam painted strip | `LiftBridge05 Mesh`, material 0, domain 2133, faces 2501–2502 | 540 × 12 | 4 m × 0.060546875 m |
| Deck soffit painted panel | `LiftBridge05_LOD1 Mesh`, material 0, domain 12386, faces 14670–14671 | 610 × 456 | 4 m × 3.000014000107 m |

Both use original serialized Surface SHA256
`40c466ebabd798b8bca9674788deefa14c10490f9f011f5377c8e48535bb984e`.
The exact physical coordinates, face membership, complete source mip provenance,
periods and density are recorded in each unit's evidence JSON. The physical spans
above describe the original mapping, not a new uniform-density assumption.

`Lift05RepeatRegions.cs` dispatches only this source digest and these measured UV
rectangles. Each descriptor preserves structural template 0 and original packed
normal interpretation. Existing road template 14 is unchanged. The public mapping
preserves the receiver's continuous UV gradient and exact phase; it does not add
geometry or runtime texture generation. Regions do not expand to bounding boxes
of unrelated parts or to the entire atlas.

## Validation and footprint

One preparation batch produced six textures. Every channel has density ratio 1
and ten output mip levels sampled from the complete 13-level authored source.
The native reader verified complete descriptor/texture consumption, owner-local
control and texture dependencies, source color space, filter/aniso settings and
packed-normal semantics. Sixty channel/mip comparisons passed. Maximum linear
conversion error was 0.00341796875. Repeat sampling was consistent at its own
seam; this does not prove equality to the original atlas neighbours. The maximum
original-atlas boundary differences were 0.14706644 for the strip and 0.25392538
for the soffit across the tested channels/mips. These differences are retained
for human observation under the accepted existing-atlas pattern policy.

The isolated receiver harness compiled with zero warnings/errors and evaluated
66 cases. It found 1,675 receivers whose complete original domain is contained by
one of the two regions. The common exact-phase/gradient predicate accepted 1,541
mappings, including 1,349 additional units beyond the former `CanUseSource` test.
The remaining 134 containment candidates are not forced through the mapper.
Density, packing and footprint checks reported zero failures. The receiver binary
is byte-identical to the preceding complete six-width inventory.

`InstallStructureUnits.py` installs only the validated new payloads and refuses
conflicting existing files. It added 16 files, retained all previous 68 file hashes,
and verified the common transparent control identity. The current style package
contains 84 files totaling 912,445 bytes. This is the packaged Lift05 material
library, **not** the complete generated bridge material graph. The latter and the
complete official SDK package must still be measured separately.

## Remaining classes

| Disposition | Original domains | Sampled receivers |
| --- | ---: | ---: |
| Original native mapping / no observed changed receiver | 43,561 | 745 |
| Two measured repeated structural surfaces | 311 | 1,675 |
| Authored nonperiodic detail outside its original chart | 3,259 | 16,111 |
| Conditioned mixed islands | 233 | 1,790 |
| No reliable part chart | 355 | 1,771 |
| Interior glass | 8 | 40 |
| Far-LOD packed atlas | 214 | 1,663 |

Receiver counts are attached to whole domain groups, including cases that remain
original at individual widths; they are not counts of failed operations.

Nonperiodic and mixed islands need measured base/detail separation that preserves
the authored detail and phase. Ambiguous topology needs reliable offline part
membership before coverage can be extended. Interior glass requires its native
template 2 interior/transparency semantics, not a painted Default replacement.
Far-LOD packed islands contain baked details/shadows and cannot inherit the near
rectangle merely because their material appears similar. No old large fallback
library is restored, and no arbitrary per-triangle units are proposed for these
classes. The audit explicitly reports unresolved coverage.

Complete preview, saved-owner closure, regeneration, export, cold load, independent
deletion/uninstall and filtered game appearance remain coordinating integration
and human acceptance. No complete-generation or ten-megabyte asset claim is made.
No user-visible strings or localization behavior change.
