# Lift05 mechanism render configuration

The generated render-data class configures fresh private render graphs for the mechanism,
column and footing. It preserves each native level's StackProperties, BaseProperties,
LOD references and content prerequisite. The caller supplies generated geometry and applies
the separately recorded procedural skeleton to each main-render level. Do not apply this
configuration to shared native renders or to a render already carrying copied components.

| Part | Native render records in LOD order |
| --- | --- |
| Mechanism | 81035, 81036, 81037 |
| Column | 81038, 81039, 81040 |
| Footing | 81041, 81042, 81043 |

The names Column and Footing identify measured object mesh slots; they are not runtime name
matching or permission to infer geometry. Footing is distinct from the road-adjacent deck base.

Source audit: game 1.6.2f1, SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
The extractor follows actual LOD references and checks the complete component list. No runtime
archetype lookup, width inference or new material is introduced.

This class has no generator caller yet. It changes no existing bridge output. Compilation
does not establish geometry, native animation, shipping or cold-start acceptance.

## All-LOD render assembly entry point (2026-10-20)

`Lift05RenderAssembly.CreateMechanism` now connects the prepared road-level inputs
to the shared native render builder. It checks the three reconstructed source
names and their transient ownership, requires native mesh counts 2/2/1, routes
mesh 0 through `Lift05StructurePlan`, and routes detailed mesh 1 through the
existing measured glass plan. Source skeleton, native Surface ownership and
prepared skinned road/paint data remain with the shared render pipeline.

Road preparation and the top-level style router are still pending. Native UV0
inspection confirms that Lift01's appearance constants cannot be copied:
Lift05 near faces 557/558 use U=0.3662109375..0.480712890625 and
V=0.402099609375..0.5546875 over the fixed 3-by-4-metre central road unit.
Moving faces 8/9 use U=0.01251220703125..0.12432861328125 and
V=0.67626953125..0.76806640625 over a shortened 3-by-2.4609375-metre
end unit. The full repeating moving unit and paint-free sampling regions must
still be identified before appearance integration. Evidence is retained in
`lift05_road_uv.py` and `lift05-main-0-uv.npy`. Native main albedo was decoded
from `LiftBridge05_BaseColor_b53d46fd176b70868429d4c498ab146c.VTTexture`
(CID `08d960c1947ac3115993f4fafa67f43b`) for visual inspection.

This increment adds no UI text. Compilation and static route/localization checks
do not establish native generation, motion or cold-load acceptance.

## Native clean-road appearance and preparation (2026-10-20)

`Lift05RoadAppearance` supplies separate fixed/moving and longitudinal/lateral
sampling regions. Full native floor islands contain baked rail tracks, so repeating
the full island is incorrect for a selected road. Inspected clean strips are:

| Domain | U interval | V interval | Repeat width | Repeat length |
| --- | --- | --- | --- | --- |
| Fixed asphalt | 0.41015625–0.4375 | 0.402099609375–0.5546875 | 0.7164179104 m | 4 m |
| Moving floor | 0.052734375–0.083984375 | 0.61865234375–0.76806640625 | 0.8384279476 m | 4 m |

The U gradient derives from each native three-metre half-road unit, divided by
the clean crop width rather than by the generated bridge width. Native atlas
sampling is about 156.33/156.25 pixels per metre for fixed U/V and 152.67/153 for
moving U/V. The moving full unit is recorded in faces 1366/1367; its end unit is
shortened and must not establish the repeat length. Both clean crops were inspected
without the baked tracks (`lift05-fixed-clean-crop.png`,
`lift05-moving-clean-crop.png`). This measures source texture density, not output
atlas allocation or game acceptance.

`Lift05RoadPreparation` now calls the common preparation pipeline with the original
three-bone skeleton and physical fixed/gap/moving ownership. Road motion remains
bone 2, not counterweight bone 1. Independent selected-sidewalk surface adaptation
is not yet complete: the current preparation uses the same appearance domain for
carriageway and sidewalk occurrences. Complete generator routing and cross-road
compatibility remain outstanding. No UI strings changed.
