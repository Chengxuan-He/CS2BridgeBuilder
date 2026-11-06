# Lift01 runtime material path audit

## Scope and evidence

This is a read-only audit of the current Lift01 call graph and installed native
source descriptors. It does not infer that every fallback is a runtime texture
bake. No Unity process, baker, main build or game action was run.

`artifacts/prepared-materials/Lift01/fallback-source-coverage.json` records a direct
scan of the installed Game and BridgesAndPorts Blob archives:

- All 26 distinct native template 16/31/34 descriptor hashes have corresponding
  Lift01 prepared lane descriptors.
- The sole native template 30 cable descriptor resolves to exactly the packaged
  cable color and metallic/smoothness/snow values using the recorded native template.
- The fixed road source CID `1d7dad9f64c9fe55a809f1080c39fb92` has exactly the three
  texture bindings expected by the packaged Lift01 road descriptor.

This source evidence does not prove that a third-party mod cannot mutate a live
asset or its template defaults. Live mismatches retain their explicit conversion
or error paths.

## Actual calls and classifications

| Path | Lift01 behavior | Runtime creation |
| --- | --- | --- |
| `MovableBridgeGeneratorBase.Apply` to `MovableRoadMaterial` to `MovableRoadSurface.Create` | The base generator always resolves the fixed native Road CID, not the selected third-party road. Its measured bindings match `BridgePreparedMaterials.Road("Lift01")`. | No material or texture creation for the measured native source. A binding mismatch creates only a Default descriptor, referring to original textures and prepared control. |
| `MovableRoadAssets` to `MovableLaneSurface.Create` | Selected rail, sleeper and catenary lane visuals use this path. Compatible object templates return the original Surface. Native curved templates 16/31/34 use prepared descriptors when resolved properties match. | A missing or changed recipe creates only a descriptor with remapped shader, retained VT header/backing references and resolved defaults; it does not bake textures. |
| `MovableLaneSurface` to `MovableCableSurface.Create` | Template 30 is converted because curve deformation is already in the geometry. The measured native recipe matches prepared Cable exactly. | Different source color/scalars create only a descriptor using prepared control, with no texture creation. Textured or VT cables fail explicitly. |
| `Lift01RoadPreparation` to `MovableRoadPreparation` to `MovableSurfaceAtlas.Compose` | Lift01 has `PrototypeRoad=false`. Its moving selected-road faces use original projections and `RoadSource=null`. Fixed prototype patch repeats use the packaged Lift01 repeat recipe. | Compatible object faces bypass composition; matching fixed repeats bypass texture decoding/baking. Other network appearances can require real shader-layer composition. |
| `MovableRoadMaterial.SaveRepeat` / `SaveComposite` | These are sinks after actual composition, not the prepared-match decision. | Each invocation creates a Surface; `SaveTexture` creates a new Texture for each passed channel. Finite composites also write VT/backing data. |

Changing network templates to object templates is required for native skinning.
Returning an unchanged curved network material would apply curve deformation
again. Those descriptor conversions are not full-original-material reuse and
cannot simply be deleted under the no-new-material rule.

## Remaining duplicate-work paths

### Selected-road periodic units are not shared for Lift01

`MovableSurfaceAtlas.Compose` marks genuinely periodic selected-road charts at
the `period > 0` branch. The later `periodicUnits` dictionary initializes its key
to the chart object. Its value-based recipe key is only built when
`c.NativeRoad && c.RoadSource != null` and the prototype-asphalt replacement applies.
Lift01's `RoadSource` is null, so its selected-road periodic charts retain object
identity keys. Independent LOD charts with identical sampling recipes can therefore
reach separate GPU pages and separate `SaveTexture`/`SaveRepeat` calls.

This is reachable for original as well as third-party roads; it is not evidence
that every page is identical. Different worldspace phases, clamp behavior, source
overrides, receiver gradients or dimensions can legitimately produce different
pixels. Native-road finite charts similarly bypass `units` sharing in `AddFinite`.

Minimum shared fix: extend per-generation exact-recipe sharing to the original
selected-road branch. Include all sampled source identities, gradients, worldspace
frame/phase, color/scalar overrides, sampler behavior, dimensions, control and any
paint contributions. Do not use approximate phase matching or reuse another generated
bridge's assets. Finite charts should only alias when their complete recipe matches.

### Constant control channels are still produced in dynamic pages

`Bind` assigns `Texture2D.blackTexture` when the source has no authored control.
The channel queue nevertheless constructs `MovableAtlasChannel` for all four
targets, then calls `owner.SaveTexture` for each. There is no channel-specific
prepared lookup in that queue. A page whose entire control output is the same
constant can therefore re-encode and serialize that constant even while its color,
normal and mask genuinely vary.

Minimum shared fix: establish constant-control eligibility from every contributor
to the page, including paint, then bind a prepared constant texture directly and
skip that channel's render/readback/encoding/save. Preserve exact RGBA and native
sampling semantics. The opaque-black repeat control is not interchangeable with
the transparent-zero cable/paint control solely because both have black RGB.
Resolution carries no spatial information for a proven constant, but alpha does.

### Descriptor-only duplicate cases

`MovableRoadAssets` shares conversions across its whole LOD graph using a
`Dictionary<SurfaceAsset, SurfaceAsset>`. The same source object converts once.
Distinct source objects/CIDs with identical unknown override recipes can still
produce equivalent new descriptors. This is a possible same-owner duplication,
not a texture-baking defect or a demonstrated missing known-native recipe.
An owner-scoped exact resolved-descriptor key can address it if measurements show
it occurs. External non-built-in dependencies still require independent per-owner
copies; those copies are required persistence, not prohibited rebaking.

## Prepared coverage boundary

The measured native cable, fixed Road descriptor and 26 lane descriptors already
have prepared coverage. The fixed Lift01 repeat and measured structure inventory
also have authored products. A missing catalogue file can still send a known
repeat or structure to its runtime producer; this is a packaging/inventory
completeness issue, not proof of a missing source recipe. Validate the final
installed inventory before attributing such fallback to variable road input.

No user-visible strings or runtime code were changed; localization is unaffected.
