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
  crosses `x = 0`, subject to contract section 8's explicit base, centred-post and Draw03
  complete-truss rules and section 5's measured Golden mappings. Metaprogramming records
  membership; it may not invent exceptions.
- The base is the complete structure immediately adjacent to and directly below the road deck.
  Identify the assembly offline; its individual side blocks need not cross `x = 0`.
  Do not substitute a pier, footing, foundation or similarly named prefab.

The hardcoding requirement is retained because other players may lack the original prototype files.
Compact measured profiles, ranges or recipes are allowed instead of exhaustive vertex literals when
they preserve the same part identities and result. No requirement to finish all styles' audits before
integrating an executable stage is implied by this identification phase.

## Output contract

Recorded numerical output must be converted to the embedded typed-array resource format in bridge
generation contract section 2: store each uncompressed BBN1 resource under its bridge-type
directory. Runtime reading is on demand directly from the embedded stream, without extraction.
Keep extraction reproducible, preserve numeric bits and order, and
run the resource conversion/check after regenerating C# recipes. Do not ship giant numeric literal
initializers. Compound records must be reconstructed with bounded loops over the stored arrays.

Emit measured affine coefficients for each affected axis, including Y and Z when they depend
on width. Constant axes use zero coefficients; retain sparse per-vertex or part-range recipes
instead of expanding every record into a dense matrix. Preserve parameter domains, anchors,
arithmetic order and binary numeric bits. Follow the
[CPU affine execution contract](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
and review the [offline lowering tool](../AffineGeometryMigration/README.md) after regeneration.
Retain manually reviewed coefficient caches and native snapshot boundaries; automatic lowering
does not prove a loop is safe to parallelize or replace geometric equivalence checks.

- Emit reviewed, immutable source data keyed by exact archetype and mesh/LOD identity, with exact
  component coordinates or vertex membership. Runtime code consumes that data without geometry
  guessing.
- Derive every LOD from the highest-detail part classification. Coarse welding must not change a
  side part into a centre-crossing part or vice versa.
- A base adjustment emits `x -> x + sign(x) * delta`, except the user-approved Draw03
  complete moving and fixed truss stretch in the bridge contract's deck-adjacent base section.
- Preserve side-part shape under translation and preserve each crossing member's own span under
  stretching. Keep bridge-pier columns translational.
- Inspect both near and far generated geometry. Automated checks may verify geometry invariants; they do not establish visual acceptance.

## Iteration and deployment

Follow the current bridge generation contract sections 12–13. Style branches are optional; keep
variant-specific measured data separate. Build and deploy coherent code increments. Documentation
and analysis artifacts alone require no deployment. Preserve healthy bridge assets unless an
identified incompatibility requires scoped, backed-up recovery. Do not pause implementation merely
because another style's tables or human visual acceptance are unfinished.

## English documentation maintenance

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.
