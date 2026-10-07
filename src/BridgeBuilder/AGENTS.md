# Runtime Bridge Generation Instructions

## Scope

These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md); this file does not replace it.

## Runtime invariants

Native-pricing revision (user request 2026-10-07): use the game's native pricing exclusively.
Preserve source roads' and bridge prototypes' PlaceableNetPiece/PlaceableObject construction,
elevation and upkeep fees. Let the game select composition pieces and charge network length,
height, auxiliary networks and placed objects. Do not bake a fixed total, apply a bridge-specific
multiplier/offset/floor, zero fees, override runtime cost data, or create pricing-only copies.
This supersedes all earlier requirements to retain the established Bridge Builder price and all
permissions to clone private components for pricing. Legacy conversion removes the custom cost
component while retaining its original native dependency graph and bridge/deck UUID/CID/geometry.
Previously baked assets remain valid native data; do not guess original fees or remove referenced
copies. Restoring their old fees requires authoritative original data and a separate verified migration.

- Self-check migration/repair and new generation share the same file layout and naming rules
  (contract section 19); preserve bridge/deck CID and UUID. Native conversion alone cannot skip
  layout migration. Localized notices distinguish migration from damage repair and report both
  when both occurred. Relocation is backed up, collision-safe and idempotent.

- Generated bridges must not depend on Bridge Builder; it is only the bridge asset manager.
  Persisted assets must remain loadable and usable after a cold start with the mod disabled or
  uninstalled, without its assembly, callbacks, patches or cached objects. Follow contract section 19;
  the deferred rail-seam limitation is unfinished compatibility work, not proof of independence.

- Apply the root independent-persistence revision: legacy conversion is additive; root and deck
  CID, UUID, version and geometry stay unchanged. Persist native unlocking, private-piece prices,
  native locales and thumbnail data. No BridgeBuilder component or UI host may be required by
  converted/new saved prefabs. Private structural cost copies are authorized; the scalar-only
  pricing rule below is superseded. Preserve bridge assets during deployment for acceptance.

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


## Post-load self-check revision (2026-10-06)

Rollback baseline before this instruction update: branch `dev`, HEAD
`3be5fed03b275a07850f413dc2396b0182e532ea`.
The user's subsequent request restores read-only in-memory validation for self-check only,
after both Bridge Builder and the main menu have completed loading (including late mod loading).
Healthy bridges skip file integrity scanning and remain unchanged. Invalid required references
cause UUID-scoped backup/removal, never reference repair, registration rejection or quarantine.
File ownership, path and change verification remain required before moving or clearing files;
they are not a second integrity verdict. Dependency persistence remains byte-for-byte file copying.


The subsequent migration revision (same `dev`/`3be5fed03b275a07850f413dc2396b0182e532ea`
rollback baseline) must include cached user PrefabAssets that failed native registration, not only
PrefabSystem's registered list. Do not call Load or republish to obtain a cached instance.
Validate required references read-only; retire proven null references or loaded, available assets
that failed registration. Missing cached instances or intentionally unavailable content alone are
inconclusive and must not trigger deletion. A targeted serialized check is allowed when no cached
instance exists. Healthy legacy bridges keep UUID, CID, name and geometry; copy external dependencies
before committing asset-local persistence version/metadata, backing up the original root. Do not
invent missing historical recipe values or use a separate registration store. Migration I/O failures
retain the bridge and report incomplete; successful migration is idempotent.


Latest self-check revision (rollback baseline `dev`, HEAD
`3be5fed03b275a07850f413dc2396b0182e532ea`): wait for native loading, mod initialization,
PDX database caching and active asset batch completion. Observe current state as well as completion
events so late mod loading cannot miss the check. For a required null reference, read the exact
original serialized CID and verify that its PrefabAsset already has a cached instance. If available,
recursively copy unchanged dependencies under the owning bridge UUID as in section 17, retain the
original bridge and request restart; do not modify live references or force-load the dependency.
If the CID is unavailable/not loaded or the source contains an explicit null with no CID, retire
the owning bridge directories. Unknown serialization or copying I/O failure remains incomplete,
not proof of corruption. This supersedes the earlier unconditional null-reference retirement rule.


Repair-or-remove policy update (baseline `dev`, HEAD
`3be5fed03b275a07850f413dc2396b0182e532ea`): game operability takes priority. After loading
completes, faulty owned bridges have only two outcomes: successful recursive same-CID dependency
copy for a null slot whose original CID is already loaded, or directory-scoped retirement.
Unresolved source/serialization, unavailable content, failed registration, missing prefab/geometry,
or unsuccessful dependency copy/migration must enter retirement, not an inconclusive-retention path.
Check dependencies even for already migrated bridges. Keep ownership and concurrent-change safeguards;
actual filesystem failures must be reported as removal failures, never falsely reported as success.
A failure affecting one bridge must not block removal of other bridges.

Contract section 18 supersedes earlier file ownership checks: b{uuid} path text is the sole ownership criterion for bridge asset reads, writes, moves and deletes.
