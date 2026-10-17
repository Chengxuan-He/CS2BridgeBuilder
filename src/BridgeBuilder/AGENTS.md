# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md) and
[`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
This file replaces neither. Numbered bridge rules refer to the bridge generation contract.

## Runtime invariants

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

## GPU computation and parallel generation

Follow bridge generation contract section 23: expensive repeated matrix/pixel/image calculations must run
on the GPU. Assume a game-capable GPU, never a specific vendor or CUDA availability. Use GPU
facilities compatible with supported vendors. Run independent bridge-generation steps in parallel
wherever possible while respecting data dependencies, resource ownership and native API thread
and graphics-context requirements. This does not authorize parallel calls to thread-affine APIs.

## Completion

The user-approved movable LOD subdivision exception in bridge generation contract section 5 applies here. Preserve original structural vertices, materials and interpolated
attributes when necessary subdivisions separate merged LOD parts. Keep all authored-part width
rules and all-LOD coverage. The user revoked the prohibition on high-detail-derived movable LODs
on 2026-10-17: deriving, simplifying or supplementing LODs from measured prototype high-detail
geometry is permitted under bridge generation contract section 5 and its scoped topology exception.
Native bone ownership, existing materials and visual acceptance requirements remain unchanged.
Compilation of partial stages is not completion.

Persistence must implement contract section 17. Traverse serialized external dependencies to the
built-in boundary and save byte-identical files plus original CID sidecars under
`ImportedData/<bridgeUUID>_Dependencies/`. One bridge stores one copy per CID; two bridges store
separate copies of a shared CID. Do not deserialize or modify prefab instances to make these copies.
Do not rename copied prefabs, allocate replacement CIDs or rewrite their references. Keep generated
bridge/geometry identities distinct from these unchanged dependency snapshots. Missing dependencies
or differing contents at an existing same-CID destination fail persistence explicitly. File self-check
and cleanup must recognize UUID-directory ownership and intentional identical-CID duplicates.

Before modifying runtime code, run `taskkill /IM Cities2.exe /F` even if a city/save is running,
without asking whether to save or waiting for a manual exit. The user explicitly authorizes losing
unsaved progress for this workflow. Verify the process is absent; still perform the post-edit
shutdown and deployment procedure required below.

Preserve native construction, elevation and upkeep fees. Do not allocate pricing-only prefabs,
persist a custom scalar total or override runtime prices. See bridge generation contract section 16.

After any source-code edit, follow contract section 13: invoke the exact `Cities2.exe` kill command,
verify the game is stopped, and determine whether bridge-generation code changed. If it did, remove
all mod-created bridges only for the affected style IDs and their UUID-owned artifacts; preserve
unaffected styles. If generation code did not change, remove no existing bridges. Shared generation
changes require tracing the affected style IDs before cleanup. After completing any code change,
immediately build and deploy to the game
without waiting for another request; back up the existing installation and verify installed files
against the build output as required by section 13. Compilation is not visual verification. For visual work, do not run unit
tests; inspect generated geometry and confirm both near and far views in game.

Documentation-only edits require no game shutdown, bridge removal, build or deployment. The
2026-10-15 cleanup revision is recorded in contract section 13 at branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`, rollback reference
`refs/rollback/scoped-cleanup-docs-20261015`.


## Post-load self-check

Contract section 19 defines migration-first self-check and its backup/removal fallback. Wait for
native loading, mod initialization, PDX caching and active asset batches; observe both current
readiness and completion events so late mod loading is covered. Include cached user PrefabAssets
that failed registration without force-loading or republishing them. Validation of live references
is read-only and is separate from dependency persistence under section 17.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters section 19's per-UUID backup/move, then deletion fallback on backup/move failure.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Source isolation (2026-10-13): creating bridges/copies must never select another bridge's Prefab from ImportedData (including UUID dependency directories), even with the same CID. Filter before reading; use original/permitted sources or fail. Ownership remains path-string-only. See contract's Source isolation subsection; baseline dev/5bea1d0bef04dd2ac718809852d3f657c7388e58, rollback refs/rollback/no-foreign-bridge-source-20261013.

## Documentation language

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.
The project contract records the expanded 2026-10-15 documentation policy. Preserve current requirements and
historical evidence; the 2026-10-13 language-only baseline remains in the project contract.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.
Revision baseline (2026-10-16): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.


## Native marking conversion approval

Follow the movable generation contract sections 7, 11 and 15: integrate paint into mechanism
receiver geometry with existing native object materials. Do not create independent marking objects,
material descriptors or baked textures. Curve segmentation, endpoint-fade and existing-atlas pattern
differences remain accepted; the former object-decal representation is superseded. Preserve native
motion, structural rules, selective persistence and human acceptance requirements.
Revision baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/movable-generation-contract-20261017`.
