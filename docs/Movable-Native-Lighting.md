# Native lighting ownership for moving road attachments

## Implemented representation

The shared conversion now separates the visible lamp, its animated effect and the network lighting
producer. All three remain native prefab data; no manager callback, lighting timer or shader is added.

| Part | Representation | Purpose |
| --- | --- | --- |
| Network lighting rule | Private copy of the source object with PlaceholderObject and no registered replacements | Preserve native candidate aggregation, StreetLightData, IsLit and AlwaysLit without spawning a stationary lamp |
| Moving lamp geometry | Private copy of the selected concrete lamp, with EffectSource and SpawnableObject removed | Preserve original mesh/LOD references and other fields while following the measured bridge bone |
| Moving light effect | Original effect reference in the mechanism root's EffectSource, transformed into the closed mechanism frame and assigned the same connection ID as its lamp | Let native EffectInitializeSystem and EffectTransformSystem bind and animate the effect |
| Light state | StreetLightObject on the road-owned mechanism root | Participate in the native road's direct-object lighting update |

An unchanged lamp without a directly declared EffectSource does not need a private visual copy.
Existing root effects are retained. The audited ten B&P mechanism roots have neither EffectSource
nor StreetLightObject; a root with pre-existing LightsOff-sensitive effects requires a separate
mapping and is rejected before that behavior is changed.

MovableSectionObjects replaces only streetlight rule object references with private lighting
producers. It preserves rule conditions, position, offsets, spacing, probability and ordering.
Signal-capable rules retain their previous native ownership path. Other moving visual rules remain
excluded from stationary network emission.

MovableRoadObjects.Bind now requires the owner and owned-prefab collection and invokes
MovableStreetLights.Bind after assigning measured connection IDs. It returns the visual lamp
placements and records all new private prefabs in that collection for the existing writer/lifecycle.
TowerFactory.AttachMovableRoadObjects now supplies that collection and adopts its partial results
in a finally block. Draw01 object assembly invokes it after creating the final root so root-owned
effects survive. The binder is still not called by a complete routed movable generator, and native
day/night, power-loss and cold-start behavior remain unverified.

## Native evidence

SecondaryObjectSystem establishes hasStreetLights from collected candidates before concrete
placeholder selection. PlaceholderObject initializes its native selection buffer; no SpawnableObject
points to the new private placeholder, so its buffer remains empty and selection emits no object.
The lighting producer therefore serves actual simulation state and is not a pricing-only copy.

StreetLightSystem updates direct road children with StreetLight. EffectControlData reads LightsOff
from an ordinary effect owner's own StreetLight component. EffectInitializeSystem resolves the
effect's parent field with RenderingUtils.FindBoneIndex, including inverse-bind conversion;
EffectTransformSystem subsequently reads native bone history. Positions passed to the source remain
closed-pose object-space values and are not inverse-bound twice.

The 81 scanned native StreetLightObject static prefabs declare 110 direct effects. All use parent
0 and animation index -1; none has ObjectSubObjects. All inspected effect color sources use Effect,
and all random position ranges are zero. Twelve effect entries retain native random rotation/color
components. Those original resources and their native randomization remain referenced; no new light
effect, material or texture resource is created. Random realization follows the generated owner's
native identity/effect index rather than copying a temporary entity's seed.

Other input schemas with nested children, independent effect parents/animations, mesh-derived effect
colors or owner-local random positions require their actual mapping. The implementation reports the
specific unsupported input feature instead of publishing a wrong transform or changed color source.

## Isolation, fees and persistence

Private visual copies must not keep SpawnableObject membership in original game placeholders:
doing so would make ordinary roads select an effect-free private lamp. The producer also removes
SpawnableObject because a private empty placeholder must not register as a replacement elsewhere.
Source prefabs and their membership lists are not changed.

Native pricing fields are not rewritten or recalculated. NetInitializeSystem.AddObjectCosts reads
the rule object's PlaceableObjectData and spacing; producer copies preserve those fields. None of
the 81 inspected native lamp prefabs declares PlaceableObject. This observation does not establish
pricing for arbitrary external lamps or the final complete bridge.

New producer and visual prefab identities are bridge-owned. Unmodified geometry/LOD/effect resources
stay references; external dependencies follow the existing original-byte snapshot policy. Foreign
bridge-owned source prefabs and effects are rejected before component reads. Persisting the new
owned prefabs must happen before reporting final creation success.

## Verification and outstanding acceptance

Both Release builds completed without warnings. Final deployment verified 18 backed-up originals
and 12 installed payloads at `artifacts/backups/movable-private-lamp-membership-20261016-064228`.
The first increment is at `artifacts/backups/movable-native-lighting-ownership-20261016-064152`.
No final routed generator invokes these additions, so no existing bridge output changed or was removed.

This implements the native data representation, not complete lighting acceptance. Verify initial
placement and replacement on an already-lit/unlit network, day/night changes, power loss/restoration,
tunnel behavior, native random variations, culling/LOD, full bridge motion and a fresh cold start.
Game.Objects.StreetLight serializes its state in saves, but that alone does not prove fresh prefab
initialization on every editing path. Lowered/OnGround attachment elevation compatibility remains
separate unfinished work. All ten final generator assemblies and full in-game acceptance remain open.
