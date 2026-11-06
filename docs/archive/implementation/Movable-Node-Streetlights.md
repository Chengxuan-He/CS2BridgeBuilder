# Movable node streetlight aggregation

## Native evidence

Game 1.6.2f1 `SecondaryObjectSystem.CreateSecondaryNodeObjects` gathers candidates across
connected-edge node halves before selecting concrete streetlight prefabs. Source locations in
`work/bridge-research/Game/Game.Objects/SecondaryObjectSystem.cs` are the node-geometry
accumulation near lines 1307-1320, candidate collection near 1495-1528, and arbitration at
1775-1855. Ordinary edge placement cannot replace this process.

## Implemented chain

`MovableNodeStreetLights.Visit` now calls `MovableNodeObjectPlacement.Visit` for each supplied
non-middle node contribution. It reads the original prefab's initialized StreetLightData and
queues light candidates; other candidates continue through the caller's callback. Both callbacks
share the same random stream. No city entity is created or modified.

The implementation preserves these native rules:

- Accumulate each contribution's native length expression and retain the last middle-curve end
  as the arbitration reference point. Count contributions even if they contain no light rules.
- Reject a candidate within squared distance 1 of any earlier queued light, before comparing
  light layers. Keep the first candidate, not the latest or highest-priority duplicate.
- Process the first pending layer; choose its initial candidate by distance plus curve-length
  priority, with strict greater-than comparisons preserving tie order.
- Calculate retained count from accumulated length, layer candidate count and summed spacing.
  Preserve the four-contribution correction from three retained candidates to two.
- Choose later candidates by minimum distance from already selected candidates plus native
  priority. Resolve concrete prefab selection only after collection and arbitration.
- Remove each processed layer with swap-back ordering, preserving subsequent layer/random order.
- Return native hasStreetLights from the collected candidates, even when retained count is zero.
  Preserve the separately accumulated tunnel/transition AlwaysLit condition.

Distances here reproduce native secondary-object placement, not authored structural-part
classification. No runtime bridge geometry classification or custom lighting controller is added.

## Remaining integration

The final generator has not yet supplied the prepared connected-node graph. Traffic sign/light
needs, utility objects, endpoint lane context, persistent attachment assembly and full generator
routing remain unfinished. Callers must collect contributions in native EdgeIterator order and
resolve lights at the corresponding native phase; collecting all other object types later would
change the random stream. No in-game placement or cold-start acceptance is claimed.

The inspected native sign logic also removes MoveableBridge signs unless the network intersects
waterways, the edge carries FixedNodeSize and the node does not. The sign's positive request is
subsequently derived from target-lane context. This evidence rules out treating empty node sign
needs or ordinary edge needs as equivalent; that path still needs implementation.

## Build and deployment

Release compilation passed with the nine existing TrafficSignNeeds CS0649 warnings. The final
increment backed up and verified 18 original installed files at
`artifacts/backups/movable-node-light-state-20261016-061701`, then verified 12 installed payloads.
The preceding arbitration increment used `movable-node-streetlights-20261016-061629`.
No routed generator output changed and no existing bridges were removed. Validation consisted
of native-source comparison, caller inspection and compilation; no visual-generation unit tests
or game automation were run.
