# Lift04 material continuation

Current state: finite native profile integration passes; full bridge acceptance
readiness is not yet established. No deployment was performed.

The current two profile products use public `repeatAxes="V"` authoring and
`Region(..., repeatU: false, repeatV: true)` consumption. The nonperiodic U
profile occupies the central half of a clamped guard texture. Wide and long
beam source coverage rectangles are separate. Bounds reside in the 256-byte
`Types_Lift04_StructuralProfileRegions.bin` resource; geometry and bones are
unchanged. No source atlas is packaged and no receiving-triangle pixels are baked.

Evidence is under `artifacts/prepared-materials/Lift04/structure-inventory/guarded-profiles`:

- Native decoding and pixel-center comparisons pass 57 channel/mip levels:
  wide beam 532x404 with 10 mips, flange 32x404 with 9 mips. Template 0,
  packed-normal interpretation, complete channel graph and density 1 on both
  axes pass. Boundary differences are recorded for human visual observation.
- A repeated prepare run reuses all six textures and produces zero textures.
- Historical source-linked replay passes on six sampled width deltas and all eleven
  original mesh/material branches. Receiver triangles: 33,470; native: 5,626;
  genuine units: 5,218; remaining: 22,626. The original beam counterexample passes.
- Remaining reasons: nonperiodic-axis extrapolation 5,560; outside measured
  regions 15,080; different source hash 1,986. No float-gradient failures.
- The original 49,004-domain inventory has thirteen complete dispositions.
  `remaining-constraints.json` gives exact unresolved counts; mixed group totals
  must not be used as unresolved counts.

The exact shared pre-Build Retile capture has now been classified through the
production `CanUseSource` and `RegionMapping` methods. The 23,653 emitted Units
classify as 5,457 original, 3,325 prepared-repeat and 14,871 remaining. Captured
direct-reuse parity has zero differences. Remaining Units comprise 10,836 outside
recorded coverage, 2,816 contained but rejected by mapping, and 1,219 without a
source region. These are Unit counts, not triangle counts. Evidence and the eleven
exact native source mappings are in `artifacts/prepared-materials/Lift04/actual-retile/`.

`frame-evidence.json` and `frame-orientation-evidence.json` identify a concrete
shared mapping defect for domains 3 and 28 of `LiftBridge04 Mesh`, mesh 0.
Reflection invokes the production plan and `MovableUvFrame`; source positions/UVs
match the native NPZ, and normalized frame outputs match the actual captured Units.
Both planes retain their Y/Z coordinates exactly under the measured X stretch.
Native U follows Y (domain 3) or Z (domain 28), while native V follows X. The current
frame rotates with its selected diagonal edge, introducing an unintended U gradient
along X. At delta 24 the normalized U maxima are 5.7038116 and 4.298477; retaining
the measured planar source axes keeps U exactly in [0,1] after Float32 storage.
The shared owner is investigating a rotation derived from the measured affine map;
this evidence does not authorize a universal world-axis rule or alter geometry.
Production metric preservation alone does not establish authored pattern orientation.
No new texture, native mip verification or completed mapping repair is claimed by
this diagnostic. Reproduce with `CaptureFrame.cs` and `CheckFrameEvidence.py`.

Formal prepared files total 1,124,560 bytes (94 files); this increment is 323,658
bytes. The target is a complete generated-owner material dependency closure at
most 10,000,000 bytes, not a padded library size. Full generated-owner closure,
cold persisted reload and remaining material paths are still unverified.

Next required work is native composition preserving the nonperiodic normal,
mask, edge, rivet and joint details on uncovered structural parts, plus separate
evidence for LOD2 source materials. Turning on U repeat or accepting similar
base color would erase original cross-section detail and is invalid. Public
bounded variable-region composition may be used only with explicit mapping and
all-channel density/phase evidence; known constant source preparation stays
offline. Existing 160 prior-beam receivers still depend on their old unit.

Style tools: `PrepareStructuralProfileUnits.py`, `InstallStructuralProfiles.py`,
`PrepareRegionReceiverCheck.py`, `RegionReceiverCheck.cs`,
`AuditStructureInventory.py`. The native checker is `RepeatUnitCheck.Editor.cs`
with `RepeatUnitCheck.compute`. The independent Unity project and product store
remain under `artifacts/agent-material-env/Lift04`; sync current public producer
snapshots before future baking. Shared pack/build must include the new BBN
resource. No localization text or original structural geometry was changed.
