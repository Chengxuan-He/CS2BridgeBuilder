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

The hardcoding requirement is retained because other players may lack the original prototype files.
Compact measured profiles, ranges or recipes are allowed instead of exhaustive vertex literals when
they preserve the same part identities and result. No requirement to finish all styles' audits before
integrating an executable stage is implied by this identification phase.

## Output contract

- Emit reviewed, immutable source data keyed by exact archetype and mesh/LOD identity, with exact
  component coordinates or vertex membership. Runtime code consumes that data without geometry
  guessing.
- Derive every LOD from the highest-detail part classification. Coarse welding must not change a
  side part into a centre-crossing part or vice versa.
- A base adjustment emits the mapping `x -> x + sign(x) * delta`, not a width scale.
- Preserve side-part shape under translation and preserve each crossing member's own span under
  stretching. Keep bridge-pier columns translational.
- Inspect both near and far generated geometry. Automated checks may verify geometry invariants; they do not establish visual acceptance.

## Iteration and deployment

Follow the current bridge generation contract sections 12–13. Style branches are optional; keep
variant-specific measured data separate. Build and deploy coherent code increments. Documentation
and analysis artifacts alone require no deployment. Preserve healthy bridge assets unless an
identified incompatibility requires scoped, backed-up recovery. Do not pause implementation merely
because another style's tables or human visual acceptance are unfinished.

## English documentation maintenance (2026-10-13)

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.
The project contract records the expanded 2026-10-15 documentation policy. This language-only revision preserves
existing requirements, exceptions, identifiers and historical evidence. Baseline: branch `dev`,
HEAD `5bea1d0bef04dd2ac718809852d3f657c7388e58`; rollback reference
`refs/rollback/english-contracts-guide-20261013`. Existing local changes are preserved.
