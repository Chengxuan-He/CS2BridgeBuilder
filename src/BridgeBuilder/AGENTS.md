# Runtime Bridge Generation Instructions

For player-requested removal and native resource retirement, follow [Bridge deletion](../../docs/agent-contract/BRIDGE_DELETION.md).

### Packaged textures belong to road running surfaces

A packaged `.Texture` must have a measured road-surface consumer. Check the actual
faces, appearance type, LOD and native Surface channel that references it. Asphalt,
walkable sidewalk paving, running grating, running caps and selected-road paint
qualify only on their measured road-facing domains. A structural platform, curb
sidewall, truss, cable or underside uses its original game or DLC texture references.
This requirement also applies when a road and a structural face share the same atlas.
A descriptor conversion needed for native rendering may bind original native channels.
It must never give a non-road consumer a dependency on a packaged texture.

Retain valid native UV projection, sampling and mip identities. A local repeating
atlas island that cannot be sampled at a requested width through its original
representation retains its authored coordinates. Record any resulting change in
physical density. Do not bake a structural repeat or wrap individual vertices into
an atlas chart to fill that gap. This supersedes earlier structural repeat permissions,
including the earlier Lift05 profile recipe. Independently persisted external owner
copies remain governed by the dependency persistence contract.


Follow [player-controlled generation](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#player-controlled-generation-without-structural-rejection-gates): do not reject a generation request
because of structural shape, width, signed scale, degeneracy, overlap, clearance or appearance
quality. Such checks belong to offline verification or non-blocking diagnostics. Material
no-match selects Original. Preserve actual file/schema/index, nonrepresentable arithmetic,
write and native API errors; never fabricate successful output. This applies to all bridge
families and LODs without changing historical dependency recovery.


## Scope

Lift01 and Lift02 central-truss memberships follow the explicit complete-assembly rule in
bridge generation contract section 8, overriding default own-span stretching,
side-part translation and base mapping for those measured members. Internal
braces and narrow connectors share their complete assembly's `x -> k*x`; only the outermost
edge trusses retain their original mapping. Record assembly spans and connection
residuals offline, apply the rule at every LOD including far supplements, and
preserve bones/material modes with the corresponding affine normal/tangent frame.
Measure each style's complete source and target assembly spans; never use a small
connector's thickness or another style's span as its scale denominator.


These instructions apply to `src/BridgeBuilder/**`. They add runtime-specific requirements to
the repository root [`AGENTS.md`](../../AGENTS.md). Before editing this directory, read the complete
[`PROJECT_CONTRACT.md`](../../docs/agent-contract/PROJECT_CONTRACT.md) and
[`BRIDGE_GENERATION_CONTRACT.md`](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md).
This file replaces neither. Numbered bridge rules refer to the bridge generation contract.

## Runtime invariants

Apply general contract section 8's pedestrian truss rules at every LOD:
PedestrianDraw02's complete central assembly scales in X; both pedestrian
movable styles keep underside longitudinal truss sections rigid, distribute
their centres evenly across X and retain the central longitudinal width.
Membership and coefficients are measured offline, never inferred at runtime.

Recorded numeric data uses versioned, checksum-validated, uncompressed BBN1 typed-array resources,
organized by bridge type and embedded in the DLL. Validate and read only a demanded resource
directly from its embedded stream into request-owned arrays; no extraction files are needed.
Load numerical resources only on demand within the current generation request. Reuse decoded
BBN arrays, SIMD programs and reconstructed parameter/membership records across that request's
stages and joined LOD workers. Release all cache-owned strong references on success, failure or
cancellation. Do not preload unused bridge types or retain permanent static numerical holders.
Independent requests must not clear each other's active data. Deferred material preparation acquires
this scope only when construction begins. Bounded metadata queries may use temporary scopes, not a
permanent fallback cache. Final native output and delayed preview/publication callbacks must own
their required data rather than retaining a session or borrowed parameter arrays. Copy required
parameter slices into output-owned storage before handoff. Do not force GC or promise an immediate
RSS decrease; later requests reload and validate only their demanded resources. Follow bridge generation contract section 2; no loose-file
or installed-prototype dependency, giant initializer, or change to independently saved bridges.

Read the [runtime invariants](../../docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md#runtime-invariants)
in full before code changes. Apply the current implementation policy.
Prototype parameters and part maps remain hardcoded and independent of the original prototype file.
Use measured assembly mappings for girder, hinge, rib, support and closure members at all LODs. Preserve the selected road’s actual sidewalk roles and verify the adjoining boundaries.

## Performance and iteration

Follow the [all-bridge mapping rule](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode)
and the complete [material contract](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md):


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
The [complete material pipeline](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#complete-material-processing-without-propagated-exceptions) must return explicit outcomes through
resolution, adaptation, assembly, persistence, preview and creation without propagated exceptions.
Valid unknown materials use compatible originals, equivalent conversion, or a visible processed
native compatibility material with non-blocking difference diagnostics. Compatibility gaps must
not reject valid input. Name/CID whitelist adapters, incompatible blind shader binding and
unreported appearance losses are prohibited; genuine resource/IO failures remain explicit.
Branch mechanism decks and native network bridgeheads
by their own source/domain and render context; do not classify a mixed piece from a section name.
Require both actual receiver classification and matching measured prototype material at the target
physical part. Moving deck, fixed cap, moving sidewalk, fixed platform and external approach do
not inherit a bridge-wide material override; unmatched external approaches retain Original.
Keep deliberate part-specific appearances and all-LOD PBR/alpha, density and paint handling.
Cover each replaced component's measured edge/sidewall/cap material domains as well as its top;
do not omit matching sidewalk or median edges or blanket-replace unmatched edges.
Resource corruption and native realization failures return explicit non-exception failure results. Follow
[the material routing rule](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#total-material-routing-by-part-and-actual-source).


- Processed movable-road appearances supported by the prototype use prototype content tiled at original resolution and
  physical texel density, never stretched, at every LOD and in fixed caps/finite terminals.
  Remove embedded road markings from every road-material channel/mip; retain intended selected-road
  paint through its native path (native rigidly attached source decals for movable mechanisms).
- Test asphalt, sidewalk and wear support independently. Wear uses the same complete measured
  source and matching same-source background. Align its measured visual center to actual native
  lane centers; preserve physical width, internal relationships and longitudinal density.
  Do not infer lanes from paint or place wear outside applicable vehicle lanes.
- Section 3 permits bounded offline authored road repeats from pure original road content, with
  aligned PBR donor coordinates and complete periodic mips. Record new periods as authored, not
  original evidence. Keep source/PBR, density and phase coherent across domains sharing the same evidenced prototype appearance.
- Non-road materials retain original identity/modes by default; only evidenced repeat domains
  must use original native references, including at widths beyond a finite atlas chart.
  Unsupported road types use compatible originals or corresponding native object road materials
  under section 4, preserving source layers/projection where representable, otherwise reporting compatibility differences.
- No full-span pixels, new period cuts, arbitrary downsampling, unauthorized custom shaders or changed original
  assets. Changed derivatives receive new CIDs. Preserve complete PBR, frames, filtering and mips;
  no global tolerance relaxation or acceptance from metadata alone. Integer-grid resampling and
  bounded two-period storage follow section 3, not an unrestricted boundary-blending permission.
- Prepare known constants outside the game; runtime copies/binds them. Section 4's bounded variable
  or unknown-selected-input conversions do not excuse missing known assets or cross-owner caches.
  Separate texture content from sampling; one identical payload per style/current owner, not per use.
- Measure complete persisted owner material dependencies for information, without a per-owner ceiling; complete official
  SDK package ZIP at most 55,000,000 bytes. Prepared-directory bytes prove neither. Preserve independent
  native cold loading and healthy saved owners; report remaining quality/budget gates honestly.
- Package complete immutable resources in `assets/Materials/<BridgeStyleId>/` and ship beside the
  DLL as `Materials/<BridgeStyleId>/`. Resolve sources relative to the loaded mod, not developer or
  other-owner paths. Keep no empty/placeholder material directories. Preflight full native dependency
  graphs before registration; byte-identical shared payloads stay within each style. Section 5 permits three identical skinned-road paint channels under read-only
  `Materials/Shared/Paint/<cid>.Texture` and the native cache under `Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` plus CID;
  each generated owner still snapshots only its required dependencies independently.

Structure coordinate generation uses the common data-driven SIMD pipeline in the bridge
generation contract's CPU affine execution section. Styles supply binary floating-point
parameters, memberships and topology recipes, not separate numerical callbacks or SIMD
adapters. Use actual interleaved XYZ SIMD stages and direct owned target-buffer consumption;
runtime expression-recognition fast paths and axis-column execution are prohibited.
Preserve existing runtime intermediate precision and ordered float-rounding
boundaries; verify real packed execution and all-LOD numerical equivalence.

Apply [CPU affine geometry execution](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
through `AffineGeometry`: each XYZ axis may have independent width coefficients or remain
constant. Snapshot native data before scheduling independent LOD plans/disjoint vertex batches,
then join workers before creating or saving native assets. Keep dependent recipe stages in order,
including repeated-index semantics. Do not assume matrix execution requires dense per-vertex
storage, GPU work, or changes to topology, original road bones or materials.

Follow bridge generation contract sections 13 and 23. Choose CPU/GPU execution and parallelism from
native API affinity, correctness and measured performance. Neither GPU conversion nor a specific
style branch is a prerequisite for implementation. Build/deploy coherent increments, not every edit.

## Completion

The user-approved movable LOD subdivision exception in bridge generation contract section 5 applies here. Preserve original structural vertices, materials and interpolated
attributes when necessary subdivisions separate merged LOD parts. Keep all authored-part width
rules and all-LOD coverage. Deriving, simplifying or supplementing LODs from measured prototype high-detail
geometry is permitted under bridge generation contract section 5 and its scoped topology exception.
Native bone ownership, existing materials and visual acceptance requirements remain unchanged.
Compilation of partial stages is not completion.

Persistence must implement contract section 17. Traverse serialized external dependencies to the
built-in boundary and save byte-identical files plus original CID sidecars under
`ImportedData/<bridgeUUID>/Dependencies/`. One bridge stores one copy per CID; two bridges store
separate copies of a shared CID. Do not deserialize or modify prefab instances to make these copies.
Do not rename copied prefabs, allocate replacement CIDs or rewrite their references. Keep generated
bridge/geometry identities distinct from these unchanged dependency snapshots. Missing dependencies
or differing contents at an existing same-CID destination fail persistence explicitly. File self-check
and cleanup must recognize UUID-directory ownership and intentional identical-CID duplicates.

Preserve native construction, elevation and upkeep fees under section 16.

Before replacing game-loaded binaries, stop `Cities2.exe`, verify it is absent, back up originals
and verify installed hashes. Build and deploy each coherent buildable code increment. Do not remove
healthy existing bridges merely because generation code changed; recover only concretely incompatible
style/UUID-owned assets under section 13. Documentation-only changes require no deployment.
Automated geometry and serialization checks are permitted but do not prove visual acceptance.
Keep unfinished native motion, near/far and cold-load checks explicit and continue unblocked work.

## Post-load self-check

Follow [self-check result reporting](../../docs/agent-contract/BRIDGE_GENERATION_CONTRACT.md#self-check-result-summary): count each UUID once per run
as migrated to the latest format, removed with complete backup, removed without complete backup,
or unfinished. Show only nonzero categories with counts and one final restart reminder in a native
dialog, localized in all twelve languages. Successful no-action runs stay silent, including manual
checks. Reset reporting each run while retaining session stale-read protections; never invent an
owner count for a global exception. Backup failure still falls through to deletion.

Contract section 19 defines migration-first self-check and its backup/removal fallback. Wait for
native loading, mod initialization, PDX caching and active asset batches; observe both current
readiness and completion events so late mod loading is covered. Include cached user PrefabAssets
that failed registration without force-loading or republishing them. Validation of live references
is read-only and is separate from dependency persistence under section 17.
Before live-reference validation, supplement missing owner-local copies of other mods' materials
and required non-built-in textures/VT payloads, even for healthy current bridges. Use creation's
dependency traversal, preserve original bytes/CIDs and geometry, leave complete copies unchanged,
and report supplementation as migration requiring restart.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters section 19's per-UUID backup/move, then deletion fallback on backup/move failure.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Creation and copy operations may read valid resources from any bridge owner. Source UUID/path
or non-built-in provenance is not a backend rejection gate. Save required non-built-in dependencies
as independent target-owner snapshots; never modify or delete another owner's source. Preserve
actual resource validity, destination conflict and UUID/path write/delete boundaries.

## Documentation language

All documentation must be written in English, including audits and research reports.
Preserve literal identifiers, paths and diagnostic evidence; explain them in English.

## Movable bridge size

Follow bridge generation contract section 24: a generated movable bridge's total Prefab size, including
its dependencies, must be comparable to its original counterpart. Measure the complete graph,
not only its root or textures; preserve required content and report unresolved measurements.
Separately follow material generation contract sections 2 and 5: report each generated owner's
complete persisted material bytes without a hard ceiling, and enforce 55,000,000 bytes for the
complete official SDK content ZIP. A style's prepared-directory size proves neither limit.

## Movable generation requirements

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–24](../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#16-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Those sections define the generation pipeline: compatible original structural Surface/VT references
or required offline conversions under material contract section 2; measured non-VT repeating road
and grating units, plus the bounded variable-content and unknown-selected-input conversions
permitted by material generation contract section 4;
original native decal appearance on rigid child components attached through native bone
connection IDs, preserving artwork, alpha blending and evaluated UV density for all retained
decal sources. This authorization supersedes the former child-projector prohibition.
No receiver cuts for decals, no structural receiver-only material copies,
no moving the road onto marking bones, and no runtime synchronization dependency. Acceptance is
specific to the tested style, input and build; every style requires its own evidence.

Read the [movable runtime invariants](../../docs/agent-contract/MOVABLE_RUNTIME_INVARIANTS.md).
Use the material generation contract for material reuse, tiling and offline preparation.

Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.

## Offline source-atlas repeat materials

Follow [supported-road tiling](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#prototype-supported-road-surfaces-must-tile),
[all-LOD marking removal](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#remove-embedded-road-markings-at-every-lod)
and [offline source-atlas derivation](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#offline-source-atlas-repeat-materials).
Road repeats may be authored from pure prototype road content at original texel density;
structural surfaces retain original native texture references.
Preserve actual shape/motion/material boundaries, native loading, complete PBR/mips, informational owner measurements and the package byte limit.
Keep original assets unchanged and assign new identities to changed derivatives. This specification
does not certify implementation, quality or acceptance. No runtime strings change with these rules.

## Dynamically generated persistent material shaders

Follow [dynamic material shaders](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders). **Not implementable under current requirements:** the current game cannot independently restore the generated shader/template from its owner-local assets with Bridge Builder disabled or uninstalled. Dynamic source compilation and bounded sampling succeeded, but complete multilayer adaptation remains unsolved under the current game/project constraints: restoration of the custom shader/template would depend on Bridge Builder registration. The complete custom-shader solution is unavailable and must not be deployed or described as a density fix. The user's permission to investigate dynamic shaders remains; it does not waive independent cold loading. Independent loading with the mod disabled or uninstalled is a non-negotiable project constraint at all times. Do not offer a startup loader dependency or relaxation of independence as a trade-off. Experimental or partial success never substitutes for it. Reconsider implementation only when an evidenced native loading capability satisfies this unchanged constraint. A fixed precompiled shader is not dynamic generation. Editor compilation or raw GPU bytecode alone does not establish Player loading or independent cold loading. No continuing renderer synchronization, custom runtime controller, game launch or launcher launch is authorized.

Selected-road decals follow [source identity and coverage](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md#source-preserving-selected-road-decals): preserve each original texture, alpha and evaluated UV; no name/type/CID filtering or generic white/yellow replacement. Distinct transparent layers cannot erase each other by footprint alone.

The movable mechanism includes its stationary end caps: selected-road deterioration
must not be added there as object decals. Only measured prototype WheelTrack road
composition supplies mechanism wear. External fixed elevated approaches retain
their original native network wear; this is not a rule to erase wear from fixed roads.
Line art, rail artwork and colored-lane decals retain their source-preserving route.
