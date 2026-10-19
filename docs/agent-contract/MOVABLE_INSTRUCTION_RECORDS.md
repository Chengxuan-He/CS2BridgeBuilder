# Retained movable instruction records

These records preserve prior instruction text. Current movable appearance and animation rules
are defined by [MOVABLE_BRIDGES_CONTRACT.md sections 16–23](MOVABLE_BRIDGES_CONTRACT.md#16-accepted-generation-pipeline-and-source-map).
Conflicting historical recipes are not implementation alternatives. The current implementation policy in BRIDGE_GENERATION_CONTRACT.md takes precedence over all process
instructions retained here. Hardcoding from real prototype evidence remains required; removed branch,
GPU-first, per-edit deployment and audit-before-coding gates must not be reintroduced from this archive.
Unrelated current outcome requirements remain binding.


## Historical instructions from AGENTS.md

### Approved GPU composition from existing textures (2026-10-17)

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.

### Native separated movable paint revision (2026-10-18)

The user now requests separate native markings and prototype-based asphalt with preserved UV scale.
This supersedes the prohibition on independent marking objects only for native decal child renderers
attached to the mechanism's recorded motion connections. Keep each disjoint projector in its own native object-space clipping bounds. The native shader
clips against one box per object: grouping disjoint volumes is prohibited unless a compatible native
per-instance clipping representation is verified. Supply its normal and tangent attributes and bind
each projector to its motion connection. Keep geometry as closed projection volumes; never render those volumes
as road surface triangles. Only projection volumes may be partitioned at physical motion seams;
markings and texture repetition must never cut receiving road geometry.

Remove full-road transparent paint overlays. Reuse the native decal material and measured native
paint patches. Asphalt uses a repeatable unit extracted from the prototype atlas, including its
albedo, normal, mask and control channels. Preserve the measured per-metre UV gradients, direction
and phase on both leaves. BC block alignment may round the extracted pixel dimensions up; do not
stretch a low-resolution whole-span atlas over the road. Other appearance domains retain their
existing path. Keep native asset persistence and owner-scoped rollback/cleanup for the complete graph.
Preview, placement, opening/closing, LOD transitions and cold loading require human game verification;
a successful build does not establish visual acceptance.

Revision baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.

### Preserve original road skinning (2026-10-18)

The user revoked moving the road onto marking child transforms. Keep Draw01 road geometry in its original procedural skeleton and skinning path, with all three LODs, UVs and materials retained. Markings must adapt to the original road bones; never move road receivers into history-driven child objects to compensate for decal lag. Native independent persistence remains mandatory, without runtime synchronization patches. Structure/underside surfaces use the original native descriptors, texture references and VT resources without receiver-only material copies or descriptor rewrites. Single-sided skinned paint replaces the old projectors; disabling structural decal reception is no longer required. The earlier rigid road child implementation is withdrawn. A native decal shader without procedural skinning must not be treated as supporting bone-bound paint merely by assigning indices.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.

### Authorized native skinned paint exception (2026-10-18)

The user explicitly permits native skinned paint surfaces and minimum anti-coplanar clearance. Draw01 keeps its original road skeleton and receiver topology. Marking polygons alone may be partitioned at motion seams and bound with the road bone indices/bind poses. Use a native procedural-skinning material, single-sided paint, and 0.001 m clearance; no custom shader or runtime synchronization patch. Paint is an additional mesh on the same native mechanism object, with matching StackProperties, authored bounds and object transform, not a history-driven child projector. Preserve source marking layout/color; do not cut the road. A compact paint mesh remains present across road LOD transitions. This supersedes earlier prohibitions on overlapping marking surfaces and the native child-projector requirement for this implementation. Preview, motion, far views and cold loading still require verification.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.



## Historical instructions from src/BridgeBuilder/AGENTS.md

### Native marking conversion approval

Follow the movable generation contract sections 7, 11 and 15: integrate paint into mechanism
receiver appearance using native object materials. Do not create independent marking objects.
The GPU composition exception below permits owner-local composite textures and native descriptors. Curve segmentation, endpoint-fade and existing-atlas pattern
differences remain accepted; the former object-decal representation is superseded. Preserve native
motion, structural rules, selective persistence and human acceptance requirements.
Revision baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/movable-generation-contract-20261017`.

### Approved GPU composition from existing textures (2026-10-17)

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.

### Native separated movable paint revision (2026-10-18)

The user now requests separate native markings and prototype-based asphalt with preserved UV scale.
This supersedes the prohibition on independent marking objects only for native decal child renderers
attached to the mechanism's recorded motion connections. Keep each disjoint projector in its own native object-space clipping bounds. The native shader
clips against one box per object: grouping disjoint volumes is prohibited unless a compatible native
per-instance clipping representation is verified. Supply its normal and tangent attributes and bind
each projector to its motion connection. Keep geometry as closed projection volumes; never render those volumes
as road surface triangles. Only projection volumes may be partitioned at physical motion seams;
markings and texture repetition must never cut receiving road geometry.

Remove full-road transparent paint overlays. Reuse the native decal material and measured native
paint patches. Asphalt uses a repeatable unit extracted from the prototype atlas, including its
albedo, normal, mask and control channels. Preserve the measured per-metre UV gradients, direction
and phase on both leaves. BC block alignment may round the extracted pixel dimensions up; do not
stretch a low-resolution whole-span atlas over the road. Other appearance domains retain their
existing path. Keep native asset persistence and owner-scoped rollback/cleanup for the complete graph.
Preview, placement, opening/closing, LOD transitions and cold loading require human game verification;
a successful build does not establish visual acceptance.

Revision baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.

### Preserve original road skinning (2026-10-18)

The user revoked moving the road onto marking child transforms. Keep Draw01 road geometry in its original procedural skeleton and skinning path, with all three LODs, UVs and materials retained. Markings must adapt to the original road bones; never move road receivers into history-driven child objects to compensate for decal lag. Native independent persistence remains mandatory, without runtime synchronization patches. Structure/underside surfaces use the original native descriptors, texture references and VT resources without receiver-only material copies or descriptor rewrites. Single-sided skinned paint replaces the old projectors; disabling structural decal reception is no longer required. The earlier rigid road child implementation is withdrawn. A native decal shader without procedural skinning must not be treated as supporting bone-bound paint merely by assigning indices.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.

### Authorized native skinned paint exception (2026-10-18)

The user explicitly permits native skinned paint surfaces and minimum anti-coplanar clearance. Draw01 keeps its original road skeleton and receiver topology. Marking polygons alone may be partitioned at motion seams and bound with the road bone indices/bind poses. Use a native procedural-skinning material, single-sided paint, and 0.001 m clearance; no custom shader or runtime synchronization patch. Paint is an additional mesh on the same native mechanism object, with matching StackProperties, authored bounds and object transform, not a history-driven child projector. Preserve source marking layout/color; do not cut the road. A compact paint mesh remains present across road LOD transitions. This supersedes earlier prohibitions on overlapping marking surfaces and the native child-projector requirement for this implementation. Preview, motion, far views and cold loading still require verification.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.



## Runtime invariants retained from src/BridgeBuilder/AGENTS.md


Bridge generation contract section 2 prohibits all Boolean operations with prototype geometry,
including hardcoded copies of prototype faces. Apply immutable measured construction coordinates
and rules directly; offline prototype inspection does not authorize runtime mesh intersection.


- Meshes must reference the prototype bridge's corresponding LOD meshes. Excessive subdivision
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
  2026-10-08 migration-first revision defines this sequence and mandatory deletion fallback.

Follow the native-pricing revision in bridge generation contract section 16, including its restrictions
on legacy conversion and fee restoration. The complete revision remains in the contract.

- Self-check migration/repair and new generation share the same file layout and naming rules
  (contract section 19); preserve bridge/deck CID and UUID. Native conversion alone cannot skip
  layout migration. Localized notices distinguish migration from damage repair and report both
  when both occurred. Relocation is backed up, collision-safe and idempotent.

- Generated bridges must not depend on Bridge Builder; it is only the bridge asset manager.
  Persisted assets must remain loadable and usable after a cold start with the mod disabled or
  uninstalled, without its assembly, callbacks, patches or cached objects. Follow contract section 19;
  the deferred rail-seam limitation is unfinished compatibility work, not proof of independence.

- Apply the root independent-persistence revision: legacy conversion is additive; root and deck
  CID, UUID, version and geometry stay unchanged. Persist native unlocking, unchanged native fees,
  native locales and thumbnail data. No BridgeBuilder component or UI host may be required by
  converted/new saved prefabs. Pricing-only copies and native fee rewrites are prohibited.
  Deployment cleanup follows the affected-style rule in contract section 13.

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
- Runtime spatial tests may compare `x`, `y` or `z` only with zero, using equality or inequalities.
  For stretch versus translation, whether the authored part reaches or crosses `x = 0` is the sole,
  non-overridable decision outside the user-approved central railing post exception in contract
  section 8 (baseline `dev/80eb1415e1a3b59263eb45693f7e2c5d80732447`, 2026-10-16).
  An independently authored vertical railing post centred on the bridge centreline retains its
  original width and centred position at every LOD. Use immutable measured membership, not runtime
  shape/position heuristics. This does not exempt transverse rails or any other crossing structure.
- Stretch a centre-crossing part against its own authored span. Translate a non-crossing side part
  rigidly. Never use one family's boundary, profile or measured constant to classify another family.
- Treat the base only as the centre-crossing structure immediately adjacent to and below the deck.
  Transform its x coordinates with `x -> x + sign(x) * delta`; do not scale its width and do not
  misclassify bridge-pier columns, footings or foundations as the base.
- Translate bridge-pier columns. Do not stretch them.
- Apply every part classification and transform to the highest-detail mesh and every LOD. Do not allow
  reduced LOD topology to reclassify a part.
- Do not over-defend internal mod logic. Internal invariant failures must surface as explicit ERROR,
  with exceptions where appropriate, rather than being hidden by special cases, silent returns or
  fallback values. Defend external boundaries, including generated Prefabs that can be damaged and
  failures originating in game or third-party APIs. Stop failed operations before publishing invalid
  geometry. This replaces the previous blanket prohibition on explicit `throw` statements.
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

