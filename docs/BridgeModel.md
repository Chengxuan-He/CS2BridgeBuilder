# How Cities: Skylines II models a bridge

This document describes the native components used by Bridge Builder and the current generator
boundaries. The project contract governs implementation and acceptance. Prototype measurements
and dated audit reports are evidence for their recorded versions, not substitutes for current data.

## Network and style identity

A bridge network is a `RoadPrefab`, `TrackPrefab` or `PathwayPrefab` derived from
`NetGeometryPrefab`. `BridgeBuildStyle` describes ground treatment (`Elevated`, `Raised`, `Quay`),
not a suspension, arch or cable-stayed design.

Bridge Builder uses explicit stable style IDs, supported deck modes and generator routes.
Runtime discovery supplies available prototype variants; an installed prefab alone does not enable
an unsupported generator. Source binding prioritizes base game, DLC, then mod content and preserves
the selected source's prerequisites. Generated bridges are excluded from source selection.

## Native components

| Component | Responsibility |
| --- | --- |
| `Bridge` | Span length, hanging, water elevation, curvature and fixed-segment behavior |
| `m_Sections` | The selected network's road/track structure and composition conditions |
| `OverheadNetSections` | Additional authored sections such as cables and trusses |
| `NetSubObjects` | Towers, piers and other objects with prototype-specific placement rules |
| `AuxiliaryNets` | Additional networks with native ownership, offset and inversion behavior |

These components form a graph, not a universal list that can be copied blindly. Preserve each
prototype's behavior and references while replacing the selected road structure and deriving the
required geometry. Fixed segments require compatible section states and placement indices.

## Geometry and width

Generation selects a compatible prototype and uses measured, immutable archetype data. Towers,
cables, pieces and LODs are private derived assets. A part reaching or crossing `x = 0` stretches
against its own authored span; a side part translates rigidly. Apply the contract's measured
base, centred-post, Draw03 truss and Golden ornament/support exceptions. Preserve native behavior
and apply only the scoped topology, representation and density-preserving material conversions.

Full-detail geometry and all LODs use the same part classification. Missing or mismatched metadata
is an explicit unsupported case. There is no general offset-only fitting rule or arbitrary
0.5x–2x clamp. Width means the complete span `maxX - minX`, measured on matching boundaries.

## Double decks

Use a genuine double-deck archetype. Preserve its `AuxiliaryNets` positions, `m_InvertWhen`,
`m_LinkEndOffsets` and fixed deck separation. Main/auxiliary ownership does not determine which
deck is physically above the other. Both selected decks receive bridge-owned network copies.

Auxiliary networks follow native editing and connection behavior. The double-deck rail-seam
compatibility limitation remains subject to explicit testing, including a cold start without
Bridge Builder. Successful generation alone does not establish independent operation.

## Movable bridges

Development is governed by the normative
[movable bridge contract](agent-contract/MOVABLE_BRIDGES_CONTRACT.md), incorporated by bridge
generation contract section 21. The required route generates the moving surface from the selected road's
closed-pose composition, bakes persistent object geometry and binds it to the measured mechanism.

Native draw and lift bridges use an animated static-object prefab placed at a waterway-crossing
node. That object carries `MoveableBridge`; its render prefabs carry procedural bone animation.
Native traffic signals coordinate road/track/pedestrian clearance, bridge movement and watercraft.
The road sections hide ordinary node meshes in the fixed-size moving span while preserving network
connectivity; the animated bridge model provides the visible moving deck.

The router contains generators for Draw01–Draw03, Lift01–Lift05 and PedestrianDraw01/02.
Lift03 retains its native road/rail auxiliary arrangement. Generator routing does not establish
acceptance of every road, LOD, motion cycle or cold-load scenario. Consult the
[implementation record](Movable-Bridges-Implementation.md) for evidence and remaining work.
Material migration status is recorded in [prepared materials](Prepared-Materials.md).

In the audited highest-detail primary render prefabs, each mechanism has one principal opaque
mesh with one submesh, plus a separate window mesh. This does not establish which triangles are
road, structure or markings. Removing the opaque submesh would remove structure as well; a
reviewed triangle-level separation must identify the prototype road contribution before replacing
it with the selected road. Repeat the correspondence review for every LOD.

These primary meshes use `BlendIndices:UInt32x1`. Native procedural bone rest transforms, bind
poses, hierarchy and motion fields live in `ProceduralAnimationProperties`. This must be traced
together with the geometry's skinning channels. An empty `ModelImporter.Model.BoneInfo` array in
an existing export helper alone does not prove that the saved native animation graph lacks bones.
Likewise, a valid skinning layout does not establish material compatibility for a baked network.

## Persistence and cleanup

Generated bridge assets use immutable `b<UUID>` identities, independent of editable display names.
External non-built-in dependencies are copied byte-for-byte with original CIDs into each bridge's
own dependency directory. Built-in game/DLC resources remain native prerequisites. Original assets
and other bridges' private Prefabs are not modified or borrowed. Asset licenses remain applicable.

Persist native unlock conditions, pricing and presentation. Bridge Builder is the asset manager,
not a required runtime dependency; cold-start acceptance must verify that requirement.
Deployment follows bridge generation contract section 13: preserve healthy bridges. Recovery
requires a demonstrated incompatibility and is limited to the affected UUID-owned assets.
A source change alone does not authorize deleting every bridge of a style.
