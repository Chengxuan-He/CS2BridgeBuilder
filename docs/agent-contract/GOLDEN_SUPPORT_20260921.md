# Golden suspension support repair — 2026-09-21

Branch: `bridge/suspension-golden`. Baseline: `a420a55`.

## Evidence

The reported filled triangles belong to `SuspensionBridge03SupportEnd` and
`SuspensionBridge03SupportMiddle`, not to the pylon. The shipped BridgesAndPorts
geometry and retained generated bridge `ba17e0bce-eb88-4dfa-8f6c-506d0507671f`
have identical index/vertex correspondence. A transverse cut at z=-4 in the end
section shows the complete original `\|/` panel and the generated panel's torn
centre column. The old height-varying scope gives vertices of that same authored
panel incompatible displacements.

## Implementation

`GeometryMetaprogram --golden-support` records the complete panel, including its
connector islands, against the full-detail longitudinal side-truss inner face
at x=±9.239746. The transverse assembly stretches against this one span; the
side longitudinal structure translates. All other upper pieces retain their
recorded centre-crossing/side identity. Both LODs inherit the full-detail labels.
The generated immutable maps contain every vertex, keyed by exact render-prefab
name; runtime checks vertex count and consumes coefficients without rediscovery.
The factory selects this path before the generic height-slice transformation.

Six maps: End 82394/62783/3498 vertices; Middle 198956/166579/6352 vertices.
No channels, triangles, materials or y/z coordinates are changed.

## Validation status

Inspected projections of the actual prototype vertices after the recorded map
at the reported +11 m width: End and Middle at full, LOD1 and LOD2 retain the
`\|/` opening. Both source and failing generated geometry are retained in
`C:/Users/admin/Downloads/BridgeBuilder-railing-audit-20260921`.
`prototype-golden/mapped-panels.png` records the six inspected projections.
Compilation succeeded. No unit tests were run.

## Superseding user requirement, 2026-09-21

The user explicitly requires the below-deck centre truss to retain its original
coordinates. This replaces the transverse-stretch choice described above only
for that recorded assembly. The metaprogram now emits zero displacement for
its full-detail membership and inherited LOD1/LOD2 membership. Runtime leaves
those source vertices untouched; side structure and railings are not frozen.
All six maps were regenerated from the retained real prototype geometry and
the branch compiled successfully. This revision is not deployed or accepted
in game yet.

Pending: deployment and human in-game near/far acceptance. Offline geometry is
not game acceptance. Do not deploy this old worktree's unrelated runtime/UI over
the current dev installation; integrate only these source changes.
