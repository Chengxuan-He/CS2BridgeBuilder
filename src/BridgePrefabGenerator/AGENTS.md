# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgePrefabGenerator/**`. They add runtime-specific requirements to
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

## Completion

After any source-code edit, follow contract section 13: invoke the exact `Cities2.exe` kill command,
verify the game is stopped, remove all mod-created bridges and generated artifacts, and verify the
ownership-scoped cleanup. Compilation is not visual verification. For visual work, do not run unit
tests; inspect generated geometry and confirm both near and far views in game.
