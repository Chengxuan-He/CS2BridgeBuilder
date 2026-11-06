# Movable crossing-node attachment placement

## Native behavior and corrected implementation direction

The crossing span is a fixed-size node. The native `SecondaryObjectSystem` dispatches node
owners to `CreateSecondaryNodeObjects`, separately from `CreateSecondaryEdgeObjects`.
Ordinary edge repetition/spacing must not be presented as the complete moving-node placement path.

For each non-middle connected-edge contribution, native node placement reads the corresponding
start/end composition and `EdgeNodeGeometry`. It folds the curve-offset range, samples its native
random phase, and selects one of these paths:

- Median/preserve-shape placement along the middle curve and its transverse vectors.
- Radius-node placement across the two remapped curve halves.
- Ordinary node-side placement with the composition's sync offsets and geometry's sync targets.

It then derives the native curve rotation, local offset transform, and length-dependent probability.
It does not use the edge path's repeated-object count as a substitute. The native right end tangent
uses `StartTangent(edge.m_End.m_Right)`; this implementation preserves that observed choice.

## Implemented scope

`MovableNodeObjectPlacement.Visit` now constructs these candidates in shared code on `dev`.
It accepts actual prepared node geometry/composition rather than fabricating empty endpoints.
Its callback receives the composed candidate, native curve-length priority, lowered state and
shared random stream. Source arithmetic and branch order match the inspected native method after
identifier/type renaming; `Candidate` performs the original local-to-world transform.

This is candidate placement, not a complete native node-object selection implementation. Native
streetlight aggregation/deduplication, traffic-sign/light requirements, utility-anchor handling,
connected-edge ordering, and prepared endpoint lanes must still be connected before producing final
attachments. The existing edge-only `MovableRoadObjects.ResolvePlacements` is not silently reused
for node owners. No final generator currently calls the new node-placement method.

## Validation and deployment

The native arithmetic source sequence was compared after explicit renaming and whitespace removal;
no synthetic geometry or visual unit tests were executed. Release compilation passed with the nine
existing TrafficSignNeeds CS0649 warnings after fixing a local parameter-name collision.
The failed build was not deployed. Successful deployment verified 18 original backup files and all
12 installed payload files at `artifacts/backups/movable-node-object-placement-20261016-061447`.
No existing routed generator output changed, so no existing bridges were removed.

Native source evidence: `work/bridge-research/Game/Game.Objects/SecondaryObjectSystem.cs`,
`CreateSecondaryNodeObjects` and `Remap`/`LerpRemap`/`LerpRemap2`, game 1.6.2f1.
Compilation/source agreement does not prove in-game placement, persistence or completed support.
