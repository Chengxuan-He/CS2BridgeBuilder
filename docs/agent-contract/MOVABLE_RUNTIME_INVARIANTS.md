# Movable runtime invariants

Selected-road attachments use the [common measured-gap rule](MOVABLE_BRIDGES_CONTRACT.md#attachment-anchors-in-measured-gaps):
omit every kind of attachment at an unsupported gap anchor, preserve supported moving/fixed
mounts, and keep external native approach ownership separate. No type/name/CID gap exceptions.

Follow [Packaged textures belong to road running surfaces](MATERIAL_GENERATION_CONTRACT.md#packaged-textures-belong-to-road-running-surfaces) for this requirement.

These runtime requirements supplement the [movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md)
and [material generation contract](MATERIAL_GENERATION_CONTRACT.md). Read their detailed rules
for source independence, geometry, native rendering and per-owner persistence.

## Runtime invariants

Follow [3. Archetype evidence and immutable mechanism data](MOVABLE_BRIDGES_CONTRACT.md#3-archetype-evidence-and-immutable-mechanism-data) for this requirement.

Apply [source-face coverage](MOVABLE_BRIDGES_CONTRACT.md#source-face-coverage-and-prevention-of-missing-structure):
use measured retained/replaced/intentionally-omitted memberships, never coarse height or bounds
as permission to remove non-road structure. Whole-face restoration retires only its obsolete
fragments. Trace actual indices, original channels/bones, material/alpha and renderer LOD ownership
through final native output for every LOD and absent/one-sided/asymmetric sidewalk configuration.
Keep geometric, material and independent-LOD causes separate in evidence; do not repair one by
masking another. Preserve healthy saved owners and report unavailable real-game verification.

Native render groups choose LODs independently. Keep semantic part ownership stable
across each complete LOD chain and verify that mixed group LOD selections neither
omit nor duplicate parts. Prefer capacity-valid structure/road boundaries over slot-count
slices; retain actual native area/count/bounds metadata and the 16-batch graph limit.
Oversized semantic groups use stable, source-provenance subgroups across LODs. Supplemental
occurrences declare a shared semantic key for the same physical part at every LOD;
ordinal position alone is not proof of correspondence. Within one role, a Surface identity
may anchor a subgroup only when present in every LOD. Keep remaining LOD-specific material
slots together in a complete residual group. Grouping does not merge same-material geometry
at different locations: every original slot and triangle remains exactly once. Verify actual
native thresholds and arbitrary independent group LOD combinations. Legacy unmeasured
capacity fallbacks are implementation limitations, not exceptions to this requirement.

See [material batches](MOVABLE_BRIDGES_CONTRACT.md#material-selection-projection-and-render-batches).

Follow [Scope and authority](MATERIAL_GENERATION_CONTRACT.md#scope-and-authority) for this requirement.

Follow [Preserve original UV projection and mapping mode](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode) for this requirement.

The [general source-mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode) preserves non-road source modes for all bridges. Movable roads requiring processing use
prototype-supported appearances tiled at original resolution/physical density without stretching
in every LOD. Fixed bridges retain selected native road materials and markings.

For movable bridges, reuse original non-road materials by default. Only evidenced repeat
domains that would otherwise require new period-wrap seams may use the narrow offline
source-atlas derivative in material contract section 3; do not add those seams. Keep source
assets unchanged and give derived resources independent identities. Road composition/precomposition (offline material baking outside the game) has
two necessary correspondence conditions: the actual receiver appearance type is verified and
the measured target prototype part has a matching material. Use that part's appearance, not a
bridge-wide type override. Follow [physical part correspondence](MATERIAL_GENERATION_CONTRACT.md#physical-prototype-part-correspondence). Test asphalt, sidewalk surfacing and wheel-track/rut effects independently
per prototype and physical domain. Other road types reuse compatible input materials or use the corresponding native
animated-object road material under material contract section 4; this replacement must preserve complete network-layer blending and dynamic worldspace projection, together with the actual
appearance category, sampling density and tiling/stretch behavior. Supported road types instead require fixed-density tiling and all-LOD marking removal;
non-road source modes remain unchanged. This is a generation rule, not a reverse-engineering finding or proof that the
current implementation complies. Road processing must not mutate a shared structural Surface.

Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.

Follow [Native track material conversion](MOVABLE_BRIDGES_CONTRACT.md#native-track-material-conversion) for this requirement.

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

- Generated bridges must cold-load with Bridge Builder enabled. Startup registration of owner-local shader/template programs is permitted; per-frame transform tracking is prohibited. Follow contract section 19. The deferred rail seam remains unfinished compatibility work.

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
  default decision, subject to the explicit base, centred-post, Lift01/Lift02 complete-assembly and complete Draw03 truss rules
  in contract section 8 and the measured Golden mappings in section 5.
  An independently authored vertical railing post centred on the bridge centreline retains its
  original width and centred position at every LOD. Use immutable measured membership, not runtime
  shape/position heuristics. This does not exempt transverse rails or any other crossing structure.
- Outside those explicit exceptions, stretch a centre-crossing part against its own authored span.
  Translate a non-crossing side part rigidly. Never use one family's boundary, profile or measured
  constant to classify another family.
- Treat the base as the complete structure immediately adjacent to and below the deck;
  its individually recorded side blocks need not cross the centre.
  Transform its x coordinates with `x -> x + sign(x) * delta`, except for the explicit Lift01/Lift02 complete-assembly mapping and Draw03 complete
  moving/fixed truss stretch in contract section 8. Do not
  misclassify bridge-pier columns, footings or foundations as the base.
- Translate bridge-pier columns. Do not stretch them.
- Apply every part classification and transform to the highest-detail mesh and every LOD. Do not allow
  reduced LOD topology to reclassify a part.
- Follow [trusted internal execution](../../AGENTS.md#trusted-internal-execution). These invariants
  describe required construction and offline verification, not runtime assertions. Trust memory and
  computed results; retain validation only at external file/network IO boundaries. Preserve actual
  failure handling, native business rules, cancellation and disposal.
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

Follow [generated material program requirements](MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders) for program persistence, Player loading and mod-enabled registration; geometry, texture and source ownership rules remain unchanged.