# PedestrianDraw02 independent-layer consumer audit

`PedestrianDraw02RoadPreparation.Prepare` supplies the measured physical partition,
four appearance patches and the selected-road layout to common road preparation.
All source LODs reach `MovableSurfaceAtlas.Compose`. The prototype-road substitution
flag is false for this style.

Fixed regions 0 and 1 use mirrored projections of the same prepared prototype
asphalt unit. Its native source is Surface `a1b3ca16b946cc34fc031e376ba03d71`, template
0, with three 4096-square VT channels. Its measured physical period is
1.05157470703125 metres transversely and 4 metres longitudinally. Fixed source faces
6708 and 6709 have exact corresponding XYZ/UV triangles at both mechanism LODs.
These are source-defined reusable units; their textures do not grow with road span.

Moving regions carry the original selected-road projection and material. The
prototype moving patch is retained as measured metadata but does not substitute
prototype asphalt on those regions. Its source faces 8556 and 8557 are independently
verified in both LODs. The common consumer must preserve `RoadSource == null` for
PedestrianDraw02 moving faces; enabling the prototype replacement would change its
appearance rule.

This excludes a prototype/selected-road blend for this style, but does not exclude
multiple layers inside the selected road itself. The current common atlas path
combines selected-road base/control channels with worldspace channels, searches for
one joint period and can fall through to finite receiver rasterization. That common
behavior requires repair under the independent-layer and no-full-road-expansion
rules. The single-layer prototype shader does not prove an object shader can retain
the selected road's independent worldspace layer, control coordinates and blend.
Native object rendering, skinning and persisted dependency behavior require shared
implementation evidence before declaring that route complete.

`BuildLayerFixture.py <repository> <output>` exports exact source triangles, matching
LOD face IDs, native Surface bytes and hashes, channel identities and BBN provenance.
The fixture also measures local and cumulative binary32 projection error over
explicit numeric repetition probes. Those probes do not establish a supported road
length range or a replacement error threshold. In particular, the fixed longitudinal
frequency 0.25 is exact; its zero drift cannot justify relaxing unrelated selected
road layer tolerances.

Current evidence is under
`artifacts/layer-decoupling/PedestrianDraw02/outputs/source-fixture`. No runtime
consumer, geometry, material bytes, bones or localization keys were changed for
this audit. The common implementation remains an explicit dependency. Human
near/far, movement and cold-load acceptance remain separate from fixture checks.
