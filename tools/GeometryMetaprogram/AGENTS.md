# Geometry Metaprogramming Instructions

## Scope

These instructions apply to `tools/GeometryMetaprogram/**`. They add metaprogram-specific requirements
to the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read sections 1–9
and 13 of [`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).

## Identification phase

- This directory is the only phase allowed to use temporary non-zero coordinates, bounds, topology,
  connected components, height bands, nearest vertices or other geometric analysis to identify the
  archetype's authored parts.
- Temporary thresholds are analysis inputs only. Do not emit them as runtime selection rules.
- Identify the highest-detail archetype's logical parts accurately, including every style of riveted
  connector, side structure, centre-crossing member, base, bridge-pier column and decoration required
  by the contract.
- Decide stretch versus rigid translation only from whether the complete authored part reaches or
  crosses `x = 0`. Metaprogramming may discover membership; it may not override that decision.
- The base is the centre-crossing structure immediately adjacent to and directly below the road deck.
  Reject a candidate that does not reach or cross `x = 0`; do not substitute a pier, footing,
  foundation or similarly named prefab.

## Output contract

- Emit reviewed, immutable source data keyed by exact archetype and mesh/LOD identity, with exact
  component coordinates or vertex membership. Runtime code consumes that data without geometry
  guessing.
- Derive every LOD from the highest-detail part classification. Coarse welding must not change a
  side part into a centre-crossing part or vice versa.
- A base adjustment emits the mapping `x -> x + sign(x) * delta`, not a width scale.
- Preserve side-part shape under translation and preserve each crossing member's own span under
  stretching. Keep bridge-pier columns translational.
- Inspect both near and far generated geometry. Unit tests are not evidence for visual component
  generation and must not be run for that purpose.

## Post-update cleanup

After code updates in this directory, perform the mandatory `Cities2.exe` stop and process
verification. If the change affects bridge generation, trace the affected style IDs and remove
only those styles' mod-created bridges and UUID-owned artifacts under contract section 13.
This includes changed archetype parameters or generated geometry data consumed by runtime.
If bridge-generation code/data is unchanged, remove no existing bridges. Documentation-only edits
require no shutdown, cleanup, build or deployment. After completing any code change, immediately
build and deploy the updated mod to the game without waiting for another request. Back up the existing
installation and verify installed files against the build output as required by that section.

Cleanup-policy revision (2026-10-15): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/scoped-cleanup-docs-20261015`.

## English documentation maintenance (2026-10-13)

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.
The project contract records the expanded 2026-10-15 documentation policy. This language-only revision preserves
existing requirements, exceptions, identifiers and historical evidence. Baseline: branch `dev`,
HEAD `5bea1d0bef04dd2ac718809852d3f657c7388e58`; rollback reference
`refs/rollback/english-contracts-guide-20261013`. Existing local changes are preserved.
