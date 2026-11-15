# Geometry identification procedure

Required companion: [Geometry metaprogram contract](../contracts/metaprogram-contract.md).

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
