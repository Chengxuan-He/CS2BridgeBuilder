# Movable runtime invariants

Follow [player-controlled generation](BRIDGE_GENERATION_CONTRACT.md#player-controlled-generation-without-structural-rejection-gates): do not reject a generation request
because of structural shape, width, signed scale, degeneracy, overlap, clearance or appearance
quality. Such checks belong to offline verification or non-blocking diagnostics. Material
no-match selects Original. Preserve actual file/schema/index, nonrepresentable arithmetic,
write and native API errors; never fabricate successful output. This applies to all bridge
families and LODs without changing historical dependency recovery.


These runtime requirements supplement the [movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md)
and [material generation contract](MATERIAL_GENERATION_CONTRACT.md). Read their detailed rules
for source independence, geometry, native rendering and per-owner persistence.

## Runtime invariants

Lift01 and Lift02 central-truss memberships follow the explicit complete-assembly rule in
bridge generation contract section 8, overriding default own-span stretching,
side-part translation and base mapping for those measured members. Internal
braces and narrow connectors share their complete assembly's `x -> k*x`; only the outermost
edge trusses retain their original mapping. Record assembly spans and connection
residuals offline, apply the rule at every LOD including far supplements, and
preserve bones/material modes with the corresponding affine normal/tangent frame.
Measure each style's complete source and target assembly spans; never use a small
connector's thickness or another style's span as its scale denominator.


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

Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose roads need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable road-processing scope.

Resolve the elevated road-level branch once at the common generation entry, before width
measurement, cloning or style construction. Every fixed and movable bridge uses that elevated
context for geometry, bridgeheads, decks, materials and every LOD. Do not enumerate, rank or
fall back to ground/other road-level branches; no downstream re-selection.
Keep all physical contributions and section-local branches within the selected road branch.
Material matching within it is noexcept: a corresponding match selects that route; no match
selects the original source-material route, without selecting a different road branch.
Branch mechanism decks and native network bridgeheads
by their own source/domain and render context; do not classify a mixed piece from a section name.
Resource corruption and native realization failures remain separate explicit errors. Follow
[the material routing rule](MATERIAL_GENERATION_CONTRACT.md#total-material-routing-by-part-and-actual-source).


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
animated-object road material under material contract section 4; this replacement may differ
in network-layer blending and dynamic worldspace projection, but must preserve the actual
appearance category, sampling density and tiling/stretch behavior. Supported road types instead require fixed-density tiling and all-LOD marking removal;
non-road source modes remain unchanged. This is a generation rule, not a reverse-engineering finding or proof that the
current implementation complies. Road processing must not mutate a shared structural Surface.

Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.

Prototype-supported road appearances must tile at original resolution and physical texel density,
never stretch, including fixed running caps, finite terminals and every LOD. Remove embedded road
markings from every road-material channel and mip; preserve intended selected-road paint through
the applicable native path (the separate skinned paint mesh for movable mechanisms). Necessary
bounded offline repeats may be authored from pure original road content with aligned PBR donor
coordinates and complete periodic mip chains; do not claim new periods as original evidence.
Domains sharing the same evidenced prototype appearance require coherent source/PBR, density, orientation and phase; intentionally different physical-part materials remain distinct. Wear uses the
same complete measured source and matching same-source background; translate the whole pattern's
measured visual center to actual native lane centers while preserving physical width, internal
relationships and longitudinal density. No vehicle lane means no wear. Non-road finite/stretch
modes retain their own source evidence. No full-span pixels, period cuts, arbitrary downsampling,
custom shaders or a relaxed package byte limit are permitted.

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
- Do not over-defend internal mod logic. Internal invariant failures must surface as explicit ERROR,
  with exceptions where appropriate, rather than being hidden by special cases, silent returns or
  fallback values. Defend external boundaries, including generated Prefabs that can be damaged and
  failures originating in game or third-party APIs. Do not report failed I/O, resource decoding or
  native registration as success. Structural or appearance quality is not an internal invariant
  failure and must not block generation or publication.
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
