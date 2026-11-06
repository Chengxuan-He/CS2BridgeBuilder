# Geometry Metaprogramming Instructions

## Scope

Lift01 and Lift02 central-truss memberships follow the explicit complete-assembly rule in
bridge generation contract section 8, overriding default own-span stretching,
side-part translation and base mapping for those measured members. Internal
braces and narrow connectors share their complete assembly's `x -> k*x`; only the outermost
edge trusses retain their original mapping. Record assembly spans and connection
residuals offline, apply the rule at every LOD including far supplements, and
preserve bones/material modes with the corresponding affine normal/tangent frame.
Measure each style's complete source and target assembly spans; never use a small
connector's thickness or another style's span as its scale denominator.


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
  crosses `x = 0`, subject to contract section 8's explicit base, centred-post, Lift01/Lift02 complete-assembly and Draw03
  complete-truss rules, the measured Draw02 central hinge plates and central longitudinal girder's original-section
  exception and internal rib distribution, and section 5's measured Golden mappings. Metaprogramming records membership;
  it may not invent exceptions.
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
Emit request-scoped cached accessors for decoded arrays and derived records, not permanent static
numerical fields. Keep extraction reproducible, preserve numeric bits and order, and
run the resource conversion/check after regenerating C# recipes. Do not ship giant numeric literal
initializers. Compound records must be reconstructed with bounded loops over the stored arrays.

Emit measured affine coefficients for each affected axis, including Y and Z when they depend
on width. Constant axes use zero coefficients; retain sparse per-vertex or part-range recipes
instead of expanding every record into a dense matrix. Preserve parameter domains, anchors,
arithmetic order and binary numeric bits. Follow the
[CPU affine execution contract](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
and review the [offline lowering tool](../AffineGeometryMigration/README.md) after regeneration.
Compile explicit packed XYZ stages and lane activity offline. Merge compatible axis work
without changing evaluation order; runtime must not recognize expressions or execute
per-axis register columns. Preserve untouched-axis bits rather than emitting identity arithmetic.
Retain manually reviewed request-owned coefficient caches and native snapshot boundaries; automatic lowering
does not prove a loop is safe to parallelize or replace geometric equivalence checks.

- Emit reviewed, immutable source data keyed by exact archetype and mesh/LOD identity, with exact
  component coordinates or vertex membership. Runtime code consumes that data without geometry
  guessing.
- Derive every LOD from the highest-detail part classification. Coarse welding must not change a
  side part into a centre-crossing part or vice versa.
- A base adjustment emits `x -> x + sign(x) * delta`, except the user-approved Lift01/Lift02 complete-assembly mapping and Draw03
  complete moving and fixed truss stretch in the bridge contract's deck-adjacent base section
  and the measured Draw02 central-girder/panel-join rule in section 8. Record the adjoining
  panel boundary separately: its fixed inner edge must remain connected to the original
  girder while its outer edge follows the rigid longitudinal rib. Verify final shared edges,
  not merely each part's face presence or bounding box.
  Draw02's measured transverse supports directly below the moving deck are base members:
  use signed displacement, not their previous own-span scale. Keep their exact source
  membership separate from the central girder, horizontal closure panels and hinge axles.
  Identify the two central hinge plates separately from the axle and central girder.
  Their original sections and central positions remain unchanged, corresponding to
  the matching rigid lateral plates. Do not use plate thickness as an assembly span.
  Identify Draw02's six thin longitudinal ribs separately: preserve their sections,
  apply the recorded quarter-width translations, and carry adjoining original panel
  endpoints with the same rib. Retain the central anchor and original outer offsets.
  Its downward outer deck slab and vertical closures are structural even without a
  Sidewalk section; do not include the replaced upward road surface in that membership.
  Record its original high-rail contact edges separately. General section 8 permits
  their necessary original base-top subdivision and lower-domain frame transform;
  preserve the lower beam anchor and verify rail contact plus car-space clearance.
- Outside the explicit complete-assembly exceptions, preserve side-part shape under translation
  and preserve each crossing member's own span under stretching. Keep bridge-pier columns translational.
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
