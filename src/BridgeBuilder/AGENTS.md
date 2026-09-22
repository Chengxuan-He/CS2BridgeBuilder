# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md); this file does not replace it.

## Runtime invariants

- Use immutable, hardcoded archetype and component metadata produced by the metaprogramming step.
  Runtime code must not rediscover parts from bounds, topology, nearest vertices, connected components,
  height bands, ratios, names or other geometric resemblance.
- Runtime spatial tests may compare `x`, `y` or `z` only with zero, using equality or inequalities.
  For stretch versus translation, whether the authored part reaches or crosses `x = 0` is the sole,
  non-overridable decision.
- Stretch a centre-crossing part against its own authored span. Translate a non-crossing side part
  rigidly. Never use one family's boundary, profile or measured constant to classify another family.
- Treat the base only as the centre-crossing structure immediately adjacent to and below the deck.
  Transform its x coordinates with `x -> x + sign(x) * delta`; do not scale its width and do not
  misclassify bridge-pier columns, footings or foundations as the base.
- Translate bridge-pier columns. Do not stretch them.
- Apply every part classification and transform to the highest-detail mesh and every LOD. Do not allow
  reduced LOD topology to reclassify a part.
- Do not write an explicit `throw` statement under this directory. Return an explicit failure result,
  report it and stop the affected prefab before persistent geometry is allocated or published. Catch
  exceptions originating in game or third-party APIs at the boundary.
- Preserve the archetype's complete prefab behavior. A generated bridge changes only requested
  geometry and the selected road structure; derived tower, cable, piece and LOD prefabs remain unique
  to that generated bridge.
- Preserve the archetype-specific constant `bridge width - deck width`. The width definition and the
  constant must come from a same-basis comparison of a real archetype and a real newly generated
  bridge, not runtime geometry inference or source arithmetic alone. Width is the complete
  left-to-right span `maxX - minX`, and the audited width increment is the increment of that complete
  span; it is not a per-side x-coordinate correction. If the absolute full-span width increment is
  greater than 1 m, do not change that bridge; report and record the bridge for review. Absence of a
  generated sample does not satisfy that skip condition:
  the invariant pass stays incomplete until a human creates the missing bridge from a selected road
  and its real report and geometry are retained. The Agent must not open or control the game or issue
  an API/request-file bridge-construction request for sample creation.
- Never apply an audit or geometry correction in runtime bridge-generation code. Metaprogramming must
  finish the correction and fold it into the original immutable archetype parameters or generated
  source metadata. Runtime may consume those final parameters as ordinary generation inputs, but it
  must not contain an audit-correction table, add a post-measurement delta, double a per-side result,
  or otherwise know that a correction step existed.
- Preserve bridge-specific lighting from the same archetype. Carry tower/pylon `ObjectSubObjects` and
  their referenced light/effect prefabs without filtering by names or rebuilding effect fields. Keep
  rotations, parent-mesh indices, groups, probabilities and activation data unchanged. A mounted
  object's non-zero x position follows its parent mesh's rigid half-width displacement; an object on
  `x = 0` remains centred. Do not replace the selected road's ordinary street lighting with the
  archetype road's street lighting.

## Completion

Construction pricing must not allocate, clone, save or publish any pricing prefab. Price is a scalar
property attached to the existing bridge prefab, projected onto native UI and composition cost data.
Read selected road prices from initialized native data; preserve each archetype's signed offset and
floor the total at the selected networks' elevated base price. See project contract section 16.

After any source-code edit, follow contract section 13: invoke the exact `Cities2.exe` kill command,
verify the game is stopped, remove all mod-created bridges and generated artifacts, and verify the
ownership-scoped cleanup. After completing any code change, immediately build and deploy to the game
without waiting for another request; back up the existing installation and verify installed files
against the build output as required by section 13. Compilation is not visual verification. For visual work, do not run unit
tests; inspect generated geometry and confirm both near and far views in game.
