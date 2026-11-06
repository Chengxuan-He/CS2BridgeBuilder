# Golden suspension support mapping

## Required transform

The recorded below-deck centre truss retains its original coordinates. Emit zero displacement
for its complete full-detail membership and inherited LOD1/LOD2 membership. Side structures
and railings remain width-adjusted. Each outer vertical return follows its complete side upright
rigidly, as defined by [return alignment](GOLDEN_RETURN_ALIGNMENT.md).
Do not stretch the recorded centre assembly or apply height-dependent transforms to one member.

## Source evidence and generation data

The reported filled triangles belong to SuspensionBridge03SupportEnd and
SuspensionBridge03SupportMiddle, not the pylon. The native geometry and retained generated
sample ba17e0bce-eb88-4dfa-8f6c-506d0507671f have matching index/vertex correspondence.
A transverse cut at z=-4 shows the native panel and the generated panel's torn centre column.
Incompatible displacement at different heights caused the defect.

GeometryMetaprogram --golden-support identifies the complete panel and connector islands
offline against the full-detail side-truss inner face at x=±9.239746. Record exact membership,
not that analysis threshold, in runtime data. All other upper pieces retain their recorded
centre-crossing or side identity. The six maps contain End 82394/62783/3498 vertices and
Middle 198956/166579/6352 vertices. Runtime validates exact mesh identity and vertex count;
it consumes immutable coefficients without rediscovering the assembly.

Preserve topology, material semantics and Y/Z. UV density follows the material contract.
The centre-truss identity mapping and rigid outer-return mapping apply at every LOD.

## Verification boundary

Native/generated evidence and projections are retained in
C:/Users/admin/Downloads/BridgeBuilder-railing-audit-20260921.
The six regenerated maps compiled successfully. The available evidence does not establish
deployment or human near/far acceptance of the identity mapping. Projections of a different
mapping must not be used to claim that acceptance.
