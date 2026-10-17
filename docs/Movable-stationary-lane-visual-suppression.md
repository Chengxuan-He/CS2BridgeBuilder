# Movable span stationary lane-visual suppression

## Native path inspected

The selected road must keep traffic lanes while its visible moving surface and markings are reconstructed on the mechanism. Hiding section geometry is not equivalent to deleting lane semantics. The native new-node-lane path supplies the required separation.

1. `NetInitializeSystem` assigns each selected piece one of the native Surface, Bottom, Top or Side flags from its `NetPieceLayer` (around lines 1265每1279).
2. `NetCompositionHelpers.GetCompositionPieces` converts hidden layer bits into a piece mask and adds `NetSectionFlags.Hidden` to matching composition pieces (around lines 650每710). A moving-node `m_HiddenLayers = -1` covers all four native layers.
3. Composition layout adds `CompositionState.Hidden` for hidden pieces, but clears it when a visible ordinary piece remains (around lines 930每970). Therefore the whole resolved composition must be inspected; hiding one section is insufficient.
4. On creating a node lane, `LaneSystem` checks the hidden state of either endpoint composition and removes `CullingInfo`, `MeshBatch` and `MeshColor` (around lines 7510每7518; set definition around 9278). It still writes the lane, curve and role-specific traffic data. This is rendering suppression, not removal of traffic connectivity or signals.
5. `SecondaryLaneSystem` records hidden status when an existing primary lane has geometry but no `CullingInfo`, carries it through lane corners and crossing-lane requests, and removes the same three rendering components from newly created hidden secondary lanes (around lines 407每437, 551每580 and 1400每1406).

## Current implementation relationship

`MovableSectionConversion` preserves selected-road conditions on the outer occurrence and applies measured mechanism rules in private subsections. Its moving-node branches use full hidden layers. `MovableSectionObjects` removes or replaces stationary object producers separately because object selection does not consult the same layer mask; it retains lane semantics and native signal producers.

Do not introduce a blanket private copy of every lane with its render references removed. Such copies are unnecessary for the inspected native creation path and could alter traffic or lane-selection identity. The selected-road sampler intentionally uses the original visible composition before moving-span hiding so the geometry and markings remain available for conversion.

## Limits and remaining checks

This evidence covers newly created node and secondary lanes. The inspected existing-lane reuse paths update/revive entries separately; this report does not prove that an upgrade or reconnection changes all existing visual components correctly. Creation, upgrade, reconnect and cold-load acceptance must still inspect the actual generated lane entities and their component sets. No game/engine patch was added, and no native traffic lane was deleted.

No final movable generator currently assembles the complete output. This path removes a suspected need for extra lane-prefab cloning but does not complete marking conversion, appearance, animation or shipping acceptance.

## Source identities

- `Game.Prefabs/NetInitializeSystem.cs`: SHA-256 `021cfcd09bf86d48a5f8b294096e56f495a50c83fae3c064f341e60f5f6e490e`.
- `Game.Prefabs/NetCompositionHelpers.cs`: SHA-256 `0d9eced94c6adbb0e6df14f0a1715a5b78ac97f43edcf393aaa2e7bc9951edd3`.
- `Game.Net/LaneSystem.cs`: SHA-256 `08d015c86765914283ce3580cd3c3a11ab03a8aa2cbfbb2d03c8a3be82985b87`.
- `Game.Net/SecondaryLaneSystem.cs`: SHA-256 `4523eeef49aeddf9506a8210ab87471ab128d72ecf96e6464eb5f7ebcf371d0d`.

This audit changed documentation only. No runtime source, installed payload or generated bridge assets changed.
