# Geometry Metaprogramming Instructions

## Scope

These instructions apply to `tools/GeometryMetaprogram/**`. They add metaprogram-specific requirements
to the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read sections 1–9
and 13 of [`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md).

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

Changes in this directory are source-code updates. After each update, perform the mandatory
`Cities2.exe` stop, process verification and removal of every mod-created bridge and generated
artifact described in project-contract section 13.
