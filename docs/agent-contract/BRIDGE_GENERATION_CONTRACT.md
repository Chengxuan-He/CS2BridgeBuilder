# Bridge generation contract

## Movable bridges: no special road-width adaptation

Do not apply special adaptation based on road width to movable bridges. Retain the
ordinary measured target-minus-prototype width mapping; do not add narrow-road
compensation, clamps, replacement geometry, special bone precedence or dedicated
width thresholds. Remove narrow-road-specific generation branches and diagnostics.
Narrow-road compatibility is no longer a completion gate for the movable batch.
Finite input checks and validity of the native geometry/skeleton remain required.
This is not a claim that every arbitrarily narrow input produces a valid bridge.

This rule applies only to movable bridges. Do not change non-movable bridge
selection, geometry, validation or persistence. Narrow-road adaptation is outside the movable
generation and acceptance scope.
Existing saved assets are not rewritten merely because generation code changes.

## Contents

- [Movable bridges: no special road-width adaptation](#movable-bridges-no-special-road-width-adaptation)
- [Contract routing](#contract-routing)
- [Preserve original UV projection and mapping mode](#preserve-original-uv-projection-and-mapping-mode)
- [Offline source-atlas repeat materials](#offline-source-atlas-repeat-materials)
- [Current implementation policy](#current-implementation-policy)
- [1. Every generated bridge follows its archetype](#1-every-generated-bridge-follows-its-archetype)
- [2. The archetype is hardcoded, never copied](#2-the-archetype-is-hardcoded-never-copied)
- [3. A generated bridge differs from its archetype in geometry and road structure alone](#3-a-generated-bridge-differs-from-its-archetype-in-geometry-and-road-structure-alone)
- [4. The tower and the cables are derived geometry, never referenced geometry](#4-the-tower-and-the-cables-are-derived-geometry-never-referenced-geometry)
- [5. A generated tower differs from its archetype in width alone](#5-a-generated-tower-differs-from-its-archetype-in-width-alone)
- [6. Nothing is claimed as fixed until it has been seen working](#6-nothing-is-claimed-as-fixed-until-it-has-been-seen-working)
- [7. A fault is found by dumping both and diffing, not by reasoning about it](#7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it)
- [8. Stretch or translate is decided by one thing: does the part cross the bridge's centre](#8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre)
- [9. The suspension family is the reference; every other family is held to the same list](#9-the-suspension-family-is-the-reference-every-other-family-is-held-to-the-same-list)
- [10. Unsupported inputs are reported; movable families are in scope](#10-unsupported-inputs-are-reported-movable-families-are-in-scope)
- [11. A double deck bridge is built from a double deck archetype, or not at all](#11-a-double-deck-bridge-is-built-from-a-double-deck-archetype-or-not-at-all)
- [12. Branches organize work without blocking integration](#12-branches-organize-work-without-blocking-integration)
- [13. Deploy coherent increments and preserve healthy assets](#13-deploy-coherent-increments-and-preserve-healthy-assets)
- [14. Preserve each archetype's width allowance](#14-preserve-each-archetypes-width-allowance)
- [15. Bridge lighting is copied from the archetype as behavior](#15-bridge-lighting-is-copied-from-the-archetype-as-behavior)
- [16. Preserve native construction and upkeep pricing](#16-preserve-native-construction-and-upkeep-pricing)
- [17. Persist external dependencies as identical-CID copies owned by each bridge](#17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge)
- [18. Bridge asset file ownership is determined only by b{uuid} text](#18-bridge-asset-file-ownership-is-determined-only-by-buuid-text)
- [19. Independent native persistence and additive legacy conversion](#19-independent-native-persistence-and-additive-legacy-conversion)
- [21. Movable bridges use generated selected-road surfaces and native mechanisms](#21-movable-bridges-use-generated-selected-road-surfaces-and-native-mechanisms)
- [22. New bridge styles require immediate UI synchronization, generator routing and multilingual names](#22-new-bridge-styles-require-immediate-ui-synchronization-generator-routing-and-multilingual-names)
- [23. Performance follows measured bottlenecks](#23-performance-follows-measured-bottlenecks)
- [24. Movable bridge asset size](#24-movable-bridge-asset-size)
- [25. Adapt sidewalk-dependent components to the actual elevated road](#25-adapt-sidewalk-dependent-components-to-the-actual-elevated-road)
- [Current repository checklist](#current-repository-checklist)

This is the general normative contract for every bridge generator, fixed or movable. It is
separate from the [project contract](PROJECT_CONTRACT.md), which governs project-wide workflow,
communication and documentation. Read both together with applicable AGENTS.md instructions.
Bridge geometry, native behavior, generation, preview acceptance, persistence and lifecycle rules
are owned here; they are not optional appendices of the movable-bridge contract.

## Contract routing

- All bridge work: this contract and the [project contract](PROJECT_CONTRACT.md).
- Generator implementation and refactoring: additionally [generator code organization](GENERATOR_CODE_CONTRACT.md).
- Movable bridges: additionally [movable bridge generation](MOVABLE_BRIDGES_CONTRACT.md), incorporated by section 21.
- Persistence: additionally [asset persistence policy](../Bridge-Asset-Persistence.md), incorporated by section 17.
- Previews: additionally [preview specification](../Bridge-Preview-Specification.md).
- Materials: additionally [material generation](MATERIAL_GENERATION_CONTRACT.md).
- Sidewalk-dependent components in every bridge: section 25. Use each side's actual elevated-mode
  sidewalk presence, position and width, including absent and asymmetric sidewalks. Adapt inner
  railings and bridge-authored sidewalk parts accordingly; never impose the prototype's layout.

Existing section numbers are retained for audit continuity. Section 20 remains in the project
contract because it defines project metadata, not bridge generation. Unqualified numbered contract
references in bridge instructions refer here except section 20.

## Preserve original UV projection and mapping mode

This is a mandatory rule for **all bridge generators, fixed/non-movable and movable alike**.
It covers every component and every LOD in preview, creation, regeneration and export.
Material-specific requirements supplement this rule; they cannot restrict it to movable bridges.

Preserve the original UV projection and tiling/stretch mode **per source part, material domain,
LOD, layer and axis**. Use the actual source selected by the applicable bridge and material
rules. Each component or composed layer follows its own source evidence; this rule does not
change appearance selection, road composition or marking layout.

| Original mapping | Required generated mapping |
| --- | --- |
| Tiling | Keep tiling, original physical density, orientation, phase and repeat boundaries. |
| Evidenced finite/stretch mapping | Retain authored UVs while authorized geometry changes; interpolate UVs for permitted construction fragments. The pattern stretches with that geometry. |
| One axis tiling, another stretching | Preserve each axis independently. |
| Mirrored, folded or partial traversal | Preserve the measured orientation, bounds, phase and terminal behavior. |
| Different mapping at another LOD | Use that LOD's own source mapping and resources. |

A width change, failed period test or inconvenient renderer must not switch tiling to stretching
or stretching to tiling. Unknown/nonperiodic coordinates are not evidence of authored stretching.
A selected source uses its own mapping evidence; it cannot inherit another source's classification.
Marking layout and source-selection rules remain separate from this mapping requirement.

Record source/version, Surface and texture identities, mesh/material slot, face/domain membership,
LOD, UV range and native sampling semantics. Inspect original geometry and actual sampler behavior:
mesh UV resets can repeat an atlas region even when its texture uses Clamp; whole-texture Repeat
alone does not prove an internal island repeats. Preserve measured adjacent resets and mirrors
where retained geometry supports them. Do not add cuts or seam vertices solely for texture-period
wraps. For movable roads and the narrowly eligible repeating structural domains, use
[offline source-atlas repeat materials](#offline-source-atlas-repeat-materials). This does not
authorize unrelated topology or bone changes.
### Offline source-atlas repeat materials

Where a movable road or structural domain would otherwise require new texture-period wrap seams,
bake a qualifying independent tiling material from its original atlas outside the game, under
[material contract section 3](MATERIAL_GENERATION_CONTRACT.md#offline-source-atlas-repeat-materials).
Use only evidenced repeat units, preserving original density, independent axes/layers, phase,
finite axes and terminal domains. This applies to the evidenced Lift05 repeating profile parts
as well as eligible road domains; it grants no period-driven topology exception.

Preserve existing authored seams and necessary boundaries for actual shape, motion and distinct
material domains. Do not add boundaries solely to reset UVs, intersect with prototype triangles,
or force independent layers into a common period. Preserve selected-road shape, lanes, original
attributes and bones. Reuse original materials wherever they already express the required mapping.
Structural offline derivation is permitted only to avoid the otherwise necessary new period-wrap
seams in an evidenced repeating domain, never as general structure remaking or a failed-test fallback.

Original source assets remain unchanged. Derived resources receive their own identities and complete
native dependencies; byte-identical ownership copies retain the identity of the resource copied.
Validate the complete PBR channels, normal frames, filtering and every mip, all affected LODs,
independent native loading and the 10 MB owner / 55 MB package limits. No complete-span pixel
expansion or global tolerance relaxation is authorized. This is a required design, not evidence
that any current baked unit or native rendering path has passed those checks.


## Current implementation policy

Implement the remaining movable generators incrementally using hardcoded data from real
prototypes: other players may not possess the original prototype files. Preserve required
bridge behavior and distinguish executable stages from completed support.

### Hardcoding and source independence are retained

All prototype-specific parameters, component layouts, native motion/bind data, part membership,
construction coordinates and LOD rules must come from a real, identified prototype and be recorded
as immutable generation data. Prototype inspection and inference happen during development, not
on the player's machine. Do not invent missing values or require the player's original Prefab to
reconstruct these facts. Hardcoded profiles, ranges and construction recipes are valid; exhaustive
per-vertex literals are not mandatory when a compact measured representation yields the same result.

Separate original authoring files from the resources actually required to generate and load the
bridge. Record the necessary geometry/material inputs and their provenance. Make required non-built-in
resources available through permitted packaged data and the existing per-bridge dependency snapshots;
a developer-local path or the original prototype Prefab is not a distribution strategy. Do not assume
that hardcoded numeric parameters alone make absent geometry or textures available. Retain native
content prerequisites where applicable, source isolation, resource identity and lawful resource use.
Verify the graph without access to the original prototype files; this is distinct from cold loading
without Bridge Builder. Both independence properties must be reported accurately.

### Incremental implementation

- No blanket deferred-design refusal for draw, lift or pedestrian movable styles. Implement and
  route each actual generator incrementally; specific missing data or failed stages remain errors.
- No requirement to complete every style's exhaustive audit before coding or integrating the next
  working stage. Continue toward full generation instead of indefinitely producing research-only work.
- Per-style branches are optional. A multi-style task may integrate shared and style-specific changes
  in one working branch while keeping style data separate and preserving unrelated changes.
- CPU/GPU placement and parallelism follow correctness, API affinity and measured performance.
  GPU conversion is not a prerequisite to implement a correct bounded path.
- Automated geometry/serialization checks and isolated native integration checks are allowed.
  Missing human feedback does not stop coding or routing. Do not equate those checks with human
  visual acceptance or claim an untested motion/cold-load result.
- Build and deploy coherent buildable increments, not each intermediate edit. Stop the game before
  replacing in-use binaries, back up original installed files and verify deployment hashes. Preserve
  healthy bridge assets; cleanup requires an identified incompatibility and remains ownership-scoped.

The selected road/upgrades, native motion/signals, original road bones, single-sided native skinned
paint, material contract section 2's mapping-specific density, every required LOD, independent persistence, native fees/unlocks,
source isolation and complete localization remain required. These are outcomes to implement and
verify, not grounds to label an unfinished route complete.

## 1. Every generated bridge follows its archetype

A generated bridge is built from a real bridge of the same type — its archetype. Every parameter of
the result must be what the archetype has, not what seemed reasonable when the code was written.

This covers the whole prefab, not the parts that looked important: the components and their fields,
the sub-object placement (`m_Position`, `m_Placement`, `m_FixedIndex`, `m_Spacing`, `m_AnchorTop`,
`m_AnchorCenter`, `m_RequireElevated`), the number of mesh parts on each half of a placeholder pair,
the spawn probability, the pillar type and its offsets.

Where the code differs from the archetype deliberately, the difference is written down at the point of
difference with the reason. Anything else is a defect, whether or not it has been noticed yet.

Faults that came from breaking this rule, each found only by looking at a screenshot:

| Deviation | What it looked like |
| --- | --- |
| `m_RequireElevated` forced true (archetype: false) | props silently reclassified; the flag also sorts the road's own pillars |
| placeholder given 3 mesh parts (archetype: 1) | the game measures the placeholder's height to place the tower, so it was placed by a number 10 m too large |
| `SpawnableObject` built fresh, probability left at 0 | the replacement never won, the placeholder stayed, and the placeholder has no base — a tower hanging in the air |
| component added to `components` directly | no back reference to the owning prefab; `PlaceholderObject.Initialize` threw and the prefab never initialised |
| tower parts given no `StackProperties` (archetype: First/Middle/Last) | no `StackData`, so no `Stack` on the placed tower: drawn at the height it was modelled at, hanging above the ground by the elevation |
| cable piece given no `NetPieceTiling` (archetype: `m_DisableTextureTiling` set) | the composition packs the piece in among the road’s own surface pieces instead of laying it out across the width — cables the right width, in the wrong place |
| every generated mesh left `SubMeshDescriptor.bounds` at its default | `ToUnityMesh` calls `SetSubMesh` with `DontRecalculateBounds` and takes `mesh.bounds` from the descriptors, so each mesh declared a zero-size box at the origin while its vertices, indices and layout were all correct |
| stretch/translate split by vertex position, at half the road | the boundary is a plane through the model rather than a property of the part, so where it fell inside a leg the leg was cut in two — outer portion carried across, inner portion scaled — and the column came out a splayed slab, while its outer edge landed exactly where it belonged |
| one family’s measured distances applied to every family | the distances were held as three constants, so an extradosed tower — whose 21 m section is narrower than its 31 m road and encloses nothing — was sized to stand a suspension bridge’s 3.53745 m outside it |
| the sub-object binding rebuilt from one family’s recorded table | every bridge got the suspension bridge’s `EdgeMiddle` — one tower at the middle of each span — so an arch bridge’s pier stood in the middle of an arch instead of at the node between two, and the golden bridge got one per span where it carries them at the ends of its course |
| only the first entry naming the source tower replaced | a bridge that names its tower several times kept the rest at the donor’s own width, so a forty metre deck wore three fifty metre structures beside one forty metre one — which reads as invented pillars and is the opposite |
| only the chosen structure derived, when a style names several | the golden bridge carries a pylon at each end of its course and a pier at every node between; deriving the pylon alone left three fifty-metre structures beside one forty-metre one, which reads as invented pillars and as a tower too narrow — what it was being compared against was the donor’s own structure at the donor’s own width |
| a double deck bridge built as a single deck one with a second road hung under it | the archetype selected was a single-deck variant (double-deck ones were actively penalised), so the two levels were two structures that had never been designed to stand together |
| the second deck’s offset written as a negative y whatever the archetype said | the extradosed bridge carries its second net above the deck, so its two levels came out the wrong way round |
| the deck separation offered as a 4–24 m slider | a double deck bridge’s structure is drawn around two decks at one separation, so every value but the archetype’s put the second deck through geometry modelled to clear it |
| stretch-or-translate asked of connected components | a portal’s legs are joined to each other through its crossbeams, so the whole tower is one component and the whole tower was scaled — legs thickened or thinned in proportion, and on a bridge whose extra width is negative the two legs drew together until the portal read as one column standing in the road |
| the metadata dump named two archetypes | a fault in a family the file does not cover is a fault nothing can be diffed against, which is rule 7 defeated by its own instrument for the second time |
| one family’s piece and mesh components applied to every family | the suspension cable piece’s `NetPieceTiling` and the suspension tower’s `StackProperties` were held as templates and put on every widened piece and mesh; an arch section is not a cable sheet, and the through arch came out as a repeating discontinuity along its own length |
| carried-across components kept their references to the archetype’s prefabs | `LodProperties.m_LodMeshes` named the archetype’s own coarse meshes, so a widened piece drew correctly up close and snapped back to the width it was authored at as soon as the game swapped to a level of detail — a fault with a viewing distance attached to it |

## 2. The archetype is hardcoded, never copied

### Numeric storage in embedded typed arrays

Hardcoding means immutable, offline-measured prototype data, not giant C# literal initializers.
Store recorded numerical values in typed arrays in versioned, uncompressed BBN1 binary
resources embedded in the mod DLL. Preserve signedness, integer width, IEEE floating-point bits,
array order and all measured values. Store the `.bin` source files under
`assets/HardcodedNumbers/<BridgeType>/`; use `Shared/` only for data genuinely shared by bridge
types. Each resource is independently readable so requesting one bridge never requires
loading another bridge's data. The checked-in resources are part of the source delivery
and must be embedded by both the command-line production build and the project build. Do not
also ship compressed duplicates or a monolithic archive of all bridge arrays. Compression belongs
to the complete Paradox Mods package; do not add per-resource compression when the final package
sizes are similar. Compare identical publication file lists after official postprocessing.

Load each resource only when its owning bridge/data class actually needs it.
Do not load all resources at startup, catalogue enumeration or style selection. Prevent
eager static initialization of unused numerical resource holders. Validate the resource format,
array lengths, complete payload and checksum directly from the embedded stream and read its
typed arrays. Uncompressed loading needs no extraction file. If a development conversion needs
decompression, its decoded file must be temporary and deleted on success or failure, never a
persistent extraction cache or installed/generated bridge asset. Retain only the decoded typed
arrays in static storage for that class, like DLL-owned static variables, so subsequent use does
not read the same resource again. Do not require loose files beside the DLL,
an installed prototype, a network request, or runtime geometry inspection to reconstruct the values.
Use bounded loops to reconstruct compound records from the numerical arrays; do not regenerate
huge static initializer methods. Ordinary algorithmic constants, enum values, array indices and
compile-time language requirements are code, not prototype measurements.

Migrate every existing recorded numerical table and parameter, including non-movable bridge data,
without changing geometry, transformation rules, material references, bones, identity or behavior.
Preserve the offline extraction tools and make resource conversion repeatable after regeneration.
Verify numerical values and ordering against the measured source data, verify all embedded
resources, and run affected checks. Generated bridge assets remain independently persisted native
assets: they must never acquire a runtime dependency on these mod resources.

The archetype's parameters are read out of the game once and written into embedded data arrays. They are
**not** copied from the archetype prefab at generation time.

### CPU affine geometry execution

Express measured coordinate transformations as affine parameter matrices:
`position = origin + C0*p0 + C1*p1 + C2*p2`. X, Y and Z each have their own row;
constant axes are zero-coefficient rows. Width parameters may be distinct for the
upper deck, lower deck and structure. Preserve measured branch/domain membership,
anchors and arithmetic order. Do not replace piecewise rules with a global scale,
infer missing coefficients at runtime, or introduce new width-specific behavior.

Use `AffineGeometry` for CPU numerical execution. Existing per-vertex coefficient
arrays and part-range recipes are sparse matrix storage; they need not expand into
a dense matrix per vertex. Keep their BBN1 typed payloads inside independently readable embedded
resources and preserve exact numeric bits when the existing representation suffices. Decode reusable coefficient tables
once. General XYZ matrices and specialized scale, translation and single-coefficient
rows share the same numerical execution path. Avoid extra zero operations on
constant axes and do not fuse or reassociate measured multiply/add operations.

Snapshot native mesh channels on the main thread before scheduling work. Independent
LOD plans and disjoint vertex batches may run concurrently; native Mesh, Prefab,
asset database, material and persistence operations remain on the main thread.
Finish all workers before publishing their arrays. Bound parallelism, batch vertex
work and suppress nested parallel scheduling. Small batches may execute serially.

Execute overlapping recipe stages in authored order. Within a source-only scatter
table, retain the last row for each repeated destination. Repeated additive index
operations retain their multiplicity and order per destination. Never parallelize
shared topology counters, output lists or dependent writes without first making
ownership explicit. Numerical parallelism does not authorize triangle changes,
bone changes, material changes or persistence-format changes.

Regenerated measured code must retain these kernels. The offline
`tools/AffineGeometryMigration` lowering pass handles independent coordinate loops;
manually prepared maps and engine snapshot boundaries require review. Run the
resource integrity check, numerical equivalence/race checks, relevant measured
geometry checks and localization checks before deployment. Generated native assets
must remain usable without the mod. Record snapshot and parallel-plan timings;
build success and microbenchmarks do not prove an in-game speed improvement.

The original authoring Prefab need not be installed. Required native game/DLC content and
permitted packaged resources must still be available; hardcoded numbers do not replace missing
material, geometry or behavior dependencies. Report an unavailable required resource explicitly.

Where this lives:

- [`BridgeTowerSpec`](../../src/BridgeBuilder/Bridges/BridgeTowerSpec.cs) — the tower archetype as
  plain numbers, with no dependency on the game, so the offline tests can hold it to what was measured.
- [`BridgeTowerTemplate`](../../src/BridgeBuilder/Bridges/BridgeTowerTemplate.cs) and
  [`BridgeCableTemplate`](../../src/BridgeBuilder/Bridges/BridgeCableTemplate.cs) — apply the specs
  to a prefab. The only files that need the game.
- [`BridgeTowers`](../../src/BridgeBuilder/Bridges/BridgeTowers.cs),
  [`BridgeCables`](../../src/BridgeBuilder/Bridges/BridgeCables.cs),
  [`BridgeMeasurements`](../../src/BridgeBuilder/Bridges/BridgeMeasurements.cs) — the measured widths,
  same rule.

**Recorded, or carried across — never recalled from another family.** Rule 2 says the archetype is
hardcoded rather than copied, and the reason is that the archetype may not be installed. That reason
does not apply to a thing already in hand. When the generator is modifying something — a sub-object
entry it is replacing, a piece or a mesh it is widening — the archetype *is* the input, and its
components and fields are carried across whole, changing only what the mod exists to change.

The distinction matters because getting it wrong has one shape and it has recurred four times: a value
measured on the suspension family, held as a constant, and applied to every family that reached the
same line. The tower-to-cable distances did it, the sub-object placement did it, the cable piece's
`NetPieceTiling` did it, and the tower parts' `StackProperties` did it. Each time the suspension bridge
looked right and something else came out as a structure it had never been.

Carrying a component across carries its **references** too, and those still name the archetype's
prefabs. Anything a carried component points at that is itself being derived has to be repointed at the
derived one — `LodProperties.m_LodMeshes` is the case that showed it, and it showed it in the worst way
available: the piece drew at the right width up close and at the archetype's width from far enough
away, because the level of detail the game swapped to had never been widened. A fault with a viewing
distance attached to it is not one a screenshot of the thing being worked on will show.

So: recorded data is for what must be reproduced when the archetype is absent. What is present is
carried, not recalled. Where both exist, the recorded value becomes the check on the carried one — see
`CheckStacking`, which is the floating tower turned into an assertion.

Measurements are taken by `TowerSelfTest` and `AssetAnatomy`, which write them to
`ModsData/BridgeBuilder/`. Re-measuring is how these tables get corrected; a number that cannot
be reconciled with a measurement is wrong even when the bridge looks right.

**Measured data is never deleted.** An entry that turns out to describe something other than what it was
recorded as — a support column read as a portal — is marked, not removed. Deleting it only means it
gets measured again.

### No Boolean operations with prototype geometry

Bridge generation must not perform any Boolean operation with the prototype bridge:
intersection, union, difference or clipping against its meshes, triangles or reconstructed
prototype surfaces is prohibited. Hardcoding a copy of prototype triangles does not make
runtime Boolean operations against that copy permissible.

All prototype-specific coordinates, boundaries, transforms and part membership required by
generation must be measured offline and hardcoded as immutable construction data. Runtime
must directly apply those recorded rules, never consult prototype geometry to discover
or recompute the result. Prototype meshes remain the offline reference for shape and LOD
complexity; permitted attribute/resource reuse and recorded structural transforms are not
Boolean operations. Actual selected-road composition remains an input, not a fixed road layout.
Necessary selected-road cuts at hardcoded motion seams and appearance boundaries are distinct
from intersecting that road with prototype faces and remain subject to section 4's complexity rule.

This rule applies to every bridge style, every LOD, preview and saved generation. Replace
prototype-face intersection with direct generation using the recorded construction rules;
do not merely move the same Boolean operation behind a helper or a hardcoded triangle table.

## 3. A generated bridge differs from its archetype in geometry and road structure alone

Except for the native representation, UV-density, LOD and persistence changes explicitly
required by this contract and its specialist contracts, preserve the components and their fields, the bridge behaviour, how the tower is
anchored, where the cables are drawn, the placement flags. Two things are allowed to differ, and they
are the two the mod exists to change —

- **the road structure**, because the point is to carry a road the archetype does not have;
- **the geometry**, because a road of a different width needs a tower and cables of a different width.

Any difference without such a required conversion or measured generation rule is a defect. When a generated bridge and its archetype are dumped by
`AssetAnatomy`, the two should read the same line for line apart from what identifies an asset rather
than describes it — names, the four `m_GuidPart` fields, `version` (which `OnSerializing` stamps and
nothing else reads) — plus mesh contents, the road's own `m_Sections` and the explicitly required
native representation, material and persistence conversions above.

## 4. The tower and the cables are derived geometry, never referenced geometry

Both are produced by modifying the archetype's mesh. Neither may point at the archetype's mesh
unchanged and neither may be built from nothing.

A mesh also declares the space it occupies, and that is not carried across either — it is computed.
`ModelImporter.Model.ToUnityMesh` calls `Mesh.SetSubMesh` with `DontRecalculateBounds` and then takes
`mesh.bounds` from the union of the submesh descriptors, so the descriptor is the only source there
is. A descriptor built from the three-argument constructor leaves that field at its default, and every
mesh this mod wrote declared a zero-size box at the origin. Each submesh's bounds are measured from the
widened vertices it indexes.

For carried structural meshes, preserve raw channels, formats, indices, submeshes and material
semantics by default. The explicit movable LOD and selected-road conversion rules permit their
scoped changes. Preserve tiling density through evidenced UV mapping where geometry deformation changes a
tiling source. For movable non-road parts retain original material/texture identities by default; only the
[offline source-atlas repeat rule](#offline-source-atlas-repeat-materials) permits a narrowly
scoped derived material to avoid new period-wrap seams. Road composition is limited
to prototype-supported appearance types under movable section 17; other types use original materials. Preserve the actual appearance source's
authored UV behavior under [the general source-mapping rule](#preserve-original-uv-projection-and-mapping-mode): tiling remains tiling,
and evidenced stretching remains stretching, including mixed-axis mappings and interpolation for
permitted construction fragments. Existing appearance-selection rules determine that source; a
selected road may retain its own evidenced stretching behavior, never inherit it from a different
bridge prototype. Unknown mappings, failed conversion or missing repeat evidence do not authorize stretching.
Neither path permits unrelated
channel re-encoding, topology changes or appearance substitution.

That last point is not a detail. A net piece declares:

    Position:Float32x3@0  Normal:SNorm16x2@1  Tangent:Float32x1@1  TexCoord0:Float16x2@1

Two components of signed normalised 16-bit for a normal, because it is octahedrally packed; one float
for a tangent, because it is an angle about that normal. Reading those back through Unity's convenience
accessors gives unpacked `Vector3` and `Vector4`, and writing those out declares three and four floats
where the shader expects two shorts and one float. The renderer walks each vertex at a stride it
computes from the declared layout, so from the first vertex on, every channel is read from the wrong
place. Positions survive — they are `Float32x3` either way and come first — which is why the geometry
was the right size and everything about it was wrong, and why the cables drew as shards lying over the
deck.

A derived render prefab is not only its mesh. The archetype puts components on these prefabs — the
stacking that lets a tower reach the ground, the tiling flag that decides which group a cable piece
is laid out in — and they are as much a part of what the thing is as its vertices. Both were missed
the same way: the fields were carried across and the components were not, so the geometry measured
correct and the result was in the wrong place. They are recorded and applied like every other
archetype parameter, by rule 2.

Outside those explicit conversion paths, nothing is re-encoded. The raw vertex buffer is read, each attribute is lifted out of its stream at
the offset and width the mesh says it occupies, and handed on unchanged. Only the positions are
rewritten unless the density-preserving UV rule requires a measured remapping.

### Prototype-referenced mesh complexity

Generated bridge meshes must use the prototype bridge's meshes as their reference.
Excessive mesh subdivision or triangle cutting is prohibited. This applies to fixed and movable
bridges, generated road surfaces and structures, previews and persisted assets, at every LOD.
Use each corresponding prototype LOD's mesh organization and level of geometric detail as the
reference; do not carry near-detail tessellation wholesale into reduced-detail levels.

Necessary cuts for actual geometry, motion, material or selected-road boundaries remain permitted,
including the approved merged-part subdivision exception. That exception and permission to derive
LODs from high-detail geometry do not authorize excessive tessellation. Do not let repeated UV
tiling, marking conversion or intermediate clipping determine an unnecessarily dense final mesh.
Preserve required road composition, appearance, structural parts and bone ownership while producing
geometry appropriate to the prototype's corresponding LOD. Inspect the resulting topology and
triangle counts against that reference; compilation alone does not establish compliance.

## 5. A generated tower differs from its archetype in width alone

### General width-only transformation rules

Except for the measured Golden ornament exception below, generation changes only tower width. Everything else — height, the
shape of the legs, the parts and where they sit, the materials, every component and field — is the
archetype's.

This tower rule keeps height and depth unchanged outside that explicit ornament exception. Other measured movable-part recipes may
have width-dependent X, Y and Z coefficients under section 2; those coefficients must come
from that part's prototype evidence, not an invented tower deformation.
Concretely, the tower-width mapping in `TowerWidening` moves vertices along x. Whether a part is stretched or
translated is rule 8's question and only rule 8's — does it cross the centre line — and both answers
move the part's outer edge by half the extra width, so the two agree wherever parts meet.

Do not split the stretch/translate decision by vertex position or half the road width. Such a
boundary can cut through one authored leg and give its vertices incompatible displacements.
Classify the complete part under rule 8. Outside the explicit base mapping, do not apply
`sign(x)` translation to a continuous centre-crossing member: the discontinuity opens a gap.

Outside the measured Golden ornament exception, tower height and depth are untouched by construction.
Mesh part offsets move by the same shift as the
vertices they belong to, so the parts stay together. Authored placement/stacking bounds are derived from the archetype with only x adjusted;
these are distinct from the indexed mesh/submesh bounds that section 4 requires computing — a pillar's authored bounds reach below what its
geometry draws, and that is how the game knows how far down it may be placed.

At the archetype's own width the shift is zero and the result is the archetype, vertex for vertex. That
is the property the tests check, and it is what makes "derived" mean something.

Two consequences worth stating, because both were got wrong:

- A tower is never scaled. Scaling thickens the legs in proportion; the tower stops being that tower.
- The tower stands the archetype's distance outside the cables, at every width. Measured on both of the
  game's suspension bridges, which are different road widths and agree to five decimals:

  | part | 5 lanes (road 24) | 4 lanes (road 20) | outside the cables |
  | --- | --- | --- | --- |
  | base | 18.75000 | 16.75000 | 5.27667 |
  | leg | 17.01078 | 15.01078 | 3.53745 |
  | top | 17.15220 | 15.15221 | 3.67887 |
  | cables | 13.47333 | 11.47333 | — |

  The tower's width is derived from this, not from the road. Solving
  `towerOuter + extra/2 == cableOuter + distance` gives the widening directly, and it comes out the same
  whichever part is measured because the archetype satisfies all three distances at once. The deck's
  width minus the tower's authored road width gives the same answer when the tower and cables share
  an archetype; it does not establish the required gap when their sources differ. For bridge types
  without cables, use the measured road-width relationship.

  This holds because the legs are carried out rigidly by half the extra width and the cable sheet is
  stretched from the span it draws, which moves its outer edge by the same half — two code paths that
  agree rather than one that enforces it. So it is also measured on the result and reported as a defect
  when it drifts. It can drift without anyone making an arithmetic mistake: the tower archetype is
  chosen by width from the recorded list and the cables come from whichever installed bridge carries
  that tower, and the same tower is carried by several. Let those resolve to different bridges and the
  distance becomes tower(A) minus cables(B), which is not this constant and never was.

- The cables keep their distance from the tower. Both are derived from the same archetype by the
  same extra width, so the gap between the cable’s outer edge and the tower’s is preserved exactly
  when both edges move by half the extra. A proportional stretch does that only if it divides by
  the distance being scaled — the span the mesh actually draws, not the width the piece declares.
  27 declared against 26.94664 drawn left the cables 1.6 cm inside the legs at a forty metre road:
  too small to see, constant, and in the one dimension the rule exists to get right.
- A continuous surface is not a portal, and rule 8 tells them apart without being told. Cables are one
  sheet reaching across the centre, so they are scaled; a portal's legs do not reach it, so they are
  carried. Both put the outer edge in the same place. Before rule 8 this was two named rules a caller
  had to choose between, and choosing the portal rule for a sheet tore it open down the centre line.

### Approved movable LOD representation exception

When a merged structural LOD triangle cannot represent its authored parts' separate width
transforms, subdivide only as necessary. Preserve original vertices, interpolate original
attributes, retain material semantics and apply the same offline part classification. This
scoped index/topology change does not authorize runtime part discovery or excessive tessellation.

Integrate paint into the same mechanism using a separate skinned mesh under movable contract
sections 7 and 18–19. Independent marking decal
objects are prohibited. No custom shader or mod-dependent runtime controller is authorized.

### High-detail-derived movable LODs permitted

Deriving, simplifying or supplementing movable LODs from the measured prototype's
high-detail geometry is permitted; it is not limited to subdividing existing coarse triangles.
Vertex/index/topology changes are limited to that measured LOD derivation. It does not change authored-part classification and width
transforms, native skeleton/bone ownership, existing-material reuse, UV projection requirements,
all-LOD coverage or human visual acceptance. Custom runtime shaders remain prohibited. Material preparation follows the material generation contract.

### Golden ornament and support mappings

The measured Golden tower fan ornament uses uniform XY scaling about the top of its central
spoke, with Z unchanged. Its recorded mounted lamps follow the same position map; their
rotation, effects and other fields stay unchanged. Tower legs and the upper crossbeam retain
their ordinary mappings. Apply only the exact offline membership and LOD correspondence in
[Golden ornament mapping](GOLDEN_ORNAMENT_MAPPING.md).

The recorded Golden below-deck centre truss retains its original coordinates. Side structures
remain width-adjusted; each outer vertical return follows its complete side upright with one
rigid displacement. See [Golden support mapping](GOLDEN_SUPPORT_MAPPING.md) and
[return alignment](GOLDEN_RETURN_ALIGNMENT.md). These measured exceptions do not authorize
runtime detection or changes to other structures. Their evidence does not imply visual acceptance.

### Native materials and prepared resources

For movable bridges, reuse original non-road materials by default. Only evidenced repeating
domains that would otherwise require new period-wrap seams may use the source-atlas offline
derivation defined in the material contract section 3; do not add those seams. Road composition/precomposition (offline material baking outside the game) has
one entry condition: the prototype supports the actual appearance type and that type must use
prototype appearance. Test asphalt, sidewalk surfacing and wheel-track/rut effects independently
per prototype and physical domain. Other road types retain their original input materials and
bypass composition. Preserve each source's projection and tiling/stretch mode per part, LOD,
layer and axis. This is a generation rule, not a reverse-engineering finding or proof that the
current implementation complies. Road processing must not mutate a shared structural Surface.

Follow the [material generation contract](MATERIAL_GENERATION_CONTRACT.md). Fully compatible
original materials remain direct references without new descriptors or texture payloads. Required
native object-template conversions preserve source appearance and dependencies; known, preparable constant derived
materials are prepared offline and packaged per style. Runtime generation only copies and binds
those finished resources. Repeatable units retain original per-axis texel density and pattern
scale. Preserve evidenced source stretching with authored UVs, native textures and material identity
under [the general source-mapping rule](#preserve-original-uv-projection-and-mapping-mode); record the actual selected appearance source and its part/LOD/axis
evidence, including mixed mappings. Selected-road/track inputs follow their own source mapping,
not a different prototype's stretching permission. Unknown or failed mappings do not qualify.
Never expand any road pattern into full-span texture pixels, including nonperiodic or mixed content
and multiple pages. Genuinely variable local content may use bounded native composition under
material contract sections 3–4 while preserving its source mapping. Selected third-party road/track inputs that
are unavailable before runtime may receive necessary descriptor or bounded texture conversion
under material contract section 4, after direct reuse and descriptor-only options are considered.
Missing known prepared resources do not qualify; no per-sampler bake or cross-bridge generated-result
cache is permitted. Do not modify original assets or introduce
custom runtime shaders. Preserve independent per-owner external dependency snapshots.

### Moving-road marking representation

Use a compact single-sided native skinned paint mesh on the same mechanism object, with
0.001 m upper-normal clearance and the original road bones, transforms and stacking metadata.
Preserve selected-road layout and color. Marking polygons may split at motion seams; receiver
triangles must not be cut for paint. Cuts solely for texture-period wraps are prohibited; use qualifying offline source-atlas repeat materials. No child projectors or per-dash objects.
See movable contract sections 18–19 for material, binding, visibility and persistence requirements.

### Approved native marking approximation

Curve segmentation and endpoint-fade differences remain accepted for converted markings, together
with existing-atlas pattern differences. Independent marking objects are prohibited.
Use separate single-sided skinned paint on the same mechanism with compatible native materials; preserve measured bone ownership. Do not add
custom shaders. Material descriptors and offline texture preparation follow the material generation contract. Structural rules, traffic lanes and human
visual acceptance remain unchanged. See the movable generation contract sections 7 and 15.

## 6. Nothing is claimed as fixed until it has been seen working

A change that compiles and passes the offline tests is a change, not a fix. The offline tests cover
arithmetic and recorded data; they cannot see a mesh, a material, or a prefab in a running world.

Automated checks may verify recorded data, topology, skinning and serialization. They do not prove
visual correctness. Inspect real prototype/generated geometry and all LODs, read diagnostics, and
record in-game near/far and motion acceptance separately. Compilation is not visual verification.
Missing human feedback does not suspend implementation of remaining generator stages.

**Near and far views are one implementation and must be changed together.** Any change to a visual
part — including its geometry, transform, component selection, material-facing mesh data or bounds —
must be applied consistently to the highest-detail mesh and to every LOD that can replace it. Changing
only the near mesh, only one LOD, or otherwise fixing a single viewing distance is forbidden. The
highest-detail archetype decides the identity and transform of the authored part once; every far-view
representation inherits that same decision. Completion requires inspecting both the near and far
generated geometry and then checking both viewing distances in game. A correct result at one distance
does not compensate for a defect at another.

So: state what the evidence shows and what it does not. "The report says the tower is 38 m across" is a
fact; "the tower is fixed" is not, until a bridge has been built with it.

## 7. A fault is found by dumping both and diffing, not by reasoning about it

Every fault in this project that survived more than one round was found the same way, and none of them
were found by thinking harder about the geometry. The method is written down because the alternative
kept being tried first.

### Dump both, normalise, diff

`AssetAnatomy` writes the generated prefab and its archetype into one file. Strip what identifies an
asset rather than describes it — names, the four `m_GuidPart` fields, `version` — and diff the two
blocks. Both of the last two faults came out of a diff that was three lines long:

| Fault | What the diff said | Rounds spent before diffing |
| --- | --- | --- |
| tower floating | archetype's three mesh parts carry `StackProperties`; the generated ones carry nothing | five, on pillar type, bounds, placement and the placeholder pair |
| cables misplaced | archetype's piece carries `NetPieceTiling`; the generated one carries nothing | three, on vertex maths |

Both were a **missing component**, not a wrong number. That is worth stating on its own: the numbers
were right every time, which is exactly why reasoning about them produced nothing. Diff the whole
prefab, not the part that looks relevant.

### Read the game's IL to learn what a field does

Never infer a field's meaning from its name, and never infer an enum's values from the order its fields
appear in metadata. Decompile the game and read it. What that has settled here:

- `PillarType` is `None = -1, Vertical = 0, Horizontal = 1, Standalone = 2, Base = 3`. Field order had
  been read as values, giving `Base` where `Standalone` was meant.
- `StackProperties` → `ObjectInitializeSystem.UpdateStackBounds` → `StackData` →
  `SubObjectSystem.CreateSubObject` adds `Game.Objects.Stack` with
  `m_Range.min = m_FirstBounds.min − Elevation.m_Elevation`. That is the whole mechanism by which a
  tower reaches the ground, and it is not guessable from any field name.
- `NetPieceTiling.m_DisableTextureTiling` → `NetPieceFlags.DisableTiling` (16) →
  `CalculateCompositionPieceOffsets`, which lays a composition's pieces out in separate groups chosen
  by that flag, each packed along its own cursor.

The instrument is a small IL dumper built on `System.Reflection.Metadata`, kept in the scratchpad. It
decodes switch jump tables, float constants and enum values, which is what these questions need.

### A component that reads as cosmetic may be structural

`NetPieceTiling` reads as a texture setting and decides where a piece is laid out. `StackProperties`
reads as a rendering detail and decides whether the tower touches the ground. So a component is never
dismissed by its name, and "we carried the fields across" is never the same as "we carried it across".

### Ruling a suspect out counts, and a shared code path is the cheapest way

Two suspects were eliminated without a single experiment:

- The vertex-format path could not be the cable fault, because the towers go through the same
  `BuildModel` and the towers render correctly.
- `isPacked: false` could not be corrupting the normals, because `PackFloatAttribute` only fires at
  `dimension == 3` and the channel is declared `SNorm16x2`.

Write down what has been ruled out and why. An unexamined suspect gets re-examined every round.

### Check that the instrument can see the thing before trusting its silence

`AssetAnatomy` capped mesh loading at 64 render prefabs **for the whole dump**, so the archetypes listed
first spent the budget and the generated bridge reached its own geometry with none left. Every vertex
layout in the file described a mesh already known to be right, and the generated cable's geometry was
never in the file at all. A round went into looking for a sign error in metadata that structurally could
not contain one.

So before concluding a dump shows nothing wrong, confirm it contains the thing. Budgets, caps, depth
limits and filters are all places where a diagnostic quietly stops covering what it is pointed at, and
they must be per-subject rather than per-run.

### A check that reports is an instrument, and is wrong until it has met its cases

Validate diagnostic quantities against the actual authored parts and representations they measure:

- Compare the same mesh and part membership before and after transformation; do not compare a
  whole section including anchorages with one isolated output mesh.
- Distinguish a spanning member's intended width change from a side member's thickness.
- When sampled runs overlap or merge across the centre, inspect their recorded membership and
  transformations before interpreting the aggregate run length as thickness.
- A vertical member must receive one consistent authored-part mapping at every height. Merging
  runs caused by incompatible displacements within that member indicate a real defect; do not
  dismiss the measurement merely because another case produced a false positive.

Exercise applicable coarse-LOD and spanning-surface cases before promoting a diagnostic to ERROR.
Respect the movable no-special-width rule: diagnostic validation does not authorize a narrow-road
correction or an additional compatibility gate. Inspect the reported geometry before changing or
removing a check; an indirect measurement can expose a defect that aggregate width misses.

### Say plainly when a fix is not the reported fault

The cable stretch divided by the piece's declared width instead of its drawn span, putting the outer
edge 1.6 cm inside where it belonged. Real, worth fixing, recorded as rule 5 — and not what the
screenshot showed. A small thing found while looking for a large one is reported as what it is. Letting
it stand in for the answer costs the next round.

### When a fault survives several rounds, change category

Five rounds of the floating tower were spent on the tower; the fault was on its parts. The cables took
three rounds of vertex arithmetic, then a missing component that was real and was not the cause, then a
4 mm arithmetic correction that was also real and also not the cause; the fault was a field nobody had
written, in a struct nobody had looked at. If two attempts inside one category have failed, the next
move is not a third — it is to dump the thing and diff it.

### Two archetypes of different sizes are a test oracle

Where the game ships the same thing at two sizes, the pair states the rule the generator has to obey,
and states it without running anything. `Suspension Bridge - Highway Oneway - 5 Lanes` and its 4-lane
sibling settled the widening rule in one table:

| part | 5 lanes (road 24) | 4 lanes (road 20) | offset from the road |
| --- | --- | --- | --- |
| Base Mesh | 37.50 | 33.50 | 13.50 |
| Mesh (shaft) | 34.02 | 30.02 | 10.02 |
| Top Mesh | 34.30 | 30.30 | 10.30 |
| cable piece | 26.95 | 22.95 | 2.95 |

Every part sits a constant distance outside the road, so widening moves every outer edge by half the
extra width — which is what the rigid branch does, and it holds for the cable sheet as much as for the
legs. A generated tower at the archetype's own width then reproduced all three parts exactly
(512/432/4236 vertices, 37.50/34.02/34.30 m), so the identity property is measured rather than asserted.

Use the pair before forming a theory. It costs one dump and it eliminates whole hypotheses: this table
is what showed the tower width was not the cable fault, and the same table is what makes
"inner spacing − road width is constant" true by construction rather than by hope.

### A field that describes the result is computed, not carried across

Rule 4 preserves source channels except for its explicitly required conversions. Bounds are not a
property of the source that can be copied and not a
property of the vertices that anything derives automatically — they describe the *result*, and the only
code in a position to compute them is the code that produced it.

So when carrying a thing across, sort its fields into three: copied, changed, and **computed from what
was changed**. The third pile is the one that gets forgotten, because nothing at the call site looks
wrong and the source has nothing to compare against. Anything describing an extent, a count, a hash or
a total belongs in it.

### A default-constructed value is an assertion, not a blank

`new SubMeshDescriptor(start, count, MeshTopology.Triangles)` leaves `bounds` at its default. That
default is not "unset, please compute" — it is a zero-size box at the origin, and the renderer believes
it. Every mesh this mod ever wrote asserted that it occupied no space.

This is a distinct hazard from a wrong value: a wrong value is a mistake somewhere, while a default is
a mistake nowhere, sitting in a constructor that reads as complete. When a constructor takes fewer
arguments than the struct has fields, the missing ones are decisions that have been made silently.

### Read the flags the callee is passed

The whole fault turned on one integer in the game's code:

    Mesh.SetSubMesh(index, descriptor, flags: 15)

15 is `DontValidateIndices | DontResetBoneBounds | DontNotifyMeshUsers | DontRecalculateBounds`. That
last bit is the entire mechanism — it is the reason Unity's usual "the mesh will work this out" does not
apply, and it is invisible from our side of the call. Assuming a well-known API behaves the way it
usually does is the same error as assuming a field means what its name suggests.

So when handing data to something that will build an object from it, read what it does with it, and
read the flags it passes on. A numeric flags argument in someone else's code is worth decoding in full.

### Worked example: the cables, end to end

Kept because the shape of it is the lesson, and because most of the effort went into the parts that
turned out not to matter.

| Round | What was suspected | What it cost | Outcome |
| --- | --- | --- | --- |
| 1–3 | the widening arithmetic | three rounds | nothing; the vertices were right the whole time |
| 4 | vertex channel formats | one round | real bug, fixed, cables still wrong |
| 5 | `NetPieceTiling` missing | one round | real bug, fixed, cables still wrong |
| 6 | tower width wrong (a stated hypothesis) | one dump | refuted by the two-archetype table above |
| 7 | **the dump could not see the generated mesh** | one line of code | the fault, visible immediately |

Round 7 is the whole method. `AssetAnatomy` capped mesh loading at 64 render prefabs for the entire
dump, so the archetypes spent the budget and nothing generated was ever read; every vertex layout and
every extent in the file described a mesh already known to be correct. Making that budget per bridge
took one line, and the next dump answered the question in one row:

    archetype 5-lane shaft   Extents: (17.01, 10.00, 2.98)
    generated Suspension-40  Extents: ( 0.00,  0.00,  0.00)

All fifteen generated meshes, towers and cables alike, with correct vertices, correct indices and
correct vertex layouts.

Three things are worth taking from it. The decisive move was **repairing the instrument, not the
code** — six rounds of hypotheses lost to a diagnostic that structurally could not report the fault.
The hypothesis that got checked in round 6 **was wrong and checking it was still right**, because
measuring it produced the table that both refuted it and made the real fault visible; the failure mode
to avoid is arguing about a hypothesis rather than measuring it. And the bug had been in **every mesh
the mod had ever written**, from the first one — a fault that old is never in the part that changed
recently, and looking there first is what cost rounds 1 to 3.

## 8. Stretch or translate is decided by one thing: does the part cross the bridge's centre

### User-approved central railing post exception

An independent vertical railing post centred on the bridge centreline retains its original
width and centred position. Apply zero transverse displacement to that entire authored post
at every road width and LOD. Identify exact membership offline from the highest-detail
archetype; no runtime position band, aspect-ratio test or name heuristic may identify it.
Coarse merged faces use the same measured correspondence and permitted subdivision.

This exception takes precedence over the general centre-crossing stretch rule and its refusal
language throughout this contract and its instruction summaries. It does not apply to transverse
rails, crossbeams, walls, decks, side posts or unrelated structural parts. It does not authorize
new structural materials, skipped LODs or a fixed-width whole bridge. Permission to derive movable
LODs from high-detail prototype geometry is defined separately in section 5.

### General classification outside the explicit exceptions

A part that crosses the centre line is stretched. A part that does not is translated. That is the whole
rule, and it is the only criterion — not how far a vertex sits from the middle, not how wide the road
is, not which mesh the part belongs to.

### Default classification and explicit measured exceptions

The default authored-part decision is whether the complete part reaches or crosses `x = 0`:
crossing parts stretch against their own span; side parts translate rigidly. Apply the recorded
deck-adjacent base mapping, centred vertical railing-post exception and complete Draw03 truss
exception in this section and the Golden mappings in section 5 before that default. All are measured offline and apply at every LOD.
Do not invent other family-specific boundaries or overrides. Reject a mapping inconsistent with
its recorded rule before writing geometry. Metaprogramming records the applicable rule and exact
membership; runtime does not rediscover or reclassify the part.

### Runtime contains no coordinate heuristics

Runtime authored-part identification must not use a fixed, non-zero coordinate. Applying an
explicitly recorded construction coordinate or transform is permitted. Comparisons of `x`, `y` or `z`
with zero are valid runtime spatial tests with any comparison operator (`<`, `<=`, `>`, `>=`, `==`
or `!=`); for the stretch-or-translate decision specifically, the `x = 0` axis remains the default
classification boundary outside the explicit exceptions above. A test such as “translate this part when `x > 10 m`”,
“this is a railing between `y = -0.5 m` and `y = 3 m`”, or the same test hidden behind a named constant,
ratio, road-width fraction, bounding-box extent or family-specific fallback is forbidden. A small
numeric tolerance may implement equality with an axis origin; it is not another boundary and may never
be used to create a non-zero selection band.

If identifying an authored part needs a non-zero coordinate, that identification belongs exclusively
to the **metaprogramming step**. The metaprogram may inspect the archetype mesh, walk topology, slice
height bands, compare bounds or use temporary non-zero thresholds. Its reviewed output is committed as
immutable source data: the exact archetype and mesh/LOD identity plus the exact component coordinates or
vertex membership which the runtime must transform. The threshold and the inference which produced the
data do not enter the game assembly's generation path.

Runtime code therefore hardcodes the metaprogram's result; it does not rediscover it. Runtime may look
up a recorded part by archetype and mesh identity and apply the recorded transform to its recorded
coordinates. It may not inspect bounds, nearest vertices, connected components, height bands, relative
span, aspect ratio, mesh size or naming resemblance to guess which part it has. Missing or mismatched
metadata is unsupported input to report, never permission to fall back to a geometric heuristic.

Metaprogramming records which logical authored parts touch or cross `x = 0` and the exact
membership of the explicit exceptions. It may not invent additional transform rules. Its purpose is to move
the expensive geometric identification out of the game runtime, not to introduce another criterion.

The highest-detail archetype mesh makes this decision once for itself and for every level of detail.
An LOD is a representation of those same authored parts, not another archetype and not another vote on
whether a part reaches `x = 0`. A coarse mesh may weld together parts which the full mesh keeps separate;
its reduced topology must never turn a translated side truss, arch or railing into stretched material.
Every LOD reuses the full-detail part profile, and generation reports the mismatch and stops the
derived prefab if a carried range does not receive the same rigid translation at every level. Apply
the error-boundary policy below; stop rejection before geometry is written.

### Runtime error boundaries

Runtime failure contract: excessive defensive programming is prohibited.
Defend external logic and data outside the mod's control, including generated bridge Prefabs that
can subsequently be damaged. Validate external input and contain game or third-party API failures
at their boundaries. Internal mod logic should not fail during correct operation: violated internal
invariants must surface as explicit ERROR, with exceptions where appropriate and diagnostic context
preserved. Do not add special cases, silent returns, fallback values or exception swallowing merely
to suppress an internal error. Fix the underlying defect. Stop failed operations before publishing
invalid geometry.

### Authored-part measurement

Identify complete authored members offline from the highest-detail prototype and carry their
membership to every LOD. Height slices and connected components are analysis aids, not the
units that independently choose transforms. A connected portal contains distinct columns and
crossbeams; a column must not change displacement at different heights. Use each crossing
member's own measured reach, never half the road width or the envelope of the entire tower.
Store the resulting membership and affine coefficients as immutable data. Runtime applies
those data without slicing, topology searches or part reclassification.

### Coordinate mappings

Outside the explicit exceptions, use `d` for half the extra width and `s` for the
recorded positive half-span (axis-to-end reach) of the crossing member, not its full width:

    does not cross    (x, y, z)  ->  (x + sgn(x) * d,  y, z)
    crosses           (x, y, z)  ->  (x * (s + d) / s, y, z)

The first has no stop at the centre, and no part of the tower is held back so that another can avoid
reaching it. The second uses the complete member's own recorded reach, not a height-dependent tower envelope.

### Deck-adjacent base definition and transformation

**Draw03 complete deck-base truss exception:** the user requires the moving deck-base
trusses to stretch rather than translate. Near/middle measured parts 0 and 1 form one
positive-leaf base; part 88 is the negative-leaf base. Use their full authored span
[-12.5, 12.5] m: `x' = x + (x / 12.5) * delta`, preserving Y/Z, topology, materials
and original bones. Do not stretch the positive halves independently.
The complete moving under-deck truss (five longitudinal frames,
diagonals and connectors, 1,498 measured components per near/middle LOD) uses the
same full-deck scale. This overrides both side-part translation and independent
centre-member stretching for these recorded vertices. Use `Draw03Truss.py` data;
do not classify components at runtime.
Fixed approaches are explicitly included: First300, Second300, Last300 and
Edge350 structural families scale their local X and native piece widths by
`1 + delta / 12.5` in every LOD. The Side50 railing remains rigid and follows
the resulting native layout. Do not put the entire width increment in First300,
or apply the old independent central-brace mapping afterward. Piers and mechanism
axles retain their separate mappings. All Y/Z, materials, bones and topology remain.
The far merged moving shells already use measured span stretching; retain that recipe.
Store membership and measured values in embedded arrays, with CPU affine execution.

The complete Draw03 fixed-truss stretch includes the 92 fixed-bone end
components embedded in the main mechanism mesh, not only the independent approach
NetPieces. Apply the shared deck-span affine map to their longitudinal beams,
diagonals and bearing/connection plates in both detailed LODs. Preserve bone zero,
materials and topology. The native far merged shell has no separate bearing details;
retain its continuous shell representation. Do not classify masonry foundations or
ordinary network pillars as trusses merely because they are stationary.

**The base is the structure immediately beneath the bridge deck.** "Base" has this one exact meaning in this project: the base
structure directly below and immediately adjacent to the road deck which supports or frames that deck.
It does not mean a pier footing, a pillar foot plate, a
tower foundation, a mesh merely containing `Base` in its prefab name, or any other lower structure.
Prefab naming must never override this spatial and structural definition. Before changing a base, the
Agent must locate this below-deck structure in the bridge archetype and apply the base rule to its
highest-detail mesh and every LOD; modifying another structure is not an implementation of a base
request.

**Outside the explicit Draw03 truss exception, the deck-adjacent base uses
`x -> x + sign(x) * d`.** Apply the complete displacement to its recorded side blocks.
Classify the complete base offline; its individual blocks need not cross the centre. It is also the part seen against the road: when it
is a metre out, the bridge is a metre out, whatever the rest of the tower is doing. Nothing about
another part of the same tower may reduce the `d` it is carried by.

Both guards that were built around the first mapping cost more than they saved:

Stopping at the centre was there so a part brought in by more than it stood out would close flat rather
than pass through itself. It closes flat as one column, which is not a portal either, and it thins the
leg on the way — the single place in the whole rule where a leg was allowed to change shape.

Holding the tower back was worse. It is one number for the whole tower, so the narrowest part decides
for every other one: a V pylon whose legs stand 5.79 m apart at the bottom held its base to 4.79 m of
narrowing where the road wanted 8, and the base came out three metres too wide to protect a part nobody
was looking at.

For movable bridges, do not add a dedicated narrow-road diagnostic or correction for a part
carried through the centre. Preserve native validity/error checks. Other width audits record the
measured result without changing the authored mapping. What it means is that the road is
narrower than the design was drawn for, which is a fact about the pairing and not something a widening
rule can repair.

### Crossing, side and centre-touching parts

The two cases, and why each is what it is:

- **Crosses the centre** — crossbeams, cable sheets, anything continuous from one side to the other.
  Scaled about the centre so that its outermost vertex moves by half the extra width, which is exactly
  as far as the legs it meets have moved. It stays attached at both ends and the middle simply spreads.
- **Does not cross** — legs, anchor blocks, everything belonging to one side. Translated by half the
  extra width, away from the centre. Its shape, thickness and proportions are untouched, which is what
  makes a widened tower the same tower.

Both move their outer edge by the same half of the same number, so parts that met still meet, and the
distances of rule 5 hold whichever branch a part takes.

A component that only touches the centre — one that reaches x = 0 and lies on a single side — counts as
crossing. Translating it would open a gap against the component mirroring it. Scaling leaves its vertex
at zero where it is and moves the far end out, so the two halves stay together.

## 9. The suspension family is the reference; every other family is held to the same list

Suspension bridges were the initial reference family. Their verified behavior supplies a checklist
for other families; it is not a claim that they are the only supported or accepted designs.
Each family must satisfy the corresponding checks against its own archetype before it is complete.

What was established, and how each was settled:

| Property | Settled by |
| --- | --- |
| the tower reaches the ground | `StackProperties` on each part — First / Middle / Last, direction Up |
| the tower is placed by the right branch | `PillarType.Standalone` = 2, read from the enum, not from field order |
| the swap happens | placeholder with 1 part, replacement with 3, `SpawnableObject` probability 100 |
| the cables sit where the road runs | `NetPieceTiling` on the piece; without it the composition packs it among the road's own surface pieces |
| every mesh declares its volume | `SubMeshDescriptor.bounds` computed from the widened vertices it indexes |
| legs keep their shape, beams stay attached | rule 8: crossing the centre decides stretch against translate |
| the tower stands the archetype's distance outside the cables | measured on two bridges of different road widths, then used to size the tower |

Nothing on that list is suspension-specific in principle, and most of it is already family-agnostic in
the code: rule 8 asks the geometry, the stacking and the tiling are the archetype's own components, the
submesh bounds are arithmetic. Two things are not.

**Measured numbers belong to the tower they were measured on.** The distances of rule 5 were first held
as three constants and applied to every tower with an overhead section. Six of the game's families have
one and two of those are the envelope the road runs between; the rest are something else entirely — an
extradosed bridge fans its cables from a low pylon over the deck and its section is 21 m against roads
of 31 and 61, a lift bridge's section is its lifting mechanism, the grand bridge's is a stiffening
truss. Sizing one of those towers to stand 3.53745 m outside its section is sizing it against a bridge
it has nothing to do with. So the distances are recorded per tower, and a tower is sized against its
cables only when **both** hold: the section is recorded as the outer envelope, and the distances were
measured on that tower.

**Unmeasured is a state, not a gap to fill in with a plausible number.** The two- and three-lane
suspension towers almost certainly share the five-lane's distances. Almost certainly is not measured,
so they have no entry and are sized by the road — which is what every tower was sized by before any of
this was measured, and is therefore not a regression. A family with no recorded archetype falls back
the same way and says so in the report.

That is the whole shape of extending to a new family: measure it, record it under its own key, and let
the fallback carry it until then. What must never happen is a family borrowing another's numbers
because the code had nowhere else to look.

<a id="10-what-is-not-generated-is-refused-and-says-why"></a>

## 10. Unsupported inputs are reported; movable families are in scope

Draw, lift and pedestrian movable bridges are not deferred families. Implement their measured,
hardcoded construction rules and route executable generators through the existing UI and registry.
Report actual unsupported inputs, missing generation data and failed construction stages. Do not
substitute another bridge, publish incomplete output or treat a stub as support.

Preserve native content prerequisites and each style's source identity. The source used to produce
immutable data is selected during development; another player need not possess the identical
prototype authoring file. Required resource availability follows section 2 and the current policy.
`Extradosed01` retains its base-game identity; native `GoldenGate` and pack-provided
`GoldenGateDouble` remain distinct archetypes.

## 11. A double deck bridge is built from a double deck archetype, or not at all

Two decks is not one deck with a road hung underneath it. A double deck archetype's towers, portals and
cables are drawn around two levels at one particular separation and on one particular side; take a
single deck bridge, hang a second net below it and you have two structures that were never designed to
stand together.

So the archetype is chosen by the same rule as everything else — follow the one that already is what is
being asked for:

- Asked for two decks, the candidates are **filtered** to variants that carry an `AuxiliaryNets`
  arrangement of their own, not merely preferred among all of them. Double deck variants used to be
  penalised in selection, which is the opposite of what a request for two decks means.
- With no such variant the style **has no double deck version**, and generation fails saying so.
  Inventing the arrangement is what produced the fault.
- The arrangement is carried across whole: `m_Position` and `m_InvertWhen` are the archetype's, and
  only `m_Prefab` changes, because only the deck itself is what this mod generates.

**The separation is not adjustable.** It was a slider from four to twenty-four metres, and every value
on it except the archetype's own puts the second deck through geometry modelled to clear it. The offset
was also written as a negative y whatever the archetype said, so a bridge that carries its second net
*above* the deck — the extradosed bridge does — had its levels the wrong way round. Both are read from
the archetype now, and both controls are gone from the settings rather than disabled: a control that
cannot change anything is worse than none, because it says the value is a choice.

<a id="12-a-bridge-is-modified-only-on-its-own-git-branch"></a>

## 12. Branches organize work without blocking integration

Inspect the current branch and preserve unrelated changes. Existing `bridge/<style-slug>` branches
remain optional organization, not prerequisites for reading, editing or building a style. A multi-style
task may implement shared machinery and style-specific immutable data on one suitable working branch.
Use focused commits and keep every variant's evidence distinct. Repeated branch switching and
cherry-picking must not replace implementation work.

<a id="13-stop-the-game-clean-up-affected-bridge-styles-and-deploy-code-changes"></a>

## 13. Deploy coherent increments and preserve healthy assets

Build and deploy each coherent, buildable code increment before delivering it. Intermediate source
edits within that increment do not each require shutdown or deployment. Before replacing game-loaded
binaries, invoke `taskkill /IM Cities2.exe /F` and verify the process is absent. Existing user
authorization for that shutdown remains in force. Back up only original installed files, copy the
build outputs and verify their hashes. Documentation-only changes require none of these operations.

A new style or uncalled generation stage does not invalidate existing assets. Do not delete healthy
bridges merely because generator code changed. When a concrete incompatibility requires recovery,
identify the affected style IDs and UUID-owned artifacts, follow the existing backed-up recovery
procedure, verify the result and preserve unrelated assets. This changes no self-check, migration,
explicit deletion or ownership semantics. Deployment is not in-game visual acceptance.

## 14. Preserve each archetype's width allowance

Every bridge generated by this mod preserves a bridge-type-specific width allowance:

    archetype constant = archetype bridge width - archetype deck width
    generated bridge width - generated deck width = archetype constant

"Bridge width" and "deck width" must use the same documented geometric boundaries in both terms and
in both objects. Width is always the complete left-to-right span `maxX - minX`. The audit must measure
and record both bounds; it is forbidden to substitute one side's x coordinate, `abs(x)`, or twice a
one-sided boundary for width. A tower width may not be compared with a road-surface width in one
object and an outer railing or visible-deck width in the other. Asymmetric roads must retain separate
recorded left and right boundaries where a single total width would hide the mismatch.

The constant is evidence, not an assumed parameter. Establish it by dumping and measuring both:

1. the real, matching bridge archetype; and
2. a real bridge newly generated by this mod from a selected road.

Source arithmetic, recorded nominal widths, a compiled assembly, an offline fixture, a screenshot or
an old archetype by itself does not complete this comparison. If no newly generated bridge exists for
the bridge type being audited, a human must select any road and create a new bridge of that type.
Failure of the selected road to support that bridge type means another road must be selected; it does
not waive the sample requirement. A missing sample is never a skip condition and never completes the
audit. Creating and judging the sample is an in-game operation reserved for human judgment. The Agent
must not open or control the game, expose or consume a bridge-construction API, or create a persistent
request file for that operation. The Agent must identify the exact human sample-creation step and keep
the audit and its compliance claim explicitly incomplete until the real sample exists. Readiness for
human acceptance may complete an implementation task when that is the user's stated delivery boundary;
it does not complete the audit. The Agent must not invent a result or
report an absent sample as a skipped bridge, compliant bridge or final unchanged disposition.

For an existing generated sample, the audit executes exactly these two formulae, in this written
order:

    widthIncrement =
        (archetypeBridgeWidth - archetypeRoadWidth)
        - (generatedBridgeWidth - generatedRoadWidth)

    newBridgeWidth = generatedBridgeWidth + widthIncrement

All four width inputs are complete spans. `widthIncrement` is the increment of the complete generated
bridge span, not a displacement to apply independently at each side. All four measured inputs, both
subtractions and the final addition must remain IEEE-754 binary32. Parse source tokens directly to
binary32, emit round-trip text and the raw bit pattern, and prohibit decimal promotion, rounding,
display-value reuse, epsilon, tolerance, normalization, forced zero and bridge-specific result
overrides. A positive-zero result is unchanged only when its raw bit pattern is exactly `0x00000000`.

If `abs(widthIncrement) > 1.0f`, where `widthIncrement` is the complete-span increment, do not change
source code, generated assets or geometry for that bridge as part of the invariant pass. Record the
bridge type, archetype, selected road, both bridge left/right bounds, all four input bit patterns, the
width-increment and new-width bit patterns, and the reason it was skipped. A large
discrepancy is evidence that the bridge or the measurement basis needs separate investigation; it is
not permission for a large automatic correction.

Apply a verified correction to the full-detail mesh and every LOD together, subject to rule 8 and the
optional branch policy in section 12. Never turn this invariant into runtime geometric guessing.
The metaprogramming step may identify the affected authored parts and emit reviewed immutable data;
runtime code consumes only that hardcoded result.

Every bridge-generation correction must be completed during metaprogramming. The metaprogramming
output must be folded into the original immutable archetype parameters or into generated source data
before the mod is built. Runtime bridge-generation code is forbidden from calculating, looking up or
applying an audit/geometry correction. In particular, it may not contain a correction table keyed by
bridge style, add an audit delta after the ordinary width calculation, convert a one-sided correction
to a span, or post-process generated geometry to make an audited result fit. Runtime may use the final
hardcoded archetype parameters produced by metaprogramming as ordinary inputs; it must not know that
a correction step existed.

The measured audit evidence is
[`BRIDGE_WIDTH_INVARIANT_AUDIT.md`](BRIDGE_WIDTH_INVARIANT_AUDIT.md). A bridge without the required
real generated sample remains mandatory unfinished work. A human must select a road and actually
create a new bridge before the bridge can receive the `1 m` decision. It must not be marked skipped,
compliant or complete merely because the implementation's algebra appears to preserve the constant.

The measured Grand reference-road correction is a specifically authorized exception to the
1 m audit limit: use the native 19 m reference, without a runtime correction. See
[Grand reference evidence](GRAND_REFERENCE_WIDTH.md). It does not waive the limit for other cases.

## 15. Bridge lighting is copied from the archetype as behavior

A bridge's lighting is part of the archetype, not an optional decoration to reconstruct from a shared
template. A generated bridge preserves every bridge-specific light the archetype carries and every
field which controls its visible effect. This includes light objects placed directly in the bridge's
`NetSubObjects`, lights and other mounted objects inside a tower or pylon's `ObjectSubObjects`, the
referenced prefab's `EffectSource`, `LightEffect`, colour, intensity, range and culling behavior, and
each placement's position, rotation, parent mesh, group, probability and activation conditions.

Do not identify lights from prefab names. Names such as `WallLight`, `Spotlight` and `WarningLight`
are useful in a diagnostic report but are not a complete type system. Carry the authored component and
its prefab references whole. A referenced light/effect prefab which is not itself derived remains the
exact shared archetype reference. If a carried component points to a prefab which is being derived,
repoint only that reference to the corresponding generated prefab, as required by rule 2.

Tower-mounted objects must remain attached when the tower width changes. The recorded Golden
ornament lamps follow section 5's ornament map; the following rigid rule applies to other mounts. `m_ParentMesh` identifies the
authored mesh part whose displacement applies. A child object standing at non-zero x is a rigid side
part and its x position moves by the same signed half-width delta as that parent mesh; a child at
`x = 0` remains on the centre line. Its y and z coordinates, rotation, parent-mesh index, group,
probability and activation data do not change. This uses only comparison with `x = 0` and does not
permit a non-zero runtime coordinate heuristic or any geometric guessing.

The user-selected road remains the deliberate road-structure difference allowed by rule 3. Its
ordinary street lights stay those of that road. They are not replaced with the archetype road's street
lights. Bridge-specific lights mounted on the archetype's towers, pylons or bridge-only sub-object
entries are carried in addition to that road behavior, with the archetype's duplication and placement
rules preserved exactly.

Lighting validation follows rules 6 and 7. Compare the real archetype and generated prefab dumps,
including nested tower replacements, and verify that their bridge-specific light/effect references and
non-width fields match. Compilation is not evidence of a lighting match. Final validation is a human
in-game comparison at night in near and far views; the Agent must not open or control the game.

## 16. Preserve native construction and upkeep pricing

Preserve the source
roads' and bridge prototypes' `PlaceableNetPiece`/`PlaceableObject` construction, elevation and
upkeep fees. The game selects composition pieces and calculates charges for network length,
height, auxiliary networks and placed objects.

Do not store a manager-defined total, apply bridge-specific multipliers, offsets or floors, zero
auxiliary fees, or override runtime native cost data. Do not create cost-only pieces, sections,
objects or private graph copies for pricing. Geometry-derived private assets retain native fees.

Legacy conversion removes the custom `BridgeConstructionCost` component while preserving the
native dependency graph, bridge/deck UUID and CID, and geometry. Previously baked native fees
remain valid data: do not guess their original values or delete referenced copies. Restoring fees
requires authoritative original data and a separately verified migration. Deployment cleanup
follows section 13; pricing history alone is not permission to delete existing bridges.

## 17. Persist external dependencies as identical-CID copies owned by each bridge

The existing bridge persistence workflow must also preserve its external asset dependencies locally.
A bridge saved in `ImportedData` can be deserialized before a subscribed asset pack becomes available.
A nonempty CID in a valid bridge file does not make that dependency available at that earlier phase.
Persist the required external files alongside the bridge instead of relying solely on the later
registration of the subscribed source.

### Persistence scope: generated data, references and dependency snapshots

[Bridge asset persistence and reference policy](../Bridge-Asset-Persistence.md) is a normative
supplement to this section. It applies to fixed and movable bridge generation and is grounded in
the existing owned-node writer, geometry factory and dependency-copy path.

Save generated or modified owned Prefabs and resource data, including their required native
configuration and references. Reuse unchanged built-in surface, material, texture, marking,
animation and attachment resources by reference. Geometry conversion alone does not require new
assets for those resources. A moving marking representation may require new geometry or native
placement without requiring a new marking texture or material.

Keep the external-dependency requirements below: unchanged non-built-in dependencies require
original-byte, original-CID snapshots; this is distinct from creating a modified resource with a
private identity. This includes materials from other mods and Bridge Builder's packaged materials,
with their required non-built-in textures and VT payloads. Retaining the source mod reference alone
is insufficient; reuse within the current owner must not borrow another bridge's private files.
A genuinely necessary resource conversion must identify the changed data and
its native serialization requirement. Do not save preview-only objects or shader state as bridge
assets, and do not omit required references by interpreting "no resource export" as "no reference".

### Copy the serialized dependency graph without changing asset identity

- Start with all persisted prefabs belonging to the bridge, including both decks and derived assets.
  Recursively follow serialized external CID references through non-built-in dependencies until each
  branch reaches a built-in asset or an asset with no further serialized dependencies. Include the
  necessary non-prefab dependencies, such as external geometry and materials; copying only the first
  external prefab is insufficient.
- Identify built-in assets from the game's asset provenance, not from names or apparent similarity.
  Built-in resource-map `UnityGUID` references are terminal. Do not copy or modify original game/DLC
  assets. Already-persisted assets owned by this bridge are traversed without making a redundant copy.
- Read original asset bytes, including entries inside source packages. Copy those bytes unchanged and
  retain the original CID in the `.cid` sidecar. Do not reserialize the prefab, change its name, replace
  its CID, or rewrite its references. Do not mutate, move or delete the source asset.
- This rule concerns unchanged dependency snapshots. Newly generated or geometrically modified
  bridge assets retain their own identities under the existing generation rules. It does not permit
  a modified asset to impersonate its donor by reusing the donor's CID. The shared references required
  by section 15 remain unchanged; this rule supplies local copies of their non-built-in targets.

### Deduplicate within a bridge, never across bridges

Store copies under:

```text
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>.cid
```

Native `.MidMips` snapshots are the narrow filename exception: use
`Dependencies/<CID>/<original TS*_MB*.MidMips basename>` and its `.cid` sidecar.
Native streaming derives the tile-size/mip-count tag from that basename. Preserve the
original cache bytes and CID; do not rename the cache to `<CID>.MidMips` or fabricate empty data.
Traverse Surface-to-MidMips dependencies through the cache's native primary and duplicate Surface
CID memberships, including current-owner snapshots when the source package is absent. The same
other-owner exclusion and conflict checks apply before reading or copying these caches.

Material contract section 5 permits a read-only packaged source at
`Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` with its CID sidecar. This is the sole
cross-style prepared-resource exception. Each owner still copies only its required native cache
dependencies independently; the source exception does not permit shared generated-owner files,
general cross-style texture storage or additional runtime baking.

Use a visited-CID set for each bridge traversal. Multiple references to the same CID within one bridge
produce one copy, including when a dependency is reachable through several paths. Traversal must
terminate even if a future dependency graph contains a cycle; do not assume every source graph is a
DAG. Separate bridges must each retain their own physical copy of a shared external CID, in their own
UUID directory. A global deduplication cache must not eliminate those per-bridge copies.

The unchanged source and the per-bridge copies deliberately share a CID. Equality of CID alone is not
proof of equality of content: if an existing destination has different bytes for that CID, report the
conflict and fail persistence rather than silently overwriting it. Preserve all required references;
an unavailable dependency is an explicit persistence failure, not permission to omit a section,
sub-object, light, material or other dependency. Do not report successful bridge creation until the
required copies have been saved and verified.

### Ownership, self-check and acceptance

Ownership of these copies comes from the exact bridge UUID directory, not the copied prefab's original
name or CID. File self-check must recognize intentional identical-CID copies across bridges and must
not retire healthy bridges merely because these snapshots duplicate an asset identity. Existing
checks for unintended collisions among independently generated assets remain separate.

Back up and retire dependency copies with their owning bridge. Deleting one bridge must not remove
another bridge's copy or the source asset in its original package. Ownership-scoped maintenance and
cleanup tools must include the dependency directories, including partial files from interrupted saves.

Dependency copying is a file persistence operation. Do not add live integrity inspection,
reference repair, quarantine or registration rejection to that operation. The separate post-load
self-check uses the read-only inspection expressly defined below and in section 19. Resolving an asset identifier
to its original data stream for copying is distinct from inspecting a live prefab for damage.

Verification must cover recursive text/binary references, stopping at built-in assets, byte and CID
preservation, per-bridge deduplication, independent copies across bridges, cycle termination, explicit
failure on missing/conflicting sources, and owner-scoped cleanup. Cold-start and old-save acceptance
remain required to establish that the game discovers and resolves the copies before network
initialization. File equality, a DAG check or a successful build alone does not establish that timing.

### Post-load inspection and recovery prerequisites

Run self-check only after native loading, mod initialization, PDX database caching and active asset
batches complete at a ready main menu. Observe current state as well as completion events so late
mod loading cannot miss inspection. Include cached user PrefabAssets that failed registration,
not only PrefabSystem's registered list. Do not force-load, republish, repair live references,
reject registration or quarantine assets to perform inspection.

Inspect required live references read-only. When no cached prefab instance exists, a targeted
serialized inspection is permitted. For a required null slot, read its exact original serialized
CID and check whether the dependency already has a cached instance. If it does, recursively copy
permitted unchanged dependency sources under the owning bridge UUID. Keep live references unchanged
and require restart; never serialize a still-null live reference over the recovered disk data.

Follow section 19 to complete missing fields and dependencies while preserving UUID/CID and valid
values. Do not invent historical recipe data. Healthy current assets remain unchanged. Unavailable
required content, explicit nulls without recoverable CIDs, failed registration, unknown required
serialization, or migration/copy failures enter removal when migration fails or is impossible.
Process each bridge independently. File ownership is determined by section 18, not an additional
content, hash or concurrent-snapshot verdict. Actual filesystem failures must be reported.

## 18. Bridge asset file ownership is determined only by b{uuid} text

A bridge asset file belongs to Bridge Builder if its relative asset path contains
`b{uuid}`. For one bridge, use literal substring matching with the fixed bridge UUID.
For all bridge assets, use the unanchored regex
`b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}`.
Do not require token boundaries, exact filenames, metadata, components, registration,
CID contents, hashes, dependency graphs or successful deserialization to establish ownership.
A matching ancestor directory owns its entire subtree, including empty directories and
CID-named dependency copies. Apply this rule before every bridge asset read, write, move
or delete, including startup recovery, management, generation and maintenance scripts.

Keep operation scope restricted to intended asset roots; paths outside those roots and
unsafe destinations are not authorized by an incidental match. Path containment and IO
errors are operational safeguards, not additional asset ownership criteria. Native SDK
asset location information may locate bytes but must not classify ownership. Reading
external source assets for dependency copying does not make those sources bridge-owned.
The shared UI asset pack and non-asset settings/logs are not individual bridge assets.

Metadata may be read or written as payload (for example, display names and creation style IDs), but
its presence, absence or values never establish or revoke file ownership. Section 19 governs
field-by-field migration, including native payloads and file layout; section 17's dependency-copy
operation alone does not rewrite source metadata. For deployment cleanup, section 13 selects the
affected style IDs and bridge UUIDs first; this section then determines the files owned by those
UUIDs. Move selected files/directories as whole units with collision-free backup names. Do not
inspect contents, hashes or tree snapshots to establish ownership. Report real move failures.

### Source isolation between bridges

Whenever creating a bridge or any dependency/migration copy, exclude another generated bridge's
ImportedData resources, including Prefabs and materials under its UUID dependency directories.
Determine ownership from path UUID text before opening the stream; matching CIDs do not permit
borrowing another bridge's private resources. Try the original source or a permitted packaged
same-CID source. Current-owner assets remain valid inputs for additive migration only while
their files exist. If no permitted source exists, fail explicitly without omitting dependencies.
Apply this rule to first-match and fallback CID lookup, generation inputs, native validation,
texture streaming and legacy dependency-copy selection. Deleted files must never be reopened.

## 19. Independent native persistence and additive legacy conversion

### Manager independence and additive conversion

**Generated bridges must not depend on Bridge Builder. Bridge Builder is only the bridge asset manager.**
This is an architectural requirement for generation, persistence and legacy conversion. Disabling,
unsubscribing from or uninstalling Bridge Builder must not make persisted bridges unavailable or
prevent their loading, display, native unlocking, construction, pricing or save/load operation.
These behaviors must work after a cold start without the Bridge Builder assembly, runtime callbacks,
patches, custom UI hosts or cached mod objects. Required base-game/DLC resources remain native
dependencies; external asset snapshots still follow section 17. Acceptance must include a cold start
with Bridge Builder absent and loading an existing save; a warm-session cache is not evidence.

Bridge Builder manages assets; new/converted saved prefabs must not depend on its assembly,
runtime unlocking, price overrides, localization source or custom UI host. Persist nested native
Unlockable AND/OR conditions, unchanged native construction fees, native LocaleAsset
names and native thumbnail presentation. The double-deck railway seam work remains
an outstanding compatibility limitation, not permission to persist a mod dependency or to declare
complete independence.
Follow section 16: do not rewrite native fees or create pricing-only copies. Preserve original
donors and shared component fees. Keep recursive byte-identical external dependency persistence
with original CIDs.

Legacy migration during post-load self-check is **additive**: preserve bridge and carried-deck UUID,
CID, native prefab version, name and geometry. Save into the existing asset, never a new bridge identity.
Add private dependencies and native payloads; retain original component files. Back up existing files,
verify identities and CID sidecars after saving. On conversion failure, roll back the incomplete phase,
log ERROR and remove the affected bridge. If backup/move fails, permanently delete remaining owned
source paths. Restart after successful migration; do not replace live
network registrations. Healthy already converted assets need no rewrite. The explicitly
authorized shared legacy-name cleanup still requires reference checks before retiring old files.

### Self-check order: migrate, back up/move, delete

**The primary objective of self-check is normal game operation. Attempt asset version migration first; if migration fails, attempt backup and removal from the loading directory. If backup or moving fails, continue deleting the remaining original assets rather than retaining them.**

Apply this sequence per bridge. First attempt version migration using the shared generation
format: complete missing fields and dependencies, preserve bridge UUID/CID and valid values,
and normalize file layout/naming. Already valid current assets remain unchanged. Successful
migration is reported as migration and must not fall through to removal. If migration fails or
required information/dependencies make migration impossible, record the reason, roll back the
incomplete migration phase, and proceed to recursive backup/move. Do not skip possible migration
merely because an asset is old or has missing fields. A backup copy alone does not complete removal;
the failed asset must also leave the active asset directories.

For a bridge already selected for removal by self-check, first attempt recursive backup/move of
its UUID-owned files/directories. If creating the backup destination, backing up files, or moving
any selected path fails, permanently delete the remaining selected source files/directories.
Do not return early, retain the faulty bridge, or skip removal merely because backup is unavailable.
If a move partially succeeds, keep successful backups and delete the remaining owned source paths;
do not restore faulty assets into active asset directories after entering removal.

Use the existing b{uuid} string-only ownership rule and intended asset roots. This fallback applies
only to bridges selected for removal, not healthy bridges or unrelated assets. Process all selected
paths and bridges even when one operation fails. Verify that selected sources are absent before
reporting removal success. Record backup failure separately from deletion failure: a failed backup
followed by verified deletion is successful removal without a complete backup. If permissions or
another filesystem error also prevent deletion, report removal failure and the remaining paths;
never claim success or imply that the mod can override operating-system permissions.

Self-check still does not unregister live prefabs or promise to repair the current game session.
Require restart after removal; if removal fails, restarting alone does not resolve retained assets.

### Post-load self-check requirements

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

Source isolation requires: exclude other bridges'
ImportedData assets of every type, including same-CID private dependencies, before reading. Use only original or
permitted sources, or fail. Ownership remains path-string-only.

Self-check must supplement missing copies of other mods' materials and their required non-built-in
dependencies for every inspected owner, including bridges with valid live references and current
metadata. Run the creation dependency traversal before live-reference validation; do not wait for
missing materials or a version change. Keep existing complete copies unchanged, preserve UUID/CID
and geometry, and report new copies as migration requiring restart. Resolve only original sources,
permitted packaged resources or existing current-owner files. Unavailable sources and byte conflicts
remain explicit migration failures under the policy above; never recover from another bridge.

### Explicit bulk deletion

The confirmed settings action to remove all bridges must run the same sequential deletion pipeline
as individually deleting every Bridge Builder bridge. It is available in a ready main menu or loaded
city. Permanently delete UUID-owned files, not move them. In a city remove placed bridge networks and
wait for native entity cleanup before deleting files and unregistering the owned prefabs, including
clearing tool/menu references. Keep the dangerous-operation confirmation, with localized
"Continue removal" action and text describing permanent deletion and effects on other saves.
Self-check retirement is separate: it still backs up/moves files and requires restart, without live
prefab unregistration. Neither operation replaces the style-scoped deployment cleanup in section 13.

### Migration failure falls back to removal

After failed dependency, native-conversion or legacy naming migration leaves an unusable bridge,
restore incomplete changes from that phase, then retire only the affected owner through recursive
backup/move. Process owners independently; do not unregister live Prefabs, and require restart.
A failed backup/move must fall back to deleting remaining selected source paths. Report removal
as removal, not migration success; verify absence and report actual failures. A layout collision
alone does not authorize retiring a healthy bridge: preserve it and report incomplete migration
under the persistence policy. Never claim files were removed while they remain.

### Field-by-field migration

Version migration must complete missing fields individually, because legacy bridges may originate from different versions and lack different fields.
After migration, bridges must use the same file structure and naming conventions as bridges generated by the current version.

Inspect each persisted field/component independently on the root and its owned networks. A version
marker or one complete root must not skip checks of other fields, auxiliary networks, locales,
dependencies, names or layout. Preserve valid values, native unlock semantics, UUID/CID and geometry;
write only assets needing an upgrade. Unknown optional historical values remain empty rather than
invented; missing required information must produce an explicit migration failure followed by removal.
Use the same native persistence helpers and naming/layout rules as generation. Repeated migration
must make no changes after successful completion; report it as migration and require restart.

### Legacy naming migration exception

Legacy naming migration exception: self-check must replace reachable
pre-UUID sections reached through the canonical bridge root's CID dependency graph, plus their
derived pieces/LODs/geometry (never use section-name regexes or child-name prefixes)
with per-bridge copies named by appending "-b{uuid}" to the first space-delimited name token. Rewrite file references to new private CIDs, back up originals,
and remove old files only after no local prefab references remain. Do not mutate live prefabs; require
restart. This exception permits graph-based source selection and changed identities only for
these pre-UUID files during explicit rename migration; ordinary snapshots remain byte-identical.

### Unified file layout and truthful migration notices

After successful self-check migration or repair, an old bridge must use the same directory layout,
file naming rules, CID sidecars and dependency-copy layout as a newly generated bridge. Generation
and migration must share the layout implementation; do not maintain an old-only naming scheme or
skip layout migration merely because native component conversion already succeeded. Identity values
may differ between distinct bridges, but their naming and directory conventions must match. Preserve
the existing bridge and deck CID, UUID and geometry. Back up before relocation, handle collisions
without overwriting unrelated files, and leave a repeated migration unchanged. Retained historical
backup files belong outside the game's active asset directories, subject to shared-reference safety.

Use [asset persistence sections 6–7](../Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
for owner-local Prefabs, Geometry, Appearance, VT/mid-mips, Locales and Dependencies. Legacy sibling
dependency directories remain migration inputs. Preserve healthy bridges on layout collisions;
native cold-load acceptance remains separate.

### Self-check result summary

Automatic and manual self-checks report only this run's final result per bridge UUID, exactly once
in one mutually exclusive category: migrated to the latest format; removed with a complete backup;
removed with a failed or incomplete backup; or unfinished. Migration is complete only after all
required dependency, native-format, naming and layout work succeeds. A layout conflict or partial
migration is unfinished, even if earlier phases wrote files. Removal supersedes earlier migration;
backup failure followed by verified deletion belongs to removed without complete backup, not
unfinished. Remaining or inaccessible selected files, incomplete enumeration and owner-scoped
exceptions are unfinished. Continue the established deletion fallback after backup failure.

Show one native localized dialog containing only nonzero categories, each with its count, in the
order above, followed by one shared restart reminder. Migration text must explicitly say migrated
to the latest format (Simplified Chinese: `已迁移 {0} 座桥梁至最新格式。`). Provide all twelve supported
languages with identical count placeholders. Do not claim incomplete backups are complete or that
restarting alone repairs unfinished operations. A successful run with no processing results shows
no dialog, whether automatic or manual. An unattributable global exception may have an uncounted
failure explanation and explicit log, but must not invent an owner or count.

Create fresh reporting state per run; never repeat old migration/removal/incomplete totals. Retain
session-level protection against reading copied, moved or retired resources through stale cached
references. Skipped session-protected owners are not new results. Preserve completed outcomes when
another owner fails, and report partial-file failures honestly. Human native-dialog interaction
remains separate from automated lifecycle and localization checks.

## 21. Movable bridges use generated selected-road surfaces and native mechanisms

[MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md) is the normative supplement for draw,
lift, pedestrian and subsequently admitted movable mechanisms. Read it before their development.
Its sections 2–12 define the general generation, rendering, graph and persistence method; section 15
records encountered failures and mandatory solutions. The method derives from the implemented
pipeline; per-style dimensions and historical section tables are evidence, not universal rules.
Section 7 requires source-faithful materials, measured UV projection and paint integrated into
mechanism geometry, with separate skinned paint under sections 18–19 of the movable contract.
Material preparation follows the material generation contract. Section 11 requires
selective Section/Piece persistence, native unlock semantics and native-format reader compatibility.

Generate the visible moving road from the selected road's resolved closed-pose composition, convert
it into persistent object geometry, partition it and bind it to the measured native mechanism.
Preserve native traffic connectivity, crossing signals, watercraft clearance and manager-independent
operation. Do not substitute the prototype road surface or a mod-owned animation/signal controller.
Network mapping is specific to the measured bridge archetype and selected road/state. The supplement's
Large Road to DrawBridge03 mapping is an example, not a universal section or node/edge template.

The user's selected-road conversion requires new road-surface topology, skinning and, where needed,
network-to-object attribute conversion. This is a scoped construction operation on the generated
road surface, not permission to re-encode carried archetype structure meshes under section 4.
Section 8's structural widening and offline part-identification rules remain unchanged. Use measured
partition data; do not infer authored hinges or part membership at runtime.

There is no deferred-family gate. Integrate executable variants using their own hardcoded generation
data, and report the supplement's verification evidence separately. Documentation and discovery are
not implementation; compilation and preview are not full acceptance. Sections 12–19 govern workflow,
scoped recovery, width audits, native pricing, ownership, persistence and migration.

## 22. New bridge styles require immediate UI synchronization, generator routing and multilingual names

Whenever a new bridge style is created, immediately synchronize the UI and connect the new style
to the generator router. Complete both in the same implementation increment. This applies to
fixed and movable bridges alike. A completed generator without a working creation-panel entry is
an incomplete style implementation; UI integration must not be deferred to another user request.

- Register the stable style ID consistently in the style definitions, catalogue, generator route,
  supported deck modes and UI bindings. With its prerequisites satisfied, the style must appear in
  the appropriate creation list and participate in search, filtering, selection and preview.
- Connect both creation actions to that style's actual generator. Keep management labels and style
  filters consistent with the creation panel. Preserve existing DLC and source-availability checks.
- Supply the style name in every supported UI language through the existing localization tables.
  Current coverage is English, Simplified Chinese, Traditional Chinese, German, Spanish, French,
  Italian, Japanese, Korean, Polish, Brazilian Portuguese and Russian. Extend coverage when the
  project's supported-language set grows; a Chinese-only label is insufficient.
- Use stable, unlocalized IDs for routing, persistence and selection. Resolve displayed names from
  the active game locale and refresh them when the language changes. Preserve user-entered bridge
  names. Keep English fallback for unsupported locales, but do not use fallback to conceal missing
  translations in a supported locale.
- Verify catalogue visibility, supported deck modes, preview selection, both creation actions,
  management labels and language changes together. Run the existing relevant localization checks;
  record any pending human UI verification explicitly. Compilation alone is not UI acceptance.

### Registered-style behavior parity for testing

All registered bridge styles must use identical UI and operation behavior regardless of implementation
completeness. This requirement applies to fixed and movable styles.

- Never display a notice, warning, tooltip, badge, placeholder or other message stating that a bridge
  style is unfinished, incomplete, not ready or still under development. Its messages and notification
  behavior must be identical to those of completed styles.
- Do not disable creation, create-and-build or preview for any registered style because its
  implementation is incomplete or its generator is not connected. Do not hide such styles or prevent
  their requests from reaching the generator router. Apply the same ordinary input, DLC, source and
  deck-mode requirements to every style; completion status must not introduce an additional gate.
- A creation or preview request for a registered style without a connected generator must explicitly
  fail with an **ERROR** identifying the requested style ID and the missing generator route. Surface
  it through the same operation-failure handling used for completed styles and record an ERROR-level
  diagnostic. A silent return, warning-only log, disabled control, substitute bridge or reported success
  is prohibited. The error describes the actual routing failure, not the style's completion status.
- Use the same request dispatch, progress, success and failure handling for all registered styles.
  Report success only when the requested operation actually succeeds. This rule does not authorize
  publishing invalid or partial assets, or claiming that an unverified generator is complete.

<a id="23-gpu-computation-and-parallel-bridge-generation"></a>

## 23. Performance follows measured bottlenecks

Use CPU or vendor-compatible GPU APIs as appropriate. Prefer GPU execution for large pixel workloads
and parallelize independent work when beneficial. A correct bounded CPU path is permitted; GPU
conversion is not a prerequisite for integrating a generator. Do not require vendor-exclusive APIs.
Respect Unity/game thread affinity, resource ownership and deterministic construction. Measure and
fix actual slow stages without postponing complete generation for speculative optimization.
This runtime scheduling policy does not authorize delegation to coding agents.

## 24. Movable bridge asset size

Report delivery size using three distinct scopes: (1) the source-delivery files selected by Git,
with component subtotals and an explicit distinction between tracked files and pending additions;
(2) the complete Paradox Mods content package after official postprocessing and SDK packaging,
including embedded data, prepared materials, required libraries, UI files and any shipped symbols;
(3) one generated bridge's complete asset footprint. For scope 2 measure the final compressed ZIP's
actual length, state the publication file list, toolchain and whether it was packaged locally or
downloaded from a published version. A local SDK package is not proof of publication. Covers and
listing screenshots are separate uploads, not installed content. For scopes 1 and 3 sum actual file
lengths without transport compression. State exact bytes and units for every scope; disk allocation
and an unpacked build-directory total are not package-size substitutes. Local authoring intermediates and backups are not delivery-size metrics. Keep them
out of source publication and compiled output, and do not describe their size as mod or bridge size.

Keep the generated bridge's material footprint as small as possible without compromising
material resolution. Material generation contract sections 2 and 5 impose two separate limits:
10,000,000 bytes for one generated owner's complete persisted material dependency graph, and
55,000,000 bytes for the complete compressed official SDK content package. The working material
budget is 5–10 MB; smaller outputs must not be padded. A prepared style-directory subtotal proves
neither limit, and an unavailable complete-owner measurement must remain explicitly unverified.
These limits do not define a byte ceiling for geometry or the entire generated Prefab graph;
the complete-graph comparison below remains independently required. An oversized package is not
a final deliverable or ready for human acceptance. Reduce redundant storage through original-resource
reuse, identical-resource sharing and source-density tiling, not resolution loss. Continue to
report the exact complete bridge footprint, including geometry, every LOD, materials, texture
and VT/mip payloads, dependency snapshots, presentation resources and CID sidecars. Control
mod-package size through the same reuse policy without duplicate baking, and report it separately
from the size of an individual generated bridge.

The total size of a generated movable bridge Prefab, including its dependencies, must be
comparable to the corresponding original movable bridge. The root Prefab file alone is not
the measurement. Count the root, generated networks/objects, every LOD and geometry payload,
surfaces, textures, required presentation assets and external dependency snapshots. Deduplicate
logical dependencies by identity within one bridge; also report physical files/bytes, including
CID sidecars and any duplicate copies. Built-in resources remain references under section 17:
include their reachable payloads in the logical comparison without exporting duplicate files.

Compare like-for-like content and serialized payload sizes using a declared compression basis.
Do not compare a complete game/DLC archive with one generated bridge, use shared UI catalogue
back-links to inflate the reference, or compare compressed bytes with decoded texture memory.
Record both serialized size and decoded texture memory separately where available. Explain
selected-road differences. Unresolved references or an absent generated sample leave the total
comparison incomplete; a texture subtotal or render-only subtree cannot establish compliance.

Reuse identical appearance resources across a bridge's LODs; avoid per-triangle texture/prefab
explosion and redundant constant textures. Do not satisfy the size requirement by omitting
required dependencies, dropping LODs, silently reducing appearance quality or introducing a
manager-owned runtime reconstruction dependency. Measure a human-created persisted bridge
before claiming this requirement is met. No arbitrary acceptance ratio is implied by this rule.

## 25. Adapt sidewalk-dependent components to the actual elevated road

This rule applies to every bridge generator, including fixed and movable bridges. Roads do not
necessarily have sidewalks on both sides, and their sides are not necessarily symmetric: a road
may have no sidewalks, a sidewalk on only one side, or sidewalks of different widths on its two
sides. Resolve the selected road's actual elevated-mode composition and applicable upgrade state;
do not infer sidewalks from its ground-mode appearance, lane count or the prototype bridge.

Only sections in the `Sidewalk` family count as sidewalks. Curbs and all other section
families are not sidewalks and must not be classified as sidewalks when resolving either side's
sidewalk presence, position or width.

Adapt every sidewalk-dependent component, including inner bridge railings and bridge-authored
sidewalk portions, to the presence, position and width of the actual elevated sidewalk on each
side independently. A side without a sidewalk must not receive a fabricated sidewalk or a
component whose purpose depends on that sidewalk. Do not mirror one side onto the other, assume
equal widths, or reserve a prototype-width sidewalk strip in the carriageway. Distinguish these
components from structural or outer safety railings that do not depend on sidewalk presence;
absence of a sidewalk alone does not authorize removing unrelated bridge structure.

The same requirements apply to sidewalk portions embedded in a composite bridge component,
including fixed bases, end platforms and transitions next to a movable mechanism. A component's
fixed or structural role does not exempt its sidewalk portion. Identify that portion from measured
archetype membership and adapt its presence, position and width independently on each side to the
selected elevated road's `Sidewalk` sections. If a side has no such section, do not retain or generate
that sidewalk portion. Preserve unrelated load-bearing structure and outer safety components;
do not remove the whole composite component merely to remove its sidewalk portion. Apply this
rule to the highest-detail mesh, every LOD, preview and persisted geometry.

Apply the resolved layout consistently to preview, formal generation and all LODs. Preserve the
selected road's native lane semantics and use measured archetype component membership when
adapting bridge-owned geometry. This rule governs sidewalk-dependent presence and placement; it
does not authorize guesses about structural parts or changes to unrelated geometry. Verify roads
with zero sidewalks, one sidewalk and unequal left/right sidewalk widths when claiming support.

## Current repository checklist

1. Extract and hardcode construction data from the correct real prototype; generation must not
   require another player to possess the developer's original prototype files.
2. Supply the necessary resource graph through permitted sources and independent persistence.
3. Generate the selected road, native motion, attachments and all LODs with correct part transforms.
4. Preserve native attributes, material contract section 2's mapping-specific density and original road bones; do not cut receivers for paint.
5. Integrate routing, UI and all supported languages; preserve fees, unlocks and identities.
6. Build/deploy coherent increments and scope any necessary recovery to affected owned assets.
7. Verify actual geometry, saved references and native behavior; label pending visual/cold-load checks.
