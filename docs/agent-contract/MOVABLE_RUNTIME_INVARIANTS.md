# Movable runtime invariants

These runtime requirements supplement the [movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md)
and [material generation contract](MATERIAL_GENERATION_CONTRACT.md). Read their detailed rules
for source independence, geometry, native rendering and per-owner persistence.

## Runtime invariants

Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.

Material contract section 2 permits stretching only for verified prototype non-tiling UV mappings.
Retain authored UVs, interpolating permitted construction fragments, native textures/resolution,
channels, mips and compatible material identity. Record source/part/LOD/axis evidence; tiling axes
retain original physical density. Failed mappings or missing repeat evidence do not qualify.
This changes no geometry/bones, selected-road/track appearance, marking layout or byte limits.

- Meshes must use the prototype bridge's corresponding LOD meshes as their shape and complexity
  reference; derived geometry must not retain an unchanged prototype geometry reference. Excessive subdivision
  or triangle cutting is prohibited. Follow bridge generation contract section 4, including for
  converted road surfaces, UV tiling and marking clipping; existing LOD exceptions do not waive it.

- Bridge generation contract section 25 applies to every generator: derive left/right sidewalk presence,
  position and width from the selected elevated composition and upgrades. Adapt sidewalk-dependent
  inner railings and bridge-authored sidewalk portions independently; never fabricate a missing
  side or mirror unequal widths. Preserve unrelated structure and apply the layout to every LOD,
  preview and persisted geometry. The root AGENTS.md is the routing authority for this rule.
  Sidewalk portions embedded in fixed bases, end platforms or other composite components are also
  subject to section 25. Only `Sidewalk` sections count as sidewalks; curbs do not. Adapt each side
  independently without removing unrelated structural parts.

- Self-check prioritizes normal game operation and attempts asset version migration first.
  Complete missing fields/dependencies without changing UUID/CID; leave healthy current assets
  unchanged. Only failed or impossible migration enters recursive backup/move. Failed backup
  creation/copy/move must fall back to permanent deletion of
  remaining UUID-owned source files/directories, not retention or an early return. Continue other
  selected paths, verify absence, and report actual deletion/access failures without claiming success.
  Keep self-check free of live prefab unregistration and require restart. Contract section 19's
  migration-first policy defines this sequence and mandatory deletion fallback.

Follow the native-pricing policy in bridge generation contract section 16, including its restrictions
on legacy conversion and fee restoration. The complete policy remains in the contract.

- Self-check migration/repair and new generation share the same file layout and naming rules
  (contract section 19); preserve bridge/deck CID and UUID. Native conversion alone cannot skip
  layout migration. Localized notices distinguish migration from damage repair and report both
  when both occurred. Relocation is backed up, collision-safe and idempotent.

- Generated bridges must not depend on Bridge Builder; it is only the bridge asset manager.
  Persisted assets must remain loadable and usable after a cold start with the mod disabled or
  uninstalled, without its assembly, callbacks, patches or cached objects. Follow contract section 19;
  the deferred rail-seam limitation is unfinished compatibility work, not proof of independence.

- Apply the root independent-persistence policy: legacy conversion is additive; root and deck
  CID, UUID, version and geometry stay unchanged. Persist native unlocking, unchanged native fees,
  native locales and thumbnail data. No BridgeBuilder component or UI host may be required by
  converted/new saved prefabs. Pricing-only copies and native fee rewrites are prohibited.
  Deployment preserves healthy bridges; only a demonstrated incompatibility permits recovery of
  affected style/UUID-owned assets under contract section 13.

- Apply the root contract's legacy naming migration exception during self-check: create per-bridge
  copies with "-b{uuid}" appended to the first space-delimited name token and new CIDs, update file references, and back up unreferenced old components.
  Keep live prefabs unchanged and request restart.
- Every modification requires the project-wide compatibility review mandated by the root instructions.
  Trace all affected generation, naming, persistence, loading, migration, cleanup and UI/error paths,
  including alternate bridge branches and legacy assets; fix incompatible callers and consumers
  together. Build or preview success alone is insufficient; report pending in-game verification.
- Use immutable, hardcoded archetype and component metadata produced by the metaprogramming step.
  Runtime code must not rediscover parts from bounds, topology, nearest vertices, connected components,
  height bands, ratios, names or other geometric resemblance.
- Runtime spatial tests for authored-part identification may compare `x`, `y` or `z` only with
  zero. Applying recorded construction coordinates and affine transforms is permitted; it is not
  runtime part identification.
  For stretch versus translation, whether the authored part reaches or crosses `x = 0` is the
  default decision, subject to the explicit base, centred-post and complete Draw03 truss rules
  in contract section 8 and the measured Golden mappings in section 5.
  An independently authored vertical railing post centred on the bridge centreline retains its
  original width and centred position at every LOD. Use immutable measured membership, not runtime
  shape/position heuristics. This does not exempt transverse rails or any other crossing structure.
- Outside those explicit exceptions, stretch a centre-crossing part against its own authored span.
  Translate a non-crossing side part rigidly. Never use one family's boundary, profile or measured
  constant to classify another family.
- Treat the base as the complete structure immediately adjacent to and below the deck;
  its individually recorded side blocks need not cross the centre.
  Transform its x coordinates with `x -> x + sign(x) * delta`, except for the explicit Draw03 complete
  moving/fixed truss stretch in contract section 8. Do not
  misclassify bridge-pier columns, footings or foundations as the base.
- Translate bridge-pier columns. Do not stretch them.
- Apply every part classification and transform to the highest-detail mesh and every LOD. Do not allow
  reduced LOD topology to reclassify a part.
- Do not over-defend internal mod logic. Internal invariant failures must surface as explicit ERROR,
  with exceptions where appropriate, rather than being hidden by special cases, silent returns or
  fallback values. Defend external boundaries, including generated Prefabs that can be damaged and
  failures originating in game or third-party APIs. Stop failed operations before publishing invalid
  geometry.
- Preserve the archetype's complete prefab behavior. A generated bridge changes requested
  geometry and the selected road structure, together with the native representation, UV-density,
  LOD and persistence conversions explicitly required by the general and specialist contracts.
  Derived tower, cable, piece and LOD prefabs remain unique to that generated bridge; compatible
  original materials and prepared constant resources retain their shared source identities.
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
  `x = 0` remains centred. The recorded Golden ornament-mounted lamps instead follow the measured
  ornament XY mapping under general contract sections 5 and 15, with rotation and effects unchanged.
  Do not replace the selected road's ordinary street lighting with the
  archetype road's street lighting.
