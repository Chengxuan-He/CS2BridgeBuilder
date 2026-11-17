# Native LOD formula source record

This record identifies the inspected native implementation behind the general [LOD calculation specification](../../specs/lod-spec.md#native-calculation-sources-and-notation). It records source identity, not visual acceptance or permission for custom calibration.

## Native evidence identity

The local Game.dll inspected by integration has SHA-256
`F076DF4B70AA0F21A80345E03A052CA2273BC6754F41DAB7E63D837837A43745`.
Assembly, file and product versions are all `0.0.0.0`; no game release version is inferred.
The reviewed decompilations are in repository-relative development evidence
`work/lod-native-source-20261117/`. Line references below identify these exact extracts.

| Extract | SHA-256 | Native location and role |
| --- | --- | --- |
| RenderingUtils.cs | `CD9D69C3CABCB757792822F6A0D2FE274E143DC81311BED0B2BAE80A4FC74ABD` | `Game.Rendering.RenderingUtils.CalculateLodLimit`, lines 75–84: forward bias and quantization; `GetRenderingSize` overloads, lines 137–161: object/network rendering size. |
| NetCompositionMeshRefSystem.cs | `76F04913CAFC3987B28970B7879D884780F9FDEED41E619806EE0C168E740284` | `Game.Prefabs.NetCompositionMeshRefSystem.CompositionMeshRefJob.CalculatePieceData`, lines 161–205: summed index density and arithmetic mean of piece biases. |
| MeshSystem.cs | `5FC3F1B29EB6EDBC6B41ED2301B40A9DFFB7E7B845783611D4111F6D90C329EA` | `Game.Prefabs.MeshSystem.OnUpdate`, lines 459–460 and 504–510: authored ordinary/shadow bias and surface-area adjustments. |
| RequiredBatchesSystem.cs | `F23E0F51FE1AEA0CE95896D92D69F573CFF0C4594843145642335DFE57530A9A` | `Game.Rendering.RequiredBatchesSystem.RequiredBatchesJob.InitializeBatchGroup`, lines 696–712: object/network branches and finer minimum from CalculateLodLimit plus one, with monotonic clamping. |

These native locations support the forward calculation and parameter meanings. They do not
contain the project's target-matching inverse. Recheck the binary and extracts when the native
implementation changes; a matching method name alone does not establish matching semantics.

## Mesh bias preparation

`Game.Prefabs.MeshSystem.OnUpdate`: lines 453–510 of the identified MeshSystem.cs, including the LOD-buffer, no-buffer small-mesh and positive-surface-area branches.

## Composition density and bias

`Game.Prefabs.NetCompositionMeshRefSystem.CompositionMeshRefJob.CalculatePieceData`: lines 161–205. The supplied piece buffer determines summed density and arithmetic bias means.

## Rendering size and quantization

`Game.Rendering.RenderingUtils`: CalculateLodLimit lines 75–84; GetRenderingSize and GetShadowRenderingSize overloads lines 137–208, including stack-direction branches.

## Batch boundaries

`Game.Rendering.RequiredBatchesSystem.RequiredBatchesJob.InitializeBatchGroup`: lines 510–712. This covers initial Net/Object/Zone fields, stack flags, reverse LOD traversal, lower-level density/count selection, emission, +1 and monotonic clamps.
